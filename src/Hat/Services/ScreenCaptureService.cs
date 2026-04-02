using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace Hat.Services;

/// <summary>
/// Screen capture service using System.Drawing.
/// Replaces macOS CGWindowListCreateImage / screencapture CLI.
/// </summary>
public static class ScreenCaptureService
{
    /// <summary>
    /// Captures the entire primary screen and returns it as a byte array (PNG).
    /// </summary>
    public static byte[]? CaptureScreen()
    {
        try
        {
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            using var bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));

            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Captures the screen and returns as base64 JPEG string (compressed).
    /// Matches macOS behavior: max 1024px dimension, JPEG 70% quality.
    /// </summary>
    public static string? CaptureScreenAsBase64()
    {
        try
        {
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            using var bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));

            // Resize to max 1024px dimension
            var resized = ResizeImage(bitmap, 1024);

            // Compress as JPEG at 70% quality
            using var ms = new MemoryStream();
            var encoder = GetEncoder(ImageFormat.Jpeg);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 70L);

            if (encoder != null)
                resized.Save(ms, encoder, encoderParams);
            else
                resized.Save(ms, ImageFormat.Jpeg);

            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap ResizeImage(Bitmap source, int maxDimension)
    {
        var width = source.Width;
        var height = source.Height;

        if (width <= maxDimension && height <= maxDimension)
            return new Bitmap(source);

        double ratio;
        if (width > height)
            ratio = (double)maxDimension / width;
        else
            ratio = (double)maxDimension / height;

        var newWidth = (int)(width * ratio);
        var newHeight = (int)(height * ratio);

        var resized = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(source, 0, 0, newWidth, newHeight);

        return resized;
    }

    private static ImageCodecInfo? GetEncoder(ImageFormat format)
    {
        return ImageCodecInfo.GetImageDecoders()
            .FirstOrDefault(codec => codec.FormatID == format.Guid);
    }
}
