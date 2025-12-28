using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Roof Trim (CRTR) resource.
/// Used for decorative trim on roofs.
/// Resource Type: 0xB0311D0F
/// Source: CRTRResource.cs lines 27-171
/// </summary>
[ResourceHandler(0xB0311D0F)]
public sealed class CrtrResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CRTR resources.
    /// </summary>
    public const uint TypeId = 0xB0311D0F;

    #region Properties

    /// <summary>
    /// Unknown field 1.
    /// </summary>
    public byte Unk01 { get; set; }

    /// <summary>
    /// Unknown field 2.
    /// </summary>
    public byte Unk02 { get; set; }

    /// <summary>
    /// Reference to the trim resource (TRIM type).
    /// </summary>
    public TgiReference TrimRef { get; set; }

    /// <summary>
    /// Material variant hash.
    /// </summary>
    public uint MaterialVariant { get; set; }

    /// <summary>
    /// Reference to the model resource (MODL type).
    /// </summary>
    public TgiReference ModlRef { get; set; }

    /// <summary>
    /// Swatch grouping instance ID for color variants.
    /// </summary>
    public ulong SwatchGrouping { get; set; }

    /// <summary>
    /// Color list (ARGB values).
    /// </summary>
    public ColorList Colors { get; set; } = new();

    #endregion

    /// <summary>
    /// Creates a new CRTR resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CrtrResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CRTRResource.cs lines 122-134
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Unk01 (byte)
        Unk01 = data[offset++];

        // Unk02 (byte)
        Unk02 = data[offset++];

        // TRIMRef (ITG order, 16 bytes)
        TrimRef = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // MaterialVariant (uint)
        MaterialVariant = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // MODLRef (ITG order, 16 bytes)
        ModlRef = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // SwatchGrouping (ulong)
        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Colors (byte count + uint values)
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CRTRResource.cs lines 136-154
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Unk01
        buffer[offset++] = Unk01;

        // Unk02
        buffer[offset++] = Unk02;

        // TRIMRef
        TrimRef.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // MaterialVariant
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], MaterialVariant);
        offset += 4;

        // MODLRef
        ModlRef.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // SwatchGrouping
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;

        // Colors
        offset += Colors.WriteTo(buffer[offset..]);
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // Unk01 (1) + Unk02 (1) + TRIMRef (16) + MaterialVariant (4) + MODLRef (16) + SwatchGrouping (8)
        size += 1; // Unk01
        size += 1; // Unk02
        size += TgiReference.SerializedSize; // TRIMRef
        size += 4; // MaterialVariant
        size += TgiReference.SerializedSize; // MODLRef
        size += 8; // SwatchGrouping

        // Colors (byte count + values)
        size += Colors.GetSerializedSize();

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        Unk01 = 0;
        Unk02 = 0;
        TrimRef = TgiReference.Empty;
        MaterialVariant = 0;
        ModlRef = TgiReference.Empty;
        SwatchGrouping = 0;
        Colors = new ColorList();
    }
}
