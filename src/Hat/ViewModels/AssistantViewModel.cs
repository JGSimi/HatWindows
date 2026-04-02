using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hat.Models;
using Hat.Models.NetworkModels;
using Hat.Services;

namespace Hat.ViewModels;

/// <summary>
/// Main chat ViewModel — handles messaging, streaming, clipboard, screen analysis.
/// Port of AssistantViewModel from ContentView.swift.
/// </summary>
public partial class AssistantViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _inputText = "";

    [ObservableProperty]
    private ObservableCollection<ChatAttachment> _pendingAttachments = new();

    [ObservableProperty]
    private bool _isAnalyzingScreen;

    [ObservableProperty]
    private string _analysisResult = "";

    [ObservableProperty]
    private byte[]? _analysisImageData;

    [ObservableProperty]
    private int _conversationInputTokens;

    [ObservableProperty]
    private int _conversationOutputTokens;

    public int ConversationTotalTokens => ConversationInputTokens + ConversationOutputTokens;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private string _streamingText = "";

    private CancellationTokenSource? _streamingCts;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Send Manual Message
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [RelayCommand]
    public async Task SendManualMessageAsync()
    {
        var text = InputText.Trim();
        var attachments = PendingAttachments.ToList();

        if (IsProcessing || (string.IsNullOrEmpty(text) && attachments.Count == 0))
            return;

        InputText = "";
        PendingAttachments.Clear();

        var finalPrompt = text;
        var extractedImages = new List<string>();

        foreach (var attachment in attachments)
        {
            if (attachment.Type == AttachmentType.Image && attachment.Base64Content != null)
            {
                extractedImages.Add(attachment.Base64Content);
            }
            else if (attachment.TextContent != null)
            {
                finalPrompt += $"\n\n[Arquivo: {attachment.FileName}]\n{attachment.TextContent}";
            }
        }

        await ExecuteRequestAsync(
            finalPrompt,
            extractedImages.Count > 0 ? extractedImages : null,
            attachments.Count > 0 ? attachments : null);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Process Clipboard (Ctrl+Shift+X)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public async void ProcessClipboard()
    {
        if (IsProcessing) return;

        var clipText = ClipboardService.GetText() ?? "";
        var clipImage = ClipboardService.ContainsImage() ? ClipboardService.GetImageAsBase64() : null;

        // If we have an image, clear text that looks like a URL/data URI
        if (clipImage != null)
        {
            var lower = clipText.Trim().ToLowerInvariant();
            if (lower.StartsWith("data:image/") || lower.StartsWith("http://") ||
                lower.StartsWith("https://") || lower.StartsWith("file://"))
            {
                clipText = "";
            }
        }

        if (string.IsNullOrEmpty(clipText) && clipImage == null)
            return;

        var images = clipImage != null ? new List<string> { clipImage } : null;
        await ExecuteRequestAsync(clipText, images, null);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Process Screen (Ctrl+Shift+Z)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public async void ProcessScreen()
    {
        if (IsAnalyzingScreen) return;

        var screenshotBase64 = ScreenCaptureService.CaptureScreenAsBase64();
        if (screenshotBase64 == null) return;

        AnalysisImageData = ScreenCaptureService.CaptureScreen();
        AnalysisResult = "";
        IsAnalyzingScreen = true;

        // Open analysis window
        ((App)System.Windows.Application.Current).OpenAnalysisWindow();

        var defaultPrompt = "Analise o que esta na minha tela e me ajude de forma proativa. Nao me pergunte o que fazer, apenas forneca a analise ou ajuda diretamente com base no contexto. Por favor, use formatacao Markdown em sua resposta.";

        try
        {
            var settings = App.Settings;
            var response = await AIApiService.Shared.ExecuteRequestAsync(
                defaultPrompt,
                new List<string> { screenshotBase64 },
                new List<ConversationTurn>(),
                settings.SystemPrompt,
                settings);

            AnalysisResult = response.Text;

            if (response.TokenUsage != null)
                settings.AddGlobalTokens(response.TokenUsage.InputTokens, response.TokenUsage.OutputTokens);

            if (settings.PlayNotifications)
                NotificationService.NotifyResponseComplete("Analise de tela concluida!", true);
        }
        catch (Exception ex)
        {
            AnalysisResult = $"Erro: {ex.Message}";
        }
        finally
        {
            IsAnalyzingScreen = false;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Core Streaming Request
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async Task ExecuteRequestAsync(string prompt, List<string>? images, List<ChatAttachment>? attachments)
    {
        IsProcessing = true;
        var settings = App.Settings;
        var conversationMgr = App.ConversationMgr;

        var history = Messages.Select(m => new ConversationTurn(
            m.IsUser ? "user" : "assistant", m.Content)).ToList();

        // Add user message
        var userMsg = new ChatMessage(prompt, true);
        Messages.Add(userMsg);
        conversationMgr.AddMessage(prompt, true);

        // Add streaming placeholder
        var placeholder = ChatMessage.StreamingPlaceholder();
        Messages.Add(placeholder);
        var placeholderIndex = Messages.Count - 1;

        IsStreaming = true;
        StreamingText = "";
        _streamingCts = new CancellationTokenSource();

        // Update tray icon to processing
        ((App)System.Windows.Application.Current).UpdateTrayIcon(true);

        try
        {
            var accumulatedText = "";
            TokenUsage? finalTokenUsage = null;

            await foreach (var chunk in AIApiService.Shared.ExecuteStreamingRequestAsync(
                prompt, images, history, settings.SystemPrompt, settings, _streamingCts.Token))
            {
                _streamingCts.Token.ThrowIfCancellationRequested();

                accumulatedText += chunk.Text;
                StreamingText = accumulatedText;

                // Update placeholder in-place
                if (placeholderIndex < Messages.Count)
                {
                    Messages[placeholderIndex].Content = accumulatedText;
                    Messages[placeholderIndex].IsStreaming = true;
                }

                if (chunk.TokenUsage != null)
                    finalTokenUsage = chunk.TokenUsage;

                if (chunk.IsFinished)
                    break;
            }

            // Finalize message
            if (placeholderIndex < Messages.Count)
            {
                Messages[placeholderIndex].Content = accumulatedText;
                Messages[placeholderIndex].IsStreaming = false;
            }

            // Persist assistant response
            conversationMgr.AddMessage(accumulatedText, false);

            // Update token counts
            if (finalTokenUsage != null)
            {
                ConversationInputTokens += finalTokenUsage.InputTokens;
                ConversationOutputTokens += finalTokenUsage.OutputTokens;
                OnPropertyChanged(nameof(ConversationTotalTokens));
                settings.AddGlobalTokens(finalTokenUsage.InputTokens, finalTokenUsage.OutputTokens);
            }

            // Copy response to clipboard
            if (!string.IsNullOrEmpty(accumulatedText))
                ClipboardService.SetText(accumulatedText);

            // Notification
            if (settings.PlayNotifications)
                NotificationService.NotifyResponseComplete(accumulatedText, true);
        }
        catch (OperationCanceledException)
        {
            // Keep partial text
            if (placeholderIndex < Messages.Count)
                Messages[placeholderIndex].IsStreaming = false;
        }
        catch (Exception ex)
        {
            // Append error to response
            if (placeholderIndex < Messages.Count)
            {
                Messages[placeholderIndex].Content += $"\n\n⚠ Resposta interrompida: {ex.Message}";
                Messages[placeholderIndex].IsStreaming = false;
            }
        }
        finally
        {
            IsProcessing = false;
            IsStreaming = false;
            ((App)System.Windows.Application.Current).UpdateTrayIcon(false);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Cancel / Clear
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [RelayCommand]
    public void CancelStreaming()
    {
        _streamingCts?.Cancel();
    }

    [RelayCommand]
    public void ClearMessages()
    {
        Messages.Clear();
        ConversationInputTokens = 0;
        ConversationOutputTokens = 0;
        OnPropertyChanged(nameof(ConversationTotalTokens));
        App.ConversationMgr.ClearActiveConversation();
    }

    /// <summary>
    /// Transfer screen analysis results to the main chat.
    /// </summary>
    public void ContinueWithAnalysis(string? followUp = null)
    {
        if (string.IsNullOrEmpty(AnalysisResult)) return;

        var userMsg = new ChatMessage("Analise de Tela", true, MessageSource.ScreenAnalysis);
        var assistantMsg = new ChatMessage(AnalysisResult, false, MessageSource.ScreenAnalysis);

        Messages.Add(userMsg);
        Messages.Add(assistantMsg);

        App.ConversationMgr.AddMessage("Analise de Tela", true, MessageSource.ScreenAnalysis);
        App.ConversationMgr.AddMessage(AnalysisResult, false, MessageSource.ScreenAnalysis);

        if (!string.IsNullOrWhiteSpace(followUp))
        {
            _ = Task.Run(async () =>
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ExecuteRequestAsync(followUp, null, null);
                });
            });
        }

        AnalysisResult = "";
        AnalysisImageData = null;
    }
}
