namespace TS4Tools.Resources;

/// <summary>
/// Base class for resources with structured data that needs parsing.
/// Implements the Parse/UnParse pattern from legacy s4pi.
/// </summary>
public abstract class TypedResource : IResource
{
    private bool _isDirty;

    /// <summary>
    /// The resource key.
    /// </summary>
    public ResourceKey Key { get; }

    /// <inheritdoc/>
    public bool IsDirty => _isDirty;

    /// <inheritdoc/>
    public ReadOnlyMemory<byte> Data => Serialize();

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <summary>
    /// Creates a new typed resource by parsing data.
    /// </summary>
    protected TypedResource(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        Key = key;
        if (!data.IsEmpty)
        {
            Parse(data.Span);
        }
        else
        {
            InitializeDefaults();
        }
    }

    /// <summary>
    /// Parses the resource data.
    /// Override this to implement format-specific parsing.
    /// </summary>
    /// <param name="data">The raw data to parse.</param>
    protected abstract void Parse(ReadOnlySpan<byte> data);

    /// <summary>
    /// Serializes the resource back to bytes.
    /// Override this to implement format-specific serialization.
    /// </summary>
    /// <returns>The serialized data.</returns>
    protected abstract ReadOnlyMemory<byte> Serialize();

    /// <summary>
    /// Initializes the resource with default values.
    /// Override this to set up a new empty resource.
    /// </summary>
    protected virtual void InitializeDefaults()
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Marks the resource as changed and raises the Changed event.
    /// Call this whenever resource data is modified.
    /// </summary>
    protected void OnChanged()
    {
        _isDirty = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        // Override to release resources
        GC.SuppressFinalize(this);
    }
}
