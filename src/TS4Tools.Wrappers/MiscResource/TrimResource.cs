using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// TRIM (Trim) resource for defining trim/border patterns.
/// Resource Type: 0x76BCF80C
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/TRIMResource.cs
///
/// Format:
/// - Magic: "TRIM" (4 bytes)
/// - Version: uint32 (3 or 4+)
/// - EntryCount: int32
/// - Entries: TrimEntry3[] (if version == 3) or TrimEntry4[] (if version >= 4)
/// - MaterialSetKey: TGI block (Type, Group, Instance - 20 bytes)
/// - HasFootprint: byte
///
/// Version 3 entries are 12 bytes (3 floats: x, y, v)
/// Version 4+ entries are 16 bytes (4 floats: x, y, v, mappingMode)
/// </summary>
public sealed class TrimResource : TypedResource
{
    private const uint Magic = 0x4D495254; // "TRIM" in little-endian
    private const int MaxEntryCount = 100000; // Reasonable limit for validation
    private const int HeaderSize = 12; // Magic + Version + EntryCount
    private const int TgiSize = 20; // TGI block size (Type 4 + Group 4 + Instance 8 + unused 4? - actually just T4+G4+I8=16... let's check)

    private uint _version;
    private List<TrimEntry> _entries;
    private ResourceKey _materialSetKey;
    private byte _hasFootprint;

    /// <summary>
    /// The format version (3 or 4+).
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
    /// The trim entries.
    /// </summary>
    public IReadOnlyList<TrimEntry> Entries => _entries;

    /// <summary>
    /// The material set TGI key.
    /// </summary>
    public ResourceKey MaterialSetKey
    {
        get => _materialSetKey;
        set
        {
            if (_materialSetKey != value)
            {
                _materialSetKey = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Whether the trim has a footprint.
    /// </summary>
    public byte HasFootprint
    {
        get => _hasFootprint;
        set
        {
            if (_hasFootprint != value)
            {
                _hasFootprint = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Creates a new TRIM resource by parsing data.
    /// </summary>
    public TrimResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
        _entries ??= [];
    }

    /// <summary>
    /// Adds an entry to the list.
    /// </summary>
    public void AddEntry(TrimEntry entry)
    {
        _entries.Add(entry);
        OnChanged();
    }

    /// <summary>
    /// Removes an entry from the list.
    /// </summary>
    public bool RemoveEntry(TrimEntry entry)
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
    public void SetEntry(int index, TrimEntry entry)
    {
        if (index < 0 || index >= _entries.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _entries[index] = entry;
        OnChanged();
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Source: TRIMResource.cs lines 318-341
        if (data.Length < HeaderSize)
        {
            throw new InvalidDataException($"TRIM data too short: {data.Length} bytes, expected at least {HeaderSize}");
        }

        int offset = 0;

        // Read and validate magic
        // Source: TRIMResource.cs lines 324-328
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        if (magic != Magic)
        {
            throw new InvalidDataException($"Expected TRIM magic 0x{Magic:X8}, got 0x{magic:X8}");
        }
        offset += 4;

        // Read version
        // Source: TRIMResource.cs line 330
        _version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read entry count
        // Source: TRIMResource.cs line 331-338 (inside entry list parsing)
        int entryCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate entry count
        if (entryCount < 0 || entryCount > MaxEntryCount)
        {
            throw new InvalidDataException($"Invalid entry count: {entryCount}");
        }

        // Read entries based on version
        // Source: TRIMResource.cs lines 331-338
        _entries = new List<TrimEntry>(entryCount);
        int entrySize = _version == 3 ? 12 : 16; // 3 or 4 floats

        int requiredDataLength = HeaderSize + (entryCount * entrySize) + 16 + 1; // header + entries + TGI + footprint
        if (data.Length < requiredDataLength)
        {
            throw new InvalidDataException($"TRIM data too short: {data.Length} bytes, expected {requiredDataLength}");
        }

        for (int i = 0; i < entryCount; i++)
        {
            float x = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            float y = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;
            float v = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;

            float mappingMode = 0;
            if (_version != 3)
            {
                mappingMode = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
                offset += 4;
            }

            _entries.Add(new TrimEntry(x, y, v, mappingMode));
        }

        // Read TGI block (default TGI order)
        // Source: TRIMResource.cs line 339
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;
        _materialSetKey = new ResourceKey(type, group, instance);

        // Read has footprint
        // Source: TRIMResource.cs line 340
        _hasFootprint = data[offset];
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Source: TRIMResource.cs lines 343-386
        int entrySize = _version == 3 ? 12 : 16;
        int size = HeaderSize + (_entries.Count * entrySize) + 16 + 1; // header + entries + TGI + footprint

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
        foreach (var entry in _entries)
        {
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.X);
            offset += 4;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.Y);
            offset += 4;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.V);
            offset += 4;

            if (_version != 3)
            {
                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.MappingMode);
                offset += 4;
            }
        }

        // Write TGI block (default TGI order)
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _materialSetKey.ResourceType);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), _materialSetKey.ResourceGroup);
        offset += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), _materialSetKey.Instance);
        offset += 8;

        // Write has footprint
        buffer[offset] = _hasFootprint;

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        // Source: TRIMResource.cs line 49 - default version is 4
        _version = 4;
        _entries = [];
        _materialSetKey = default;
        _hasFootprint = 0;
    }
}

/// <summary>
/// Represents a single trim entry point.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <param name="V">The V value.</param>
/// <param name="MappingMode">The mapping mode (only used in version 4+).</param>
public readonly record struct TrimEntry(float X, float Y, float V, float MappingMode = 0);
