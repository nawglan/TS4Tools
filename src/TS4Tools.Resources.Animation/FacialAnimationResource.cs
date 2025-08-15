using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Implementation of IFacialAnimationResource for handling facial animation resources.
/// Supports facial expressions, emotions, lip sync, and advanced morph-based animations.
/// </summary>
public class FacialAnimationResource : IFacialAnimationResource
{
    private static readonly byte[] MagicBytes = [0x46, 0x41, 0x43, 0x45]; // "FACE"

    private readonly Dictionary<string, float> _blendShapes = [];
    private readonly List<FacialBoneTransform> _boneTransforms = [];
    private readonly List<FacialMorphTarget> _morphTargets = [];
    private readonly List<string> _contentFields =
    [
        "AnimationType",
        "AnimationName",
        "Duration",
        "FrameRate",
        "EmotionType",
        "Intensity",
        "BlendShapes",
        "BoneTransforms",
        "EyeControl",
        "MouthShape",
        "IsLooped",
        "Priority",
        "MorphTargets",
        "CompatibleAges",
        "CompatibleGenders"
    ];

    private MemoryStream? _stream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FacialAnimationResource"/> class.
    /// </summary>
    public FacialAnimationResource()
    {
        AnimationType = FacialAnimationType.None;
        AnimationName = string.Empty;
        Duration = 0.0f;
        FrameRate = 30.0f;
        EmotionType = EmotionType.Neutral;
        Intensity = 0.0f;
        IsLooped = false;
        Priority = 0;
        CompatibleAges = AgeGroupFlags.All;
        CompatibleGenders = GenderFlags.All;
        RequestedApiVersion = 1;
        RecommendedApiVersion = 1;
    }

    /// <inheritdoc />
    public FacialAnimationType AnimationType { get; set; }

    /// <inheritdoc />
    public string AnimationName { get; set; }

    /// <inheritdoc />
    public float Duration { get; set; }

    /// <inheritdoc />
    public float FrameRate { get; set; }

    /// <inheritdoc />
    public EmotionType EmotionType { get; set; }

