using Microsoft.Extensions.Logging;
using System.Text.Json;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Utility;

/// <summary>
/// Represents a Sims 4 Configuration Resource - stores configuration data and settings
/// </summary>
public sealed class ConfigResource : IResource, IDisposable
{
    private readonly ILogger<ConfigResource> _logger;
    private readonly Dictionary<string, object> _configData = new(StringComparer.OrdinalIgnoreCase);
    private MemoryStream? _stream;
    private bool _disposed;
    private const int ApiVersionValue = 1;

    // Configuration data storage
    // private byte[]? _rawData;
    private bool _isJsonFormat;

    // Events
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Initializes a new instance of the ConfigResource class
    /// </summary>
    public ConfigResource(ILogger<ConfigResource> logger, ResourceKey key, Stream? stream = null)
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
        nameof(ConfigCount),
        nameof(ConfigKeys)
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
            nameof(ConfigCount) => new TypedValue(typeof(int), ConfigCount),
            nameof(ConfigKeys) => new TypedValue(typeof(string), string.Join(", ", ConfigKeys)),
            _ => throw new ArgumentException($"Unknown field: {name}", nameof(name))
        };
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        switch (name)
        {
            // Note: All fields are read-only computed properties
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
    /// Indicates if this configuration is stored in JSON format
    /// </summary>
    public bool IsJsonFormat => _isJsonFormat;

    /// <summary>
    /// Number of configuration entries
    /// </summary>
    public int ConfigCount => _configData.Count;

    /// <summary>
    /// Keys of all configuration entries
    /// </summary>
    public IEnumerable<string> ConfigKeys => _configData.Keys;

    // Configuration Management Methods
    /// <summary>
    /// Gets a configuration value by key
    /// </summary>
    public T? GetConfigValue<T>(string key)
    {
        // Handle nested property access with dot notation (e.g., "nested.innerValue")
        if (key.Contains('.'))
        {
            return GetNestedValue<T>(key);
        }

        if (_configData.TryGetValue(key, out var value))
        {
            if (value is T directValue)
                return directValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// Gets a nested configuration value using dot notation
    /// </summary>
    private T? GetNestedValue<T>(string key)
    {
        var parts = key.Split('.');
        object current = _configData;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict && dict.TryGetValue(part, out var nextValue))
            {
                current = nextValue;
            }
            else
            {
                return default;
            }
        }

        if (current is T directValue)
            return directValue;

        try
        {
            return (T)Convert.ChangeType(current, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Sets a configuration value by key
    /// </summary>
    public void SetConfigValue<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var oldValue = _configData.TryGetValue(key, out var existing) ? existing : null;
        _configData[key] = value ?? throw new ArgumentNullException(nameof(value));

        if (!Equals(oldValue, value))
        {
            OnResourceChanged();
        }
    }

    /// <summary>
    /// Checks if a configuration key exists
    /// </summary>
    public bool HasConfigKey(string key) => _configData.ContainsKey(key);

    /// <summary>
    /// Removes a configuration key
    /// </summary>
    public bool RemoveConfigKey(string key)
    {
        if (_configData.Remove(key))
        {
            OnResourceChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all configuration data
    /// </summary>
    public void ClearConfig()
    {
        if (_configData.Count > 0)
        {
            _configData.Clear();
            OnResourceChanged();
        }
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
            _logger.LogError(ex, "Failed to parse configuration stream");
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
                _configData[property.Name] = ExtractJsonValue(property.Value);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private object ExtractJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractJsonValue).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ExtractJsonValue(p.Value)),
            _ => element.ToString()
        };
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

                // Remove surrounding quotes if present
                if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
                {
                    value = value[1..^1];
                }

                _configData[key] = value;
            }
        }
    }

    /// <summary>
    /// Serializes the configuration to a stream
    /// </summary>
    public async Task<Stream> SerializeAsync()
    {
        var stream = new MemoryStream();

        // If resource is empty, return empty stream
        if (_configData.Count == 0)
        {
            return stream;
        }

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

        foreach (var kvp in _configData)
        {
            writer.WritePropertyName(kvp.Key);
            WriteJsonValue(writer, kvp.Value);
        }

        writer.WriteEndObject();
        await writer.FlushAsync();
    }

    private void WriteJsonValue(Utf8JsonWriter writer, object value)
    {
        switch (value)
        {
            case string s:
                writer.WriteStringValue(s);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case Array arr:
                writer.WriteStartArray();
                foreach (var item in arr)
                    WriteJsonValue(writer, item);
                writer.WriteEndArray();
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    private async Task SerializeAsKeyValueAsync(Stream stream)
    {
        using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteLineAsync("# Configuration file generated by TS4Tools");
        await writer.WriteLineAsync();

        foreach (var kvp in _configData)
        {
            await writer.WriteLineAsync($"{kvp.Key}={kvp.Value}");
        }

        await writer.FlushAsync();
    }

    /// <summary>
    /// Validates this configuration resource
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        // Configuration resources are generally always valid
        // Add specific validation rules here if needed

        return errors.Count == 0;
    }

    public override string ToString()
    {
        return $"ConfigResource: {_configData.Count} entries ({(_isJsonFormat ? "JSON" : "Key=Value")} format)";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _stream?.Dispose();
        _disposed = true;
    }
}
