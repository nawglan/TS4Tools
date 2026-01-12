using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Spindle (CSPN) resource.
/// Used for spindle/spandrel definitions (ceiling rails).
/// Resource Type: 0x3F0C529A
/// Source: CSPNResource.cs lines 28-187
/// </summary>
public sealed class CspnResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CSPN resources.
    /// </summary>
    public const uint TypeId = 0x3F0C529A;

    /// <summary>
    /// Default version for CSPN resources.
    /// </summary>
    public new const uint DefaultVersion = 0x07;

    #region Properties

    /// <summary>
    /// MODL entry list 1.
    /// </summary>
    public SpnFenModlEntryList ModlEntryList01 { get; set; } = new();

    /// <summary>
    /// MODL entry list 2.
    /// </summary>
    public SpnFenModlEntryList ModlEntryList02 { get; set; } = new();

    /// <summary>
    /// MODL entry list 3.
    /// </summary>
    public SpnFenModlEntryList ModlEntryList03 { get; set; } = new();

    /// <summary>
    /// MODL entry list 4.
    /// </summary>
    public SpnFenModlEntryList ModlEntryList04 { get; set; } = new();

    /// <summary>
    /// The 7 TGI references.
    /// </summary>
    public Gp7References ReferenceList { get; set; } = new();

    /// <summary>
    /// Material variant hash.
    /// </summary>
    public uint MaterialVariant { get; set; }

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
    /// Creates a new CSPN resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CspnResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CSPNResource.cs lines 122-135
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // ModlEntryList01
        ModlEntryList01 = SpnFenModlEntryList.Parse(data[offset..], out int bytes1);
        offset += bytes1;

        // ModlEntryList02
        ModlEntryList02 = SpnFenModlEntryList.Parse(data[offset..], out int bytes2);
        offset += bytes2;

        // ModlEntryList03
        ModlEntryList03 = SpnFenModlEntryList.Parse(data[offset..], out int bytes3);
        offset += bytes3;

        // ModlEntryList04
        ModlEntryList04 = SpnFenModlEntryList.Parse(data[offset..], out int bytes4);
        offset += bytes4;

        // ReferenceList (7 TGIs)
        ReferenceList = Gp7References.Parse(data[offset..]);
        offset += Gp7References.SerializedSize;

        // MaterialVariant (uint)
        MaterialVariant = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // SwatchGrouping (ulong)
        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Colors (byte count + uint values)
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CSPNResource.cs lines 137-159
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // ModlEntryList01
        offset += ModlEntryList01.WriteTo(buffer[offset..]);

        // ModlEntryList02
        offset += ModlEntryList02.WriteTo(buffer[offset..]);

        // ModlEntryList03
        offset += ModlEntryList03.WriteTo(buffer[offset..]);

        // ModlEntryList04
        offset += ModlEntryList04.WriteTo(buffer[offset..]);

        // ReferenceList
        ReferenceList.WriteTo(buffer[offset..]);
        offset += Gp7References.SerializedSize;

        // MaterialVariant
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], MaterialVariant);
        offset += 4;

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

        // ModlEntryList01-04
        size += ModlEntryList01.GetSerializedSize();
        size += ModlEntryList02.GetSerializedSize();
        size += ModlEntryList03.GetSerializedSize();
        size += ModlEntryList04.GetSerializedSize();

        // ReferenceList (7 * 16 = 112)
        size += Gp7References.SerializedSize;

        // MaterialVariant (4) + SwatchGrouping (8) = 12
        size += 12;

        // Colors (1 + 4*count)
        size += Colors.GetSerializedSize();

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        ModlEntryList01 = new SpnFenModlEntryList();
        ModlEntryList02 = new SpnFenModlEntryList();
        ModlEntryList03 = new SpnFenModlEntryList();
        ModlEntryList04 = new SpnFenModlEntryList();
        ReferenceList = new Gp7References();
        MaterialVariant = 0;
        SwatchGrouping = 0;
        Colors = new ColorList();
    }
}
