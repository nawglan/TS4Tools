
namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A list of uint32 values with a uint16 count prefix.
/// Used in fence (CFEN) catalog resources.
/// Source: CFENResource.cs lines 251-279 (WhateverList class)
/// </summary>
public sealed class UInt16CountedUInt32List
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
    public UInt16CountedUInt32List()
    {
    }

    /// <summary>
    /// Creates a list from existing values.
    /// </summary>
    public UInt16CountedUInt32List(IEnumerable<uint> values)
    {
        _values.AddRange(values);
    }

    /// <summary>
    /// Parses a list from binary data.
    /// </summary>
    /// <param name="data">The data span positioned at the list.</param>
    /// <param name="bytesRead">The number of bytes consumed.</param>
    /// <returns>The parsed list.</returns>
    public static UInt16CountedUInt32List Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        ushort count = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        if (count > 10000)
            throw new InvalidOperationException($"Unreasonable list count: {count}");

        var list = new UInt16CountedUInt32List();
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
    public int GetSerializedSize() => 2 + (_values.Count * 4);

    /// <summary>
    /// Writes the list to a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], (ushort)_values.Count);
        offset += 2;

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
