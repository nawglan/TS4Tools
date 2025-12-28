namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ThumResource instances.
/// Handles THUM metadata resources (not image data).
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/THMResource.cs line 196
/// </remarks>
[ResourceHandler(0x16CA6BC4)]
public sealed class ThumResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ThumResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ThumResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
