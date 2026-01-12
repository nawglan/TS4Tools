using TS4Tools.Resources;

namespace TS4Tools.Wrappers.MiscResource;

/// <summary>
/// Factory for creating World Color Timeline resources.
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/WorldColorTimelineResource.cs lines 563-569
/// </summary>
[ResourceHandler(WorldColorTimelineResource.TypeId)]
public sealed class WorldColorTimelineResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        return new WorldColorTimelineResource(key, data);
    }

    /// <inheritdoc/>
    public IResource CreateEmpty(ResourceKey key)
    {
        return new WorldColorTimelineResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
