using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CSTR (Catalog Stair) resources.
/// Source: CSTRResource.cs lines 358-364
/// </summary>
[ResourceHandler(0x9A20CD1C)]
public sealed class CstrResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CstrResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CstrResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
