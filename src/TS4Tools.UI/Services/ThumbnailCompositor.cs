using SkiaSharp;

namespace TS4Tools.UI.Services;

/// <summary>
/// Combines the JPEG colour data of a JFIF+ALFA thumbnail with its embedded alpha PNG.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ThumbnailResource.cs lines 119-160
/// (TransformToPNG: the colour image and the alpha image must have the same size, otherwise
/// "Not a proper TS4 Thumbnail image") and lines 195-234 (UpdateAlpha: alpha = the R channel of the
/// alpha image, colour = RGB of the JPEG).
/// </remarks>
public static class ThumbnailCompositor
{
    /// <summary>
    /// Composites the colour and alpha images into RGBA32 pixels.
    /// </summary>
    /// <param name="jpegData">The JFIF data (the embedded ALFA segment is ignored by the decoder).</param>
    /// <param name="alphaPng">The embedded alpha PNG.</param>
    /// <returns>Unpremultiplied RGBA32 pixels (row by row, 4 bytes per pixel) and the image size.</returns>
    /// <exception cref="InvalidDataException">An image cannot be decoded or the two sizes differ.</exception>
    public static (byte[] Rgba, int Width, int Height) Composite(ReadOnlySpan<byte> jpegData, ReadOnlySpan<byte> alphaPng)
    {
        using var color = DecodeRgba(jpegData, "thumbnail colour image");
        using var alpha = DecodeRgba(alphaPng, "thumbnail alpha image");

        // Source: ThumbnailResource.cs line 143
        if (color.Width != alpha.Width || color.Height != alpha.Height)
        {
            throw new InvalidDataException("Not a proper TS4 Thumbnail image");
        }

        var rgba = color.GetPixelSpan().ToArray();
        var alphaPixels = alpha.GetPixelSpan();
        for (var i = 0; i < rgba.Length; i += 4)
        {
            rgba[i + 3] = alphaPixels[i]; // A = R of the alpha image
        }

        return (rgba, color.Width, color.Height);
    }

    private static SKBitmap DecodeRgba(ReadOnlySpan<byte> data, string what)
    {
        SKBitmap? decoded;
        try
        {
            decoded = SKBitmap.Decode(data); // throws ArgumentNullException when no codec recognises the data
        }
        catch (ArgumentNullException)
        {
            throw new InvalidDataException($"Cannot decode the {what}");
        }
        using var _ = decoded;
        if (decoded == null)
        {
            throw new InvalidDataException($"Cannot decode the {what}");
        }

        var info = new SKImageInfo(decoded.Width, decoded.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var converted = new SKBitmap(info);
        if (!decoded.CopyTo(converted, SKColorType.Rgba8888))
        {
            converted.Dispose();
            throw new InvalidDataException($"Cannot convert the {what}");
        }
        return converted;
    }
}
