// Source: legacy_references/Sims4Tools/s4pi Wrappers/NameMapResource/NameMapResource.cs lines 204-212

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating NameMapResource instances.
/// </summary>
[ResourceHandler(0x0166038C)]
public sealed class NameMapResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new NameMapResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new NameMapResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
