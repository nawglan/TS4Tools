using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating COBJ (Catalog Object) resources.
/// Source: COBJResource.cs lines 38-44
/// </summary>
[ResourceHandler(0x319E4F1D)]
public sealed class CobjResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CobjResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CobjResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
