// Source: legacy_references/Sims4Tools/s4pi Wrappers/ComplateResource/ComplateResource.cs lines 114-123

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ComplateResource instances.
/// </summary>
[ResourceHandler(0x044AE110)]
public sealed class ComplateResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ComplateResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ComplateResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
