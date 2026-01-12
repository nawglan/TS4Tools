using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CFTR (Catalog Fountain Trim) resources.
/// Source: CFTRResource.cs lines 164-170
/// </summary>
[ResourceHandler(0xE7ADA79D)]
public sealed class CftrResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CftrResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CftrResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
