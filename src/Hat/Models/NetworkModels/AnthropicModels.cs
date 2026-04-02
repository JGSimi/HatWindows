using System.Text.Json.Serialization;

namespace Hat.Models.NetworkModels;

// MARK: - Anthropic-specific API models

public class AnthropicRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 4096;

    [JsonPropertyName("system")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? System { get; set; }

    [JsonPropertyName("messages")]
    public List<AnthropicMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

public class AnthropicStreamRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 4096;

    [JsonPropertyName("system")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? System { get; set; }

    [JsonPropertyName("messages")]
    public List<AnthropicMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = true;
}

public class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public List<AnthropicContent> Content { get; set; } = new();
}

public class AnthropicContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AnthropicImageSource? Source { get; set; }

    public static AnthropicContent TextContent(string text) =>
        new() { Type = "text", Text = text };

    public static AnthropicContent ImageContent(string base64, string mediaType = "image/jpeg") =>
        new()
        {
            Type = "image",
            Source = new AnthropicImageSource
            {
                Type = "base64",
                MediaType = mediaType,
                Data = base64
            }
        };
}

public class AnthropicImageSource
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "base64";

    [JsonPropertyName("media_type")]
    public string MediaType { get; set; } = "image/jpeg";

    [JsonPropertyName("data")]
    public string Data { get; set; } = "";
}

// Response

public class AnthropicResponse
{
    [JsonPropertyName("content")]
    public List<AnthropicResponseContent>? Content { get; set; }

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }

    public TokenUsage? GetTokenUsage()
    {
        if (Usage?.InputTokens == null || Usage?.OutputTokens == null) return null;
        return new TokenUsage(Usage.InputTokens.Value, Usage.OutputTokens.Value);
    }
}

public class AnthropicResponseContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public class AnthropicUsage
{
    [JsonPropertyName("input_tokens")]
    public int? InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int? OutputTokens { get; set; }
}

// Streaming

public class AnthropicStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("delta")]
    public AnthropicStreamDelta? Delta { get; set; }

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}

public class AnthropicStreamDelta
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
