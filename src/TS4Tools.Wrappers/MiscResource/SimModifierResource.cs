using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// SMOD (SimModifier) resource for storing sim body modifiers.
/// Resource Type: 0xC5F6763E
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SimModifierResource.cs
///
/// Format:
/// - ContexData header (version, key lists)
/// - Version: uint32
/// - Gender: uint32
/// - Region: uint32
/// - LinkTag: uint32
/// - BonePoseKey: TGIBlock (ITG order, 16 bytes)
/// - DeformerMapShapeKey: TGIBlock (ITG order, 16 bytes)
/// - DeformerMapNormalKey: TGIBlock (ITG order, 16 bytes)
/// - BoneEntryCount: uint32
/// - BoneEntries: BoneEntry[] (8 bytes each)
///
/// ContexData format:
/// - Version: uint32
/// - PublicKeyCount: uint32
/// - ExternalKeyCount: uint32
/// - DelayLoadKeyCount: uint32
/// - ObjectKeyCount: uint32
/// - PublicKeys: TGIBlock[] (ITG order, 16 bytes each)
/// - ExternalKeys: TGIBlock[] (ITG order, 16 bytes each)
/// - DelayLoadKeys: TGIBlock[] (ITG order, 16 bytes each)
/// - ObjectData: ObjectDataEntry[] (8 bytes each)
/// </summary>
public sealed class SimModifierResource : TypedResource
{
    // Validation limits
    private const int MaxKeyCount = 10000;
    private const int MaxBoneEntryCount = 10000;

    // Sizes
    private const int TgiBlockSize = 16; // Instance (8) + Type (4) + Group (4)
    private const int ObjectDataSize = 8; // Position (4) + Length (4)
    private const int BoneEntrySize = 8; // BoneHash (4) + Multiplier (4)

    #region Fields

    private uint _version;
    private uint _gender;
    private uint _region;
    private uint _linkTag;
    private ResourceKey _bonePoseKey;
    private ResourceKey _deformerMapShapeKey;
    private ResourceKey _deformerMapNormalKey;
    private List<SimModifierBoneEntry> _boneEntries;

    #endregion

    #region Properties

    /// <summary>
    /// The context data header.
    /// </summary>
    public SimModifierContextData ContextData { get; private set; }

