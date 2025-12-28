using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CSTL (Catalog Stair Landing) resources.
/// Source: CSTLResource.cs lines 251-257
/// </summary>
[ResourceHandler(0x9F5CFF10)]
public sealed class CstlResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CstlResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CstlResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
