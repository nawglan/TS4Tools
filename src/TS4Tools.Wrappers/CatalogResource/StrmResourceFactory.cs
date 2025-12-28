using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating STRM (Styled Room) resources.
/// Source: STRMResource.cs lines 202-209
/// </summary>
[ResourceHandler(0x74050B1F)]
public sealed class StrmResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new StrmResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new StrmResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
