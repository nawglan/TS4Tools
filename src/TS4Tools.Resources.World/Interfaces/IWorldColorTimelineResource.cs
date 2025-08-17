using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.World.Models;

namespace TS4Tools.Resources.World.Interfaces;

/// <summary>
/// Interface for World Color Timeline resources that define day/night lighting cycles and color transitions
/// </summary>
public interface IWorldColorTimelineResource : IResource, IContentFields
{
    /// <summary>
    /// Gets or sets the version of the color timeline format
    /// </summary>
    uint Version { get; set; }

    /// <summary>
    /// Gets the collection of color timelines
    /// </summary>
    IList<ColorTimeline> ColorTimelines { get; }
}
