// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/S3CLIP.cs lines 28-142

namespace TS4Tools.Wrappers;

/// <summary>
/// S3CLIP codec data containing animation channel information.
/// </summary>
/// <remarks>
/// Source: S3CLIP.cs lines 28-200+
/// This implementation parses the header and preserves raw data for round-trip compatibility.
/// Full channel/track parsing can be added later if needed.
/// </remarks>
public sealed class ClipCodecData
{
    private byte[] _rawData = [];

    /// <summary>Format token (8 characters, typically "_pilc3S_" = "S3CLIP" reversed).</summary>
    public string FormatToken { get; set; } = "_pilc3S_";

    /// <summary>Codec version.</summary>
    public uint Version { get; set; }

    /// <summary>Codec flags.</summary>
    public uint Flags { get; set; }

    /// <summary>Length of a single tick in seconds.</summary>
    public float TickLength { get; set; }

    /// <summary>Number of ticks in the animation.</summary>
    public ushort NumTicks { get; set; }

    /// <summary>Padding bytes.</summary>
    public ushort Padding { get; set; }

    /// <summary>Number of channels.</summary>
    public uint ChannelCount { get; private set; }

    /// <summary>Size of the F1 palette.</summary>
    public uint F1PaletteSize { get; private set; }

    /// <summary>Clip name.</summary>
    public string ClipName { get; set; } = string.Empty;

    /// <summary>Source asset name.</summary>
    public string SourceAssetName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the codec data was successfully parsed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Raw codec data for round-trip serialization.
    /// </summary>
    public ReadOnlyMemory<byte> RawData => _rawData;

    /// <summary>
    /// Total duration in seconds.
    /// </summary>
    public float Duration => TickLength * NumTicks;

    /// <summary>
    /// Creates empty codec data.
    /// </summary>
    public ClipCodecData()
    {
    }

    /// <summary>
    /// Parses codec data from binary data.
    /// </summary>
    internal static ClipCodecData Parse(ReadOnlySpan<byte> data, int length)
    {
        var codec = new ClipCodecData();

        if (length <= 0)
            return codec;

        // Preserve raw data for round-trip
        codec._rawData = data[..length].ToArray();

        // Header size is 48 bytes minimum
        if (length < 48)
        {
            codec.IsValid = false;
            return codec;
        }

        try
        {
            int offset = 0;

            // Format token (8 bytes)
            codec.FormatToken = Encoding.ASCII.GetString(data.Slice(offset, 8));
            offset += 8;

            codec.Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            codec.Flags = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            codec.TickLength = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;

            codec.NumTicks = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;

            codec.Padding = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;

            codec.ChannelCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            codec.F1PaletteSize = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            uint channelDataOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            uint f1PaletteDataOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            uint nameOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            uint sourceAssetNameOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            // Read null-terminated strings from their offsets
            if (nameOffset < length)
                codec.ClipName = ReadNullTerminatedString(data, (int)nameOffset);

            if (sourceAssetNameOffset < length)
                codec.SourceAssetName = ReadNullTerminatedString(data, (int)sourceAssetNameOffset);

            codec.IsValid = true;
        }
        catch
        {
            codec.IsValid = false;
        }

        return codec;
    }

    /// <summary>
    /// Writes codec data to a stream.
    /// </summary>
    /// <remarks>
    /// Uses preserved raw data for round-trip compatibility.
    /// </remarks>
    internal void Write(BinaryWriter writer)
    {
        // For round-trip compatibility, write the preserved raw data
        if (_rawData.Length > 0)
        {
            writer.Write(_rawData);
        }
    }

    private static string ReadNullTerminatedString(ReadOnlySpan<byte> data, int offset)
    {
        if (offset >= data.Length)
            return string.Empty;

        int end = offset;
        while (end < data.Length && data[end] != 0)
            end++;

        return Encoding.UTF8.GetString(data.Slice(offset, end - offset));
    }
}
