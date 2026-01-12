using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CTPT (Catalog Terrain Paint) resources.
/// Source: CTPTResource.cs lines 159-165
/// </summary>
[ResourceHandler(0xEBCBB16C)]
public sealed class CtptResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CtptResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CtptResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
