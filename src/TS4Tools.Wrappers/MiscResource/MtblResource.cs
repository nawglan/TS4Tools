using TS4Tools.Resources;
using TS4Tools.Wrappers.MeshChunks;

namespace TS4Tools.Wrappers;

/// <summary>
/// MTBL (Model Table) resource for storing model rendering metadata.
/// Resource Type: 0x81CA1A10
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/MTBLResource.cs
///
/// Format:
/// - Magic: "MTBL" (4 bytes, 0x4C42544D little-endian)
/// - Version: uint32
/// - EntryCount: int32
/// - Entries: MtblEntry[] (56 bytes each)
///
/// Each entry contains model IID, hash, flags, bounds, and VFX data.
/// </summary>
[ResourceHandler(0x81CA1A10)]
public sealed class MtblResource : TypedResource
{
    private const uint Magic = 0x4C42544D; // "MTBL" in little-endian
    private const int MaxEntryCount = 100000; // Reasonable limit for validation
    private const int HeaderSize = 12; // Magic + Version + EntryCount
    private const int EntrySize = 56; // Size of each MtblEntry

    private uint _version;
    private List<MtblEntry> _entries;

    /// <summary>
    /// The format version.
    /// </summary>
    public uint Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// The model table entries.
    /// </summary>
    public IReadOnlyList<MtblEntry> Entries => _entries;

    /// <summary>
    /// Creates a new MTBL resource by parsing data.
    /// </summary>
    public MtblResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
        _entries ??= [];
    }

    /// <summary>
    /// Adds an entry to the list.
    /// </summary>
    public void AddEntry(MtblEntry entry)
    {
        _entries.Add(entry);
        OnChanged();
    }

    /// <summary>
    /// Removes an entry from the list.
    /// </summary>
    public bool RemoveEntry(MtblEntry entry)
    {
        if (_entries.Remove(entry))
        {
            OnChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void ClearEntries()
    {
        if (_entries.Count > 0)
        {
            _entries.Clear();
            OnChanged();
        }
    }

    /// <summary>
    /// Sets the entry at the specified index.
    /// </summary>
    public void SetEntry(int index, MtblEntry entry)
    {
        if (index < 0 || index >= _entries.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _entries[index] = entry;
        OnChanged();
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Source: MTBLResource.cs lines 292-304
        if (data.Length < HeaderSize)
        {
            throw new InvalidDataException($"MTBL data too short: {data.Length} bytes, expected at least {HeaderSize}");
        }

        int offset = 0;

        // Read and validate magic
        // Source: MTBLResource.cs lines 297-300
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        if (magic != Magic)
        {
            throw new InvalidDataException($"Expected MTBL magic 0x{Magic:X8}, got 0x{magic:X8}");
        }
        offset += 4;

        // Read version
        // Source: MTBLResource.cs line 302
        _version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read entry count (from entry list parsing)
        // Source: MTBLResource.cs line 303 - MTBLEntryList constructor reads count
        int entryCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate entry count
        if (entryCount < 0 || entryCount > MaxEntryCount)
        {
            throw new InvalidDataException($"Invalid entry count: {entryCount}");
        }

        // Validate data length for entries
        int requiredDataLength = HeaderSize + (entryCount * EntrySize);
        if (data.Length < requiredDataLength)
        {
            throw new InvalidDataException($"MTBL data too short: {data.Length} bytes, expected {requiredDataLength}");
        }

        // Read entries
        // Source: MTBLResource.cs lines 230-248 (MTBLEntry.Parse)
        _entries = new List<MtblEntry>(entryCount);
        for (int i = 0; i < entryCount; i++)
        {
            ulong modelIid = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;
            ulong baseFileNameHash = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;
            var widthAndMappingFlags = (WidthAndMappingFlags)data[offset++];
            byte minimumWallHeight = data[offset++];
            byte numberOfLevels = data[offset++];
            byte unused = data[offset++];
            float thumbnailBoundsMinX = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            float thumbnailBoundsMinZ = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            float thumbnailBoundsMinY = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            float thumbnailBoundsMaxX = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            float thumbnailBoundsMaxZ = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            float thumbnailBoundsMaxY = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            var modelFlags = (ModelFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            ulong vfxHash = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            _entries.Add(new MtblEntry(
                modelIid,
                baseFileNameHash,
                widthAndMappingFlags,
                minimumWallHeight,
                numberOfLevels,
                unused,
                thumbnailBoundsMinX,
                thumbnailBoundsMinZ,
                thumbnailBoundsMinY,
                thumbnailBoundsMaxX,
                thumbnailBoundsMaxZ,
                thumbnailBoundsMaxY,
                modelFlags,
                vfxHash));
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Source: MTBLResource.cs lines 306-323
        int size = HeaderSize + (_entries.Count * EntrySize);
        var buffer = new byte[size];
        int offset = 0;

        // Write magic
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Magic);
        offset += 4;

        // Write version
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _version);
        offset += 4;

        // Write entry count
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), _entries.Count);
        offset += 4;

        // Write entries
        // Source: MTBLResource.cs lines 252-269 (MTBLEntry.UnParse)
        foreach (var entry in _entries)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), entry.ModelIid);
            offset += 8;
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), entry.BaseFileNameHash);
            offset += 8;
            buffer[offset++] = (byte)entry.WidthAndMappingFlags;
            buffer[offset++] = entry.MinimumWallHeight;
            buffer[offset++] = entry.NumberOfLevels;
            buffer[offset++] = entry.Unused;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.ThumbnailBoundsMinX);
            offset += 4;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.ThumbnailBoundsMinZ);
            offset += 4;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.ThumbnailBoundsMinY);
            offset += 4;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.ThumbnailBoundsMaxX);
            offset += 4;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.ThumbnailBoundsMaxZ);
            offset += 4;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.ThumbnailBoundsMaxY);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), (uint)entry.ModelFlags);
            offset += 4;
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), entry.VfxHash);
            offset += 8;
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        _version = 0;
        _entries = [];
    }
}

