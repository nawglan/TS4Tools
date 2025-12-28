using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Thumbnail image resource for Sims 4 JFIF+ALFA format.
/// </summary>
/// <remarks>
/// These are JPEG images with an optional embedded PNG alpha channel.
/// Used for visual previews of objects, CAS parts, and other game content.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ThumbnailResource.cs
///
/// Binary format:
///   0-23: Standard JFIF/JPEG header data
///   24: Check for ALFA magic (0x41464C41)
///   If ALFA present:
///     28: Alpha PNG length (big-endian!)
///     32: Alpha PNG data (N bytes)
///   Rest: JPEG image data continues
///
/// The alpha PNG's R channel provides the alpha values for the JPEG color data.
/// </remarks>
public sealed class ThumbnailResource : TypedResource
{
    /// <summary>
    /// "ALFA" magic marker indicating embedded alpha PNG.
    /// </summary>
    private const uint AlfaMagic = 0x41464C41;

    /// <summary>
    /// Offset in JFIF where ALFA marker is located.
    /// </summary>
    private const int AlfaOffset = 24;

    /// <summary>
    /// Minimum size to check for ALFA marker.
    /// </summary>
    private const int MinimumAlfaCheckSize = 32;

    private byte[] _rawData = [];
    private int _alphaPngOffset;
    private int _alphaPngLength;

    /// <summary>
    /// Gets the raw JFIF+ALFA data.
    /// </summary>
    public ReadOnlyMemory<byte> RawData => _rawData;

    /// <summary>
    /// Gets whether this thumbnail has an embedded alpha PNG.
    /// </summary>
    public bool HasEmbeddedAlpha => _alphaPngOffset > 0;

    /// <summary>
    /// Gets the offset of the alpha PNG data within RawData.
    /// Returns 0 if no alpha is embedded.
    /// </summary>
    public int AlphaPngOffset => _alphaPngOffset;

    /// <summary>
    /// Gets the length of the alpha PNG data.
    /// Returns 0 if no alpha is embedded.
    /// </summary>
    public int AlphaPngLength => _alphaPngLength;

    /// <summary>
    /// Gets the data length in bytes.
    /// </summary>
    public int DataLength => _rawData.Length;

    /// <summary>
    /// Creates a new ThumbnailResource by parsing data.
    /// </summary>
    public ThumbnailResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Source: ThumbnailResource.cs lines 47-59, 112-136
        _rawData = data.ToArray();
        _alphaPngOffset = 0;
        _alphaPngLength = 0;

        if (data.Length < MinimumAlfaCheckSize)
        {
            return; // Too short to have ALFA, treat as plain JPEG
        }

        DetectAlphaChannel(data);
    }

    /// <summary>
    /// Detects the embedded alpha channel in the JFIF data.
    /// </summary>
    /// <remarks>
    /// Source: ThumbnailResource.cs lines 119-132
    /// </remarks>
    private void DetectAlphaChannel(ReadOnlySpan<byte> data)
    {
        // Check for ALFA magic at offset 24
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(data[AlfaOffset..]);

        if (magic != AlfaMagic)
        {
            return; // No ALFA marker
        }

        // Alpha PNG length is big-endian (unusual for Sims 4 formats)
        // Source: ThumbnailResource.cs lines 122-123
        var length = BinaryPrimitives.ReadInt32BigEndian(data[(AlfaOffset + 4)..]);

        if (length <= 0 || AlfaOffset + 8 + length > data.Length)
        {
            // Invalid length, ignore alpha
            return;
        }

        _alphaPngOffset = AlfaOffset + 8;
        _alphaPngLength = length;
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Source: ThumbnailResource.cs lines 61-67
        // Return raw data unchanged - we don't modify the image format
        return _rawData;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _rawData = [];
        _alphaPngOffset = 0;
        _alphaPngLength = 0;
    }

    /// <summary>
    /// Gets the embedded alpha PNG data.
    /// Returns empty if no alpha is embedded.
    /// </summary>
    /// <returns>The alpha PNG data as a ReadOnlyMemory.</returns>
    public ReadOnlyMemory<byte> GetAlphaPngData()
    {
        if (!HasEmbeddedAlpha)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        return _rawData.AsMemory(_alphaPngOffset, _alphaPngLength);
    }

    /// <summary>
    /// Gets the raw JPEG data (the entire JFIF stream).
    /// Note: This includes the ALFA segment if present - the JPEG decoder
    /// will ignore the APP0 extension containing the alpha PNG.
    /// </summary>
    /// <returns>The raw JPEG data.</returns>
    public ReadOnlyMemory<byte> GetJpegData()
    {
        return _rawData;
    }

    /// <summary>
    /// Sets the image data from raw JFIF+ALFA bytes.
    /// </summary>
    /// <param name="data">The new image data.</param>
    public void SetImageData(ReadOnlySpan<byte> data)
    {
        _rawData = data.ToArray();
        _alphaPngOffset = 0;
        _alphaPngLength = 0;

        if (data.Length >= MinimumAlfaCheckSize)
        {
            DetectAlphaChannel(data);
        }

        OnChanged();
    }
}
