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
public readonly record struct Vector3(float X, float Y, float Z)
{
    /// <summary>
    /// Zero vector (0, 0, 0).
    /// </summary>
    public static Vector3 Zero => new(0, 0, 0);
}

/// <summary>
/// Represents a 4D vector (typically used for quaternions).
/// </summary>
/// <param name="X">X component.</param>
/// <param name="Y">Y component.</param>
/// <param name="Z">Z component.</param>
/// <param name="W">W component.</param>
public readonly record struct Vector4(float X, float Y, float Z, float W)
{
    /// <summary>
    /// Zero vector (0, 0, 0, 0).
    /// </summary>
    public static Vector4 Zero => new(0, 0, 0, 0);
}

/// <summary>
/// Represents a quaternion for rotations (alias for Vector4).
/// </summary>
/// <param name="X">X component.</param>
/// <param name="Y">Y component.</param>
/// <param name="Z">Z component.</param>
/// <param name="W">W component.</param>
public readonly record struct Quaternion(float X, float Y, float Z, float W)
{
    /// <summary>
    /// Identity quaternion (0, 0, 0, 1).
    /// </summary>
    public static Quaternion Identity => new(0, 0, 0, 1);

    /// <summary>
    /// Implicit conversion from Quaternion to Vector4.
    /// </summary>
    /// <param name="quaternion">Quaternion to convert.</param>
    public static implicit operator Vector4(Quaternion quaternion) => new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

    /// <summary>
    /// Implicit conversion from Vector4 to Quaternion.
    /// </summary>
    /// <param name="vector">Vector4 to convert.</param>
    public static implicit operator Quaternion(Vector4 vector) => new(vector.X, vector.Y, vector.Z, vector.W);

    /// <summary>
    /// Converts this quaternion to a Vector4.
    /// </summary>
    /// <returns>Vector4 representation of this quaternion.</returns>
    public Vector4 ToVector4() => new(X, Y, Z, W);

    /// <summary>
    /// Creates a Quaternion from a Vector4.
    /// </summary>
    /// <param name="vector">Vector4 to convert.</param>
    /// <returns>Quaternion created from the vector.</returns>
    public static Quaternion FromVector4(Vector4 vector) => new(vector.X, vector.Y, vector.Z, vector.W);
}

/// <summary>
/// Types of animation tracks.
/// </summary>
public enum TrackType
{
    /// <summary>
    /// Position track.
    /// </summary>
    Position = 0,

    /// <summary>
    /// Rotation track.
    /// </summary>
    Rotation = 1,

    /// <summary>
    /// Scale track.
    /// </summary>
    Scale = 2,

    /// <summary>
    /// Blend weight track.
    /// </summary>
    BlendWeight = 3
}

/// <summary>
/// Represents a generic animation keyframe.
/// </summary>
/// <typeparam name="T">The value type of the keyframe.</typeparam>
/// <param name="Time">Time position in seconds.</param>
/// <param name="Value">Keyframe value.</param>
/// <param name="Interpolation">Type of interpolation to next keyframe.</param>
public readonly record struct Keyframe<T>(float Time, T Value, InterpolationType Interpolation);

/// <summary>
/// Represents a generic animation track for a specific bone and property.
/// </summary>
/// <typeparam name="T">The value type of keyframes in this track.</typeparam>
public class AnimationTrack<T>
{
    private readonly List<Keyframe<T>> _keyframes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationTrack{T}"/> class.
    /// </summary>
    /// <param name="boneName">Name of the bone this track affects.</param>
    /// <param name="trackType">Type of track (position, rotation, etc.).</param>
    public AnimationTrack(string boneName, TrackType trackType)
    {
        BoneName = boneName ?? throw new ArgumentNullException(nameof(boneName));
        TrackType = trackType;
    }

    /// <summary>
    /// Gets the name of the bone this track affects.
    /// </summary>
    public string BoneName { get; }

    /// <summary>
    /// Gets the type of track.
    /// </summary>
    public TrackType TrackType { get; }

    /// <summary>
    /// Gets the collection of keyframes for this track.
    /// </summary>
    public IReadOnlyList<Keyframe<T>> Keyframes => _keyframes.AsReadOnly();

