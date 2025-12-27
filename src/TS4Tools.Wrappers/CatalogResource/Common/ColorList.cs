using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A list of ARGB color values used in catalog resources.
/// Uses a byte count prefix followed by uint32 ARGB values.
/// Source: CatalogCommon.cs lines 709-768
/// </summary>
public sealed class ColorList
{
    private readonly List<uint> _colors = [];

    /// <summary>
    /// The colors in ARGB format.
    /// </summary>
    public IReadOnlyList<uint> Colors => _colors;

    /// <summary>
    /// Number of colors.
    /// </summary>
    public int Count => _colors.Count;

    /// <summary>
    /// Creates an empty color list.
    /// </summary>
    public ColorList()
    {
    }

    /// <summary>
    /// Creates a color list from existing colors.
    /// </summary>
    public ColorList(IEnumerable<uint> colors)
    {
        _colors.AddRange(colors);
    }

    /// <summary>
    /// Parses a color list from binary data.
    /// </summary>
    /// <param name="data">The data span positioned at the color list.</param>
    /// <param name="bytesRead">The number of bytes consumed.</param>
    /// <returns>The parsed color list.</returns>
    public static ColorList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];

        var colorList = new ColorList();
        colorList._colors.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            uint color = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            colorList._colors.Add(color);
        }

        bytesRead = offset;
        return colorList;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 1 + (_colors.Count * 4);

    /// <summary>
    /// Writes the color list to a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = (byte)_colors.Count;

        foreach (uint color in _colors)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], color);
            offset += 4;
        }

        return offset;
    }

    /// <summary>
    /// Adds a color.
    /// </summary>
    public void Add(uint argb) => _colors.Add(argb);

    /// <summary>
    /// Clears all colors.
    /// </summary>
    public void Clear() => _colors.Clear();

    /// <summary>
    /// Gets or sets a color by index.
    /// </summary>
    public uint this[int index]
    {
        get => _colors[index];
        set => _colors[index] = value;
    }
}
