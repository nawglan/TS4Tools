using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Floor Pattern (CFLR) resource.
/// Used for floor textures/patterns.
/// Resource Type: 0xB4F762C9
/// Source: CFLRResource.cs lines 27-174
/// </summary>
[ResourceHandler(0xB4F762C9)]
public sealed class CflrResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CFLR resources.
    /// </summary>
    public const uint TypeId = 0xB4F762C9;

    /// <summary>
    /// Default version for CFLR resources.
    /// </summary>
    public new const uint DefaultVersion = 0x06;

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
    /// List of material (MATD) references.
    /// </summary>
    public TgiReferenceList32 MaterialList { get; set; } = new();

    /// <summary>
    /// Color list (ARGB values).
    /// </summary>
    public ColorList Colors { get; set; } = new();

    /// <summary>
    /// Unknown field.
    /// </summary>
    public uint Unk02 { get; set; }

    /// <summary>
    /// Swatch grouping instance ID for color variants.
    /// </summary>
    public ulong SwatchGrouping { get; set; }

    #endregion

    /// <summary>
    /// Creates a new CFLR resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CflrResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CFLRResource.cs lines 121-135
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

        // MaterialList (uint count + TGI entries in ITG order)
        MaterialList = TgiReferenceList32.Parse(data[offset..], out int matBytes);
        offset += matBytes;

        // Colors (byte count + uint values)
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;

        // Unk02 (uint)
        Unk02 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // SwatchGrouping (ulong)
        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CFLRResource.cs lines 137-156
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

        // MaterialList
        offset += MaterialList.WriteTo(buffer[offset..]);

        // Colors
        offset += Colors.WriteTo(buffer[offset..]);

        // Unk02
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk02);
        offset += 4;

        // SwatchGrouping
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // HashIndicator (4) + Hash01 (4) + Hash02 (4) + Hash03 (4) = 16
        size += 16;

        // MaterialList (4 + 16*count)
        size += MaterialList.GetSerializedSize();

        // Colors (1 + 4*count)
        size += Colors.GetSerializedSize();

        // Unk02 (4) + SwatchGrouping (8) = 12
        size += 12;

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        HashIndicator = 0x1;
        Hash01 = 0x811C9DC5;
        Hash02 = 0x811C9DC5;
        Hash03 = 0x811C9DC5;
        MaterialList = new TgiReferenceList32();
        Colors = new ColorList();
        Unk02 = 0;
        SwatchGrouping = 0;
    }
}
