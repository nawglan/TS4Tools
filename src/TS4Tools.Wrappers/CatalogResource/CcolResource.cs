using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Color (CCOL) resource.
/// Used for defining color variations of objects.
/// Resource Type: 0x1D6DF1CF
/// Source: CCOLResource.cs lines 31-213
/// </summary>
public sealed class CcolResource : AbstractCatalogResource
{
    /// <summary>
    /// Resource type ID for CCOL resources.
    /// </summary>
    public const uint TypeId = 0x1D6DF1CF;

    #region Properties

    /// <summary>
    /// Unknown field.
    /// </summary>
    public uint Unknown02 { get; set; }

    /// <summary>
    /// References indicator. Controls which reference set is used.
    /// If 0: NullRefs (Gp4References)
    /// Otherwise: ModlRefs + FtptRefs (Gp9References each)
    /// </summary>
    public uint ReferencesIndicator { get; set; }

    /// <summary>
    /// MODL references (9 TGIs). Only used when ReferencesIndicator != 0.
    /// </summary>
    public Gp9References? ModlRefs { get; set; }

    /// <summary>
    /// FTPT references (9 TGIs). Only used when ReferencesIndicator != 0.
    /// </summary>
    public Gp9References? FtptRefs { get; set; }

    /// <summary>
    /// Null references (4 TGIs). Only used when ReferencesIndicator == 0.
    /// </summary>
    public Gp4References? NullRefs { get; set; }

    #endregion

    /// <summary>
    /// Creates a new CCOL resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CcolResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CCOLResource.cs lines 162-178
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Unknown02 (uint)
        Unknown02 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // ReferencesIndicator (uint)
        ReferencesIndicator = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (ReferencesIndicator == 0)
        {
            // Gp4references (4 TGIs)
            NullRefs = Gp4References.Parse(data[offset..]);
            offset += Gp4References.SerializedSize;
            ModlRefs = null;
            FtptRefs = null;
        }
        else
        {
            // Gp9references + Gp9references (18 TGIs total)
            ModlRefs = Gp9References.Parse(data[offset..]);
            offset += Gp9References.SerializedSize;

            FtptRefs = Gp9References.Parse(data[offset..]);
            offset += Gp9References.SerializedSize;
            NullRefs = null;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CCOLResource.cs lines 180-210
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Unknown02
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unknown02);
        offset += 4;

        // ReferencesIndicator
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], ReferencesIndicator);
        offset += 4;

        if (ReferencesIndicator == 0)
        {
            // Write NullRefs
            if (NullRefs == null)
            {
                NullRefs = new Gp4References();
            }
            NullRefs.WriteTo(buffer[offset..]);
            offset += Gp4References.SerializedSize;
        }
        else
        {
            // Write ModlRefs + FtptRefs
            if (ModlRefs == null)
            {
                ModlRefs = new Gp9References();
            }
            ModlRefs.WriteTo(buffer[offset..]);
            offset += Gp9References.SerializedSize;

            if (FtptRefs == null)
            {
                FtptRefs = new Gp9References();
            }
            FtptRefs.WriteTo(buffer[offset..]);
            offset += Gp9References.SerializedSize;
        }
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // Unknown02 (4) + ReferencesIndicator (4) = 8
        size += 8;

        if (ReferencesIndicator == 0)
        {
            // Gp4references (64)
            size += Gp4References.SerializedSize;
        }
        else
        {
            // Gp9references * 2 (288)
            size += Gp9References.SerializedSize * 2;
        }

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        base.InitializeDefaults();
        Unknown02 = 0;
        ReferencesIndicator = 0;
        NullRefs = new Gp4References();
        ModlRefs = null;
        FtptRefs = null;
    }
}
