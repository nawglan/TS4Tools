using System.Buffers.Binary;
using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// CAS Part resource (0x034AEECB) - defines Create-A-Sim clothing and accessory parts.
/// Supports versions from 27 (0x1B) through 37+ with appropriate version gating.
///
/// Source: legacy_references/.../CASPartResource/CASPartResource.cs
/// </summary>
public sealed class CasPartResource : TypedResource
{
    /// <summary>
    /// Resource type ID for CAS Part resources.
    /// </summary>
    public const uint TypeId = 0x034AEECB;

    /// <summary>
    /// Default version for new resources.
    /// </summary>
    public const uint DefaultVersion = 37;

    #region Version Constants

    /// <summary>Version threshold for sharedUVMapSpace field.</summary>
    private const uint VersionSharedUvMapSpace = 0x1B; // 27

    /// <summary>Version threshold for voiceEffectHash field.</summary>
    private const uint VersionVoiceEffectHash = 0x1C; // 28

    /// <summary>Version threshold for material hashes and emission map.</summary>
    private const uint VersionMaterialHashes = 0x1E; // 30

    /// <summary>Version threshold for hideForOccultFlags field.</summary>
    private const uint VersionHideForOccult = 0x1F; // 31

    /// <summary>Version threshold for reserved1 field.</summary>
    private const uint VersionReserved1 = 0x20; // 32

    /// <summary>Version threshold for pack info (replaces unused2/unused3).</summary>
    private const uint VersionPackInfo = 34;

    /// <summary>Version threshold for 64-bit excludeModifierRegionFlags.</summary>
    private const uint VersionExcludeModifier64 = 36;

    /// <summary>Version threshold for 32-bit flag values.</summary>
    private const uint VersionFlag32Bit = 37;

    #endregion

    #region Properties

    /// <summary>Resource format version.</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Preset count (should always be 0).</summary>
    public uint PresetCount { get; set; }

    /// <summary>Display name of the CAS part.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Sort priority for display ordering.</summary>
    public float SortPriority { get; set; }

    /// <summary>Secondary sort index.</summary>
    public ushort SecondarySortIndex { get; set; }

    /// <summary>Property ID.</summary>
    public uint PropertyId { get; set; }

    /// <summary>Aural material hash for sound effects.</summary>
    public uint AuralMaterialHash { get; set; }

    /// <summary>Parameter flags controlling visibility and behavior.</summary>
    public ParmFlag ParmFlags { get; set; }

    /// <summary>Body part exclusion flags.</summary>
    public ExcludePartFlag ExcludePartFlags { get; set; }

    /// <summary>Modifier region exclusion flags.</summary>
    public ulong ExcludeModifierRegionFlags { get; set; }

    /// <summary>List of catalog tag flags.</summary>
    public CaspFlagList FlagList { get; set; } = new();

    /// <summary>Deprecated price field.</summary>
    public uint DeprecatedPrice { get; set; }

    /// <summary>String table key for part title.</summary>
    public uint PartTitleKey { get; set; }

    /// <summary>String table key for part description.</summary>
    public uint PartDescriptionKey { get; set; }

    /// <summary>Unique texture space flag.</summary>
    public byte UniqueTextureSpace { get; set; }

    /// <summary>Body type for this CAS part.</summary>
    public BodyType BodyType { get; set; }

    /// <summary>Body sub-type.</summary>
    public int BodySubType { get; set; }

    /// <summary>Age and gender flags.</summary>
    public AgeGenderFlags AgeGender { get; set; }

    /// <summary>Reserved field 1 (v32+).</summary>
    public uint Reserved1 { get; set; } = 1;

    /// <summary>Pack ID (v34+).</summary>
    public short PackId { get; set; }

    /// <summary>Pack flags (v34+).</summary>
    public PackFlag PackFlags { get; set; }

    /// <summary>Reserved bytes (v34+, 9 bytes).</summary>
    public byte[] Reserved2 { get; set; } = new byte[9];

    /// <summary>Unused byte 2 (pre-v34).</summary>
    public byte Unused2 { get; set; }

    /// <summary>Unused byte 3 (pre-v34, only if Unused2 > 0).</summary>
    public byte Unused3 { get; set; }

    /// <summary>Swatch colors for this part.</summary>
    public SwatchColorList SwatchColors { get; set; } = new();

    /// <summary>TGI index for buff resource.</summary>
    public byte BuffResKey { get; set; }

    /// <summary>TGI index for variant thumbnail.</summary>
    public byte VariantThumbnailKey { get; set; }

    /// <summary>Voice effect hash (v28+).</summary>
    public ulong VoiceEffectHash { get; set; }

