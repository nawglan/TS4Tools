namespace TS4Tools.Resources.Specialized;

/// <summary>
/// A modern implementation of The Sims 4 User CAS (Create-A-Sim) Preset resource.
/// UserCAStPreset resources manage user-created character customization presets,
/// providing storage and retrieval of character appearance configurations.
/// </summary>
public sealed class UserCAStPresetResource : IUserCAStPresetResource, IDisposable
{
    /// <summary>
    /// The standard resource type identifier for UserCAStPreset resources
    /// </summary>
    public const uint ResourceType = 0x0591B1AF;

    /// <summary>
    /// The supported version of the UserCAStPreset format
    /// </summary>
    public const uint SupportedVersion = 3;

    private readonly int _requestedApiVersion;
    private readonly List<CASPreset> _presets;
    private uint _version;
    private uint _unknown1;
    private uint _unknown2;
    private uint _unknown3;
    private bool _isModified;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new empty UserCAStPresetResource.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    public UserCAStPresetResource(int requestedApiVersion = 1)
    {
        _requestedApiVersion = requestedApiVersion;
        _presets = new List<CASPreset>();
        _version = SupportedVersion;
        _unknown1 = 0;
        _unknown2 = 0;
        _unknown3 = 0;
        _isModified = false;
    }

    /// <summary>
    /// Creates a UserCAStPresetResource from binary data.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="data">The binary UserCAStPreset data to parse</param>
    /// <returns>A new UserCAStPresetResource instance</returns>
    /// <exception cref="ArgumentException">Thrown when the data is invalid</exception>
    public static UserCAStPresetResource FromData(int requestedApiVersion, ReadOnlySpan<byte> data)
    {
        var resource = new UserCAStPresetResource(requestedApiVersion);
        resource.ParseData(data);
        return resource;
    }

