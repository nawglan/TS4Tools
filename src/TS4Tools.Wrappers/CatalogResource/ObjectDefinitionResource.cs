using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Object Definition resource (0xC0DB5AE7).
/// Contains object properties with a table-based format where property IDs point to data at offsets.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/ObjectDefinitionResource.cs lines 34-663
/// </summary>
public sealed class ObjectDefinitionResource : TypedResource
{
    /// <summary>
    /// Type ID for Object Definition resources.
    /// </summary>
    public const uint TypeId = 0xC0DB5AE7;

    #region Properties

    /// <summary>
    /// Resource version.
    /// Source: ObjectDefinitionResource.cs line 38
    /// </summary>
    public ushort Version { get; private set; }

    /// <summary>
    /// Object name string.
    /// Source: ObjectDefinitionResource.cs line 39, PropertyID.Name = 0xE7F07786
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Tuning string.
    /// Source: ObjectDefinitionResource.cs line 40, PropertyID.Tuning = 0x790FA4BC
    /// </summary>
    public string? Tuning { get; set; }

    /// <summary>
    /// Tuning ID.
    /// Source: ObjectDefinitionResource.cs line 41, PropertyID.TuningID = 0xB994039B
    /// </summary>
    public ulong? TuningId { get; set; }

    /// <summary>
    /// Icon TGI references (swapped instance format).
    /// Source: ObjectDefinitionResource.cs line 43, PropertyID.Icon = 0xCADED888
    /// </summary>
    public List<TgiReference>? Icon { get; set; }

    /// <summary>
    /// Rig TGI references (swapped instance format).
    /// Source: ObjectDefinitionResource.cs line 44, PropertyID.Rig = 0xE206AE4F
    /// </summary>
    public List<TgiReference>? Rig { get; set; }

    /// <summary>
    /// Slot TGI references (swapped instance format).
    /// Source: ObjectDefinitionResource.cs line 45, PropertyID.Slot = 0x8A85AFF3
    /// </summary>
    public List<TgiReference>? Slot { get; set; }

    /// <summary>
    /// Model TGI references (swapped instance format).
    /// Source: ObjectDefinitionResource.cs line 46, PropertyID.Model = 0x8D20ACC6
    /// </summary>
    public List<TgiReference>? Model { get; set; }

    /// <summary>
    /// Footprint TGI references (swapped instance format).
    /// Source: ObjectDefinitionResource.cs line 47, PropertyID.Footprint = 0x6C737AD8
    /// </summary>
    public List<TgiReference>? Footprint { get; set; }

    /// <summary>
    /// Component IDs list.
    /// Source: ObjectDefinitionResource.cs line 48, PropertyID.Components = 0xE6E421FB
    /// </summary>
    public List<uint>? Components { get; set; }

    /// <summary>
    /// Material variant string.
    /// Source: ObjectDefinitionResource.cs line 49, PropertyID.MaterialVariant = 0xECD5A95F
    /// </summary>
    public string? MaterialVariant { get; set; }

    /// <summary>
    /// Unknown byte value.
    /// Source: ObjectDefinitionResource.cs line 50, PropertyID.Unknown1 = 0xAC8E1BC0
    /// </summary>
    public byte? Unknown1 { get; set; }

    /// <summary>
    /// Simoleon price.
    /// Source: ObjectDefinitionResource.cs line 51, PropertyID.SimoleonPrice = 0xE4F4FAA4
    /// </summary>
    public uint? SimoleonPrice { get; set; }

    /// <summary>
    /// Positive environment score.
    /// Source: ObjectDefinitionResource.cs line 52, PropertyID.PositiveEnvironmentScore = 0x7236BEEA
    /// </summary>
    public float? PositiveEnvironmentScore { get; set; }

    /// <summary>
    /// Negative environment score.
    /// Source: ObjectDefinitionResource.cs line 53, PropertyID.NegativeEnvironmentScore = 0x44FC7512
    /// </summary>
    public float? NegativeEnvironmentScore { get; set; }

