using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating Object Definition resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/ObjectDefinitionResource.cs lines 665-671
/// </summary>
[ResourceHandler(ObjectDefinitionResource.TypeId)]
public sealed class ObjectDefinitionResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ObjectDefinitionResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ObjectDefinitionResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
