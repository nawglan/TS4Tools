// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/StyleLookResource.cs lines 155-160

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating StyleLookResource instances.
/// </summary>
[ResourceHandler(StyleLookResource.TypeId)]
public sealed class StyleLookResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new StyleLookResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new StyleLookResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
