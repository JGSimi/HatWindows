namespace Hat.Models;

/// <summary>
/// A file attachment in a chat message.
/// Port of ChatAttachment from ContentView.swift.
/// </summary>
public class ChatAttachment
{
    public Guid Id { get; } = Guid.NewGuid();
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public AttachmentType Type { get; set; }
    public byte[]? Data { get; set; }
    public string? Base64Content { get; set; }
    public string? TextContent { get; set; }

    /// <summary>
    /// For image attachments, a thumbnail preview (BitmapImage source).
    /// </summary>
    public object? ThumbnailSource { get; set; }
}

public enum AttachmentType
{
    Image,
    Pdf,
    Text,
    Other
}
