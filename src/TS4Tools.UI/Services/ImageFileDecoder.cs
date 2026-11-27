using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace TS4Tools.UI.Services;

/// <summary>An image decoded to straight (unpremultiplied) RGBA32 pixels.</summary>
/// <param name="Rgba">Row by row, 4 bytes per pixel in R, G, B, A order.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct DecodedImage(byte[] Rgba, int Width, int Height);

/// <summary>
/// Decodes PNG/JPEG (anything Avalonia's <see cref="Bitmap"/> reads) to RGBA32 pixels, so images can be
/// imported into DDS/DST/RLE resources.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DDSPanel.cs lines 417-462 (Import(string/Stream):
/// System.Drawing.Image.FromStream, then DdsFile.CreateImage(Bitmap)). Avalonia's Bitmap replaces System.Drawing.
/// </remarks>
public static class ImageFileDecoder
{
    /// <summary>The largest side accepted, as for DDS files.</summary>
    private const int MaxDimension = 16384;

    /// <summary>Decodes an image file.</summary>
    /// <returns>The pixels, or null when the data is not an image Avalonia can read.</returns>
    public static DecodedImage? Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        try
        {
            using var stream = new MemoryStream(data);
            using var bitmap = new Bitmap(stream);
            return Convert(bitmap);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or NotSupportedException)
        {
            return null;
        }
    }

    private static DecodedImage? Convert(Bitmap bitmap)
    {
        var width = bitmap.PixelSize.Width;
        var height = bitmap.PixelSize.Height;
        if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension) return null;

        var stride = width * 4;
        var pixels = new byte[stride * height];
        var buffer = Marshal.AllocHGlobal(pixels.Length);
        try
        {
            bitmap.CopyPixels(new PixelRect(0, 0, width, height), buffer, pixels.Length, stride);
            Marshal.Copy(buffer, pixels, 0, pixels.Length);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        var format = bitmap.Format ?? PixelFormat.Bgra8888;
        if (format != PixelFormat.Bgra8888 && format != PixelFormat.Rgba8888) return null;

        var swap = format == PixelFormat.Bgra8888;
        var premultiplied = bitmap.AlphaFormat == AlphaFormat.Premul;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            if (swap)
            {
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
            }

            var alpha = pixels[i + 3];
            if (premultiplied && alpha is > 0 and < 255)
            {
                pixels[i] = (byte)Math.Min(255, (pixels[i] * 255 + alpha / 2) / alpha);
                pixels[i + 1] = (byte)Math.Min(255, (pixels[i + 1] * 255 + alpha / 2) / alpha);
                pixels[i + 2] = (byte)Math.Min(255, (pixels[i + 2] * 255 + alpha / 2) / alpha);
            }
        }

        return new DecodedImage(pixels, width, height);
    }
}
