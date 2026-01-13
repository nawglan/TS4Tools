
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Material states used in MTST blocks.
/// Source: MTST.cs State enum, lines 108-116
/// </summary>
public enum MaterialState : uint
{
    /// <summary>Default material state.</summary>
    Default = 0x2EA8FB98,
    /// <summary>Dirty material state.</summary>
    Dirty = 0xEEAB4327,
    /// <summary>Very dirty material state.</summary>
    VeryDirty = 0x2E5DF9BB,
    /// <summary>Burnt material state.</summary>
    Burnt = 0xC3867C32,
    /// <summary>Clogged material state.</summary>
    Clogged = 0x257FB026,
    /// <summary>Car lights off state.</summary>
    CarLightsOff = 0xE4AF52C1,
}

/// <summary>
/// MTST (Material Set) block - defines material state variations.
/// Resource Type: 0x02019972
/// Source: s4pi Wrappers/s4piRCOLChunks/MTST.cs
/// </summary>
public sealed class MtstBlock : RcolBlock
{
    /// <summary>Resource type identifier for MTST.</summary>
    public const uint TypeId = 0x02019972;

    /// <summary>Version threshold for Type300 entries.</summary>
    private const uint Version300Threshold = 768;

    /// <inheritdoc/>
    public override string Tag => "MTST";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (0x200 for Type200, 0x300+ for Type300).</summary>
    public uint Version { get; private set; } = 0x00000200;

    /// <summary>Name hash for this material set.</summary>
    public uint NameHash { get; private set; }

    /// <summary>Chunk reference index.</summary>
    public RcolChunkReference Index { get; private set; }

    /// <summary>Whether this MTST uses Type300 entries (version >= 768).</summary>
    public bool IsType300 => Version >= Version300Threshold;

    /// <summary>Material entries (Type200 format, used when Version &lt; 768).</summary>
    public List<MtstEntry200> Type200Entries { get; } = [];

    /// <summary>Material entries (Type300 format, used when Version >= 768).</summary>
    public List<MtstEntry300> Type300Entries { get; } = [];

    /// <summary>
    /// Creates an empty MTST block.
    /// </summary>
    public MtstBlock() : base()
    {
    }

    /// <summary>
    /// Creates an MTST block from raw data.
    /// </summary>
    public MtstBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the MTST block data.
    /// Source: MTST.cs lines 64-84
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid MTST tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read name hash
        NameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read chunk reference (4 bytes)
        Index = RcolChunkReference.Read(data[pos..]);
        pos += RcolChunkReference.Size;

        // Read entry list based on version
        // Source: MTST.cs lines 76-83
        Type200Entries.Clear();
        Type300Entries.Clear();

        int entryCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        if (entryCount < 0 || entryCount > 10000)
            throw new InvalidDataException($"Invalid MTST entry count: {entryCount}");

        if (Version < Version300Threshold)
        {
            // Type200: matdIndex (4) + materialState (4) = 8 bytes per entry
            for (int i = 0; i < entryCount; i++)
            {
                var matdIndex = RcolChunkReference.Read(data[pos..]);
                pos += RcolChunkReference.Size;
                var materialState = (MaterialState)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
                pos += 4;
                Type200Entries.Add(new MtstEntry200(matdIndex, materialState));
            }
        }
        else
        {
            // Type300: matdIndex (4) + materialState (4) + materialVariant (4) = 12 bytes per entry
            for (int i = 0; i < entryCount; i++)
            {
                var matdIndex = RcolChunkReference.Read(data[pos..]);
                pos += RcolChunkReference.Size;
                var materialState = (MaterialState)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
                pos += 4;
                var materialVariant = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
                pos += 4;
                Type300Entries.Add(new MtstEntry300(matdIndex, materialState, materialVariant));
            }
        }
    }

    /// <summary>
    /// Serializes the MTST block back to bytes.
    /// Source: MTST.cs lines 86-104
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'S');
        writer.Write((byte)'T');

        // Write version
        writer.Write(Version);

        // Write name hash
        writer.Write(NameHash);

        // Write chunk reference
        var indexBuffer = new byte[RcolChunkReference.Size];
        Index.Write(indexBuffer);
        writer.Write(indexBuffer);

        // Write entries based on version
        if (Version < Version300Threshold)
        {
            writer.Write(Type200Entries.Count);
            foreach (var entry in Type200Entries)
            {
                entry.MatdIndex.Write(indexBuffer);
                writer.Write(indexBuffer);
                writer.Write((uint)entry.MaterialState);
            }
        }
        else
        {
            writer.Write(Type300Entries.Count);
            foreach (var entry in Type300Entries)
            {
                entry.MatdIndex.Write(indexBuffer);
                writer.Write(indexBuffer);
                writer.Write((uint)entry.MaterialState);
                writer.Write(entry.MaterialVariant);
            }
        }

        return ms.ToArray();
    }
}