    /// <summary>
    /// Thumbnail geometry state.
    /// Source: ObjectDefinitionResource.cs line 54, PropertyID.ThumbnailGeometryState = 0x4233F8A0
    /// </summary>
    public uint? ThumbnailGeometryState { get; set; }

    /// <summary>
    /// Unknown boolean value.
    /// Source: ObjectDefinitionResource.cs line 55, PropertyID.Unknown2 = 0xEC3712E6
    /// </summary>
    public bool? Unknown2 { get; set; }

    /// <summary>
    /// Environment score emotion tags (ushort values).
    /// Source: ObjectDefinitionResource.cs line 56, PropertyID.EnvironmentScoreEmotionTags = 0x2172AEBE
    /// </summary>
    public List<ushort>? EnvironmentScoreEmotionTags { get; set; }

    /// <summary>
    /// Environment scores array.
    /// Source: ObjectDefinitionResource.cs line 57, PropertyID.EnvironmentScores = 0xDCD08394
    /// </summary>
    public List<float>? EnvironmentScores { get; set; }

    /// <summary>
    /// Unknown ulong value.
    /// Source: ObjectDefinitionResource.cs line 58, PropertyID.Unknown3 = 0x52F7F4BC
    /// </summary>
    public ulong? Unknown3 { get; set; }

    /// <summary>
    /// Is baby flag.
    /// Source: ObjectDefinitionResource.cs line 59, PropertyID.IsBaby = 0xAEE67A1C
    /// </summary>
    public bool? IsBaby { get; set; }

    /// <summary>
    /// Unknown byte array.
    /// Source: ObjectDefinitionResource.cs line 60, PropertyID.Unknown4 = 0xF3936A90
    /// </summary>
    public byte[]? Unknown4 { get; set; }

    /// <summary>
    /// List of property IDs in original order (for round-trip serialization).
    /// </summary>
    private List<ObjectDefinitionPropertyId> _propertyOrder = [];

    #endregion

