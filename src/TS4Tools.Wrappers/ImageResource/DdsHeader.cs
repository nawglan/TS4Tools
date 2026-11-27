// Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DdsFile.cs lines 28-757 (class Dds, DdsHeader, DdsPixelFormat, DdsFormat)

namespace TS4Tools.Wrappers;

/// <summary>
/// The pixel formats the legacy <c>Dds</c> class can read and write.
/// </summary>
/// <remarks>Source: DdsFile.cs lines 745-759 (enum DdsFormat).</remarks>
public enum DdsFormat
{
    /// <summary>BC1, 8 bytes per 4x4 block, 1-bit alpha.</summary>
    Dxt1,
    /// <summary>BC2, 16 bytes per 4x4 block, explicit 4-bit alpha.</summary>
    Dxt3,
    /// <summary>BC3, 16 bytes per 4x4 block, interpolated alpha.</summary>
    Dxt5,
    /// <summary>32-bit, alpha/red/green/blue.</summary>
    A8R8G8B8,
    /// <summary>32-bit, unused/red/green/blue.</summary>
    X8R8G8B8,
    /// <summary>32-bit, alpha/blue/green/red.</summary>
    A8B8G8R8,
    /// <summary>32-bit, unused/blue/green/red.</summary>
    X8B8G8R8,
    /// <summary>16-bit, 1-bit alpha and 5-bit colour.</summary>
    A1R5G5B5,
    /// <summary>16-bit, 4 bits per channel.</summary>
    A4R4G4B4,
    /// <summary>24-bit RGB.</summary>
    R8G8B8,
    /// <summary>16-bit RGB, 5-6-5.</summary>
    R5G6B5,
    /// <summary>16-bit, 8-bit alpha and 8-bit luminance.</summary>
    A8L8,
}

/// <summary>
/// Parsed/constructed DDS header including the pixel format block.
/// </summary>
/// <remarks>
/// Source: DdsFile.cs lines 30-743. The legacy code "corrects" many malformed header values
/// (printing to the console) rather than failing; the same leniency is preserved here without
/// the console output. Only the base surface is described: legacy collapses mipmap counts to 1.
/// </remarks>
internal sealed class DdsHeader
{
    private const uint Magic = 0x20534444; // "DDS "
    private const uint FourCcDxt1 = 0x31545844;
    private const uint FourCcDxt3 = 0x33545844;
    private const uint FourCcDxt5 = 0x35545844;

    // PFFlags (DdsFile.cs lines 36-49)
    private const uint PfAlphaPixels = 0x01;
    private const uint PfAlpha = 0x02;
    private const uint PfFourCc = 0x04;
    private const uint PfPaletteIndexed8 = 0x20;
    private const uint PfRgb = 0x40;
    private const uint PfYuv = 0x0200;
    private const uint PfLuminance = 0x00020000;
    private const uint PfNvidiaNormal = 0x8000000;
    private const uint PfNotFourCc = PfAlpha | PfPaletteIndexed8 | PfRgb | PfYuv | PfLuminance | PfNvidiaNormal;

    // DdsFlags (DdsFile.cs lines 219-232)
    private const uint FlagCaps = 0x01;
    private const uint FlagHeight = 0x02;
    private const uint FlagWidth = 0x04;
    private const uint FlagPitch = 0x08;
    private const uint FlagPixelFormat = 0x1000;
    private const uint FlagMipMapCount = 0x00020000;
    private const uint FlagLinearSize = 0x00080000;
    private const uint WriteFlags = FlagCaps | FlagHeight | FlagWidth | FlagPixelFormat;

    private const uint SurfaceCapsTexture = 0x1000;
    private const uint SurfaceCapsComplex = 0x08;
    private const uint SurfaceCapsMipMap = 0x00400000;

    /// <summary>Fixed size of the header including the magic number.</summary>
    internal const int FileHeaderSize = 128;

    /// <summary>Maximum accepted width or height.</summary>
    internal const int MaxDimension = 16384;

    private uint _flags;
    private uint _pitchOrLinearSize;
    private uint _fourCc;
    private uint _rgbBitCount;
    private uint _rBitMask;
    private uint _gBitMask;
    private uint _bBitMask;
    private uint _aBitMask;

    private DdsHeader()
    {
    }

