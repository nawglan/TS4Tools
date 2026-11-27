
namespace TS4Tools.Package;

/// <summary>
/// Implementation of a resource index entry.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs
///
/// Entry layout (32 bytes when fully expanded):
/// - Type (4 bytes) - lines 51-55
/// - Group (4 bytes) - lines 61-65
/// - Instance (8 bytes: high shifted left 32 bits OR low) - lines 71-80
/// - ChunkOffset (4 bytes) - lines 86-90
/// - FileSize (4 bytes, bit 31 always set, mask with 0x7FFFFFFF) - lines 96-100
/// - MemSize (4 bytes) - lines 106-110
/// - Compressed (2 bytes) - lines 116-120
/// - Unknown2 (2 bytes, always 1) - lines 126-130
///
/// Index type flags optimize storage (PackageIndex.cs lines 35-46):
/// - 0x01: Type stored in index header
/// - 0x02: Group stored in index header
/// - 0x04: InstanceHigh stored in index header
/// </remarks>
internal sealed class ResourceIndexEntry : IMutableResourceIndexEntry
{
    private ResourceKey _key;
    private bool _isDeleted;
    private ushort _compressionType;

    /// <summary>
    /// Callback raised when a user-visible field (Key, IsDeleted, CompressionType) changes.
    /// The owning package uses this to mark itself dirty.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 51-80, 116-120, 144
    /// (every setter calls OnElementChanged()).
    /// </remarks>
    internal Action<ResourceIndexEntry>? ElementChanged { get; set; }

    public ResourceKey Key
    {
        get => _key;
        set
        {
            if (_key == value) return;
            _key = value;
            ElementChanged?.Invoke(this);
        }
    }

    public uint ChunkOffset { get; internal set; }
    public uint FileSize { get; internal set; }
    public uint MemorySize { get; internal set; }

