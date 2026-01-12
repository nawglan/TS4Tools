using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Styled Room (STRM) resource.
/// Used for styled room presets.
/// Resource Type: 0x74050B1F
/// Source: STRMResource.cs lines 27-199
/// </summary>
public sealed class StrmResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for STRM resources.
    /// </summary>
    public const uint TypeId = 0x74050B1F;

    /// <summary>
    /// Default version for STRM resources.
    /// </summary>
    public new const uint DefaultVersion = 0x06;

    #region Properties

    /// <summary>
    /// Unknown field 1.
    /// </summary>
    public uint Unk01 { get; set; }

    /// <summary>
    /// Unknown field 2.
    /// </summary>
    public uint Unk02 { get; set; }

    /// <summary>
    /// Unknown field 3.
    /// </summary>
    public uint Unk03 { get; set; }

    /// <summary>
    /// Unknown field 4.
    /// </summary>
    public uint Unk04 { get; set; }

    /// <summary>
    /// Unknown field 5.
    /// </summary>
    public uint Unk05 { get; set; }

    /// <summary>
    /// Swatch grouping instance ID for color variants.
    /// </summary>
    public ulong SwatchGrouping { get; set; }

    /// <summary>
    /// Color list (ARGB values).
    /// </summary>
    public ColorList Colors { get; set; } = new();

    /// <summary>
    /// Unknown TGI reference.
    /// </summary>
    public TgiReference UnkRef1 { get; set; }

    /// <summary>
    /// Unknown field 6.
    /// </summary>
    public uint Unk06 { get; set; }

    /// <summary>
    /// Unknown field 7.
    /// </summary>
    public uint Unk07 { get; set; }

    #endregion

    /// <summary>
    /// Creates a new STRM resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public StrmResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: STRMResource.cs lines 145-160
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Unk01 (uint)
        Unk01 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk02 (uint)
        Unk02 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk03 (uint)
        Unk03 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk04 (uint)
        Unk04 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk05 (uint)
        Unk05 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // SwatchGrouping (ulong)
        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Colors (byte count + uint values)
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;

        // UnkRef1 (ITG order, 16 bytes)
        UnkRef1 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // Unk06 (uint)
        Unk06 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk07 (uint)
        Unk07 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: STRMResource.cs lines 162-182
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Unk01
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk01);
        offset += 4;

        // Unk02
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk02);
        offset += 4;

        // Unk03
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk03);
        offset += 4;

        // Unk04
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk04);
        offset += 4;

        // Unk05
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk05);
        offset += 4;

        // SwatchGrouping
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;

        // Colors
        offset += Colors.WriteTo(buffer[offset..]);

        // UnkRef1
        UnkRef1.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // Unk06
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk06);
        offset += 4;

        // Unk07
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk07);
        offset += 4;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // Unk01-05 (5 * 4 = 20)
        size += 20;

        // SwatchGrouping (8)
        size += 8;

        // Colors (1 + 4*count)
        size += Colors.GetSerializedSize();

        // UnkRef1 (16)
        size += TgiReference.SerializedSize;

        // Unk06 + Unk07 (8)
        size += 8;

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        Unk01 = 0;
        Unk02 = 0;
        Unk03 = 0;
        Unk04 = 0;
        Unk05 = 0;
        SwatchGrouping = 0;
        Colors = new ColorList();
        UnkRef1 = TgiReference.Empty;
        Unk06 = 0;
        Unk07 = 0;
    }
}
