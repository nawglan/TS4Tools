using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CFRZ (Catalog Frieze) resources.
/// Source: CFRZResource.cs lines 186-193
/// </summary>
[ResourceHandler(0xA057811C)]
public sealed class CfrzResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CfrzResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CfrzResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
