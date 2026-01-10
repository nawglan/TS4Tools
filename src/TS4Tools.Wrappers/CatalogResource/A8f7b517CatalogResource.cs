using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Resource type 0xA8F7B517 catalog resource.
/// Extends ObjectCatalogResource with 3 COBJ TGI references.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/A8F7B517CatalogResource.cs lines 37-93
/// </summary>
public sealed class A8f7b517CatalogResource : ObjectCatalogResource
{
    /// <summary>
    /// Type ID for A8F7B517 catalog resources.
    /// </summary>
    public const uint TypeId = 0xA8F7B517;

    #region Properties

    /// <summary>
    /// First COBJ TGI reference (ITG order).
    /// Source: A8F7B517CatalogResource.cs line 40
    /// </summary>
    public TgiReference CobjTgiReference1 { get; set; } = TgiReference.Empty;

    /// <summary>
    /// Second COBJ TGI reference (ITG order).
    /// Source: A8F7B517CatalogResource.cs line 41
    /// </summary>
    public TgiReference CobjTgiReference2 { get; set; } = TgiReference.Empty;

    /// <summary>
    /// Third COBJ TGI reference (ITG order).
    /// Source: A8F7B517CatalogResource.cs line 42
    /// </summary>
    public TgiReference CobjTgiReference3 { get; set; } = TgiReference.Empty;

    #endregion

    /// <summary>
    /// Creates a new A8F7B517 catalog resource.
    /// </summary>
    public A8f7b517CatalogResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Source: A8F7B517CatalogResource.cs lines 52-54
        CobjTgiReference1 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        CobjTgiReference2 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        CobjTgiReference3 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;
    }

    /// <inheritdoc/>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Source: A8F7B517CatalogResource.cs lines 61-66
        CobjTgiReference1.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        CobjTgiReference2.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        CobjTgiReference3.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        // 3 TGI references = 3 * 16 bytes = 48 bytes
        return TgiReference.SerializedSize * 3;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        CobjTgiReference1 = TgiReference.Empty;
        CobjTgiReference2 = TgiReference.Empty;
        CobjTgiReference3 = TgiReference.Empty;
    }
}
