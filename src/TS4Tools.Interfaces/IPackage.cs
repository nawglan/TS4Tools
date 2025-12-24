namespace TS4Tools;

/// <summary>
/// Represents a DBPF package file containing resources.
/// </summary>
public interface IPackage : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the file path of the package, if opened from a file.
    /// </summary>
    string? FilePath { get; }

    /// <summary>
    /// Gets whether the package has been modified since loading.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Gets whether the package is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets the package major version (typically 2 for Sims 4).
    /// </summary>
    int MajorVersion { get; }

    /// <summary>
    /// Gets the package minor version (typically 1 for Sims 4).
    /// </summary>
    int MinorVersion { get; }

    /// <summary>
    /// Gets the number of resources in the package.
    /// </summary>
    int ResourceCount { get; }

    /// <summary>
    /// Gets the resource index entries.
    /// </summary>
    IReadOnlyList<IResourceIndexEntry> Resources { get; }

    /// <summary>
    /// Raised when the resource index is invalidated.
    /// </summary>
    event EventHandler? ResourceIndexInvalidated;

    /// <summary>
    /// Finds the first resource matching the specified key.
    /// </summary>
    IResourceIndexEntry? Find(ResourceKey key);

    /// <summary>
    /// Finds the first resource matching the specified predicate.
    /// </summary>
    IResourceIndexEntry? Find(Func<IResourceIndexEntry, bool> predicate);

    /// <summary>
    /// Finds all resources matching the specified predicate.
    /// </summary>
    IEnumerable<IResourceIndexEntry> FindAll(Func<IResourceIndexEntry, bool> predicate);

    /// <summary>
    /// Gets the resource data for the specified index entry.
    /// </summary>
    ValueTask<ReadOnlyMemory<byte>> GetResourceDataAsync(
        IResourceIndexEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resource wrapper for the specified index entry.
    /// </summary>
    ValueTask<IResource> GetResourceAsync(
        IResourceIndexEntry entry,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A package that can be modified.
/// </summary>
public interface IMutablePackage : IPackage
{
    /// <summary>
    /// Adds a resource to the package.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The resource data.</param>
    /// <param name="rejectDuplicates">If true, fails if the key already exists.</param>
    /// <returns>The new index entry, or null if rejected as duplicate.</returns>
    IResourceIndexEntry? AddResource(ResourceKey key, ReadOnlyMemory<byte> data, bool rejectDuplicates = true);

    /// <summary>
    /// Replaces the data for an existing resource.
    /// </summary>
    void ReplaceResource(IResourceIndexEntry entry, ReadOnlyMemory<byte> data);

    /// <summary>
    /// Marks a resource as deleted.
    /// </summary>
    void DeleteResource(IResourceIndexEntry entry);

    /// <summary>
    /// Saves the package to its original location.
    /// </summary>
    ValueTask SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the package to a new location.
    /// </summary>
    ValueTask SaveAsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the package to a stream.
    /// </summary>
    ValueTask SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}
