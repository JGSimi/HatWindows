namespace Hat.Services;

/// <summary>
/// Auto-update service using Velopack.
/// Replaces macOS Sparkle framework.
/// Checks for updates from GitHub Releases on startup.
/// </summary>
public static class UpdateService
{
    /// <summary>
    /// Checks for updates in background. Non-blocking, silent on failure.
    /// </summary>
    public static async Task CheckForUpdatesAsync()
    {
        try
        {
            // Velopack update check
            // When building with Velopack, uncomment:
            // var mgr = new Velopack.UpdateManager("https://github.com/JGSimi/HatWindows/releases");
            // var updateInfo = await mgr.CheckForUpdatesAsync();
            // if (updateInfo != null)
            // {
            //     await mgr.DownloadUpdatesAsync(updateInfo);
            //     // Optionally prompt user or apply silently
            // }

            await Task.CompletedTask; // Placeholder until Velopack is configured
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }
}
