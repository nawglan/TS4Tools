
namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// A list of catalog category tags.
/// Handles version-aware parsing (v11+ uses uint32 tags, earlier uses uint16).
/// Source: CatalogCommon.cs lines 369-382
/// </summary>
public sealed class CatalogTagList
{
    private readonly List<uint> _tags = [];

    /// <summary>
    /// The tag values.
    /// </summary>
    public IReadOnlyList<uint> Tags => _tags;

    /// <summary>
    /// Number of tags.
    /// </summary>
    public int Count => _tags.Count;

    /// <summary>
    /// Creates an empty tag list.
    /// </summary>
    public CatalogTagList()
    {
    }

    /// <summary>
    /// Creates a tag list from existing tags.
    /// </summary>
    public CatalogTagList(IEnumerable<uint> tags)
    {
        _tags.AddRange(tags);
    }

    /// <summary>
    /// Parses a tag list for version 11+ (uint32 tags with uint32 count).
    /// </summary>
    public static CatalogTagList ParseV11(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data);
        offset += 4;

        if (count > 1000)
            throw new InvalidOperationException($"Unreasonable tag count: {count}");

        var list = new CatalogTagList();
        list._tags.Capacity = (int)count;

        for (uint i = 0; i < count; i++)
        {
            uint tag = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            offset += 4;
            list._tags.Add(tag);
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Parses a tag list for versions before 11 (uint16 tags with uint32 count).
    /// </summary>
    public static CatalogTagList ParseLegacy(ReadOnlySpan<byte> data, out int bytesRead)
    {
        int offset = 0;
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data);
        offset += 4;

        if (count > 1000)
            throw new InvalidOperationException($"Unreasonable tag count: {count}");

        var list = new CatalogTagList();
        list._tags.Capacity = (int)count;

        for (uint i = 0; i < count; i++)
        {
            ushort tag = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;
            list._tags.Add(tag);
        }

        bytesRead = offset;
        return list;
    }

    /// <summary>
    /// Gets the size in bytes when serialized for version 11+.
    /// </summary>
    public int GetSerializedSizeV11() => 4 + (_tags.Count * 4);

    /// <summary>
    /// Gets the size in bytes when serialized for legacy versions.
    /// </summary>
    public int GetSerializedSizeLegacy() => 4 + (_tags.Count * 2);

    /// <summary>
    /// Writes the tag list for version 11+ (uint32 tags).
    /// </summary>
    public int WriteToV11(Span<byte> buffer)
    {
        int offset = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)_tags.Count);
        offset += 4;

        foreach (uint tag in _tags)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], tag);
            offset += 4;
        }

        return offset;
    }

    /// <summary>
    /// Writes the tag list for legacy versions (uint16 tags).
    /// </summary>
    public int WriteToLegacy(Span<byte> buffer)
    {
        int offset = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)_tags.Count);
        offset += 4;

        foreach (uint tag in _tags)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], (ushort)tag);
            offset += 2;
        }

        return offset;
    }

    /// <summary>
    /// Adds a tag.
    /// </summary>
    public void Add(uint tag) => _tags.Add(tag);

    /// <summary>
    /// Clears all tags.
    /// </summary>
    public void Clear() => _tags.Clear();

    /// <summary>
    /// Gets or sets a tag by index.
    /// </summary>
    public uint this[int index]
    {
        get => _tags[index];
        set => _tags[index] = value;
    }
}
