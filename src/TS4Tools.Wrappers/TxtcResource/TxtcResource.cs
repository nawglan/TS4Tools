using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Texture Compositor resource for CAS texture compositing.
/// Resource Types: 0x033A1435, 0x0341ACC9
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/TxtcResource/TxtcResource.cs
/// </summary>
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
        // Source: TxtcResource.cs line 41 (root = new ContentType(...)); the root is parsed as a TxtcContent
        var root = TxtcContent.Parse(data, nested: false, depth: 0, out _);

        Version = root.Version;
        PatternSize = root.PatternSize;
        DataType = root.DataType;
        Unknown3 = root.Unknown3;
        Unknown4 = root.Unknown4;
        _superBlocks.Clear();
        _superBlocks.AddRange(root.SuperBlocks);
        _entries.Clear();
        _entries.AddRange(root.Entries);
        _tgiBlocks.Clear();
        _tgiBlocks.AddRange(root.TgiBlocks);
    }

    internal static (TxtcEntryBlock block, int bytesRead) ParseEntryBlock(ReadOnlySpan<byte> data)
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
                // Source: legacy TxtcResource.cs line 473: new String(r.ReadChars(r.ReadUInt16())), UTF-8 chars
                int strLen = BinaryPrimitives.ReadUInt16LittleEndian(data);
                int strOffset = 2;
                string str = LegacyChars.ReadUtf8Chars(data, ref strOffset, strLen, "TXTC string entry");
                return (new TxtcEntryString(property, unknown, str), strOffset);
            }

            default:
                throw new ResourceFormatException($"Unknown TXTC entry data type: 0x{dataType:X2}");
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        var root = new TxtcContent
        {
            Version = Version,
            PatternSize = PatternSize,
            DataType = DataType,
            Unknown3 = Unknown3,
            Unknown4 = Unknown4,
        };
        root.SuperBlocks.AddRange(_superBlocks);
        root.Entries.AddRange(_entries);
        root.TgiBlocks.AddRange(_tgiBlocks);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
        root.Write(writer);
        return ms.ToArray();
    }

    internal static void WriteEntryBlock(BinaryWriter writer, TxtcEntryBlock entryBlock)
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
                // Source: legacy TxtcResource.cs line 1000: char count, then the chars as UTF-8
                writer.Write((ushort)e.Value.Length);
                writer.Write(Encoding.UTF8.GetBytes(e.Value));
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
    /// <summary>
    /// Default pattern size.
    /// </summary>
    Default = 0x00,

    /// <summary>
    /// Large pattern size.
    /// </summary>
    Large = 0x01,
}

/// <summary>
/// Data type flags for TXTC resources.
/// </summary>
[Flags]
public enum TxtcDataType : uint
{
    /// <summary>
    /// Hair data type.
    /// </summary>
    Hair = 0x00000001,

    /// <summary>
    /// Scalp data type.
    /// </summary>
    Scalp = 0x00000002,

    /// <summary>
    /// Face overlay data type.
    /// </summary>
    FaceOverlay = 0x00000004,

    /// <summary>
    /// Body data type.
    /// </summary>
    Body = 0x00000008,

    /// <summary>
    /// Accessory data type.
    /// </summary>
    Accessory = 0x00000010,
}

/// <summary>
/// Super block for version 7+ TXTC resources.
/// </summary>
/// <remarks>Source: TxtcResource.cs SuperBlock, lines 281-330; the fabric is a nested ContentType.</remarks>
/// <param name="TgiIndex">TGI block index.</param>
/// <param name="Fabric">Nested fabric content.</param>
/// <param name="Unknown1">Unknown field 1.</param>
/// <param name="Unknown2">Unknown field 2.</param>
/// <param name="Unknown3">Unknown field 3.</param>
public sealed record TxtcSuperBlock(
    byte TgiIndex,
    TxtcContent Fabric,
    byte Unknown1,
    byte Unknown2,
    byte Unknown3);

/// <summary>
/// Entry block containing multiple entries terminated by null.
/// </summary>
/// <param name="Entries">List of entries in this block.</param>
public sealed record TxtcEntryBlock(IReadOnlyList<TxtcEntry> Entries);

/// <summary>
/// Base class for TXTC entries.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
public abstract record TxtcEntry(uint Property, byte Unknown);

/// <summary>
/// Boolean entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">Boolean value.</param>
public sealed record TxtcEntryBoolean(uint Property, byte Unknown, bool Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Signed byte entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">Signed byte value.</param>
public sealed record TxtcEntrySByte(uint Property, byte Unknown, sbyte Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Unsigned byte entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">Unsigned byte value.</param>
public sealed record TxtcEntryByte(uint Property, byte Unknown, byte Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// TGI index entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">TGI block index value.</param>
public sealed record TxtcEntryTgiIndex(uint Property, byte Unknown, byte Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Int16 entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">Int16 value.</param>
public sealed record TxtcEntryInt16(uint Property, byte Unknown, short Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// UInt16 entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">UInt16 value.</param>
public sealed record TxtcEntryUInt16(uint Property, byte Unknown, ushort Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Int32 entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">Int32 value.</param>
public sealed record TxtcEntryInt32(uint Property, byte Unknown, int Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// UInt32 entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">UInt32 value.</param>
public sealed record TxtcEntryUInt32(uint Property, byte Unknown, uint Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Int64 entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">Int64 value.</param>
public sealed record TxtcEntryInt64(uint Property, byte Unknown, long Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// UInt64 entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">UInt64 value.</param>
public sealed record TxtcEntryUInt64(uint Property, byte Unknown, ulong Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Single (float) entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">Single (float) value.</param>
public sealed record TxtcEntrySingle(uint Property, byte Unknown, float Value) : TxtcEntry(Property, Unknown);

/// <summary>
/// Rectangle entry (4 floats).
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="X">X coordinate.</param>
/// <param name="Y">Y coordinate.</param>
/// <param name="Width">Width dimension.</param>
/// <param name="Height">Height dimension.</param>
public sealed record TxtcEntryRectangle(uint Property, byte Unknown, float X, float Y, float Width, float Height) : TxtcEntry(Property, Unknown);

/// <summary>
/// Vector entry (4 floats).
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="X">X component.</param>
/// <param name="Y">Y component.</param>
/// <param name="Z">Z component.</param>
/// <param name="W">W component.</param>
public sealed record TxtcEntryVector(uint Property, byte Unknown, float X, float Y, float Z, float W) : TxtcEntry(Property, Unknown);

/// <summary>
/// String entry.
/// </summary>
/// <param name="Property">Property identifier.</param>
/// <param name="Unknown">Unknown field.</param>
/// <param name="Value">String value.</param>
public sealed record TxtcEntryString(uint Property, byte Unknown, string Value) : TxtcEntry(Property, Unknown);
