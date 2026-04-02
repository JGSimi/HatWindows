using System.Windows;
using System.Windows.Input;
using Hat.Theme;

namespace Hat.Views.Windows;

public partial class TrayPopoverWindow : Window
{
    public TrayPopoverWindow()
    {
        InitializeComponent();
        DataContext = App.AssistantVM;
        PositionNearTray();
        UpdateGreeting();
        UpdateModelTag();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcrylicHelper.EnableAcrylic(this);

        // Apply stealth mode if enabled
        if (App.Settings.PopoverStealthMode)
        {
            Opacity = 0.02;
            MouseEnter += (_, _) => Opacity = App.Settings.PopoverStealthHoverOpacity;
            MouseLeave += (_, _) => Opacity = 0.02;
        }

        InputBox.Focus();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Popover stays open (pinned behavior) — do NOT hide
        // This matches macOS behavior where the popover doesn't close on focus loss
    }

    private void PositionNearTray()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        var taskbarHeight = screenHeight - SystemParameters.WorkArea.Height;

        Width = App.Settings.PopoverWidth;
        Height = App.Settings.PopoverHeight;

        // Position at bottom-right, above taskbar (Windows tray is at bottom)
        Left = screenWidth - Width - 8;
        Top = screenHeight - taskbarHeight - Height - 8;
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        GreetingText.Text = hour switch
        {
            < 12 => "Bom dia",
            < 18 => "Boa tarde",
            _ => "Boa noite"
        };
    }

    private void UpdateModelTag()
    {
        var settings = App.Settings;
        ModelTag.Text = settings.InferenceMode == Models.InferenceMode.Local
            ? $"Local: {settings.LocalModelName}"
            : $"{settings.SelectedProvider.ShortName()}: {settings.ApiModelName}";
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).OpenMainWindow();
        Hide();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        await App.AssistantVM.SendManualMessageAsync();
        ScrollToBottom();
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            await App.AssistantVM.SendManualMessageAsync();
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }
}
