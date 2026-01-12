using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CPLT (Catalog Pool Trim) resources.
/// Source: CPLTResource.cs lines 167-173
/// </summary>
[ResourceHandler(0xA5DFFCF3)]
public sealed class CpltResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CpltResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CpltResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
