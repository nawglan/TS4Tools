using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Roof style catalog resource (0x91EDBD3E).
/// Extends ObjectCatalogResource with roof-specific TGI references.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/RoofStyleResource.cs lines 37-109
/// </summary>
public sealed class RoofStyleResource : ObjectCatalogResource
{
    /// <summary>
    /// Type ID for roof style resources.
    /// </summary>
    public const uint TypeId = 0x91EDBD3E;

    #region Properties

    /// <summary>
    /// Unknown field 1.
    /// Source: RoofStyleResource.cs line 40
    /// </summary>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// CRMT (Catalog Roof Material Trim?) TGI reference.
    /// Source: RoofStyleResource.cs line 41
    /// </summary>
    public TgiReference CrmtTgiReference { get; set; } = TgiReference.Empty;

    /// <summary>
    /// First CRTR (Catalog Roof Trim Rim?) TGI reference.
    /// Source: RoofStyleResource.cs line 42
    /// </summary>
    public TgiReference CrtrTgiReference1 { get; set; } = TgiReference.Empty;

    /// <summary>
    /// Second CRTR TGI reference.
    /// Source: RoofStyleResource.cs line 43
    /// </summary>
    public TgiReference CrtrTgiReference2 { get; set; } = TgiReference.Empty;

    /// <summary>
    /// Tool TGI reference.
    /// Source: RoofStyleResource.cs line 44
    /// </summary>
    public TgiReference ToolTgiReference { get; set; } = TgiReference.Empty;

    /// <summary>
    /// Unknown field 2.
    /// Source: RoofStyleResource.cs line 45
    /// </summary>
    public uint Unknown2 { get; set; }

    #endregion

    /// <summary>
    /// Creates a new roof style resource.
    /// </summary>
    public RoofStyleResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Source: RoofStyleResource.cs lines 55-60
        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        CrmtTgiReference = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        CrtrTgiReference1 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        CrtrTgiReference2 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        ToolTgiReference = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
    }

    /// <inheritdoc/>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Source: RoofStyleResource.cs lines 67-76
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown1);
        offset += 4;

        CrmtTgiReference.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        CrtrTgiReference1.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        CrtrTgiReference2.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        ToolTgiReference.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown2);
        offset += 4;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        // unknown1 (4) + 4 TGI refs (4 * 16) + unknown2 (4) = 72 bytes
        return 4 + (TgiReference.SerializedSize * 4) + 4;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        Unknown1 = 0;
        CrmtTgiReference = TgiReference.Empty;
        CrtrTgiReference1 = TgiReference.Empty;
        CrtrTgiReference2 = TgiReference.Empty;
        ToolTgiReference = TgiReference.Empty;
        Unknown2 = 0;
    }
}
