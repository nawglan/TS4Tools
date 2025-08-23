namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Represents a Clip Header resource containing complete animation clip data for The Sims 4.
/// These resources contain structured animation data including spatial transforms, events, IK assignments, and timeline data.
/// Resource Type: 0xBC4A5044 (CLHD)
/// </summary>
public interface IClipHeaderResource : IResource
{
    /// <summary>
    /// Gets the version of the clip format.
    /// </summary>
    uint Version { get; }

    /// <summary>
    /// Gets the name of the animation clip.
    /// </summary>
    string? ClipName { get; }

    /// <summary>
    /// Gets the actor name for this clip.
    /// </summary>
    string? ActorName { get; }

    /// <summary>
    /// Gets the duration of the animation clip in seconds.
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Gets the flags associated with this clip.
    /// </summary>
    uint Flags { get; }

    /// <summary>
    /// Gets the initial offset quaternion (rotation).
    /// </summary>
    string? InitialOffsetQ { get; }

    /// <summary>
    /// Gets the initial offset translation (position).
    /// </summary>
    string? InitialOffsetT { get; }

    /// <summary>
    /// Gets the reference namespace hash.
    /// </summary>
    uint ReferenceNamespaceHash { get; }

    /// <summary>
    /// Gets the surface namespace hash.
    /// </summary>
    uint SurfaceNamespaceHash { get; }

    /// <summary>
    /// Gets the surface joint name hash.
    /// </summary>
    uint SurfaceJointNameHash { get; }

    /// <summary>
    /// Gets the surface child namespace hash.
    /// </summary>
    uint SurfaceChildNamespaceHash { get; }

    /// <summary>
    /// Gets the rig hash reference.
    /// </summary>
    ulong Rig { get; }

    /// <summary>
    /// Gets the rig name if available.
    /// </summary>
    string? RigName { get; }

    /// <summary>
    /// Gets the explicit namespaces used in this clip.
    /// </summary>
    IReadOnlyList<string> ExplicitNamespaces { get; }

    /// <summary>
    /// Gets the complete JSON representation of this clip.
    /// This contains all the parsed clip data in JSON format.
    /// </summary>
    string? JsonData { get; }

    /// <summary>
    /// Gets whether this clip has valid data.
    /// </summary>
    bool HasValidData { get; }

    /// <summary>
    /// Gets a specific property from the clip data.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve</param>
    /// <returns>The property value as a string, or null if not found</returns>
    string? GetProperty(string propertyName);

    /// <summary>
    /// Sets a property in the clip data.
    /// </summary>
    /// <param name="propertyName">The name of the property to set</param>
    /// <param name="value">The value to set</param>
    void SetProperty(string propertyName, object? value);
}
