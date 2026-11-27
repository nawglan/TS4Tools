namespace TS4Tools;

/// <summary>
/// An entry in the package index, describing a resource's location and metadata.
/// </summary>
public interface IResourceIndexEntry
{
    /// <summary>
    /// Gets the resource key (Type/Group/Instance).
    /// </summary>
    ResourceKey Key { get; }

    /// <summary>
    /// Gets the offset of the resource data within the package file.
    /// </summary>
    uint ChunkOffset { get; }

    /// <summary>
    /// Gets the compressed size of the resource in the package file.
    /// </summary>
    uint FileSize { get; }

    /// <summary>
    /// Gets the uncompressed size of the resource in memory.
    /// </summary>
    uint MemorySize { get; }

    /// <summary>
    /// Gets whether the resource is compressed.
    /// </summary>
    bool IsCompressed { get; }

    /// <summary>
    /// Gets the compression type flag.
    /// </summary>
    ushort CompressionType { get; }

    /// <summary>
    /// Gets whether the entry has been marked as deleted.
    /// </summary>
    bool IsDeleted { get; }
}

/// <summary>
/// A mutable resource index entry.
/// </summary>
public interface IMutableResourceIndexEntry : IResourceIndexEntry
{
    /// <summary>
    /// Gets or sets the resource key. Setting it marks the owning package dirty.
    /// </summary>
    new ResourceKey Key { get; set; }

    /// <summary>
    /// Gets or sets whether the entry is marked as deleted.
    /// Setting it marks the owning package dirty.
    /// </summary>
    new bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the compression type flag: 0x0000 stores the resource uncompressed; any non-zero
    /// value (e.g. 0x5A42 ZLIB, 0xFFFF) asks the package to compress it on save, keeping the result
    /// only if smaller (it is then labelled 0x5A42). Changing it marks the entry and package dirty so
    /// the resource is re-encoded on the next save.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi/Package/ResourceIndexEntry.cs lines 116-120;
    /// legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 127-138, 570-572
    /// </remarks>
    new ushort CompressionType { get; set; }
}
