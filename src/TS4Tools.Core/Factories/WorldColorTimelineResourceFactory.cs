using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Factories;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Factory for creating WorldColorTimelineResource instances.
/// </summary>
/// <remarks>
/// Handles creation of world color timeline resources that manage environmental lighting data
/// including day/night cycles, weather effects, and point-of-interest specific lighting.
/// Supports legacy format versions 13 and 14 with byte-perfect compatibility.
/// </remarks>
[ResourceFactory(0x19301120)] // WorldColorTimelineResource type ID
public class WorldColorTimelineResourceFactory : IResourceFactory
{
    public Type ResourceType => typeof(IWorldColorTimelineResource);

    public uint ResourceTypeId => 0x19301120;

    public IResource CreateResource()
    {
        return new Resources.WorldColorTimelineResource();
    }

    public IResource CreateResource(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new Resources.WorldColorTimelineResource(stream);
    }

    public bool CanHandle(uint resourceTypeId)
    {
        return resourceTypeId == ResourceTypeId;
    }

    public string GetResourceTypeName()
    {
        return "WorldColorTimelineResource";
    }

    public string GetResourceDescription()
    {
        return "World Color Timeline resource containing environmental lighting data " +
               "for day/night cycles, weather effects, and time-based color transitions.";
    }
}
