using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Terrain Paint (CTPT) resource.
/// Used for terrain painting materials.
/// Resource Type: 0xEBCBB16C
/// Source: CTPTResource.cs lines 28-153
/// </summary>
[ResourceHandler(0xEBCBB16C)]
public sealed class CtptResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CTPT resources.
    /// </summary>
    public const uint TypeId = 0xEBCBB16C;

    /// <summary>
    /// Default version for CTPT resources.
    /// Note: CTPT uses version 0x02, not the standard 0x07.
    /// </summary>
    public const uint DefaultCtptVersion = 0x02;

    /// <summary>
    /// FNV-1a hash seed value used as default for hash fields.
    /// </summary>
    public const uint FnvSeed = 0x811C9DC5;

    #region Properties

    /// <summary>
    /// Hash indicator field.
    /// </summary>
    public uint HashIndicator { get; set; } = 0x1;

    /// <summary>
    /// Hash field 1 (FNV hash).
    /// </summary>
    public uint Hash01 { get; set; } = FnvSeed;

    /// <summary>
    /// Hash field 2 (FNV hash).
    /// </summary>
    public uint Hash02 { get; set; } = FnvSeed;

    /// <summary>
    /// Hash field 3 (FNV hash).
    /// </summary>
    public uint Hash03 { get; set; } = FnvSeed;

    /// <summary>
    /// List of material definition references (MATD type).
    /// Uses uint32 count prefix.
    /// </summary>
    public TgiReferenceList32 MaterialList { get; set; } = new();

    #endregion

    /// <summary>
    /// Creates a new CTPT resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CtptResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CTPTResource.cs lines 110-121
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

        // MaterialList (uint32 count + TGI entries)
        MaterialList = TgiReferenceList32.Parse(data[offset..], out int listBytes);
        offset += listBytes;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CTPTResource.cs lines 123-138
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
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // HashIndicator (4) + Hash01 (4) + Hash02 (4) + Hash03 (4) = 16
        size += 16;

        // MaterialList (uint32 count + TGI entries)
        size += MaterialList.GetSerializedSize();

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        // CTPT uses version 0x02, not the default 0x07
        Version = DefaultCtptVersion;

        HashIndicator = 0x1;
        Hash01 = FnvSeed;
        Hash02 = FnvSeed;
        Hash03 = FnvSeed;
        MaterialList = new TgiReferenceList32();
    }
}
