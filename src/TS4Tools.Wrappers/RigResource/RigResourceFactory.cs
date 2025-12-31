// Source: legacy_references/Sims4Tools/s4pi Wrappers/RigResource/RigResource.cs lines 609-615

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating RigResource instances.
/// </summary>
[ResourceHandler(RigResource.TypeId)]
public sealed class RigResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new RigResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new RigResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
