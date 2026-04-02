using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hat.Helpers;
using Hat.Models;
using Hat.Theme;
using Microsoft.Win32;

namespace Hat.Views.Windows;

/// <summary>
/// Complete quick input with file picker, recent prompts, drag-drop.
/// Port of QuickInputWindow.swift.
/// </summary>
public partial class QuickInputWindow : Window
{
    private static readonly List<string> _recentPrompts = new();
    private const int MaxRecentPrompts = 3;

    public QuickInputWindow()
    {
        InitializeComponent();
        DataContext = App.AssistantVM;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcrylicHelper.EnableAcrylic(this);

        var screenHeight = SystemParameters.PrimaryScreenHeight;
        Top = screenHeight * 0.35 - ActualHeight / 2;

        QuickInputBox.Focus();
        LoadRecentPrompts();
    }

    private void Window_Deactivated(object sender, EventArgs e) => Hide();

    // File picker
    private async void AttachFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Todos|*.*|Imagens|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|Texto|*.txt;*.md;*.json;*.xml;*.csv|PDF|*.pdf|Codigo|*.py;*.cs;*.js;*.ts;*.swift;*.java;*.cpp;*.go;*.rs;*.rb"
        };

        if (dialog.ShowDialog() != true) return;

        foreach (var file in dialog.FileNames)
        {
            var attachment = await FileAttachmentHelper.ProcessFileAsync(file);
            if (attachment != null)
            {
                App.AssistantVM.PendingAttachments.Add(attachment);
            }
        }
        UpdateAttachmentBar();
    }

    // Send
    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        var text = App.AssistantVM.InputText?.Trim();
        if (string.IsNullOrWhiteSpace(text) && App.AssistantVM.PendingAttachments.Count == 0) return;

        if (!string.IsNullOrWhiteSpace(text))
            AddRecentPrompt(text);

        await App.AssistantVM.SendManualMessageAsync();
        Hide();
    }

    private async void QuickInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Hide();
        }
        else if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
        {
            e.Handled = true;
            var text = App.AssistantVM.InputText?.Trim();
            if (!string.IsNullOrWhiteSpace(text) || App.AssistantVM.PendingAttachments.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(text))
                    AddRecentPrompt(text);
                await App.AssistantVM.SendManualMessageAsync();
                Hide();
            }
        }
    }

    // Recent prompts
    private void LoadRecentPrompts()
    {
        RecentPromptsPanel.Children.Clear();
        foreach (var prompt in _recentPrompts.TakeLast(MaxRecentPrompts))
        {
            var truncated = prompt.Length > 30 ? prompt[..27] + "..." : prompt;
            var chip = new Button
            {
                Content = truncated,
                FontSize = 10,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Background = (Brush)FindResource("GlassSurfaceSecondaryBrush"),
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                BorderBrush = (Brush)FindResource("GlassBorderSubtleBrush"),
                BorderThickness = new Thickness(0.5),
                Cursor = Cursors.Hand,
                Tag = prompt
            };
            chip.Template = CreateChipTemplate();
            chip.Click += (s, _) =>
            {
                App.AssistantVM.InputText = (string)((Button)s!).Tag;
                QuickInputBox.Focus();
                QuickInputBox.CaretIndex = QuickInputBox.Text.Length;
            };
            RecentPromptsPanel.Children.Add(chip);
        }

        RecentPromptsPanel.Visibility = _recentPrompts.Count > 0
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void AddRecentPrompt(string prompt)
    {
        _recentPrompts.Remove(prompt); // Remove if exists (to move to end)
        _recentPrompts.Add(prompt);
        while (_recentPrompts.Count > MaxRecentPrompts)
            _recentPrompts.RemoveAt(0);
    }

    // Drag-drop
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
                App.AssistantVM.PendingAttachments.Add(attachment);
        }
        UpdateAttachmentBar();
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
                Tag = att
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text = att.Type == AttachmentType.Image ? "🖼" : "📄",
                FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0)
            });
            stack.Children.Add(new TextBlock
            {
                Text = att.FileName.Length > 20 ? att.FileName[..17] + "..." : att.FileName,
                FontSize = 10, Foreground = (Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });

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
                App.AssistantVM.PendingAttachments.Remove((ChatAttachment)((Button)s!).Tag);
                UpdateAttachmentBar();
            };
            stack.Children.Add(removeBtn);
            chip.Child = stack;
            AttachmentPreviews.Children.Add(chip);
        }
    }

    private static ControlTemplate CreateChipTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
        border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding")
            { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;
        return template;
    }
}
