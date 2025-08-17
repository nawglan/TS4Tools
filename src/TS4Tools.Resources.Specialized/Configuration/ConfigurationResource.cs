using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Resources.Specialized.Configuration;

namespace TS4Tools.Resources.Specialized.Configuration;

/// <summary>
/// Phase 4.19 P3 MEDIUM - Configuration Resource Implementation.
/// Provides hierarchical configuration management with sections, inheritance, and validation.
/// Implements advanced configuration settings and overrides for specialized modding scenarios.
/// </summary>
public sealed class ConfigurationResource : IConfigurationResource, IDisposable
{
    private readonly ILogger<ConfigurationResource>? _logger;
    private bool _disposed;

    // Configuration properties
    private string _configurationName = string.Empty;
    private string _configurationVersion = "1.0";
    private string _configurationCategory = "general";
    private uint? _parentConfigurationId;
    private bool _isValidated;
    private bool _isInheritanceResolved;

    // Configuration data
    private readonly Dictionary<string, ConfigurationSection> _sections = new();
    private readonly Dictionary<string, object> _configurationValues = new();
    private readonly Dictionary<string, ConfigurationSchema> _schemas = new();
    private byte[] _rawConfigurationData = Array.Empty<byte>();

    // IResource properties
    private Stream? _stream;
    private int _apiVersion = 1;

    /// <summary>
    /// Initializes a new instance of the ConfigurationResource class.
    /// </summary>
    /// <param name="requestedApiVersion">The requested API version</param>
    public ConfigurationResource(int requestedApiVersion = 1)
    {
        _apiVersion = requestedApiVersion;
    }

    internal ConfigurationResource(ILogger<ConfigurationResource> logger, int requestedApiVersion = 1)
    {
        _logger = logger;
        _apiVersion = requestedApiVersion;
    }

    #region IConfigurationResource Implementation

    /// <summary>
    /// Gets the configuration name/identifier.
    /// </summary>
    public string ConfigurationName
    {
        get => _configurationName;
        private set => _configurationName = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the configuration version.
    /// </summary>
    public string ConfigurationVersion
    {
        get => _configurationVersion;
        private set => _configurationVersion = value ?? "1.0";
    }

    /// <summary>
    /// Gets the configuration category/namespace.
    /// </summary>
    public string ConfigurationCategory
    {
        get => _configurationCategory;
        private set => _configurationCategory = value ?? "general";
    }

    /// <summary>
    /// Gets the parent configuration ID for inheritance.
    /// </summary>
    public uint? ParentConfigurationId
    {
        get => _parentConfigurationId;
        private set => _parentConfigurationId = value;
    }

    /// <summary>
    /// Gets whether the configuration has been validated.
    /// </summary>
    public bool IsValidated
    {
        get => _isValidated;
        private set => _isValidated = value;
    }

    /// <summary>
    /// Gets whether inheritance has been resolved.
    /// </summary>
    public bool IsInheritanceResolved
    {
        get => _isInheritanceResolved;
        private set => _isInheritanceResolved = value;
    }

    /// <summary>
    /// Gets the configuration sections.
    /// </summary>
    public IReadOnlyDictionary<string, ConfigurationSection> Sections => _sections;

    /// <summary>
    /// Gets the configuration values.
    /// </summary>
    public IReadOnlyDictionary<string, object> ConfigurationValues => _configurationValues;

    /// <summary>
    /// Gets the raw configuration data.
    /// </summary>
    public IReadOnlyDictionary<string, object> RawConfigurationData => _configurationValues;

    /// <summary>
    /// Gets the configuration schemas.
    /// </summary>
    public IReadOnlyDictionary<string, ConfigurationSchema> Schemas => _schemas;

    /// <summary>
    /// Validates the configuration asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors and warnings</returns>
    public Task<ConfigurationValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Validating configuration {Name} version {Version}", ConfigurationName, ConfigurationVersion);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate configuration name
        if (string.IsNullOrWhiteSpace(ConfigurationName))
        {
            errors.Add("Configuration name cannot be empty");
        }

        // Validate sections against schemas
        foreach (var (sectionName, section) in _sections)
        {
            if (_schemas.TryGetValue(sectionName, out var schema))
            {
                var sectionValidation = ValidateSection(section, schema);
                errors.AddRange(sectionValidation.Errors);
                warnings.AddRange(sectionValidation.Warnings);
            }
            else
            {
                warnings.Add($"No schema found for section '{sectionName}'");
            }
        }

        var result = ConfigurationValidationResult.Success();
        if (errors.Count > 0)
        {
            result = ConfigurationValidationResult.Failure(errors.ToArray());
        }
        else if (warnings.Count > 0)
        {
            result = ConfigurationValidationResult.WithWarnings(warnings.ToArray());
        }

        _isValidated = errors.Count == 0;

        _logger?.LogDebug("Configuration validation completed: {ErrorCount} errors, {WarningCount} warnings",
            errors.Count, warnings.Count);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Resolves inheritance from parent configurations.
    /// </summary>
    /// <param name="resolver">Configuration resolver</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    public async Task ResolveInheritanceAsync(IConfigurationResolver resolver, CancellationToken cancellationToken = default)
    {
        if (ParentConfigurationId == null)
        {
            _isInheritanceResolved = true;
            return;
        }

        _logger?.LogDebug("Resolving inheritance for configuration {Name} from parent {ParentId}",
            ConfigurationName, ParentConfigurationId);

        var parent = await resolver.ResolveConfigurationAsync(ParentConfigurationId.Value, cancellationToken);
        if (parent != null)
        {
            // Merge parent configuration values (child overrides parent)
            foreach (string key in parent.ConfigurationValues.Keys)
            {
                var value = parent.ConfigurationValues[key];
                if (!_configurationValues.ContainsKey(key))
                {
                    _configurationValues[key] = value;
                }
            }

            // Merge parent sections
            foreach (string sectionName in parent.Sections.Keys)
            {
                var parentSection = parent.Sections[sectionName];
                if (!_sections.ContainsKey(sectionName))
                {
                    _sections[sectionName] = parentSection;
                }
                else
                {
                    // Merge section values
                    var childSection = _sections[sectionName];
                    foreach (string key in parentSection.Values.Keys)
                    {
                        var value = parentSection.Values[key];
                        if (!childSection.Values.ContainsKey(key))
                        {
                            childSection.Values[key] = value;
                        }
                    }
                }
            }
        }

        _isInheritanceResolved = true;
        _logger?.LogDebug("Inheritance resolution completed for configuration {Name}", ConfigurationName);
    }

    /// <summary>
    /// Gets configuration dependencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of configuration dependencies</returns>
    public Task<IReadOnlyCollection<uint>> GetDependenciesAsync(CancellationToken cancellationToken = default)
    {
        var dependencies = new List<uint>();

        if (ParentConfigurationId.HasValue)
        {
            dependencies.Add(ParentConfigurationId.Value);
        }

        // Add any other dependencies found in configuration values
        foreach (var value in _configurationValues.Values)
        {
            if (value is uint configId && configId != 0)
            {
                dependencies.Add(configId);
            }
        }

        return Task.FromResult<IReadOnlyCollection<uint>>(dependencies);
    }

    /// <summary>
    /// Gets a configuration value with type conversion.
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Configuration value or default</returns>
    public T GetValue<T>(string key, T defaultValue = default!) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(key))
            return defaultValue;

