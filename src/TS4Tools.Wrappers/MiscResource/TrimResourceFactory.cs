// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/TRIMResource.cs lines 447-453

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating TrimResource instances.
/// </summary>
[ResourceHandler(0x76BCF80C)]
public sealed class TrimResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new TrimResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new TrimResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
