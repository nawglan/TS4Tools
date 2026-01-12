using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CWAL (Catalog Wall Pattern) resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CWALResource.cs lines 439-445
/// </summary>
[ResourceHandler(CwalResource.TypeId)]
public sealed class CwalResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CwalResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CwalResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
