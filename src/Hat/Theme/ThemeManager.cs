using System.Windows;
using Microsoft.Win32;

namespace Hat.Theme;

/// <summary>
/// Manages light/dark theme switching and accent color changes.
/// Listens to Windows system theme changes.
/// </summary>
public static class ThemeManager
{
    private static bool _isDarkMode = true;
    public static bool IsDarkMode => _isDarkMode;

    public static event Action? ThemeChanged;

    public static void ApplySystemTheme()
    {
        _isDarkMode = IsSystemDarkTheme();
        ApplyTheme(_isDarkMode);

        // Listen for system theme changes
        try
        {
            SystemEvents.UserPreferenceChanged += (_, args) =>
            {
                if (args.Category == UserPreferenceCategory.General)
                {
                    var isDark = IsSystemDarkTheme();
                    if (isDark != _isDarkMode)
                    {
                        _isDarkMode = isDark;
                        Application.Current?.Dispatcher?.Invoke(() => ApplyTheme(isDark));
                    }
                }
            };
        }
        catch { }
    }

    public static void ApplyTheme(bool isDark)
    {
        _isDarkMode = isDark;

        var app = Application.Current;
        if (app == null) return;

        try
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri(isDark
                    ? "pack://application:,,,/Theme/DarkTheme.xaml"
                    : "pack://application:,,,/Theme/LightTheme.xaml")
            };

            app.Resources.MergedDictionaries.Clear();
            app.Resources.MergedDictionaries.Add(dict);

            ThemeChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Theme apply failed: {ex.Message}");
        }
    }

    public static void SetAccentTheme(string themeName)
    {
        if (!ThemeColors.ThemePresets.TryGetValue(themeName, out var preset)) return;

        var app = Application.Current;
        if (app == null) return;

        try
        {
            app.Resources["AccentPrimaryBrush"] = new System.Windows.Media.SolidColorBrush(preset.Primary);
            app.Resources["AccentPrimaryHoverBrush"] = new System.Windows.Media.SolidColorBrush(preset.Hover);
            app.Resources["AccentPrimaryColor"] = preset.Primary;
            app.Resources["AccentPrimaryHoverColor"] = preset.Hover;

            app.Resources["AccentSubtleBrush"] = new System.Windows.Media.SolidColorBrush(
                ThemeColors.ColorFromAlpha(preset.Primary, _isDarkMode ? 0.12 : 0.08));
            app.Resources["BorderFocusedBrush"] = new System.Windows.Media.SolidColorBrush(
                ThemeColors.ColorFromAlpha(preset.Primary, 0.50));

            ThemeChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Accent theme failed: {ex.Message}");
        }
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intVal && intVal == 0;
        }
        catch
        {
            return true;
        }
    }
}
