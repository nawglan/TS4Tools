// Source: legacy_references/Sims4Tools/s4pi Wrappers/UserCAStPresetResource/UserCAStPresetResource.cs lines 239-251

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating UserCAStPresetResource instances.
/// </summary>
public sealed class UserCAStPresetResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new UserCAStPresetResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new UserCAStPresetResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
