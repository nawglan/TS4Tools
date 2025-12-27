using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CRPT (Catalog Roof Pattern) resources.
/// Source: CRPTResource.cs lines 158-164
/// </summary>
[ResourceHandler(0xF1EDBD86)]
public sealed class CrptResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CrptResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CrptResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
