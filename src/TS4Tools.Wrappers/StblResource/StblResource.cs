using System.Buffers.Binary;
using System.Text;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// String Table (STBL) resource containing localized strings.
/// Resource Type: 0x220557DA
/// </summary>
[ResourceHandler(0x220557DA)]
public sealed class StblResource : TypedResource
{
    /// <summary>
    /// Magic bytes: "STBL" in little-endian.
    /// </summary>
    public const uint Magic = 0x4C425453; // "STBL"

    /// <summary>
    /// Current format version.
    /// </summary>
    public const ushort CurrentVersion = 5;

    private readonly List<StringEntry> _entries = [];

    /// <summary>
    /// Format version (should be 5 for Sims 4).
    /// </summary>
    public ushort Version { get; private set; } = CurrentVersion;

    /// <summary>
    /// Compression flag (typically 0).
    /// </summary>
    public byte IsCompressed { get; private set; }

    /// <summary>
    /// Reserved bytes preserved from parsing.
    /// </summary>
    public byte[] Reserved { get; private set; } = new byte[2];

    /// <summary>
    /// The string entries in this table.
    /// </summary>
    public IReadOnlyList<StringEntry> Entries => _entries;

    /// <summary>
    /// Number of entries in this string table.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Creates a new STBL resource by parsing data.
    /// </summary>
    public StblResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 21)
            throw new ResourceFormatException("STBL data too short for header.");

        int offset = 0;

        // Read header
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (magic != Magic)
            throw new ResourceFormatException($"Invalid STBL magic: expected 0x{Magic:X8}, got 0x{magic:X8}");

        Version = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        if (Version != CurrentVersion)
            throw new ResourceFormatException($"Unsupported STBL version: {Version}. Expected {CurrentVersion}.");

        IsCompressed = data[offset++];

        ulong entryCount = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        Reserved = data.Slice(offset, 2).ToArray();
        offset += 2;

        uint stringDataLength = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate entry count
        if (entryCount > int.MaxValue)
            throw new ResourceFormatException($"STBL entry count too large: {entryCount}");

        _entries.Clear();
        _entries.Capacity = (int)entryCount;

        // Read entries
        for (ulong i = 0; i < entryCount; i++)
        {
            if (offset + 7 > data.Length)
                throw new ResourceFormatException($"Unexpected end of STBL data at entry {i}");

            uint keyHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            byte flags = data[offset++];

            ushort stringLength = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;

            if (offset + stringLength > data.Length)
                throw new ResourceFormatException($"String length exceeds data at entry {i}");

            string value = Encoding.UTF8.GetString(data.Slice(offset, stringLength));
            offset += stringLength;

            _entries.Add(new StringEntry(keyHash, flags, value));
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Calculate total size
        int totalStringBytes = 0;
        foreach (var entry in _entries)
        {
            totalStringBytes += Encoding.UTF8.GetByteCount(entry.Value);
        }

        // Header (21 bytes) + entries (7 bytes each + string data)
        int totalSize = 21 + (_entries.Count * 7) + totalStringBytes;
        var buffer = new byte[totalSize];
        int offset = 0;

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Magic);
        offset += 4;

        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), Version);
        offset += 2;

        buffer[offset++] = IsCompressed;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), (ulong)_entries.Count);
        offset += 8;

        Reserved.CopyTo(buffer.AsSpan(offset));
        offset += 2;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), (uint)totalStringBytes);
        offset += 4;

        // Write entries
        foreach (var entry in _entries)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), entry.KeyHash);
            offset += 4;

            buffer[offset++] = entry.Flags;

            byte[] stringBytes = Encoding.UTF8.GetBytes(entry.Value);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), (ushort)stringBytes.Length);
            offset += 2;

            stringBytes.CopyTo(buffer.AsSpan(offset));
            offset += stringBytes.Length;
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = CurrentVersion;
        IsCompressed = 0;
        Reserved = new byte[2];
        _entries.Clear();
    }

    /// <summary>
    /// Adds a new string entry.
    /// </summary>
    /// <param name="keyHash">The FNV-32 hash of the string key.</param>
    /// <param name="value">The string value.</param>
    /// <param name="flags">Optional flags.</param>
    public void Add(uint keyHash, string value, byte flags = 0)
    {
        _entries.Add(new StringEntry(keyHash, flags, value));
        OnChanged();
    }

    /// <summary>
    /// Removes an entry by key hash.
    /// </summary>
    /// <param name="keyHash">The FNV-32 hash to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool Remove(uint keyHash)
    {
        int index = _entries.FindIndex(e => e.KeyHash == keyHash);
        if (index >= 0)
        {
            _entries.RemoveAt(index);
            OnChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tries to get a string value by key hash.
    /// </summary>
    /// <param name="keyHash">The FNV-32 hash to look up.</param>
    /// <param name="value">The string value if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    public bool TryGetValue(uint keyHash, out string? value)
    {
        foreach (var entry in _entries)
        {
            if (entry.KeyHash == keyHash)
            {
                value = entry.Value;
                return true;
            }
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Gets or sets a string value by key hash.
    /// </summary>
    /// <param name="keyHash">The FNV-32 hash.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="KeyNotFoundException">If the key hash is not found on get.</exception>
    public string this[uint keyHash]
    {
        get
        {
            if (TryGetValue(keyHash, out string? value))
                return value!;
            throw new KeyNotFoundException($"Key hash 0x{keyHash:X8} not found.");
        }
        set
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].KeyHash == keyHash)
                {
                    _entries[i] = new StringEntry(keyHash, _entries[i].Flags, value);
                    OnChanged();
                    return;
                }
            }
            // Not found, add new
            Add(keyHash, value);
        }
    }

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        OnChanged();
    }

    /// <summary>
    /// Checks if a key hash exists.
    /// </summary>
    public bool ContainsKey(uint keyHash)
    {
        return _entries.Exists(e => e.KeyHash == keyHash);
    }
}
