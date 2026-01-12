using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Base class for object catalog resources including roof styles, A8F7B517, and 48C28979 catalog types.
/// Contains common catalog data: version, naming hashes, price, style TGI list, tags, and selling points.
///
/// This is different from SimpleCatalogResource which uses CatalogCommon block.
/// ObjectCatalogResource has its own distinct binary format.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/ObjectCatalogResource.cs lines 30-342
/// </summary>
public class ObjectCatalogResource : TypedResource
{
    /// <summary>
    /// Default version for new resources.
    /// </summary>
    public const uint DefaultVersion = 0x01;

    /// <summary>
    /// Default catalog version (0x09 per legacy code).
    /// </summary>
    public const uint DefaultCatalogVersion = 0x09;

    #region Properties

    /// <summary>
    /// Resource format version.
    /// Source: ObjectCatalogResource.cs line 33
    /// </summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>
    /// Catalog format version.
    /// Source: ObjectCatalogResource.cs line 34
    /// </summary>
    public uint CatalogVersion { get; set; } = DefaultCatalogVersion;

    /// <summary>
    /// FNV32 hash of catalog name STBL key.
    /// Source: ObjectCatalogResource.cs line 35
    /// </summary>
    public uint CatalogNameHash { get; set; }

    /// <summary>
    /// FNV32 hash of catalog description STBL key.
    /// Source: ObjectCatalogResource.cs line 36
    /// </summary>
    public uint CatalogDescHash { get; set; }

    /// <summary>
    /// Price in simoleons.
    /// Source: ObjectCatalogResource.cs line 37
    /// </summary>
    public uint CatalogPrice { get; set; }

    /// <summary>
    /// Unknown field 1.
    /// Source: ObjectCatalogResource.cs line 39
    /// </summary>
    public uint CatalogUnknown1 { get; set; }

    /// <summary>
    /// Unknown field 2.
    /// Source: ObjectCatalogResource.cs line 40
    /// </summary>
    public uint CatalogUnknown2 { get; set; }

    /// <summary>
    /// Unknown field 3.
    /// Source: ObjectCatalogResource.cs line 41
    /// </summary>
    public uint CatalogUnknown3 { get; set; }

    /// <summary>
    /// List of style TGI references (byte count prefix).
    /// Source: ObjectCatalogResource.cs line 43
    /// </summary>
    public TgiReferenceList CatalogStyleTgiList { get; set; } = new();

    /// <summary>
    /// Unknown field 4.
    /// Source: ObjectCatalogResource.cs line 45
    /// </summary>
    public ushort CatalogUnknown4 { get; set; }

    /// <summary>
    /// List of catalog category tags.
    /// Source: ObjectCatalogResource.cs line 46
    /// </summary>
    public List<ushort> CatalogTagList { get; set; } = [];

    /// <summary>
    /// List of selling points.
    /// Source: ObjectCatalogResource.cs line 47
    /// </summary>
    public ObjectCatalogSellingPointList CatalogSellingPointList { get; set; } = new();

    /// <summary>
    /// Unknown field 5 (ulong).
    /// Source: ObjectCatalogResource.cs line 48
    /// </summary>
    public ulong CatalogUnknown5 { get; set; }

    /// <summary>
    /// Unknown field 6 (ushort).
    /// Source: ObjectCatalogResource.cs line 49
    /// </summary>
    public ushort CatalogUnknown6 { get; set; }

    /// <summary>
    /// Unknown field 7 (ulong).
    /// Source: ObjectCatalogResource.cs line 50
    /// </summary>
    public ulong CatalogUnknown7 { get; set; }

    #endregion

    /// <summary>
    /// Creates a new ObjectCatalogResource.
    /// </summary>
    protected ObjectCatalogResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;

        // Base fields (56 bytes header + variable TGI list + variable lists)
        // Source: ObjectCatalogResource.cs lines 56-79
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        CatalogVersion = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        CatalogNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        CatalogDescHash = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        CatalogPrice = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        CatalogUnknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        CatalogUnknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        CatalogUnknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        // TGI list with byte count prefix (ITG order)
        // Source: ObjectCatalogResource.cs lines 67-70
        CatalogStyleTgiList = TgiReferenceList.Parse(data[offset..], out int tgiBytes);
        offset += tgiBytes;

