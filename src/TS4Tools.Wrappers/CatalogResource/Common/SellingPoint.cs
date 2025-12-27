using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A selling point entry with commodity tag and amount.
/// Source: CatalogCommon.cs lines 596-701
/// </summary>
public readonly record struct SellingPoint(ushort CommodityTag, int Amount)
{
    /// <summary>
    /// The size of a serialized selling point in bytes.
    /// </summary>
    public const int SerializedSize = 6; // ushort + int

    /// <summary>
    /// Parses a selling point from binary data.
    /// </summary>
    public static SellingPoint Parse(ReadOnlySpan<byte> data)
    {
        ushort commodity = BinaryPrimitives.ReadUInt16LittleEndian(data);
        int amount = BinaryPrimitives.ReadInt32LittleEndian(data[2..]);
        return new SellingPoint(commodity, amount);
    }

    /// <summary>
    /// Writes the selling point to a buffer.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, CommodityTag);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[2..], Amount);
    }
}

/// <summary>
/// A list of selling points.
/// Source: CatalogCommon.cs lines 568-594
/// </summary>
public sealed class SellingPointList
{
    private readonly List<SellingPoint> _points = [];

    /// <summary>
    /// The selling points.
    /// </summary>
    public IReadOnlyList<SellingPoint> Points => _points;

    /// <summary>
    /// Number of selling points.
    /// </summary>
    public int Count => _points.Count;

    /// <summary>
    /// Creates an empty selling point list.
    /// </summary>
    public SellingPointList()
    {
    }

    /// <summary>
    /// Parses a selling point list from binary data.
    /// Uses uint32 count prefix.
    /// </summary>
    public static SellingPointList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data);
        offset += 4;

        if (count > 1000)
            throw new InvalidOperationException($"Unreasonable selling point count: {count}");

        var list = new SellingPointList();
        list._points.Capacity = (int)count;

        for (uint i = 0; i < count; i++)
        {
            var point = SellingPoint.Parse(data[offset..]);
            offset += SellingPoint.SerializedSize;
            list._points.Add(point);
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 4 + (_points.Count * SellingPoint.SerializedSize);

    /// <summary>
    /// Writes the selling point list to a buffer.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)_points.Count);
        offset += 4;

        foreach (var point in _points)
        {
            point.WriteTo(buffer[offset..]);
            offset += SellingPoint.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Adds a selling point.
    /// </summary>
    public void Add(SellingPoint point) => _points.Add(point);

    /// <summary>
    /// Clears all selling points.
    /// </summary>
    public void Clear() => _points.Clear();
}
