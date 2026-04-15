using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Hat.Services;

/// <summary>
/// Result of a clipboard read attempt.
/// </summary>
public enum ClipboardReadStatus
{
    /// <summary>Read succeeded and clipboard had text and/or image content.</summary>
    Ok,
    /// <summary>Read succeeded but clipboard was actually empty.</summary>
    Empty,
    /// <summary>Read failed — another process held the clipboard lock after all retries.</summary>
    Locked,
}

/// <summary>
/// Snapshot of clipboard contents produced by <see cref="ClipboardService.Snapshot"/>.
/// </summary>
public readonly record struct ClipboardSnapshot(
    string? Text,
    string? ImageBase64,
    ClipboardReadStatus Status);

/// <summary>
/// Clipboard read/write service.
/// Replaces macOS NSPasteboard / PasteboardClient.
///
/// The Win32 clipboard requires exclusive access. When a global hotkey fires
/// right after Ctrl+C the source app may still hold the clipboard lock, causing
/// <see cref="COMException"/> with HRESULT 0x800401D0 (CLIPBRD_E_CANT_OPEN).
/// All read/write methods retry briefly to absorb this race.
/// </summary>
public static class ClipboardService
{
    private const int MaxAttempts = 10;
    private const int RetryDelayMs = 10;

    /// <summary>
    /// Atomically reads text and image from the clipboard with retries.
    /// Returns a status so callers can distinguish "empty" from "locked by another process".
    /// </summary>
    public static ClipboardSnapshot Snapshot()
    {
        string? text = null;
        BitmapSource? image = null;
        bool completed = false;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                image = Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
                completed = true;
                break;
            }
            catch (COMException)
            {
                if (attempt < MaxAttempts - 1)
                    Thread.Sleep(RetryDelayMs);
            }
            catch
            {
                // Non-COM exception — something is wrong beyond a simple lock contention.
                break;
            }
        }

        if (!completed)
            return new ClipboardSnapshot(null, null, ClipboardReadStatus.Locked);

        string? imageBase64 = image != null ? EncodeImageAsBase64(image) : null;
        bool hasText = !string.IsNullOrEmpty(text);
        bool hasImage = imageBase64 != null;

        if (!hasText && !hasImage)
            return new ClipboardSnapshot(null, null, ClipboardReadStatus.Empty);

        return new ClipboardSnapshot(text, imageBase64, ClipboardReadStatus.Ok);
    }

    /// <summary>
    /// Gets text from clipboard, or null if no text or the read failed.
    /// Prefer <see cref="Snapshot"/> when you need to distinguish empty from locked.
    /// </summary>
    public static string? GetText()
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch (COMException)
            {
                if (attempt < MaxAttempts - 1)
                    Thread.Sleep(RetryDelayMs);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Sets text to clipboard with retries — the lock may be briefly held by
    /// whichever app had focus at the moment of the call.
    /// </summary>
    public static void SetText(string text)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return;
            }
            catch (COMException)
            {
                if (attempt < MaxAttempts - 1)
                    Thread.Sleep(RetryDelayMs);
            }
            catch
            {
                return;
            }
        }
    }

    private static string? EncodeImageAsBase64(BitmapSource image)
    {
        try
        {
            // Convert BitmapSource to System.Drawing.Bitmap
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(ms);

            using var bitmap = new Bitmap(ms);

            // Resize to max 1024px
            var resized = ResizeImage(bitmap, 1024);

            // Compress as JPEG 70%
            using var jpegMs = new MemoryStream();
            var jpegEncoder = ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 70L);

            if (jpegEncoder != null)
                resized.Save(jpegMs, jpegEncoder, encoderParams);
            else
                resized.Save(jpegMs, ImageFormat.Jpeg);

            return Convert.ToBase64String(jpegMs.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap ResizeImage(Bitmap source, int maxDimension)
    {
        if (source.Width <= maxDimension && source.Height <= maxDimension)
            return new Bitmap(source);

        double ratio = source.Width > source.Height
            ? (double)maxDimension / source.Width
            : (double)maxDimension / source.Height;

        var newWidth = (int)(source.Width * ratio);
        var newHeight = (int)(source.Height * ratio);

        var resized = new Bitmap(newWidth, newHeight);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(source, 0, 0, newWidth, newHeight);
        return resized;
    }
}
