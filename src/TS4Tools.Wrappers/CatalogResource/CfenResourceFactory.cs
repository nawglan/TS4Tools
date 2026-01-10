using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CFEN (Catalog Fence) resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFENResource.cs lines 284-290
/// </summary>
[ResourceHandler(CfenResource.TypeId)]
public sealed class CfenResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CfenResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CfenResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
