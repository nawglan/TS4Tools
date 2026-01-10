using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Factory for creating Skin Tone resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SkinToneResource.cs lines 426-432
/// </summary>
[ResourceHandler(SkinToneResource.TypeId)]
public sealed class SkinToneResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new SkinToneResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new SkinToneResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