    /// <summary>Image width in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Image height in pixels.</summary>
    public int Height { get; private set; }

    /// <summary>Number of mipmap levels written into the header (1 when parsed).</summary>
    public int MipMapCount { get; private set; } = 1;

    /// <summary>True when the format is DXT1/3/5.</summary>
    public bool IsBlockCompressed => _fourCc != 0;

    /// <summary>The detected file format.</summary>
    public DdsFormat Format { get; private set; }

    /// <summary>Bytes per block (8 for DXT1, 16 otherwise). Only for block-compressed formats.</summary>
    public int BlockSize => _fourCc == FourCcDxt1 ? 8 : 16;

    /// <summary>Bytes per uncompressed pixel.</summary>
    public int PixelSize => (int)(_rgbBitCount / 8);

    /// <summary>Bytes per row of the base surface (for uncompressed formats).</summary>
    public int RowPitch { get; private set; }

    /// <summary>Size in bytes of the base surface data.</summary>
    public long DataSize => IsBlockCompressed
        ? (long)Math.Max(1, (Width + 3) / 4) * BlockSize * Math.Max(1, (Height + 3) / 4)
        : (long)RowPitch * Height;

    /// <summary>
    /// Creates a header describing a new image.
    /// Source: DdsFile.cs lines 328-348 (DdsHeader(DdsFormat, width, height)) and lines 76-185 (DdsPixelFormat(DdsFormat)).
    /// </summary>
    public static DdsHeader Create(DdsFormat format, int width, int height, int mipMapCount = 1)
    {
        var h = new DdsHeader { Width = width, Height = height, Format = format, MipMapCount = Math.Max(1, mipMapCount) };

        switch (format)
        {
            case DdsFormat.Dxt1: h._fourCc = FourCcDxt1; break;
            case DdsFormat.Dxt3: h._fourCc = FourCcDxt3; break;
            case DdsFormat.Dxt5: h._fourCc = FourCcDxt5; break;
            case DdsFormat.A8L8:
                h._rgbBitCount = 16; h._aBitMask = 0x0000ff00; h._rBitMask = 0x000000ff;
                break;
            case DdsFormat.A8R8G8B8:
                h._rgbBitCount = 32; h._aBitMask = 0xff000000; h._rBitMask = 0x00ff0000; h._gBitMask = 0x0000ff00; h._bBitMask = 0x000000ff;
                break;
            case DdsFormat.X8R8G8B8:
                h._rgbBitCount = 32; h._rBitMask = 0x00ff0000; h._gBitMask = 0x0000ff00; h._bBitMask = 0x000000ff;
                break;
            case DdsFormat.A8B8G8R8:
                h._rgbBitCount = 32; h._aBitMask = 0xff000000; h._rBitMask = 0x000000ff; h._gBitMask = 0x0000ff00; h._bBitMask = 0x00ff0000;
                break;
            case DdsFormat.X8B8G8R8:
                h._rgbBitCount = 32; h._rBitMask = 0x000000ff; h._gBitMask = 0x0000ff00; h._bBitMask = 0x00ff0000;
                break;
            case DdsFormat.R8G8B8:
                h._rgbBitCount = 24; h._rBitMask = 0x00ff0000; h._gBitMask = 0x0000ff00; h._bBitMask = 0x000000ff;
                break;
            case DdsFormat.A4R4G4B4:
                h._rgbBitCount = 16; h._aBitMask = 0x0000f000; h._rBitMask = 0x00000f00; h._gBitMask = 0x000000f0; h._bBitMask = 0x0000000f;
                break;
            case DdsFormat.A1R5G5B5:
                h._rgbBitCount = 16; h._aBitMask = 0x00008000; h._rBitMask = 0x00007c00; h._gBitMask = 0x000003e0; h._bBitMask = 0x0000001f;
                break;
            case DdsFormat.R5G6B5:
                h._rgbBitCount = 16; h._rBitMask = 0x0000f800; h._gBitMask = 0x000007e0; h._bBitMask = 0x0000001f;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported DDS format.");
        }

        h.RowPitch = (int)(((width * (long)h._rgbBitCount) + 7) / 8);
        h._flags = WriteFlags | (h.IsBlockCompressed ? FlagLinearSize : FlagPitch);
        // Linear size is the compressed size (the GIMP DDS plugin convention noted at DdsFile.cs line 563).
        h._pitchOrLinearSize = h.IsBlockCompressed ? (uint)h.DataSize : (uint)h.RowPitch;
        return h;
    }

