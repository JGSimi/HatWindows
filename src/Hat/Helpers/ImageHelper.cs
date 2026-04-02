using System.Drawing;
using System.Drawing.Imaging;

namespace Hat.Helpers;

/// <summary>
/// Image processing utilities.
/// Port of NSImage.resizedAndCompressedBase64() from ContentView.swift.
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// Resizes image to max dimension and compresses as JPEG base64.
    /// Matches macOS behavior: max 1024px, JPEG 70% quality.
    /// </summary>
    public static string? ResizeAndCompressBase64(byte[] imageData, int maxDimension = 1024, int quality = 70)
    {
        try
        {
            using var ms = new MemoryStream(imageData);
            using var original = new Bitmap(ms);
            using var resized = Resize(original, maxDimension);

            using var output = new MemoryStream();
            var encoder = GetJpegEncoder();
            var encoderParams = new EncoderParameters(1)
            {
                Param = { [0] = new EncoderParameter(Encoder.Quality, (long)quality) }
            };

            if (encoder != null)
                resized.Save(output, encoder, encoderParams);
            else
                resized.Save(output, ImageFormat.Jpeg);

            return Convert.ToBase64String(output.ToArray());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resizes image from file path to base64.
    /// </summary>
    public static string? ResizeAndCompressBase64(string filePath, int maxDimension = 1024, int quality = 70)
    {
        try
        {
            var data = File.ReadAllBytes(filePath);
            return ResizeAndCompressBase64(data, maxDimension, quality);
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap Resize(Bitmap source, int maxDimension)
    {
        if (source.Width <= maxDimension && source.Height <= maxDimension)
            return new Bitmap(source);

        double ratio = source.Width > source.Height
            ? (double)maxDimension / source.Width
            : (double)maxDimension / source.Height;

        var newWidth = (int)(source.Width * ratio);
        var newHeight = (int)(source.Height * ratio);

        var resized = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(source, 0, 0, newWidth, newHeight);

        return resized;
    }

    private static ImageCodecInfo? GetJpegEncoder()
    {
        return ImageCodecInfo.GetImageDecoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
    }
}
