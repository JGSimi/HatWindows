using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Hat.Models;
using Hat.Models.NetworkModels;

namespace Hat.Services;

/// <summary>
/// Multi-provider AI API service with streaming support.
/// Port of AIAPIService.swift — supports Ollama (local), OpenAI-compatible, and Anthropic.
/// </summary>
public class AIApiService
{
    private static readonly Lazy<AIApiService> _instance = new(() => new AIApiService());
    public static AIApiService Shared => _instance.Value;

    private readonly HttpClient _httpClient;

    private AIApiService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
    }

    /// <summary>
    /// Maps HTTP status codes to user-friendly Portuguese error messages.
    /// </summary>
    private static Exception FriendlyError(int statusCode, string? rawBody = null)
    {
        var message = statusCode switch
        {
            401 => "Chave de API invalida ou expirada. Verifique suas configuracoes.",
            403 => "Acesso negado. Sua chave de API nao tem permissao para este modelo.",
            404 => "Modelo nao encontrado. Verifique o nome do modelo nas configuracoes.",
            429 => "Limite de requisicoes atingido. Aguarde alguns segundos e tente novamente.",
            >= 500 and <= 599 => "Erro no servidor do provedor de IA. Tente novamente em instantes.",
            _ => $"Erro de conexao (HTTP {statusCode}). Verifique sua internet e configuracoes."
        };
        return new HttpRequestException(message, null, (System.Net.HttpStatusCode)statusCode);
    }

    /// <summary>
    /// Truncates conversation history to fit within character budget.
    /// Keeps most recent messages, drops oldest first.
    /// </summary>
    private static List<ConversationTurn> TruncateHistory(List<ConversationTurn> history, int maxChars)
    {
        var totalChars = 0;
        var startIndex = history.Count;

        for (var i = history.Count - 1; i >= 0; i--)
        {
            var turnChars = history[i].TextContent.Length;
            if (totalChars + turnChars > maxChars) break;
            totalChars += turnChars;
            startIndex = i;
        }

        return history.GetRange(startIndex, history.Count - startIndex);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Non-streaming request
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public async Task<AIResponse> ExecuteRequestAsync(
        string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings, CancellationToken ct = default)
    {
        if (settings.InferenceMode == InferenceMode.Local)
            return await ExecuteLocalRequestAsync(prompt, images, history, systemPrompt, settings, ct);
        else
            return await ExecuteApiRequestAsync(prompt, images, history, systemPrompt, settings, ct);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Streaming request (IAsyncEnumerable)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public async IAsyncEnumerable<StreamChunk> ExecuteStreamingRequestAsync(
        string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (settings.InferenceMode == InferenceMode.Local)
        {
            await foreach (var chunk in StreamLocalRequestAsync(prompt, images, history, systemPrompt, settings, ct))
                yield return chunk;
        }
        else
        {
            await foreach (var chunk in StreamApiRequestAsync(prompt, images, history, systemPrompt, settings, ct))
                yield return chunk;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Ollama local streaming
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async IAsyncEnumerable<StreamChunk> StreamLocalRequestAsync(
        string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var trimmedHistory = TruncateHistory(history, 16_000);
        var messages = new List<OllamaChatMessage>();

        var trimmedSystem = systemPrompt.Trim();
        if (!string.IsNullOrEmpty(trimmedSystem))
            messages.Add(new OllamaChatMessage { Role = "system", Content = trimmedSystem });

        foreach (var turn in trimmedHistory)
            messages.Add(new OllamaChatMessage { Role = turn.Role, Content = turn.TextContent });

        messages.Add(new OllamaChatMessage { Role = "user", Content = prompt, Images = images });

        var payload = new OllamaChatRequest
        {
            Model = settings.LocalModelName,
            Messages = messages,
            Stream = true,
            Options = new Dictionary<string, double> { ["temperature"] = 0.0 }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/chat") { Content = content };
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
            throw FriendlyError((int)response.StatusCode);

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            if (chunk?.Message == null) continue;

            var isDone = chunk.EvalCount != null;
            yield return new StreamChunk(
                chunk.Message.Content,
                isDone ? chunk.GetTokenUsage() : null,
                isDone);

            if (isDone) break;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Cloud API streaming router
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async IAsyncEnumerable<StreamChunk> StreamApiRequestAsync(
        string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var trimmedHistory = TruncateHistory(history, 100_000);
        var endpoint = settings.GetEffectiveEndpoint();

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new UriFormatException("URL de endpoint invalida.");

        var isAnthropic = settings.SelectedProvider == CloudProvider.Anthropic;

        if (isAnthropic)
        {
            await foreach (var chunk in StreamAnthropicRequestAsync(uri, prompt, images, trimmedHistory, systemPrompt, settings, ct))
                yield return chunk;
        }
        else
        {
            await foreach (var chunk in StreamOpenAIRequestAsync(uri, prompt, images, trimmedHistory, systemPrompt, settings, ct))
                yield return chunk;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - OpenAI-compatible streaming
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async IAsyncEnumerable<StreamChunk> StreamOpenAIRequestAsync(
        Uri uri, string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var apiMessages = new List<APIMessage>();

        var trimmedSystem = systemPrompt.Trim();
        if (!string.IsNullOrEmpty(trimmedSystem))
            apiMessages.Add(APIMessage.FromText("system", trimmedSystem));

        foreach (var turn in history)
            apiMessages.Add(APIMessage.FromText(turn.Role, turn.TextContent));

        // Current user message with potential images
        var contentParts = new List<MessageContent> { MessageContent.TextContent(prompt) };
        if (images != null)
            foreach (var img in images)
                contentParts.Add(MessageContent.ImageContent(img));

        apiMessages.Add(APIMessage.FromArray("user", contentParts));

        var modelName = settings.ApiModelName;
        var isOModel = modelName.StartsWith("o1") || modelName.StartsWith("o3");

        var payload = new APIRequest
        {
            Model = modelName,
            Messages = apiMessages,
            Temperature = isOModel ? null : 0.0,
            MaxTokens = 4096,
            Stream = true
        };

        var request = CreateRequest(HttpMethod.Post, uri, settings);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw FriendlyError((int)response.StatusCode);

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (!line.StartsWith("data: ")) continue;
            var jsonStr = line[6..];

            if (jsonStr == "[DONE]")
            {
                yield return new StreamChunk("", null, true);
                break;
            }

            var delta = JsonSerializer.Deserialize<APIStreamDelta>(jsonStr);
            if (delta == null) continue;

            var text = delta.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(text))
                yield return new StreamChunk(text, null, false);

            if (delta.Usage is { PromptTokens: not null, CompletionTokens: not null })
                yield return new StreamChunk("", new TokenUsage(delta.Usage.PromptTokens.Value, delta.Usage.CompletionTokens.Value), false);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Anthropic streaming (SSE)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async IAsyncEnumerable<StreamChunk> StreamAnthropicRequestAsync(
        Uri uri, string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var anthropicMessages = new List<AnthropicMessage>();

        foreach (var turn in history)
        {
            anthropicMessages.Add(new AnthropicMessage
            {
                Role = turn.Role,
                Content = new List<AnthropicContent> { AnthropicContent.TextContent(turn.TextContent) }
            });
        }

        // Current user message
        var currentContent = new List<AnthropicContent> { AnthropicContent.TextContent(prompt) };
        if (images != null)
            foreach (var img in images)
                currentContent.Add(AnthropicContent.ImageContent(img));

        anthropicMessages.Add(new AnthropicMessage { Role = "user", Content = currentContent });

        var trimmedSystem = systemPrompt.Trim();
        var payload = new AnthropicStreamRequest
        {
            Model = settings.ApiModelName,
            MaxTokens = 4096,
            System = string.IsNullOrEmpty(trimmedSystem) ? null : trimmedSystem,
            Messages = anthropicMessages,
            Temperature = 0.0,
            Stream = true
        };

        var request = CreateRequest(HttpMethod.Post, uri, settings);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw FriendlyError((int)response.StatusCode);

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        TokenUsage? finalUsage = null;

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var jsonStr = line[6..];
            var evt = JsonSerializer.Deserialize<AnthropicStreamEvent>(jsonStr);
            if (evt == null) continue;

            switch (evt.Type)
            {
                case "content_block_delta":
                    var text = evt.Delta?.Text;
                    if (!string.IsNullOrEmpty(text))
                        yield return new StreamChunk(text, null, false);
                    break;

                case "message_delta":
                    if (evt.Usage is { InputTokens: not null, OutputTokens: not null })
                        finalUsage = new TokenUsage(evt.Usage.InputTokens.Value, evt.Usage.OutputTokens.Value);
                    break;

                case "message_stop":
                    yield return new StreamChunk("", finalUsage, true);
                    break;
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Non-streaming implementations
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async Task<AIResponse> ExecuteLocalRequestAsync(
        string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings, CancellationToken ct)
    {
        var trimmedHistory = TruncateHistory(history, 16_000);
        var messages = new List<OllamaChatMessage>();

        var trimmedSystem = systemPrompt.Trim();
        if (!string.IsNullOrEmpty(trimmedSystem))
            messages.Add(new OllamaChatMessage { Role = "system", Content = trimmedSystem });

        foreach (var turn in trimmedHistory)
            messages.Add(new OllamaChatMessage { Role = turn.Role, Content = turn.TextContent });

        messages.Add(new OllamaChatMessage { Role = "user", Content = prompt, Images = images });

        var payload = new OllamaChatRequest
        {
            Model = settings.LocalModelName,
            Messages = messages,
            Stream = false,
            Options = new Dictionary<string, double> { ["temperature"] = 0.0 }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("http://localhost:11434/api/chat", content, ct);

        if (!response.IsSuccessStatusCode)
            throw FriendlyError((int)response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<OllamaChatResponse>(responseBody);

        return new AIResponse(
            result?.Message?.Content.Trim() ?? "",
            result?.GetTokenUsage());
    }

    private async Task<AIResponse> ExecuteApiRequestAsync(
        string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings, CancellationToken ct)
    {
        var trimmedHistory = TruncateHistory(history, 100_000);
        var endpoint = settings.GetEffectiveEndpoint();

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            throw new UriFormatException("URL de endpoint invalida.");

        var isAnthropic = settings.SelectedProvider == CloudProvider.Anthropic;

        if (isAnthropic)
        {
            return await ExecuteAnthropicRequestAsync(uri, prompt, images, trimmedHistory, systemPrompt, settings, ct);
        }
        else
        {
            return await ExecuteOpenAIRequestAsync(uri, prompt, images, trimmedHistory, systemPrompt, settings, ct);
        }
    }

    private async Task<AIResponse> ExecuteOpenAIRequestAsync(
        Uri uri, string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings, CancellationToken ct)
    {
        var apiMessages = new List<APIMessage>();
        var trimmedSystem = systemPrompt.Trim();
        if (!string.IsNullOrEmpty(trimmedSystem))
            apiMessages.Add(APIMessage.FromText("system", trimmedSystem));

        foreach (var turn in history)
            apiMessages.Add(APIMessage.FromText(turn.Role, turn.TextContent));

        var contentParts = new List<MessageContent> { MessageContent.TextContent(prompt) };
        if (images != null)
            foreach (var img in images)
                contentParts.Add(MessageContent.ImageContent(img));
        apiMessages.Add(APIMessage.FromArray("user", contentParts));

        var modelName = settings.ApiModelName;
        var isOModel = modelName.StartsWith("o1") || modelName.StartsWith("o3");

        var payload = new APIRequest
        {
            Model = modelName,
            Messages = apiMessages,
            Temperature = isOModel ? null : 0.0,
            MaxTokens = 4096,
            Stream = false
        };

        var request = CreateRequest(HttpMethod.Post, uri, settings);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw FriendlyError((int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<APIResponse>(body);
        var text = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? "";

        return new AIResponse(text, result?.GetTokenUsage());
    }

    private async Task<AIResponse> ExecuteAnthropicRequestAsync(
        Uri uri, string prompt, List<string>? images, List<ConversationTurn> history,
        string systemPrompt, SettingsStore settings, CancellationToken ct)
    {
        var anthropicMessages = new List<AnthropicMessage>();
        foreach (var turn in history)
        {
            anthropicMessages.Add(new AnthropicMessage
            {
                Role = turn.Role,
                Content = new List<AnthropicContent> { AnthropicContent.TextContent(turn.TextContent) }
            });
        }

        var currentContent = new List<AnthropicContent> { AnthropicContent.TextContent(prompt) };
        if (images != null)
            foreach (var img in images)
                currentContent.Add(AnthropicContent.ImageContent(img));
        anthropicMessages.Add(new AnthropicMessage { Role = "user", Content = currentContent });

        var trimmedSystem = systemPrompt.Trim();
        var payload = new AnthropicRequest
        {
            Model = settings.ApiModelName,
            MaxTokens = 4096,
            System = string.IsNullOrEmpty(trimmedSystem) ? null : trimmedSystem,
            Messages = anthropicMessages,
            Temperature = 0.0
        };

        var request = CreateRequest(HttpMethod.Post, uri, settings);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw FriendlyError((int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<AnthropicResponse>(body);
        var text = result?.Content?.FirstOrDefault(c => c.Type == "text")?.Text.Trim() ?? "";

        return new AIResponse(text, result?.GetTokenUsage());
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Helpers
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, SettingsStore settings)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var apiKey = settings.ApiKey;
        if (string.IsNullOrEmpty(apiKey)) return request;

        switch (settings.SelectedProvider)
        {
            case CloudProvider.Anthropic:
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                break;
            default:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                break;
        }

        return request;
    }
}