/// <summary>
/// Represents a single model table entry.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/MTBLResource.cs lines 77-289
/// </summary>
/// <param name="ModelIid">The model instance ID.</param>
/// <param name="BaseFileNameHash">The base file name hash.</param>
/// <param name="WidthAndMappingFlags">Width and mapping flags.</param>
/// <param name="MinimumWallHeight">The minimum wall height.</param>
/// <param name="NumberOfLevels">The number of levels.</param>
/// <param name="Unused">Unused byte.</param>
/// <param name="ThumbnailBoundsMinX">Thumbnail bounds minimum X.</param>
/// <param name="ThumbnailBoundsMinZ">Thumbnail bounds minimum Z.</param>
/// <param name="ThumbnailBoundsMinY">Thumbnail bounds minimum Y.</param>
/// <param name="ThumbnailBoundsMaxX">Thumbnail bounds maximum X.</param>
/// <param name="ThumbnailBoundsMaxZ">Thumbnail bounds maximum Z.</param>
/// <param name="ThumbnailBoundsMaxY">Thumbnail bounds maximum Y.</param>
/// <param name="ModelFlags">The model flags.</param>
/// <param name="VfxHash">The VFX hash.</param>
public readonly record struct MtblEntry(
    ulong ModelIid,
    ulong BaseFileNameHash,
    WidthAndMappingFlags WidthAndMappingFlags,
    byte MinimumWallHeight,
    byte NumberOfLevels,
    byte Unused,
    float ThumbnailBoundsMinX,
    float ThumbnailBoundsMinZ,
    float ThumbnailBoundsMinY,
    float ThumbnailBoundsMaxX,
    float ThumbnailBoundsMaxZ,
    float ThumbnailBoundsMaxY,
    ModelFlags ModelFlags,
    ulong VfxHash);
