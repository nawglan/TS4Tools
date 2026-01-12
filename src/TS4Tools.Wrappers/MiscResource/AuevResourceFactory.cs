// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/AUEVResource.cs lines 125-131

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating AuevResource instances.
/// </summary>
[ResourceHandler(0xBDD82221)]
public sealed class AuevResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new AuevResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new AuevResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
