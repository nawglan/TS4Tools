using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Fountain Trim (CFTR) resource.
/// Used for decorative trim around fountains.
/// Resource Type: 0xE7ADA79D
/// Source: CFTRResource.cs lines 27-162
/// </summary>
[ResourceHandler(0xE7ADA79D)]
public sealed class CftrResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CFTR resources.
    /// </summary>
    public const uint TypeId = 0xE7ADA79D;

    #region Properties

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

    /// <summary>
    /// Unknown field (appears to be a boolean or flags).
    /// </summary>
    public byte Unk01 { get; set; }

    #endregion

    /// <summary>
    /// Creates a new CFTR resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CftrResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CFTRResource.cs lines 115-126
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
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

        // Unk01 (byte)
        Unk01 = data[offset++];
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CFTRResource.cs lines 128-145
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
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

        // Unk01
        buffer[offset++] = Unk01;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // TRIMRef (16) + MaterialVariant (4) + MODLRef (16) + SwatchGrouping (8) = 44
        size += TgiReference.SerializedSize; // TRIMRef
        size += 4; // MaterialVariant
        size += TgiReference.SerializedSize; // MODLRef
        size += 8; // SwatchGrouping

        // Colors (byte count + values)
        size += Colors.GetSerializedSize();

        // Unk01
        size += 1;

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        TrimRef = TgiReference.Empty;
        MaterialVariant = 0;
        ModlRef = TgiReference.Empty;
        SwatchGrouping = 0;
        Colors = new ColorList();
        Unk01 = 0;
    }
}
