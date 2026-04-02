using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hat.Models;

namespace Hat.Views.Controls;

public partial class SidebarView : UserControl
{
    private System.Timers.Timer? _searchDebounce;

    public SidebarView()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshList();
    }

    private void RefreshList(string? searchQuery = null)
    {
        ConversationList.Items.Clear();
        var manager = App.ConversationMgr;

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var results = manager.Search(searchQuery);
            foreach (var conv in results)
                ConversationList.Items.Add(CreateConversationRow(conv));
            return;
        }

        var groups = manager.GetGroupedConversations();
        foreach (var (group, items) in groups)
        {
            // Section header
            var header = new TextBlock
            {
                Text = group.DisplayName().ToUpper(),
                FontSize = 10, FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Margin = new Thickness(8, 10, 0, 4)
            };
            ConversationList.Items.Add(header);

            foreach (var conv in items)
                ConversationList.Items.Add(CreateConversationRow(conv));
        }
    }

    private Border CreateConversationRow(Conversation conv)
    {
        var isActive = conv.Id == App.ConversationMgr.ActiveConversationId;

        var row = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(4, 1, 4, 1),
            Background = isActive
                ? (Brush)FindResource("AccentSubtleBrush")
                : Brushes.Transparent,
            Cursor = Cursors.Hand,
            Tag = conv.Id
        };

        if (isActive)
        {
            row.BorderBrush = new SolidColorBrush(
                ThemeColors.ColorFromAlpha(
                    ((SolidColorBrush)FindResource("AccentPrimaryBrush")).Color, 0.25));
            row.BorderThickness = new Thickness(0.5);
        }

        var stack = new StackPanel();

        // Title
        var title = new TextBlock
        {
            Text = (conv.IsPinned ? "📌 " : "") + conv.Title,
            FontSize = 12,
            FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        stack.Children.Add(title);

        // Preview + time
        var detailPanel = new Grid { Margin = new Thickness(0, 2, 0, 0) };
        detailPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        detailPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var preview = new TextBlock
        {
            Text = conv.Preview,
            FontSize = 10.5,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(preview, 0);
        detailPanel.Children.Add(preview);

        var time = new TextBlock
        {
            Text = FormatRelativeTime(conv.UpdatedAt),
            FontSize = 9,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(time, 1);
        detailPanel.Children.Add(time);

        stack.Children.Add(detailPanel);
        row.Child = stack;

        // Click to select
        row.MouseLeftButtonUp += (_, _) =>
        {
            App.ConversationMgr.SelectConversation(conv.Id);
            RefreshList();
        };

        // Right-click context menu
        row.ContextMenu = CreateContextMenu(conv);

        return row;
    }

    private ContextMenu CreateContextMenu(Conversation conv)
    {
        var menu = new ContextMenu();

        var pin = new MenuItem { Header = conv.IsPinned ? "Desafixar" : "Fixar" };
        pin.Click += (_, _) => { App.ConversationMgr.TogglePin(conv.Id); RefreshList(); };
        menu.Items.Add(pin);

        var delete = new MenuItem { Header = "Excluir" };
        delete.Click += (_, _) => { App.ConversationMgr.DeleteConversation(conv.Id); RefreshList(); };
        menu.Items.Add(delete);

        return menu;
    }

    private static string FormatRelativeTime(DateTime date)
    {
        var diff = DateTime.Now - date;
        if (diff.TotalMinutes < 1) return "agora";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h";
        return date.ToString("dd/MM");
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        App.ConversationMgr.CreateConversation();
        App.AssistantVM.ClearMessages();
        RefreshList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce?.Stop();
        _searchDebounce = new System.Timers.Timer(300) { AutoReset = false };
        _searchDebounce.Elapsed += (_, _) =>
        {
            Dispatcher.Invoke(() => RefreshList(SearchBox.Text));
        };
        _searchDebounce.Start();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).OpenSettingsWindow();
    }
}

// Helper used inline
file static class ThemeColors
{
    public static System.Windows.Media.Color ColorFromAlpha(System.Windows.Media.Color baseColor, double opacity) =>
        System.Windows.Media.Color.FromArgb((byte)(opacity * 255), baseColor.R, baseColor.G, baseColor.B);
}