    /// <inheritdoc />
    [Range(0.0, 1.0)]
    public float Intensity { get; set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, float> BlendShapes => _blendShapes.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<FacialBoneTransform> BoneTransforms => _boneTransforms.AsReadOnly();

    /// <inheritdoc />
    public FacialEyeControl? EyeControl { get; set; }

    /// <inheritdoc />
    public FacialMouthShape? MouthShape { get; set; }

    /// <inheritdoc />
    public bool IsLooped { get; set; }

    /// <inheritdoc />
    public int Priority { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<FacialMorphTarget> MorphTargets => _morphTargets.AsReadOnly();

    /// <inheritdoc />
    public AgeGroupFlags CompatibleAges { get; set; }

    /// <inheritdoc />
    public GenderFlags CompatibleGenders { get; set; }

    #region IResource Implementation

    /// <inheritdoc />
    public uint ResourceType => 0x0C772E27;

    /// <inheritdoc />
    public uint ResourceGroup { get; set; } = 0x00000000;

    /// <inheritdoc />
    public ulong ResourceInstance { get; set; } = 0x0000000000000000;

    /// <inheritdoc />
    public string ResourceName { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool IsCompressed { get; set; } = false;

    /// <inheritdoc />
    public int UncompressedSize { get; set; } = 0;

    /// <inheritdoc />
    public int CompressedSize => _stream?.Length > 0 ? (int)_stream.Length : UncompressedSize;

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFields.AsReadOnly();

    /// <inheritdoc />
    public bool IsDirty { get; private set; } = false;

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FacialAnimationResource));

            _stream ??= new MemoryStream();
            return _stream;
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FacialAnimationResource));

            return SerializeAsync().GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public int RequestedApiVersion { get; }

    /// <inheritdoc />
    public int RecommendedApiVersion { get; }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => GetFieldValue(index);
        set => SetFieldValue(index, value);
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get => GetFieldValue(name);
        set => SetFieldValue(name, value);
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    protected virtual void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets a field value by index.
    /// </summary>
    /// <param name="index">The field index</param>
    /// <returns>The field value</returns>
    private TypedValue GetFieldValue(int index)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return GetFieldValue(_contentFields[index]);
    }

    /// <summary>
    /// Gets a field value by name.
    /// </summary>
    /// <param name="name">The field name</param>
    /// <returns>The field value</returns>
    private TypedValue GetFieldValue(string name)
    {
        return name switch
        {
            "AnimationType" => TypedValue.Create(AnimationType),
            "AnimationName" => TypedValue.Create(AnimationName),
            "Duration" => TypedValue.Create(Duration),
            "FrameRate" => TypedValue.Create(FrameRate),
            "EmotionType" => TypedValue.Create(EmotionType),
            "Intensity" => TypedValue.Create(Intensity),
            "BlendShapes" => TypedValue.Create(BlendShapes.Count),
            "BoneTransforms" => TypedValue.Create(BoneTransforms.Count),
            "EyeControl" => TypedValue.Create(EyeControl != null),
            "MouthShape" => TypedValue.Create(MouthShape != null),
            "IsLooped" => TypedValue.Create(IsLooped),
            "Priority" => TypedValue.Create(Priority),
            "MorphTargets" => TypedValue.Create(MorphTargets.Count),
            "CompatibleAges" => TypedValue.Create(CompatibleAges),
            "CompatibleGenders" => TypedValue.Create(CompatibleGenders),
            _ => throw new ArgumentException($"Unknown field name: {name}", nameof(name))
        };
    }

    /// <summary>
    /// Sets a field value by index.
    /// </summary>
    /// <param name="index">The field index</param>
    /// <param name="value">The field value</param>
    private void SetFieldValue(int index, TypedValue value)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        SetFieldValue(_contentFields[index], value);
    }

    /// <summary>
    /// Sets a field value by name.
    /// </summary>
    /// <param name="name">The field name</param>
    /// <param name="value">The field value</param>
    private void SetFieldValue(string name, TypedValue value)
    {
        switch (name)
        {
            case "AnimationType":
                if (value.Value is FacialAnimationType animationType)
                    AnimationType = animationType;
                else if (value.Value is int animationIntValue)
                    AnimationType = (FacialAnimationType)animationIntValue;
                break;
            case "AnimationName":
                AnimationName = value.Value?.ToString() ?? string.Empty;
                break;
            case "Duration":
                if (value.Value is float durationFloat)
                    Duration = durationFloat;
                else if (value.Value is double durationDouble)
                    Duration = (float)durationDouble;
                break;
            case "FrameRate":
                if (value.Value is float frameRateFloat)
                    FrameRate = frameRateFloat;
                else if (value.Value is double frameRateDouble)
                    FrameRate = (float)frameRateDouble;
                break;
            case "EmotionType":
                if (value.Value is EmotionType emotionType)
                    EmotionType = emotionType;
                else if (value.Value is int emotionIntValue)
                    EmotionType = (EmotionType)emotionIntValue;
                break;
            case "Intensity":
                if (value.Value is float intensityFloat)
                    Intensity = intensityFloat;
                else if (value.Value is double intensityDouble)
                    Intensity = (float)intensityDouble;
                break;
            case "IsLooped":
                if (value.Value is bool isLooped)
                    IsLooped = isLooped;
                break;
            case "Priority":
                if (value.Value is int priority)
                    Priority = priority;
                break;
            case "CompatibleAges":
                if (value.Value is AgeGroupFlags compatibleAges)
                    CompatibleAges = compatibleAges;
                else if (value.Value is int agesIntValue)
                    CompatibleAges = (AgeGroupFlags)agesIntValue;
                break;
            case "CompatibleGenders":
                if (value.Value is GenderFlags compatibleGenders)
                    CompatibleGenders = compatibleGenders;
                else if (value.Value is int gendersIntValue)
                    CompatibleGenders = (GenderFlags)gendersIntValue;
                break;
            default:
                throw new ArgumentException($"Field '{name}' is read-only or unknown", nameof(name));
        }

        SetDirty();
        OnResourceChanged();
    }

    /// <inheritdoc />
    public void SetDirty()
    {
        IsDirty = true;
    }

    /// <inheritdoc />
    public void ClearDirty()
    {
        IsDirty = false;
    }

    #endregion

    #region Blend Shape Management

    /// <summary>
    /// Adds or updates a blend shape value.
    /// </summary>
    /// <param name="name">The blend shape name</param>
    /// <param name="value">The blend shape weight (0.0 to 1.0)</param>
    public void SetBlendShape(string name, float value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (value is < 0.0f or > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(value), "Blend shape value must be between 0.0 and 1.0");

        _blendShapes[name] = value;
        SetDirty();
    }

    /// <summary>
    /// Removes a blend shape.
    /// </summary>
    /// <param name="name">The blend shape name</param>
    /// <returns>True if the blend shape was removed, false if it didn't exist</returns>
    public bool RemoveBlendShape(string name)
    {
        var removed = _blendShapes.Remove(name);
        if (removed)
            SetDirty();
        return removed;
    }

    /// <summary>
    /// Clears all blend shapes.
    /// </summary>
    public void ClearBlendShapes()
    {
        if (_blendShapes.Count > 0)
        {
            _blendShapes.Clear();
            SetDirty();
        }
    }

    #endregion

    #region Bone Transform Management

    /// <summary>
    /// Adds a facial bone transform.
    /// </summary>
    /// <param name="transform">The bone transform to add</param>
    public void AddBoneTransform(FacialBoneTransform transform)
    {
        if (string.IsNullOrEmpty(transform.BoneName))
            throw new ArgumentException("Bone name cannot be null or empty", nameof(transform));

        _boneTransforms.Add(transform);
        SetDirty();
    }

    /// <summary>
    /// Removes a bone transform by name.
    /// </summary>
    /// <param name="boneName">The bone name</param>
    /// <returns>True if the bone transform was removed, false if not found</returns>
    public bool RemoveBoneTransform(string boneName)
    {
        var index = _boneTransforms.FindIndex(bt => bt.BoneName == boneName);
        if (index >= 0)
        {
            _boneTransforms.RemoveAt(index);
            SetDirty();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all bone transforms.
    /// </summary>
    public void ClearBoneTransforms()
    {
        if (_boneTransforms.Count > 0)
        {
            _boneTransforms.Clear();
            SetDirty();
        }
    }

    #endregion

    #region Morph Target Management

    /// <summary>
    /// Adds a facial morph target.
    /// </summary>
    /// <param name="morphTarget">The morph target to add</param>
    public void AddMorphTarget(FacialMorphTarget morphTarget)
    {
        ArgumentNullException.ThrowIfNull(morphTarget);
        if (string.IsNullOrEmpty(morphTarget.Name))
            throw new ArgumentException("Morph target name cannot be null or empty", nameof(morphTarget));

        _morphTargets.Add(morphTarget);
        SetDirty();
    }

    /// <summary>
    /// Removes a morph target by name.
    /// </summary>
    /// <param name="name">The morph target name</param>
    /// <returns>True if the morph target was removed, false if not found</returns>
    public bool RemoveMorphTarget(string name)
    {
        var index = _morphTargets.FindIndex(mt => mt.Name == name);
        if (index >= 0)
        {
            _morphTargets.RemoveAt(index);
            SetDirty();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all morph targets.
    /// </summary>
    public void ClearMorphTargets()
    {
        if (_morphTargets.Count > 0)
        {
            _morphTargets.Clear();
            SetDirty();
        }
    }

    #endregion

    #region Serialization

    /// <inheritdoc />
    public Task<byte[]> SerializeAsync(CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write magic bytes
        writer.Write(MagicBytes);

        // Write version
        writer.Write((uint)1);

        // Write basic properties
        writer.Write((uint)AnimationType);
        writer.Write(AnimationName);
        writer.Write(Duration);
        writer.Write(FrameRate);
        writer.Write((uint)EmotionType);
        writer.Write(Intensity);
        writer.Write(IsLooped);
        writer.Write(Priority);
        writer.Write((uint)CompatibleAges);
        writer.Write((uint)CompatibleGenders);

        // Write blend shapes
        writer.Write(_blendShapes.Count);
        foreach (var kvp in _blendShapes)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        // Write bone transforms
        writer.Write(_boneTransforms.Count);
        foreach (var transform in _boneTransforms)
        {
            writer.Write(transform.BoneName);
            writer.Write(transform.Translation.X);
            writer.Write(transform.Translation.Y);
            writer.Write(transform.Translation.Z);
            writer.Write(transform.Rotation.X);
            writer.Write(transform.Rotation.Y);
            writer.Write(transform.Rotation.Z);
            writer.Write(transform.Rotation.W);
            writer.Write(transform.Scale.X);
            writer.Write(transform.Scale.Y);
            writer.Write(transform.Scale.Z);
            writer.Write(transform.Weight);
        }

        // Write eye control
        if (EyeControl != null)
        {
            writer.Write(true);
            writer.Write(EyeControl.GazeDirection.X);
            writer.Write(EyeControl.GazeDirection.Y);
            writer.Write(EyeControl.GazeDirection.Z);
            writer.Write(EyeControl.BlinkState);
            writer.Write(EyeControl.LeftEyeRotation.X);
            writer.Write(EyeControl.LeftEyeRotation.Y);
            writer.Write(EyeControl.LeftEyeRotation.Z);
            writer.Write(EyeControl.LeftEyeRotation.W);
            writer.Write(EyeControl.RightEyeRotation.X);
            writer.Write(EyeControl.RightEyeRotation.Y);
            writer.Write(EyeControl.RightEyeRotation.Z);
            writer.Write(EyeControl.RightEyeRotation.W);
            writer.Write(EyeControl.PupilDilation);
        }
        else
        {
            writer.Write(false);
        }

        // Write mouth shape
        if (MouthShape != null)
        {
            writer.Write(true);
            writer.Write(MouthShape.Openness);
            writer.Write(MouthShape.LipStretch);
            writer.Write(MouthShape.JawRotation);
            writer.Write(MouthShape.TonguePosition.X);
            writer.Write(MouthShape.TonguePosition.Y);
            writer.Write(MouthShape.TonguePosition.Z);
            writer.Write(MouthShape.Phoneme ?? string.Empty);
        }
        else
        {
            writer.Write(false);
        }

        // Write morph targets
        writer.Write(_morphTargets.Count);
        foreach (var morphTarget in _morphTargets)
        {
            writer.Write(morphTarget.Name);
            writer.Write(morphTarget.Weight);
            writer.Write(morphTarget.AffectedVertices.Count);
            foreach (var vertex in morphTarget.AffectedVertices)
            {
                writer.Write(vertex);
            }
            writer.Write(morphTarget.VertexDeltas.Count);
            foreach (var delta in morphTarget.VertexDeltas)
            {
                writer.Write(delta.X);
                writer.Write(delta.Y);
                writer.Write(delta.Z);
            }
        }

        var data = stream.ToArray();
        UncompressedSize = data.Length;
        ClearDirty();

        return Task.FromResult(data);
    }

    /// <inheritdoc />
    public Task DeserializeAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        // Read magic bytes
        var magic = reader.ReadBytes(4);
        if (!magic.SequenceEqual(MagicBytes))
            throw new InvalidDataException("Invalid facial animation resource magic bytes");

        // Read version
        var version = reader.ReadUInt32();
        if (version != 1)
            throw new NotSupportedException($"Facial animation resource version {version} is not supported");

        // Read basic properties
        AnimationType = (FacialAnimationType)reader.ReadUInt32();
        AnimationName = reader.ReadString();
        Duration = reader.ReadSingle();
        FrameRate = reader.ReadSingle();
        EmotionType = (EmotionType)reader.ReadUInt32();
        Intensity = reader.ReadSingle();
        IsLooped = reader.ReadBoolean();
        Priority = reader.ReadInt32();
        CompatibleAges = (AgeGroupFlags)reader.ReadUInt32();
        CompatibleGenders = (GenderFlags)reader.ReadUInt32();

        // Read blend shapes
        var blendShapeCount = reader.ReadInt32();
        _blendShapes.Clear();
        for (int i = 0; i < blendShapeCount; i++)
        {
            var name = reader.ReadString();
            var value = reader.ReadSingle();
            _blendShapes[name] = value;
        }

        // Read bone transforms
        var boneTransformCount = reader.ReadInt32();
        _boneTransforms.Clear();
        for (int i = 0; i < boneTransformCount; i++)
        {
            var transform = new FacialBoneTransform
            {
                BoneName = reader.ReadString(),
                Translation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Scale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Weight = reader.ReadSingle()
            };
            _boneTransforms.Add(transform);
        }

        // Read eye control
        var hasEyeControl = reader.ReadBoolean();
        if (hasEyeControl)
        {
            EyeControl = new FacialEyeControl
            {
                GazeDirection = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                BlinkState = reader.ReadSingle(),
                LeftEyeRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                RightEyeRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                PupilDilation = reader.ReadSingle()
            };
        }
        else
        {
            EyeControl = null;
        }

        // Read mouth shape
        var hasMouthShape = reader.ReadBoolean();
        if (hasMouthShape)
        {
            MouthShape = new FacialMouthShape
            {
                Openness = reader.ReadSingle(),
                LipStretch = reader.ReadSingle(),
                JawRotation = reader.ReadSingle(),
                TonguePosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Phoneme = reader.ReadString()
            };
            if (string.IsNullOrEmpty(MouthShape.Phoneme))
                MouthShape.Phoneme = null;
        }
        else
        {
            MouthShape = null;
        }

        // Read morph targets
        var morphTargetCount = reader.ReadInt32();
        _morphTargets.Clear();
        for (int i = 0; i < morphTargetCount; i++)
        {
            var morphTarget = new FacialMorphTarget
            {
                Name = reader.ReadString(),
                Weight = reader.ReadSingle()
            };

            var vertexCount = reader.ReadInt32();
            var vertices = new int[vertexCount];
            for (int j = 0; j < vertexCount; j++)
            {
                vertices[j] = reader.ReadInt32();
            }
            morphTarget.AffectedVertices = vertices;

            var deltaCount = reader.ReadInt32();
            var deltas = new Vector3[deltaCount];
            for (int j = 0; j < deltaCount; j++)
            {
                deltas[j] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            morphTarget.VertexDeltas = deltas;

            _morphTargets.Add(morphTarget);
        }

        UncompressedSize = data.Length;
        ClearDirty();

        return Task.CompletedTask;
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of managed resources.
    /// </summary>
    /// <param name="disposing">Whether disposing is in progress</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
