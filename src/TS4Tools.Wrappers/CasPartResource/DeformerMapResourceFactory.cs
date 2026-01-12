using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Factory for creating Deformer Map resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs lines 518-525
/// </summary>
[ResourceHandler(DeformerMapResource.TypeId)]
public sealed class DeformerMapResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new DeformerMapResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new DeformerMapResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
