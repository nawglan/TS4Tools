// Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DdsFile.cs lines 28-1903 (class Dds and class DdsFile)
// Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/RGBHSV.cs (HSV support)

namespace TS4Tools.Wrappers;

/// <summary>
/// An editable in-memory DDS image: loads any DDS format the legacy <c>DdsFile</c> supports, offers the
/// same alpha/HSV/resize operations, and saves back to DDS (DXT compression via <see cref="DxtEncoder"/>).
/// </summary>
/// <remarks>
/// Source: DdsFile.cs lines 930-1903 (class DdsFile). Pixels are stored as packed ARGB (<c>uint</c>,
/// alpha in the high byte) exactly like the legacy <c>uint[]</c> image data. Differences from legacy:
/// <list type="bullet">
/// <item>No <c>System.Drawing</c>: the Bitmap/Image overloads are replaced by <see cref="FromRgba32"/> and <see cref="ToRgba32"/>.</item>
/// <item><see cref="Resize"/> uses a managed bilinear filter instead of GDI+ (bicubic) and, like legacy
/// (lines 1103-1125), resizes colour with alpha stripped and alpha separately.</item>
/// <item>Legacy <c>SetPixel</c> indexes <c>y * height + x</c> (lines 1559-1560), which is wrong for non-square images; the
/// correct <c>y * width + x</c> is used.</item>
/// <item>Mipmap generation (<see cref="SaveToDds(DdsFormat?, bool)"/> with <c>generateMipmaps</c>) is an addition: legacy always
/// writes a single level. Levels are produced with a 2x2 box filter, which is what RLE/DST import needs.</item>
/// </list>
/// </remarks>
public sealed class DdsImage
{
    private uint[] _pixels;
    private ColorHsva[]? _hsv;

    private DdsImage(int width, int height, uint[] pixels)
    {
        Width = width;
        Height = height;
        _pixels = pixels;
    }

    /// <summary>Image width in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Image height in pixels.</summary>
    public int Height { get; private set; }

    /// <summary>True when saved with block (DXT) compression.</summary>
    public bool UseDxt { get; private set; } = true;

    /// <summary>True when the image is an A8L8 luminance map.</summary>
    public bool UseLuminance { get; private set; }

    /// <summary>
    /// Alpha depth in bits as the legacy code tracks it: 0 none, 1 (DXT1/A1R5G5B5), 3 (DXT3), 4 (A4R4G4B4), 5 (DXT5), 8.
    /// </summary>
    public int AlphaDepth { get; set; } = 5;

    /// <summary>The hue/saturation/value shift applied by <see cref="ToRgba32(bool)"/> when HSV support is enabled.</summary>
    public HsvShift HsvShift { get; set; }

    /// <summary>True when HSV data is maintained for this image.</summary>
    public bool SupportsHsv
    {
        get => _hsv is not null;
        set
        {
            if (value == SupportsHsv)
                return;
            _hsv = value ? ColorHsva.FromArgbArray(_pixels) : null;
        }
    }

    // ------------------------------------------------------------------ construction

    /// <summary>
    /// Creates an image of one colour.
    /// Source: DdsFile.cs lines 1000-1050 (CreateImage(byte r,g,b,a,...)).
    /// </summary>
    public static DdsImage CreateSolid(uint argb, int width, int height)
    {
        ValidateSize(width, height);
        var pixels = new uint[width * height];
        Array.Fill(pixels, argb);
        return new DdsImage(width, height, pixels);
    }

    /// <summary>Creates an image from packed ARGB pixels (copied).</summary>
    public static DdsImage FromArgb(ReadOnlySpan<uint> argb, int width, int height)
    {
        ValidateSize(width, height);
        if (argb.Length != width * height)
            throw new ArgumentException("Image data must contain width * height elements.", nameof(argb));
        return new DdsImage(width, height, argb.ToArray());
    }

