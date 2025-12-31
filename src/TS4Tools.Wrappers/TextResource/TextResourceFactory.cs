// Source: legacy_references/Sims4Tools/s4pi Wrappers/TextResource/TextResource.cs lines 129-144

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating TextResource instances.
/// </summary>
public sealed class TextResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new TextResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new TextResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
