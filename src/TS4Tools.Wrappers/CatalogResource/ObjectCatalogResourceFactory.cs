using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for ObjectCatalogResource.
/// This is an abstract base - concrete derived types have their own type IDs.
/// No TypeId registered since this is a base class.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/ObjectCatalogResource.cs lines 30-342
/// </summary>
public sealed class ObjectCatalogResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ConcreteObjectCatalogResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ConcreteObjectCatalogResource(key, ReadOnlyMemory<byte>.Empty);
    }

    /// <summary>
    /// Concrete implementation for resources that are ObjectCatalogResource without extension.
    /// </summary>
    private sealed class ConcreteObjectCatalogResource : ObjectCatalogResource
    {
        public ConcreteObjectCatalogResource(ResourceKey key, ReadOnlyMemory<byte> data)
            : base(key, data)
        {
        }
    }
}
