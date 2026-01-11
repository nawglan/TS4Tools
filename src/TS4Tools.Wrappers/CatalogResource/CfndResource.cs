using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Foundation (CFND) resource.
/// Used for building foundations.
/// Resource Type: 0x2FAE983E
/// Source: CFNDResource.cs lines 27-199
/// </summary>
public sealed class CfndResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CFND resources.
    /// </summary>
    public const uint TypeId = 0x2FAE983E;

    /// <summary>
    /// Default version for CFND resources.
    /// </summary>
    public new const uint DefaultVersion = 0x06;

    #region Properties

    /// <summary>
    /// Unknown byte 1.
    /// </summary>
    public byte Unk01 { get; set; }

    /// <summary>
    /// Unknown byte 2.
    /// </summary>
    public byte Unk02 { get; set; }

    /// <summary>
    /// Reference to the first model resource (MODL type).
    /// </summary>
    public TgiReference ModlRef1 { get; set; }

    /// <summary>
    /// Material variant hash.
    /// </summary>
    public uint MaterialVariant { get; set; }

    /// <summary>
    /// Swatch grouping instance ID for color variants.
    /// </summary>
    public ulong SwatchGrouping { get; set; }

    /// <summary>
    /// Float value 1 (purpose unknown).
    /// </summary>
    public float Float1 { get; set; }

    /// <summary>
    /// Float value 2 (purpose unknown).
    /// </summary>
    public float Float2 { get; set; }

    /// <summary>
    /// Reference to the trim resource (TRIM type).
    /// </summary>
    public TgiReference TrimRef { get; set; }

    /// <summary>
    /// Reference to the second model resource (MODL type).
    /// </summary>
    public TgiReference ModlRef2 { get; set; }

    /// <summary>
    /// Color list (ARGB values).
    /// </summary>
    public ColorList Colors { get; set; } = new();

    #endregion

    /// <summary>
    /// Creates a new CFND resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CfndResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CFNDResource.cs lines 144-159
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Unk01 (byte)
        Unk01 = data[offset++];

        // Unk02 (byte)
        Unk02 = data[offset++];

        // ModlRef1 (ITG order, 16 bytes)
        ModlRef1 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // MaterialVariant (uint)
        MaterialVariant = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // SwatchGrouping (ulong)
        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Float1 (float)
        Float1 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        // Float2 (float)
        Float2 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        // TrimRef (ITG order, 16 bytes)
        TrimRef = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // ModlRef2 (ITG order, 16 bytes)
        ModlRef2 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // Colors (byte count + uint values)
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CFNDResource.cs lines 161-182
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Unk01
        buffer[offset++] = Unk01;

        // Unk02
        buffer[offset++] = Unk02;

        // ModlRef1
        ModlRef1.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // MaterialVariant
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], MaterialVariant);
        offset += 4;

        // SwatchGrouping
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;

        // Float1
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], Float1);
        offset += 4;

        // Float2
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], Float2);
        offset += 4;

        // TrimRef
        TrimRef.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // ModlRef2
        ModlRef2.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // Colors
        offset += Colors.WriteTo(buffer[offset..]);
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // Unk01 (1) + Unk02 (1) = 2
        size += 2;

        // ModlRef1 (16)
        size += TgiReference.SerializedSize;

        // MaterialVariant (4) + SwatchGrouping (8) + Float1 (4) + Float2 (4) = 20
        size += 20;

        // TrimRef (16) + ModlRef2 (16) = 32
        size += TgiReference.SerializedSize * 2;

        // Colors (1 + 4*count)
        size += Colors.GetSerializedSize();

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        Unk01 = 0;
        Unk02 = 0;
        ModlRef1 = TgiReference.Empty;
        MaterialVariant = 0;
        SwatchGrouping = 0;
        Float1 = 0;
        Float2 = 0;
        TrimRef = TgiReference.Empty;
        ModlRef2 = TgiReference.Empty;
        Colors = new ColorList();
    }
}
