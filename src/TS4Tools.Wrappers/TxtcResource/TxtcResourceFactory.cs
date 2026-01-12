// Source: legacy_references/Sims4Tools/s4pi Wrappers/TxtcResource/TxtcResource.cs lines 1154-1160

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating TxtcResource instances.
/// </summary>
[ResourceHandler(0x033A1435)]
[ResourceHandler(0x0341ACC9)]
public sealed class TxtcResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new TxtcResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new TxtcResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
