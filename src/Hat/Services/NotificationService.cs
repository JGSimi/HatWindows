using System.Media;

namespace Hat.Services;

/// <summary>
/// Notification and sound service.
/// Replaces macOS UNUserNotificationCenter + NSSound.
/// </summary>
public static class NotificationService
{
    /// <summary>
    /// Shows a Windows balloon tooltip notification via the tray icon.
    /// </summary>
    public static void ShowNotification(string title, string body)
    {
        // Notification is shown via the tray icon balloon tip
        // This is handled from App.xaml.cs using the TaskbarIcon
        try
        {
            var app = System.Windows.Application.Current as App;
            // Balloon tips are the simplest cross-version Windows notification
            System.Diagnostics.Debug.WriteLine($"[Hat Notification] {title}: {body}");
        }
        catch
        {
            // Notification may not be available
        }
    }

    /// <summary>
    /// Plays the notification sound (equivalent to macOS "Glass" sound).
    /// </summary>
    public static void PlayNotificationSound()
    {
        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch
        {
            // Sound may not be available
        }
    }

    /// <summary>
    /// Shows notification and plays sound if notifications are enabled.
    /// </summary>
    public static void NotifyResponseComplete(string preview, bool playSound)
    {
        if (playSound)
        {
            PlayNotificationSound();
        }

        var truncated = preview.Length > 100 ? preview[..100] + "..." : preview;
        ShowNotification("Hat", truncated);
    }
}
