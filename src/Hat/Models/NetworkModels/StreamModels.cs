namespace Hat.Models.NetworkModels;

/// <summary>
/// A chunk of streaming response data.
/// </summary>
public record StreamChunk(string Text, TokenUsage? TokenUsage, bool IsFinished);
