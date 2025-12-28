using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Frieze (CFRZ) resource.
/// Used for decorative frieze/crown molding elements.
/// Resource Type: 0xA057811C
/// Source: CFRZResource.cs lines 27-180
/// </summary>
[ResourceHandler(0xA057811C)]
public sealed class CfrzResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CFRZ resources.
    /// </summary>
    public const uint TypeId = 0xA057811C;

    /// <summary>
    /// Default version for CFRZ resources.
    /// </summary>
    public new const uint DefaultVersion = 0x09;

    #region Properties

    /// <summary>
    /// Reference to the trim resource (TRIM type).
    /// </summary>
    public TgiReference TrimRef { get; set; }

    /// <summary>
    /// Reference to the first model resource (MODL type).
    /// </summary>
    public TgiReference ModlRef1 { get; set; }

    /// <summary>
    /// Material variant hash.
    /// </summary>
    public uint MaterialVariant { get; set; }

    /// <summary>
    /// Swatch grouping instance ID for color variants.
    /// </summary>
    public ulong SwatchGrouping { get; set; }

    /// <summary>
    /// Float value 1 (purpose unknown).
    /// </summary>
    public float Float01 { get; set; }

    /// <summary>
    /// Float value 2 (purpose unknown).
    /// </summary>
    public float Float02 { get; set; }

    /// <summary>
    /// Reference to the second model resource (MODL type).
    /// </summary>
    public TgiReference ModlRef2 { get; set; }

    /// <summary>
    /// Color list (ARGB values).
    /// </summary>
    public ColorList Colors { get; set; } = new();

    #endregion

    /// <summary>
    /// Creates a new CFRZ resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CfrzResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CFRZResource.cs lines 129-142
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // TrimRef (ITG order, 16 bytes)
        TrimRef = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // ModlRef1 (ITG order, 16 bytes)
        ModlRef1 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // MaterialVariant (uint)
        MaterialVariant = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // SwatchGrouping (ulong)
        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Float01 (float)
        Float01 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        // Float02 (float)
        Float02 = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        // ModlRef2 (ITG order, 16 bytes)
        ModlRef2 = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        // Colors (byte count + uint values)
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CFRZResource.cs lines 144-164
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // TrimRef
        TrimRef.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // ModlRef1
        ModlRef1.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // MaterialVariant
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], MaterialVariant);
        offset += 4;

        // SwatchGrouping
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;

        // Float01
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], Float01);
        offset += 4;

        // Float02
        BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], Float02);
        offset += 4;

        // ModlRef2
        ModlRef2.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        // Colors
        offset += Colors.WriteTo(buffer[offset..]);
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // TrimRef (16) + ModlRef1 (16) = 32
        size += TgiReference.SerializedSize * 2;

        // MaterialVariant (4) + SwatchGrouping (8) + Float01 (4) + Float02 (4) = 20
        size += 20;

        // ModlRef2 (16)
        size += TgiReference.SerializedSize;

        // Colors (1 + 4*count)
        size += Colors.GetSerializedSize();

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        TrimRef = TgiReference.Empty;
        ModlRef1 = TgiReference.Empty;
        MaterialVariant = 0;
        SwatchGrouping = 0;
        Float01 = 0;
        Float02 = 0;
        ModlRef2 = TgiReference.Empty;
        Colors = new ColorList();
    }
}
