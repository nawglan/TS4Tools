using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Utility;

/// <summary>
/// Represents a Sims 4 Metadata Resource - stores asset metadata and descriptive information
/// </summary>
public sealed class MetadataResource : IResource, IDisposable
{
    private readonly ILogger<MetadataResource> _logger;
    private readonly Dictionary<string, MetadataEntry> _metadata = new(StringComparer.OrdinalIgnoreCase);
    private MemoryStream? _stream;
    private bool _disposed;
    private const int ApiVersionValue = 1;

    // Metadata storage
    // private byte[]? _rawData;
    private bool _isJsonFormat;

    // Events
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Initializes a new instance of the MetadataResource class
    /// </summary>
    public MetadataResource(ILogger<MetadataResource> logger, ResourceKey key, Stream? stream = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ResourceKey = key ?? throw new ArgumentNullException(nameof(key));
        _stream = new MemoryStream();

        if (stream != null && stream.Length > 0)
        {
            ParseFromStream(stream);
        }
    }

    /// <summary>
    /// Gets the resource key associated with this resource
    /// </summary>
    public ResourceKey ResourceKey { get; }

    // IApiVersion Implementation
    public int RequestedApiVersion => ApiVersionValue;
    public int RecommendedApiVersion => ApiVersionValue;

    // IContentFields Implementation
    public IReadOnlyList<string> ContentFields => new List<string>
    {
        nameof(IsJsonFormat),
        nameof(MetadataCount),
        nameof(MetadataKeys),
        nameof(AssetName),
        nameof(AssetDescription),
        nameof(CreatedDate),
        nameof(ModifiedDate)
    }.AsReadOnly();

    public TypedValue this[int index]
    {
        get => GetFieldByIndex(index);
        set => SetFieldByIndex(index, value);
    }

    public TypedValue this[string name]
    {
        get => GetFieldByName(name);
        set => SetFieldByName(name, value);
    }

    // IResource Implementation
    public Stream Stream
    {
        get
        {
            if (_stream == null)
                _stream = new MemoryStream();
            return _stream;
        }
    }

    public byte[] AsBytes
    {
        get
        {
            Stream.Position = 0;
            var buffer = new byte[Stream.Length];
            Stream.ReadExactly(buffer);
            return buffer;
        }
    }