    /// <summary>
    /// Adds a keyframe to this track.
    /// </summary>
    /// <param name="keyframe">The keyframe to add.</param>
    public void AddKeyframe(Keyframe<T> keyframe)
    {
        _keyframes.Add(keyframe);
        _keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    /// <summary>
    /// Removes a keyframe from this track.
    /// </summary>
    /// <param name="keyframe">The keyframe to remove.</param>
    /// <returns>True if the keyframe was removed; otherwise, false.</returns>
    public bool RemoveKeyframe(Keyframe<T> keyframe)
    {
        return _keyframes.Remove(keyframe);
    }

    /// <summary>
    /// Clears all keyframes from this track.
    /// </summary>
    public void ClearKeyframes()
    {
        _keyframes.Clear();
    }
}

/// <summary>
/// Represents a bone in a character rig (class version for hierarchical operations).
/// This is distinct from the Bone record struct used for data transfer.
/// </summary>
public class BoneNode
{
    private readonly List<BoneNode> _children = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BoneNode"/> class.
    /// </summary>
    /// <param name="name">The bone name.</param>
    /// <param name="position">Local position relative to parent.</param>
    /// <param name="rotation">Local rotation as quaternion.</param>
    /// <param name="parent">Parent bone (null for root bones).</param>
    /// <param name="scale">Local scale (defaults to 1,1,1).</param>
    public BoneNode(string name, Vector3 position, Quaternion rotation, BoneNode? parent = null, Vector3? scale = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Position = position;
        Rotation = rotation;
        Scale = scale ?? new Vector3(1, 1, 1);
        Parent = parent;

        if (parent != null)
        {
            parent.AddChild(this);
        }
    }

    /// <summary>
    /// Gets the bone name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the local position relative to parent.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Gets the local rotation as quaternion.
    /// </summary>
    public Quaternion Rotation { get; }

    /// <summary>
    /// Gets the local scale.
    /// </summary>
    public Vector3 Scale { get; }

    /// <summary>
    /// Gets or sets the parent bone (null for root bones).
    /// </summary>
    public BoneNode? Parent { get; private set; }

    /// <summary>
    /// Gets the collection of child bones.
    /// </summary>
    public IReadOnlyList<BoneNode> Children => _children.AsReadOnly();

    /// <summary>
    /// Adds a child bone to this bone.
    /// </summary>
    /// <param name="child">The child bone to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when child is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when child already has a parent.</exception>
    public void AddChild(BoneNode child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (child.Parent != null && child.Parent != this)
            throw new InvalidOperationException("Child bone already has a parent");

        if (!_children.Contains(child))
        {
            _children.Add(child);
            child.Parent = this;
        }
    }

    /// <summary>
    /// Removes a child bone from this bone.
    /// </summary>
    /// <param name="child">The child bone to remove.</param>
    /// <returns>True if the child was removed; otherwise, false.</returns>
    public bool RemoveChild(BoneNode child)
    {
        if (child == null)
            return false;

        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }

        return false;
    }
}

/// <summary>
/// Types of facial animation resources supported in The Sims 4.
/// </summary>
public enum FacialAnimationType
{
    /// <summary>
    /// No facial animation specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic facial expression.
    /// </summary>
    Expression = 0x00000001,

    /// <summary>
    /// Emotional state animation.
    /// </summary>
    Emotion = 0x00000002,

    /// <summary>
    /// Speech lip sync animation.
    /// </summary>
    LipSync = 0x00000003,

    /// <summary>
    /// Eye movement and blinking.
    /// </summary>
    EyeMovement = 0x00000004,

    /// <summary>
    /// Complex morph-based expression.
    /// </summary>
    MorphExpression = 0x00000005,

    /// <summary>
    /// Age transition facial changes.
    /// </summary>
    AgeTransition = 0x00000006,

    /// <summary>
    /// Reaction-based facial animation.
    /// </summary>
    Reaction = 0x00000007
}

/// <summary>
/// Emotion types for facial animations.
/// </summary>
public enum EmotionType
{
    /// <summary>
    /// Neutral expression.
    /// </summary>
    Neutral = 0,

    /// <summary>
    /// Happy expression.
    /// </summary>
    Happy = 1,

    /// <summary>
    /// Sad expression.
    /// </summary>
    Sad = 2,

    /// <summary>
    /// Angry expression.
    /// </summary>
    Angry = 3,

    /// <summary>
    /// Surprised expression.
    /// </summary>
    Surprised = 4,

    /// <summary>
    /// Disgusted expression.
    /// </summary>
    Disgusted = 5,

    /// <summary>
    /// Fearful expression.
    /// </summary>
    Fearful = 6,

    /// <summary>
    /// Contemptuous expression.
    /// </summary>
    Contemptuous = 7,

    /// <summary>
    /// Excited expression.
    /// </summary>
    Excited = 8,

    /// <summary>
    /// Confused expression.
    /// </summary>
    Confused = 9,

    /// <summary>
    /// Embarrassed expression.
    /// </summary>
    Embarrassed = 10,

