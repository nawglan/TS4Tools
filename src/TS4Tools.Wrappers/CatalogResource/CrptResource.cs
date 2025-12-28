using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Roof Pattern (CRPT) resource.
/// Used for roof material patterns.
/// Resource Type: 0xF1EDBD86
/// Source: CRPTResource.cs lines 27-153
/// </summary>
[ResourceHandler(0xF1EDBD86)]
public sealed class CrptResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CRPT resources.
    /// </summary>
    public const uint TypeId = 0xF1EDBD86;

    #region Properties

    /// <summary>
    /// Unknown field 1.
    /// </summary>
    public uint Unk01 { get; set; }

    /// <summary>
    /// Reference to the material definition resource (MATD type).
    /// </summary>
    public TgiReference MatdRef { get; set; }

    /// <summary>
    /// Reference to the floor resource.
    /// </summary>
    public TgiReference FloorRef { get; set; }

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
    /// Creates a new CRPT resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CrptResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CRPTResource.cs lines 108-118
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Unk01 (uint)
        Unk01 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // MATDRef (ITG order, 16 bytes)
        MatdRef = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // FloorRef (ITG order, 16 bytes)
        FloorRef = TgiReference.Parse(data[offset..]);
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
    /// Source: CRPTResource.cs lines 120-136
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Unk01
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk01);
        offset += 4;

        // MATDRef
        MatdRef.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // FloorRef
        FloorRef.WriteTo(buffer[offset..]);
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

        // Unk01 (4) + MATDRef (16) + FloorRef (16) + SwatchGrouping (8) = 44
        size += 4; // Unk01
        size += TgiReference.SerializedSize; // MATDRef
        size += TgiReference.SerializedSize; // FloorRef
        size += 8; // SwatchGrouping

        // Colors (byte count + values)
        size += Colors.GetSerializedSize();

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        Unk01 = 0;
        MatdRef = TgiReference.Empty;
        FloorRef = TgiReference.Empty;
        SwatchGrouping = 0;
        Colors = new ColorList();
    }
}
