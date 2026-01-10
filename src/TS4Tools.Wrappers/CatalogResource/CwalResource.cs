using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Catalog Wall Pattern resource (0xD5F0F921).
/// Contains wall pattern data with material references and texture groups.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CWALResource.cs lines 28-445
/// </summary>
public sealed class CwalResource : SimpleCatalogResource
{
    /// <summary>
    /// Type ID for CWAL resources.
    /// </summary>
    public const uint TypeId = 0xD5F0F921;

    #region Properties

    /// <summary>
    /// Material list (wall height + MATD TGI reference per entry).
    /// Source: CWALResource.cs line 36
    /// </summary>
    public WallMatdEntryList MaterialList { get; set; } = new();

    /// <summary>
    /// Corner texture groups (wall height + 3 TGI references per entry).
    /// Source: CWALResource.cs line 37
    /// </summary>
    public WallImgGroupList CornerTextures { get; set; } = new();

    /// <summary>
    /// Cornering factor.
    /// Source: CWALResource.cs line 38
    /// </summary>
    public uint CorneringFactor { get; set; }

    /// <summary>
    /// Color list.
    /// Source: CWALResource.cs line 39
    /// </summary>
    public ColorList Colors { get; set; } = new();

    /// <summary>
    /// Swatch grouping identifier.
    /// Source: CWALResource.cs line 40
    /// </summary>
    public ulong SwatchGrouping { get; set; }

    #endregion

    /// <summary>
    /// Creates a new CWAL resource.
    /// </summary>
    public CwalResource(ResourceKey key, ReadOnlyMemory<byte> data)
        : base(key, data)
    {
    }

    /// <inheritdoc/>
    protected override void ParseTypeSpecific(ReadOnlySpan<byte> data, ref int offset)
    {
        // Source: CWALResource.cs lines 115-119
        MaterialList = WallMatdEntryList.Parse(data[offset..], out int matdBytes);
        offset += matdBytes;

        CornerTextures = WallImgGroupList.Parse(data[offset..], out int imgBytes);
        offset += imgBytes;

        CorneringFactor = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        offset += 4;

        Colors = ColorList.Parse(data[offset..], out int colorBytes);
        offset += colorBytes;

        SwatchGrouping = BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]);
        offset += 8;
    }

    /// <inheritdoc/>
    protected override void SerializeTypeSpecific(Span<byte> buffer, ref int offset)
    {
        // Source: CWALResource.cs lines 129-136
        offset += MaterialList.WriteTo(buffer[offset..]);
        offset += CornerTextures.WriteTo(buffer[offset..]);

        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], CorneringFactor);
        offset += 4;

        offset += Colors.WriteTo(buffer[offset..]);

        BinaryPrimitives.WriteUInt64LittleEndian(buffer[offset..], SwatchGrouping);
        offset += 8;
    }

    /// <inheritdoc/>
    protected override int GetTypeSpecificSerializedSize()
    {
        int size = 0;

        size += MaterialList.GetSerializedSize();
        size += CornerTextures.GetSerializedSize();
        size += 4; // CorneringFactor
        size += Colors.GetSerializedSize();
        size += 8; // SwatchGrouping

        return size;
    }

    /// <inheritdoc/>
    protected override void InitializeTypeSpecificDefaults()
    {
        MaterialList = new WallMatdEntryList();
        CornerTextures = new WallImgGroupList();
        CorneringFactor = 0;
        Colors = new ColorList();
        SwatchGrouping = 0;
    }
}

/// <summary>
/// Wall height enum for main wall materials.
/// Source: CWALResource.cs lines 270-275
/// </summary>
public enum MainWallHeight : byte
{
    ShortWall = 0x03,
    MediumWall = 0x04,
    TallWall = 0x05,
}

/// <summary>
/// Wall height enum for corner textures.
/// Source: CWALResource.cs lines 424-429
/// </summary>
public enum CornerWallHeight : byte
{
    ShortWall = 0xC3,
    MediumWall = 0xC4,
    TallWall = 0xC5,
}

/// <summary>
/// A wall MATD entry containing height label and TGI reference.
/// Source: CWALResource.cs lines 185-279
/// </summary>
public readonly record struct WallMatdEntry(MainWallHeight Height, TgiReference MatdRef)
{
    /// <summary>
    /// Serialized size: 1 (height) + 16 (TGI) = 17 bytes.
    /// </summary>
    public const int SerializedSize = 1 + TgiReference.SerializedSize;

    /// <summary>
    /// Parses a wall MATD entry from binary data.
    /// </summary>
    public static WallMatdEntry Parse(ReadOnlySpan<byte> data)
    {
        var height = (MainWallHeight)data[0];
        var matdRef = TgiReference.Parse(data[1..]);
        return new WallMatdEntry(height, matdRef);
    }

    /// <summary>
    /// Writes the entry to a buffer.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        buffer[0] = (byte)Height;
        MatdRef.WriteTo(buffer[1..]);
    }
}

