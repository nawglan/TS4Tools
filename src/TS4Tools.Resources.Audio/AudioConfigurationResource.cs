using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Audio;

/// <summary>
/// Represents an Audio Configuration resource that defines audio settings and voice acting configurations.
/// This resource type (0xFD04E3BE) handles audio effect configurations, voice acting setups, and lip sync triggers.
/// </summary>
public sealed class AudioConfigurationResource : IResource, IDisposable
{
    private bool _disposed;
    private readonly ResourceKey _key;
    private byte[] _data;
    private MemoryStream? _stream;

    /// <summary>
    /// Gets the resource key.
    /// </summary>
    public ResourceKey Key => _key;

    /// <summary>
    /// Gets the raw data of the audio configuration resource.
    /// </summary>
    public byte[] Data => _data;

    /// <summary>
    /// Gets the version of the audio configuration format.
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>
    /// Gets the audio effect name associated with this configuration.
    /// </summary>
    public string AudioEffectName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether this is a voice acting (vo) configuration that triggers lip sync.
    /// </summary>
    public bool IsVoiceActing { get; private set; }

    /// <summary>
    /// Gets the instance ID hash for the audio effect.
    /// </summary>
    public ulong InstanceHash { get; private set; }

    /// <summary>
    /// Gets the number of audio parameters in this configuration.
    /// </summary>
    public int ParameterCount { get; private set; }

    #region IResource Implementation

    /// <summary>
    /// Gets the resource data as a stream.
    /// </summary>
    public Stream Stream
    {
        get
        {
            ThrowIfDisposed();
            return _stream ??= new MemoryStream(_data, writable: false);
        }
    }

    /// <summary>
    /// Gets the resource data as a byte array.
    /// </summary>
    public byte[] AsBytes
    {
        get
        {
            ThrowIfDisposed();
            return (byte[])_data.Clone();
        }
    }

    /// <summary>
    /// Occurs when the resource has been changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Gets the requested API version for this resource.
    /// </summary>
    public int RequestedApiVersion { get; init; } = 1;

    /// <summary>
    /// Gets the recommended API version for this resource.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Gets the content fields for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; } = new List<string>
    {
        "Version", "AudioEffectName", "IsVoiceActing", "InstanceHash", "ParameterCount", "Data"
    }.AsReadOnly();