        if (!_configurationValues.TryGetValue(key, out var value))
            return defaultValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to convert configuration value {Key} to type {Type}", key, typeof(T));
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    public Task SetValueAsync(string key, object value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        _configurationValues[key] = value;
        OnResourceChanged();

        _logger?.LogDebug("Set configuration value {Key} = {Value}", key, value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a configuration value.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    public Task RemoveValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.CompletedTask;

        var removed = _configurationValues.Remove(key);
        if (removed)
        {
            OnResourceChanged();
            _logger?.LogDebug("Removed configuration value {Key}", key);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets a configuration section.
    /// </summary>
    /// <param name="sectionName">Section name</param>
    /// <param name="section">Configuration section</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    public Task SetSectionAsync(string sectionName, ConfigurationSection section, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
            throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

        ArgumentNullException.ThrowIfNull(section);

        _sections[sectionName] = section;
        OnResourceChanged();

        _logger?.LogDebug("Set configuration section {SectionName}", sectionName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a configuration section.
    /// </summary>
    /// <param name="sectionName">Section name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    public Task RemoveSectionAsync(string sectionName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
            return Task.CompletedTask;

        var removed = _sections.Remove(sectionName);
        if (removed)
        {
            OnResourceChanged();
            _logger?.LogDebug("Removed configuration section {SectionName}", sectionName);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets a configuration schema.
    /// </summary>
    /// <param name="schemaName">Schema name</param>
    /// <param name="schema">Configuration schema</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    public Task SetSchemaAsync(string schemaName, ConfigurationSchema schema, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("Schema name cannot be null or empty", nameof(schemaName));

        ArgumentNullException.ThrowIfNull(schema);

        _schemas[schemaName] = schema;
        _logger?.LogDebug("Set configuration schema {SchemaName}", schemaName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates configuration data in bulk.
    /// </summary>
    /// <param name="configurationData">Configuration data to merge</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    public Task UpdateConfigurationDataAsync(IDictionary<string, object> configurationData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configurationData);

        foreach (var kvp in configurationData)
        {
            _configurationValues[kvp.Key] = kvp.Value;
        }

        OnResourceChanged();
        _logger?.LogDebug("Updated configuration data with {Count} values", configurationData.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets configuration values by section.
    /// </summary>
    /// <param name="sectionName">Section name</param>
    /// <returns>Configuration values in the section</returns>
    public IReadOnlyDictionary<string, object> GetSection(string sectionName)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
            return new Dictionary<string, object>();

        return _sections.TryGetValue(sectionName, out var section)
            ? section.Values
            : new Dictionary<string, object>();
    }

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    /// <param name="key">Configuration key to check</param>
    /// <returns>True if key exists, false otherwise</returns>
    public bool HasValue(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && _configurationValues.ContainsKey(key);
    }

    #endregion

    #region IResource Implementation

    /// <summary>
    /// Occurs when the resource is changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Gets the requested API version.
    /// </summary>
    public int RequestedApiVersion => _apiVersion;

    /// <summary>
    /// Gets the recommended API version.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Gets the content fields for debugging and introspection.
    /// </summary>
    public IReadOnlyList<string> ContentFields => new[]
    {
        nameof(ConfigurationName),
        nameof(ConfigurationVersion),
        nameof(ConfigurationCategory),
        nameof(ParentConfigurationId),
        nameof(IsValidated),
        nameof(IsInheritanceResolved),
        nameof(Sections),
        nameof(ConfigurationValues)
    };

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get => index switch
        {
            nameof(ConfigurationName) => new TypedValue(typeof(string), ConfigurationName),
            nameof(ConfigurationVersion) => new TypedValue(typeof(string), ConfigurationVersion),
            nameof(ConfigurationCategory) => new TypedValue(typeof(string), ConfigurationCategory),
            nameof(ParentConfigurationId) => new TypedValue(typeof(uint), ParentConfigurationId),
            nameof(IsValidated) => new TypedValue(typeof(bool), IsValidated),
            nameof(IsInheritanceResolved) => new TypedValue(typeof(bool), IsInheritanceResolved),
            nameof(Sections) => new TypedValue(typeof(IReadOnlyDictionary<string, ConfigurationSection>), Sections),
            nameof(ConfigurationValues) => new TypedValue(typeof(IReadOnlyDictionary<string, object>), ConfigurationValues),
            _ => throw new ArgumentException($"Unknown field: {index}", nameof(index))
        };
        set => throw new NotSupportedException("Configuration fields are read-only via string indexer");
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => this[nameof(ConfigurationName)],
            1 => this[nameof(ConfigurationVersion)],
            2 => this[nameof(ConfigurationCategory)],
            3 => this[nameof(ParentConfigurationId)],
            4 => this[nameof(IsValidated)],
            5 => this[nameof(IsInheritanceResolved)],
            6 => this[nameof(Sections)],
            7 => this[nameof(ConfigurationValues)],
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index must be 0-7, got {index}")
        };
        set => throw new NotSupportedException("Configuration fields are read-only via integer indexer");
    }

    /// <summary>
    /// Gets or sets the resource stream.
    /// </summary>
    public Stream Stream
    {
        get => _stream ?? new MemoryStream();
        set => _stream = value;
    }

    /// <summary>
    /// Gets the resource as a byte array.
    /// </summary>
    public byte[] AsBytes => SaveToByteArray();

    #endregion

    #region Data Parsing and Serialization

    internal async Task ParseFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var reader = new BinaryReader(stream);

            // Read configuration header
            var nameLength = reader.ReadInt32();
            ConfigurationName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

            var versionLength = reader.ReadInt32();
            ConfigurationVersion = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(versionLength));

            var categoryLength = reader.ReadInt32();
            ConfigurationCategory = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(categoryLength));

            ParentConfigurationId = reader.ReadUInt32();

            // Read configuration values
            var valueCount = reader.ReadInt32();
            for (int i = 0; i < valueCount; i++)
            {
                var keyLength = reader.ReadInt32();
                var key = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(keyLength));

                var valueType = reader.ReadByte();
                var value = ReadConfigurationValue(reader, valueType);

                _configurationValues[key] = value;
            }

            // Read sections
            var sectionCount = reader.ReadInt32();
            for (int i = 0; i < sectionCount; i++)
            {
                var section = await ReadConfigurationSectionAsync(reader, cancellationToken);
                _sections[section.Name] = section;
            }

            _logger?.LogDebug("Parsed configuration {Name} with {ValueCount} values and {SectionCount} sections",
                ConfigurationName, _configurationValues.Count, _sections.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to parse configuration from stream");
            throw;
        }
    }