    /// <summary>
    /// Parses a DDS header. Returns false (with a reason) when the data is not a supported DDS.
    /// Source: DdsFile.cs lines 350-545 (DdsHeader(Stream)) and 187-340 (DdsPixelFormat(Stream)).
    /// </summary>
    public static bool TryParse(ReadOnlySpan<byte> data, out DdsHeader? header, out string error)
    {
        header = null;
        error = string.Empty;

        if (data.Length < FileHeaderSize)
        {
            error = "Data shorter than a DDS header.";
            return false;
        }

        if (BinaryPrimitives.ReadUInt32LittleEndian(data) != Magic)
        {
            error = "DDS magic number invalid.";
            return false;
        }

        uint size = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        if (size != 0 && size != 124)
        {
            error = $"DDS header size invalid: {size}.";
            return false;
        }

        var h = new DdsHeader();
        h._flags = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        if (System.Numerics.BitOperations.PopCount(h._flags & (WriteFlags | FlagPitch | FlagLinearSize)) != System.Numerics.BitOperations.PopCount(WriteFlags) + 1)
        {
            h._flags |= WriteFlags; // legacy "Correcting..."
        }

        uint height = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        uint width = BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
        if (height == 0 || width == 0)
        {
            error = "DDS width/height is zero.";
            return false;
        }

        if (height > MaxDimension || width > MaxDimension)
        {
            error = $"DDS dimensions {width}x{height} exceed the supported maximum of {MaxDimension}.";
            return false;
        }

        h.Height = (int)height;
        h.Width = (int)width;
        h._pitchOrLinearSize = BinaryPrimitives.ReadUInt32LittleEndian(data[20..]);
        // depth (24), mipmap count (28) and reserved (32..76) are ignored: legacy supports only simple textures.

        if (!h.ParsePixelFormat(data.Slice(76, 32), out error))
            return false;

        h.Format = h.DetermineFormat(out error);
        if (error.Length > 0)
            return false;

        h.RowPitch = h.ComputeRowPitch();
        header = h;
        return true;
    }

