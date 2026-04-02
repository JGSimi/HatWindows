using System.Media;

namespace Hat.Services;

/// <summary>
/// Notification and sound service.
/// Replaces macOS UNUserNotificationCenter + NSSound.
/// Uses Windows toast notifications and system sounds.
/// </summary>
public static class NotificationService
{
    /// <summary>
    /// Shows a toast notification with the given title and body.
    /// </summary>
    public static void ShowNotification(string title, string body)
    {
        try
        {
            // Using Microsoft.Toolkit.Uwp.Notifications
            new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .Show();
        }
        catch
        {
            // Toast notifications may not be available in all environments
        }
    }

    /// <summary>
    /// Plays the notification sound (equivalent to macOS "Glass" sound).
    /// </summary>
    public static void PlayNotificationSound()
    {
        try
        {
            // Use Windows system sound as equivalent to macOS "Glass"
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
