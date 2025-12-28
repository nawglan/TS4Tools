namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A list of swatch colors with byte count prefix.
/// Source: legacy_references/.../Lists/SwatchColorList.cs
/// </summary>
public sealed class SwatchColorList : List<SwatchColor>
{

    /// <summary>
    /// Creates an empty SwatchColorList.
    /// </summary>
    public SwatchColorList()
    {
    }

    /// <summary>
    /// Creates a SwatchColorList with the specified colors.
    /// </summary>
    public SwatchColorList(IEnumerable<SwatchColor> colors) : base(colors)
    {
    }

    /// <summary>
    /// Parses a SwatchColorList from a span.
    /// Format: byte count + (count * 4 bytes)
    /// </summary>
    public static SwatchColorList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];
        var list = new SwatchColorList();
        list.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            if (offset + SwatchColor.SerializedSize > data.Length)
            {
                throw new InvalidDataException($"Truncated swatch color data at index {i}");
            }

            list.Add(SwatchColor.Parse(data[offset..]));
            offset += SwatchColor.SerializedSize;
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
            throw new InvalidOperationException($"SwatchColorList count {Count} exceeds byte max (255)");
        }

        int offset = 0;
        buffer[offset++] = (byte)Count;

        foreach (var color in this)
        {
            color.WriteTo(buffer[offset..]);
            offset += SwatchColor.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Gets the serialized size in bytes.
    /// </summary>
    public int GetSerializedSize() => 1 + (Count * SwatchColor.SerializedSize);
}
