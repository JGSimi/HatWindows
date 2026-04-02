using System.Text.Json.Serialization;

namespace Hat.Models;

/// <summary>
/// A conversation with its messages.
/// Port of Conversation from ConversationManager.swift.
/// </summary>
public class Conversation
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Nova Conversa";

    [JsonPropertyName("messages")]
    public List<SavedMessage> Messages { get; set; } = new();

    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Preview text: first ~80 chars of the last message.
    /// </summary>
    [JsonIgnore]
    public string Preview
    {
        get
        {
            var last = Messages.LastOrDefault();
            if (last == null) return "";
            return last.Content.Length > 80
                ? last.Content[..80] + "..."
                : last.Content;
        }
    }

    /// <summary>
    /// Auto-generate title from first user message.
    /// </summary>
    public void AutoTitle()
    {
        var firstUser = Messages.FirstOrDefault(m => m.IsUser);
        if (firstUser == null) return;

        var text = firstUser.Content.Trim();
        Title = text.Length > 50 ? text[..50] + "..." : text;
    }
}

/// <summary>
/// A persisted message within a conversation.
/// Port of SavedMessage from ConversationManager.swift.
/// </summary>
public class SavedMessage
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("isUser")]
    public bool IsUser { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("source")]
    public string Source { get; set; } = "chat";
}

/// <summary>
/// Conversation index entry (metadata only, for fast loading).
/// </summary>
public class ConversationIndex
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Grouped conversations for sidebar display.
/// </summary>
public enum ConversationGroup
{
    Pinned,
    Today,
    Yesterday,
    LastWeek,
    Older
}

public static class ConversationGroupExtensions
{
    public static string DisplayName(this ConversationGroup group) => group switch
    {
        ConversationGroup.Pinned => "Fixadas",
        ConversationGroup.Today => "Hoje",
        ConversationGroup.Yesterday => "Ontem",
        ConversationGroup.LastWeek => "Ultimos 7 dias",
        ConversationGroup.Older => "Mais Antigas",
        _ => ""
    };
}
