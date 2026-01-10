using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating roof style resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/RoofStyleResource.cs lines 29-35
/// </summary>
[ResourceHandler(RoofStyleResource.TypeId)]
public sealed class RoofStyleResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new RoofStyleResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new RoofStyleResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
