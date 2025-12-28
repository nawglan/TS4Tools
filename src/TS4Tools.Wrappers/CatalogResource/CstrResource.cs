using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Stair (CSTR) resource.
/// Used for staircase definitions.
/// Resource Type: 0x9A20CD1C
/// Source: CSTRResource.cs lines 27-365
/// </summary>
[ResourceHandler(0x9A20CD1C)]
public sealed class CstrResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CSTR resources.
    /// </summary>
    public const uint TypeId = 0x9A20CD1C;

    /// <summary>
    /// Default version for CSTR resources.
    /// </summary>
    public new const uint DefaultVersion = 0x0A;

    #region Properties

    /// <summary>
    /// Hash indicator flag.
    /// </summary>
    public uint HashIndicator { get; set; } = 0x1;

    /// <summary>
    /// Hash value 1. Default FNV hash seed.
    /// </summary>
    public uint Hash01 { get; set; } = 0x811C9DC5;

    /// <summary>
    /// Hash value 2. Default FNV hash seed.
    /// </summary>
    public uint Hash02 { get; set; } = 0x811C9DC5;

    /// <summary>
    /// Hash value 3. Default FNV hash seed.
    /// </summary>
    public uint Hash03 { get; set; } = 0x811C9DC5;

    /// <summary>
    /// The 6 TGI references for stair resources.
    /// </summary>
    public CstrReferences ReferenceList { get; set; } = new();

    /// <summary>
    /// Unknown field 1 (byte).
    /// </summary>
    public byte Unk01 { get; set; }

    /// <summary>
    /// Unknown field 2 (byte).
    /// </summary>
    public byte Unk02 { get; set; }

    /// <summary>
    /// Unknown field 3 (byte).
    /// </summary>
    public byte Unk03 { get; set; }

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

    /// <summary>
    /// Unknown field 5 (byte).
    /// </summary>
    public byte Unk05 { get; set; }

    #endregion

    /// <summary>
    /// Creates a new CSTR resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CstrResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CSTRResource.cs lines 158-175
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // HashIndicator (uint)
        HashIndicator = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Hash01 (uint)
        Hash01 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Hash02 (uint)
        Hash02 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Hash03 (uint)
        Hash03 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // CSTR_references (6 TGIs)
        ReferenceList = CstrReferences.Parse(data[offset..]);
        offset += CstrReferences.SerializedSize;

        // Unk01 (byte)
        Unk01 = data[offset++];

        // Unk02 (byte)
        Unk02 = data[offset++];

        // Unk03 (byte)
        Unk03 = data[offset++];

        // MaterialVariant (uint)
        MaterialVariant = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // SwatchGrouping (ulong)
        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Colors (byte count + uint values)
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;

        // Unk05 (byte)
        Unk05 = data[offset++];
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CSTRResource.cs lines 177-199
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // HashIndicator
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], HashIndicator);
        offset += 4;

        // Hash01
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Hash01);
        offset += 4;

        // Hash02
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Hash02);
        offset += 4;

        // Hash03
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Hash03);
        offset += 4;

        // ReferenceList
        ReferenceList.WriteTo(buffer[offset..]);
        offset += CstrReferences.SerializedSize;

        // Unk01
        buffer[offset++] = Unk01;

        // Unk02
        buffer[offset++] = Unk02;

        // Unk03
        buffer[offset++] = Unk03;

        // MaterialVariant
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], MaterialVariant);
        offset += 4;

        // SwatchGrouping
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;

        // Colors
        offset += Colors.WriteTo(buffer[offset..]);

        // Unk05
        buffer[offset++] = Unk05;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // HashIndicator (4) + Hash01-03 (12) = 16
        size += 16;

        // ReferenceList (6 * 16 = 96)
        size += CstrReferences.SerializedSize;

        // Unk01-03 (3)
        size += 3;

        // MaterialVariant (4) + SwatchGrouping (8) = 12
        size += 12;

        // Colors (1 + 4*count)
        size += Colors.GetSerializedSize();

        // Unk05 (1)
        size += 1;

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        HashIndicator = 0x1;
        Hash01 = 0x811C9DC5;
        Hash02 = 0x811C9DC5;
        Hash03 = 0x811C9DC5;
        ReferenceList = new CstrReferences();
        Unk01 = 0;
        Unk02 = 0;
        Unk03 = 0;
        MaterialVariant = 0;
        SwatchGrouping = 0;
        Colors = new ColorList();
        Unk05 = 0;
    }
}
