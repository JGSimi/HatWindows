using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace Hat.Services;

/// <summary>
/// Clipboard read/write service.
/// Replaces macOS NSPasteboard / PasteboardClient.
/// </summary>
public static class ClipboardService
{
    /// <summary>
    /// Gets text from clipboard, or null if no text.
    /// </summary>
    public static string? GetText()
    {
        try
        {
            if (Clipboard.ContainsText())
                return Clipboard.GetText();
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets text to clipboard.
    /// </summary>
    public static void SetText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            // Clipboard may be locked by another process
        }
    }

    /// <summary>
    /// Gets an image from clipboard as base64 JPEG (compressed).
    /// Matches macOS: max 1024px, JPEG 70% quality.
    /// </summary>
    public static string? GetImageAsBase64()
    {
        try
        {
            if (!Clipboard.ContainsImage()) return null;

            var image = Clipboard.GetImage();
            if (image == null) return null;

            // Convert BitmapSource to System.Drawing.Bitmap
            using var ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
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

    /// <summary>
    /// Returns true if clipboard contains an image.
    /// </summary>
    public static bool ContainsImage()
    {
        try { return Clipboard.ContainsImage(); }
        catch { return false; }
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
