// Source: legacy_references/Sims4Tools/s4pi Wrappers/ObjKeyResource/ObjKeyResource.cs lines 508-514

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ObjKeyResource instances.
/// </summary>
[ResourceHandler(0x02DC343F)]
public sealed class ObjKeyResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ObjKeyResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ObjKeyResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
