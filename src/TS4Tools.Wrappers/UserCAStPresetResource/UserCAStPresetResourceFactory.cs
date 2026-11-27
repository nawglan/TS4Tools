// Source: legacy_references/Sims4Tools/s4pi Wrappers/UserCAStPresetResource/UserCAStPresetResource.cs lines 239-246

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating UserCAStPresetResource instances.
/// </summary>
/// <remarks>Legacy UserCAStPresetResourceHandler registers 0x0591B1AF (UserCAStPresetResource.cs line 244).</remarks>
[ResourceHandler(0x0591B1AF)]
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
