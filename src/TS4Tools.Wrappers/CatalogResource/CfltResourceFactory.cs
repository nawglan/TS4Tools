using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CFLT (Catalog Floor Trim) resources.
/// Source: CFLTResource.cs lines 167-173
/// </summary>
[ResourceHandler(0x84C23219)]
public sealed class CfltResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CfltResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CfltResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
