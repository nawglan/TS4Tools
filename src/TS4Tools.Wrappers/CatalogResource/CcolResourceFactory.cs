using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CCOL (Catalog Color) resources.
/// Source: CCOLResource.cs lines 215-224
/// </summary>
[ResourceHandler(0x1D6DF1CF)]
public sealed class CcolResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CcolResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CcolResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
