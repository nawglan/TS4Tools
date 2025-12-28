using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Stair Landing (CSTL) resource.
/// Used for stair landing definitions.
/// Resource Type: 0x9F5CFF10
/// Source: CSTLResource.cs lines 28-258
/// </summary>
[ResourceHandler(0x9F5CFF10)]
public sealed class CstlResource : SimpleCatalogResource
{
    /// <summary>
    /// Resource type ID for CSTL resources.
    /// </summary>
    public const uint TypeId = 0x9F5CFF10;

    /// <summary>
    /// Default version for CSTL resources.
    /// </summary>
    public new const uint DefaultVersion = 0x0E;

    /// <summary>
    /// Number of TGI references for version 0x0D.
    /// </summary>
    private const int RefCountV0D = 21;

    /// <summary>
    /// Number of TGI references for version 0x0E and later.
    /// </summary>
    private const int RefCountV0E = 25;

    #region Properties

    /// <summary>
    /// The TGI references for stair landing resources.
    /// Size depends on version: 21 for v0x0D, 25 for v0x0E.
    /// </summary>
    public List<TgiReference> CstlRefs { get; set; } = [];

    /// <summary>
    /// Unknown uint list.
    /// </summary>
    public CstlUintList UnkList1 { get; set; } = new();

    /// <summary>
    /// Unknown field 1.
    /// </summary>
    public uint Unk01 { get; set; }

    /// <summary>
    /// Unknown field 2.
    /// </summary>
    public uint Unk02 { get; set; }

    /// <summary>
    /// Unknown field 3.
    /// </summary>
    public uint Unk03 { get; set; }

    /// <summary>
    /// Unknown field 4.
    /// </summary>
    public uint Unk04 { get; set; }

    /// <summary>
    /// Unknown field 5.
    /// </summary>
    public byte Unk05 { get; set; }

    /// <summary>
    /// Unknown instance ID.
    /// </summary>
    public ulong UnkIID01 { get; set; }

    #endregion

    /// <summary>
    /// Creates a new CSTL resource by parsing data.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    public CstlResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <summary>
    /// Gets the expected reference count based on version.
    /// </summary>
    private int GetRefCount() => Version == 0x0D ? RefCountV0D : RefCountV0E;

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CSTLResource.cs lines 136-156
    /// </remarks>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // CSTLRefs (version-dependent size)
        int refCount = GetRefCount();
        CstlRefs = new List<TgiReference>(refCount);
        for (int i = 0; i < refCount; i++)
        {
            CstlRefs.Add(TgiReference.Parse(data[offset..]));
            offset += TgiReference.SerializedSize;
        }

        // UnkList1 (byte count + uint values)
        UnkList1 = CstlUintList.Parse(data[offset..], out int listBytes);
        offset += listBytes;

        // Unk01 (uint)
        Unk01 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk02 (uint)
        Unk02 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk03 (uint)
        Unk03 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk04 (uint)
        Unk04 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Unk05 (byte)
        Unk05 = data[offset++];

        // UnkIID01 (ulong)
        UnkIID01 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: CSTLResource.cs lines 158-199
    /// </remarks>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // CSTLRefs (version-dependent size)
        int refCount = GetRefCount();

        // Ensure we have enough refs, filling with empty if needed
        while (CstlRefs.Count < refCount)
        {
            CstlRefs.Add(TgiReference.Empty);
        }

        // Write only the required number of refs
        for (int i = 0; i < refCount; i++)
        {
            CstlRefs[i].WriteTo(buffer[offset..]);
            offset += TgiReference.SerializedSize;
        }

        // UnkList1
        offset += UnkList1.WriteTo(buffer[offset..]);

        // Unk01
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk01);
        offset += 4;

        // Unk02
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk02);
        offset += 4;

        // Unk03
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk03);
        offset += 4;

        // Unk04
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk04);
        offset += 4;

        // Unk05
        buffer[offset++] = Unk05;

        // UnkIID01
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], UnkIID01);
        offset += 8;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        // CSTLRefs (version-dependent: 21 or 25 refs * 16 bytes)
        size += GetRefCount() * TgiReference.SerializedSize;

        // UnkList1 (1 + 4*count)
        size += UnkList1.GetSerializedSize();

        // Unk01-04 (16)
        size += 16;

        // Unk05 (1)
        size += 1;

        // UnkIID01 (8)
        size += 8;

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        // Initialize refs for version 0x0E (default)
        CstlRefs = new List<TgiReference>(RefCountV0E);
        for (int i = 0; i < RefCountV0E; i++)
        {
            CstlRefs.Add(TgiReference.Empty);
        }

        UnkList1 = new CstlUintList();
        Unk01 = 0;
        Unk02 = 0;
        Unk03 = 0;
        Unk04 = 0;
        Unk05 = 0;
        UnkIID01 = 0;
    }
}
