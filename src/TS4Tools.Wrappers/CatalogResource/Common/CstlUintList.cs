
namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A list of uint values used in CSTL resources.
/// Uses a byte count prefix followed by uint32 values.
/// Source: CSTLResource.cs lines 218-243 (UnkCSTLUintList class)
/// </summary>
public sealed class CstlUintList
{
    private readonly List<uint> _values = [];

    /// <summary>
    /// The values.
    /// </summary>
    public IReadOnlyList<uint> Values => _values;

    /// <summary>
    /// Number of values.
    /// </summary>
    public int Count => _values.Count;

    /// <summary>
    /// Creates an empty list.
    /// </summary>
    public CstlUintList()
    {
    }

    /// <summary>
    /// Creates a list from existing values.
    /// </summary>
    public CstlUintList(IEnumerable<uint> values)
    {
        _values.AddRange(values);
    }

    /// <summary>
    /// Parses a CstlUintList from binary data.
    /// </summary>
    /// <param name="data">The data span positioned at the list.</param>
    /// <param name="bytesRead">The number of bytes consumed.</param>
    /// <returns>The parsed list.</returns>
    public static CstlUintList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];

        var list = new CstlUintList();
        list._values.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            list._values.Add(value);
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 1 + (_values.Count * 4);

    /// <summary>
    /// Writes the list to a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = (byte)_values.Count;

        foreach (uint value in _values)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], value);
            offset += 4;
        }

        return offset;
    }

    /// <summary>
    /// Adds a value.
    /// </summary>
    public void Add(uint value) => _values.Add(value);

    /// <summary>
    /// Clears all values.
    /// </summary>
    public void Clear() => _values.Clear();

    /// <summary>
    /// Gets or sets a value by index.
    /// </summary>
    public uint this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }
}
