using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CBLK (Catalog Block) resources.
/// Source: CBLKResource.cs lines 287-295
/// </summary>
[ResourceHandler(0x07936CE0)]
public sealed class CblkResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CblkResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CblkResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
