using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hat.Models.NetworkModels;

// MARK: - OpenAI-compatible API models (also used by Google, Inception, OpenRouter, Custom)

public class APIRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<APIMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

/// <summary>
/// API message with polymorphic content (string or array of content parts).
/// Uses a custom JsonConverter to handle both formats.
/// </summary>
[JsonConverter(typeof(APIMessageConverter))]
public class APIMessage
{
    public string Role { get; set; } = "";
    public string? TextContent { get; set; }
    public List<MessageContent>? ArrayContent { get; set; }

    public static APIMessage FromText(string role, string content) =>
        new() { Role = role, TextContent = content };

    public static APIMessage FromArray(string role, List<MessageContent> content) =>
        new() { Role = role, ArrayContent = content };
}

public class MessageContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImageUrl? ImageUrl { get; set; }

    public static MessageContent TextContent(string text) =>
        new() { Type = "text", Text = text };

    public static MessageContent ImageContent(string base64) =>
        new() { Type = "image_url", ImageUrl = new ImageUrl { Url = $"data:image/png;base64,{base64}" } };
}

public class ImageUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

// Response models

public class APIResponse
{
    [JsonPropertyName("choices")]
    public List<APIChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public APIUsage? Usage { get; set; }

    public TokenUsage? GetTokenUsage()
    {
        if (Usage?.PromptTokens == null || Usage?.CompletionTokens == null) return null;
        return new TokenUsage(Usage.PromptTokens.Value, Usage.CompletionTokens.Value);
    }
}

public class APIChoice
{
    [JsonPropertyName("message")]
    public APIResponseMessage? Message { get; set; }
}

public class APIResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class APIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int? PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int? CompletionTokens { get; set; }
}

// Streaming models

public class APIStreamDelta
{
    [JsonPropertyName("choices")]
    public List<StreamChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public APIUsage? Usage { get; set; }
}

public class StreamChoice
{
    [JsonPropertyName("delta")]
    public StreamDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class StreamDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

// Model listing

public class APIModelListResponse
{
    [JsonPropertyName("data")]
    public List<APIModelItem>? Data { get; set; }
}

public class APIModelItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
}

// Custom JSON converter for APIMessage polymorphic content

public class APIMessageConverter : JsonConverter<APIMessage>
{
    public override APIMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var role = root.GetProperty("role").GetString() ?? "";

        var msg = new APIMessage { Role = role };
        if (root.TryGetProperty("content", out var content))
        {
            if (content.ValueKind == JsonValueKind.String)
                msg.TextContent = content.GetString();
            else if (content.ValueKind == JsonValueKind.Array)
                msg.ArrayContent = JsonSerializer.Deserialize<List<MessageContent>>(content.GetRawText(), options);
        }
        return msg;
    }

    public override void Write(Utf8JsonWriter writer, APIMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("role", value.Role);
        if (value.TextContent != null)
        {
            writer.WriteString("content", value.TextContent);
        }
        else if (value.ArrayContent != null)
        {
            writer.WritePropertyName("content");
            JsonSerializer.Serialize(writer, value.ArrayContent, options);
        }
        writer.WriteEndObject();
    }
}
