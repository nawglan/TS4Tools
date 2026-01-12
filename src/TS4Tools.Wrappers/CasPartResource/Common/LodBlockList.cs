namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A list of LOD info entries with byte count prefix.
/// Source: legacy_references/.../Lists/LODBlockList.cs
/// </summary>
public sealed class LodBlockList : List<LodInfoEntry>
{

    /// <summary>
    /// Creates an empty LodBlockList.
    /// </summary>
    public LodBlockList()
    {
    }

    /// <summary>
    /// Creates a LodBlockList with the specified entries.
    /// </summary>
    public LodBlockList(IEnumerable<LodInfoEntry> entries) : base(entries)
    {
    }

    /// <summary>
    /// Parses a LodBlockList from a span.
    /// Format: byte count + (count * LodInfoEntry)
    /// </summary>
    public static LodBlockList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];
        var list = new LodBlockList();
        list.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            if (offset >= data.Length)
            {
                throw new InvalidDataException($"Truncated LOD block data at index {i}");
            }

            var entry = LodInfoEntry.Parse(data[offset..], out int entryBytes);
            list.Add(entry);
            offset += entryBytes;
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Writes this list to a span.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        if (Count > 255)
        {
            throw new InvalidOperationException($"LodBlockList count {Count} exceeds byte max (255)");
        }

        int offset = 0;
        buffer[offset++] = (byte)Count;

        foreach (var entry in this)
        {
            offset += entry.WriteTo(buffer[offset..]);
        }

        return offset;
    }

    /// <summary>
    /// Gets the serialized size in bytes.
    /// </summary>
    public int GetSerializedSize()
    {
        int size = 1; // count byte
        foreach (var entry in this)
        {
            size += entry.GetSerializedSize();
        }
        return size;
    }
}
