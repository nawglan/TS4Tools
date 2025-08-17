using System.Collections.ObjectModel;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Implementation of IAnimationResource for handling animation resources.
/// </summary>
public class AnimationResource : IAnimationResource
{
    private static readonly byte[] MagicBytes = [0x41, 0x4E, 0x49, 0x4D]; // "ANIM"

    private readonly List<AnimationTrack> _tracks = [];
    private readonly List<string> _contentFields =
    [
        "AnimationType",
        "AnimationName",
        "Duration",
        "FrameRate",
        "PlayMode",
        "BlendMode",
        "IsLooped",
        "Priority",
        "Tracks"
    ];

    private MemoryStream? _stream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationResource"/> class.
    /// </summary>
    public AnimationResource()
    {
        AnimationType = AnimationType.None;
        AnimationName = string.Empty;
        Duration = 0.0f;
        FrameRate = 30.0f;
        PlayMode = AnimationPlayMode.Once;
        BlendMode = AnimationBlendMode.Replace;
        IsLooped = false;
        Priority = 0;
        Tracks = new ReadOnlyCollection<AnimationTrack>(_tracks);
        RequestedApiVersion = 1;
        RecommendedApiVersion = 1;
        _stream = new MemoryStream();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationResource"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    public AnimationResource(Stream stream) : this()
    {
        ArgumentNullException.ThrowIfNull(stream);
        ReadFromStream(stream);
    }

    /// <inheritdoc />
    public AnimationType AnimationType { get; set; }

    /// <inheritdoc />
    public string AnimationName { get; set; } = string.Empty;

    /// <inheritdoc />
    public float Duration { get; set; }

    /// <inheritdoc />
    public float FrameRate { get; set; }

    /// <inheritdoc />
    public AnimationPlayMode PlayMode { get; set; }

    /// <inheritdoc />
    public AnimationBlendMode BlendMode { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<AnimationTrack> Tracks { get; private set; }

    /// <inheritdoc />
    public bool IsLooped { get; set; }

    /// <inheritdoc />
    public int Priority { get; set; }

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AnimationResource));

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
                throw new ObjectDisposedException(nameof(AnimationResource));