    /// <summary>
    /// Creates a new Object Definition resource.
    /// </summary>
    public ObjectDefinitionResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            InitializeDefaults();
            return;
        }

        int offset = 0;

        // Read version and table position
        // Source: ObjectDefinitionResource.cs lines 79-80
        Version = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        uint tablePosition = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);

        // Validate table position
        if (tablePosition > data.Length)
            throw new InvalidDataException($"Invalid table position: 0x{tablePosition:X8}");

        // Read property table at tablePosition
        // Source: ObjectDefinitionResource.cs lines 82-85
        int tableOffset = (int)tablePosition;
        ushort entryCount = BinaryPrimitives.ReadUInt16LittleEndian(data[tableOffset..]);
        tableOffset += 2;

        if (entryCount > 1000)
            throw new InvalidDataException($"Invalid entry count: {entryCount}");

        _propertyOrder = new List<ObjectDefinitionPropertyId>(entryCount);

        // Parse each property entry
        // Source: ObjectDefinitionResource.cs lines 86-184
        for (int i = 0; i < entryCount; i++)
        {
            uint typeValue = BinaryPrimitives.ReadUInt32LittleEndian(data[tableOffset..]);
            tableOffset += 4;
            uint dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[tableOffset..]);
            tableOffset += 4;

            if (dataOffset > data.Length)
                throw new InvalidDataException($"Invalid data offset: 0x{dataOffset:X8}");

            var id = (ObjectDefinitionPropertyId)typeValue;
            _propertyOrder.Add(id);

            int propOffset = (int)dataOffset;
            ParseProperty(data, id, ref propOffset);
        }
    }

    private void ParseProperty(ReadOnlySpan<byte> data, ObjectDefinitionPropertyId id, ref int offset)
    {
        switch (id)
        {
            case ObjectDefinitionPropertyId.Name:
                Name = ReadString(data, ref offset);
                break;
            case ObjectDefinitionPropertyId.Tuning:
                Tuning = ReadString(data, ref offset);
                break;
            case ObjectDefinitionPropertyId.TuningId:
                TuningId = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
                offset += 8;
                break;
            case ObjectDefinitionPropertyId.Icon:
                Icon = ReadSwappedTgiBlockList(data, ref offset);
                break;
            case ObjectDefinitionPropertyId.Rig:
                Rig = ReadSwappedTgiBlockList(data, ref offset);
                break;
            case ObjectDefinitionPropertyId.Slot:
                Slot = ReadSwappedTgiBlockList(data, ref offset);
                break;
            case ObjectDefinitionPropertyId.Model:
                Model = ReadSwappedTgiBlockList(data, ref offset);
                break;
            case ObjectDefinitionPropertyId.Footprint:
                Footprint = ReadSwappedTgiBlockList(data, ref offset);
                break;
            case ObjectDefinitionPropertyId.Components:
                int componentCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;
                Components = new List<uint>(componentCount);
                for (int j = 0; j < componentCount; j++)
                {
                    Components.Add(BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]));
                    offset += 4;
                }
                break;
            case ObjectDefinitionPropertyId.MaterialVariant:
                MaterialVariant = ReadString(data, ref offset);
                break;
            case ObjectDefinitionPropertyId.Unknown1:
                Unknown1 = data[offset++];
                break;
            case ObjectDefinitionPropertyId.SimoleonPrice:
                SimoleonPrice = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;
                break;
            case ObjectDefinitionPropertyId.PositiveEnvironmentScore:
                PositiveEnvironmentScore = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
                offset += 4;
                break;
            case ObjectDefinitionPropertyId.NegativeEnvironmentScore:
                NegativeEnvironmentScore = BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
                offset += 4;
                break;
            case ObjectDefinitionPropertyId.ThumbnailGeometryState:
                ThumbnailGeometryState = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
                offset += 4;
                break;
            case ObjectDefinitionPropertyId.Unknown2:
                Unknown2 = data[offset++] != 0;
                break;
            case ObjectDefinitionPropertyId.EnvironmentScoreEmotionTags:
                int tagCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;
                EnvironmentScoreEmotionTags = new List<ushort>(tagCount);
                for (int j = 0; j < tagCount; j++)
                {
                    EnvironmentScoreEmotionTags.Add(BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]));
                    offset += 2;
                }
                break;
            case ObjectDefinitionPropertyId.EnvironmentScores:
                int scoreCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;
                EnvironmentScores = new List<float>(scoreCount);
                for (int j = 0; j < scoreCount; j++)
                {
                    EnvironmentScores.Add(BinaryPrimitives.ReadSingleLittleEndian(data[offset..]));
                    offset += 4;
                }
                break;
            case ObjectDefinitionPropertyId.Unknown3:
                Unknown3 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
                offset += 8;
                break;
            case ObjectDefinitionPropertyId.IsBaby:
                IsBaby = data[offset++] != 0;
                break;
            case ObjectDefinitionPropertyId.Unknown4:
                int byteCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
                offset += 4;
                Unknown4 = data.Slice(offset, byteCount).ToArray();
                offset += byteCount;
                break;
        }
    }

    private static string ReadString(ReadOnlySpan<byte> data, ref int offset)
    {
        int length = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;
        if (length < 0 || length > 10000)
            throw new InvalidDataException($"Invalid string length: {length}");
        string result = Encoding.ASCII.GetString(data.Slice(offset, length));
        offset += length;
        return result;
    }

    /// <summary>
    /// Reads a TGI block list with swapped instance bytes.
    /// Source: ObjectDefinitionResource.cs lines 298-326
    /// </summary>
    private static List<TgiReference> ReadSwappedTgiBlockList(ReadOnlySpan<byte> data, ref int offset)
    {
        // Count is (byte count / 4) in legacy code, but each TGI is 20 bytes (I=8, T=4, G=4)
        // Actually legacy does count / 4 which seems wrong - let me check
        // Legacy: int count = r.ReadInt32() / 4;
        // This doesn't make sense for TGI blocks which are 20 bytes each
        // Looking more carefully: it's count = r.ReadInt32() / 4 then reads count TGI blocks
        // So the value stored is actually count * 4, not byte size
        int countValue = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;
        int count = countValue / 4;

        var list = new List<TgiReference>(count);
        for (int i = 0; i < count; i++)
        {
            // Instance is stored with high/low 32 bits swapped
            // Source: ObjectDefinitionResource.cs lines 309-310
            ulong instanceRaw = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
            ulong instance = (instanceRaw << 32) | (instanceRaw >> 32);
            offset += 8;

            uint type = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            uint group = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;

            list.Add(new TgiReference(instance, type, group));
        }
        return list;
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        int totalSize = GetSerializedSize();
        var buffer = new byte[totalSize];
        int offset = 0;

        // Write version
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), Version);
        offset += 2;

        int tablePositionOffset = offset;
        offset += 4; // Skip table position, fill in later

        // Track positions for each property
        var positionTable = new List<int>(_propertyOrder.Count);

        // Write property data
        // Source: ObjectDefinitionResource.cs lines 201-282
        foreach (var id in _propertyOrder)
        {
            positionTable.Add(offset);
            SerializeProperty(buffer.AsSpan(), id, ref offset);
        }

        // Record table position
        int tablePosition = offset;

        // Write table position back at offset 2
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(tablePositionOffset), (uint)tablePosition);

        // Write property table
        // Source: ObjectDefinitionResource.cs lines 283-289
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), (ushort)_propertyOrder.Count);
        offset += 2;

        for (int i = 0; i < _propertyOrder.Count; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), (uint)_propertyOrder[i]);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), (uint)positionTable[i]);
            offset += 4;
        }

        return buffer;
    }

    private void SerializeProperty(Span<byte> buffer, ObjectDefinitionPropertyId id, ref int offset)
    {
        switch (id)
        {
            case ObjectDefinitionPropertyId.Name:
                WriteString(buffer, Name ?? string.Empty, ref offset);
                break;
            case ObjectDefinitionPropertyId.Tuning:
                WriteString(buffer, Tuning ?? string.Empty, ref offset);
                break;
            case ObjectDefinitionPropertyId.TuningId:
                BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], TuningId ?? 0);
                offset += 8;
                break;
            case ObjectDefinitionPropertyId.Icon:
                WriteSwappedTgiBlockList(buffer, Icon ?? [], ref offset);
                break;
            case ObjectDefinitionPropertyId.Rig:
                WriteSwappedTgiBlockList(buffer, Rig ?? [], ref offset);
                break;
            case ObjectDefinitionPropertyId.Slot:
                WriteSwappedTgiBlockList(buffer, Slot ?? [], ref offset);
                break;
            case ObjectDefinitionPropertyId.Model:
                WriteSwappedTgiBlockList(buffer, Model ?? [], ref offset);
                break;
            case ObjectDefinitionPropertyId.Footprint:
                WriteSwappedTgiBlockList(buffer, Footprint ?? [], ref offset);
                break;
            case ObjectDefinitionPropertyId.Components:
                var components = Components ?? [];
                BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], components.Count);
                offset += 4;
                foreach (var c in components)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], c);
                    offset += 4;
                }
                break;
            case ObjectDefinitionPropertyId.MaterialVariant:
                WriteString(buffer, MaterialVariant ?? string.Empty, ref offset);
                break;
            case ObjectDefinitionPropertyId.Unknown1:
                buffer[offset++] = Unknown1 ?? 0;
                break;
            case ObjectDefinitionPropertyId.SimoleonPrice:
                BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], SimoleonPrice ?? 0);
                offset += 4;
                break;
            case ObjectDefinitionPropertyId.PositiveEnvironmentScore:
                BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], PositiveEnvironmentScore ?? 0f);
                offset += 4;
                break;
            case ObjectDefinitionPropertyId.NegativeEnvironmentScore:
                BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], NegativeEnvironmentScore ?? 0f);
                offset += 4;
                break;
            case ObjectDefinitionPropertyId.ThumbnailGeometryState:
                BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], ThumbnailGeometryState ?? 0);
                offset += 4;
                break;
            case ObjectDefinitionPropertyId.Unknown2:
                buffer[offset++] = (byte)(Unknown2 == true ? 1 : 0);
                break;
            case ObjectDefinitionPropertyId.EnvironmentScoreEmotionTags:
                var tags = EnvironmentScoreEmotionTags ?? [];
                BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], tags.Count);
                offset += 4;
                foreach (var t in tags)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], t);
                    offset += 2;
                }
                break;
            case ObjectDefinitionPropertyId.EnvironmentScores:
                var scores = EnvironmentScores ?? [];
                BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], scores.Count);
                offset += 4;
                foreach (var s in scores)
                {
                    BinaryPrimitives.WriteSingleLittleEndian(buffer[offset..], s);
                    offset += 4;
                }
                break;
            case ObjectDefinitionPropertyId.Unknown3:
                BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], Unknown3 ?? 0);
                offset += 8;
                break;
            case ObjectDefinitionPropertyId.IsBaby:
                buffer[offset++] = (byte)(IsBaby == true ? 1 : 0);
                break;
            case ObjectDefinitionPropertyId.Unknown4:
                var bytes = Unknown4 ?? [];
                BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], bytes.Length);
                offset += 4;
                bytes.CopyTo(buffer[offset..]);
                offset += bytes.Length;
                break;
        }
    }

    private static void WriteString(Span<byte> buffer, string value, ref int offset)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], value.Length);
        offset += 4;
        Encoding.ASCII.GetBytes(value, buffer[offset..]);
        offset += value.Length;
    }

    /// <summary>
    /// Writes a TGI block list with swapped instance bytes.
    /// Source: ObjectDefinitionResource.cs lines 334-353
    /// </summary>
    private static void WriteSwappedTgiBlockList(Span<byte> buffer, List<TgiReference> list, ref int offset)
    {
        // Write count * 4
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], list.Count * 4);
        offset += 4;

        foreach (var tgi in list)
        {
            // Swap instance high/low 32 bits
            ulong instance = (tgi.Instance << 32) | (tgi.Instance >> 32);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], instance);
            offset += 8;

            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], tgi.Type);
            offset += 4;

            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], tgi.Group);
            offset += 4;
        }
    }

    /// <summary>
    /// Gets the total serialized size of the resource.
    /// </summary>
    private int GetSerializedSize()
    {
        int size = 2 + 4; // version + table position

        // Property data sizes
        foreach (var id in _propertyOrder)
        {
            size += GetPropertySize(id);
        }

        // Property table: count (2) + entries (8 each)
        size += 2 + (_propertyOrder.Count * 8);

        return size;
    }

    private int GetPropertySize(ObjectDefinitionPropertyId id)
    {
        return id switch
        {
            ObjectDefinitionPropertyId.Name => 4 + (Name?.Length ?? 0),
            ObjectDefinitionPropertyId.Tuning => 4 + (Tuning?.Length ?? 0),
            ObjectDefinitionPropertyId.TuningId => 8,
            ObjectDefinitionPropertyId.Icon => 4 + ((Icon?.Count ?? 0) * 20),
            ObjectDefinitionPropertyId.Rig => 4 + ((Rig?.Count ?? 0) * 20),
            ObjectDefinitionPropertyId.Slot => 4 + ((Slot?.Count ?? 0) * 20),
            ObjectDefinitionPropertyId.Model => 4 + ((Model?.Count ?? 0) * 20),
            ObjectDefinitionPropertyId.Footprint => 4 + ((Footprint?.Count ?? 0) * 20),
            ObjectDefinitionPropertyId.Components => 4 + ((Components?.Count ?? 0) * 4),
            ObjectDefinitionPropertyId.MaterialVariant => 4 + (MaterialVariant?.Length ?? 0),
            ObjectDefinitionPropertyId.Unknown1 => 1,
            ObjectDefinitionPropertyId.SimoleonPrice => 4,
            ObjectDefinitionPropertyId.PositiveEnvironmentScore => 4,
            ObjectDefinitionPropertyId.NegativeEnvironmentScore => 4,
            ObjectDefinitionPropertyId.ThumbnailGeometryState => 4,
            ObjectDefinitionPropertyId.Unknown2 => 1,
            ObjectDefinitionPropertyId.EnvironmentScoreEmotionTags => 4 + ((EnvironmentScoreEmotionTags?.Count ?? 0) * 2),
            ObjectDefinitionPropertyId.EnvironmentScores => 4 + ((EnvironmentScores?.Count ?? 0) * 4),
            ObjectDefinitionPropertyId.Unknown3 => 8,
            ObjectDefinitionPropertyId.IsBaby => 1,
            ObjectDefinitionPropertyId.Unknown4 => 4 + (Unknown4?.Length ?? 0),
            _ => 0
        };
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = 1;
        _propertyOrder = [];
    }

    /// <summary>
    /// Checks if a property is present in the resource.
    /// </summary>
    public bool HasProperty(ObjectDefinitionPropertyId id) => _propertyOrder.Contains(id);

    /// <summary>
    /// Adds a property to the resource. Has no effect if property already exists.
    /// </summary>
    public void AddProperty(ObjectDefinitionPropertyId id)
    {
        if (!_propertyOrder.Contains(id))
            _propertyOrder.Add(id);
    }

    /// <summary>
    /// Removes a property from the resource.
    /// </summary>
    public void RemoveProperty(ObjectDefinitionPropertyId id)
    {
        _propertyOrder.Remove(id);
    }
}

