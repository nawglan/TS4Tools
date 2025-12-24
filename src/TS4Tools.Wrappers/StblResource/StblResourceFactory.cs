namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating StblResource instances.
/// </summary>
[ResourceHandler(0x220557DA)]
public sealed class StblResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new StblResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new StblResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
