using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Hat.Models;
using Hat.Services;

namespace Hat.Views.Controls;

/// <summary>
/// Complete chat bubble matching ChatBubbleView.swift.
/// User: accent glass bubble. Assistant: markdown with avatar.
/// Hover reveals copy button + timestamp.
/// </summary>
public partial class ChatBubble : UserControl
{
    private ChatMessage? _message;
    private bool _isHovered;
    private DispatcherTimer? _copyResetTimer;

    public ChatBubble()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ChatMessage oldMsg)
            oldMsg.PropertyChanged -= OnMessagePropertyChanged;

        if (e.NewValue is ChatMessage msg)
        {
            _message = msg;
            msg.PropertyChanged += OnMessagePropertyChanged;
            UpdateLayout(msg);
        }
    }

    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_message == null) return;
        Dispatcher.Invoke(() => UpdateLayout(_message));
    }

    private void UpdateLayout(ChatMessage msg)
    {
        if (msg.IsUser)
        {
            UserPanel.Visibility = Visibility.Visible;
            AssistantPanel.Visibility = Visibility.Collapsed;

            UserText.Text = msg.Content;
            UserTimestamp.Text = msg.Timestamp.ToString("HH:mm");

            // Source badge
            UserSourceBadge.Visibility = msg.Source == MessageSource.ScreenAnalysis
                ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            UserPanel.Visibility = Visibility.Collapsed;
            AssistantPanel.Visibility = Visibility.Visible;

            AssistantTimestamp.Text = msg.Timestamp.ToString("HH:mm");

            if (msg.IsStreaming && string.IsNullOrEmpty(msg.Content))
            {
                // Waiting for first token
                StreamingDots.Visibility = Visibility.Visible;
                StreamingText.Visibility = Visibility.Collapsed;
                MarkdownContent.Visibility = Visibility.Collapsed;
            }
            else if (msg.IsStreaming)
            {
                // Streaming: use plain text to avoid markdown flicker
                StreamingDots.Visibility = Visibility.Collapsed;
                StreamingText.Visibility = Visibility.Visible;
                StreamingText.Text = msg.Content;
                MarkdownContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Complete: render markdown
                StreamingDots.Visibility = Visibility.Collapsed;
                StreamingText.Visibility = Visibility.Collapsed;
                MarkdownContent.Visibility = Visibility.Visible;
                MarkdownContent.MarkdownText = msg.Content;
            }
        }
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isHovered = true;
        var actions = _message?.IsUser == true ? UserHoverActions : AssistantHoverActions;
        actions.Visibility = Visibility.Visible;
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isHovered = false;
        UserHoverActions.Visibility = Visibility.Collapsed;
        AssistantHoverActions.Visibility = Visibility.Collapsed;
    }

    private void CopyMessage_Click(object sender, RoutedEventArgs e)
    {
        if (_message == null || string.IsNullOrEmpty(_message.Content)) return;

        ClipboardService.SetText(_message.Content);

        // Show "Copiado" feedback
        var iconBlock = _message.IsUser ? UserCopyIcon : AssistantCopyIcon;
        iconBlock.Text = "✓";
        iconBlock.Foreground = FindBrush("SuccessBrush");

        _copyResetTimer?.Stop();
        _copyResetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _copyResetTimer.Tick += (_, _) =>
        {
            iconBlock.Text = "\uE8C8"; // Copy icon
            iconBlock.Foreground = FindBrush("TextMutedBrush");
            _copyResetTimer?.Stop();
        };
        _copyResetTimer.Start();
    }

    private System.Windows.Media.Brush FindBrush(string key)
    {
        try { return (System.Windows.Media.Brush)FindResource(key); }
        catch { return System.Windows.Media.Brushes.White; }
    }
}
