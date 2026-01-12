using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Railing (CRAL) resource.
/// Used for stair railings.
/// Resource Type: 0x1C1CF1F7
/// Source: CRALResource.cs lines 27-146
/// </summary>
public sealed class CralResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CRAL resources.
    /// </summary>
    public const uint TypeId = 0x1C1CF1F7;

    /// <summary>
    /// Default version for CRAL resources.
    /// </summary>
    public new const uint DefaultVersion = 0x08;

    #region Properties

    /// <summary>
    /// Group of 8 TGI references for railing models/materials.
    /// </summary>
    public Gp8References ReferenceList { get; set; } = new();

    /// <summary>
    /// Material variant hash.
    /// </summary>
    public uint MaterialVariant { get; set; }

    /// <summary>
    /// Swatch grouping instance ID for color variants.
    /// </summary>
    public ulong SwatchGrouping { get; set; }

    /// <summary>
    /// Unknown field.
    /// </summary>
    public uint Unk02 { get; set; }

    /// <summary>
    /// Color list (ARGB values).
    /// </summary>
    public ColorList Colors { get; set; } = new();

    #endregion

    /// <summary>
    /// Creates a new CRAL resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CralResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CRALResource.cs lines 102-112
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // ReferenceList (8 TGI references = 128 bytes)
        ReferenceList = Gp8References.Parse(data[offset..]);
        offset += Gp8References.SerializedSize;

        // MaterialVariant (uint)
        MaterialVariant = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // SwatchGrouping (ulong)
        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Unk02 (uint)
        Unk02 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Colors (byte count + uint values)
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CRALResource.cs lines 114-128
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // ReferenceList
        ReferenceList.WriteTo(buffer[offset..]);
        offset += Gp8References.SerializedSize;

        // MaterialVariant
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], MaterialVariant);
        offset += 4;

        // SwatchGrouping
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;

        // Unk02
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk02);
        offset += 4;

        // Colors
        offset += Colors.WriteTo(buffer[offset..]);
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // ReferenceList (128 bytes)
        size += Gp8References.SerializedSize;

        // MaterialVariant (4) + SwatchGrouping (8) + Unk02 (4) = 16
        size += 16;

        // Colors (1 + 4*count)
        size += Colors.GetSerializedSize();

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        ReferenceList = new Gp8References();
        MaterialVariant = 0;
        SwatchGrouping = 0;
        Unk02 = 0;
        Colors = new ColorList();
    }
}
