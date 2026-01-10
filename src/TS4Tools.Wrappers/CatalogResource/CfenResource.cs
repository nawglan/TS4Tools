using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Fence resource (0x0418FE2A).
/// Contains fence data with MODL references, GP7 references, slot, colors, and unknown lists.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFENResource.cs lines 28-291
/// </summary>
public sealed class CfenResource : SimpleCatalogResource
{
    /// <summary>
    /// Type ID for CFEN resources.
    /// </summary>
    public const uint TypeId = 0x0418FE2A;

    #region Properties

    /// <summary>
    /// MODL entry list 1.
    /// Source: CFENResource.cs line 37
    /// </summary>
    public SpnFenModlEntryList ModlEntryList01 { get; set; } = new();

    /// <summary>
    /// MODL entry list 2.
    /// Source: CFENResource.cs line 38
    /// </summary>
    public SpnFenModlEntryList ModlEntryList02 { get; set; } = new();

    /// <summary>
    /// MODL entry list 3.
    /// Source: CFENResource.cs line 39
    /// </summary>
    public SpnFenModlEntryList ModlEntryList03 { get; set; } = new();

    /// <summary>
    /// MODL entry list 4.
    /// Source: CFENResource.cs line 40
    /// </summary>
    public SpnFenModlEntryList ModlEntryList04 { get; set; } = new();

    /// <summary>
    /// GP7 reference list (7 fixed TGI references).
    /// Source: CFENResource.cs line 41
    /// </summary>
    public Gp7References ReferenceList { get; set; } = new();

    /// <summary>
    /// Unknown byte field 1.
    /// Source: CFENResource.cs line 42
    /// </summary>
    public byte Unk01 { get; set; }

    /// <summary>
    /// Unknown uint field 2.
    /// Source: CFENResource.cs line 43
    /// </summary>
    public uint Unk02 { get; set; }

    /// <summary>
    /// Material variant hash.
    /// Source: CFENResource.cs line 44
    /// </summary>
    public uint MaterialVariant { get; set; }

    /// <summary>
    /// Swatch grouping identifier.
    /// Source: CFENResource.cs line 45
    /// </summary>
    public ulong SwatchGrouping { get; set; }

    /// <summary>
    /// Slot TGI reference.
    /// Source: CFENResource.cs line 46
    /// </summary>
    public TgiReference Slot { get; set; } = TgiReference.Empty;

    /// <summary>
    /// Unknown list 1 (ushort count + uint values).
    /// Source: CFENResource.cs line 47
    /// </summary>
    public UInt16CountedUInt32List UnkList01 { get; set; } = new();

    /// <summary>
    /// Unknown list 2 (ushort count + uint values).
    /// Source: CFENResource.cs line 48
    /// </summary>
    public UInt16CountedUInt32List UnkList02 { get; set; } = new();

    /// <summary>
    /// Unknown list 3 (ushort count + uint values).
    /// Source: CFENResource.cs line 49
    /// </summary>
    public UInt16CountedUInt32List UnkList03 { get; set; } = new();

    /// <summary>
    /// Color list.
    /// Source: CFENResource.cs line 50
    /// </summary>
    public ColorList Colors { get; set; } = new();

    /// <summary>
    /// Unknown uint field 4.
    /// Source: CFENResource.cs line 51
    /// </summary>
    public uint Unk04 { get; set; }

    #endregion

    /// <summary>
    /// Creates a new CFEN resource.
    /// </summary>
    public CfenResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Source: CFENResource.cs lines 176-190
        ModlEntryList01 = SpnFenModlEntryList.Parse(data[offset..], out int bytes1);
        offset += bytes1;

        ModlEntryList02 = SpnFenModlEntryList.Parse(data[offset..], out int bytes2);
        offset += bytes2;

        ModlEntryList03 = SpnFenModlEntryList.Parse(data[offset..], out int bytes3);
        offset += bytes3;

        ModlEntryList04 = SpnFenModlEntryList.Parse(data[offset..], out int bytes4);
        offset += bytes4;

        ReferenceList = Gp7References.Parse(data[offset..]);
        offset += Gp7References.SerializedSize;

        Unk01 = data[offset++];

        Unk02 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        MaterialVariant = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        Slot = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;

        UnkList01 = UInt16CountedUInt32List.Parse(data[offset..], out int listBytes1);
        offset += listBytes1;

        UnkList02 = UInt16CountedUInt32List.Parse(data[offset..], out int listBytes2);
        offset += listBytes2;

        UnkList03 = UInt16CountedUInt32List.Parse(data[offset..], out int listBytes3);
        offset += listBytes3;

        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;

        Unk04 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
    }

    /// <inheritdoc/>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Source: CFENResource.cs lines 200-224
        offset += ModlEntryList01.WriteTo(buffer[offset..]);
        offset += ModlEntryList02.WriteTo(buffer[offset..]);
        offset += ModlEntryList03.WriteTo(buffer[offset..]);
        offset += ModlEntryList04.WriteTo(buffer[offset..]);

        ReferenceList.WriteTo(buffer[offset..]);
        offset += Gp7References.SerializedSize;

        buffer[offset++] = Unk01;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk02);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], MaterialVariant);
        offset += 4;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;

        Slot.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;

        offset += UnkList01.WriteTo(buffer[offset..]);
        offset += UnkList02.WriteTo(buffer[offset..]);
        offset += UnkList03.WriteTo(buffer[offset..]);

        offset += Colors.WriteTo(buffer[offset..]);

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], Unk04);
        offset += 4;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        size += ModlEntryList01.GetSerializedSize();
        size += ModlEntryList02.GetSerializedSize();
        size += ModlEntryList03.GetSerializedSize();
        size += ModlEntryList04.GetSerializedSize();

        size += Gp7References.SerializedSize; // 7 * 16 = 112

        size += 1; // Unk01
        size += 4; // Unk02
        size += 4; // MaterialVariant
        size += 8; // SwatchGrouping
        size += TgiReference.SerializedSize; // Slot (16)

        size += UnkList01.GetSerializedSize();
        size += UnkList02.GetSerializedSize();
        size += UnkList03.GetSerializedSize();

        size += Colors.GetSerializedSize();

        size += 4; // Unk04

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
        Unk01 = 0;
        Unk02 = 0;
        MaterialVariant = 0;
        SwatchGrouping = 0;
        Slot = TgiReference.Empty;
        UnkList01 = new UInt16CountedUInt32List();
        UnkList02 = new UInt16CountedUInt32List();
        UnkList03 = new UInt16CountedUInt32List();
        Colors = new ColorList();
        Unk04 = 0;
    }
}