            using var ms = new MemoryStream();
            WriteToStream(ms);
            return ms.ToArray();
        }
    }

    /// <inheritdoc />
    public int RequestedApiVersion { get; }

    /// <inheritdoc />
    public int RecommendedApiVersion { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFields;

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
    /// Adds a track to the animation.
    /// </summary>
    /// <param name="track">The track to add</param>
    public void AddTrack(AnimationTrack track)
    {
        _tracks.Add(track);
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a track from the animation.
    /// </summary>
    /// <param name="track">The track to remove</param>
    /// <returns>True if the track was removed</returns>
    public bool RemoveTrack(AnimationTrack track)
    {
        var removed = _tracks.Remove(track);
        if (removed)
            OnResourceChanged();
        return removed;
    }

    /// <summary>
    /// Clears all tracks.
    /// </summary>
    public void ClearTracks()
    {
        if (_tracks.Count > 0)
        {
            _tracks.Clear();
            OnResourceChanged();
        }
    }

    private TypedValue GetFieldValue(int index)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return GetFieldValue(_contentFields[index]);
    }

    private TypedValue GetFieldValue(string name)
    {
        return name switch
        {
            "AnimationType" => TypedValue.Create(AnimationType),
            "AnimationName" => TypedValue.Create(AnimationName),
            "Duration" => TypedValue.Create(Duration),
            "FrameRate" => TypedValue.Create(FrameRate),
            "PlayMode" => TypedValue.Create(PlayMode),
            "BlendMode" => TypedValue.Create(BlendMode),
            "IsLooped" => TypedValue.Create(IsLooped),
            "Priority" => TypedValue.Create(Priority),
            "Tracks" => TypedValue.Create(Tracks.Count),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldValue(int index, TypedValue value)
    {
        if (index < 0 || index >= _contentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        SetFieldValue(_contentFields[index], value);
    }

    private void SetFieldValue(string name, TypedValue value)
    {
        switch (name)
        {
            case "AnimationType":
                if (value.Value is AnimationType animationType)
                    AnimationType = animationType;
                else if (value.Value is int animationIntValue)
                    AnimationType = (AnimationType)animationIntValue;
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
            case "PlayMode":
                if (value.Value is AnimationPlayMode playMode)
                    PlayMode = playMode;
                else if (value.Value is int playModeInt)
                    PlayMode = (AnimationPlayMode)playModeInt;
                break;
            case "BlendMode":
                if (value.Value is AnimationBlendMode blendMode)
                    BlendMode = blendMode;
                else if (value.Value is int blendModeInt)
                    BlendMode = (AnimationBlendMode)blendModeInt;
                break;
            case "IsLooped":
                if (value.Value is bool isLooped)
                    IsLooped = isLooped;
                break;
            case "Priority":
                if (value.Value is int priority)
                    Priority = priority;
                break;
            default:
                throw new ArgumentException($"Field '{name}' is read-only or unknown", nameof(name));
        }

        OnResourceChanged();
    }

    /// <summary>
    /// Loads the animation resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Copy stream to memory stream for processing
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        memoryStream.Position = 0;

        ReadFromStream(memoryStream);
    }

    private void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);

        // Handle empty stream gracefully
        if (stream.Length == 0)
        {
            // Initialize with default values for empty stream
            AnimationType = AnimationType.None;
            AnimationName = string.Empty;
            Duration = 0.0f;
            FrameRate = 30.0f;
            Tracks = new List<AnimationTrack>();
            return;
        }

        // Read and validate magic bytes
        if (stream.Length < 4)
        {
            throw new InvalidDataException("Invalid animation resource format");
        }

        var magic = reader.ReadBytes(4);
        if (!magic.SequenceEqual(MagicBytes))
        {
            throw new InvalidDataException("Invalid animation resource format");
        }

        // Read animation data
        AnimationType = (AnimationType)reader.ReadInt32();
        var nameLength = reader.ReadInt32();
        AnimationName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
        Duration = reader.ReadSingle();
        FrameRate = reader.ReadSingle();
        PlayMode = (AnimationPlayMode)reader.ReadInt32();
        BlendMode = (AnimationBlendMode)reader.ReadInt32();
        IsLooped = reader.ReadBoolean();
        Priority = reader.ReadInt32();

        // Read tracks
        var trackCount = reader.ReadInt32();
        _tracks.Clear();

        for (int i = 0; i < trackCount; i++)
        {
            var boneNameLength = reader.ReadInt32();
            var boneName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(boneNameLength));

            var propertyNameLength = reader.ReadInt32();
            var propertyName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(propertyNameLength));

            var keyframeCount = reader.ReadInt32();
            var keyframes = new List<AnimationKeyframe>();

            for (int j = 0; j < keyframeCount; j++)
            {
                var time = reader.ReadSingle();
                var valueType = reader.ReadInt32();

                object value = valueType switch
                {
                    0 => reader.ReadSingle(), // float
                    1 => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()), // Vector3
                    2 => new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()), // Vector4/Quaternion
                    _ => reader.ReadSingle() // default to float
                };

                var interpolationType = (InterpolationType)reader.ReadInt32();
                keyframes.Add(new AnimationKeyframe(time, value, interpolationType));
            }

            _tracks.Add(new AnimationTrack(boneName, propertyName, keyframes));
        }
    }

    private void WriteToStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);

        // Write magic bytes
        writer.Write(MagicBytes);

        // Write animation data
        writer.Write((int)AnimationType);
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(AnimationName);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write(Duration);
        writer.Write(FrameRate);
        writer.Write((int)PlayMode);
        writer.Write((int)BlendMode);
        writer.Write(IsLooped);
        writer.Write(Priority);

        // Write tracks
        writer.Write(_tracks.Count);

        foreach (var track in _tracks)
        {
            var boneNameBytes = System.Text.Encoding.UTF8.GetBytes(track.BoneName);
            writer.Write(boneNameBytes.Length);
            writer.Write(boneNameBytes);

            var propertyNameBytes = System.Text.Encoding.UTF8.GetBytes(track.PropertyName);
            writer.Write(propertyNameBytes.Length);
            writer.Write(propertyNameBytes);

            writer.Write(track.Keyframes.Count);

            foreach (var keyframe in track.Keyframes)
            {
                writer.Write(keyframe.Time);

                // Write value type and value
                switch (keyframe.Value)
                {
                    case float floatValue:
                        writer.Write(0); // float type
                        writer.Write(floatValue);
                        break;
                    case Vector3 vector3Value:
                        writer.Write(1); // Vector3 type
                        writer.Write(vector3Value.X);
                        writer.Write(vector3Value.Y);
                        writer.Write(vector3Value.Z);
                        break;
                    case Vector4 vector4Value:
                        writer.Write(2); // Vector4 type
                        writer.Write(vector4Value.X);
                        writer.Write(vector4Value.Y);
                        writer.Write(vector4Value.Z);
                        writer.Write(vector4Value.W);
                        break;
                    default:
                        writer.Write(0); // default to float
                        writer.Write(0.0f);
                        break;
                }

                writer.Write((int)keyframe.InterpolationType);
            }
        }
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if called from Dispose()</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _stream?.Dispose();
            _stream = null;
            _disposed = true;
        }
    }
}
