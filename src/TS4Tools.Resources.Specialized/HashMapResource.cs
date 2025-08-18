namespace TS4Tools.Resources.Specialized;

/// <summary>
/// A modern implementation of The Sims 4 Hash Map resource.
/// Hash Map resources provide efficient string-to-value mappings with support for
/// hash-based lookups, capacity management, and bulk operations.
/// </summary>
public sealed class HashMapResource : IHashMapResource, IDisposable
{
    /// <summary>
    /// The standard resource type identifier for hash map resources
    /// </summary>
    public const uint ResourceType = 0x49596978;

    /// <summary>
    /// The supported version of the hash map format
    /// </summary>
    public const uint SupportedVersion = 1;

    private readonly int _requestedApiVersion;
    private readonly Dictionary<uint, object> _hashMap;
    private uint _version;
    private bool _isModified;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new empty HashMapResource.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    public HashMapResource(int requestedApiVersion = 1)
    {
        _requestedApiVersion = requestedApiVersion;
        _hashMap = new Dictionary<uint, object>();
        _version = SupportedVersion;
        _isModified = false;
    }

    /// <summary>
    /// Initializes a new HashMapResource with the specified capacity.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="capacity">The initial capacity of the hash map</param>
    public HashMapResource(int requestedApiVersion, int capacity)
    {
        _requestedApiVersion = requestedApiVersion;
        _hashMap = new Dictionary<uint, object>(capacity);
        _version = SupportedVersion;
        _isModified = false;
    }

    /// <summary>
    /// Creates a HashMapResource from binary data.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="data">The binary hash map data to parse</param>
    /// <returns>A new HashMapResource instance</returns>
    /// <exception cref="ArgumentException">Thrown when the data is invalid</exception>
    public static HashMapResource FromData(int requestedApiVersion, ReadOnlySpan<byte> data)
    {
        var resource = new HashMapResource(requestedApiVersion);
        resource.ParseData(data);
        return resource;
    }

    /// <summary>
    /// Creates a HashMapResource from a stream asynchronously.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="stream">The stream containing hash map data</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created HashMapResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    public static async Task<HashMapResource> FromStreamAsync(
        int requestedApiVersion,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        return FromData(requestedApiVersion, memoryStream.ToArray());
    }

    #region IHashMapResource Implementation

    /// <inheritdoc />
    public uint Version
    {
        get => _version;
        set
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
    public int Count
    {
        get
        {
            ThrowIfDisposed();
            return _hashMap.Count;
        }
    }

    /// <inheritdoc />
    public int Capacity
    {
        get
        {
            ThrowIfDisposed();
            return _hashMap.EnsureCapacity(0);
        }
        set
        {
            ThrowIfDisposed();
            if (value < Count)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Capacity ({value}) cannot be less than count ({Count})");
            }
            _hashMap.EnsureCapacity(value);
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public double LoadFactor
    {
        get
        {
            ThrowIfDisposed();
            return Capacity > 0 ? (double)Count / Capacity : 0.0;
        }
    }

    /// <inheritdoc />
    public bool ContainsKey(uint key)
    {
        ThrowIfDisposed();
        return _hashMap.ContainsKey(key);
    }

    /// <inheritdoc />
    public T? GetValue<T>(uint key)
    {
        ThrowIfDisposed();
        if (_hashMap.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Try to convert
            try
            {
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <inheritdoc />
    public bool TryGetValue<T>(uint key, out T? value)
    {
        ThrowIfDisposed();
        value = default;

        if (_hashMap.TryGetValue(key, out var obj))
        {
            if (obj is T typedValue)
            {
                value = typedValue;
                return true;
            }

            // Try to convert
            try
            {
                value = (T?)Convert.ChangeType(obj, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public void SetValue(uint key, object value)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(value);

        var oldValue = _hashMap.TryGetValue(key, out var existing) ? existing : null;
        if (!Equals(oldValue, value))
        {
            _hashMap[key] = value;
            _isModified = true;
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public bool Remove(uint key)
    {
        ThrowIfDisposed();
        var removed = _hashMap.Remove(key);
        if (removed)
        {
            _isModified = true;
            OnResourceChanged();
        }
        return removed;
    }

    /// <inheritdoc />
    public void Clear()
    {
        ThrowIfDisposed();
        if (_hashMap.Count > 0)
        {
            _hashMap.Clear();
            _isModified = true;
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public IEnumerable<uint> GetKeys()
    {
        ThrowIfDisposed();
        return _hashMap.Keys;
    }

    /// <inheritdoc />
    public IEnumerable<object> GetValues()
    {
        ThrowIfDisposed();
        return _hashMap.Values;
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
                .GetAwaiter().GetResult();
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
                .GetAwaiter().GetResult();
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
        nameof(Count),
        nameof(Capacity)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(Version) => new TypedValue(typeof(uint), Version),
            nameof(Count) => new TypedValue(typeof(int), Count),
            nameof(Capacity) => new TypedValue(typeof(int), Capacity),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("HashMap fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(Version)],
            1 => this[nameof(Count)],
            2 => this[nameof(Capacity)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-2, got {index}")
        };
        set => throw new NotSupportedException("HashMap fields are read-only via integer indexer");
    }

    #endregion

    /// <summary>
    /// Converts the hash map resource to binary format asynchronously.
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
        writer.Write((uint)Count);

        // Write hash entries
        foreach (var kvp in _hashMap)
        {
            writer.Write(kvp.Key);

            var valueString = kvp.Value?.ToString() ?? string.Empty;
            var stringBytes = Encoding.UTF8.GetBytes(valueString);
            writer.Write(stringBytes.Length);
            writer.Write(stringBytes);
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    #region Private Methods

    private void ParseData(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8) // Minimum header size
        {
            throw new ArgumentException("Data too short to be a valid hash map resource", nameof(data));
        }

        using var stream = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(stream);

        // Parse header
        Version = reader.ReadUInt32();
        var entryCount = reader.ReadUInt32();

        // Parse entries
        _hashMap.Clear();
        for (var i = 0; i < entryCount; i++)
        {
            if (stream.Position >= stream.Length - 8) break;

            var hash = reader.ReadUInt32();
            var stringLength = reader.ReadInt32();

            if (stringLength > 0 && stream.Position + stringLength <= stream.Length)
            {
                var stringBytes = reader.ReadBytes(stringLength);
                var value = Encoding.UTF8.GetString(stringBytes);
                _hashMap[hash] = value; // Store as object
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
    /// Disposes of the resources used by this HashMapResource.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _hashMap.Clear();
            _isDisposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Returns a string representation of this hash map resource.
    /// </summary>
    /// <returns>A summary of the hash map resource contents</returns>
    public override string ToString()
    {
        if (_isDisposed)
        {
            return "HashMapResource (Disposed)";
        }

        return $"HashMapResource (Version: {Version}, Count: {Count:N0}, Capacity: {Capacity:N0})";
    }
}
