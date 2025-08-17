using System.ComponentModel.DataAnnotations;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Interface for animation resources in The Sims 4.
/// </summary>
public interface IAnimationResource : IResource
{
    /// <summary>
    /// Gets the animation type.
    /// </summary>
    AnimationType AnimationType { get; }

    /// <summary>
    /// Gets the animation name or identifier.
    /// </summary>
    string AnimationName { get; }

    /// <summary>
    /// Gets the duration of the animation in seconds.
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Gets the frame rate of the animation.
    /// </summary>
    float FrameRate { get; }

    /// <summary>
    /// Gets the playback mode for the animation.
    /// </summary>
    AnimationPlayMode PlayMode { get; }

    /// <summary>
    /// Gets the blend mode for combining with other animations.
    /// </summary>
    AnimationBlendMode BlendMode { get; }

    /// <summary>
    /// Gets the animation tracks.
    /// </summary>
    IReadOnlyList<AnimationTrack> Tracks { get; }

    /// <summary>
    /// Gets a value indicating whether the animation is looped.
    /// </summary>
    bool IsLooped { get; }

    /// <summary>
    /// Gets the priority for animation blending.
    /// </summary>
    int Priority { get; }
}
