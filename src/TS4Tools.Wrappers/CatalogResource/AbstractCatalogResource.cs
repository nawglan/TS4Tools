using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Abstract base class for catalog resources like COBJ and CCOL.
/// Contains common fields for aural properties, placement, and object behavior.
/// Source: AbstractCatalogResource.cs lines 31-593
/// </summary>
public abstract class AbstractCatalogResource : TypedResource
{
    /// <summary>
    /// Current recommended version for new resources.
    /// </summary>
    public const uint DefaultVersion = 0x19;

    /// <summary>
    /// FNV-32 offset basis used as default for empty hashes.
    /// </summary>
    public const uint FnvOffsetBasis = 0x811C9DC5;

    #region Properties

    /// <summary>
    /// Resource format version.
    /// </summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>
    /// Common catalog block shared by all catalog types.
    /// </summary>
    public CatalogCommon CommonBlock { get; set; } = new();

    /// <summary>
    /// Aural materials version.
    /// </summary>
    public uint AuralMaterialsVersion { get; set; } = 0x1;

    /// <summary>
    /// Aural material hash 1.
    /// </summary>
    public uint AuralMaterials1 { get; set; } = FnvOffsetBasis;

    /// <summary>
    /// Aural material hash 2.
    /// </summary>
    public uint AuralMaterials2 { get; set; } = FnvOffsetBasis;

    /// <summary>
    /// Aural material hash 3.
    /// </summary>
    public uint AuralMaterials3 { get; set; } = FnvOffsetBasis;

    /// <summary>
    /// Aural properties version (controls which fields are present).
    /// </summary>
    public uint AuralPropertiesVersion { get; set; } = 0x2;

    /// <summary>
    /// Aural quality hash.
    /// </summary>
    public uint AuralQuality { get; set; } = FnvOffsetBasis;

    /// <summary>
    /// Aural ambient object hash (version 2+).
    /// </summary>
    public uint AuralAmbientObject { get; set; } = FnvOffsetBasis;

    /// <summary>
    /// Ambience file instance ID (version 3 only).
    /// </summary>
    public ulong AmbienceFileInstanceId { get; set; }

    /// <summary>
    /// Override ambience flag (version 3 only).
    /// </summary>
    public byte IsOverrideAmbience { get; set; }

    /// <summary>
    /// Unknown field (version 4 only).
    /// </summary>
    public byte Unknown01 { get; set; }

    /// <summary>
    /// Unused field 0.
    /// </summary>
    public uint Unused0 { get; set; }

    /// <summary>
    /// Unused field 1.
    /// </summary>
    public uint Unused1 { get; set; }

    /// <summary>
    /// Unused field 2.
    /// </summary>
    public uint Unused2 { get; set; }

    /// <summary>
    /// Placement flags (high bits).
    /// </summary>
    public uint PlacementFlagsHigh { get; set; }

    /// <summary>
    /// Placement flags (low bits).
    /// </summary>
    public uint PlacementFlagsLow { get; set; }

    /// <summary>
    /// Slot type set.
    /// </summary>
    public ulong SlotTypeSet { get; set; }

    /// <summary>
    /// Slot decoration size.
    /// </summary>
    public byte SlotDecoSize { get; set; }

    /// <summary>
    /// Catalog group identifier.
    /// </summary>
    public ulong CatalogGroup { get; set; }

    /// <summary>
    /// State usage flags.
    /// </summary>
    public byte StateUsage { get; set; }

    /// <summary>
    /// Color list (ARGB values).
    /// </summary>
    public ColorList Colors { get; set; } = new();

    /// <summary>
    /// Fence height (used for fence-like objects).
    /// </summary>
    public uint FenceHeight { get; set; }

    /// <summary>
    /// Whether the item is stackable.
    /// </summary>
    public byte IsStackable { get; set; }

    /// <summary>
    /// Whether the item can depreciate.
    /// </summary>
    public byte CanItemDepreciate { get; set; }

    /// <summary>
    /// Fallback object key (version 0x19+).
    /// </summary>
    public TgiReference FallbackObjectKey { get; set; }

    #endregion

    /// <summary>
    /// Creates a new AbstractCatalogResource.
    /// </summary>
    protected AbstractCatalogResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: AbstractCatalogResource.cs lines 491-534
    /// </remarks>
    protected sealed override void Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;

        // Version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Common block
        CommonBlock = CatalogCommon.Parse(data[offset..], out int commonBytes);
        offset += commonBytes;

        // Aural materials
        AuralMaterialsVersion = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        AuralMaterials1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        AuralMaterials2 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        AuralMaterials3 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Aural properties (version-dependent)
        AuralPropertiesVersion = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        AuralQuality = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (AuralPropertiesVersion > 1)
        {
            AuralAmbientObject = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
        }

        if (AuralPropertiesVersion == 3)
        {
            AmbienceFileInstanceId = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;

            IsOverrideAmbience = data[offset++];
        }

        if (AuralPropertiesVersion == 4)
        {
            Unknown01 = data[offset++];
        }

