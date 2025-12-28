namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A list of CAS part overrides with byte count prefix.
/// Source: legacy_references/.../Lists/OverrideList.cs
/// </summary>
public sealed class CaspOverrideList : List<CaspOverride>
{

    /// <summary>
    /// Creates an empty CaspOverrideList.
    /// </summary>
    public CaspOverrideList()
    {
    }

    /// <summary>
    /// Creates a CaspOverrideList with the specified overrides.
    /// </summary>
    public CaspOverrideList(IEnumerable<CaspOverride> overrides) : base(overrides)
    {
    }

    /// <summary>
    /// Parses a CaspOverrideList from a span.
    /// Format: byte count + (count * 5 bytes)
    /// </summary>
    public static CaspOverrideList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];
        var list = new CaspOverrideList();
        list.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            if (offset + CaspOverride.SerializedSize > data.Length)
            {
                throw new InvalidDataException($"Truncated override data at index {i}");
            }

            list.Add(CaspOverride.Parse(data[offset..]));
            offset += CaspOverride.SerializedSize;
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
            throw new InvalidOperationException($"CaspOverrideList count {Count} exceeds byte max (255)");
        }

        int offset = 0;
        buffer[offset++] = (byte)Count;

        foreach (var item in this)
        {
            item.WriteTo(buffer[offset..]);
            offset += CaspOverride.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Gets the serialized size in bytes.
    /// </summary>
    public int GetSerializedSize() => 1 + (Count * CaspOverride.SerializedSize);
}