/// <summary>
/// Property IDs used in Object Definition resources.
/// Source: ObjectDefinitionResource.cs lines 389-412
/// </summary>
public enum ObjectDefinitionPropertyId : uint
{
    /// <summary>Object name (string)</summary>
    Name = 0xE7F07786,

    /// <summary>Tuning (string)</summary>
    Tuning = 0x790FA4BC,

    /// <summary>Tuning ID (ulong)</summary>
    TuningId = 0xB994039B,

    /// <summary>Icon TGI list (swapped ITG)</summary>
    Icon = 0xCADED888,

    /// <summary>Rig TGI list (swapped ITG)</summary>
    Rig = 0xE206AE4F,

    /// <summary>Slot TGI list (swapped ITG)</summary>
    Slot = 0x8A85AFF3,

    /// <summary>Model TGI list (swapped ITG)</summary>
    Model = 0x8D20ACC6,

    /// <summary>Footprint TGI list (swapped ITG)</summary>
    Footprint = 0x6C737AD8,

    /// <summary>Components (uint32 list)</summary>
    Components = 0xE6E421FB,

    /// <summary>Material variant (string)</summary>
    MaterialVariant = 0xECD5A95F,

    /// <summary>Unknown byte</summary>
    Unknown1 = 0xAC8E1BC0,

    /// <summary>Simoleon price (uint32)</summary>
    SimoleonPrice = 0xE4F4FAA4,

    /// <summary>Positive environment score (float)</summary>
    PositiveEnvironmentScore = 0x7236BEEA,

    /// <summary>Negative environment score (float)</summary>
    NegativeEnvironmentScore = 0x44FC7512,

    /// <summary>Thumbnail geometry state (uint32)</summary>
    ThumbnailGeometryState = 0x4233F8A0,

    /// <summary>Unknown boolean</summary>
    Unknown2 = 0xEC3712E6,

    /// <summary>Environment score emotion tags (uint16 array)</summary>
    EnvironmentScoreEmotionTags = 0x2172AEBE,

    /// <summary>Environment scores (float array)</summary>
    EnvironmentScores = 0xDCD08394,

    /// <summary>Unknown ulong</summary>
    Unknown3 = 0x52F7F4BC,

    /// <summary>Is baby flag (bool)</summary>
    IsBaby = 0xAEE67A1C,

    /// <summary>Unknown bytes</summary>
    Unknown4 = 0xF3936A90,
}