    /// <summary>
    /// The format version.
    /// </summary>
    public uint Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// The gender flag.
    /// </summary>
    public uint Gender
    {
        get => _gender;
        set
        {
            if (_gender != value)
            {
                _gender = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// The body region.
    /// </summary>
    public uint Region
    {
        get => _region;
        set
        {
            if (_region != value)
            {
                _region = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// The link tag.
    /// </summary>
    public uint LinkTag
    {
        get => _linkTag;
        set
        {
            if (_linkTag != value)
            {
                _linkTag = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Reference to the bone pose resource.
    /// </summary>
    public ResourceKey BonePoseKey
    {
        get => _bonePoseKey;
        set
        {
            if (_bonePoseKey != value)
            {
                _bonePoseKey = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Reference to the deformer map shape resource.
    /// </summary>
    public ResourceKey DeformerMapShapeKey
    {
        get => _deformerMapShapeKey;
        set
        {
            if (_deformerMapShapeKey != value)
            {
                _deformerMapShapeKey = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// Reference to the deformer map normal resource.
    /// </summary>
    public ResourceKey DeformerMapNormalKey
    {
        get => _deformerMapNormalKey;
        set
        {
            if (_deformerMapNormalKey != value)
            {
                _deformerMapNormalKey = value;
                OnChanged();
            }
        }
    }

    /// <summary>
    /// The bone entries.
    /// </summary>
    public IReadOnlyList<SimModifierBoneEntry> BoneEntries => _boneEntries;

    #endregion

    /// <summary>
    /// Creates a new SMOD resource by parsing data.
    /// </summary>
    public SimModifierResource(ResourceKey key, ReadOnlyMemory<byte> data) : base(key, data)
    {
        _boneEntries ??= [];
        ContextData ??= new SimModifierContextData();
    }

    #region Bone Entry Management

    /// <summary>
    /// Adds a bone entry.
    /// </summary>
    public void AddBoneEntry(SimModifierBoneEntry entry)
    {
        _boneEntries.Add(entry);
        OnChanged();
    }

    /// <summary>
    /// Removes a bone entry.
    /// </summary>
    public bool RemoveBoneEntry(SimModifierBoneEntry entry)
    {
        if (_boneEntries.Remove(entry))
        {
            OnChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all bone entries.
    /// </summary>
    public void ClearBoneEntries()
    {
        if (_boneEntries.Count > 0)
        {
            _boneEntries.Clear();
            OnChanged();
        }
    }

    /// <summary>
    /// Sets the bone entry at the specified index.
    /// </summary>
    public void SetBoneEntry(int index, SimModifierBoneEntry entry)
    {
        if (index < 0 || index >= _boneEntries.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _boneEntries[index] = entry;
        OnChanged();
    }

    #endregion

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Source: SimModifierResource.cs lines 52-64
        int offset = 0;

        // Parse ContexData header
        // Source: SimModifierResource.cs line 55
        ContextData = ParseContextData(data, ref offset);

        // Parse main resource fields
        // Source: SimModifierResource.cs lines 56-63
        _version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        _gender = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        _region = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        _linkTag = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Read TGI blocks in ITG order (Instance, Type, Group)
        // Source: SimModifierResource.cs lines 60-62
        _bonePoseKey = ReadTgiBlock(data, ref offset);
        _deformerMapShapeKey = ReadTgiBlock(data, ref offset);
        _deformerMapNormalKey = ReadTgiBlock(data, ref offset);

        // Read bone entries
        // Source: SimModifierResource.cs line 63 - BoneEntryLIst constructor
        // Source: BoneEntryLIst.Parse lines 238-243
        if (offset + 4 > data.Length)
            throw new InvalidDataException("Data too short for bone entry count");

        int boneCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (boneCount < 0 || boneCount > MaxBoneEntryCount)
            throw new InvalidDataException($"Invalid bone entry count: {boneCount}");

        int requiredBoneDataLength = boneCount * BoneEntrySize;
        if (offset + requiredBoneDataLength > data.Length)
            throw new InvalidDataException($"Data too short for {boneCount} bone entries");

        // Parse bone entries
        // Source: BoneEntry.Parse lines 265-270
        _boneEntries = new List<SimModifierBoneEntry>(boneCount);
        for (int i = 0; i < boneCount; i++)
        {
            uint boneHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            float multiplier = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
            offset += 4;

            _boneEntries.Add(new SimModifierBoneEntry(boneHash, multiplier));
        }
    }

    private static SimModifierContextData ParseContextData(ReadOnlySpan<byte> data, ref int offset)
    {
        // Source: ContexData.Parse lines 106-124
        if (offset + 20 > data.Length)
            throw new InvalidDataException("Data too short for ContexData header");

        uint version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        uint publicKeyCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        uint externalKeyCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        uint delayLoadKeyCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        uint objectKeyCount = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // Validate counts
        if (publicKeyCount > MaxKeyCount || externalKeyCount > MaxKeyCount ||
            delayLoadKeyCount > MaxKeyCount || objectKeyCount > MaxKeyCount)
        {
            throw new InvalidDataException("Invalid key count in ContexData");
        }

        // Calculate required size
        int keyDataSize = (int)(publicKeyCount + externalKeyCount + delayLoadKeyCount) * TgiBlockSize;
        int objectDataSize = (int)objectKeyCount * ObjectDataSize;

        if (offset + keyDataSize + objectDataSize > data.Length)
            throw new InvalidDataException("Data too short for ContexData keys");

        // Read public keys
        // Source: ContexData.Parse lines 114-116
        var publicKeys = new List<ResourceKey>((int)publicKeyCount);
        for (int i = 0; i < publicKeyCount; i++)
        {
            publicKeys.Add(ReadTgiBlock(data, ref offset));
        }

        // Read external keys
        // Source: ContexData.Parse lines 117-119
        var externalKeys = new List<ResourceKey>((int)externalKeyCount);
        for (int i = 0; i < externalKeyCount; i++)
        {
            externalKeys.Add(ReadTgiBlock(data, ref offset));
        }

        // Read delay load keys
        // Source: ContexData.Parse lines 120-122
        var delayLoadKeys = new List<ResourceKey>((int)delayLoadKeyCount);
        for (int i = 0; i < delayLoadKeyCount; i++)
        {
            delayLoadKeys.Add(ReadTgiBlock(data, ref offset));
        }

        // Read object data
        // Source: ContexData.Parse line 123 - ObjectDataLIst
        var objectData = new List<SimModifierObjectData>((int)objectKeyCount);
        for (int i = 0; i < objectKeyCount; i++)
        {
            uint position = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            objectData.Add(new SimModifierObjectData(position, length));
        }

        return new SimModifierContextData(version, publicKeys, externalKeys, delayLoadKeys, objectData);
    }

    private static ResourceKey ReadTgiBlock(ReadOnlySpan<byte> data, ref int offset)
    {
        // ITG order: Instance (8 bytes), Type (4 bytes), Group (4 bytes)
        ulong instance = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;
        uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        return new ResourceKey(type, group, instance);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        // Source: SimModifierResource.UnParse lines 66-85

        // Calculate size
        int contextDataSize = CalculateContextDataSize();
        int mainDataSize = 16 + (3 * TgiBlockSize) + 4 + (_boneEntries.Count * BoneEntrySize);
        // 16 = Version (4) + Gender (4) + Region (4) + LinkTag (4)
        // 3 * 16 = 3 TGI blocks
        // 4 = bone count
        // boneEntries.Count * 8 = bone entries

        int totalSize = contextDataSize + mainDataSize;
        var buffer = new byte[totalSize];
        int offset = 0;

        // Write ContexData
        SerializeContextData(buffer.AsSpan(), ref offset);

        // Write main fields
        // Source: SimModifierResource.UnParse lines 72-75
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Gender);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Region);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), LinkTag);
        offset += 4;

        // Write TGI blocks
        // Source: SimModifierResource.UnParse lines 76-81
        WriteTgiBlock(buffer.AsSpan(offset), BonePoseKey);
        offset += TgiBlockSize;
        WriteTgiBlock(buffer.AsSpan(offset), DeformerMapShapeKey);
        offset += TgiBlockSize;
        WriteTgiBlock(buffer.AsSpan(offset), DeformerMapNormalKey);
        offset += TgiBlockSize;

        // Write bone entries
        // Source: BoneEntryLIst.UnParse lines 245-249
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), _boneEntries.Count);
        offset += 4;

        foreach (var entry in _boneEntries)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), entry.BoneHash);
            offset += 4;
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset), entry.Multiplier);
            offset += 4;
        }

        return buffer;
    }

    private int CalculateContextDataSize()
    {
        // 5 uint32 header fields
        int headerSize = 20;

        // Key lists
        int keyCount = ContextData.PublicKeys.Count + ContextData.ExternalKeys.Count + ContextData.DelayLoadKeys.Count;
        int keysSize = keyCount * TgiBlockSize;

        // Object data
        int objectDataSize = ContextData.ObjectData.Count * ObjectDataSize;

        return headerSize + keysSize + objectDataSize;
    }

    private void SerializeContextData(Span<byte> buffer, ref int offset)
    {
        // Source: ContexData.UnParse lines 127-143

        // Write header
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], ContextData.Version);
        offset += 4;
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], ContextData.PublicKeys.Count);
        offset += 4;
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], ContextData.ExternalKeys.Count);
        offset += 4;
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], ContextData.DelayLoadKeys.Count);
        offset += 4;
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], ContextData.ObjectData.Count);
        offset += 4;

        // Write public keys
        foreach (var key in ContextData.PublicKeys)
        {
            WriteTgiBlock(buffer[offset..], key);
            offset += TgiBlockSize;
        }

        // Write external keys
        foreach (var key in ContextData.ExternalKeys)
        {
            WriteTgiBlock(buffer[offset..], key);
            offset += TgiBlockSize;
        }

        // Write delay load keys
        foreach (var key in ContextData.DelayLoadKeys)
        {
            WriteTgiBlock(buffer[offset..], key);
            offset += TgiBlockSize;
        }

        // Write object data
        foreach (var obj in ContextData.ObjectData)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], obj.Position);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], obj.Length);
            offset += 4;
        }
    }

    private static void WriteTgiBlock(Span<byte> buffer, ResourceKey key)
    {
        // ITG order: Instance (8 bytes), Type (4 bytes), Group (4 bytes)
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, key.Instance);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], key.ResourceType);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[12..], key.ResourceGroup);
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        ContextData = new SimModifierContextData();
        _version = 0;
        _gender = 0;
        _region = 0;
        _linkTag = 0;
        _bonePoseKey = default;
        _deformerMapShapeKey = default;
        _deformerMapNormalKey = default;
        _boneEntries = [];
    }
}

