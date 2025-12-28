using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// NGMP Hash Map resource containing hash-to-instance mappings.
/// Resource Type: 0xF3A38370
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/NGMPHashMapResource/NGMPHashMapResource.cs
/// </summary>
[ResourceHandler(0xF3A38370)]
public sealed class NgmpHashMapResource : TypedResource
{
    /// <summary>
    /// Current format version.
    /// </summary>
    public const uint CurrentVersion = 1;

    private readonly List<NgmpPair> _entries = [];

    /// <summary>
    /// Format version (should be 1).
    /// </summary>
    public uint Version { get; private set; } = CurrentVersion;

    /// <summary>
    /// The hash-instance pairs in this resource.
    /// </summary>
    public IReadOnlyList<NgmpPair> Entries => _entries;

    /// <summary>
    /// Creates a new NgmpHashMapResource by parsing data.
    /// </summary>
    public NgmpHashMapResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
            throw new ResourceFormatException("NGMP data too short for header.");

        int offset = 0;

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (Version != CurrentVersion)
            throw new ResourceFormatException($"Unsupported NGMP version: {Version}. Expected {CurrentVersion}.");

        // Read count
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (count < 0)
            throw new ResourceFormatException($"Invalid NGMP entry count: {count}");

        // Validate we have enough data for all entries (16 bytes each: 2x ulong)
        int requiredLength = 8 + (count * 16);
        if (data.Length < requiredLength)
            throw new ResourceFormatException($"NGMP data too short. Expected {requiredLength} bytes, got {data.Length}.");

        _entries.Clear();
        _entries.EnsureCapacity(count);

        // Read entries
        for (int i = 0; i < count; i++)
        {
            ulong nameHash = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            _entries.Add(new NgmpPair(nameHash, instance));
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Header (8 bytes) + entries (16 bytes each)
        int totalSize = 8 + (_entries.Count * 16);
        var buffer = new byte[totalSize];
        int offset = 0;

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), _entries.Count);
        offset += 4;

        // Write entries
        foreach (var entry in _entries)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), entry.NameHash);
            offset += 8;

            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), entry.Instance);
            offset += 8;
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = CurrentVersion;
        _entries.Clear();
    }

    /// <summary>
    /// Adds a new hash-instance pair.
    /// </summary>
    public void Add(ulong nameHash, ulong instance)
    {
        _entries.Add(new NgmpPair(nameHash, instance));
        OnChanged();
    }

    /// <summary>
    /// Removes all entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        OnChanged();
    }

    /// <summary>
    /// Gets the number of entries.
    /// </summary>
    public int Count => _entries.Count;
}

/// <summary>
/// A hash-to-instance pair in an NGMP resource.
/// </summary>
/// <param name="NameHash">The name hash (FNV64).</param>
/// <param name="Instance">The instance ID.</param>
public readonly record struct NgmpPair(ulong NameHash, ulong Instance);
