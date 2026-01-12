namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ThumbnailResource instances.
/// Handles JFIF+ALFA format thumbnails used for visual previews.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ThumbnailResource.cs lines 329-335
/// </remarks>
[ResourceHandler(0x0D338A3A)]
[ResourceHandler(0x16CCF748)]
[ResourceHandler(0x3BD45407)]
[ResourceHandler(0x3C1AF1F2)]
[ResourceHandler(0x3C2A8647)]
[ResourceHandler(0x5B282D45)]
[ResourceHandler(0xCD9DE247)]
[ResourceHandler(0xE18CAEE2)]
[ResourceHandler(0xE254AE6E)]
public sealed class ThumbnailResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ThumbnailResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ThumbnailResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
