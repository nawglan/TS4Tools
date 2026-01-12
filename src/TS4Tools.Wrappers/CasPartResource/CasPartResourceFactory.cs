// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResourceTS4.cs lines 765-779

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Factory for creating CasPartResource instances.
/// </summary>
[ResourceHandler(CasPartResource.TypeId)]
public sealed class CasPartResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CasPartResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CasPartResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
