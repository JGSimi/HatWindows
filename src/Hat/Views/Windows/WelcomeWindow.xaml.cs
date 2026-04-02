using System.Windows;
using Hat.Theme;

namespace Hat.Views.Windows;

public partial class WelcomeWindow : Window
{
    public WelcomeWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcrylicHelper.EnableAcrylic(this);
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.HasCompletedOnboarding = true;
        Close();
    }
}
