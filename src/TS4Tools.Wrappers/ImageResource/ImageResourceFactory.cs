// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ImageResource.cs lines 71-82

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ImageResource instances.
/// </summary>
public sealed class ImageResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ImageResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ImageResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
