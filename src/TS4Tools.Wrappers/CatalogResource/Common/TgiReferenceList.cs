using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A TGI reference stored in ITG order (Instance, Type, Group).
/// This is the standard order for catalog resources.
/// </summary>
public readonly record struct TgiReference(ulong Instance, uint Type, uint Group)
{
    /// <summary>
    /// The size of a serialized TGI reference in bytes.
    /// </summary>
    public const int SerializedSize = 16; // ulong + uint + uint

    /// <summary>
    /// An empty/null TGI reference.
    /// </summary>
    public static readonly TgiReference Empty = new(0, 0, 0);

    /// <summary>
    /// Parses a TGI reference from binary data in ITG order.
    /// </summary>
    public static TgiReference Parse(ReadOnlySpan<byte> data)
    {
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data);
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        return new TgiReference(instance, type, group);
    }

    /// <summary>
    /// Writes the TGI reference to a buffer in ITG order.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, Instance);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], Type);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[12..], Group);
    }

    /// <summary>
    /// Converts to a ResourceKey.
    /// </summary>
    public ResourceKey ToResourceKey() => new(Type, Group, Instance);

    /// <summary>
    /// Creates from a ResourceKey.
    /// </summary>
    public static TgiReference FromResourceKey(ResourceKey key) =>
        new(key.Instance, key.ResourceType, key.ResourceGroup);

    /// <inheritdoc/>
    public override string ToString() =>
        $"0x{Type:X8}:0x{Group:X8}:0x{Instance:X16}";
}

/// <summary>
/// A list of TGI references with a byte count prefix.
/// Used for ProductStyles in CatalogCommon.
/// Source: CatalogCommon.cs lines 351-352
/// </summary>
public sealed class TgiReferenceList
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
    public TgiReferenceList()
    {
    }

    /// <summary>
    /// Parses a TGI reference list from binary data.
    /// Uses a byte count prefix.
    /// </summary>
    public static TgiReferenceList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];

        var list = new TgiReferenceList();
        list._references.Capacity = count;

        for (int i = 0; i < count; i++)
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
    public int GetSerializedSize() => 1 + (_references.Count * TgiReference.SerializedSize);

    /// <summary>
    /// Writes the reference list to a buffer.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = (byte)_references.Count;

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
    /// Gets or sets a reference by index.
    /// </summary>
    public TgiReference this[int index]
    {
        get => _references[index];
        set => _references[index] = value;
    }
}
