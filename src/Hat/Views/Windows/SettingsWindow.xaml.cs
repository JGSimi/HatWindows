using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hat.Models;
using Hat.Services;
using Hat.Theme;

namespace Hat.Views.Windows;

public partial class SettingsWindow : Window
{
    private readonly SettingsStore _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = App.Settings;
        ShowGeneralTab();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AcrylicHelper.EnableAcrylic(this);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender == TabGeneral) ShowGeneralTab();
        else if (sender == TabAppearance) ShowAppearanceTab();
        else if (sender == TabModels) ShowModelsTab();
        else if (sender == TabBehavior) ShowBehaviorTab();
        else if (sender == TabShortcuts) ShowShortcutsTab();
    }

    private void ShowGeneralTab()
    {
        TabContent.Children.Clear();

        // Notifications toggle
        var notifCheck = new CheckBox
        {
            Content = "Notificacoes sonoras",
            IsChecked = _settings.PlayNotifications,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12)
        };
        notifCheck.Checked += (_, _) => _settings.PlayNotifications = true;
        notifCheck.Unchecked += (_, _) => _settings.PlayNotifications = false;
        TabContent.Children.Add(notifCheck);

        // Auto-start toggle
        var autoStartCheck = new CheckBox
        {
            Content = "Iniciar com o Windows",
            IsChecked = IsAutoStartEnabled(),
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12)
        };
        autoStartCheck.Checked += (_, _) => SetAutoStart(true);
        autoStartCheck.Unchecked += (_, _) => SetAutoStart(false);
        TabContent.Children.Add(autoStartCheck);

        // Version info
        var version = new TextBlock
        {
            Text = "Hat para Windows v1.0.0",
            Foreground = (Brush)FindResource("TextMutedBrush"),
            FontSize = 11,
            Margin = new Thickness(0, 24, 0, 0)
        };
        TabContent.Children.Add(version);
    }

    private void ShowAppearanceTab()
    {
        TabContent.Children.Clear();

        // Theme color picker
        var header = new TextBlock
        {
            Text = "TEMA DE COR",
            FontSize = 10, FontWeight = FontWeights.Medium,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        TabContent.Children.Add(header);

        var themePanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };
        foreach (var theme in Enum.GetValues<AppTheme>())
        {
            var color = ThemeColors.ColorFromHex(theme.HexColor());
            var btn = new Button
            {
                Width = 28, Height = 28,
                Margin = new Thickness(0, 0, 8, 8),
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = theme
            };
            btn.Template = CreateCircleButtonTemplate();
            btn.Click += (s, _) =>
            {
                var t = (AppTheme)((Button)s!).Tag;
                _settings.AppThemeName = t.DisplayName();
                ThemeManager.SetAccentTheme(t.DisplayName());
            };
            themePanel.Children.Add(btn);
        }
        TabContent.Children.Add(themePanel);

        // Stealth mode
        var stealthCheck = new CheckBox
        {
            Content = "Modo Stealth (popover quase invisivel)",
            IsChecked = _settings.PopoverStealthMode,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12)
        };
        stealthCheck.Checked += (_, _) => _settings.PopoverStealthMode = true;
        stealthCheck.Unchecked += (_, _) => _settings.PopoverStealthMode = false;
        TabContent.Children.Add(stealthCheck);
    }

    private void ShowModelsTab()
    {
        TabContent.Children.Clear();

        // Inference mode
        var modeHeader = new TextBlock
        {
            Text = "MODO DE INFERENCIA",
            FontSize = 10, FontWeight = FontWeights.Medium,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        TabContent.Children.Add(modeHeader);

        var modeCombo = new ComboBox { Width = 300, Margin = new Thickness(0, 0, 0, 16) };
        modeCombo.Items.Add("Local (Ollama)");
        modeCombo.Items.Add("API na Nuvem");
        modeCombo.SelectedIndex = _settings.InferenceMode == InferenceMode.Local ? 0 : 1;
        modeCombo.SelectionChanged += (_, _) =>
            _settings.InferenceMode = modeCombo.SelectedIndex == 0 ? InferenceMode.Local : InferenceMode.Api;
        TabContent.Children.Add(modeCombo);

        // Provider selection
        var providerHeader = new TextBlock
        {
            Text = "PROVEDOR",
            FontSize = 10, FontWeight = FontWeights.Medium,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        TabContent.Children.Add(providerHeader);

        var providerCombo = new ComboBox { Width = 300, Margin = new Thickness(0, 0, 0, 16) };
        foreach (var p in Enum.GetValues<CloudProvider>())
            providerCombo.Items.Add(p.DisplayName());
        providerCombo.SelectedIndex = (int)_settings.SelectedProvider;
        providerCombo.SelectionChanged += (_, _) =>
            _settings.SelectedProvider = (CloudProvider)providerCombo.SelectedIndex;
        TabContent.Children.Add(providerCombo);

        // API Key
        var keyHeader = new TextBlock
        {
            Text = "CHAVE DE API",
            FontSize = 10, FontWeight = FontWeights.Medium,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        TabContent.Children.Add(keyHeader);

        var keyBox = new PasswordBox
        {
            Width = 300, HorizontalAlignment = HorizontalAlignment.Left,
            Password = CredentialManager.Shared.LoadKey(_settings.SelectedProvider) ?? "",
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 16)
        };
        keyBox.PasswordChanged += (_, _) =>
            CredentialManager.Shared.SaveKey(keyBox.Password, _settings.SelectedProvider);
        TabContent.Children.Add(keyBox);

        // Model name
        var modelHeader = new TextBlock
        {
            Text = "NOME DO MODELO",
            FontSize = 10, FontWeight = FontWeights.Medium,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        TabContent.Children.Add(modelHeader);

        var modelBox = new TextBox
        {
            Width = 300, HorizontalAlignment = HorizontalAlignment.Left,
            Text = _settings.ApiModelName,
            FontSize = 12, FontFamily = new FontFamily("Cascadia Code, Consolas"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        modelBox.TextChanged += (_, _) => _settings.ApiModelName = modelBox.Text;
        TabContent.Children.Add(modelBox);
    }

    private void ShowBehaviorTab()
    {
        TabContent.Children.Clear();

        var promptHeader = new TextBlock
        {
            Text = "PROMPT DO SISTEMA",
            FontSize = 10, FontWeight = FontWeights.Medium,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        TabContent.Children.Add(promptHeader);

        var promptBox = new TextBox
        {
            Text = _settings.SystemPrompt,
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            MinHeight = 120, MaxHeight = 300,
            FontSize = 12,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        promptBox.TextChanged += (_, _) => _settings.SystemPrompt = promptBox.Text;
        TabContent.Children.Add(promptBox);
    }

    private void ShowShortcutsTab()
    {
        TabContent.Children.Clear();

        var shortcuts = new[]
        {
            ("Processar Clipboard", "Ctrl+Shift+X"),
            ("Captura de Tela", "Ctrl+Shift+Z"),
            ("Quick Input", "Ctrl+Shift+Space"),
            ("Toggle Popover", "Ctrl+Shift+H")
        };

        foreach (var (name, key) in shortcuts)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            panel.Children.Add(new TextBlock
            {
                Text = name, Width = 180, FontSize = 13,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(new Border
            {
                Background = (Brush)FindResource("SurfaceTertiaryBrush"),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10, 4, 10, 4),
                Child = new TextBlock
                {
                    Text = key, FontSize = 11, FontFamily = new FontFamily("Cascadia Code, Consolas"),
                    Foreground = (Brush)FindResource("TextSecondaryBrush")
                }
            });
            TabContent.Children.Add(panel);
        }
    }

    // Helpers

    private static ControlTemplate CreateCircleButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CursorProperty, Cursors.Hand);
        template.VisualTree = border;
        return template;
    }

    private static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            return key?.GetValue("Hat") != null;
        }
        catch { return false; }
    }

    private static void SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;
            if (enabled)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath != null) key.SetValue("Hat", exePath);
            }
            else
            {
                key.DeleteValue("Hat", false);
            }
        }
        catch { }
    }
}
