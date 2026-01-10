using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating A8F7B517 catalog resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/A8F7B517CatalogResource.cs lines 29-35
/// </summary>
[ResourceHandler(A8f7b517CatalogResource.TypeId)]
public sealed class A8f7b517CatalogResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new A8f7b517CatalogResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new A8f7b517CatalogResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
