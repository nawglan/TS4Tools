using TS4Tools.Resources;

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating RLE texture resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/RLEResource.cs lines 940-946
/// </summary>
[ResourceHandler(RleResource.TypeId)]
[ResourceHandler(RleResource.TypeIdAlternate)]
public sealed class RleResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new RleResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new RleResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
