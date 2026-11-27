using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// A TXTC content block: version, optional super blocks (whose fabric is itself a content block),
/// pattern size, data type, entries and TGI blocks. The resource root and every super block fabric use this layout.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/TxtcResource/TxtcResource.cs ContentType, lines 77-190
/// (Parse lines 118-146, UnParse lines 148-176) and SuperBlock lines 281-330.
/// </remarks>
public sealed class TxtcContent
{
    // Legacy SuperBlockList is capped at byte.MaxValue entries; nesting depth is bounded to guard against hostile data.
    private const int MaxDepth = 16;

    /// <summary>Format version.</summary>
    public uint Version { get; set; } = 8;

    /// <summary>Pattern size type.</summary>
    public TxtcPatternSize PatternSize { get; set; } = TxtcPatternSize.Default;

    /// <summary>Data type flags.</summary>
    public TxtcDataType DataType { get; set; } = TxtcDataType.Body;

    /// <summary>Unknown byte field.</summary>
    public byte Unknown3 { get; set; }

    /// <summary>Unknown byte field (version 8+).</summary>
    public byte Unknown4 { get; set; }

    /// <summary>Super blocks (version 7+).</summary>
    public List<TxtcSuperBlock> SuperBlocks { get; } = [];

    /// <summary>Entry blocks.</summary>
    public List<TxtcEntryBlock> Entries { get; } = [];

    /// <summary>TGI blocks.</summary>
    public List<ResourceKey> TgiBlocks { get; } = [];

