using System.Buffers.Binary;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A list of TGI references with a uint32 count prefix.
/// Used for CountedTGIBlockList patterns in catalog resources like CTPT.
/// Source: CTPTResource.cs lines 119-120, DependentList.cs lines 124-127
/// </summary>
public sealed class TgiReferenceList32
{
    private readonly List<TgiReference> _references = [];

    /// <summary>
    /// The TGI references.
    /// </summary>
    public IReadOnlyList<TgiReference> References => _references;

    /// <summary>
    /// Number of references.
    /// </summary>
    public int Count => _references.Count;

    /// <summary>
    /// Creates an empty reference list.
    /// </summary>
    public TgiReferenceList32()
    {
    }

    /// <summary>
    /// Parses a TGI reference list from binary data.
    /// Uses a uint32 count prefix.
    /// </summary>
    public static TgiReferenceList32 Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate count to prevent allocation attacks
        // Legacy code uses Convert.ToUInt16() which suggests max 65535 is reasonable
        if (count > ushort.MaxValue)
        {
            throw new InvalidDataException($"Material list count too large: {count}");
        }

        var list = new TgiReferenceList32();
        list._references.Capacity = (int)count;

        for (uint i = 0; i < count; i++)
        {
            var reference = TgiReference.Parse(data[offset..]);
            offset += TgiReference.SerializedSize;
            list._references.Add(reference);
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 4 + (_references.Count * TgiReference.SerializedSize);

    /// <summary>
    /// Writes the reference list to a buffer.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], (uint)_references.Count);
        offset += 4;

        foreach (var reference in _references)
        {
            reference.WriteTo(buffer[offset..]);
            offset += TgiReference.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Adds a reference.
    /// </summary>
    public void Add(TgiReference reference) => _references.Add(reference);

    /// <summary>
    /// Clears all references.
    /// </summary>
    public void Clear() => _references.Clear();

    /// <summary>
    /// Removes a reference at the specified index.
    /// </summary>
    public void RemoveAt(int index) => _references.RemoveAt(index);

    /// <summary>
    /// Gets or sets a reference by index.
    /// </summary>
    public TgiReference this[int index]
    {
        get => _references[index];
        set => _references[index] = value;
    }
}
