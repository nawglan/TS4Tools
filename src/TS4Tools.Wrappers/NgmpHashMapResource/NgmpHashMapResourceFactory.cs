// Source: legacy_references/Sims4Tools/s4pi Wrappers/NGMPHashMapResource/NGMPHashMapResource.cs lines 154-162

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating NgmpHashMapResource instances.
/// </summary>
[ResourceHandler(0xF3A38370)]
public sealed class NgmpHashMapResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new NgmpHashMapResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new NgmpHashMapResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
