using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// A TGI block stored in IGT order (Instance, Group, Type).
/// This is the order used by CAS Part resources.
/// Source: legacy uses "IGT" tag for TGI blocks in CASPartResource
/// </summary>
public readonly record struct CaspTgiBlock(ulong Instance, uint Group, uint Type)
{
    /// <summary>
    /// The size of a serialized TGI block in bytes.
    /// </summary>
    public const int SerializedSize = 16; // ulong + uint + uint

    /// <summary>
    /// An empty/null TGI block.
    /// </summary>
    public static readonly CaspTgiBlock Empty = new(0, 0, 0);

    /// <summary>
    /// Parses a TGI block from binary data in IGT order.
    /// </summary>
    public static CaspTgiBlock Parse(ReadOnlySpan<byte> data)
    {
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data);
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        return new CaspTgiBlock(instance, group, type);
    }

    /// <summary>
    /// Writes the TGI block to a buffer in IGT order.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, Instance);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], Group);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[12..], Type);
    }

    /// <summary>
    /// Converts to a ResourceKey.
    /// </summary>
    public ResourceKey ToResourceKey() => new(Type, Group, Instance);

    /// <summary>
    /// Creates from a ResourceKey.
    /// </summary>
    public static CaspTgiBlock FromResourceKey(ResourceKey key) =>
        new(key.Instance, key.ResourceGroup, key.ResourceType);

    /// <inheritdoc/>
    public override string ToString() =>
        $"0x{Type:X8}:0x{Group:X8}:0x{Instance:X16}";
}

/// <summary>
/// A list of TGI blocks with a byte count prefix.
/// Used by CAS Part resources.
/// </summary>
public sealed class CaspTgiBlockList : List<CaspTgiBlock>
{

    /// <summary>
    /// Creates an empty TGI block list.
    /// </summary>
    public CaspTgiBlockList()
    {
    }

    /// <summary>
    /// Creates a TGI block list with the specified blocks.
    /// </summary>
    public CaspTgiBlockList(IEnumerable<CaspTgiBlock> blocks) : base(blocks)
    {
    }

    /// <summary>
    /// Parses a TGI block list from binary data.
    /// Format: byte count + (count * 16 bytes)
    /// </summary>
    public static CaspTgiBlockList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];
        var list = new CaspTgiBlockList();
        list.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            if (offset + CaspTgiBlock.SerializedSize > data.Length)
            {
                throw new InvalidDataException($"Truncated TGI block data at index {i}");
            }

            list.Add(CaspTgiBlock.Parse(data[offset..]));
            offset += CaspTgiBlock.SerializedSize;
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
            throw new InvalidOperationException($"TGI block count {Count} exceeds byte max (255)");
        }

        int offset = 0;
        buffer[offset++] = (byte)Count;

        foreach (var block in this)
        {
            block.WriteTo(buffer[offset..]);
            offset += CaspTgiBlock.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Gets the serialized size in bytes.
    /// </summary>
    public int GetSerializedSize() => 1 + (Count * CaspTgiBlock.SerializedSize);
}
