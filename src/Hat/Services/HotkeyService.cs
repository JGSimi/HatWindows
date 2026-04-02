using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;

namespace Hat.Services;

/// <summary>
/// Global keyboard shortcut registration.
/// Replaces macOS KeyboardShortcuts library.
/// Uses NHotkey.Wpf wrapping Win32 RegisterHotKey.
///
/// Default shortcuts (Cmd → Ctrl):
/// - Ctrl+Shift+X: Process clipboard
/// - Ctrl+Shift+Z: Screen capture analysis
/// - Ctrl+Shift+Space: Quick input toggle
/// - Ctrl+Shift+H: Toggle tray popover
/// </summary>
public class HotkeyService
{
    private Action? _onProcessClipboard;
    private Action? _onProcessScreen;
    private Action? _onToggleQuickInput;
    private Action? _onTogglePopover;

    public void Register(
        Action onProcessClipboard,
        Action onProcessScreen,
        Action onToggleQuickInput,
        Action onTogglePopover)
    {
        _onProcessClipboard = onProcessClipboard;
        _onProcessScreen = onProcessScreen;
        _onToggleQuickInput = onToggleQuickInput;
        _onTogglePopover = onTogglePopover;

        try
        {
            HotkeyManager.Current.AddOrReplace("ProcessClipboard",
                Key.X, ModifierKeys.Control | ModifierKeys.Shift, OnHotkey);

            HotkeyManager.Current.AddOrReplace("ProcessScreen",
                Key.Z, ModifierKeys.Control | ModifierKeys.Shift, OnHotkey);

            HotkeyManager.Current.AddOrReplace("QuickInput",
                Key.Space, ModifierKeys.Control | ModifierKeys.Shift, OnHotkey);

            HotkeyManager.Current.AddOrReplace("TogglePopover",
                Key.H, ModifierKeys.Control | ModifierKeys.Shift, OnHotkey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register hotkeys: {ex.Message}");
        }
    }

    public void Unregister()
    {
        try
        {
            HotkeyManager.Current.Remove("ProcessClipboard");
            HotkeyManager.Current.Remove("ProcessScreen");
            HotkeyManager.Current.Remove("QuickInput");
            HotkeyManager.Current.Remove("TogglePopover");
        }
        catch { }
    }

    private void OnHotkey(object? sender, HotkeyEventArgs e)
    {
        switch (e.Name)
        {
            case "ProcessClipboard":
                _onProcessClipboard?.Invoke();
                break;
            case "ProcessScreen":
                _onProcessScreen?.Invoke();
                break;
            case "QuickInput":
                _onToggleQuickInput?.Invoke();
                break;
            case "TogglePopover":
                _onTogglePopover?.Invoke();
                break;
        }
        e.Handled = true;
    }
}
