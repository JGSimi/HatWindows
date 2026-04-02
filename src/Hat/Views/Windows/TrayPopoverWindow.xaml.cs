using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Hat.Helpers;
using Hat.Models;
using Hat.Theme;

namespace Hat.Views.Windows;

/// <summary>
/// Complete tray popover — matches MenuBarPopoverView.swift.
/// Includes stealth mode, inline settings, screenshot, scroll-to-bottom, attachments.
/// </summary>
public partial class TrayPopoverWindow : Window
{
    private bool _settingsVisible;
    private bool _isStealthMode;

    public TrayPopoverWindow()
    {
        InitializeComponent();
        DataContext = App.AssistantVM;
        PositionNearTray();
        UpdateGreeting();
        UpdateModelTag();
        UpdateEmptyState();

        // Listen for message changes to update empty state
        App.AssistantVM.Messages.CollectionChanged += (_, _) =>
        {
            Dispatcher.Invoke(UpdateEmptyState);
            Dispatcher.Invoke(ScrollToBottom);
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcrylicHelper.EnableAcrylic(this);

        // Stealth mode
        _isStealthMode = App.Settings.PopoverStealthMode;
        if (_isStealthMode)
        {
            Opacity = 0.02;
            // Grayscale effect
            MainBorder.Effect = new BlurEffect { Radius = 0 };
        }

        Width = App.Settings.PopoverWidth;
        Height = App.Settings.PopoverHeight;

        InputBox.Focus();
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (_isStealthMode)
        {
            Opacity = App.Settings.PopoverStealthHoverOpacity;
            MainBorder.Effect = new DropShadowEffect { BlurRadius = 20, Opacity = 0.2, ShadowDepth = 8 };
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_isStealthMode)
        {
            Opacity = 0.02;
        }
    }

    private void PositionNearTray()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        var taskbarHeight = screenHeight - SystemParameters.WorkArea.Height;

        Left = screenWidth - Width - 8;
        Top = screenHeight - taskbarHeight - Height - 8;
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        GreetingText.Text = hour switch { < 12 => "Bom dia", < 18 => "Boa tarde", _ => "Boa noite" };
    }

    private void UpdateModelTag()
    {
        var s = App.Settings;
        ModelTag.Text = s.InferenceMode == InferenceMode.Local
            ? $"Local: {s.LocalModelName}"
            : $"{s.SelectedProvider.ShortName()}: {s.ApiModelName}";
    }

    private void UpdateEmptyState()
    {
        EmptyState.Visibility = App.AssistantVM.Messages.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // Header actions
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }

    private void ToggleSettings_Click(object sender, RoutedEventArgs e)
    {
        _settingsVisible = !_settingsVisible;
        SettingsPanelBorder.Visibility = _settingsVisible ? Visibility.Visible : Visibility.Collapsed;
        if (_settingsVisible) InlineSettings.Refresh();
    }

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).OpenMainWindow();
        Hide();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    // Chat
    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (App.AssistantVM.IsProcessing)
        {
            App.AssistantVM.CancelStreaming();
            return;
        }
        await App.AssistantVM.SendManualMessageAsync();
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            await App.AssistantVM.SendManualMessageAsync();
        }
    }

    private void Screenshot_Click(object sender, RoutedEventArgs e)
    {
        Hide(); // Hide popover during capture
        System.Threading.Thread.Sleep(200);
        App.AssistantVM.ProcessScreen();
    }

    // Scroll
    private void ChatScroll_Changed(object sender, ScrollChangedEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        var atBottom = sv.VerticalOffset >= sv.ScrollableHeight - 20;
        ScrollToBottomBtn.Visibility = atBottom ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ScrollToBottom_Click(object sender, RoutedEventArgs e) => ScrollToBottom();

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }

    // Drag-drop attachments
    private void Input_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Input_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;

        foreach (var file in files)
        {
            var attachment = await FileAttachmentHelper.ProcessFileAsync(file);
            if (attachment != null)
            {
                App.AssistantVM.PendingAttachments.Add(attachment);
                UpdateAttachmentBar();
            }
        }
    }

    private void UpdateAttachmentBar()
    {
        AttachmentPreviews.Children.Clear();
        var attachments = App.AssistantVM.PendingAttachments;

        if (attachments.Count == 0)
        {
            AttachmentBar.Visibility = Visibility.Collapsed;
            return;
        }

        AttachmentBar.Visibility = Visibility.Visible;
        foreach (var att in attachments)
        {
            var chip = new Border
            {
                Background = (Brush)FindResource("GlassSurfaceSecondaryBrush"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 4, 0),
                Height = 44,
                Tag = att
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text = att.Type == AttachmentType.Image ? "🖼" : "📄",
                FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0)
            });
            stack.Children.Add(new TextBlock
            {
                Text = att.FileName.Length > 15 ? att.FileName[..12] + "..." : att.FileName,
                FontSize = 10, Foreground = (Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });

            // Remove button
            var removeBtn = new Button
            {
                Content = "✕", FontSize = 9, Width = 16, Height = 16,
                Margin = new Thickness(4, 0, 0, 0),
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Tag = att, Cursor = Cursors.Hand
            };
            removeBtn.Click += (s, _) =>
            {
                var a = (Models.ChatAttachment)((Button)s!).Tag;
                App.AssistantVM.PendingAttachments.Remove(a);
                UpdateAttachmentBar();
            };
            stack.Children.Add(removeBtn);

            chip.Child = stack;
            AttachmentPreviews.Children.Add(chip);
        }
    }
}
