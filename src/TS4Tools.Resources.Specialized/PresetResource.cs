using System.Text.Json;
using TS4Tools.Core.Interfaces.Resources.Specialized;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Specialized;

/// <summary>
/// Generic preset resource implementation supporting various data types, inheritance, and versioning.
/// Provides a flexible foundation for storing and managing preset configurations.
/// </summary>
public sealed class PresetResource : IPresetResource, IDisposable
{
    /// <summary>
    /// The resource type identifier for PresetResource.
    /// </summary>
    public const uint ResourceType = 0x4E69E952; // "PRES" in hex

    private readonly Dictionary<string, object> _data;
    private readonly List<string> _contentFields;
    private bool _disposed;

    private string _presetType;
    private string _presetName;
    private Version _presetVersion;
    private IPresetResource? _parentPreset;

    /// <summary>
    /// Event raised when the resource is changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Initializes a new empty PresetResource.
    /// </summary>
    /// <param name="presetType">The type identifier for this preset</param>
    /// <param name="name">The display name for this preset</param>
    /// <param name="requestedApiVersion">The requested API version</param>
    public PresetResource(string presetType, string name, int requestedApiVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(presetType);
        ArgumentNullException.ThrowIfNull(name);

        _presetType = presetType;
        _presetName = name;
        _presetVersion = new Version(1, 0);
        RequestedApiVersion = requestedApiVersion;

        _data = new Dictionary<string, object>();
        _contentFields = new List<string> { "PresetType", "PresetName", "PresetVersion", "Data" };
    }

    /// <summary>
    /// Creates a PresetResource from binary data.
    /// </summary>
    /// <param name="presetType">The type identifier for this preset</param>
    /// <param name="requestedApiVersion">The requested API version</param>
    /// <param name="data">The binary data to parse</param>
    /// <returns>A new PresetResource instance</returns>
    public static PresetResource FromData(string presetType, int requestedApiVersion, ReadOnlySpan<byte> data)
    {
        var resource = new PresetResource(presetType, "Unnamed", requestedApiVersion);
        resource.ParseFromBinary(data);
        return resource;
    }

    /// <summary>
    /// Creates a PresetResource from a stream asynchronously.
    /// </summary>
    /// <param name="presetType">The type identifier for this preset</param>
    /// <param name="requestedApiVersion">The requested API version</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task containing the created PresetResource</returns>
    public static async Task<PresetResource> FromStreamAsync(
        string presetType,
        int requestedApiVersion,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[stream.Length];
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        return FromData(presetType, requestedApiVersion, buffer);
    }

    #region IPresetResource Implementation