    /// <summary>Creates an image from RGBA32 bytes (R, G, B, A per pixel).</summary>
    /// <remarks>Source: DdsFile.cs lines 1129-1170 (CreateImage(Bitmap)): a new bitmap becomes a DXT5 image.</remarks>
    public static DdsImage FromRgba32(ReadOnlySpan<byte> rgba, int width, int height)
    {
        ValidateSize(width, height);
        if (rgba.Length != width * height * 4)
            throw new ArgumentException("Pixel data must contain width * height * 4 bytes.", nameof(rgba));

        var pixels = new uint[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            int o = i * 4;
            pixels[i] = ((uint)rgba[o + 3] << 24) | ((uint)rgba[o] << 16) | ((uint)rgba[o + 1] << 8) | rgba[o + 2];
        }

        return new DdsImage(width, height, pixels);
    }

    /// <summary>Creates an independent copy.</summary>
    /// <remarks>Source: DdsFile.cs lines 1052-1066 (CreateImage(DdsFile, bool)).</remarks>
    public DdsImage Clone()
    {
        var copy = new DdsImage(Width, Height, (uint[])_pixels.Clone())
        {
            UseDxt = UseDxt,
            UseLuminance = UseLuminance,
            AlphaDepth = AlphaDepth,
            HsvShift = HsvShift,
        };
        copy.SupportsHsv = SupportsHsv;
        return copy;
    }

    /// <summary>
    /// Loads a DDS file. Only the first (largest) surface is read.
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a DDS file, is truncated, or is of an unsupported format.</exception>
    /// <remarks>Source: DdsFile.cs lines 1250-1290 (DdsFile.Load) and 594-651 (Dds(Stream)).</remarks>
    public static DdsImage Load(ReadOnlySpan<byte> ddsData)
    {
        if (!DdsHeader.TryParse(ddsData, out var header, out var error) || header is null)
            throw new InvalidDataException(error);

        if ((long)header.Width * header.Height > 100_000_000)
            throw new InvalidDataException("Image too large.");

        var body = ddsData[DdsHeader.FileHeaderSize..];
        if (header.DataSize > body.Length)
            throw new InvalidDataException("DDS data is shorter than the header describes.");

        var rgba = DxtDecoder.DecompressDds(ddsData)
            ?? throw new InvalidDataException("Unable to decode DDS pixel data.");

        var img = FromRgba32(rgba, header.Width, header.Height);
        img.UseDxt = false;
        img.UseLuminance = false;
        img.AlphaDepth = 0;

        switch (header.Format)
        {
            case DdsFormat.Dxt1: img.UseDxt = true; img.AlphaDepth = 1; break;
            case DdsFormat.Dxt3: img.UseDxt = true; img.AlphaDepth = 3; break;
            case DdsFormat.Dxt5: img.UseDxt = true; img.AlphaDepth = 5; break;
            case DdsFormat.A8L8: img.UseLuminance = true; img.AlphaDepth = 8; break;
            case DdsFormat.A1R5G5B5: img.AlphaDepth = 1; break;
            case DdsFormat.A4R4G4B4: img.AlphaDepth = 4; break;
            case DdsFormat.A8B8G8R8:
            case DdsFormat.A8R8G8B8: img.AlphaDepth = 8; break;
            default: break;
        }

        return img;
    }

    /// <summary>Attempts to load a DDS file, returning null instead of throwing.</summary>
    public static DdsImage? TryLoad(ReadOnlySpan<byte> ddsData)
    {
        try
        {
            return Load(ddsData);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    // ------------------------------------------------------------------ pixel access

    /// <summary>Gets a copy of the packed ARGB pixels (alpha in the high byte).</summary>
    public uint[] GetArgb() => (uint[])_pixels.Clone();

    /// <summary>Gets one packed ARGB pixel.</summary>
    public uint GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(x));
        return _pixels[(y * Width) + x];
    }

