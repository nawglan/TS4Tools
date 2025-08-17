namespace TS4Tools.Resources.Strings;

/// <summary>
/// A modern implementation of The Sims 4 String Table (STBL) resource.
/// String Tables store localized text strings for the game's user interface,
/// object names, descriptions, and other text content.
/// </summary>
/// <remarks>
/// The STBL format consists of:
/// - Magic number "STBL" (0x4C425453)
/// - Version (typically 0x05)
/// - Compression flag
/// - Number of entries
/// - Reserved bytes  
/// - Total string data length
/// - String entries (key-value pairs)
/// 
/// This implementation provides full compatibility with the legacy s4pi format
/// while offering modern async patterns, proper resource management, and
/// comprehensive validation.
/// </remarks>
public sealed class StringTableResource : IResource, IDisposable
{
    /// <summary>
    /// The magic number identifying STBL resources ("STBL")
    /// </summary>
    public const uint MagicNumber = 0x4C425453; // "STBL" in little-endian

    /// <summary>
    /// The standard resource type identifier for STBL resources
    /// </summary>
    public const uint ResourceType = 0x220557DA;

    /// <summary>
    /// The supported version of the STBL format
    /// </summary>
    public const ushort SupportedVersion = 0x05;

    private readonly int _requestedApiVersion;
    private readonly Dictionary<uint, string> _strings;
    private readonly byte[] _reserved;
    private bool _isModified;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new empty StringTableResource.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    public StringTableResource(int requestedApiVersion = 1)
    {
        _requestedApiVersion = requestedApiVersion;
        _strings = new Dictionary<uint, string>();
        _reserved = new byte[2]; // Standard reserved bytes
        Version = SupportedVersion;
        IsCompressed = 0; // No compression support in this implementation
        _isModified = false;
    }

    /// <summary>
    /// Creates a StringTableResource from binary data.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="data">The binary STBL data to parse</param>
    /// <param name="encoding">The text encoding to use (defaults to UTF-8)</param>
    /// <returns>A new StringTableResource instance</returns>
    /// <exception cref="ArgumentException">Thrown when the data is invalid</exception>
    public static StringTableResource FromData(int requestedApiVersion, ReadOnlySpan<byte> data, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var resource = new StringTableResource(requestedApiVersion);
        resource.ParseData(data, encoding);
        return resource;
    }

