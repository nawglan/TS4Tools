namespace TS4Tools.Resources.Images;

/// <summary>
/// Enumeration of DDS FourCC compression formats used in The Sims 4.
/// </summary>
public enum DdsFourCC : uint
{
    /// <summary>
    /// No FourCC compression.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// DXT1 compression (BC1) - 1-bit alpha, 4 bits per pixel.
    /// </summary>
    DXT1 = 0x31545844,

    /// <summary>
    /// DXT3 compression (BC2) - Explicit alpha, 8 bits per pixel.
    /// </summary>
    DXT3 = 0x33545844,

    /// <summary>
    /// DXT5 compression (BC3) - Interpolated alpha, 8 bits per pixel.
    /// </summary>
    DXT5 = 0x35545844,

    /// <summary>
    /// DST1 compression - Custom Sims format variant.
    /// </summary>
    DST1 = 0x31545344,

    /// <summary>
    /// DST3 compression - Custom Sims format variant.
    /// </summary>
    DST3 = 0x33545344,

    /// <summary>
    /// DST5 compression - Custom Sims format variant.
    /// </summary>
    DST5 = 0x35545344,

    /// <summary>
    /// ATI1 compression (BC4) - Single channel compression.
    /// </summary>
    ATI1 = 0x31495441,

    /// <summary>
    /// ATI2 compression (BC5) - Two channel compression.
    /// </summary>
    ATI2 = 0x32495441
}

/// <summary>
/// DDS pixel format flags indicating the type of data stored.
/// </summary>
[Flags]
public enum DdsPixelFormatFlags : uint
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// Texture contains alpha data; alphaBitMask contains valid data.
    /// </summary>
    AlphaPixels = 0x00000001,

    /// <summary>
    /// Used in some older DDS files for alpha channel only uncompressed data.
    /// </summary>
    Alpha = 0x00000002,

    /// <summary>
    /// Texture contains compressed RGB data; fourCC contains valid data.
    /// </summary>
    FourCC = 0x00000004,

    /// <summary>
    /// Texture contains uncompressed RGB data.
    /// </summary>
    RGB = 0x00000040,

    /// <summary>
    /// Texture contains uncompressed RGBA data.
    /// </summary>
    RGBA = RGB | AlphaPixels,

    /// <summary>
    /// Used in some DDS files for single channel color uncompressed data.
    /// </summary>
    Luminance = 0x00020000,

    /// <summary>
    /// Used in some DDS files for dual channel uncompressed data.
    /// </summary>
    LuminanceAlpha = Luminance | AlphaPixels
}

/// <summary>
/// Represents the pixel format information in a DDS header.
/// </summary>
public readonly record struct DdsPixelFormat
{
    /// <summary>
    /// Structure size in bytes. Always 32.
    /// </summary>
    public uint Size { get; init; } = 32;

    /// <summary>
    /// Values which indicate what type of data is in the surface.
    /// </summary>
    public DdsPixelFormatFlags Flags { get; init; }

    /// <summary>
    /// Four-character code for specifying compressed or custom format.
    /// </summary>
    public DdsFourCC FourCC { get; init; }

    /// <summary>
    /// Number of bits in an RGB (possibly including alpha) format.
    /// </summary>
    public uint RgbBitCount { get; init; }

    /// <summary>
    /// Red (or lumiannce or Y) mask for reading color data.
    /// </summary>
    public uint RedBitMask { get; init; }

    /// <summary>
    /// Green (or U) mask for reading color data.
    /// </summary>
    public uint GreenBitMask { get; init; }

    /// <summary>
    /// Blue (or V) mask for reading color data.
    /// </summary>
    public uint BlueBitMask { get; init; }

    /// <summary>
    /// Alpha mask for reading alpha data.
    /// </summary>
    public uint AlphaBitMask { get; init; }

    /// <summary>
    /// Initializes a new instance of the DdsPixelFormat struct.
    /// </summary>
    public DdsPixelFormat()
    {
        Size = 32;
        Flags = DdsPixelFormatFlags.None;
        FourCC = DdsFourCC.None;
        RgbBitCount = 0;
        RedBitMask = 0;
        GreenBitMask = 0;
        BlueBitMask = 0;
        AlphaBitMask = 0;
    }

    /// <summary>
    /// Creates a DDS pixel format for the specified FourCC compression format.
    /// </summary>
    /// <param name="fourCC">The compression format.</param>
    /// <returns>A pixel format configured for the specified compression.</returns>
    public static DdsPixelFormat CreateForFourCC(DdsFourCC fourCC) => new()
    {
        Flags = DdsPixelFormatFlags.FourCC,
        FourCC = fourCC,
        RgbBitCount = 0
    };

    /// <summary>
    /// Creates a DDS pixel format for uncompressed RGBA data.
    /// </summary>
    /// <returns>A pixel format configured for 32-bit RGBA data.</returns>
    public static DdsPixelFormat CreateForRGBA32() => new()
    {
        Flags = DdsPixelFormatFlags.RGBA,
        FourCC = DdsFourCC.None,
        RgbBitCount = 32,
        RedBitMask = 0x00FF0000,
        GreenBitMask = 0x0000FF00,
        BlueBitMask = 0x000000FF,
        AlphaBitMask = 0xFF000000
    };

    /// <summary>
    /// Creates a DDS pixel format for uncompressed RGB data.
    /// </summary>
    /// <returns>A pixel format configured for 24-bit RGB data.</returns>
    public static DdsPixelFormat CreateForRGB24() => new()
    {
        Flags = DdsPixelFormatFlags.RGB,
        FourCC = DdsFourCC.None,
        RgbBitCount = 24,
        RedBitMask = 0x00FF0000,
        GreenBitMask = 0x0000FF00,
        BlueBitMask = 0x000000FF,
        AlphaBitMask = 0x00000000
    };

    /// <summary>
    /// Creates a DDS pixel format for single-channel luminance data.
    /// </summary>
    /// <returns>A pixel format configured for 8-bit luminance data.</returns>
    public static DdsPixelFormat CreateForLuminance8() => new()
    {
        Flags = DdsPixelFormatFlags.Luminance,
        FourCC = DdsFourCC.None,
        RgbBitCount = 8,
        RedBitMask = 0x000000FF,
        GreenBitMask = 0x00000000,
        BlueBitMask = 0x00000000,
        AlphaBitMask = 0x00000000
    };
}

