using System.Windows;
using System.Windows.Input;
using Hat.Theme;

namespace Hat.Views.Windows;

public partial class QuickInputWindow : Window
{
    public QuickInputWindow()
    {
        InitializeComponent();
        DataContext = App.AssistantVM;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcrylicHelper.EnableAcrylic(this);

        // Position: centered horizontally, 15% above center vertically
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        Top = screenHeight * 0.35 - ActualHeight / 2;

        QuickInputBox.Focus();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Quick input closes on focus loss (unlike popover)
        Hide();
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(App.AssistantVM.InputText)) return;
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
            if (!string.IsNullOrWhiteSpace(App.AssistantVM.InputText))
            {
                await App.AssistantVM.SendManualMessageAsync();
                Hide();
            }
        }
    }
}