    /// <summary>
    /// Writes the 128-byte header (magic plus DDS_HEADER).
    /// Source: DdsFile.cs lines 547-565 (DdsHeader.Write) and 283-304 (DdsPixelFormat.Write).
    /// </summary>
    public void Write(List<byte> output)
    {
        Span<byte> buf = stackalloc byte[FileHeaderSize];
        buf.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(buf, Magic);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[4..], 124);
        uint flags = WriteFlags | (IsBlockCompressed ? FlagLinearSize : FlagPitch);
        if (MipMapCount > 1)
            flags |= FlagMipMapCount;
        BinaryPrimitives.WriteUInt32LittleEndian(buf[8..], flags);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[12..], (uint)Height);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[16..], (uint)Width);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[20..], IsBlockCompressed ? (uint)DataSize : (uint)RowPitch);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[24..], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[28..], (uint)MipMapCount);
        // reserved1 (32..76) stays zero
        BinaryPrimitives.WriteUInt32LittleEndian(buf[76..], 32);
        bool block = IsBlockCompressed;
        uint pfFlags;
        if (block)
        {
            pfFlags = PfFourCc;
        }
        else
        {
            pfFlags = 0;
            if (_aBitMask != 0)
            {
                pfFlags |= PfAlphaPixels;
                if (_aBitMask == 0x0000ff00 && _rBitMask == 0x000000ff && _gBitMask == 0 && _bBitMask == 0)
                    pfFlags |= PfLuminance;
                else
                    pfFlags |= PfRgb;
            }
            else
            {
                pfFlags |= PfRgb;
            }
        }

        BinaryPrimitives.WriteUInt32LittleEndian(buf[80..], pfFlags);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[84..], _fourCc);
        BinaryPrimitives.WriteUInt32LittleEndian(buf[88..], block ? 0 : _rgbBitCount);
        if (!block)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buf[92..], _rBitMask);
            BinaryPrimitives.WriteUInt32LittleEndian(buf[96..], _gBitMask);
            BinaryPrimitives.WriteUInt32LittleEndian(buf[100..], _bBitMask);
            BinaryPrimitives.WriteUInt32LittleEndian(buf[104..], _aBitMask);
        }

        uint caps = SurfaceCapsTexture;
        if (MipMapCount > 1)
            caps |= SurfaceCapsComplex | SurfaceCapsMipMap;
        BinaryPrimitives.WriteUInt32LittleEndian(buf[108..], caps);
        output.AddRange(buf.ToArray());
    }

    private bool ParsePixelFormat(ReadOnlySpan<byte> pf, out string error)
    {
        error = string.Empty;

        // size (offset 0) != 32 is "corrected" by legacy
        uint flags = BinaryPrimitives.ReadUInt32LittleEndian(pf[4..]);
        _fourCc = BinaryPrimitives.ReadUInt32LittleEndian(pf[8..]);
        _rgbBitCount = BinaryPrimitives.ReadUInt32LittleEndian(pf[12..]);
        _rBitMask = BinaryPrimitives.ReadUInt32LittleEndian(pf[16..]);
        _gBitMask = BinaryPrimitives.ReadUInt32LittleEndian(pf[20..]);
        _bBitMask = BinaryPrimitives.ReadUInt32LittleEndian(pf[24..]);
        _aBitMask = BinaryPrimitives.ReadUInt32LittleEndian(pf[28..]);

        if ((flags & PfFourCc) != 0)
        {
            if (!IsKnownFourCc(_fourCc))
            {
                error = $"Unsupported DDS FourCC 0x{_fourCc:X8}; only DXT1/3/5 supported.";
                return false;
            }

            flags &= ~PfNotFourCc;
            _rgbBitCount = 0;
        }
        else if (IsKnownFourCc(_fourCc))
        {
            // "Supported FourCC but Flags.FourCC not set. Overruling flags."
            flags |= PfFourCc;
            flags &= ~PfNotFourCc;
            _rgbBitCount = 0;
        }
        else
        {
            _fourCc = 0;
        }

        if (_fourCc == 0)
        {
            if (System.Numerics.BitOperations.PopCount(flags & (PfAlphaPixels | PfAlpha | PfRgb)) == 3)
                flags &= ~PfAlpha;
            if (System.Numerics.BitOperations.PopCount(flags & (PfAlpha | PfRgb)) == 2)
            {
                flags &= ~PfAlpha;
                flags |= PfAlphaPixels;
            }

            if (System.Numerics.BitOperations.PopCount(flags & PfNotFourCc) > 1)
            {
                error = "Unsupported DDS pixel format flags (more than one format flag set).";
                return false;
            }

            if ((flags & PfAlpha) != 0) { error = "Alpha-only format data is not supported."; return false; }
            if ((flags & PfPaletteIndexed8) != 0) { error = "Palette indexed format data is not supported."; return false; }
            if ((flags & PfYuv) != 0) { error = "YUV format data is not supported."; return false; }
            if ((flags & PfNvidiaNormal) != 0) { error = "nVidia normal format data is not supported."; return false; }

            if ((flags & PfLuminance) != 0
                && (_aBitMask != 0x0000ff00 || _rBitMask != 0x000000ff || _gBitMask != 0 || _bBitMask != 0))
            {
                error = "Only A8L8 luminance data is supported.";
                return false;
            }

            if (System.Numerics.BitOperations.PopCount(flags & PfNotFourCc) == 0)
                flags |= PfRgb; // "Uncompressed texture but no format indicated. Defaulting to RGB."

            if ((flags & PfAlphaPixels) != 0 && _aBitMask == 0)
                flags &= ~PfAlphaPixels;

            if ((flags & PfRgb) != 0 && _rgbBitCount == 0)
            {
                _rgbBitCount = (uint)(System.Numerics.BitOperations.PopCount(_aBitMask) + System.Numerics.BitOperations.PopCount(_rBitMask)
                    + System.Numerics.BitOperations.PopCount(_gBitMask) + System.Numerics.BitOperations.PopCount(_bBitMask));
            }

            if ((flags & PfRgb) != 0 && _rgbBitCount == 0)
            {
                error = "Invalid uncompressed header: RGB bit count is zero and no masks set.";
                return false;
            }

            if ((flags & PfRgb) != 0 && (_rBitMask == 0 || _gBitMask == 0 || _bBitMask == 0))
            {
                error = "RGB is supported only where data is present for all channels.";
                return false;
            }
        }

        return true;
    }

    private static bool IsKnownFourCc(uint fourCc) =>
        fourCc == FourCcDxt1 || fourCc == FourCcDxt3 || fourCc == FourCcDxt5;

    private DdsFormat DetermineFormat(out string error)
    {
        error = string.Empty;

        if (_fourCc == FourCcDxt1) return DdsFormat.Dxt1;
        if (_fourCc == FourCcDxt3) return DdsFormat.Dxt3;
        if (_fourCc == FourCcDxt5) return DdsFormat.Dxt5;

        if (_aBitMask == 0x0000ff00 && _rBitMask == 0x000000ff && _gBitMask == 0 && _bBitMask == 0)
            return DdsFormat.A8L8;

        if (_rgbBitCount == 32)
        {
            if (_rBitMask == 0x00ff0000 && _gBitMask == 0x0000ff00 && _bBitMask == 0x000000ff)
                return _aBitMask == 0xff000000 ? DdsFormat.A8R8G8B8 : DdsFormat.X8R8G8B8;
            if (_rBitMask == 0x000000ff && _gBitMask == 0x0000ff00 && _bBitMask == 0x00ff0000)
                return _aBitMask == 0xff000000 ? DdsFormat.A8B8G8R8 : DdsFormat.X8B8G8R8;
        }
        else if (_rgbBitCount == 24)
        {
            if (_rBitMask == 0x00ff0000 && _gBitMask == 0x0000ff00 && _bBitMask == 0x000000ff && _aBitMask == 0)
                return DdsFormat.R8G8B8;
        }
        else if (_rgbBitCount == 16)
        {
            if (_rBitMask == 0x00000f00 && _gBitMask == 0x000000f0 && _bBitMask == 0x0000000f && _aBitMask == 0x0000f000)
                return DdsFormat.A4R4G4B4;
            if (_rBitMask == 0x00007c00 && _gBitMask == 0x000003e0 && _bBitMask == 0x0000001f && _aBitMask == 0x00008000)
                return DdsFormat.A1R5G5B5;
            if (_rBitMask == 0x0000f800 && _gBitMask == 0x000007e0 && _bBitMask == 0x0000001f && _aBitMask == 0)
                return DdsFormat.R5G6B5;
        }

        error = $"Uncompressed image with unsupported format: {_rgbBitCount} bits, mask {_aBitMask | _rBitMask | _gBitMask | _bBitMask:X8}.";
        return DdsFormat.A8R8G8B8;
    }

    private int ComputeRowPitch()
    {
        if (IsBlockCompressed)
            return 0;

        // Source: DdsFile.cs lines 595-615 trusts the pitch the encoder supplied. Some encoders
        // set it wrongly (legacy comments "some encoders behave badly"), so only honour it when it is
        // at least the minimum row size; otherwise compute the pitch from the width.
        long minPitch = ((Width * (long)_rgbBitCount) + 7) / 8;
        if ((_flags & FlagPitch) != 0 && _pitchOrLinearSize >= minPitch && _pitchOrLinearSize <= minPitch + 4096)
            return (int)_pitchOrLinearSize;
        return (int)minPitch;
    }

    // ---- pixel conversion: DDS pixel (little-endian bytes as a uint) <-> packed ARGB ----
    // Source: DdsFile.cs lines 396-473 (fromDDS_*) and 480-528 (toDDS_*)

    /// <summary>Converts one stored pixel value to packed ARGB.</summary>
    public uint ToArgb(uint pixel)
    {
        switch (Format)
        {
            case DdsFormat.A8R8G8B8: return pixel;
            case DdsFormat.X8R8G8B8: return 0xFF000000 | pixel;
            case DdsFormat.A8B8G8R8:
            case DdsFormat.Dxt1:
            case DdsFormat.Dxt3:
            case DdsFormat.Dxt5:
                return (pixel & 0xFF00FF00) | ((pixel & 0x00FF0000) >> 16) | ((pixel & 0x000000FF) << 16);
            case DdsFormat.X8B8G8R8:
                return 0xFF000000 | ((pixel & 0x00FF0000) >> 16) | ((pixel & 0x000000FF) << 16) | (pixel & 0x0000FF00);
            case DdsFormat.A1R5G5B5:
            {
                // Legacy (lines 413-422) has two bugs here: alpha is left as 0/1 rather than 0/255 and the
                // red/green shifts bind wrongly (operator precedence). Implemented as clearly intended.
                uint a = ((pixel >> 15) & 1) != 0 ? 0xFFu : 0u;
                uint r = (pixel >> 10) & 0x1f;
                uint g = (pixel >> 5) & 0x1f;
                uint b = pixel & 0x1f;
                return (a << 24) | (((r << 3) | (r >> 2)) << 16) | (((g << 3) | (g >> 2)) << 8) | ((b << 3) | (b >> 2));
            }

            case DdsFormat.A4R4G4B4:
            {
                uint a = (pixel >> 12) & 0x0f;
                uint r = (pixel >> 8) & 0x0f;
                uint g = (pixel >> 4) & 0x0f;
                uint b = pixel & 0x0f;
                return (((a << 4) | a) << 24) | (((r << 4) | r) << 16) | (((g << 4) | g) << 8) | ((b << 4) | b);
            }

            case DdsFormat.R8G8B8: return 0xFF000000 | pixel;
            case DdsFormat.R5G6B5:
            {
                uint r = (pixel >> 11) & 0x1f;
                uint g = (pixel >> 5) & 0x3f;
                uint b = pixel & 0x1f;
                return 0xFF000000 | (((r << 3) | (r >> 2)) << 16) | (((g << 2) | (g >> 4)) << 8) | ((b << 3) | (b >> 2));
            }

            case DdsFormat.A8L8:
            {
                uint a = (pixel & 0x0000FF00) >> 8;
                uint l = pixel & 0x000000FF;
                return (a << 24) | (l << 16) | (l << 8) | l;
            }

            default:
                throw new InvalidOperationException("Unsupported DDS format.");
        }
    }

    /// <summary>Converts packed ARGB to one stored pixel value.</summary>
    public uint FromArgb(uint argb)
    {
        switch (Format)
        {
            case DdsFormat.A8R8G8B8: return argb;
            case DdsFormat.X8R8G8B8: return argb & 0x00FFFFFF;
            case DdsFormat.A8B8G8R8:
            case DdsFormat.Dxt1:
            case DdsFormat.Dxt3:
            case DdsFormat.Dxt5:
                return (argb & 0xFF00FF00) | ((argb & 0x00FF0000) >> 16) | ((argb & 0x000000FF) << 16);
            case DdsFormat.X8B8G8R8:
                return (argb & 0x0000FF00) | ((argb & 0x00FF0000) >> 16) | ((argb & 0x000000FF) << 16);
            case DdsFormat.A1R5G5B5:
            {
                uint a = (argb & 0xFF000000) == 0 ? 0u : 1u << 15;
                uint r = ((argb & 0x00F80000) >> 3) >> 6;
                uint g = ((argb & 0x0000F800) >> 3) >> 3;
                uint b = (argb & 0x000000F8) >> 3;
                return a | r | g | b;
            }

            case DdsFormat.A4R4G4B4:
            {
                uint a = ((argb & 0xF0000000) >> 4) >> 12;
                uint r = ((argb & 0x00F00000) >> 4) >> 8;
                uint g = ((argb & 0x0000F000) >> 4) >> 4;
                uint b = (argb & 0x000000F0) >> 4;
                return a | r | g | b;
            }

            case DdsFormat.R8G8B8: return argb & 0x00FFFFFF;
            case DdsFormat.R5G6B5:
            {
                uint r = ((argb & 0x00F80000) >> 3) >> 5;
                uint g = ((argb & 0x0000FC00) >> 2) >> 3;
                uint b = (argb & 0x000000F8) >> 3;
                return r | g | b;
            }

            case DdsFormat.A8L8:
            {
                uint l = (((argb >> 16) & 0xFF) + ((argb >> 8) & 0xFF) + (argb & 0xFF)) / 3;
                return ((argb >> 24) << 8) | (l & 0xFF);
            }

            default:
                throw new InvalidOperationException("Unsupported DDS format.");
        }
    }
}
