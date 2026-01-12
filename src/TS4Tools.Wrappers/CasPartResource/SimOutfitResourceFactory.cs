using TS4Tools.Resources;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Factory for creating Sim Outfit resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SimOutfitResource.cs lines 515-522
/// </summary>
[ResourceHandler(SimOutfitResource.TypeId)]
public sealed class SimOutfitResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new SimOutfitResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new SimOutfitResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