    /// <summary>Number of used materials (v30+).</summary>
    public byte UsedMaterialCount { get; set; }

    /// <summary>Upper body material set hash (v30+, if UsedMaterialCount > 0).</summary>
    public uint MaterialSetUpperBodyHash { get; set; }

    /// <summary>Lower body material set hash (v30+, if UsedMaterialCount > 0).</summary>
    public uint MaterialSetLowerBodyHash { get; set; }

    /// <summary>Shoes material set hash (v30+, if UsedMaterialCount > 0).</summary>
    public uint MaterialSetShoesHash { get; set; }

    /// <summary>Occult types for which this part is hidden (v31+).</summary>
    public OccultTypesDisabled HideForOccultFlags { get; set; }

    /// <summary>TGI index for naked layer.</summary>
    public byte NakedKey { get; set; }

    /// <summary>TGI index for parent part.</summary>
    public byte ParentKey { get; set; }

    /// <summary>Sort layer for rendering order.</summary>
    public int SortLayer { get; set; }

    /// <summary>LOD block list.</summary>
    public LodBlockList LodBlocks { get; set; } = new();

    /// <summary>Slot key indices.</summary>
    public List<byte> SlotKeys { get; set; } = [];

    /// <summary>TGI index for diffuse shadow.</summary>
    public byte DiffuseShadowKey { get; set; }

    /// <summary>TGI index for shadow.</summary>
    public byte ShadowKey { get; set; }

    /// <summary>Composition method.</summary>
    public byte CompositionMethod { get; set; }

    /// <summary>TGI index for region map.</summary>
    public byte RegionMapKey { get; set; }

    /// <summary>Override list.</summary>
    public CaspOverrideList Overrides { get; set; } = new();

    /// <summary>TGI index for normal map.</summary>
    public byte NormalMapKey { get; set; }

    /// <summary>TGI index for specular map.</summary>
    public byte SpecularMapKey { get; set; }

    /// <summary>Shared UV map space (v27+).</summary>
    public uint SharedUvMapSpace { get; set; }

    /// <summary>TGI index for emission map (v30+).</summary>
    public byte EmissionMapKey { get; set; }

    /// <summary>TGI block list for referenced resources.</summary>
    public CaspTgiBlockList TgiBlocks { get; set; } = new();

    #endregion

    /// <summary>
    /// Creates a new CasPartResource.
    /// </summary>
    public CasPartResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;

        // Header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // TGI offset - stored as (actualOffset - 8)
        uint tgiOffsetRaw = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        int tgiOffset = (int)(tgiOffsetRaw + 8);
        offset += 4;

        PresetCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        if (PresetCount != 0)
        {
            throw new InvalidDataException($"CAS Part preset count must be 0, found {PresetCount}");
        }

        // Name (BigEndianUnicode string)
        Name = BigEndianUnicodeString.Read(data[offset..], out int nameBytes);
        offset += nameBytes;

        // Basic properties
        SortPriority = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
        offset += 4;

        SecondarySortIndex = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        PropertyId = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        AuralMaterialHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        ParmFlags = (ParmFlag)data[offset++];

        ExcludePartFlags = (ExcludePartFlag)BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Version-dependent excludeModifierRegionFlags size
        if (Version >= VersionExcludeModifier64)
        {
            ExcludeModifierRegionFlags = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;
        }
        else
        {
            ExcludeModifierRegionFlags = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
        }

        // Flag list (version-aware parsing)
        FlagList = CaspFlagList.Parse(data[offset..], Version, out int flagBytes);
        offset += flagBytes;

        // More basic properties
        DeprecatedPrice = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        PartTitleKey = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        PartDescriptionKey = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        UniqueTextureSpace = data[offset++];

