using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Hat.Services;
using Hat.Theme;
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
        // Global exception handler — prevents silent crashes
        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Single-instance enforcement
        const string mutexName = "HatWindowsApp_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show("Hat ja esta em execucao.", "Hat", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        try
        {
            // Initialize core services first (before theme, since theme updates resources)
            Settings = new SettingsStore();
            ConversationMgr = new ConversationManager();
            AssistantVM = new AssistantViewModel();

            // Apply theme based on Windows system setting
            ThemeManager.ApplySystemTheme();

            // Apply saved accent color
            if (!string.IsNullOrEmpty(Settings.AppThemeName))
                ThemeManager.SetAccentTheme(Settings.AppThemeName);

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
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao iniciar Hat:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Hat - Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void SetupTrayIcon()
    {
        _taskbarIcon = new H.NotifyIcon.TaskbarIcon();
        _taskbarIcon.ToolTipText = "Hat - Assistente de IA";

        // Set icon — use embedded resource or fallback
        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/Icons/hat-icon.ico");
            var streamInfo = GetResourceStream(iconUri);
            if (streamInfo?.Stream != null)
            {
                _taskbarIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
            }
        }
        catch
        {
            _taskbarIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        // Left click to toggle popover
        _taskbarIcon.TrayLeftMouseUp += (_, _) => TogglePopover();

        // Right-click context menu
        var contextMenu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem { Header = "Abrir Hat" };
        openItem.Click += (_, _) => OpenMainWindow();
        contextMenu.Items.Add(openItem);

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Configuracoes" };
        settingsItem.Click += (_, _) => OpenSettingsWindow();
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Sair" };
        exitItem.Click += (_, _) => Shutdown();
        contextMenu.Items.Add(exitItem);

        _taskbarIcon.ContextMenu = contextMenu;
    }

    public void UpdateTrayIcon(bool isProcessing)
    {
        if (_taskbarIcon == null) return;

        try
        {
            var iconName = isProcessing ? "sunglasses.ico" : "hat-icon.ico";
            var iconUri = new Uri($"pack://application:,,,/Assets/Icons/{iconName}");
            var streamInfo = GetResourceStream(iconUri);
            if (streamInfo?.Stream != null)
            {
                _taskbarIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
            }
        }
        catch { }

        _taskbarIcon.ToolTipText = isProcessing
            ? "Hat - Processando..."
            : "Hat - Assistente de IA";
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

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Global Exception Handlers
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Erro inesperado:\n\n{e.Exception.Message}",
            "Hat - Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show($"Erro critico:\n\n{ex.Message}",
                "Hat - Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved(); // Prevent crash
        System.Diagnostics.Debug.WriteLine($"Unobserved task exception: {e.Exception.Message}");
    }
}
