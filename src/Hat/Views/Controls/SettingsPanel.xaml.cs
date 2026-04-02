using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hat.Models;

namespace Hat.Views.Controls;

/// <summary>
/// Inline settings panel for the popover.
/// Port of SettingsView.swift (the compact version inside MenuBarPopoverView).
/// </summary>
public partial class SettingsPanel : UserControl
{
    public SettingsPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    public void Refresh()
    {
        var settings = App.Settings;

        // Inference mode buttons
        var isLocal = settings.InferenceMode == InferenceMode.Local;
        BtnLocal.Background = isLocal
            ? (Brush)FindResource("AccentPrimaryBrush")
            : (Brush)FindResource("SurfaceTertiaryBrush");
        BtnLocal.Foreground = isLocal ? Brushes.White : (Brush)FindResource("TextSecondaryBrush");
        BtnApi.Background = !isLocal
            ? (Brush)FindResource("AccentPrimaryBrush")
            : (Brush)FindResource("SurfaceTertiaryBrush");
        BtnApi.Foreground = !isLocal ? Brushes.White : (Brush)FindResource("TextSecondaryBrush");

        // Provider chips
        ProviderChips.Children.Clear();
        foreach (var provider in Enum.GetValues<CloudProvider>())
        {
            var isSelected = provider == settings.SelectedProvider;
            var chip = new Button
            {
                Content = provider.ShortName(),
                FontSize = 11,
                FontWeight = isSelected ? FontWeights.Medium : FontWeights.Normal,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 4, 0),
                Background = isSelected
                    ? (Brush)FindResource("AccentPrimaryBrush")
                    : (Brush)FindResource("GlassSurfaceSecondaryBrush"),
                Foreground = isSelected ? Brushes.White : (Brush)FindResource("TextPrimaryBrush"),
                BorderThickness = new Thickness(isSelected ? 0 : 0.5),
                BorderBrush = (Brush)FindResource("GlassBorderSubtleBrush"),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = provider
            };
            chip.Template = CreateChipTemplate();
            chip.Click += (s, _) =>
            {
                settings.SelectedProvider = (CloudProvider)((Button)s!).Tag;
                Refresh();
            };
            ProviderChips.Children.Add(chip);
        }

        // Model display
        ModelDisplay.Text = isLocal
            ? settings.LocalModelName
            : settings.ApiModelName;

        // Token counter
        var vm = App.AssistantVM;
        TokenIn.Text = $"IN: {FormatTokens(vm.ConversationInputTokens)}";
        TokenOut.Text = $"OUT: {FormatTokens(vm.ConversationOutputTokens)}";
        TokenTotal.Text = $"Total: {FormatTokens(vm.ConversationTotalTokens)}";

        // Token progress (relative to conversation)
        var total = vm.ConversationTotalTokens;
        var maxEstimate = 8192.0; // rough max for visual
        var ratio = Math.Min(1.0, total / maxEstimate);
        TokenProgressFill.Width = ratio * (TokenProgressFill.Parent as FrameworkElement)?.ActualWidth ?? 0;
    }

    private static string FormatTokens(int count)
    {
        if (count >= 1_000_000) return $"{count / 1_000_000.0:F1}M";
        if (count >= 1_000) return $"{count / 1_000.0:F1}K";
        return count.ToString();
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
        border.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;
        return template;
    }

    private void LocalMode_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.InferenceMode = InferenceMode.Local;
        Refresh();
    }

    private void ApiMode_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.InferenceMode = InferenceMode.Api;
        Refresh();
    }

    private void Advanced_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).OpenSettingsWindow();
    }
}
