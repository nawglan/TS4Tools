using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Texture Compositor resource for CAS texture compositing.
/// Resource Types: 0x033A1435, 0x0341ACC9
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/TxtcResource/TxtcResource.cs
/// </summary>
[ResourceHandler(0x033A1435)]
[ResourceHandler(0x0341ACC9)]
public sealed class TxtcResource : TypedResource
{
    private readonly List<TxtcSuperBlock> _superBlocks = [];
    private readonly List<TxtcEntryBlock> _entries = [];
    private readonly List<ResourceKey> _tgiBlocks = [];

    /// <summary>
    /// Format version.
    /// </summary>
    public uint Version { get; set; } = 8;

    /// <summary>
    /// Pattern size type.
    /// </summary>
    public TxtcPatternSize PatternSize { get; set; } = TxtcPatternSize.Default;

    /// <summary>
    /// Data type flags.
    /// </summary>
    public TxtcDataType DataType { get; set; } = TxtcDataType.Body;

    /// <summary>
    /// Unknown byte field.
    /// </summary>
    public byte Unknown3 { get; set; }

    /// <summary>
    /// Unknown byte field (version 8+).
    /// </summary>
    public byte Unknown4 { get; set; }

    /// <summary>
    /// Super blocks (version 7+).
    /// </summary>
    public IReadOnlyList<TxtcSuperBlock> SuperBlocks => _superBlocks;

    /// <summary>
    /// Entry blocks.
    /// </summary>
    public IReadOnlyList<TxtcEntryBlock> Entries => _entries;

    /// <summary>
    /// TGI blocks.
    /// </summary>
    public IReadOnlyList<ResourceKey> TgiBlocks => _tgiBlocks;

