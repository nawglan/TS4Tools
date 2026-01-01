// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/THMResource.cs lines 189-198

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ThumWorldResource instances.
/// </summary>
[ResourceHandler(0x16CA6BC4)]
public sealed class ThumWorldResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ThumWorldResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ThumWorldResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