    private TypedValue GetFieldByIndex(int index)
    {
        if (index < 0 || index >= ContentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return GetFieldByName(ContentFields[index]);
    }

    private void SetFieldByIndex(int index, TypedValue value)
    {
        if (index < 0 || index >= ContentFields.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        SetFieldByName(ContentFields[index], value);
    }

    private TypedValue GetFieldByName(string name)
    {
        return name switch
        {
            nameof(IsJsonFormat) => new TypedValue(typeof(bool), IsJsonFormat),
            nameof(MetadataCount) => new TypedValue(typeof(int), MetadataCount),
            nameof(MetadataKeys) => new TypedValue(typeof(string), string.Join(", ", MetadataKeys)),
            nameof(AssetName) => new TypedValue(typeof(string), AssetName),
            nameof(AssetDescription) => new TypedValue(typeof(string), AssetDescription),
            nameof(CreatedDate) => new TypedValue(typeof(string), CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")),
            nameof(ModifiedDate) => new TypedValue(typeof(string), ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss")),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        switch (name)
        {
            case nameof(AssetName):
                AssetName = value.Value as string ?? string.Empty;
                break;
            case nameof(AssetDescription):
                AssetDescription = value.Value as string ?? string.Empty;
                break;
            case nameof(CreatedDate):
                if (DateTime.TryParse(value.Value as string ?? string.Empty, out var createdDate))
                    CreatedDate = createdDate;
                break;
            case nameof(ModifiedDate):
                if (DateTime.TryParse(value.Value as string ?? string.Empty, out var modifiedDate))
                    ModifiedDate = modifiedDate;
                break;
            // Note: Other fields are read-only computed properties
            default:
                throw new ArgumentException($"Unknown or read-only field: {name}", nameof(name));
        }
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    // Properties
    /// <summary>
    /// Indicates if this metadata is stored in JSON format
    /// </summary>
    public bool IsJsonFormat => _isJsonFormat;

    /// <summary>
    /// Number of metadata entries
    /// </summary>
    public int MetadataCount => _metadata.Count;

    /// <summary>
    /// Keys of all metadata entries
    /// </summary>
    public IEnumerable<string> MetadataKeys => _metadata.Keys;

    /// <summary>
    /// API version used by this resource
    /// </summary>
    public int ApiVersion { get; set; } = ApiVersionValue;

    // Common Metadata Properties
    /// <summary>
    /// Asset name
    /// </summary>
    public string AssetName
    {
        get => GetMetadataValue("AssetName") ?? string.Empty;
        set => SetMetadata("AssetName", value, MetadataType.String, "Name of the asset");
    }

    /// <summary>
    /// Asset description
    /// </summary>
    public string AssetDescription
    {
        get => GetMetadataValue("Description") ?? string.Empty;
        set => SetMetadata("Description", value, MetadataType.String, "Description of the asset");
    }

    /// <summary>
    /// Asset creation date
    /// </summary>
    public DateTime CreatedDate
    {
        get => DateTime.TryParse(GetMetadataValue("CreatedDate"), out var date) ? date : DateTime.MinValue;
        set => SetMetadata("CreatedDate", value.ToString("yyyy-MM-dd HH:mm:ss"), MetadataType.DateTime, "Asset creation date");
    }

    /// <summary>
    /// Asset modification date
    /// </summary>
    public DateTime ModifiedDate
    {
        get => DateTime.TryParse(GetMetadataValue("ModifiedDate"), out var date) ? date : DateTime.MinValue;
        set => SetMetadata("ModifiedDate", value.ToString("yyyy-MM-dd HH:mm:ss"), MetadataType.DateTime, "Asset modification date");
    }

    /// <summary>
    /// Asset version
    /// </summary>
    public string Version
    {
        get => GetMetadataValue("Version") ?? string.Empty;
        set => SetMetadata("Version", value, MetadataType.String, "Asset version");
    }

    /// <summary>
    /// Asset author
    /// </summary>
    public string Author
    {
        get => GetMetadataValue("Author") ?? string.Empty;
        set => SetMetadata("Author", value, MetadataType.String, "Asset author");
    }

    /// <summary>
    /// Asset tags
    /// </summary>
    public IEnumerable<string> Tags
    {
        get
        {
            var tagString = GetMetadataValue("Tags") ?? string.Empty;
            return tagString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.Trim())
                           .Where(t => !string.IsNullOrEmpty(t));
        }
        set => SetMetadata("Tags", string.Join(", ", value), MetadataType.String, "Asset tags");
    }

    // Metadata Management Methods
    /// <summary>
    /// Sets a metadata entry
    /// </summary>
    public void SetMetadata(string key, string value, MetadataType type = MetadataType.String, string description = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var entry = new MetadataEntry
        {
            Key = key,
            Value = value,
            Type = type,
            Description = description,
            LastModified = DateTime.UtcNow
        };

        var changed = !_metadata.TryGetValue(key, out var existing) || !existing.Equals(entry);
        _metadata[key] = entry;

        if (changed)
            OnResourceChanged();
    }

    /// <summary>
    /// Gets a metadata entry by key
    /// </summary>
    public MetadataEntry? GetMetadata(string key)
    {
        return _metadata.TryGetValue(key, out var entry) ? entry : null;
    }

    /// <summary>
    /// Gets a metadata value by key
    /// </summary>
    public string? GetMetadataValue(string key)
    {
        return _metadata.TryGetValue(key, out var entry) ? entry.Value : null;
    }

    /// <summary>
    /// Checks if a metadata key exists
    /// </summary>
    public bool HasMetadata(string key) => _metadata.ContainsKey(key);

    /// <summary>
    /// Removes a metadata entry
    /// </summary>
    public bool RemoveMetadata(string key)
    {
        if (_metadata.Remove(key))
        {
            OnResourceChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all metadata
    /// </summary>
    public void ClearMetadata()
    {
        if (_metadata.Count > 0)
        {
            _metadata.Clear();
            OnResourceChanged();
        }
    }

    /// <summary>
    /// Gets metadata entries by type
    /// </summary>
    public IEnumerable<MetadataEntry> GetMetadataByType(MetadataType type)
    {
        return _metadata.Values.Where(m => m.Type == type);
    }

    /// <summary>
    /// Gets metadata entries modified after a specific date
    /// </summary>
    public IEnumerable<MetadataEntry> GetMetadataModifiedAfter(DateTime date)
    {
        return _metadata.Values.Where(m => m.LastModified > date);
    }

    // Parsing and Serialization
    private void ParseFromStream(Stream stream)
    {
        try
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, leaveOpen: true);
            var content = reader.ReadToEnd();

            // Try parsing as JSON first
            if (TryParseAsJson(content))
            {
                _isJsonFormat = true;
                return;
            }

            // Fall back to key=value format
            ParseAsKeyValue(content);
            _isJsonFormat = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse metadata stream");
            throw;
        }
    }

    private bool TryParseAsJson(string content)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(content);
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                var entry = ParseJsonMetadataEntry(property);
                _metadata[entry.Key] = entry;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private MetadataEntry ParseJsonMetadataEntry(JsonProperty property)
    {
        var entry = new MetadataEntry { Key = property.Name };

        if (property.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var subProp in property.Value.EnumerateObject())
            {
                switch (subProp.Name.ToLowerInvariant())
                {
                    case "value":
                        entry.Value = subProp.Value.ToString();
                        break;
                    case "type":
                        if (Enum.TryParse<MetadataType>(subProp.Value.GetString(), out var type))
                            entry.Type = type;
                        break;
                    case "description":
                        entry.Description = subProp.Value.GetString() ?? string.Empty;
                        break;
                }
            }
        }
        else
        {
            entry.Value = property.Value.ToString();
            entry.Type = MetadataType.String;
        }

        return entry;
    }

    private void ParseAsKeyValue(string content)
    {
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            var equalIndex = line.IndexOf('=');
            if (equalIndex > 0 && equalIndex < line.Length - 1)
            {
                var key = line[..equalIndex].Trim();
                var value = line[(equalIndex + 1)..].Trim();
                SetMetadata(key, value, MetadataType.String);
            }
        }
    }

    /// <summary>
    /// Serializes the metadata to a stream
    /// </summary>
    public async Task<Stream> SerializeAsync()
    {
        var stream = new MemoryStream();

        if (_isJsonFormat)
        {
            await SerializeAsJsonAsync(stream);
        }
        else
        {
            await SerializeAsKeyValueAsync(stream);
        }

        stream.Position = 0;
        return stream;
    }

    private async Task SerializeAsJsonAsync(Stream stream)
    {
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();

        foreach (var kvp in _metadata)
        {
            writer.WritePropertyName(kvp.Key);
            writer.WriteStartObject();
            writer.WriteString("value", kvp.Value.Value);
            writer.WriteString("type", kvp.Value.Type.ToString());
            if (!string.IsNullOrEmpty(kvp.Value.Description))
                writer.WriteString("description", kvp.Value.Description);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        await writer.FlushAsync();
    }

    private async Task SerializeAsKeyValueAsync(Stream stream)
    {
        using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteLineAsync("# Metadata file generated by TS4Tools");
        await writer.WriteLineAsync();

        foreach (var kvp in _metadata)
        {
            if (!string.IsNullOrEmpty(kvp.Value.Description))
                await writer.WriteLineAsync($"# {kvp.Value.Description}");
            await writer.WriteLineAsync($"{kvp.Key}={kvp.Value.Value}");
        }

        await writer.FlushAsync();
    }

    /// <summary>
    /// Creates a copy of this metadata resource
    /// </summary>
    public IResource Clone()
    {
        var clone = new MetadataResource(_logger, ResourceKey);
        foreach (var kvp in _metadata)
        {
            clone._metadata[kvp.Key] = new MetadataEntry
            {
                Key = kvp.Value.Key,
                Value = kvp.Value.Value,
                Type = kvp.Value.Type,
                Description = kvp.Value.Description,
                LastModified = kvp.Value.LastModified
            };
        }
        clone._isJsonFormat = _isJsonFormat;
        return clone;
    }

    /// <summary>
    /// Validates this metadata resource
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        // Basic validation
        foreach (var kvp in _metadata)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
                errors.Add("Metadata key cannot be empty");
        }

        return errors.Count == 0;
    }

    public override string ToString()
    {
        return $"MetadataResource: {_metadata.Count} entries";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _stream?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Represents a metadata entry
/// </summary>
public class MetadataEntry : IEquatable<MetadataEntry>
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public MetadataType Type { get; set; } = MetadataType.String;
    public string Description { get; set; } = string.Empty;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public bool Equals(MetadataEntry? other)
    {
        if (other is null) return false;
        return Key == other.Key && Value == other.Value && Type == other.Type && Description == other.Description;
    }

    public override bool Equals(object? obj) => Equals(obj as MetadataEntry);

    public override int GetHashCode() => HashCode.Combine(Key, Value, Type, Description);

    public override string ToString() => $"{Key}: {Value} ({Type})";
}

/// <summary>
/// Types of metadata values
/// </summary>
public enum MetadataType
{
    String,
    Integer,
    Float,
    Boolean,
    DateTime,
    Url,
    Json,
    Binary
}
