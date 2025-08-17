using System.ComponentModel.DataAnnotations;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Interface for facial animation resources in The Sims 4.
/// Represents facial expressions, emotion mappings, and face animation systems.
/// </summary>
public interface IFacialAnimationResource : IResource
{
    /// <summary>
    /// Gets the facial animation type.
    /// </summary>
    FacialAnimationType AnimationType { get; }

    /// <summary>
    /// Gets the facial animation name or identifier.
    /// </summary>
    string AnimationName { get; }

    /// <summary>
    /// Gets the duration of the facial animation in seconds.
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Gets the frame rate of the facial animation.
    /// </summary>
    float FrameRate { get; }

    /// <summary>
    /// Gets the emotion type associated with this facial animation.
    /// </summary>
    EmotionType EmotionType { get; }

    /// <summary>
    /// Gets the intensity level of the facial expression (0.0 to 1.0).
    /// </summary>
    [Range(0.0, 1.0)]
    float Intensity { get; }

    /// <summary>
    /// Gets the facial blend shapes and their weights.
    /// </summary>
    IReadOnlyDictionary<string, float> BlendShapes { get; }

    /// <summary>
    /// Gets the bone transforms for facial rigging.
    /// </summary>
    IReadOnlyList<FacialBoneTransform> BoneTransforms { get; }

    /// <summary>
    /// Gets the eye control data for gaze and blinking.
    /// </summary>
    FacialEyeControl? EyeControl { get; }

    /// <summary>
    /// Gets the mouth shape data for speech and expressions.
    /// </summary>
    FacialMouthShape? MouthShape { get; }

    /// <summary>
    /// Gets a value indicating whether the animation is looped.
    /// </summary>
    bool IsLooped { get; }

    /// <summary>
    /// Gets the priority for animation blending with other facial animations.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the morph targets for advanced facial deformation.
    /// </summary>
    IReadOnlyList<FacialMorphTarget> MorphTargets { get; }

    /// <summary>
    /// Gets the age group compatibility flags.
    /// </summary>
    AgeGroupFlags CompatibleAges { get; }

    /// <summary>
    /// Gets the gender compatibility flags.
    /// </summary>
    GenderFlags CompatibleGenders { get; }
}
