// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/GEOMListResource.cs lines 246-251

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating GeomListResource instances.
/// </summary>
[ResourceHandler(GeomListResource.TypeId)]
public sealed class GeomListResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new GeomListResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new GeomListResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