/// <summary>
/// A list of wall MATD entries.
/// Source: CWALResource.cs lines 157-183
/// </summary>
public sealed class WallMatdEntryList
{
    private readonly List<WallMatdEntry> _entries = [];

    /// <summary>
    /// The entries.
    /// </summary>
    public IReadOnlyList<WallMatdEntry> Entries => _entries;

    /// <summary>
    /// Number of entries.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Creates an empty entry list.
    /// </summary>
    public WallMatdEntryList()
    {
    }

    /// <summary>
    /// Parses a WallMatdEntryList from binary data.
    /// </summary>
    public static WallMatdEntryList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];

        var list = new WallMatdEntryList();
        list._entries.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            var entry = WallMatdEntry.Parse(data[offset..]);
            offset += WallMatdEntry.SerializedSize;
            list._entries.Add(entry);
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 1 + (_entries.Count * WallMatdEntry.SerializedSize);

    /// <summary>
    /// Writes the entry list to a buffer.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = (byte)_entries.Count;

        foreach (var entry in _entries)
        {
            entry.WriteTo(buffer[offset..]);
            offset += WallMatdEntry.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Adds an entry.
    /// </summary>
    public void Add(WallMatdEntry entry) => _entries.Add(entry);

    /// <summary>
    /// Adds an entry with specified height and reference.
    /// </summary>
    public void Add(MainWallHeight height, TgiReference matdRef) =>
        _entries.Add(new WallMatdEntry(height, matdRef));

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void Clear() => _entries.Clear();

    /// <summary>
    /// Gets or sets an entry by index.
    /// </summary>
    public WallMatdEntry this[int index]
    {
        get => _entries[index];
        set => _entries[index] = value;
    }
}

/// <summary>
/// A wall image group entry containing height and 3 TGI references (diffuse, bump, specular maps).
/// Source: CWALResource.cs lines 309-432
/// </summary>
public readonly record struct WallImgGroupEntry(
    CornerWallHeight Height,
    TgiReference DiffuseMap,
    TgiReference BumpMap,
    TgiReference SpecularMap)
{
    /// <summary>
    /// Serialized size: 1 (height) + 3 * 16 (TGIs) = 49 bytes.
    /// </summary>
    public const int SerializedSize = 1 + (TgiReference.SerializedSize * 3);

    /// <summary>
    /// Parses a wall image group entry from binary data.
    /// </summary>
    public static WallImgGroupEntry Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;
        var height = (CornerWallHeight)data[offset++];
        var diffuse = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;
        var bump = TgiReference.Parse(data[offset..]);
        offset += TgiReference.SerializedSize;
        var specular = TgiReference.Parse(data[offset..]);
        return new WallImgGroupEntry(height, diffuse, bump, specular);
    }

    /// <summary>
    /// Writes the entry to a buffer.
    /// </summary>
    public void WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = (byte)Height;
        DiffuseMap.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;
        BumpMap.WriteTo(buffer[offset..]);
        offset += TgiReference.SerializedSize;
        SpecularMap.WriteTo(buffer[offset..]);
    }
}

/// <summary>
/// A list of wall image group entries.
/// Source: CWALResource.cs lines 281-307
/// </summary>
public sealed class WallImgGroupList
{
    private readonly List<WallImgGroupEntry> _entries = [];

    /// <summary>
    /// The entries.
    /// </summary>
    public IReadOnlyList<WallImgGroupEntry> Entries => _entries;

    /// <summary>
    /// Number of entries.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Creates an empty entry list.
    /// </summary>
    public WallImgGroupList()
    {
    }

    /// <summary>
    /// Parses a WallImgGroupList from binary data.
    /// </summary>
    public static WallImgGroupList Parse(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        byte count = data[offset++];

        var list = new WallImgGroupList();
        list._entries.Capacity = count;

        for (int i = 0; i < count; i++)
        {
            var entry = WallImgGroupEntry.Parse(data[offset..]);
            offset += WallImgGroupEntry.SerializedSize;
            list._entries.Add(entry);
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Gets the size in bytes when serialized.
    /// </summary>
    public int GetSerializedSize() => 1 + (_entries.Count * WallImgGroupEntry.SerializedSize);

    /// <summary>
    /// Writes the entry list to a buffer.
    /// </summary>
    public int WriteTo(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = (byte)_entries.Count;

        foreach (var entry in _entries)
        {
            entry.WriteTo(buffer[offset..]);
            offset += WallImgGroupEntry.SerializedSize;
        }

        return offset;
    }

    /// <summary>
    /// Adds an entry.
    /// </summary>
    public void Add(WallImgGroupEntry entry) => _entries.Add(entry);

    /// <summary>
    /// Adds an entry with specified values.
    /// </summary>
    public void Add(CornerWallHeight height, TgiReference diffuse, TgiReference bump, TgiReference specular) =>
        _entries.Add(new WallImgGroupEntry(height, diffuse, bump, specular));

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void Clear() => _entries.Clear();

    /// <summary>
    /// Gets or sets an entry by index.
    /// </summary>
    public WallImgGroupEntry this[int index]
    {
        get => _entries[index];
        set => _entries[index] = value;
    }
}