/// <summary>
/// DDS surface description flags.
/// </summary>
[Flags]
public enum DdsFlags : uint
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// Required in every .dds file.
    /// </summary>
    Caps = 0x00000001,

    /// <summary>
    /// Required in every .dds file.
    /// </summary>
    Height = 0x00000002,

    /// <summary>
    /// Required in every .dds file.
    /// </summary>
    Width = 0x00000004,

    /// <summary>
    /// Required when pitch is provided for an uncompressed texture.
    /// </summary>
    Pitch = 0x00000008,

    /// <summary>
    /// Required in every .dds file.
    /// </summary>
    PixelFormat = 0x00001000,

    /// <summary>
    /// Required in a mipmapped texture.
    /// </summary>
    MipMapCount = 0x00020000,

    /// <summary>
    /// Required when pitch is provided for a compressed texture.
    /// </summary>
    LinearSize = 0x00080000,

    /// <summary>
    /// Required in a depth texture.
    /// </summary>
    Depth = 0x00800000
}

/// <summary>
/// DDS surface capability flags.
/// </summary>
[Flags]
public enum DdsCaps : uint
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// Optional; must be used on any file that contains more than one surface.
    /// </summary>
    Complex = 0x00000008,

    /// <summary>
    /// Required in every .dds file.
    /// </summary>
    Texture = 0x00001000,

    /// <summary>
    /// Optional; should be used for a mipmap.
    /// </summary>
    MipMap = 0x00400000
}

/// <summary>
/// Additional DDS capability flags.
/// </summary>
[Flags]
public enum DdsCaps2 : uint
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// Required for a cube map.
    /// </summary>
    Cubemap = 0x00000200,

    /// <summary>
    /// Required when these surfaces are stored in a cube map.
    /// </summary>
    CubemapPositiveX = 0x00000400,

    /// <summary>
    /// Required when these surfaces are stored in a cube map.
    /// </summary>
    CubemapNegativeX = 0x00000800,

    /// <summary>
    /// Required when these surfaces are stored in a cube map.
    /// </summary>
    CubemapPositiveY = 0x00001000,

    /// <summary>
    /// Required when these surfaces are stored in a cube map.
    /// </summary>
    CubemapNegativeY = 0x00002000,

    /// <summary>
    /// Required when these surfaces are stored in a cube map.
    /// </summary>
    CubemapPositiveZ = 0x00004000,

    /// <summary>
    /// Required when these surfaces are stored in a cube map.
    /// </summary>
    CubemapNegativeZ = 0x00008000,

    /// <summary>
    /// Required for a volume texture.
    /// </summary>
    Volume = 0x00200000
}
