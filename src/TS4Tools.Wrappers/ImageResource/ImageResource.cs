using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Image resource for PNG and DDS/DST textures.
/// Resource Types: 0x00B00000 (PNG), 0x00B2D882 (DDS/DST).
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/DSTResource.cs
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/RLEResource.cs
///
/// Legacy implementation handles DST (shuffled DXT) and RLE compressed textures.
/// This modern implementation combines both into a single unified resource that
/// can parse and serve PNG, DDS, and DST formats.
/// </remarks>
[ResourceHandler(0x00B00000)] // PNG
[ResourceHandler(0x00B2D882)] // DDS/DST
public sealed class ImageResource : TypedResource
{
    /// <summary>
    /// PNG file signature: 0x89 'P' 'N' 'G' 0x0D 0x0A 0x1A 0x0A
    /// </summary>
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// DDS file signature: "DDS "
    /// </summary>
    private static readonly byte[] DdsSignature = [0x44, 0x44, 0x53, 0x20]; // "DDS "

    private byte[] _imageData = [];

    /// <summary>
    /// The detected image format.
    /// </summary>
    public ImageFormat Format { get; private set; } = ImageFormat.Unknown;

    /// <summary>
    /// Image width in pixels (if detected).
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Image height in pixels (if detected).
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets the raw image data.
    /// </summary>
    public ReadOnlyMemory<byte> ImageData => _imageData;

    /// <summary>
    /// Gets the data length in bytes.
    /// </summary>
    public int DataLength => _imageData.Length;

    /// <summary>
    /// Creates a new image resource by parsing data.
    /// </summary>
    public ImageResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        _imageData = data.ToArray();
        DetectFormat(data);
    }

    private void DetectFormat(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
        {
            Format = ImageFormat.Unknown;
            return;
        }

        // Check for PNG signature
        if (data.Length >= PngSignature.Length && data[..PngSignature.Length].SequenceEqual(PngSignature))
        {
            Format = ImageFormat.Png;
            ParsePngDimensions(data);
            return;
        }

        // Check for DDS signature
        if (data.Length >= DdsSignature.Length && data[..DdsSignature.Length].SequenceEqual(DdsSignature))
        {
            Format = ImageFormat.Dds;
            ParseDdsDimensions(data);
            return;
        }

        // DST files from TS4 often start with different headers
        // Check for common DST patterns
        if (Key.ResourceType == 0x00B2D882)
        {
            Format = ImageFormat.Dst;
            return;
        }

        Format = ImageFormat.Unknown;
    }

    private void ParsePngDimensions(ReadOnlySpan<byte> data)
    {
        // PNG IHDR chunk starts at offset 8 after signature
        // Structure: length (4) + "IHDR" (4) + width (4) + height (4)
        if (data.Length < 24)
            return;

        // Check for IHDR chunk type
        if (data[12] == 0x49 && data[13] == 0x48 && data[14] == 0x44 && data[15] == 0x52) // "IHDR"
        {
            // Width and height are big-endian at offsets 16 and 20
            Width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
            Height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        }
    }

    private void ParseDdsDimensions(ReadOnlySpan<byte> data)
    {
        // DDS header: magic (4) + dwSize (4) + dwFlags (4) + dwHeight (4) + dwWidth (4)
        if (data.Length < 20)
            return;

        // Height is at offset 12, Width at offset 16 (little-endian)
        Height = data[12] | (data[13] << 8) | (data[14] << 16) | (data[15] << 24);
        Width = data[16] | (data[17] << 8) | (data[18] << 16) | (data[19] << 24);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        return _imageData;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _imageData = [];
        Format = ImageFormat.Unknown;
        Width = 0;
        Height = 0;
    }

    /// <summary>
    /// Sets the image data.
    /// </summary>
    /// <param name="data">The new image data.</param>
    public void SetImageData(ReadOnlySpan<byte> data)
    {
        _imageData = data.ToArray();
        DetectFormat(data);
        OnChanged();
    }

    /// <summary>
    /// Gets the DXT compression format from the DDS header FourCC.
    /// </summary>
    public DxtFormat DxtFormat
    {
        get
        {
            if (Format != ImageFormat.Dds && Format != ImageFormat.Dst)
                return DxtFormat.Unknown;

            var fourCc = DxtDecoder.GetFourCc(_imageData);

            // Handle DST (shuffled) formats
            if (fourCc == DstDecoder.FourCcDst1) return DxtFormat.Dxt1;
            if (fourCc == DstDecoder.FourCcDst5) return DxtFormat.Dxt5;

            // Handle standard DXT formats
            if (fourCc == DxtDecoder.FourCcDxt1) return DxtFormat.Dxt1;
            if (fourCc == DxtDecoder.FourCcDxt5) return DxtFormat.Dxt5;

            return DxtFormat.Unknown;
        }
    }

    /// <summary>
    /// Decodes the image to RGBA32 pixels (4 bytes per pixel: R, G, B, A).
    /// Returns null if decoding is not supported for this format.
    /// </summary>
    /// <returns>RGBA32 pixel data, or null if unsupported.</returns>
    public byte[]? GetDecodedPixels()
    {
        return Format switch
        {
            ImageFormat.Png => null, // PNG is handled directly by Avalonia
            ImageFormat.Dds => DecodeDds(_imageData),
            ImageFormat.Dst => DecodeDst(_imageData),
            _ => null
        };
    }

    /// <summary>
    /// Decodes DDS texture data to RGBA32 pixels.
    /// </summary>
    private static byte[]? DecodeDds(ReadOnlySpan<byte> data)
    {
        return DxtDecoder.DecompressDds(data);
    }

    /// <summary>
    /// Decodes DST (shuffled) texture data to RGBA32 pixels.
    /// First unshuffles to DDS, then decompresses.
    /// </summary>
    private static byte[]? DecodeDst(ReadOnlySpan<byte> data)
    {
        // First unshuffle DST to standard DDS
        var ddsData = DstDecoder.UnshuffleToDds(data);
        if (ddsData == null)
            return null;

        // Then decompress the DDS
        return DxtDecoder.DecompressDds(ddsData);
    }
}

/// <summary>
/// DXT compression formats.
/// </summary>
public enum DxtFormat
{
    /// <summary>Unknown or unsupported format.</summary>
    Unknown,

    /// <summary>DXT1 (BC1) - 4:1 compression, 1-bit alpha.</summary>
    Dxt1,

    /// <summary>DXT5 (BC3) - 4:1 compression, interpolated alpha.</summary>
    Dxt5
}

/// <summary>
/// Supported image formats.
/// </summary>
public enum ImageFormat
{
    /// <summary>Unknown or unsupported format.</summary>
    Unknown,

    /// <summary>PNG image format.</summary>
    Png,

    /// <summary>DirectDraw Surface (DDS) format.</summary>
    Dds,

    /// <summary>Sims 4 DST texture format.</summary>
    Dst
}
