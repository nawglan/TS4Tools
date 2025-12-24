namespace TS4Tools;

/// <summary>
/// Represents a resource within a package.
/// </summary>
public interface IResource : IDisposable
{
    /// <summary>
    /// Gets the raw resource data.
    /// </summary>
    ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Gets whether the resource has been modified since loading.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Raised when the resource data changes.
    /// </summary>
    event EventHandler? Changed;
}

/// <summary>
/// A resource that can be modified.
/// </summary>
public interface IMutableResource : IResource
{
    /// <summary>
    /// Gets or sets the resource data.
    /// </summary>
    new Memory<byte> Data { get; set; }

    /// <summary>
    /// Marks the resource as clean (not modified).
    /// </summary>
    void MarkClean();
}
