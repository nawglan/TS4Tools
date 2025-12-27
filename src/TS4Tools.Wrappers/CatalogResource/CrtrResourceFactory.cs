using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CRTR (Catalog Roof Trim) resources.
/// Source: CRTRResource.cs lines 176-182
/// </summary>
[ResourceHandler(0xB0311D0F)]
public sealed class CrtrResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CrtrResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CrtrResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