    /// <summary>Sets one packed ARGB pixel.</summary>
    public void SetPixel(int x, int y, uint argb)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(x));
        _pixels[(y * Width) + x] = argb;
        RefreshHsv();
    }

    /// <summary>
    /// Converts to RGBA32 bytes (R, G, B, A), optionally applying <see cref="HsvShift"/>.
    /// Source: DdsFile.cs lines 1490-1530 (GetImage): the HSV-shifted source is used when HSV is supported and the shift is non-empty.
    /// </summary>
    public byte[] ToRgba32(bool applyHsvShift = true)
    {
        var src = applyHsvShift && SupportsHsv && !HsvShift.IsEmpty
            ? ColorHsva.ToArgbArray(_hsv!, HsvShift)
            : _pixels;

        var rgba = new byte[src.Length * 4];
        for (int i = 0; i < src.Length; i++)
        {
            uint p = src[i];
            rgba[i * 4] = (byte)(p >> 16);
            rgba[(i * 4) + 1] = (byte)(p >> 8);
            rgba[(i * 4) + 2] = (byte)p;
            rgba[(i * 4) + 3] = (byte)(p >> 24);
        }

        return rgba;
    }

    // ------------------------------------------------------------------ format selection

    /// <summary>Enables/disables DXT compression. Source: DdsFile.cs lines 1325-1345 (UseDXT).</summary>
    public void SetUseDxt(bool value)
    {
        if (value == UseDxt)
            return;

        UseDxt = value;
        if (value)
        {
            UseLuminance = false;
            AlphaDepth = 5;
        }
        else
        {
            AlphaDepth = 8;
        }
    }

    /// <summary>Enables/disables luminance (A8L8) storage. Source: DdsFile.cs lines 1347-1362 (UseLuminance).</summary>
    public void SetUseLuminance(bool value)
    {
        if (value == UseLuminance)
            return;

        UseLuminance = value;
        if (value)
            SetUseDxt(false);
    }

    /// <summary>The format <see cref="SaveToDds(DdsFormat?, bool)"/> chooses by default.</summary>
    /// <remarks>Source: DdsFile.cs lines 1292-1316 (DdsFile.Save format selection).</remarks>
    public DdsFormat SaveFormat
    {
        get
        {
            if (UseDxt)
            {
                return AlphaDepth switch
                {
                    1 => DdsFormat.Dxt1,
                    3 => DdsFormat.Dxt3,
                    _ => DdsFormat.Dxt5,
                };
            }

            if (UseLuminance)
                return DdsFormat.A8L8;

            return AlphaDepth switch
            {
                0 => DdsFormat.R8G8B8,
                1 => DdsFormat.A1R5G5B5,
                4 => DdsFormat.A4R4G4B4,
                _ => DdsFormat.A8R8G8B8,
            };
        }
    }

    // ------------------------------------------------------------------ saving

    /// <summary>
    /// Encodes to a complete DDS file.
    /// </summary>
    /// <param name="format">The format to write; null uses <see cref="SaveFormat"/> (legacy behaviour).</param>
    /// <param name="generateMipmaps">When true, a full mip chain down to 1x1 is generated with a 2x2 box filter.</param>
    /// <remarks>
    /// Source: DdsFile.cs lines 1292-1320 (DdsFile.Save), 653-700 (Dds.Write). Uses the base (un-shifted) pixels.
    /// </remarks>
    public byte[] SaveToDds(DdsFormat? format = null, bool generateMipmaps = false)
    {
        var fmt = format ?? SaveFormat;

        var levels = new List<(uint[] Pixels, int W, int H)> { (_pixels, Width, Height) };
        if (generateMipmaps)
        {
            var (cur, w, h) = levels[0];
            while (w > 1 || h > 1)
            {
                int nw = Math.Max(1, w / 2);
                int nh = Math.Max(1, h / 2);
                cur = BoxDownsample(cur, w, h, nw, nh);
                w = nw;
                h = nh;
                levels.Add((cur, w, h));
            }
        }

        var header = DdsHeader.Create(fmt, Width, Height, levels.Count);
        var output = new List<byte>((int)Math.Min(int.MaxValue, DdsHeader.FileHeaderSize + header.DataSize * 2));
        header.Write(output);

        foreach (var (pixels, w, h) in levels)
            output.AddRange(EncodeLevel(header, fmt, pixels, w, h));

        return [.. output];
    }

    private static byte[] EncodeLevel(DdsHeader header, DdsFormat fmt, uint[] pixels, int w, int h)
    {
        if (fmt is DdsFormat.Dxt1 or DdsFormat.Dxt3 or DdsFormat.Dxt5)
        {
            var rgba = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                uint p = pixels[i];
                rgba[i * 4] = (byte)(p >> 16);
                rgba[(i * 4) + 1] = (byte)(p >> 8);
                rgba[(i * 4) + 2] = (byte)p;
                rgba[(i * 4) + 3] = (byte)(p >> 24);
            }

            return DxtEncoder.Compress(rgba, w, h, fmt);
        }

        // Uncompressed: levels other than the first have their own pitch
        int pixelSize = header.PixelSize;
        int rowPitch = (int)(((w * (long)pixelSize * 8) + 7) / 8);
        var buf = new byte[rowPitch * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                uint stored = header.FromArgb(pixels[(y * w) + x]);
                int o = (y * rowPitch) + (x * pixelSize);
                for (int i = 0; i < pixelSize; i++)
                    buf[o + i] = (byte)((stored >> (8 * i)) & 0xFF);
            }
        }

        return buf;
    }

    private static uint[] BoxDownsample(uint[] src, int w, int h, int nw, int nh)
    {
        var dst = new uint[nw * nh];
        for (int y = 0; y < nh; y++)
        {
            int y0 = Math.Min(y * 2, h - 1);
            int y1 = Math.Min((y * 2) + 1, h - 1);
            for (int x = 0; x < nw; x++)
            {
                int x0 = Math.Min(x * 2, w - 1);
                int x1 = Math.Min((x * 2) + 1, w - 1);
                uint a = src[(y0 * w) + x0], b = src[(y0 * w) + x1], c = src[(y1 * w) + x0], d = src[(y1 * w) + x1];
                uint result = 0;
                for (int shift = 0; shift <= 24; shift += 8)
                {
                    uint sum = ((a >> shift) & 0xFF) + ((b >> shift) & 0xFF) + ((c >> shift) & 0xFF) + ((d >> shift) & 0xFF);
                    result |= ((sum + 2) / 4) << shift;
                }

                dst[(y * nw) + x] = result;
            }
        }

        return dst;
    }

    // ------------------------------------------------------------------ alpha channel

    /// <summary>
    /// Replaces the alpha channel with the average of the R, G and B channels of <paramref name="source"/>
    /// (tiled when the sizes differ).
    /// </summary>
    /// <remarks>Source: DdsFile.cs lines 1392-1412 (SetAlphaFromGreyscale(DdsFile)): alpha = (R + G + B) / 3.</remarks>
    public void SetAlphaFromGreyscale(DdsImage source)
    {
        ArgumentNullException.ThrowIfNull(source);

        AlphaDepth = UseDxt ? 5 : 8;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                uint src = source._pixels[((y % source.Height) * source.Width) + (x % source.Width)];
                uint alpha = (((src >> 16) & 0xFF) + ((src >> 8) & 0xFF) + (src & 0xFF)) / 3 & 0xFF;
                int i = (y * Width) + x;
                _pixels[i] = (_pixels[i] & 0x00FFFFFF) | (alpha << 24);
            }
        }

        RefreshHsv();
    }

    /// <summary>
    /// Makes the image fully opaque and removes the alpha channel.
    /// </summary>
    /// <exception cref="InvalidOperationException">The image is a luminance map.</exception>
    /// <remarks>Source: DdsFile.cs lines 1454-1466 (DeleteAlphaChannel).</remarks>
    public void DeleteAlphaChannel()
    {
        if (UseLuminance)
            throw new InvalidOperationException("Alpha channel cannot be deleted for a luminance map.");

        AlphaDepth = UseDxt ? 1 : 0;
        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] |= 0xFF000000;

        RefreshHsv();
    }

    /// <summary>
    /// Returns a greyscale image whose R, G and B all equal this image's alpha.
    /// </summary>
    /// <remarks>Source: DdsFile.cs lines 1468-1480 (GetGreyscaleFromAlpha).</remarks>
    public DdsImage GetGreyscaleFromAlpha()
    {
        var grey = new uint[_pixels.Length];
        for (int i = 0; i < grey.Length; i++)
        {
            uint a = _pixels[i] >> 24;
            grey[i] = 0xFF000000 | (a << 16) | (a << 8) | a;
        }

        return new DdsImage(Width, Height, grey);
    }

    // ------------------------------------------------------------------ resize

    /// <summary>
    /// Returns a resized copy.
    /// </summary>
    /// <remarks>Source: DdsFile.cs lines 1070-1127 and 1316-1323 (Resize/CreateImage(DdsFile,w,h)).</remarks>
    public DdsImage Resize(int width, int height)
    {
        ValidateSize(width, height);

        var colour = new uint[_pixels.Length];
        var alpha = new uint[_pixels.Length];
        for (int i = 0; i < _pixels.Length; i++)
        {
            colour[i] = _pixels[i] | 0xFF000000; // alpha stripped, as legacy does
            alpha[i] = 0xFF000000 | ((_pixels[i] >> 24) * 0x010101);
        }

        var result = new DdsImage(width, height, UseLuminance ? new uint[width * height] : Bilinear(colour, Width, Height, width, height))
        {
            UseDxt = UseDxt,
            UseLuminance = UseLuminance,
            AlphaDepth = AlphaDepth,
        };

        if (UseLuminance || AlphaDepth != 0)
            result.SetAlphaFromGreyscale(new DdsImage(width, height, Bilinear(alpha, Width, Height, width, height)));

        result.SupportsHsv = SupportsHsv;
        result.HsvShift = HsvShift;
        return result;
    }

    private static uint[] Bilinear(uint[] src, int sw, int sh, int dw, int dh)
    {
        var dst = new uint[dw * dh];
        for (int y = 0; y < dh; y++)
        {
            float fy = ((y + 0.5f) * sh / dh) - 0.5f;
            int y0 = Math.Clamp((int)MathF.Floor(fy), 0, sh - 1);
            int y1 = Math.Min(y0 + 1, sh - 1);
            float ty = Math.Clamp(fy - y0, 0f, 1f);
            for (int x = 0; x < dw; x++)
            {
                float fx = ((x + 0.5f) * sw / dw) - 0.5f;
                int x0 = Math.Clamp((int)MathF.Floor(fx), 0, sw - 1);
                int x1 = Math.Min(x0 + 1, sw - 1);
                float tx = Math.Clamp(fx - x0, 0f, 1f);

                uint result = 0;
                for (int shift = 0; shift <= 24; shift += 8)
                {
                    float top = Lerp((src[(y0 * sw) + x0] >> shift) & 0xFF, (src[(y0 * sw) + x1] >> shift) & 0xFF, tx);
                    float bottom = Lerp((src[(y1 * sw) + x0] >> shift) & 0xFF, (src[(y1 * sw) + x1] >> shift) & 0xFF, tx);
                    uint channel = (uint)Math.Clamp((int)MathF.Round(Lerp(top, bottom, ty)), 0, 255);
                    result |= channel << shift;
                }

                dst[(y * dw) + x] = result;
            }
        }

        return dst;
    }

    private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

    // ------------------------------------------------------------------ HSV

    /// <summary>
    /// Permanently applies an HSV shift to the pixels.
    /// </summary>
    /// <remarks>Source: RGBHSV.cs lines 160-171 and DdsFile.cs lines 1535-1560.</remarks>
    public void ApplyHsvShift(HsvShift shift)
    {
        if (shift.IsEmpty)
            return;

        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] = ColorHsva.FromArgb(_pixels[i]).Shift(shift).ToArgb();

        RefreshHsv();
    }

    private void RefreshHsv()
    {
        if (SupportsHsv)
            _hsv = ColorHsva.FromArgbArray(_pixels);
    }

    private static void ValidateSize(int width, int height)
    {
        if (width <= 0 || height <= 0 || width > DdsHeader.MaxDimension || height > DdsHeader.MaxDimension)
            throw new ArgumentOutOfRangeException(nameof(width), "Invalid image dimensions.");
    }
}
