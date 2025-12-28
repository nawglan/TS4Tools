using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A list of CBLK entries, each containing 3 bytes.
/// Source: CBLKResource.cs lines 16-74 (CBLKEntryList class)
/// </summary>
public sealed class CblkEntryList
{
    private readonly List<CblkEntry> _entries = [];

    /// <summary>
    /// The entries.
    /// </summary>
    public IReadOnlyList<CblkEntry> Entries => _entries;

    /// <summary>
    /// Number of entries.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Creates an empty entry list.
    /// </summary>
    public CblkEntryList()
    {
    }

    /// <summary>
    /// Parses a CblkEntryList from binary data.
    /// </summary>
    /// <param name="data">The data span positioned at the entry list.</param>
    /// <param name="bytesRead">The number of bytes consumed.</param>
    /// <returns>The parsed entry list.</returns>
    public static CblkEntryList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];

        var entryList = new CblkEntryList();
        entryList._entries.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            var entry = new CblkEntry(
                data[offset],
                data[offset + 1],
                data[offset + 2]);
            offset += 3;
            entryList._entries.Add(entry);
        }

        bytesRead = offset;
        return entryList;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 1 + (_entries.Count * 3);

    /// <summary>
    /// Writes the entry list to a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = (byte)_entries.Count;

        foreach (var entry in _entries)
        {
            buffer[offset] = entry.Byte1;
            buffer[offset + 1] = entry.Byte2;
            buffer[offset + 2] = entry.Byte3;
            offset += 3;
        }

        return offset;
    }

    /// <summary>
    /// Adds an entry.
    /// </summary>
    public void Add(CblkEntry entry) => _entries.Add(entry);

    /// <summary>
    /// Adds an entry with specified bytes.
    /// </summary>
    public void Add(byte b1, byte b2, byte b3) => _entries.Add(new CblkEntry(b1, b2, b3));

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void Clear() => _entries.Clear();

    /// <summary>
    /// Gets or sets an entry by index.
    /// </summary>
    public CblkEntry this[int index]
    {
        get => _entries[index];
        set => _entries[index] = value;
    }
}

/// <summary>
/// A single CBLK entry containing 3 bytes.
/// </summary>
/// <param name="Byte1">First byte.</param>
/// <param name="Byte2">Second byte.</param>
/// <param name="Byte3">Third byte.</param>
public readonly record struct CblkEntry(byte Byte1, byte Byte2, byte Byte3);
