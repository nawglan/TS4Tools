namespace TS4Tools.Resources.Specialized;

/// <summary>
/// A modern implementation of The Sims 4 Object Key (OBJK) resource.
/// Object Key resources manage object component definitions and their associated data,
/// providing efficient access to component data, keys, and TGI block references for object identification.
/// </summary>
public sealed class ObjKeyResource : IObjKeyResource, IDisposable
{
    /// <summary>
    /// The standard resource type identifier for OBJK resources
    /// </summary>
    public const uint ResourceType = 0x48C28979;

    /// <summary>
    /// The supported version of the OBJK format
    /// </summary>
    public const uint SupportedVersion = 1;

    private readonly int _requestedApiVersion;
    private ulong _objectKey;
    private uint _objectType;
    private byte[] _additionalData;
    private bool _isModified;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new empty ObjKeyResource.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    public ObjKeyResource(int requestedApiVersion = 1)
    {
        _requestedApiVersion = requestedApiVersion;
        Version = SupportedVersion;
        _objectKey = 0;
        _objectType = 0;
        _additionalData = Array.Empty<byte>();
        _isModified = false;
    }

    /// <summary>
    /// Creates an ObjKeyResource from binary data.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="data">The binary OBJK data to parse</param>
    /// <returns>A new ObjKeyResource instance</returns>
    /// <exception cref="ArgumentException">Thrown when the data is invalid</exception>
    public static ObjKeyResource FromData(int requestedApiVersion, ReadOnlySpan<byte> data)
    {
        var resource = new ObjKeyResource(requestedApiVersion);
        resource.ParseData(data);
        return resource;
    }

    /// <summary>
    /// Creates an ObjKeyResource from a stream asynchronously.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="stream">The stream containing OBJK data</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created ObjKeyResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    public static async Task<ObjKeyResource> FromStreamAsync(
        int requestedApiVersion,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        return FromData(requestedApiVersion, memoryStream.ToArray());
    }

    #region IObjKeyResource Implementation

    /// <inheritdoc />
    public uint Version { get; private set; }

    /// <inheritdoc />
    public ulong ObjectKey
    {
        get => _objectKey;
        set
        {
            ThrowIfDisposed();
            if (_objectKey != value)
            {
                _objectKey = value;
                _isModified = true;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public uint ObjectType
    {
        get => _objectType;
        set
        {
            ThrowIfDisposed();
            if (_objectType != value)
            {
                _objectType = value;
                _isModified = true;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public byte[] AdditionalData
    {
        get => _additionalData;
        set
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(value);
            if (!_additionalData.SequenceEqual(value))
            {
                _additionalData = value;
                _isModified = true;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public bool IsValid()
    {
        ThrowIfDisposed();
        return ObjectKey != 0 && ObjectType != 0;
    }

    /// <inheritdoc />
    public Task GenerateNewKeyAsync(uint objectType, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        ObjectType = objectType;
        ObjectKey = (ulong)Random.Shared.NextInt64(1, long.MaxValue);

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
            var data = ToBinaryAsync().GetAwaiter().GetResult();
            return new MemoryStream(data);
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            ThrowIfDisposed();
            return ToBinaryAsync().GetAwaiter().GetResult();
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
        nameof(ObjectKey),
        nameof(ObjectType),
        nameof(AdditionalData)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(Version) => new TypedValue(typeof(uint), Version),
            nameof(ObjectKey) => new TypedValue(typeof(ulong), ObjectKey),
            nameof(ObjectType) => new TypedValue(typeof(uint), ObjectType),
            nameof(AdditionalData) => new TypedValue(typeof(byte[]), AdditionalData),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("ObjKey fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(Version)],
            1 => this[nameof(ObjectKey)],
            2 => this[nameof(ObjectType)],
            3 => this[nameof(AdditionalData)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-3, got {index}")
        };
        set => throw new NotSupportedException("ObjKey fields are read-only via integer indexer");
    }

    #endregion

    /// <summary>
    /// Converts the object key resource to binary format asynchronously.
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
        writer.Write(ObjectKey);
        writer.Write(ObjectType);
        writer.Write(AdditionalData.Length);
        writer.Write(AdditionalData);

        return Task.FromResult(memoryStream.ToArray());
    }

    #region Private Methods

    private void ParseData(ReadOnlySpan<byte> data)
    {
        if (data.Length < 20) // Minimum header size
        {
            throw new ArgumentException("Data too short to be a valid OBJK resource", nameof(data));
        }

        using var stream = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(stream);

        // Parse header
        Version = reader.ReadUInt32();
        ObjectKey = reader.ReadUInt64();
        ObjectType = reader.ReadUInt32();

        var dataLength = reader.ReadInt32();
        if (dataLength > 0 && stream.Length >= stream.Position + dataLength)
        {
            AdditionalData = reader.ReadBytes(dataLength);
        }
        else
        {
            AdditionalData = Array.Empty<byte>();
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
    /// Disposes of the resources used by this ObjKeyResource.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _additionalData = Array.Empty<byte>();
            _isDisposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Returns a string representation of this object key resource.
    /// </summary>
    /// <returns>A summary of the object key resource contents</returns>
    public override string ToString()
    {
        if (_isDisposed)
        {
            return "ObjKeyResource (Disposed)";
        }

        return $"ObjKeyResource (Version: {Version}, ObjectKey: 0x{ObjectKey:X16}, ObjectType: 0x{ObjectType:X8}, Data Length: {AdditionalData.Length:N0} bytes)";
    }
}