    /// <summary>
    /// Gets or sets a content field value by index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <returns>The field value.</returns>
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => TypedValue.Create(Version),
            1 => TypedValue.Create(AudioEffectName),
            2 => TypedValue.Create(IsVoiceActing),
            3 => TypedValue.Create(InstanceHash),
            4 => TypedValue.Create(ParameterCount),
            5 => TypedValue.Create(_data),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set
        {
            switch (index)
            {
                case 1: // AudioEffectName
                    var name = value.GetValue<string>();
                    if (name != null)
                    {
                        AudioEffectName = name;
                        IsVoiceActing = name.StartsWith("vo", StringComparison.OrdinalIgnoreCase);
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 5: // Data field
                    var newData = value.GetValue<byte[]>();
                    if (newData != null)
                    {
                        _data = newData;
                        _stream?.Dispose();
                        _stream = null;
                        ParseData();
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>
    /// Gets or sets a content field value by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    public TypedValue this[string name]
    {
        get => name switch
        {
            "Version" => TypedValue.Create(Version),
            "AudioEffectName" => TypedValue.Create(AudioEffectName),
            "IsVoiceActing" => TypedValue.Create(IsVoiceActing),
            "InstanceHash" => TypedValue.Create(InstanceHash),
            "ParameterCount" => TypedValue.Create(ParameterCount),
            "Data" => TypedValue.Create(_data),
            _ => throw new ArgumentException($"Unknown field name: {name}", nameof(name))
        };
        set
        {
            switch (name)
            {
                case "AudioEffectName":
                    var effectName = value.GetValue<string>();
                    if (effectName != null)
                    {
                        AudioEffectName = effectName;
                        IsVoiceActing = effectName.StartsWith("vo", StringComparison.OrdinalIgnoreCase);
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case "Data":
                    var newData = value.GetValue<byte[]>();
                    if (newData != null)
                    {
                        _data = newData;
                        _stream?.Dispose();
                        _stream = null;
                        ParseData();
                        ResourceChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown field name: {name}", nameof(name));
            }
        }
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioConfigurationResource"/> class.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="data">The raw audio configuration data.</param>
    /// <param name="audioEffectName">Optional audio effect name.</param>
    public AudioConfigurationResource(ResourceKey key, byte[] data, string audioEffectName = "")
    {
        _key = key;
        _data = data ?? throw new ArgumentNullException(nameof(data));
        AudioEffectName = audioEffectName;
        ParseData();
    }

    /// <summary>
    /// Parses the audio configuration data to extract settings.
    /// </summary>
    private void ParseData()
    {
        if (_data.Length < 8)
        {
            Version = 1;
            return;
        }

        try
        {
            using var reader = new BinaryReader(new MemoryStream(_data));
            
            // Read version
            Version = reader.ReadUInt32();
            
            // Try to read instance hash if available
            if (_data.Length >= 16)
            {
                InstanceHash = reader.ReadUInt64();
            }
            
            // Try to read parameter count
            if (_data.Length >= 20)
            {
                ParameterCount = reader.ReadInt32();
            }
            
            // Try to extract audio effect name from data if not provided
            if (string.IsNullOrEmpty(AudioEffectName) && _data.Length > 24)
            {
                try
                {
                    var nameLength = reader.ReadUInt32();
                    if (nameLength > 0 && nameLength < 512 && reader.BaseStream.Position + nameLength <= reader.BaseStream.Length)
                    {
                        var nameBytes = reader.ReadBytes((int)nameLength);
                        AudioEffectName = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
                        
                        // Check if this is voice acting (starts with "vo")
                        IsVoiceActing = AudioEffectName.StartsWith("vo", StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // If name parsing fails, continue with defaults
                }
            }
        }
        catch
        {
            // If parsing fails, use defaults
            Version = 1;
            ParameterCount = 0;
        }
    }

    /// <summary>
    /// Gets the audio effect hash from the name.
    /// </summary>
    /// <param name="effectName">The audio effect name.</param>
    /// <returns>The hash of the effect name.</returns>
    public static ulong GetAudioEffectHash(string effectName)
    {
        if (string.IsNullOrEmpty(effectName))
            return 0;

        // Use FNV-1a hash algorithm (common in Sims 4)
        const ulong FnvOffsetBasis = 0xCBF29CE484222325;
        const ulong FnvPrime = 0x00000100000001B3;

        var hash = FnvOffsetBasis;
        var bytes = System.Text.Encoding.UTF8.GetBytes(effectName.ToLowerInvariant());

        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= FnvPrime;
        }

        return hash;
    }

    /// <summary>
    /// Determines if the audio effect should trigger lip sync animation.
    /// </summary>
    /// <returns>True if this audio effect should trigger lip sync.</returns>
    public bool ShouldTriggerLipSync()
    {
        return IsVoiceActing && !string.IsNullOrEmpty(AudioEffectName);
    }

    /// <summary>
    /// Serializes the audio configuration resource to a byte array.
    /// </summary>
    /// <returns>The serialized data.</returns>
    public byte[] Serialize()
    {
        ThrowIfDisposed();
        return (byte[])_data.Clone();
    }

    /// <summary>
    /// Serializes the audio configuration resource to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await stream.WriteAsync(_data, cancellationToken);
    }

    /// <summary>
    /// Gets the size of the resource data.
    /// </summary>
    /// <returns>The size in bytes.</returns>
    public long GetSize()
    {
        ThrowIfDisposed();
        return _data.Length;
    }

    /// <summary>
    /// Creates a clone of this audio configuration resource with a new key.
    /// </summary>
    /// <param name="newKey">The new resource key.</param>
    /// <returns>A cloned audio configuration resource.</returns>
    public AudioConfigurationResource Clone(ResourceKey newKey)
    {
        ThrowIfDisposed();
        return new AudioConfigurationResource(newKey, (byte[])_data.Clone(), AudioEffectName);
    }

    /// <summary>
    /// Throws if the resource has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AudioConfigurationResource));
        }
    }

    /// <summary>
    /// Releases all resources used by the AudioConfigurationResource.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Returns a string representation of this audio configuration resource.
    /// </summary>
    /// <returns>A descriptive string.</returns>
    public override string ToString()
    {
        var type = IsVoiceActing ? "Voice Acting" : "Sound Effect";
        var name = !string.IsNullOrEmpty(AudioEffectName) ? AudioEffectName : "Unknown";
        return $"AudioConfigurationResource [Key={_key}, Type={type}, Name={name}, Hash=0x{InstanceHash:X16}, Parameters={ParameterCount}, Size={_data.Length} bytes]";
    }
}
