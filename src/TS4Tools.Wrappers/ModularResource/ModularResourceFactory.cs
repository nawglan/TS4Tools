// Source: legacy_references/Sims4Tools/s4pi Wrappers/ModularResource/ModularResource.cs lines 101-107

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ModularResource instances.
/// </summary>
[ResourceHandler(0xCF9A4ACE)]
public sealed class ModularResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ModularResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ModularResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
