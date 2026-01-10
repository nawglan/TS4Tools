// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/BoneResource.cs lines 327-332

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating BoneResource instances.
/// </summary>
[ResourceHandler(BoneResource.TypeId)]
public sealed class BoneResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new BoneResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new BoneResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