    /// <summary>
    /// Creates a new TxtcResource by parsing data.
    /// </summary>
    public TxtcResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 18)
            throw new ResourceFormatException("TXTC resource data too short for header.");

        int offset = 0;

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read TGI offset (relative to current position)
        uint tgiOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        long tgiPosn = offset + tgiOffset;

        // Read super blocks if version >= 7
        _superBlocks.Clear();
        if (Version >= 7)
        {
            int superBlockCount = data[offset++];
            for (int i = 0; i < superBlockCount; i++)
            {
                var (superBlock, bytesRead) = ParseSuperBlock(data[offset..]);
                _superBlocks.Add(superBlock);
                offset += bytesRead;
            }
        }

        // Read PatternSize
        PatternSize = (TxtcPatternSize)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read DataType
        DataType = (TxtcDataType)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read Unknown3
        Unknown3 = data[offset++];

        // Read entry count
        int entryCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read Unknown4 if version >= 8
        if (Version >= 8)
        {
            Unknown4 = data[offset++];
        }

        // Read entry blocks
        _entries.Clear();
        _entries.EnsureCapacity(entryCount);
        for (int i = 0; i < entryCount; i++)
        {
            var (entryBlock, bytesRead) = ParseEntryBlock(data[offset..]);
            _entries.Add(entryBlock);
            offset += bytesRead;
        }

        // Read TGI blocks (IGT order per legacy)
        _tgiBlocks.Clear();
        if (tgiPosn < data.Length)
        {
            int tgiBlockCount = data[(int)tgiPosn];
            int tgiReadOffset = (int)tgiPosn + 1;
            _tgiBlocks.EnsureCapacity(tgiBlockCount);

            for (int i = 0; i < tgiBlockCount && tgiReadOffset + 16 <= data.Length; i++)
            {
                // IGT order: Instance (8), Group (4), Type (4)
                ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 8;
                uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 4;
                uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[tgiReadOffset..]);
                tgiReadOffset += 4;
                _tgiBlocks.Add(new ResourceKey(type, group, instance));
            }
        }
    }

    private static (TxtcSuperBlock block, int bytesRead) ParseSuperBlock(ReadOnlySpan<byte> data)
    {
        int offset = 0;

        byte tgiIndex = data[offset++];
        uint blockSize = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        int nestedStart = offset;
        // For now, store the nested fabric content as raw bytes
        // Full implementation would recursively parse ContentType
        var fabricData = data.Slice(offset, (int)blockSize - 3).ToArray(); // -3 for unknown1/2/3
        offset += (int)blockSize - 3;

        byte unknown1 = data[offset++];
        byte unknown2 = data[offset++];
        byte unknown3 = data[offset++];

        return (new TxtcSuperBlock(tgiIndex, fabricData, unknown1, unknown2, unknown3), offset);
    }

    private static (TxtcEntryBlock block, int bytesRead) ParseEntryBlock(ReadOnlySpan<byte> data)
    {
        var entries = new List<TxtcEntry>();
        int offset = 0;

        while (offset + 4 <= data.Length)
        {
            uint property = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            if (property == 0)
                break; // Null terminator

            byte unknown = data[offset++];
            byte dataType = data[offset++];

            var (entry, entryBytesRead) = ParseEntry(property, unknown, dataType, data[offset..]);
            if (entry != null)
            {
                entries.Add(entry);
            }
            offset += entryBytesRead;
        }

        return (new TxtcEntryBlock(entries), offset);
    }

    private static (TxtcEntry? entry, int bytesRead) ParseEntry(uint property, byte unknown, byte dataType, ReadOnlySpan<byte> data)
    {
        switch (dataType)
        {
            case 0x00: // Boolean
                return (new TxtcEntryBoolean(property, unknown, data[0] != 0), 1);

            case 0x01: // SByte
                return (new TxtcEntrySByte(property, unknown, (sbyte)data[0]), 1);

            case 0x05: // Byte
                return (new TxtcEntryByte(property, unknown, data[0]), 1);

            case 0x0C: // TGI Index
                return (new TxtcEntryTgiIndex(property, unknown, data[0]), 1);

            case 0x02: // Int16
                return (new TxtcEntryInt16(property, unknown, BinaryPrimitives.ReadInt16LittleEndian(data)), 2);

            case 0x06: // UInt16
                return (new TxtcEntryUInt16(property, unknown, BinaryPrimitives.ReadUInt16LittleEndian(data)), 2);

            case 0x03: // Int32
                return (new TxtcEntryInt32(property, unknown, BinaryPrimitives.ReadInt32LittleEndian(data)), 4);

            case 0x07: // UInt32
                return (new TxtcEntryUInt32(property, unknown, BinaryPrimitives.ReadUInt32LittleEndian(data)), 4);

            case 0x04: // Int64
                return (new TxtcEntryInt64(property, unknown, BinaryPrimitives.ReadInt64LittleEndian(data)), 8);

            case 0x08: // UInt64
                return (new TxtcEntryUInt64(property, unknown, BinaryPrimitives.ReadUInt64LittleEndian(data)), 8);

            case 0x09: // Single
                return (new TxtcEntrySingle(property, unknown, BinaryPrimitives.ReadSingleLittleEndian(data)), 4);

            case 0x0A: // Rectangle (4 floats)
            {
                float x = BinaryPrimitives.ReadSingleLittleEndian(data);
                float y = BinaryPrimitives.ReadSingleLittleEndian(data[4..]);
                float w = BinaryPrimitives.ReadSingleLittleEndian(data[8..]);
                float h = BinaryPrimitives.ReadSingleLittleEndian(data[12..]);
                return (new TxtcEntryRectangle(property, unknown, x, y, w, h), 16);
            }

            case 0x0B: // Vector (4 floats)
            {
                float x = BinaryPrimitives.ReadSingleLittleEndian(data);
                float y = BinaryPrimitives.ReadSingleLittleEndian(data[4..]);
                float z = BinaryPrimitives.ReadSingleLittleEndian(data[8..]);
                float w = BinaryPrimitives.ReadSingleLittleEndian(data[12..]);
                return (new TxtcEntryVector(property, unknown, x, y, z, w), 16);
            }

            case 0x0D: // String
            {
                int strLen = BinaryPrimitives.ReadUInt16LittleEndian(data);
                // String is stored as char[] (2 bytes per char in legacy, but we'll handle as ASCII for safety)
                string str = Encoding.Unicode.GetString(data.Slice(2, strLen * 2));
                return (new TxtcEntryString(property, unknown, str), 2 + strLen * 2);
            }

            default:
                throw new ResourceFormatException($"Unknown TXTC entry data type: 0x{dataType:X2}");
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

        // Write version
        writer.Write(Version);

        // Placeholder for TGI offset
        long offsetPos = ms.Position;
        writer.Write(0u);

        // Write super blocks if version >= 7
        if (Version >= 7)
        {
            writer.Write((byte)_superBlocks.Count);
            foreach (var superBlock in _superBlocks)
            {
                WriteSuperBlock(writer, superBlock);
            }
        }

        // Write PatternSize
        writer.Write((uint)PatternSize);

        // Write DataType
        writer.Write((uint)DataType);

        // Write Unknown3
        writer.Write(Unknown3);

        // Write entry count
        writer.Write(_entries.Count);

        // Write Unknown4 if version >= 8
        if (Version >= 8)
        {
            writer.Write(Unknown4);
        }

        // Write entry blocks
        foreach (var entryBlock in _entries)
        {
            WriteEntryBlock(writer, entryBlock);
        }

        // Record TGI position and write offset
        long tgiStart = ms.Position;
        uint tgiOffset = (uint)(tgiStart - offsetPos - 4);

        // Write TGI blocks (IGT order)
        writer.Write((byte)_tgiBlocks.Count);
        foreach (var tgi in _tgiBlocks)
        {
            writer.Write(tgi.Instance);
            writer.Write(tgi.ResourceGroup);
            writer.Write(tgi.ResourceType);
        }

        // Go back and write offset
        long endPos = ms.Position;
        ms.Position = offsetPos;
        writer.Write(tgiOffset);
        ms.Position = endPos;

        return ms.ToArray();
    }

    private static void WriteSuperBlock(BinaryWriter writer, TxtcSuperBlock superBlock)
    {
        writer.Write(superBlock.TgiIndex);
        uint blockSize = (uint)(superBlock.FabricData.Length + 3);
        writer.Write(blockSize);
        writer.Write(superBlock.FabricData);
        writer.Write(superBlock.Unknown1);
        writer.Write(superBlock.Unknown2);
        writer.Write(superBlock.Unknown3);
    }

    private static void WriteEntryBlock(BinaryWriter writer, TxtcEntryBlock entryBlock)
    {
        foreach (var entry in entryBlock.Entries)
        {
            WriteEntry(writer, entry);
        }
        writer.Write(0u); // Null terminator
    }

    private static void WriteEntry(BinaryWriter writer, TxtcEntry entry)
    {
        writer.Write(entry.Property);
        writer.Write(entry.Unknown);

        switch (entry)
        {
            case TxtcEntryBoolean e:
                writer.Write((byte)0x00);
                writer.Write(e.Value ? (byte)1 : (byte)0);
                break;

            case TxtcEntrySByte e:
                writer.Write((byte)0x01);
                writer.Write(e.Value);
                break;

            case TxtcEntryByte e:
                writer.Write((byte)0x05);
                writer.Write(e.Value);
                break;

            case TxtcEntryTgiIndex e:
                writer.Write((byte)0x0C);
                writer.Write(e.Value);
                break;

            case TxtcEntryInt16 e:
                writer.Write((byte)0x02);
                writer.Write(e.Value);
                break;

            case TxtcEntryUInt16 e:
                writer.Write((byte)0x06);
                writer.Write(e.Value);
                break;

            case TxtcEntryInt32 e:
                writer.Write((byte)0x03);
                writer.Write(e.Value);
                break;

            case TxtcEntryUInt32 e:
                writer.Write((byte)0x07);
                writer.Write(e.Value);
                break;

            case TxtcEntryInt64 e:
                writer.Write((byte)0x04);
                writer.Write(e.Value);
                break;

            case TxtcEntryUInt64 e:
                writer.Write((byte)0x08);
                writer.Write(e.Value);
                break;

            case TxtcEntrySingle e:
                writer.Write((byte)0x09);
                writer.Write(e.Value);
                break;

            case TxtcEntryRectangle e:
                writer.Write((byte)0x0A);
                writer.Write(e.X);
                writer.Write(e.Y);
                writer.Write(e.Width);
                writer.Write(e.Height);
                break;

            case TxtcEntryVector e:
                writer.Write((byte)0x0B);
                writer.Write(e.X);
                writer.Write(e.Y);
                writer.Write(e.Z);
                writer.Write(e.W);
                break;

            case TxtcEntryString e:
                writer.Write((byte)0x0D);
                writer.Write((ushort)e.Value.Length);
                writer.Write(Encoding.Unicode.GetBytes(e.Value));
                break;
        }
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = 8;
        PatternSize = TxtcPatternSize.Default;
        DataType = TxtcDataType.Body;
        Unknown3 = 0;
        Unknown4 = 0;
        _superBlocks.Clear();
        _entries.Clear();
        _tgiBlocks.Clear();
    }

    /// <summary>
    /// Adds a super block.
    /// </summary>
    public void AddSuperBlock(TxtcSuperBlock superBlock)
    {
        _superBlocks.Add(superBlock);
        OnChanged();
    }

    /// <summary>
    /// Adds an entry block.
    /// </summary>
    public void AddEntryBlock(TxtcEntryBlock entryBlock)
    {
        _entries.Add(entryBlock);
        OnChanged();
    }

    /// <summary>
    /// Adds a TGI block.
    /// </summary>
    public void AddTgiBlock(ResourceKey tgiBlock)
    {
        _tgiBlocks.Add(tgiBlock);
        OnChanged();
    }

    /// <summary>
    /// Clears all super blocks.
    /// </summary>
    public void ClearSuperBlocks()
    {
        _superBlocks.Clear();
        OnChanged();
    }

    /// <summary>
    /// Clears all entry blocks.
    /// </summary>
    public void ClearEntryBlocks()
    {
        _entries.Clear();
        OnChanged();
    }

    /// <summary>
    /// Clears all TGI blocks.
    /// </summary>
    public void ClearTgiBlocks()
    {
        _tgiBlocks.Clear();
        OnChanged();
    }
}