/// <summary>
/// MTST entry for version &lt; 768 (Type200).
/// Contains material index and state.
/// Source: MTST.cs Type200Entry class, lines 219-300
/// </summary>
public readonly struct MtstEntry200 : IEquatable<MtstEntry200>
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 8;

    /// <summary>Reference to the MATD chunk.</summary>
    public RcolChunkReference MatdIndex { get; }

    /// <summary>Material state (Default, Dirty, etc.).</summary>
    public MaterialState MaterialState { get; }

    /// <summary>
    /// Creates a new MTST entry.
    /// </summary>
    public MtstEntry200(RcolChunkReference matdIndex, MaterialState materialState)
    {
        MatdIndex = matdIndex;
        MaterialState = materialState;
    }

    /// <inheritdoc/>
    public bool Equals(MtstEntry200 other) =>
        MatdIndex == other.MatdIndex && MaterialState == other.MaterialState;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MtstEntry200 other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(MatdIndex, MaterialState);
    /// <summary>Equality operator.</summary>
    public static bool operator ==(MtstEntry200 left, MtstEntry200 right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(MtstEntry200 left, MtstEntry200 right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"Index={MatdIndex}, State={MaterialState}";
}

/// <summary>
/// MTST entry for version >= 768 (Type300).
/// Contains material index, state, and variant.
/// Source: MTST.cs Type300Entry class, lines 118-205
/// </summary>
public readonly struct MtstEntry300 : IEquatable<MtstEntry300>
{
    /// <summary>Size in bytes when serialized.</summary>
    public const int Size = 12;

    /// <summary>Reference to the MATD chunk.</summary>
    public RcolChunkReference MatdIndex { get; }

    /// <summary>Material state (Default, Dirty, etc.).</summary>
    public MaterialState MaterialState { get; }

    /// <summary>Material variant identifier.</summary>
    public uint MaterialVariant { get; }

    /// <summary>
    /// Creates a new MTST entry with variant.
    /// </summary>
    public MtstEntry300(RcolChunkReference matdIndex, MaterialState materialState, uint materialVariant)
    {
        MatdIndex = matdIndex;
        MaterialState = materialState;
        MaterialVariant = materialVariant;
    }

    /// <inheritdoc/>
    public bool Equals(MtstEntry300 other) =>
        MatdIndex == other.MatdIndex &&
        MaterialState == other.MaterialState &&
        MaterialVariant == other.MaterialVariant;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MtstEntry300 other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(MatdIndex, MaterialState, MaterialVariant);
    /// <summary>Equality operator.</summary>
    public static bool operator ==(MtstEntry300 left, MtstEntry300 right) => left.Equals(right);
    /// <summary>Inequality operator.</summary>
    public static bool operator !=(MtstEntry300 left, MtstEntry300 right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() =>
        $"Index={MatdIndex}, State={MaterialState}, Variant=0x{MaterialVariant:X8}";
}
