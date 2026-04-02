using System.Threading;
using System.Windows;
using Hat.Services;
using Hat.ViewModels;
using Hat.Views.Windows;

namespace Hat;

public partial class App : Application
{
    private static Mutex? _mutex;
    private HotkeyService? _hotkeyService;
    private TrayPopoverWindow? _trayPopover;
    private H.NotifyIcon.TaskbarIcon? _taskbarIcon;

    public static AssistantViewModel AssistantVM { get; private set; } = null!;
    public static ConversationManager ConversationMgr { get; private set; } = null!;
    public static SettingsStore Settings { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Single-instance enforcement
        const string mutexName = "HatWindowsApp_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show("Hat ja esta em execucao.", "Hat", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Apply theme based on Windows system setting
        ThemeManager.ApplySystemTheme();

        // Initialize core services
        Settings = new SettingsStore();
        ConversationMgr = new ConversationManager();
        AssistantVM = new AssistantViewModel();

        // Setup system tray
        SetupTrayIcon();

        // Register global hotkeys
        _hotkeyService = new HotkeyService();
        _hotkeyService.Register(
            onProcessClipboard: () => Dispatcher.Invoke(() => AssistantVM.ProcessClipboard()),
            onProcessScreen: () => Dispatcher.Invoke(() => AssistantVM.ProcessScreen()),
            onToggleQuickInput: () => Dispatcher.Invoke(ToggleQuickInput),
            onTogglePopover: () => Dispatcher.Invoke(TogglePopover)
        );

        // Check for first run
        if (!Settings.HasCompletedOnboarding)
        {
            var welcome = new WelcomeWindow();
            welcome.Show();
        }

        // Check for updates in background
        _ = UpdateService.CheckForUpdatesAsync();
    }

    private void SetupTrayIcon()
    {
        _taskbarIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "Hat - Assistente de IA",
        };

        // Set icon based on processing state
        UpdateTrayIcon(false);

        _taskbarIcon.TrayLeftMouseUp += (_, _) => TogglePopover();
    }

    public void UpdateTrayIcon(bool isProcessing)
    {
        // Icon switching will use embedded resources
        // hat-icon.ico for normal, sunglasses for processing
        var iconUri = isProcessing
            ? new Uri("pack://application:,,,/Assets/Icons/sunglasses.ico")
            : new Uri("pack://application:,,,/Assets/Icons/hat-icon.ico");

        try
        {
            var iconStream = GetResourceStream(iconUri)?.Stream;
            if (iconStream != null && _taskbarIcon != null)
            {
                _taskbarIcon.Icon = new System.Drawing.Icon(iconStream);
            }
        }
        catch
        {
            // Fallback: use default icon
        }
    }

    private void TogglePopover()
    {
        if (_trayPopover == null || !_trayPopover.IsLoaded)
        {
            _trayPopover = new TrayPopoverWindow();
            _trayPopover.Show();
        }
        else if (_trayPopover.IsVisible)
        {
            _trayPopover.Hide();
        }
        else
        {
            _trayPopover.Show();
            _trayPopover.Activate();
        }
    }

    private QuickInputWindow? _quickInputWindow;

    private void ToggleQuickInput()
    {
        if (_quickInputWindow == null || !_quickInputWindow.IsLoaded)
        {
            _quickInputWindow = new QuickInputWindow();
            _quickInputWindow.Show();
            _quickInputWindow.Activate();
        }
        else if (_quickInputWindow.IsVisible)
        {
            _quickInputWindow.Hide();
        }
        else
        {
            _quickInputWindow.Show();
            _quickInputWindow.Activate();
        }
    }

    public void OpenMainWindow()
    {
        var existing = Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }
        var main = new MainWindow();
        main.Show();
    }

    public void OpenSettingsWindow()
    {
        var existing = Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }
        var settings = new SettingsWindow();
        settings.Show();
    }

    public void OpenAnalysisWindow()
    {
        var existing = Current.Windows.OfType<AnalysisWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }
        var analysis = new AnalysisWindow();
        analysis.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Unregister();
        _taskbarIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
