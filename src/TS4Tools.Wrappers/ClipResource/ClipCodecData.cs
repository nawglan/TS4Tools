// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/S3CLIP.cs lines 28-142

namespace TS4Tools.Wrappers;

/// <summary>
/// S3CLIP codec data containing animation channel information.
/// </summary>
/// <remarks>
/// Source: S3CLIP.cs lines 28-230 (Parse lines 55-85, UnParse lines 87-142).
/// Channels, frames and the F1 palette are fully parsed. If re-serialising the parsed structure would not
/// reproduce the original bytes exactly (or the data is malformed), the raw bytes are kept and written back
/// verbatim so round-trips stay byte-identical; <see cref="HasStructuredData"/> reports which mode is active.
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

    /// <summary>Animation channels (tracks). Empty unless <see cref="HasStructuredData"/>.</summary>
    public List<ClipChannel> Channels { get; } = [];

    /// <summary>F1 palette values. Empty unless <see cref="HasStructuredData"/>.</summary>
    public List<float> F1Palette { get; } = [];

    /// <summary>
    /// True when channels/palette were parsed and re-serialise byte-identically; false when raw bytes are preserved instead.
    /// </summary>
    public bool HasStructuredData { get; private set; }

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

            // Full structure; only trusted if it re-serialises to exactly the original bytes. Real clips pad 8-bit
            // index runs either to 2 bytes or (as legacy does) to 4 bytes, so both layouts are tried.
            foreach (bool legacyPadding in new[] { false, true })
            {
                codec._legacyFramePadding = legacyPadding;
                try
                {
                    codec.ParseStructure(data[..length], channelDataOffset, f1PaletteDataOffset);
                    if (codec.SerializeStructure().AsSpan().SequenceEqual(data[..length]))
                    {
                        codec.HasStructuredData = true;
                        break;
                    }
                }
                catch (Exception ex) when (ex is InvalidDataException or ArgumentException or InvalidOperationException
                                               or IndexOutOfRangeException or OverflowException)
                {
                    // try the next layout
                }

                codec.ResetStructure();
            }
        }
        catch
        {
            codec.IsValid = false;
        }

        return codec;
    }

    private bool _legacyFramePadding;

    private void ResetStructure()
    {
        Channels.Clear();
        F1Palette.Clear();
        HasStructuredData = false;
    }

    /// <summary>Upper bound on frames per channel (the count field is a ushort).</summary>
    private const int MaxFramesPerChannel = ushort.MaxValue;

    /// <summary>
    /// Parses the F1 palette and channel table/frames. Source: S3CLIP.cs lines 77-84, 233-272, 335-374, 444-466, 518-589, 591-690.
    /// </summary>
    private void ParseStructure(ReadOnlySpan<byte> data, uint channelDataOffset, uint f1PaletteDataOffset)
    {
        if (channelDataOffset > data.Length || f1PaletteDataOffset > data.Length)
            throw new InvalidDataException("Offset beyond codec data.");

        Channels.Clear();
        F1Palette.Clear();

        // Palette: one float per entry (Source: S3CLIP.cs lines 244-249, 311-315)
        if ((ulong)F1PaletteSize * 4 > (ulong)(data.Length - (int)f1PaletteDataOffset))
            throw new InvalidDataException($"Invalid F1 palette size: {F1PaletteSize}");
        int pos = (int)f1PaletteDataOffset;
        for (uint i = 0; i < F1PaletteSize; i++, pos += 4)
            F1Palette.Add(BinaryPrimitives.ReadSingleLittleEndian(data[pos..]));

        // Channel table: 20-byte entries (Source: S3CLIP.cs lines 447-466)
        if ((ulong)ChannelCount * ClipChannel.EntrySize > (ulong)(data.Length - (int)channelDataOffset))
            throw new InvalidDataException($"Invalid channel count: {ChannelCount}");
        pos = (int)channelDataOffset;
        for (uint i = 0; i < ChannelCount; i++, pos += ClipChannel.EntrySize)
        {
            var e = data[pos..];
            uint dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(e);
            var channel = new ClipChannel
            {
                Target = BinaryPrimitives.ReadUInt32LittleEndian(e[4..]),
                Offset = BinaryPrimitives.ReadSingleLittleEndian(e[8..]),
                Scale = BinaryPrimitives.ReadSingleLittleEndian(e[12..]),
                ChannelType = (ClipChannelType)e[18],
                SubTarget = (ClipSubTargetType)e[19],
            };
            ushort numFrames = BinaryPrimitives.ReadUInt16LittleEndian(e[16..]);

            uint width = ClipChannel.GetWidth(channel.ChannelType);
            uint count = ClipChannel.GetCount(channel.ChannelType);
            if (numFrames > MaxFramesPerChannel || dataOffset > data.Length)
                throw new InvalidDataException("Invalid channel frame data.");
            ParseFrames(data, (int)dataOffset, numFrames, width, count, channel.Frames, _legacyFramePadding);
            Channels.Add(channel);
        }
    }

    /// <summary>Source: S3CLIP.Frame.Parse, S3CLIP.cs lines 636-690.</summary>
    private static void ParseFrames(ReadOnlySpan<byte> data, int pos, int numFrames, uint width, uint count, List<ClipFrame> frames, bool legacyPadding)
    {
        for (int f = 0; f < numFrames; f++)
        {
            if (data.Length - pos < 4)
                throw new InvalidDataException("Frame data truncated.");
            var frame = new ClipFrame
            {
                StartTick = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]),
                Flags = BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + 2)..]),
            };
            pos += 4;
            switch (width)
            {
                case 1:
                    // Legacy reads count bytes + (4 - count) padding (S3CLIP.cs lines 650-657). Real Sims 4 clips pad the
                    // index bytes to 2-byte alignment instead (verified across the whole real-data corpus), so follow the data.
                    int byteBytes = legacyPadding ? 4 : ((int)count + 1) & ~1;
                    if (data.Length - pos < byteBytes)
                        throw new InvalidDataException("Frame data truncated.");
                    frame.Indices = new ushort[count];
                    for (int i = 0; i < count; i++)
                        frame.Indices[i] = data[pos + i];
                    if (!legacyPadding && byteBytes != count)
                        frame.Padding = data[pos + (int)count];
                    pos += byteBytes;
                    break;
                case 2:
                    // Legacy adds 2 padding bytes for odd counts (S3CLIP.cs line 666); real data has none.
                    int bytes = (int)count * 2;
                    if (data.Length - pos < bytes)
                        throw new InvalidDataException("Frame data truncated.");
                    frame.Indices = new ushort[count];
                    for (int i = 0; i < count; i++)
                        frame.Indices[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + i * 2)..]);
                    pos += bytes;
                    break;
                case 10:
                    if (data.Length - pos < 4)
                        throw new InvalidDataException("Frame data truncated.");
                    uint packed = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
                    frame.Indices =
                    [
                        (ushort)((packed << 22) >> 22),
                        (ushort)((packed << 12) >> 22),
                        (ushort)((packed << 2) >> 22),
                    ];
                    pos += 4;
                    break;
                default:
                    frame.Indices = [];
                    break;
            }
            frames.Add(frame);
        }
    }

    /// <summary>
    /// Serialises the structured data in legacy layout: header, channel table, names, palette, frame data.
    /// Source: S3CLIP.UnParse, S3CLIP.cs lines 87-142; S3Channel.UnParse/UpdateOffset/UnParseFrames, lines 468-501.
    /// </summary>
    private byte[] SerializeStructure()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        var token = new byte[8];
        Encoding.ASCII.GetBytes(FormatToken.AsSpan(0, Math.Min(8, FormatToken.Length)), token);
        w.Write(token);
        w.Write(Version);
        w.Write(Flags);
        w.Write(TickLength);
        w.Write(NumTicks);
        w.Write(Padding);
        w.Write((uint)Channels.Count);
        w.Write((uint)F1Palette.Count);
        long offsetsPos = ms.Position;
        w.Write(0U);
        w.Write(0U);
        w.Write(0U);
        w.Write(0U);

        long channelPos = ms.Position;
        ms.Position += (long)Channels.Count * ClipChannel.EntrySize;

        long namePos = ms.Position;
        w.Write(Encoding.UTF8.GetBytes(ClipName));
        w.Write((byte)0);

        long sourcePos = ms.Position;
        w.Write(Encoding.UTF8.GetBytes(SourceAssetName));
        w.Write((byte)0);

        long palettePos = ms.Position;
        foreach (float p in F1Palette)
            w.Write(p);

        var dataOffsets = new long[Channels.Count];
        for (int c = 0; c < Channels.Count; c++)
        {
            var ch = Channels[c];
            dataOffsets[c] = ms.Position;
            uint width = ClipChannel.GetWidth(ch.ChannelType);
            foreach (var frame in ch.Frames)
                WriteFrame(w, frame, width, _legacyFramePadding);
        }
        long end = ms.Position;

        ms.Position = channelPos;
        for (int c = 0; c < Channels.Count; c++)
        {
            var ch = Channels[c];
            w.Write((uint)dataOffsets[c]);
            w.Write(ch.Target);
            w.Write(ch.Offset);
            w.Write(ch.Scale);
            w.Write((ushort)ch.Frames.Count);
            w.Write((byte)ch.ChannelType);
            w.Write((byte)ch.SubTarget);
        }

        ms.Position = offsetsPos;
        w.Write((uint)channelPos);
        w.Write((uint)palettePos);
        w.Write((uint)namePos);
        w.Write((uint)sourcePos);

        ms.Position = end;
        return ms.ToArray();
    }

    /// <summary>Source: S3CLIP.Frame.UnParse, S3CLIP.cs lines 692-730.</summary>
    private static void WriteFrame(BinaryWriter w, ClipFrame frame, uint width, bool legacyPadding)
    {
        w.Write(frame.StartTick);
        w.Write(frame.Flags);
        var idx = frame.Indices;
        switch (width)
        {
            case 1:
                if (idx.Length > 4)
                    throw new InvalidOperationException("Too many indices for 8-bit frame.");
                foreach (ushort i in idx)
                    w.Write((byte)i);
                if (legacyPadding)
                {
                    for (int i = 0; i < 4 - idx.Length; i++)
                        w.Write((byte)0);
                }
                else if (idx.Length % 2 != 0)
                {
                    w.Write(frame.Padding);
                }
                break;
            case 2:
                foreach (ushort i in idx)
                    w.Write(i);
                break;
            case 10:
                if (idx.Length < 3)
                    throw new InvalidOperationException("Packed frame needs 3 indices.");
                w.Write(((uint)idx[2] << 20) + ((uint)idx[1] << 10) + idx[0]);
                break;
        }
    }

    /// <summary>
    /// Writes codec data to a stream.
    /// </summary>
    /// <remarks>
    /// Writes the parsed structure when it round-trips exactly, otherwise the preserved raw data.
    /// </remarks>
    internal void Write(BinaryWriter writer)
    {
        if (HasStructuredData)
        {
            writer.Write(SerializeStructure());
        }
        else if (_rawData.Length > 0)
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
