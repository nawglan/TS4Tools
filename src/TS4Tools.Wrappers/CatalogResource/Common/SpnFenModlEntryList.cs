using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A list of SpnFen MODL entries, each containing a ushort label and a TGI reference.
/// Used in spindle (CSPN) and fence (CFEN) catalog resources.
/// Source: CatalogCommon.cs lines 977-1023 (SpnFenMODLEntryList class)
/// </summary>
public sealed class SpnFenModlEntryList
{
    private readonly List<SpnFenModlEntry> _entries = [];

    /// <summary>
    /// The entries.
    /// </summary>
    public IReadOnlyList<SpnFenModlEntry> Entries => _entries;

    /// <summary>
    /// Number of entries.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Creates an empty entry list.
    /// </summary>
    public SpnFenModlEntryList()
    {
    }

    /// <summary>
    /// Parses a SpnFenModlEntryList from binary data.
    /// </summary>
    /// <param name="data">The data span positioned at the entry list.</param>
    /// <param name="bytesRead">The number of bytes consumed.</param>
    /// <returns>The parsed entry list.</returns>
    public static SpnFenModlEntryList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];

        var entryList = new SpnFenModlEntryList();
        entryList._entries.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            ushort label = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;

            var reference = TgiReference.Parse(data[offset..]);
            offset += TgiReference.SerializedSize;

            entryList._entries.Add(new SpnFenModlEntry(label, reference));
        }

        bytesRead = offset;
        return entryList;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 1 + (_entries.Count * (2 + TgiReference.SerializedSize));

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
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], entry.Label);
            offset += 2;

            entry.Reference.WriteTo(buffer[offset..]);
            offset += TgiReference.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Adds an entry.
    /// </summary>
    public void Add(SpnFenModlEntry entry) => _entries.Add(entry);

    /// <summary>
    /// Adds an entry with specified label and reference.
    /// </summary>
    public void Add(ushort label, TgiReference reference) => _entries.Add(new SpnFenModlEntry(label, reference));

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void Clear() => _entries.Clear();

    /// <summary>
    /// Gets or sets an entry by index.
    /// </summary>
    public SpnFenModlEntry this[int index]
    {
        get => _entries[index];
        set => _entries[index] = value;
    }
}

/// <summary>
/// A single SpnFen MODL entry containing a label and TGI reference.
/// </summary>
/// <param name="Label">The label value.</param>
/// <param name="Reference">The TGI reference.</param>
public readonly record struct SpnFenModlEntry(ushort Label, TgiReference Reference);
