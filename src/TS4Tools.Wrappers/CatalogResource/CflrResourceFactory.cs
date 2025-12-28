using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CFLR (Catalog Floor Pattern) resources.
/// Source: CFLRResource.cs lines 176-185
/// </summary>
[ResourceHandler(0xB4F762C9)]
public sealed class CflrResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CflrResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CflrResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