    /// <summary>
    /// Creates a StringTableResource from a stream asynchronously.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="stream">The stream containing STBL data</param>
    /// <param name="encoding">The text encoding to use (defaults to UTF-8)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created StringTableResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    public static async Task<StringTableResource> FromStreamAsync(
        int requestedApiVersion,
        Stream stream,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        return FromData(requestedApiVersion, memoryStream.ToArray(), encoding);
    }

    /// <summary>
    /// Gets the version of the STBL format.
    /// </summary>
    public ushort Version { get; private set; }

    /// <summary>
    /// Gets whether the string data is compressed (not currently supported).
    /// </summary>
    public byte IsCompressed { get; private set; }

    /// <summary>
    /// Gets the number of string entries in this table.
    /// </summary>
    public ulong NumberOfEntries => (ulong)_strings.Count;

    /// <summary>
    /// Gets a value indicating whether the resource has been modified since creation or last save.
    /// </summary>
    public bool IsModified => _isModified;

    /// <summary>
    /// Gets the total length of all string data in bytes.
    /// </summary>
    public uint StringDataLength
    {
        get
        {
            uint total = 0;
            foreach (var value in _strings.Values)
            {
                total += (uint)Encoding.UTF8.GetByteCount(value);
                total += 5; // 4 bytes for key + 1 byte for length
            }
            return total;
        }
    }

    /// <summary>
    /// Gets all string entries as a read-only collection.
    /// </summary>
    public IReadOnlyDictionary<uint, string> Strings => _strings;

    /// <summary>
    /// Gets or sets a string value by its key.
    /// </summary>
    /// <param name="key">The string key</param>
    /// <returns>The string value, or null if the key doesn't exist</returns>
    public string? this[uint key]
    {
        get => _strings.TryGetValue(key, out var value) ? value : null;
        set
        {
            if (value == null)
            {
                RemoveString(key);
            }
            else
            {
                SetString(key, value);
            }
        }
    }

    /// <summary>
    /// Adds or updates a string entry.
    /// </summary>
    /// <param name="key">The unique key for the string</param>
    /// <param name="value">The string value</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public void SetString(uint key, string value)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(value);

        _strings[key] = value;
        _isModified = true;
        OnResourceChanged();
    }

    /// <summary>
    /// Removes a string entry by key.
    /// </summary>
    /// <param name="key">The key to remove</param>
    /// <returns>True if the key was found and removed; otherwise false</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public bool RemoveString(uint key)
    {
        ThrowIfDisposed();

        var removed = _strings.Remove(key);
        if (removed)
        {
            _isModified = true;
            OnResourceChanged();
        }
        return removed;
    }

    /// <summary>
    /// Checks if a string key exists in the table.
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key exists; otherwise false</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public bool ContainsKey(uint key)
    {
        ThrowIfDisposed();
        return _strings.ContainsKey(key);
    }

    /// <summary>
    /// Clears all string entries from the table.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public void Clear()
    {
        ThrowIfDisposed();

        if (_strings.Count > 0)
        {
            _strings.Clear();
            _isModified = true;
            OnResourceChanged();
        }
    }

    /// <summary>
    /// Gets all string entries as StringEntry records.
    /// </summary>
    /// <returns>An enumerable of StringEntry records</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public IEnumerable<StringEntry> GetEntries()
    {
        ThrowIfDisposed();
        return _strings.Select(kvp => new StringEntry(kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Converts the string table to binary format asynchronously.
    /// </summary>
    /// <param name="encoding">The text encoding to use (defaults to UTF-8)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the binary data</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public Task<byte[]> ToBinaryAsync(Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        encoding ??= Encoding.UTF8;

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, encoding, leaveOpen: true);

        // Write header
        writer.Write(MagicNumber);
        writer.Write(Version);
        writer.Write(IsCompressed);
        writer.Write(NumberOfEntries);
        writer.Write(_reserved);
        writer.Write(StringDataLength);

        // Write string entries
        foreach (var entry in GetEntries())
        {
            entry.WriteTo(writer, encoding);
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    /// <summary>
    /// Gets the resource as a stream.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public Stream Stream
    {
        get
        {
            ThrowIfDisposed();
            var data = ToBinaryAsync().GetAwaiter().GetResult();
            return new MemoryStream(data);
        }
    }

    /// <summary>
    /// Gets the resource as a byte array.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed</exception>
    public byte[] AsBytes
    {
        get
        {
            ThrowIfDisposed();
            return ToBinaryAsync().GetAwaiter().GetResult();
        }
    }

    #region IResource Implementation

    /// <inheritdoc />
    public int RequestedApiVersion => _requestedApiVersion;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => new[]
    {
        nameof(Version),
        nameof(IsCompressed),
        nameof(NumberOfEntries),
        nameof(StringDataLength),
        nameof(Strings)
    };

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(Version) => new TypedValue(typeof(ushort), Version),
            nameof(IsCompressed) => new TypedValue(typeof(byte), IsCompressed),
            nameof(NumberOfEntries) => new TypedValue(typeof(ulong), NumberOfEntries),
            nameof(StringDataLength) => new TypedValue(typeof(uint), StringDataLength),
            nameof(Strings) => new TypedValue(typeof(IReadOnlyDictionary<uint, string>), Strings),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("String table fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(Version)],
            1 => this[nameof(IsCompressed)],
            2 => this[nameof(NumberOfEntries)],
            3 => this[nameof(StringDataLength)],
            4 => this[nameof(Strings)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-4, got {index}")
        };
        set => throw new NotSupportedException("String table fields are read-only via integer indexer");
    }

    #endregion

    #region Private Methods

    private void ParseData(ReadOnlySpan<byte> data, Encoding encoding)
    {
        if (data.Length < 19) // Minimum header size
        {
            throw new ArgumentException("Data too short to be a valid STBL resource", nameof(data));
        }

        using var stream = new MemoryStream(data.ToArray());
        using var reader = new BinaryReader(stream, encoding);

        // Parse header
        var magic = reader.ReadUInt32();
        if (magic != MagicNumber)
        {
            throw new ArgumentException($"Invalid magic number: expected 0x{MagicNumber:X8}, got 0x{magic:X8}", nameof(data));
        }

        Version = reader.ReadUInt16();
        IsCompressed = reader.ReadByte();
        var entryCount = reader.ReadUInt64();

        var reserved = reader.ReadBytes(2);
        Array.Copy(reserved, _reserved, Math.Min(reserved.Length, _reserved.Length));

        var stringDataLength = reader.ReadUInt32();

        // Parse string entries
        for (ulong i = 0; i < entryCount; i++)
        {
            var entry = StringEntry.ReadFrom(reader, encoding);
            _strings[entry.Key] = entry.Value;
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
    /// Disposes of the resources used by this StringTableResource.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _strings.Clear();
            _isDisposed = true;
        }
    }

    #endregion

    /// <summary>
    /// Returns a string representation of this string table.
    /// </summary>
    /// <returns>A summary of the string table contents</returns>
    public override string ToString()
    {
        if (_isDisposed)
        {
            return "StringTableResource (Disposed)";
        }

        return $"StringTableResource (Version: {Version}, Entries: {NumberOfEntries}, Data Length: {StringDataLength:N0} bytes)";
    }
}