    private async Task<ConfigurationSection> ReadConfigurationSectionAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        var nameLength = reader.ReadInt32();
        var name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

        var valueCount = reader.ReadInt32();
        var values = new Dictionary<string, object>();

        for (int i = 0; i < valueCount; i++)
        {
            var keyLength = reader.ReadInt32();
            var key = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(keyLength));

            var valueType = reader.ReadByte();
            var value = ReadConfigurationValue(reader, valueType);

            values[key] = value;
        }

        await Task.CompletedTask; // For async pattern
        return new ConfigurationSection
        {
            Name = name,
            Values = values
        };
    }

    private object ReadConfigurationValue(BinaryReader reader, byte valueType)
    {
        switch (valueType)
        {
            case 0:
                return reader.ReadInt32();
            case 1:
                return reader.ReadSingle();
            case 2:
                return reader.ReadBoolean();
            case 3:
                var length = reader.ReadInt32();
                return System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));
            default:
                throw new InvalidDataException($"Unknown value type: {valueType}");
        }
    }

    internal async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);

            // Write configuration header
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(ConfigurationName);
            writer.Write(nameBytes.Length);
            writer.Write(nameBytes);

            var versionBytes = System.Text.Encoding.UTF8.GetBytes(ConfigurationVersion);
            writer.Write(versionBytes.Length);
            writer.Write(versionBytes);

            var categoryBytes = System.Text.Encoding.UTF8.GetBytes(ConfigurationCategory);
            writer.Write(categoryBytes.Length);
            writer.Write(categoryBytes);

            // Serialize parent configuration ID
            writer.Write(ParentConfigurationId.HasValue);
            if (ParentConfigurationId.HasValue)
            {
                writer.Write(ParentConfigurationId.Value);
            }

            // Write configuration values
            writer.Write(_configurationValues.Count);
            foreach (var (key, value) in _configurationValues)
            {
                var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
                writer.Write(keyBytes.Length);
                writer.Write(keyBytes);

                WriteConfigurationValue(writer, value);
            }

            // Write sections
            writer.Write(_sections.Count);
            foreach (var section in _sections.Values)
            {
                await WriteConfigurationSectionAsync(writer, section, cancellationToken);
            }

            _logger?.LogDebug("Saved configuration {Name} to stream", ConfigurationName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save configuration to stream");
            throw;
        }
    }

    private async Task WriteConfigurationSectionAsync(BinaryWriter writer, ConfigurationSection section, CancellationToken cancellationToken)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(section.Name);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);

        writer.Write(section.Values.Count);
        foreach (var (key, value) in section.Values)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            writer.Write(keyBytes.Length);
            writer.Write(keyBytes);

            WriteConfigurationValue(writer, value);
        }

        await Task.CompletedTask; // For async pattern
    }

    private void WriteConfigurationValue(BinaryWriter writer, object value)
    {
        switch (value)
        {
            case int intValue:
                writer.Write((byte)0);
                writer.Write(intValue);
                break;
            case float floatValue:
                writer.Write((byte)1);
                writer.Write(floatValue);
                break;
            case bool boolValue:
                writer.Write((byte)2);
                writer.Write(boolValue);
                break;
            case string stringValue:
                writer.Write((byte)3);
                var stringBytes = System.Text.Encoding.UTF8.GetBytes(stringValue);
                writer.Write(stringBytes.Length);
                writer.Write(stringBytes);
                break;
            default:
                throw new InvalidOperationException($"Unsupported value type: {value.GetType()}");
        }
    }

    private byte[] SaveToByteArray()
    {
        using var stream = new MemoryStream();
        SaveToStreamAsync(stream).GetAwaiter().GetResult();
        return stream.ToArray();
    }

    #endregion

    #region Helper Methods

    private ConfigurationValidationResult ValidateSection(ConfigurationSection section, ConfigurationSchema schema)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate required properties
        foreach (var requiredProperty in schema.RequiredProperties)
        {
            if (!section.Values.ContainsKey(requiredProperty))
            {
                errors.Add($"Required property '{requiredProperty}' missing in section '{section.Name}'");
            }
        }

        // Validate property types using schema properties
        foreach (var (key, value) in section.Values)
        {
            if (schema.Properties.TryGetValue(key, out var propertyDef))
            {
                if (!propertyDef.PropertyType.IsInstanceOfType(value))
                {
                    errors.Add($"Property '{key}' in section '{section.Name}' has incorrect type");
                }
            }
        }

        return errors.Count > 0
            ? ConfigurationValidationResult.Failure(errors.ToArray())
            : warnings.Count > 0
                ? ConfigurationValidationResult.WithWarnings(warnings.ToArray())
                : ConfigurationValidationResult.Success();
    }

    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the configuration resource and releases any managed resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stream?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