    /// <summary>
    /// Gets or sets the compression type flag (0x0000 = none, 0x5A42 = ZLIB, 0xFFFF = RefPack, ...).
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 116-120 (setter
    /// raises OnElementChanged, which marks the entry dirty so Package.packedChunk re-encodes it,
    /// Package.cs lines 559-573).
    /// Setting a different value through the public interface marks the entry dirty so its data is
    /// re-encoded (compressed or stored uncompressed) on the next save.
    /// </remarks>
    public ushort CompressionType
    {
        get => _compressionType;
        set
        {
            if (_compressionType == value) return;
            _compressionType = value;
            IsDirty = true;
            ElementChanged?.Invoke(this);
        }
    }

    /// <summary>Initialises the compression flag without marking the entry dirty (object initialisers).</summary>
    internal ushort CompressionTypeInit { init => _compressionType = value; }

    /// <summary>Sets the compression flag without marking the entry dirty (used by index readers/writers).</summary>
    internal void SetCompressionTypeRaw(ushort value) => _compressionType = value;

    public ushort Unknown2 { get; internal set; } = 1;

    public bool IsCompressed => FileSize != MemorySize;

    public bool IsDeleted
    {
        get => _isDeleted;
        set
        {
            // Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs line 144
            if (_isDeleted == value) return;
            _isDeleted = value;
            ElementChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// Replaced/added resource data. Only set for new or replaced resources: clean reads are not
    /// cached here (legacy only holds ResourceStream for replaced data,
    /// legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 252-256).
    /// </summary>
    internal Memory<byte>? ResourceData { get; set; }

    /// <summary>
    /// Whether the resource must be re-encoded on save (new/replaced data or compression change).
    /// </summary>
    internal bool IsDirty { get; set; }

    public ResourceIndexEntry() { }

    public ResourceIndexEntry(ResourceKey key)
    {
        _key = key;
        ChunkOffset = 0xFFFFFFFF; // Not yet written
    }

    /// <summary>
    /// Creates a deep copy of this entry.
    /// </summary>
    internal ResourceIndexEntry Clone()
    {
        return new ResourceIndexEntry
        {
            _key = _key,
            ChunkOffset = ChunkOffset,
            FileSize = FileSize,
            MemorySize = MemorySize,
            _compressionType = _compressionType,
            Unknown2 = Unknown2,
            _isDeleted = _isDeleted,
            ResourceData = ResourceData,
            IsDirty = IsDirty
        };
    }

    public override string ToString() =>
        $"{Key} Offset:0x{ChunkOffset:X8} Size:{FileSize}/{MemorySize} Compressed:{IsCompressed}";
}

/// <summary>
/// Reads and writes resource index entries from/to binary format.
/// </summary>
internal static class ResourceIndexEntrySerializer
{
    /// <summary>
    /// Full entry size when not using index type compression.
    /// 7 uint fields (Type, Group, InstanceHigh, InstanceLow, ChunkOffset, FileSize, MemSize)
    /// plus 2 ushort fields (Compressed, Unknown2) = 7*4 + 2*2 = 32 bytes.
    /// </summary>
    public const int FullEntrySize = 32;

    /// <summary>
    /// Base entry size (always present): InstanceLow, ChunkOffset, FileSize, MemSize (4*4)
    /// plus Compressed + Unknown2 (2+2) = 20 bytes.
    /// </summary>
    public const int BaseEntrySize = 20;

    /// <summary>
    /// Reads an index entry from a binary span.
    /// </summary>
    /// <param name="data">The binary data.</param>
    /// <param name="indexType">The index type flags from the package header.</param>
    /// <param name="headerType">The shared type value (if bit 0 set).</param>
    /// <param name="headerGroup">The shared group value (if bit 1 set).</param>
    /// <param name="headerInstanceHigh">The shared instance high value (if bit 2 set).</param>
    public static ResourceIndexEntry Read(
        ReadOnlySpan<byte> data,
        uint indexType,
        uint headerType,
        uint headerGroup,
        uint headerInstanceHigh)
    {
        int offset = 0;

        // Type: from header if bit 0, else from entry
        uint type = (indexType & 0x01) != 0
            ? headerType
            : ReadUInt32(data, ref offset);

        // Group: from header if bit 1, else from entry
        uint group = (indexType & 0x02) != 0
            ? headerGroup
            : ReadUInt32(data, ref offset);

        // Instance high: from header if bit 2, else from entry
        uint instanceHigh = (indexType & 0x04) != 0
            ? headerInstanceHigh
            : ReadUInt32(data, ref offset);

        // Remaining fields always in entry
        uint instanceLow = ReadUInt32(data, ref offset);
        uint chunkOffset = ReadUInt32(data, ref offset);
        uint fileSize = ReadUInt32(data, ref offset) & 0x7FFFFFFF; // Bit 31 always set, mask it
        uint memSize = ReadUInt32(data, ref offset);
        ushort compressed = ReadUInt16(data, ref offset);
        ushort unknown2 = ReadUInt16(data, ref offset);

        ulong instance = ((ulong)instanceHigh << 32) | instanceLow;

        return new ResourceIndexEntry
        {
            Key = new ResourceKey(type, group, instance),
            ChunkOffset = chunkOffset,
            FileSize = fileSize,
            MemorySize = memSize,
            CompressionTypeInit = compressed,
            Unknown2 = unknown2
        };
    }

    /// <summary>
    /// Calculates the size of an entry based on index type flags.
    /// </summary>
    public static int GetEntrySize(uint indexType)
    {
        // Base size is always present: InstanceLow, ChunkOffset, FileSize, MemSize, Compressed+Unknown2
        int size = BaseEntrySize;

        // Add 4 bytes for each optional field NOT in the header
        if ((indexType & 0x01) == 0) size += 4; // Type in entry
        if ((indexType & 0x02) == 0) size += 4; // Group in entry
        if ((indexType & 0x04) == 0) size += 4; // InstanceHigh in entry

        return size;
    }

    /// <summary>
    /// Calculates the number of header fields based on index type.
    /// </summary>
    public static int GetHeaderFieldCount(uint indexType)
    {
        int count = 1; // indexType itself
        if ((indexType & 0x01) != 0) count++;
        if ((indexType & 0x02) != 0) count++;
        if ((indexType & 0x04) != 0) count++;
        return count;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        return value;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;
        return value;
    }
}
