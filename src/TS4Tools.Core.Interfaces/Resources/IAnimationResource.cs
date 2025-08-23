namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Represents an animation resource containing animation data for The Sims 4.
/// These resources contain the actual animation data and are referenced by Animation State Machine (ASM) resources.
/// Resource Type: 0xBC4A5044
/// </summary>
public interface IAnimationResource : IResource
{
    /// <summary>
    /// Gets the animation name or identifier.
    /// </summary>
    string? AnimationName { get; }

    /// <summary>
    /// Gets the animation version.
    /// </summary>
    uint Version { get; }

    /// <summary>
    /// Gets the animation duration in frames or time units.
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Gets the frame rate of the animation.
    /// </summary>
    float FrameRate { get; }

    /// <summary>
    /// Gets the raw animation data.
    /// This contains the binary animation data that defines bone transformations, curves, etc.
    /// </summary>
    ReadOnlyMemory<byte> AnimationData { get; }

    /// <summary>
    /// Gets whether this animation has valid data.
    /// </summary>
    bool HasValidData { get; }
}