/// <summary>
/// Context data header for SMOD resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SimModifierResource.cs lines 89-230
/// </summary>
public sealed class SimModifierContextData
{
    /// <summary>
    /// The context data version.
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// Public resource keys.
    /// </summary>
    public List<ResourceKey> PublicKeys { get; }

    /// <summary>
    /// External resource keys.
    /// </summary>
    public List<ResourceKey> ExternalKeys { get; }

    /// <summary>
    /// Delay load resource keys.
    /// </summary>
    public List<ResourceKey> DelayLoadKeys { get; }

    /// <summary>
    /// Object data entries.
    /// </summary>
    public List<SimModifierObjectData> ObjectData { get; }

    /// <summary>
    /// Creates new context data with default values.
    /// </summary>
    public SimModifierContextData()
    {
        PublicKeys = [];
        ExternalKeys = [];
        DelayLoadKeys = [];
        ObjectData = [];
    }

    /// <summary>
    /// Creates context data with the specified values.
    /// </summary>
    public SimModifierContextData(
        uint version,
        List<ResourceKey> publicKeys,
        List<ResourceKey> externalKeys,
        List<ResourceKey> delayLoadKeys,
        List<SimModifierObjectData> objectData)
    {
        Version = version;
        PublicKeys = publicKeys;
        ExternalKeys = externalKeys;
        DelayLoadKeys = delayLoadKeys;
        ObjectData = objectData;
    }
}

/// <summary>
/// Object data entry for SMOD resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SimModifierResource.cs lines 152-184
/// </summary>
/// <param name="Position">The position in the data stream.</param>
/// <param name="Length">The length of the data.</param>
public readonly record struct SimModifierObjectData(uint Position, uint Length);

/// <summary>
/// Bone entry for SMOD resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SimModifierResource.cs lines 257-292
/// </summary>
/// <param name="BoneHash">The bone name hash.</param>
/// <param name="Multiplier">The modifier multiplier value.</param>
public readonly record struct SimModifierBoneEntry(uint BoneHash, float Multiplier);