        // Unused fields
        Unused0 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unused1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Unused2 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Placement
        PlacementFlagsHigh = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        PlacementFlagsLow = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        SlotTypeSet = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        SlotDecoSize = data[offset++];

        CatalogGroup = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        StateUsage = data[offset++];

        // Colors
        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;

        // Remaining fields
        FenceHeight = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        IsStackable = data[offset++];
        CanItemDepreciate = data[offset++];

        // Fallback object key (version 0x19+)
        if (Version >= 0x19)
        {
            FallbackObjectKey = TgiReference.Parse(data[offset..]);
            offset += TgiReference.SerializedSize;
        }

        // Type-specific fields
        ParseTypeSpecific(data, ref offset);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: AbstractCatalogResource.cs lines 536-592
    /// </remarks>
    protected sealed override ReadOnlyMemory<byte> Serialize()
    {
        int size = CalculateSerializedSize();
        var buffer = new byte[size];
        int offset = 0;

        // Version
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;

        // Common block
        offset += CommonBlock.WriteTo(buffer.AsSpan(offset));

        // Aural materials
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), AuralMaterialsVersion);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), AuralMaterials1);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), AuralMaterials2);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), AuralMaterials3);
        offset += 4;

        // Aural properties
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), AuralPropertiesVersion);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), AuralQuality);
        offset += 4;

        if (AuralPropertiesVersion > 1)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), AuralAmbientObject);
            offset += 4;
        }

        if (AuralPropertiesVersion == 3)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), AmbienceFileInstanceId);
            offset += 8;

            buffer[offset++] = IsOverrideAmbience;
        }

        if (AuralPropertiesVersion == 4)
        {
            buffer[offset++] = Unknown01;
        }

        // Unused fields
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unused0);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unused1);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Unused2);
        offset += 4;

        // Placement
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), PlacementFlagsHigh);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), PlacementFlagsLow);
        offset += 4;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), SlotTypeSet);
        offset += 8;

        buffer[offset++] = SlotDecoSize;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), CatalogGroup);
        offset += 8;

        buffer[offset++] = StateUsage;

        // Colors
        offset += Colors.WriteTo(buffer.AsSpan(offset));

        // Remaining fields
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), FenceHeight);
        offset += 4;

        buffer[offset++] = IsStackable;
        buffer[offset++] = CanItemDepreciate;

        // Fallback object key (version 0x19+)
        if (Version >= 0x19)
        {
            FallbackObjectKey.WriteTo(buffer.AsSpan(offset));
            offset += TgiReference.SerializedSize;
        }

        // Type-specific fields
        SerializeTypeSpecific(buffer.AsSpan(), ref offset);

        return buffer;
    }

    /// <summary>
    /// Parses type-specific fields after the base AbstractCatalogResource fields.
    /// Override this in derived classes to parse additional fields.
    /// </summary>
    /// <param name="data">The full data span.</param>
    /// <param name="offset">The current offset after base fields. Update this as you read.</param>
    protected virtual void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Serializes type-specific fields after the base AbstractCatalogResource fields.
    /// Override this in derived classes to serialize additional fields.
    /// </summary>
    /// <param name="buffer">The full buffer span.</param>
    /// <param name="offset">The current offset after base fields. Update this as you write.</param>
    protected virtual void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Gets the size in bytes of the type-specific fields when serialized.
    /// Override this in derived classes.
    /// </summary>
    protected virtual int GetTypeSpecificSerializedSize() => 0;

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = DefaultVersion;
        CommonBlock = new CatalogCommon();
        AuralMaterialsVersion = 0x1;
        AuralMaterials1 = FnvOffsetBasis;
        AuralMaterials2 = FnvOffsetBasis;
        AuralMaterials3 = FnvOffsetBasis;
        AuralPropertiesVersion = 0x2;
        AuralQuality = FnvOffsetBasis;
        AuralAmbientObject = FnvOffsetBasis;
        Colors = new ColorList();
        FallbackObjectKey = TgiReference.Empty;
    }

    private int CalculateSerializedSize()
    {
        int size = 0;

        // Version
        size += 4;

        // Common block
        size += CommonBlock.GetSerializedSize();

        // Aural materials: version(4) + 3 hashes(12) = 16
        size += 16;

        // Aural properties: version(4) + quality(4) = 8
        size += 8;

        if (AuralPropertiesVersion > 1)
            size += 4; // ambientObject

        if (AuralPropertiesVersion == 3)
            size += 9; // instanceId(8) + override(1)

        if (AuralPropertiesVersion == 4)
            size += 1; // unknown01

        // Unused: 3 * 4 = 12
        size += 12;

        // Placement: high(4) + low(4) + slotTypeSet(8) + decoSize(1) + catalogGroup(8) + stateUsage(1) = 26
        size += 26;

        // Colors
        size += Colors.GetSerializedSize();

        // Remaining: fenceHeight(4) + stackable(1) + depreciate(1) = 6
        size += 6;

        // Fallback key (version 0x19+)
        if (Version >= 0x19)
            size += TgiReference.SerializedSize;

        // Type-specific fields
        size += GetTypeSpecificSerializedSize();

        return size;
    }
}
