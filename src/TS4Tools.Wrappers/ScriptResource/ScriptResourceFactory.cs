// Source: legacy_references/Sims4Tools/s4pi Wrappers/ScriptResource/ScriptResource.cs lines 276-285

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating ScriptResource instances.
/// </summary>
[ResourceHandler(0x073FAA07)]
public sealed class ScriptResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new ScriptResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new ScriptResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
