// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SkyBoxTextureResource.cs lines 199-205

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for creating SkyBoxTextureResource instances.
/// </summary>
[ResourceHandler(0x71A449C9)]
public sealed class SkyBoxTextureResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new SkyBoxTextureResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new SkyBoxTextureResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
