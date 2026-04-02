using CommunityToolkit.Mvvm.ComponentModel;

namespace Hat.Models;

/// <summary>
/// A single chat message. Mutable (ObservableObject) to support
/// in-place streaming updates in the UI.
/// Port of ChatMessage struct from ContentView.swift.
/// </summary>
public partial class ChatMessage : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private string _content = "";

    [ObservableProperty]
    private bool _isUser;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    [ObservableProperty]
    private MessageSource _source = MessageSource.Chat;

    [ObservableProperty]
    private List<ChatAttachment>? _attachments;

    /// <summary>
    /// Base64-encoded image data for images sent with the message.
    /// </summary>
    [ObservableProperty]
    private List<string>? _imageBase64List;

    public ChatMessage() { }

    public ChatMessage(string content, bool isUser, MessageSource source = MessageSource.Chat)
    {
        _content = content;
        _isUser = isUser;
        _source = source;
        _timestamp = DateTime.Now;
    }

    /// <summary>
    /// Creates a streaming placeholder message (assistant response in progress).
    /// </summary>
    public static ChatMessage StreamingPlaceholder()
    {
        return new ChatMessage
        {
            Content = "",
            IsUser = false,
            IsStreaming = true,
            Source = MessageSource.Chat
        };
    }
}

public enum MessageSource
{
    Chat,
    ScreenAnalysis,
    Clipboard
}
