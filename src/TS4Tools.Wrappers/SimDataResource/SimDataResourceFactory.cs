// Source: legacy_references/Sims4Tools/s4pi Wrappers/DataResource/DataResource.cs lines 806-812

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating SimDataResource instances.
/// </summary>
/// <remarks>Legacy DataResourceHandler registers 0x545AC67A (DataResource.cs line 810).</remarks>
[ResourceHandler(0x545AC67A)]
public sealed class SimDataResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new SimDataResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new SimDataResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
