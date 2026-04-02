using System.Text.Json.Serialization;

namespace Hat.Models.NetworkModels;

// MARK: - Ollama local API models

public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<OllamaChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("options")]
    public Dictionary<string, double> Options { get; set; } = new();
}

public class OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("images")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Images { get; set; }
}

public class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaChatResponseMessage? Message { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; set; }

    public TokenUsage? GetTokenUsage()
    {
        if (PromptEvalCount == null || EvalCount == null) return null;
        return new TokenUsage(PromptEvalCount.Value, EvalCount.Value);
    }
}

public class OllamaChatResponseMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}