    /// <summary>
    /// Creates a UserCAStPresetResource from a stream asynchronously.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="stream">The stream containing UserCAStPreset data</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created UserCAStPresetResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    public static async Task<UserCAStPresetResource> FromStreamAsync(
        int requestedApiVersion,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        return FromData(requestedApiVersion, memoryStream.ToArray());
    }

    #region IUserCAStPresetResource Implementation

    /// <inheritdoc />
    public uint Version
    {
        get => _version;
        private set
        {
            ThrowIfDisposed();
            if (_version != value)
            {
                _version = value;
                _isModified = true;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ICASPreset> Presets
    {
        get
        {
            ThrowIfDisposed();
            return _presets.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public uint Unknown1
    {
        get => _unknown1;
        set
        {
            ThrowIfDisposed();
            if (_unknown1 != value)
            {
                _unknown1 = value;
                _isModified = true;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public uint Unknown2
    {
        get => _unknown2;
        set
        {
            ThrowIfDisposed();
            if (_unknown2 != value)
            {
                _unknown2 = value;
                _isModified = true;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public uint Unknown3
    {
        get => _unknown3;
        set
        {
            ThrowIfDisposed();
            if (_unknown3 != value)
            {
                _unknown3 = value;
                _isModified = true;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public Task AddPresetAsync(ICASPreset preset, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(preset);
        cancellationToken.ThrowIfCancellationRequested();

        // Convert to our internal type if needed
        CASPreset casPreset;
        if (preset is CASPreset cp)
        {
            casPreset = cp;
        }
        else
        {
            casPreset = new CASPreset(
                preset.Xml,
                preset.Unknown1,
                preset.Unknown2,
                preset.Unknown3,
                preset.Unknown4,
                preset.Unknown5,
                preset.Unknown6);
        }

        _presets.Add(casPreset);
        _isModified = true;
        OnResourceChanged();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemovePresetAsync(int index, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (index < 0 || index >= _presets.Count)
        {
            return Task.FromResult(false);
        }

        _presets.RemoveAt(index);
        _isModified = true;
        OnResourceChanged();

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task ClearPresetsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (_presets.Count > 0)
        {
            _presets.Clear();
            _isModified = true;
            OnResourceChanged();
        }

        return Task.CompletedTask;
    }

    #endregion

    /// <summary>
    /// Gets a value indicating whether this resource has been modified since it was loaded or created.
    /// </summary>
    public bool IsModified => _isModified;

    #region IResource Implementation

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            ThrowIfDisposed();
            // Use Task.Run to avoid deadlocks in synchronization contexts
            var data = Task.Run(async () => await ToBinaryAsync().ConfigureAwait(false))
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return new MemoryStream(data);
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            ThrowIfDisposed();
            // Use Task.Run to avoid deadlocks in synchronization contexts
            return Task.Run(async () => await ToBinaryAsync().ConfigureAwait(false))
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    #endregion

    #region IApiVersion Implementation

    /// <inheritdoc />
    public int RequestedApiVersion => _requestedApiVersion;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    #endregion

    #region IContentFields Implementation

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => new[]
    {
        nameof(Version),
        nameof(Unknown1),
        nameof(Unknown2),
        nameof(Unknown3),
        nameof(Presets)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(Version) => new TypedValue(typeof(uint), Version),
            nameof(Unknown1) => new TypedValue(typeof(uint), Unknown1),
            nameof(Unknown2) => new TypedValue(typeof(uint), Unknown2),
            nameof(Unknown3) => new TypedValue(typeof(uint), Unknown3),
            nameof(Presets) => new TypedValue(typeof(IReadOnlyList<ICASPreset>), Presets),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("UserCAStPreset fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(Version)],
            1 => this[nameof(Unknown1)],
            2 => this[nameof(Unknown2)],
            3 => this[nameof(Unknown3)],
            4 => this[nameof(Presets)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-4, got {index}")
        };
        set => throw new NotSupportedException("UserCAStPreset fields are read-only via integer indexer");
    }

    #endregion

    /// <summary>
    /// Converts the UserCAStPreset resource to binary format asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the binary data</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public Task<byte[]> ToBinaryAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        // Write header
        writer.Write(Version);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);

        // Write preset count
        writer.Write((uint)_presets.Count);

        // Write each preset
        foreach (var preset in _presets)
        {
            preset.WriteToBinaryWriter(writer);
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    #region Private Methods

    private void ParseData(ReadOnlySpan<byte> data)
    {
        if (data.Length < 20) // Minimum header size
        {
            throw new ArgumentException("Data too short to be a valid UserCAStPreset resource", nameof(data));
        }

        using var stream = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(stream);

        // Parse header
        Version = reader.ReadUInt32();
        Unknown1 = reader.ReadUInt32();
        Unknown2 = reader.ReadUInt32();
        Unknown3 = reader.ReadUInt32();

        // Parse preset count
        var presetCount = reader.ReadUInt32();
        if (presetCount > 10000) // Reasonable limit
        {
            throw new ArgumentException($"Invalid preset count: {presetCount}", nameof(data));
        }

        // Parse presets
        _presets.Clear();
        for (var i = 0; i < presetCount; i++)
        {
            try
            {
                var preset = CASPreset.FromBinaryReader(reader);
                _presets.Add(preset);
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
            {
                throw new ArgumentException($"Failed to parse preset {i}: {ex.Message}", nameof(data), ex);
            }
        }

        _isModified = false;
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of the resources used by this UserCAStPresetResource.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _presets.Clear();
            _isDisposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Returns a string representation of this UserCAStPreset resource.
    /// </summary>
    /// <returns>A summary of the UserCAStPreset resource contents</returns>
    public override string ToString()
    {
        if (_isDisposed)
        {
            return "UserCAStPresetResource (Disposed)";
        }

        return $"UserCAStPresetResource (Version: {Version}, Presets: {_presets.Count:N0}, " +
               $"Unknown1: {Unknown1}, Unknown2: {Unknown2}, Unknown3: {Unknown3})";
    }
}
