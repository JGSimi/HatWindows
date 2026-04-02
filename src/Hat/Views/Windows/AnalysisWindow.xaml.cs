using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Hat.Theme;

namespace Hat.Views.Windows;

public partial class AnalysisWindow : Window
{
    public AnalysisWindow()
    {
        InitializeComponent();
        DataContext = App.AssistantVM;
        LoadScreenshot();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcrylicHelper.EnableAcrylic(this);
    }

    private void LoadScreenshot()
    {
        var imageData = App.AssistantVM.AnalysisImageData;
        if (imageData != null)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(imageData);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            ScreenshotPreview.Source = bitmap;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void FollowUp_Click(object sender, RoutedEventArgs e)
    {
        var text = FollowUpBox.Text.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            App.AssistantVM.ContinueWithAnalysis(text);
            FollowUpBox.Clear();
        }
    }

    private void TransferToChat_Click(object sender, RoutedEventArgs e)
    {
        App.AssistantVM.ContinueWithAnalysis();
        ((App)Application.Current).OpenMainWindow();
        Close();
    }
}
