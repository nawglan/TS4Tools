namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating SimDataResource instances.
/// </summary>
public sealed class SimDataResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new SimDataResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new SimDataResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
