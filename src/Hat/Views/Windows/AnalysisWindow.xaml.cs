using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Hat.Theme;

namespace Hat.Views.Windows;

/// <summary>
/// Complete analysis window with follow-up, transfer, loading states.
/// Port of AnalysisWindow.swift.
/// </summary>
public partial class AnalysisWindow : Window
{
    public AnalysisWindow()
    {
        InitializeComponent();
        DataContext = App.AssistantVM;
        LoadScreenshot();

        // Listen for analysis state changes
        App.AssistantVM.PropertyChanged += OnViewModelChanged;
        UpdateState();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcrylicHelper.EnableAcrylic(this);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(App.AssistantVM.IsAnalyzingScreen) or
            nameof(App.AssistantVM.AnalysisResult) or
            nameof(App.AssistantVM.AnalysisImageData))
        {
            Dispatcher.Invoke(UpdateState);
        }
    }

    private void UpdateState()
    {
        var vm = App.AssistantVM;

        ProcessingBadge.Visibility = vm.IsAnalyzingScreen ? Visibility.Visible : Visibility.Collapsed;

        if (vm.IsAnalyzingScreen)
        {
            // Loading
            LoadingState.Visibility = Visibility.Visible;
            AnalysisEmptyState.Visibility = Visibility.Collapsed;
            ResultScroll.Visibility = Visibility.Collapsed;
            ActionButtons.Visibility = Visibility.Collapsed;
            FollowUpArea.Visibility = Visibility.Collapsed;
        }
        else if (!string.IsNullOrEmpty(vm.AnalysisResult))
        {
            // Has result
            LoadingState.Visibility = Visibility.Collapsed;
            AnalysisEmptyState.Visibility = Visibility.Collapsed;
            ResultScroll.Visibility = Visibility.Visible;
            AnalysisMarkdown.MarkdownText = vm.AnalysisResult;
            ActionButtons.Visibility = Visibility.Visible;
            FollowUpArea.Visibility = Visibility.Visible;
            FollowUpBox.Focus();
        }
        else
        {
            // Empty
            LoadingState.Visibility = Visibility.Collapsed;
            AnalysisEmptyState.Visibility = Visibility.Visible;
            ResultScroll.Visibility = Visibility.Collapsed;
            ActionButtons.Visibility = Visibility.Collapsed;
            FollowUpArea.Visibility = Visibility.Collapsed;
        }

        // Screenshot
        if (vm.AnalysisImageData != null)
        {
            LoadScreenshot();
            ScreenshotEmpty.Visibility = Visibility.Collapsed;
        }
        else
        {
            ScreenshotPreview.Source = null;
            ScreenshotEmpty.Visibility = Visibility.Visible;
        }
    }

    private void LoadScreenshot()
    {
        var imageData = App.AssistantVM.AnalysisImageData;
        if (imageData == null) return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(imageData);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            ScreenshotPreview.Source = bitmap;
            ScreenshotEmpty.Visibility = Visibility.Collapsed;
        }
        catch { }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void FollowUp_Click(object sender, RoutedEventArgs e) => SendFollowUp();

    private void FollowUp_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
        {
            e.Handled = true;
            SendFollowUp();
        }
    }

    private void SendFollowUp()
    {
        var text = FollowUpBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        App.AssistantVM.ContinueWithAnalysis(text);
        FollowUpBox.Clear();
    }

    private void NewAnalysis_Click(object sender, RoutedEventArgs e)
    {
        App.AssistantVM.ProcessScreen();
    }

    private void TransferToChat_Click(object sender, RoutedEventArgs e)
    {
        App.AssistantVM.ContinueWithAnalysis();
        ((App)Application.Current).OpenMainWindow();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        App.AssistantVM.PropertyChanged -= OnViewModelChanged;
        base.OnClosed(e);
    }
}
