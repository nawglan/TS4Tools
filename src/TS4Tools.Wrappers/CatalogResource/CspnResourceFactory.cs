using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CatalogResource;

/// <summary>
/// Factory for creating CSPN (Catalog Spindle) resources.
/// Source: CSPNResource.cs lines 180-186
/// </summary>
[ResourceHandler(0x3F0C529A)]
public sealed class CspnResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new CspnResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new CspnResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
