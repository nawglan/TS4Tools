using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CRAL (Catalog Railing) resources.
/// Source: CRALResource.cs lines 151-158
/// </summary>
[ResourceHandler(0x1C1CF1F7)]
public sealed class CralResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CralResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CralResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
