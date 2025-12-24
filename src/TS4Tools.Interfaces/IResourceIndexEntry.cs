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
    /// Gets or sets the resource key.
    /// </summary>
    new ResourceKey Key { get; set; }

    /// <summary>
    /// Gets or sets whether the entry is marked as deleted.
    /// </summary>
    new bool IsDeleted { get; set; }
}
