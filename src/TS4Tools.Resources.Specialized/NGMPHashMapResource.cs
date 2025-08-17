using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Represents an NGMP (Named Game Map) hash map resource.
/// Provides efficient key-value lookup functionality for game data mapping.
/// </summary>
public sealed class NGMPHashMapResource : INGMPHashMapResource, IDisposable
{
    private readonly ILogger<NGMPHashMapResource> _logger;
    private readonly List<NGMPPair> _pairs;
    private readonly Dictionary<ulong, ulong> _hashMap;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Represents a name hash to instance mapping pair.
    /// </summary>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "NGMPPair is tightly coupled to NGMPHashMapResource and represents a domain-specific data structure")]
    public sealed class NGMPPair : IEquatable<NGMPPair>
    {
        /// <summary>
        /// Gets or sets the name hash.
        /// </summary>
        public ulong NameHash { get; set; }

        /// <summary>
        /// Gets or sets the instance value.
        /// </summary>
        public ulong Instance { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NGMPPair"/> class.
        /// </summary>
        /// <param name="nameHash">The name hash.</param>
        /// <param name="instance">The instance value.</param>
        public NGMPPair(ulong nameHash, ulong instance)
        {
            NameHash = nameHash;
            Instance = instance;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The other NGMP pair to compare.</param>
        /// <returns>True if the objects are equal; otherwise, false.</returns>
        public bool Equals(NGMPPair? other)
        {
            return other != null && NameHash == other.NameHash && Instance == other.Instance;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the objects are equal; otherwise, false.</returns>
        public override bool Equals(object? obj) => Equals(obj as NGMPPair);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => HashCode.Combine(NameHash, Instance);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NGMPHashMapResource"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public NGMPHashMapResource(ILogger<NGMPHashMapResource>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NGMPHashMapResource>.Instance;
        _pairs = new List<NGMPPair>();
        _hashMap = new Dictionary<ulong, ulong>();

        _logger.LogDebug("Created new NGMPHashMapResource instance");
    }

    /// <inheritdoc />
    public uint Version { get; private set; } = 1;

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _pairs.Count;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ulong> NameHashes
    {
        get
        {
            lock (_lock)
            {
                return _pairs.Select(p => p.NameHash).ToList().AsReadOnly();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ulong> Instances
    {
        get
        {
            lock (_lock)
            {
                return _pairs.Select(p => p.Instance).ToList().AsReadOnly();
            }
        }
    }

    /// <inheritdoc />
    public Stream Stream { get; private set; } = new MemoryStream();

    /// <inheritdoc />
    public int RequestedApiVersion { get; } = 1;

    /// <inheritdoc />
    public int RecommendedApiVersion { get; } = 1;

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            using var memoryStream = new MemoryStream();
            Stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }

    private readonly Dictionary<string, TypedValue> _contentFields = new();
    private readonly List<string> _contentFieldNames = ["Version", "Count", "PairData"];

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => _contentFieldNames.AsReadOnly();

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => this[_contentFieldNames[index]];
        set => this[_contentFieldNames[index]] = value;
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get
        {
            lock (_lock)
            {
                return name switch
                {
                    nameof(Version) => TypedValue.Create(Version),
                    nameof(Count) => TypedValue.Create(Count),
                    "PairData" => TypedValue.Create(string.Join(", ", _pairs.Select(p => $"({p.NameHash:X16}, {p.Instance:X16})"))),
                    _ => _contentFields.TryGetValue(name, out var value) ? value : TypedValue.Create<object?>(null)
                };
            }
        }
        set
        {
            lock (_lock)
            {
                _contentFields[name] = value;
                OnResourceChanged();
            }
        }
    }

    /// <inheritdoc />
    public bool ContainsNameHash(ulong nameHash)
    {
        lock (_lock)
        {
            return _hashMap.ContainsKey(nameHash);
        }
    }

    /// <inheritdoc />
    public ulong? GetInstance(ulong nameHash)
    {
        lock (_lock)
        {
            return _hashMap.TryGetValue(nameHash, out var instance) ? instance : null;
        }
    }

    /// <inheritdoc />
    public bool TryGetInstance(ulong nameHash, out ulong instance)
    {
        lock (_lock)
        {
            return _hashMap.TryGetValue(nameHash, out instance);
        }
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(ulong nameHash, ulong instance, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        await Task.Run(() =>
        {
            lock (_lock)
            {
                // Remove existing entry if it exists
                var existingIndex = _pairs.FindIndex(p => p.NameHash == nameHash);
                if (existingIndex >= 0)
                {
                    _pairs.RemoveAt(existingIndex);
                    _logger.LogDebug("Updated existing NGMP pair: NameHash={NameHash:X16}, Instance={Instance:X16}", nameHash, instance);
                }
                else
                {
                    _logger.LogDebug("Added new NGMP pair: NameHash={NameHash:X16}, Instance={Instance:X16}", nameHash, instance);
                }

                // Add new entry
                _pairs.Add(new NGMPPair(nameHash, instance));
                _hashMap[nameHash] = instance;

                OnResourceChanged();
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(ulong nameHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var index = _pairs.FindIndex(p => p.NameHash == nameHash);
                if (index >= 0)
                {
                    _pairs.RemoveAt(index);
                    _hashMap.Remove(nameHash);
                    _logger.LogDebug("Removed NGMP pair: NameHash={NameHash:X16}", nameHash);
                    OnResourceChanged();
                    return true;
                }
                return false;
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        await Task.Run(() =>
        {
            lock (_lock)
            {
                var count = _pairs.Count;
                _pairs.Clear();
                _hashMap.Clear();
                _logger.LogDebug("Cleared {Count} NGMP pairs", count);
                OnResourceChanged();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Parses NGMP hash map data from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the data.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException("Stream must be readable", nameof(stream));

        await Task.Run(() =>
        {
            lock (_lock)
            {
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

                // Read version
                Version = reader.ReadUInt32();
                if (Version != 1)
                {
                    throw new InvalidDataException($"Unsupported NGMP version: {Version}. Expected: 1");
                }

                // Read pairs count
                var count = reader.ReadUInt32();
                _logger.LogDebug("Parsing NGMP hash map with {Count} pairs, version {Version}", count, Version);

                // Clear existing data
                _pairs.Clear();
                _hashMap.Clear();

                // Read pairs
                for (var i = 0; i < count; i++)
                {
                    var nameHash = reader.ReadUInt64();
                    var instance = reader.ReadUInt64();

                    var pair = new NGMPPair(nameHash, instance);
                    _pairs.Add(pair);
                    _hashMap[nameHash] = instance;
                }

                _logger.LogInformation("Successfully parsed NGMP hash map with {Count} pairs", _pairs.Count);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Serializes the NGMP hash map data to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable", nameof(stream));

        await Task.Run(() =>
        {
            lock (_lock)
            {
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

                // Write version
                writer.Write(Version);

                // Write pairs count
                writer.Write((uint)_pairs.Count);

                // Write pairs
                foreach (var pair in _pairs)
                {
                    writer.Write(pair.NameHash);
                    writer.Write(pair.Instance);
                }

                _logger.LogDebug("Serialized NGMP hash map with {Count} pairs", _pairs.Count);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            Stream?.Dispose();
            _disposed = true;
            _logger.LogDebug("NGMPHashMapResource disposed");
        }
    }
}
