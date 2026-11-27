// Source: legacy_references/Sims4Tools/s4pi Wrappers/NameMapResource/NameMapResource.cs

using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Name Map resource containing hash-to-name mappings.
/// Resource Type: 0x0166038C
/// </summary>
/// <remarks>
/// Source: s4pi Wrappers/NameMapResource/NameMapResource.cs
/// Format: version (uint32), count (int32), entries (ulong hash, int32 length, char[] name).
/// Names are BinaryReader/BinaryWriter chars, i.e. UTF-8 encoded with the length counted in chars
/// (NameMapResource.cs lines 60-61 ReadChars, lines 75-76 Write(char[])).
/// Handler TypeId: 0x0166038C
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Domain-specific resource type, not a generic collection")]
public sealed class NameMapResource : TypedResource, IDictionary<ulong, string>
{
    /// <summary>
    /// Current format version.
    /// </summary>
    public const uint CurrentVersion = 1;

    private readonly Dictionary<ulong, string> _entries = [];

    /// <summary>
    /// Format version (should be 1).
    /// </summary>
    public uint Version { get; private set; } = CurrentVersion;

    /// <summary>
    /// Number of entries in this name map.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Creates a new NameMapResource by parsing data.
    /// </summary>
    public NameMapResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
            throw new ResourceFormatException("NameMap data too short for header.");

        int offset = 0;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (Version != CurrentVersion)
            throw new ResourceFormatException($"Unsupported NameMap version: {Version}. Expected {CurrentVersion}.");

        int entryCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Each entry is at least 12 bytes (hash + string length)
        CountGuard.Check(entryCount, data.Length - offset, 12, "NameMap entry");

        _entries.Clear();
        _entries.EnsureCapacity(entryCount);

        // Read entries
        for (int i = 0; i < entryCount; i++)
        {
            if (offset + 12 > data.Length)
                throw new ResourceFormatException($"Unexpected end of NameMap data at entry {i}");

            ulong hash = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            int stringLength = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
            offset += 4;

            // Each char takes at least one UTF-8 byte.
            if (stringLength < 0 || stringLength > data.Length - offset)
                throw new ResourceFormatException($"Invalid string length at entry {i}: {stringLength}");

            _entries[hash] = ReadChars(data, ref offset, stringLength, i);
        }
    }

    /// <summary>
    /// Reads <paramref name="charCount"/> UTF-8 encoded chars, like legacy <c>BinaryReader.ReadChars</c>
    /// (NameMapResource.cs line 61).
    /// </summary>
    private static string ReadChars(ReadOnlySpan<byte> data, ref int offset, int charCount, int entry)
    {
        if (charCount == 0)
            return string.Empty;

        var decoder = Encoding.UTF8.GetDecoder();
        var chars = new char[charCount];
        int produced = 0;
        Span<char> one = stackalloc char[2];
        while (produced < charCount)
        {
            if (offset >= data.Length)
                throw new ResourceFormatException($"Unexpected end of NameMap data in name of entry {entry}");

            int n = decoder.GetChars(data.Slice(offset++, 1), one, flush: false);
            if (produced + n > charCount)
                throw new ResourceFormatException($"Name of entry {entry} ends inside a surrogate pair");
            one[..n].CopyTo(chars.AsSpan(produced));
            produced += n;
        }

        return new string(chars);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Calculate total size
        int totalCharBytes = 0;
        foreach (var value in _entries.Values)
        {
            totalCharBytes += Encoding.UTF8.GetByteCount(value);
        }

        // Header (8 bytes) + entries (12 bytes each + char data)
        int totalSize = 8 + (_entries.Count * 12) + totalCharBytes;
        var buffer = new byte[totalSize];
        int offset = 0;

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), _entries.Count);
        offset += 4;

        // Write entries
        foreach (var (hash, name) in _entries)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), hash);
            offset += 8;

            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), name.Length);
            offset += 4;

            // Source: NameMapResource.cs line 76 - BinaryWriter.Write(char[]) writes UTF-8
            offset += Encoding.UTF8.GetBytes(name, buffer.AsSpan(offset));
        }

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = CurrentVersion;
        _entries.Clear();
    }

    #region IDictionary<ulong, string> Implementation

    /// <inheritdoc/>
    public ICollection<ulong> Keys => _entries.Keys;

    /// <inheritdoc/>
    public ICollection<string> Values => _entries.Values;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public string this[ulong key]
    {
        get => _entries[key];
        set
        {
            _entries[key] = value;
            OnChanged();
        }
    }

    /// <inheritdoc/>
    public void Add(ulong key, string value)
    {
        _entries.Add(key, value);
        OnChanged();
    }

    /// <inheritdoc/>
    public bool ContainsKey(ulong key) => _entries.ContainsKey(key);

    /// <inheritdoc/>
    public bool Remove(ulong key)
    {
        if (_entries.Remove(key))
        {
            OnChanged();
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool TryGetValue(ulong key, out string value) => _entries.TryGetValue(key, out value!);

    /// <inheritdoc/>
    public void Add(KeyValuePair<ulong, string> item)
    {
        ((ICollection<KeyValuePair<ulong, string>>)_entries).Add(item);
        OnChanged();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _entries.Clear();
        OnChanged();
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<ulong, string> item) =>
        ((ICollection<KeyValuePair<ulong, string>>)_entries).Contains(item);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<ulong, string>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<ulong, string>>)_entries).CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<ulong, string> item)
    {
        if (((ICollection<KeyValuePair<ulong, string>>)_entries).Remove(item))
        {
            OnChanged();
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<ulong, string>> GetEnumerator() => _entries.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