        CatalogUnknown4 = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        // Tag list with int32 count prefix, ushort values
        // Source: ObjectCatalogResource.cs lines 73-75
        int tagCount = BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        offset += 4;

        if (tagCount < 0 || tagCount > 10000)
            throw new InvalidOperationException($"Unreasonable tag count: {tagCount}");

        CatalogTagList = new List<ushort>(tagCount);
        for (int i = 0; i < tagCount; i++)
        {
            CatalogTagList.Add(BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]));
            offset += 2;
        }

        // Selling point list
        // Source: ObjectCatalogResource.cs line 76
        CatalogSellingPointList = ObjectCatalogSellingPointList.Parse(data[offset..], out int spBytes);
        offset += spBytes;

        CatalogUnknown5 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        CatalogUnknown6 = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
        offset += 2;

        CatalogUnknown7 = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;

        // Parse type-specific fields
        ParseTypeSpecific(data, ref offset);
    }

    /// <inheritdoc/>
    protected override ReadOnlyMemory<byte> Serialize()
    {
        int size = CalculateSerializedSize();
        var buffer = new byte[size];
        int offset = 0;

        // Base fields
        // Source: ObjectCatalogResource.cs lines 85-108
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), Version);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), CatalogVersion);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), CatalogNameHash);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), CatalogDescHash);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), CatalogPrice);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), CatalogUnknown1);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), CatalogUnknown2);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), CatalogUnknown3);
        offset += 4;

        // TGI list
        // Note: Legacy writes in ITG order (Instance, Type, Group) - TgiReferenceList handles this
        // Source: ObjectCatalogResource.cs lines 96-97
        offset += CatalogStyleTgiList.WriteTo(buffer.AsSpan(offset));

        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), CatalogUnknown4);
        offset += 2;

        // Tag list
        // Source: ObjectCatalogResource.cs lines 100-102
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), CatalogTagList.Count);
        offset += 4;

        foreach (ushort tag in CatalogTagList)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), tag);
            offset += 2;
        }

        // Selling point list
        // Source: ObjectCatalogResource.cs lines 103-104
        offset += CatalogSellingPointList.WriteTo(buffer.AsSpan(offset));

        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), CatalogUnknown5);
        offset += 8;

        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), CatalogUnknown6);
        offset += 2;

        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset), CatalogUnknown7);
        offset += 8;

        // Type-specific fields
        SerializeTypeSpecific(buffer.AsSpan(), ref offset);

        return buffer;
    }

    /// <inheritdoc/>
    protected override void InitializeDefaults()
    {
        Version = DefaultVersion;
        CatalogVersion = DefaultCatalogVersion;
        CatalogNameHash = 0;
        CatalogDescHash = 0;
        CatalogPrice = 0;
        CatalogUnknown1 = 0;
        CatalogUnknown2 = 0;
        CatalogUnknown3 = 0;
        CatalogStyleTgiList = new TgiReferenceList();
        CatalogUnknown4 = 0;
        CatalogTagList = [];
        CatalogSellingPointList = new ObjectCatalogSellingPointList();
        CatalogUnknown5 = 0;
        CatalogUnknown6 = 0;
        CatalogUnknown7 = 0;

        InitializeTypeSpecificDefaults();
    }

    /// <summary>
    /// Parses type-specific fields after the base ObjectCatalogResource fields.
    /// Override this in derived classes to parse additional fields.
    /// </summary>
    /// <param name="data">The full data span.</param>
    /// <param name="offset">The current offset. Update this as you read.</param>
    protected virtual void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Serializes type-specific fields after the base ObjectCatalogResource fields.
    /// Override this in derived classes to serialize additional fields.
    /// </summary>
    /// <param name="buffer">The full buffer span.</param>
    /// <param name="offset">The current offset. Update this as you write.</param>
    protected virtual void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Gets the size in bytes of the type-specific fields when serialized.
    /// Override this in derived classes.
    /// </summary>
    protected virtual int GetTypeSpecificSerializedSize() => 0;

    /// <summary>
    /// Initializes type-specific defaults for a new empty resource.
    /// Override this in derived classes.
    /// </summary>
    protected virtual void InitializeTypeSpecificDefaults()
    {
        // Default implementation does nothing
    }

    private int CalculateSerializedSize()
    {
        int size = 0;

        // Fixed header: version + catalogVersion + nameHash + descHash + price + unknown1-3 = 8 * 4 = 32 bytes
        size += 32;

        // TGI list (byte count prefix + 16 bytes per TGI)
        size += CatalogStyleTgiList.GetSerializedSize();

        // unknown4 (ushort)
        size += 2;

        // Tag list (int32 count + ushort per tag)
        size += 4 + (CatalogTagList.Count * 2);

        // Selling point list
        size += CatalogSellingPointList.GetSerializedSize();

        // unknown5 (ulong) + unknown6 (ushort) + unknown7 (ulong) = 8 + 2 + 8 = 18
        size += 18;

        // Type-specific fields
        size += GetTypeSpecificSerializedSize();

        return size;
    }
}

