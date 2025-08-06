namespace TS4Tools.Resources.Animation;

/// <summary>
/// Types of animation resources supported in The Sims 4.
/// </summary>
public enum AnimationType
{
    /// <summary>
    /// No animation specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Character animation clip.
    /// </summary>
    Clip = 0x00000001,

    /// <summary>
    /// Character pose data.
    /// </summary>
    Pose = 0x00000002,

    /// <summary>
    /// Inverse kinematics configuration.
    /// </summary>
    IkConfiguration = 0x00000003,

    /// <summary>
    /// Animation track mask.
    /// </summary>
    TrackMask = 0x00000004,

    /// <summary>
    /// Bone rig definition.
    /// </summary>
    Rig = 0x00000005,

    /// <summary>
    /// Bone deformation data.
    /// </summary>
    Bone = 0x00000006,

    /// <summary>
    /// Character outfit animation.
    /// </summary>
    Outfit = 0x00000007,

    /// <summary>
    /// Character Asset System (CAS) part animation.
    /// </summary>
    CasPart = 0x00000008
}

/// <summary>
/// Animation playback modes.
/// </summary>
public enum AnimationPlayMode
{
    /// <summary>
    /// Play once and stop.
    /// </summary>
    Once = 0,

    /// <summary>
    /// Loop continuously.
    /// </summary>
    Loop = 1,

    /// <summary>
    /// Play once and hold final frame.
    /// </summary>
    Hold = 2,

    /// <summary>
    /// Ping-pong between start and end.
    /// </summary>
    PingPong = 3
}

/// <summary>
/// Animation blending modes for combining animations.
/// </summary>
public enum AnimationBlendMode
{
    /// <summary>
    /// Replace existing animation.
    /// </summary>
    Replace = 0,

    /// <summary>
    /// Additive blending.
    /// </summary>
    Add = 1,

    /// <summary>
    /// Multiply existing animation.
    /// </summary>
    Multiply = 2,

    /// <summary>
    /// Blend with existing animation.
    /// </summary>
    Blend = 3
}

/// <summary>
/// Represents an animation keyframe.
/// </summary>
/// <param name="Time">Time position in seconds.</param>
/// <param name="Value">Keyframe value.</param>
/// <param name="InterpolationType">Type of interpolation to next keyframe.</param>
public readonly record struct AnimationKeyframe(float Time, object Value, InterpolationType InterpolationType);

/// <summary>
/// Animation interpolation types.
/// </summary>
public enum InterpolationType
{
    /// <summary>
    /// No interpolation (step).
    /// </summary>
    Step = 0,

    /// <summary>
    /// Linear interpolation.
    /// </summary>
    Linear = 1,

    /// <summary>
    /// Cubic spline interpolation.
    /// </summary>
    Cubic = 2,

    /// <summary>
    /// Bezier curve interpolation.
    /// </summary>
    Bezier = 3
}

/// <summary>
/// Represents an animation track for a specific bone or property.
/// </summary>
/// <param name="BoneName">Name of the bone this track affects.</param>
/// <param name="PropertyName">Name of the property being animated.</param>
/// <param name="Keyframes">Collection of keyframes for this track.</param>
public readonly record struct AnimationTrack(string BoneName, string PropertyName, IReadOnlyList<AnimationKeyframe> Keyframes);

/// <summary>
/// Represents a bone in a character rig.
/// </summary>
/// <param name="Name">Bone name.</param>
/// <param name="ParentName">Parent bone name (null for root bones).</param>
/// <param name="Position">Local position relative to parent.</param>
/// <param name="Rotation">Local rotation as quaternion (x, y, z, w).</param>
/// <param name="Scale">Local scale.</param>
public readonly record struct Bone(string Name, string? ParentName, Vector3 Position, Vector4 Rotation, Vector3 Scale);

/// <summary>
/// Represents a 3D vector.
/// </summary>
/// <param name="X">X component.</param>
/// <param name="Y">Y component.</param>
/// <param name="Z">Z component.</param>
public readonly record struct Vector3(float X, float Y, float Z);

/// <summary>
/// Represents a 4D vector (typically used for quaternions).
/// </summary>
/// <param name="X">X component.</param>
/// <param name="Y">Y component.</param>
/// <param name="Z">Z component.</param>
/// <param name="W">W component.</param>
public readonly record struct Vector4(float X, float Y, float Z, float W);