    /// <summary>
    /// Flirty expression.
    /// </summary>
    Flirty = 11,

    /// <summary>
    /// Focused expression.
    /// </summary>
    Focused = 12,

    /// <summary>
    /// Playful expression.
    /// </summary>
    Playful = 13
}

/// <summary>
/// Age group compatibility flags.
/// </summary>
[Flags]
public enum AgeGroupFlags
{
    /// <summary>
    /// No age group specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Baby age group.
    /// </summary>
    Baby = 1 << 0,

    /// <summary>
    /// Toddler age group.
    /// </summary>
    Toddler = 1 << 1,

    /// <summary>
    /// Child age group.
    /// </summary>
    Child = 1 << 2,

    /// <summary>
    /// Teen age group.
    /// </summary>
    Teen = 1 << 3,

    /// <summary>
    /// Young adult age group.
    /// </summary>
    YoungAdult = 1 << 4,

    /// <summary>
    /// Adult age group.
    /// </summary>
    Adult = 1 << 5,

    /// <summary>
    /// Elder age group.
    /// </summary>
    Elder = 1 << 6,

    /// <summary>
    /// All age groups.
    /// </summary>
    All = Baby | Toddler | Child | Teen | YoungAdult | Adult | Elder
}

/// <summary>
/// Gender compatibility flags.
/// </summary>
[Flags]
public enum GenderFlags
{
    /// <summary>
    /// No gender specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Male gender.
    /// </summary>
    Male = 1 << 0,

    /// <summary>
    /// Female gender.
    /// </summary>
    Female = 1 << 1,

    /// <summary>
    /// All genders.
    /// </summary>
    All = Male | Female
}

/// <summary>
/// Facial bone transform data for rigging.
/// </summary>
public struct FacialBoneTransform
{
    /// <summary>
    /// Gets or sets the bone name.
    /// </summary>
    public string BoneName { get; set; }

    /// <summary>
    /// Gets or sets the translation vector.
    /// </summary>
    public Vector3 Translation { get; set; }

    /// <summary>
    /// Gets or sets the rotation quaternion.
    /// </summary>
    public Quaternion Rotation { get; set; }

    /// <summary>
    /// Gets or sets the scale vector.
    /// </summary>
    public Vector3 Scale { get; set; }

    /// <summary>
    /// Gets or sets the blend weight (0.0 to 1.0).
    /// </summary>
    public float Weight { get; set; }
}

/// <summary>
/// Eye control data for facial animations.
/// </summary>
public class FacialEyeControl
{
    /// <summary>
    /// Gets or sets the gaze direction.
    /// </summary>
    public Vector3 GazeDirection { get; set; }

    /// <summary>
    /// Gets or sets the blink state (0.0 = open, 1.0 = closed).
    /// </summary>
    public float BlinkState { get; set; }

    /// <summary>
    /// Gets or sets the left eye rotation.
    /// </summary>
    public Quaternion LeftEyeRotation { get; set; }

    /// <summary>
    /// Gets or sets the right eye rotation.
    /// </summary>
    public Quaternion RightEyeRotation { get; set; }

    /// <summary>
    /// Gets or sets the pupil dilation (0.0 to 1.0).
    /// </summary>
    public float PupilDilation { get; set; }
}

/// <summary>
/// Mouth shape data for facial animations.
/// </summary>
public class FacialMouthShape
{
    /// <summary>
    /// Gets or sets the mouth openness (0.0 = closed, 1.0 = fully open).
    /// </summary>
    public float Openness { get; set; }

    /// <summary>
    /// Gets or sets the lip stretch (-1.0 = frown, 1.0 = smile).
    /// </summary>
    public float LipStretch { get; set; }

    /// <summary>
    /// Gets or sets the jaw rotation.
    /// </summary>
    public float JawRotation { get; set; }

    /// <summary>
    /// Gets or sets the tongue position.
    /// </summary>
    public Vector3 TonguePosition { get; set; }

    /// <summary>
    /// Gets or sets the lip sync phoneme.
    /// </summary>
    public string? Phoneme { get; set; }
}

/// <summary>
/// Facial morph target for advanced deformation.
/// </summary>
public class FacialMorphTarget
{
    /// <summary>
    /// Gets or sets the morph target name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the morph weight (0.0 to 1.0).
    /// </summary>
    public float Weight { get; set; }

    /// <summary>
    /// Gets or sets the affected vertices.
    /// </summary>
    public IReadOnlyList<int> AffectedVertices { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the vertex deltas.
    /// </summary>
    public IReadOnlyList<Vector3> VertexDeltas { get; set; } = Array.Empty<Vector3>();
}