/// <summary>
/// Pattern size type.
/// </summary>
public enum TxtcPatternSize : uint
{
    Default = 0x00,
    Large = 0x01,
}

/// <summary>
/// Data type flags for TXTC resources.
/// </summary>
[Flags]
public enum TxtcDataType : uint
{
    Hair = 0x00000001,
    Scalp = 0x00000002,
    FaceOverlay = 0x00000004,
    Body = 0x00000008,
    Accessory = 0x00000010,
}

/// <summary>
/// Super block for version 7+ TXTC resources.
/// </summary>
public sealed record TxtcSuperBlock(
    byte TgiIndex,
    byte[] FabricData,
    byte Unknown1,
    byte Unknown2,
    byte Unknown3);

/// <summary>
/// Entry block containing multiple entries terminated by null.
/// </summary>
public sealed record TxtcEntryBlock(IReadOnlyList<TxtcEntry> Entries);

/// <summary>
/// Base class for TXTC entries.
/// </summary>
public abstract record TxtcEntry(uint Property, byte Unknown);

/// <summary>
/// Boolean entry.
/// </summary>
public sealed record TxtcEntryBoolean(uint Property, byte Unknown, bool Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Signed byte entry.
/// </summary>
public sealed record TxtcEntrySByte(uint Property, byte Unknown, sbyte Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Unsigned byte entry.
/// </summary>
public sealed record TxtcEntryByte(uint Property, byte Unknown, byte Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// TGI index entry.
/// </summary>
public sealed record TxtcEntryTgiIndex(uint Property, byte Unknown, byte Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Int16 entry.
/// </summary>
public sealed record TxtcEntryInt16(uint Property, byte Unknown, short Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// UInt16 entry.
/// </summary>
public sealed record TxtcEntryUInt16(uint Property, byte Unknown, ushort Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Int32 entry.
/// </summary>
public sealed record TxtcEntryInt32(uint Property, byte Unknown, int Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// UInt32 entry.
/// </summary>
public sealed record TxtcEntryUInt32(uint Property, byte Unknown, uint Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Int64 entry.
/// </summary>
public sealed record TxtcEntryInt64(uint Property, byte Unknown, long Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// UInt64 entry.
/// </summary>
public sealed record TxtcEntryUInt64(uint Property, byte Unknown, ulong Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Single (float) entry.
/// </summary>
public sealed record TxtcEntrySingle(uint Property, byte Unknown, float Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Rectangle entry (4 floats).
/// </summary>
public sealed record TxtcEntryRectangle(uint Property, byte Unknown, float X, float Y, float Width, float Height) : TxtcEntry(Property, Unknown);

/// <summary>
/// Vector entry (4 floats).
/// </summary>
public sealed record TxtcEntryVector(uint Property, byte Unknown, float X, float Y, float Z, float W) : TxtcEntry(Property, Unknown);

/// <summary>
/// String entry.
/// </summary>
public sealed record TxtcEntryString(uint Property, byte Unknown, string Value) : TxtcEntry(Property, Unknown);