        BodyType = (BodyType)BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        BodySubType = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        AgeGender = (AgeGenderFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Version-gated fields: reserved1
        if (Version >= VersionReserved1)
        {
            Reserved1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
        }

        // Version-gated fields: pack info or unused2/unused3
        if (Version >= VersionPackInfo)
        {
            PackId = BinaryPrimitives.ReadInt16LittleEndian(data[offset..]);
            offset += 2;

            PackFlags = (PackFlag)data[offset++];

            Reserved2 = data.Slice(offset, 9).ToArray();
            offset += 9;
        }
        else
        {
            Unused2 = data[offset++];
            if (Unused2 > 0)
            {
                Unused3 = data[offset++];
            }
        }

        // Swatch colors
        SwatchColors = SwatchColorList.Parse(data[offset..], out int swatchBytes);
        offset += swatchBytes;

        BuffResKey = data[offset++];
        VariantThumbnailKey = data[offset++];

        // Version-gated: voiceEffectHash
        if (Version >= VersionVoiceEffectHash)
        {
            VoiceEffectHash = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            offset += 8;
        }

        // Version-gated: material hashes
        if (Version >= VersionMaterialHashes)
        {
            UsedMaterialCount = data[offset++];
            if (UsedMaterialCount > 0)
            {
                MaterialSetUpperBodyHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;

                MaterialSetLowerBodyHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;

                MaterialSetShoesHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;
            }
        }

        // Version-gated: hideForOccultFlags
        if (Version >= VersionHideForOccult)
        {
            HideForOccultFlags = (OccultTypesDisabled)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
        }

        NakedKey = data[offset++];
        ParentKey = data[offset++];

        SortLayer = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        // Critical: Parse TGI block list BEFORE LOD blocks
        // LOD blocks reference TGI indices, so we need TGI list first
        // Save current position, jump to TGI offset, parse, then return
        int savedOffset = offset;

        if (tgiOffset >= data.Length)
        {
            throw new InvalidDataException($"TGI offset {tgiOffset} exceeds data length {data.Length}");
        }

        TgiBlocks = CaspTgiBlockList.Parse(data[tgiOffset..], out _);

        // Return to saved position and continue parsing
        offset = savedOffset;

        // LOD blocks
        LodBlocks = LodBlockList.Parse(data[offset..], out int lodBytes);
        offset += lodBytes;

        // Slot keys
        byte slotKeyCount = data[offset++];
        SlotKeys = new List<byte>(slotKeyCount);
        for (int i = 0; i < slotKeyCount; i++)
        {
            SlotKeys.Add(data[offset++]);
        }

        DiffuseShadowKey = data[offset++];
        ShadowKey = data[offset++];
        CompositionMethod = data[offset++];
        RegionMapKey = data[offset++];

        // Overrides
        Overrides = CaspOverrideList.Parse(data[offset..], out int overrideBytes);
        offset += overrideBytes;

        NormalMapKey = data[offset++];
        SpecularMapKey = data[offset++];

        // Version-gated: sharedUVMapSpace
        if (Version >= VersionSharedUvMapSpace)
        {
            SharedUvMapSpace = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
        }

        // Version-gated: emissionMapKey
        if (Version >= VersionMaterialHashes)
        {
            EmissionMapKey = data[offset++];
        }
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        int size = CalculateSerializedSize();
        var buffer = new byte[size];
        var span = buffer.AsSpan();
        int offset = 0;

        // Header
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], Version);
        offset += 4;

        // TGI offset placeholder (will be patched later)
        int tgiOffsetPosition = offset;
        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], 0);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], PresetCount);
        offset += 4;

        offset += BigEndianUnicodeString.Write(span[offset..], Name);

        BinaryPrimitives.WriteSingleLittleEndian(span[offset..], SortPriority);
        offset += 4;

        BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], SecondarySortIndex);
        offset += 2;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], PropertyId);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], AuralMaterialHash);
        offset += 4;

        span[offset++] = (byte)ParmFlags;

        BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], (ulong)ExcludePartFlags);
        offset += 8;

        if (Version >= VersionExcludeModifier64)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], ExcludeModifierRegionFlags);
            offset += 8;
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], (uint)ExcludeModifierRegionFlags);
            offset += 4;
        }

        offset += FlagList.WriteTo(span[offset..], Version);

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], DeprecatedPrice);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], PartTitleKey);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], PartDescriptionKey);
        offset += 4;

        span[offset++] = UniqueTextureSpace;

        BinaryPrimitives.WriteInt32LittleEndian(span[offset..], (int)BodyType);
        offset += 4;

        BinaryPrimitives.WriteInt32LittleEndian(span[offset..], BodySubType);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], (uint)AgeGender);
        offset += 4;

        if (Version >= VersionReserved1)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], Reserved1);
            offset += 4;
        }

        if (Version >= VersionPackInfo)
        {
            BinaryPrimitives.WriteInt16LittleEndian(span[offset..], PackId);
            offset += 2;

            span[offset++] = (byte)PackFlags;

            Reserved2.AsSpan().CopyTo(span[offset..]);
            offset += 9;
        }
        else
        {
            span[offset++] = Unused2;
            if (Unused2 > 0)
            {
                span[offset++] = Unused3;
            }
        }

        offset += SwatchColors.WriteTo(span[offset..]);

        span[offset++] = BuffResKey;
        span[offset++] = VariantThumbnailKey;

        if (Version >= VersionVoiceEffectHash)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], VoiceEffectHash);
            offset += 8;
        }

        if (Version >= VersionMaterialHashes)
        {
            span[offset++] = UsedMaterialCount;
            if (UsedMaterialCount > 0)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], MaterialSetUpperBodyHash);
                offset += 4;

                BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], MaterialSetLowerBodyHash);
                offset += 4;

                BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], MaterialSetShoesHash);
                offset += 4;
            }
        }

        if (Version >= VersionHideForOccult)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], (uint)HideForOccultFlags);
            offset += 4;
        }

        span[offset++] = NakedKey;
        span[offset++] = ParentKey;

        BinaryPrimitives.WriteInt32LittleEndian(span[offset..], SortLayer);
        offset += 4;

        offset += LodBlocks.WriteTo(span[offset..]);

        span[offset++] = (byte)SlotKeys.Count;
        foreach (byte key in SlotKeys)
        {
            span[offset++] = key;
        }

        span[offset++] = DiffuseShadowKey;
        span[offset++] = ShadowKey;
        span[offset++] = CompositionMethod;
        span[offset++] = RegionMapKey;

        offset += Overrides.WriteTo(span[offset..]);

        span[offset++] = NormalMapKey;
        span[offset++] = SpecularMapKey;

        if (Version >= VersionSharedUvMapSpace)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], SharedUvMapSpace);
            offset += 4;
        }

        if (Version >= VersionMaterialHashes)
        {
            span[offset++] = EmissionMapKey;
        }

        // Now patch the TGI offset
        // TGI offset is stored as (actualPosition - 8)
        int tgiPosition = offset;
        BinaryPrimitives.WriteUInt32LittleEndian(span[tgiOffsetPosition..], (uint)(tgiPosition - 8));

        // Write TGI blocks
        offset += TgiBlocks.WriteTo(span[offset..]);

        return buffer.AsMemory(0, offset);
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = DefaultVersion;
        PresetCount = 0;
        Name = string.Empty;
        Reserved1 = 1;
        Reserved2 = new byte[9];
        FlagList = new CaspFlagList();
        SwatchColors = new SwatchColorList();
        LodBlocks = new LodBlockList();
        SlotKeys = [];
        Overrides = new CaspOverrideList();
        TgiBlocks = new CaspTgiBlockList();
    }

    private int CalculateSerializedSize()
    {
        int size = 0;

        // Header: version (4) + tgiOffset (4) + presetCount (4)
        size += 12;

        // Name
        size += BigEndianUnicodeString.GetSerializedSize(Name);

        // Basic properties through ageGender
        // sortPriority(4) + secondarySortIndex(2) + propertyId(4) + auralMaterialHash(4)
        // + parmFlags(1) + excludePartFlags(8)
        size += 23;

        // excludeModifierRegionFlags
        size += Version >= VersionExcludeModifier64 ? 8 : 4;

        // Flag list
        size += FlagList.GetSerializedSize(Version);

        // deprecatedPrice(4) + partTitleKey(4) + partDescriptionKey(4)
        // + uniqueTextureSpace(1) + bodyType(4) + bodySubType(4) + ageGender(4)
        size += 25;

        // Version-gated reserved1
        if (Version >= VersionReserved1)
        {
            size += 4;
        }

        // Version-gated pack info or unused2/unused3
        if (Version >= VersionPackInfo)
        {
            size += 2 + 1 + 9; // packId + packFlags + reserved2
        }
        else
        {
            size += 1; // unused2
            if (Unused2 > 0) size += 1; // unused3
        }

        // Swatch colors
        size += SwatchColors.GetSerializedSize();

        // buffResKey + variantThumbnailKey
        size += 2;

        // Version-gated voiceEffectHash
        if (Version >= VersionVoiceEffectHash)
        {
            size += 8;
        }

        // Version-gated material hashes
        if (Version >= VersionMaterialHashes)
        {
            size += 1; // usedMaterialCount
            if (UsedMaterialCount > 0)
            {
                size += 12; // 3 x uint32
            }
        }

        // Version-gated hideForOccultFlags
        if (Version >= VersionHideForOccult)
        {
            size += 4;
        }

        // nakedKey + parentKey + sortLayer
        size += 2 + 4;

        // LOD blocks
        size += LodBlocks.GetSerializedSize();

        // Slot keys
        size += 1 + SlotKeys.Count;

        // diffuseShadowKey + shadowKey + compositionMethod + regionMapKey
        size += 4;

        // Overrides
        size += Overrides.GetSerializedSize();

        // normalMapKey + specularMapKey
        size += 2;

        // Version-gated sharedUVMapSpace
        if (Version >= VersionSharedUvMapSpace)
        {
            size += 4;
        }

        // Version-gated emissionMapKey
        if (Version >= VersionMaterialHashes)
        {
            size += 1;
        }

        // TGI blocks
        size += TgiBlocks.GetSerializedSize();

        return size;
    }
}
