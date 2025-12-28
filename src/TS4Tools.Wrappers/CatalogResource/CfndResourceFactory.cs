using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CFND (Catalog Foundation) resources.
/// Source: CFNDResource.cs lines 202-209
/// </summary>
[ResourceHandler(0x2FAE983E)]
public sealed class CfndResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CfndResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CfndResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
