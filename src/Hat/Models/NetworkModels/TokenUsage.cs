namespace Hat.Models.NetworkModels;

public record TokenUsage(int InputTokens, int OutputTokens)
{
    public int TotalTokens => InputTokens + OutputTokens;
}

public record AIResponse(string Text, TokenUsage? TokenUsage);

public record ConversationTurn(string Role, string TextContent);
