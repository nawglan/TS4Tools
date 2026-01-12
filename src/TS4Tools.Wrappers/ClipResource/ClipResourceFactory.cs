// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/ClipResourceHandler.cs lines 25-31

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ClipResource instances.
/// </summary>
[ResourceHandler(ClipResource.TypeId)]
public sealed class ClipResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ClipResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ClipResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