    /// <summary>
    /// Parses a content block. When <paramref name="nested"/> the TGI block must be present and exactly follow the entries
    /// (legacy strict checks); the root stays lenient as before.
    /// </summary>
    internal static TxtcContent Parse(ReadOnlySpan<byte> data, bool nested, int depth, out int bytesRead)
    {
        if (depth > MaxDepth)
            throw new ResourceFormatException("TXTC super block nesting too deep.");
        if (data.Length < 18)
            throw new ResourceFormatException("TXTC resource data too short for header.");

        var content = new TxtcContent();
        int offset = 0;

        content.Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // TGI offset, relative to the position after this field
        uint tgiOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        long tgiPosn = offset + (long)tgiOffset;

        if (content.Version >= 7)
        {
            int superBlockCount = data[offset++];
            for (int i = 0; i < superBlockCount; i++)
            {
                var (superBlock, read) = ParseSuperBlock(data[offset..], depth);
                content.SuperBlocks.Add(superBlock);
                offset += read;
            }
        }

        content.PatternSize = (TxtcPatternSize)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        content.DataType = (TxtcDataType)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        content.Unknown3 = data[offset++];

        int entryCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;
        if (content.Version >= 8)
            content.Unknown4 = data[offset++];

        // Each entry block is at least a 4-byte null terminator
        if (entryCount < 0 || (long)entryCount * 4 > data.Length - offset)
            throw new ResourceFormatException($"Invalid TXTC entry count: {entryCount}");
        content.Entries.EnsureCapacity(entryCount);
        for (int i = 0; i < entryCount; i++)
        {
            var (entryBlock, read) = TxtcResource.ParseEntryBlock(data[offset..]);
            content.Entries.Add(entryBlock);
            offset += read;
        }

        // Source: TxtcResource.cs lines 141-142 (position check, TGI count byte)
        if (nested && tgiPosn != offset)
            throw new ResourceFormatException($"TXTC TGI block found at 0x{offset:X8}; expected position 0x{tgiPosn:X8}");

        int end = offset;
        if (tgiPosn < data.Length)
        {
            int tgiBlockCount = data[(int)tgiPosn];
            int tgiReadOffset = (int)tgiPosn + 1;
            if (nested && (long)tgiBlockCount * 16 > data.Length - tgiReadOffset)
                throw new ResourceFormatException("TXTC TGI blocks truncated.");
            content.TgiBlocks.EnsureCapacity(tgiBlockCount);

            for (int i = 0; i < tgiBlockCount && tgiReadOffset + 16 <= data.Length; i++)
            {
                // IGT order: Instance (8), Group (4), Type (4)
                ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[tgiReadOffset..]);
                uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[(tgiReadOffset + 8)..]);
                uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[(tgiReadOffset + 12)..]);
                tgiReadOffset += 16;
                content.TgiBlocks.Add(new ResourceKey(type, group, instance));
            }
            end = tgiReadOffset;
        }
        else if (nested)
        {
            throw new ResourceFormatException("TXTC TGI block missing.");
        }

        bytesRead = end;
        return content;
    }

    /// <summary>Source: TxtcResource.cs SuperBlock.Parse, lines 299-317.</summary>
    private static (TxtcSuperBlock block, int bytesRead) ParseSuperBlock(ReadOnlySpan<byte> data, int depth)
    {
        if (data.Length < 5)
            throw new ResourceFormatException("TXTC super block header truncated.");

        int offset = 0;
        byte tgiIndex = data[offset++];
        uint blockSize = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // blockSize covers the fabric plus the three trailing unknown bytes
        if (blockSize < 3 || blockSize > (uint)(data.Length - offset))
            throw new ResourceFormatException($"Invalid TXTC super block size: {blockSize}");
        int fabricSize = (int)blockSize - 3;

        TxtcContent fabric;
        try
        {
            fabric = Parse(data.Slice(offset, fabricSize), nested: true, depth + 1, out int fabricRead);
            if (fabricRead != fabricSize)
                throw new ResourceFormatException(
                    $"Unexpected data in TXTC super block; read 0x{fabricRead:X8} bytes, expected 0x{fabricSize:X8} bytes.");
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            throw new ResourceFormatException("TXTC super block fabric truncated.", ex);
        }
        offset += fabricSize;

        byte unknown1 = data[offset++];
        byte unknown2 = data[offset++];
        byte unknown3 = data[offset++];

        return (new TxtcSuperBlock(tgiIndex, fabric, unknown1, unknown2, unknown3), offset);
    }

    /// <summary>Source: TxtcResource.cs ContentType.UnParse, lines 148-176; SuperBlock.UnParse, lines 319-330.</summary>
    internal void Write(BinaryWriter writer)
    {
        var ms = writer.BaseStream;
        writer.Write(Version);
        long offsetPos = ms.Position;
        writer.Write(0u);

        if (Version >= 7)
        {
            writer.Write((byte)SuperBlocks.Count);
            foreach (var sb in SuperBlocks)
            {
                writer.Write(sb.TgiIndex);
                long sizePos = ms.Position;
                writer.Write(0u);
                long fabricStart = ms.Position;
                sb.Fabric.Write(writer);
                writer.Write(sb.Unknown1);
                writer.Write(sb.Unknown2);
                writer.Write(sb.Unknown3);
                long blockEnd = ms.Position;
                ms.Position = sizePos;
                writer.Write((uint)(blockEnd - fabricStart));
                ms.Position = blockEnd;
            }
        }

        writer.Write((uint)PatternSize);
        writer.Write((uint)DataType);
        writer.Write(Unknown3);
        writer.Write(Entries.Count);
        if (Version >= 8)
            writer.Write(Unknown4);

        foreach (var entryBlock in Entries)
            TxtcResource.WriteEntryBlock(writer, entryBlock);

        long tgiStart = ms.Position;
        writer.Write((byte)TgiBlocks.Count);
        foreach (var tgi in TgiBlocks)
        {
            writer.Write(tgi.Instance);
            writer.Write(tgi.ResourceGroup);
            writer.Write(tgi.ResourceType);
        }

        long endPos = ms.Position;
        ms.Position = offsetPos;
        writer.Write((uint)(tgiStart - offsetPos - 4));
        ms.Position = endPos;
    }
}