/// <summary>
/// A selling point entry for ObjectCatalogResource.
/// Uses ushort commodity + uint amount format.
/// Source: ObjectCatalogResource.cs lines 113-159
/// </summary>
public readonly record struct ObjectCatalogSellingPoint(ushort Commodity, uint Amount)
{
    /// <summary>
    /// The size of a serialized selling point in bytes.
    /// </summary>
    public const int SerializedSize = 6; // ushort + uint

    /// <summary>
    /// Parses a selling point from binary data.
    /// </summary>
    public static ObjectCatalogSellingPoint Parse(ReadOnlySpan<byte> data)
    {
        ushort commodity = BinaryPrimitives.ReadUInt16LittleEndian(data);
        uint amount = BinaryPrimitives.ReadUInt32LittleEndian(data[2..]);
        return new ObjectCatalogSellingPoint(commodity, amount);
    }

    /// <summary>
    /// Writes the selling point to a buffer.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Commodity);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[2..], Amount);
    }
}

/// <summary>
/// A list of selling points for ObjectCatalogResource.
/// Source: ObjectCatalogResource.cs lines 161-194
/// </summary>
public sealed class ObjectCatalogSellingPointList
{
    private readonly List<ObjectCatalogSellingPoint> _points = [];

    /// <summary>
    /// The selling points.
    /// </summary>
    public IReadOnlyList<ObjectCatalogSellingPoint> Points => _points;

    /// <summary>
    /// Number of selling points.
    /// </summary>
    public int Count => _points.Count;

    /// <summary>
    /// Creates an empty selling point list.
    /// </summary>
    public ObjectCatalogSellingPointList()
    {
    }

    /// <summary>
    /// Parses a selling point list from binary data.
    /// Uses int32 count prefix.
    /// </summary>
    public static ObjectCatalogSellingPointList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        int count = BinaryPrimitives.ReadInt32LittleEndian(data);
        offset += 4;

        if (count < 0 || count > 1000)
            throw new InvalidOperationException($"Unreasonable selling point count: {count}");

        var list = new ObjectCatalogSellingPointList();
        list._points.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            var point = ObjectCatalogSellingPoint.Parse(data[offset..]);
            offset += ObjectCatalogSellingPoint.SerializedSize;
            list._points.Add(point);
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 4 + (_points.Count * ObjectCatalogSellingPoint.SerializedSize);

    /// <summary>
    /// Writes the selling point list to a buffer.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        BinaryPrimitives.WriteInt32LittleEndian(buffer, _points.Count);
        offset += 4;

        foreach (var point in _points)
        {
            point.WriteTo(buffer[offset..]);
            offset += ObjectCatalogSellingPoint.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Adds a selling point.
    /// </summary>
    public void Add(ObjectCatalogSellingPoint point) => _points.Add(point);

    /// <summary>
    /// Clears all selling points.
    /// </summary>
    public void Clear() => _points.Clear();
}