    /// <inheritdoc />
    public string PresetType
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _presetType;
        }
    }

    /// <inheritdoc />
    public string PresetName
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _presetName;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _presetName = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <inheritdoc />
    public Version PresetVersion
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _presetVersion;
        }
    }

    /// <inheritdoc />
    public IDictionary<string, object> Data
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _data;
        }
    }

    /// <inheritdoc />
    public TValue? GetValue<TValue>(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);

        // Check local data first
        if (_data.TryGetValue(key, out var value))
        {
            if (value is TValue typedValue)
            {
                return typedValue;
            }

            // Try to convert the value
            try
            {
                return (TValue?)Convert.ChangeType(value, typeof(TValue));
            }
            catch
            {
                return default;
            }
        }

        // Check parent preset if available
        if (_parentPreset != null)
        {
            return _parentPreset.GetValue<TValue>(key);
        }

        return default;
    }

    /// <inheritdoc />
    public Task SetValueAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);

        _data[key] = value!;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IPresetResource? ParentPreset
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _parentPreset;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Prevent circular references
            if (value != null && IsAncestor(value))
            {
                throw new InvalidOperationException("Cannot set parent to an ancestor preset to avoid circular references.");
            }

            _parentPreset = value;
        }
    }

    /// <inheritdoc />
    public Task<IPresetResource> CreateChildPresetAsync(string name, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(name);

        var child = new PresetResource(_presetType, name, RequestedApiVersion)
        {
            ParentPreset = this,
            _presetVersion = _presetVersion
        };

        return Task.FromResult<IPresetResource>(child);
    }

    /// <inheritdoc />
    public Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            // Validate basic properties
            if (string.IsNullOrWhiteSpace(_presetType))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(_presetName))
                return Task.FromResult(false);

            if (_presetVersion == null)
                return Task.FromResult(false);

            // Validate no circular inheritance
            if (_parentPreset != null && IsAncestor(_parentPreset))
                return Task.FromResult(false);

            // Validate data serialization
            try
            {
                var _ = JsonSerializer.Serialize(_data);
            }
            catch
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<IPresetResource> MigrateToVersionAsync(Version targetVersion, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(targetVersion);

        if (targetVersion <= _presetVersion)
        {
            return Task.FromResult<IPresetResource>(this);
        }

        // Create a new preset with the target version
        var migrated = new PresetResource(_presetType, _presetName, RequestedApiVersion)
        {
            _presetVersion = targetVersion,
            ParentPreset = _parentPreset
        };

        // Copy all data
        foreach (var kvp in _data)
        {
            migrated._data[kvp.Key] = kvp.Value;
        }

        // Perform version-specific migrations if needed
        // (This would be extended based on specific migration requirements)

        return Task.FromResult<IPresetResource>(migrated);
    }

    #endregion

    #region IResource Implementation

    /// <inheritdoc />
    public int RequestedApiVersion { get; }

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return new MemoryStream(ToByteArray());
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ToByteArray();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _contentFields.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return index switch
            {
                0 => TypedValue.Create(PresetType),
                1 => TypedValue.Create(PresetName),
                2 => TypedValue.Create(PresetVersion.ToString()),
                3 => TypedValue.Create(_data.Count.ToString()),
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            switch (index)
            {
                case 0:
                    _presetType = value.GetValue<string>() ?? string.Empty;
                    break;
                case 1:
                    _presetName = value.GetValue<string>() ?? string.Empty;
                    break;
                case 2:
                    if (Version.TryParse(value.GetValue<string>(), out var version))
                        _presetVersion = version;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return name switch
            {
                "PresetType" => TypedValue.Create(PresetType),
                "PresetName" => TypedValue.Create(PresetName),
                "PresetVersion" => TypedValue.Create(PresetVersion.ToString()),
                "Data" => TypedValue.Create(_data.Count.ToString()),
                _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
            };
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            switch (name)
            {
                case "PresetType":
                    _presetType = value.GetValue<string>() ?? string.Empty;
                    break;
                case "PresetName":
                    _presetName = value.GetValue<string>() ?? string.Empty;
                    break;
                case "PresetVersion":
                    if (Version.TryParse(value.GetValue<string>(), out var version))
                        _presetVersion = version;
                    break;
                default:
                    throw new ArgumentException($"Unknown field: {name}", nameof(name));
            }
            OnResourceChanged();
        }
    }

    /// <summary>
    /// Raises the ResourceChanged event.
    /// </summary>
    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Binary Serialization

    private void ParseFromBinary(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16) // Minimum size for header
        {
            throw new ArgumentException("Data too short for preset resource", nameof(data));
        }

        using var reader = new BinaryReader(new MemoryStream(data.ToArray()));

        // Read header
        var version = new Version(reader.ReadInt32(), reader.ReadInt32());
        _presetVersion = version;

        // Read preset type
        var presetTypeLength = reader.ReadInt32();
        var presetTypeBytes = reader.ReadBytes(presetTypeLength);
        _presetType = System.Text.Encoding.UTF8.GetString(presetTypeBytes);

        // Read preset name
        var presetNameLength = reader.ReadInt32();
        var presetNameBytes = reader.ReadBytes(presetNameLength);
        _presetName = System.Text.Encoding.UTF8.GetString(presetNameBytes);

        // Read data count
        var dataCount = reader.ReadInt32();

        // Read data entries
        _data.Clear();
        for (int i = 0; i < dataCount; i++)
        {
            // Read key
            var keyLength = reader.ReadInt32();
            var keyBytes = reader.ReadBytes(keyLength);
            var key = System.Text.Encoding.UTF8.GetString(keyBytes);

            // Read value type and value
            var valueType = reader.ReadByte();
            object value = valueType switch
            {
                1 => reader.ReadString(),
                2 => reader.ReadInt32(),
                3 => reader.ReadDouble(),
                4 => reader.ReadBoolean(),
                5 => reader.ReadBytes(reader.ReadInt32()), // byte array
                _ => throw new InvalidDataException($"Unknown value type: {valueType}")
            };

            _data[key] = value;
        }
    }

    private byte[] ToByteArray()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(_presetVersion.Major);
        writer.Write(_presetVersion.Minor);

        // Write preset type
        var presetTypeBytes = System.Text.Encoding.UTF8.GetBytes(_presetType);
        writer.Write(presetTypeBytes.Length);
        writer.Write(presetTypeBytes);

        // Write preset name
        var presetNameBytes = System.Text.Encoding.UTF8.GetBytes(_presetName);
        writer.Write(presetNameBytes.Length);
        writer.Write(presetNameBytes);

        // Write data count
        writer.Write(_data.Count);

        // Write data entries
        foreach (var kvp in _data)
        {
            // Write key
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(kvp.Key);
            writer.Write(keyBytes.Length);
            writer.Write(keyBytes);

            // Write value type and value
            switch (kvp.Value)
            {
                case string str:
                    writer.Write((byte)1);
                    writer.Write(str);
                    break;
                case int intVal:
                    writer.Write((byte)2);
                    writer.Write(intVal);
                    break;
                case double doubleVal:
                    writer.Write((byte)3);
                    writer.Write(doubleVal);
                    break;
                case bool boolVal:
                    writer.Write((byte)4);
                    writer.Write(boolVal);
                    break;
                case byte[] byteArray:
                    writer.Write((byte)5);
                    writer.Write(byteArray.Length);
                    writer.Write(byteArray);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported value type: {kvp.Value.GetType()}");
            }
        }

        return stream.ToArray();
    }

    #endregion

    #region Helper Methods

    private bool IsAncestor(IPresetResource preset)
    {
        var current = this;
        while (current != null)
        {
            if (ReferenceEquals(current, preset))
                return true;
            current = current.ParentPreset as PresetResource;
        }
        return false;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes of the resources used by this PresetResource.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _data.Clear();
        _parentPreset = null;
        _disposed = true;
    }

    #endregion

    #region ToString

    /// <inheritdoc />
    public override string ToString()
    {
        if (_disposed)
        {
            return "PresetResource (Disposed)";
        }

        return $"PresetResource (Type: {PresetType}, Name: {PresetName}, Version: {PresetVersion}, " +
               $"Data: {_data.Count:N0} items, Parent: {(_parentPreset != null ? "Yes" : "No")})";
    }

    #endregion
}
