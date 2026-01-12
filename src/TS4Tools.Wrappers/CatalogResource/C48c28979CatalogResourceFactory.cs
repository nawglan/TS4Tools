using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating 48C28979 catalog resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/48C28979CatalogResource.cs lines 29-35
/// </summary>
[ResourceHandler(C48c28979CatalogResource.TypeId)]
public sealed class C48c28979CatalogResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new C48c28979CatalogResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new C48c28979CatalogResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
