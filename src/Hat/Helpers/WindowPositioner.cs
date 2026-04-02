using System.Windows;

namespace Hat.Helpers;

/// <summary>
/// Utility for positioning windows relative to screen and taskbar.
/// </summary>
public static class WindowPositioner
{
    /// <summary>
    /// Centers a window on screen, offset slightly above center (like Spotlight).
    /// </summary>
    public static void CenterAbove(Window window, double verticalOffset = 0.35)
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        window.Left = (screenWidth - window.Width) / 2;
        window.Top = screenHeight * verticalOffset - window.ActualHeight / 2;
    }

    /// <summary>
    /// Positions window near the system tray (bottom-right, above taskbar).
    /// </summary>
    public static void PositionNearTray(Window window, double marginRight = 8, double marginBottom = 8)
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        var taskbarHeight = screenHeight - SystemParameters.WorkArea.Height;

        window.Left = screenWidth - window.Width - marginRight;
        window.Top = screenHeight - taskbarHeight - window.Height - marginBottom;
    }
}
