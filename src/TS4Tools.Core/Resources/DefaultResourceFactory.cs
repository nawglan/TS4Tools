namespace TS4Tools.Resources;

/// <summary>
/// Default factory that creates resources holding raw bytes without parsing.
/// </summary>
[ResourceHandler(0)] // Register as default handler
public sealed class DefaultResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new DefaultResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new DefaultResource(key, ReadOnlyMemory<byte>.Empty);
    }
}

/// <summary>
/// Default resource implementation that holds raw bytes without parsing.
/// </summary>
public sealed class DefaultResource : IResource
{
    private byte[] _data;
    private bool _isDirty;

    /// <summary>
    /// The resource key.
    /// </summary>
    public ResourceKey Key { get; }

    /// <inheritdoc/>
    public ReadOnlyMemory<byte> Data => _data;

    /// <inheritdoc/>
    public bool IsDirty => _isDirty;

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <summary>
    /// Creates a new default resource.
    /// </summary>
    public DefaultResource(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        Key = key;
        _data = data.ToArray();
    }

    /// <summary>
    /// Replaces the raw data.
    /// </summary>
    public void SetData(ReadOnlyMemory<byte> data)
    {
        _data = data.ToArray();
        _isDirty = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose
        GC.SuppressFinalize(this);
    }
}
