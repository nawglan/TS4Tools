using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Specialized.Configuration
{
    /// <summary>
    /// Interface for configuration resources that manage hierarchical configuration data.
    /// ConfigurationResource provides structured configuration management with sections, inheritance, and validation.
    /// </summary>
    public interface IConfigurationResource : IResource
    {
        /// <summary>
        /// Gets the configuration name/identifier.
        /// </summary>
        string ConfigurationName { get; }

        /// <summary>
        /// Gets the configuration version.
        /// </summary>
        string ConfigurationVersion { get; }

        /// <summary>
        /// Gets the configuration category/namespace.
        /// </summary>
        string ConfigurationCategory { get; }

        /// <summary>
        /// Gets the parent configuration ID if this configuration inherits from another.
        /// </summary>
        uint? ParentConfigurationId { get; }

        /// <summary>
        /// Gets whether this configuration has been validated.
        /// </summary>
        bool IsValidated { get; }

        /// <summary>
        /// Gets whether inheritance has been resolved.
        /// </summary>
        bool IsInheritanceResolved { get; }

        /// <summary>
        /// Gets the configuration sections.
        /// </summary>
        IReadOnlyDictionary<string, ConfigurationSection> Sections { get; }

        /// <summary>
        /// Gets all configuration values (flattened view).
        /// </summary>
        IReadOnlyDictionary<string, object> ConfigurationValues { get; }

        /// <summary>
        /// Gets the raw configuration data (before inheritance resolution).
        /// </summary>
        IReadOnlyDictionary<string, object> RawConfigurationData { get; }

        /// <summary>
        /// Gets configuration schema definitions.
        /// </summary>
        IReadOnlyDictionary<string, ConfigurationSchema> Schemas { get; }

        /// <summary>
        /// Validates the configuration data against schemas and constraints.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with errors, warnings, and information.</returns>
        Task<ConfigurationValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves configuration inheritance by merging with parent configurations.
        /// </summary>
        /// <param name="configurationResolver">Configuration resolver for parent lookup.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ResolveInheritanceAsync(
            IConfigurationResolver configurationResolver,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets configuration dependencies (referenced configuration IDs).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of configuration dependency IDs.</returns>
        Task<IReadOnlyCollection<uint>> GetDependenciesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a configuration value with type conversion.
        /// </summary>
        /// <typeparam name="T">Type to convert value to.</typeparam>
        /// <param name="key">Configuration key (supports dot notation for nested values).</param>
        /// <param name="defaultValue">Default value if key not found.</param>
        /// <returns>Configuration value or default.</returns>
        T GetValue<T>(string key, T defaultValue = default!) where T : notnull;

        /// <summary>
        /// Sets a configuration value.
        /// </summary>
        /// <param name="key">Configuration key (supports dot notation for nested values).</param>
        /// <param name="value">Value to set.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetValueAsync(string key, object value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a configuration value.
        /// </summary>
        /// <param name="key">Configuration key to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveValueAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates a configuration section.
        /// </summary>
        /// <param name="sectionName">Section name.</param>
        /// <param name="section">Section configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetSectionAsync(string sectionName, ConfigurationSection section, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a configuration section.
        /// </summary>
        /// <param name="sectionName">Section name to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveSectionAsync(string sectionName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates a configuration schema.
        /// </summary>
        /// <param name="schemaName">Schema name.</param>
        /// <param name="schema">Schema definition.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetSchemaAsync(string schemaName, ConfigurationSchema schema, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates configuration data in bulk.
        /// </summary>
        /// <param name="configurationData">Configuration data to merge.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateConfigurationDataAsync(
            IDictionary<string, object> configurationData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets configuration values by section.
        /// </summary>
        /// <param name="sectionName">Section name.</param>
        /// <returns>Configuration values in the section.</returns>
        IReadOnlyDictionary<string, object> GetSection(string sectionName);

        /// <summary>
        /// Checks if a configuration key exists.
        /// </summary>
        /// <param name="key">Configuration key to check.</param>
        /// <returns>True if key exists, false otherwise.</returns>
        bool HasValue(string key);
    }

    /// <summary>
    /// Represents a configuration section with metadata and validation rules.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class with mutable collections")]
    public class ConfigurationSection
    {
        /// <summary>
        /// Gets or sets the section name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the section description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this section is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets whether this section can be inherited.
        /// </summary>
        public bool IsInheritable { get; set; } = true;

        /// <summary>
        /// Gets or sets the section values.
        /// </summary>
        public Dictionary<string, object> Values { get; set; } = new();

        /// <summary>
        /// Gets or sets nested subsections.
        /// </summary>
        public Dictionary<string, ConfigurationSection> Subsections { get; set; } = new();

        /// <summary>
        /// Gets or sets validation rules for this section.
        /// </summary>
        public List<IConfigurationValidator> Validators { get; set; } = new();

        /// <summary>
        /// Gets or sets metadata for this section.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a configuration schema for validation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class with mutable collections")]
    public class ConfigurationSchema
    {
        /// <summary>
        /// Gets or sets the schema name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the schema version.
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets the schema description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets property definitions.
        /// </summary>
        public Dictionary<string, ConfigurationProperty> Properties { get; set; } = new();

        /// <summary>
        /// Gets or sets required property names.
        /// </summary>
        public HashSet<string> RequiredProperties { get; set; } = new();

        /// <summary>
        /// Gets or sets additional properties allowed flag.
        /// </summary>
        public bool AllowAdditionalProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets schema-level validators.
        /// </summary>
        public List<IConfigurationValidator> Validators { get; set; } = new();
    }

    /// <summary>
    /// Represents a configuration property definition.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class with mutable collections")]
    public class ConfigurationProperty
    {
        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the property type.
        /// </summary>
        public Type PropertyType { get; set; } = typeof(object);

        /// <summary>
        /// Gets or sets the property description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this property is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets allowed values for enumeration properties.
        /// </summary>
        public List<object> AllowedValues { get; set; } = new();

        /// <summary>
        /// Gets or sets minimum value for numeric properties.
        /// </summary>
        public object? MinValue { get; set; }

        /// <summary>
        /// Gets or sets maximum value for numeric properties.
        /// </summary>
        public object? MaxValue { get; set; }

        /// <summary>
        /// Gets or sets pattern for string properties.
        /// </summary>
        public string? Pattern { get; set; }

        /// <summary>
        /// Gets or sets minimum length for string/array properties.
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// Gets or sets maximum length for string/array properties.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets property-specific validators.
        /// </summary>
        public List<IConfigurationValidator> Validators { get; set; } = new();
    }

    /// <summary>
    /// Represents the result of configuration validation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "DTO-style class with mutable collections")]
    public class ConfigurationValidationResult
    {
        /// <summary>
        /// Gets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets validation errors.
        /// </summary>
        public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Gets validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets validation information.
        /// </summary>
        public List<string> Information { get; set; } = new();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>Successful validation result.</returns>
        public static ConfigurationValidationResult Success()
        {
            return new ConfigurationValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with errors.
        /// </summary>
        /// <param name="errors">Validation errors.</param>
        /// <returns>Failed validation result.</returns>
        public static ConfigurationValidationResult Failure(params string[] errors)
        {
            return new ConfigurationValidationResult
            {
                IsValid = false,
                Errors = errors.ToArray()
            };
        }

        /// <summary>
        /// Creates a successful validation result with warnings.
        /// </summary>
        /// <param name="warnings">Validation warnings.</param>
        /// <returns>Successful validation result with warnings.</returns>
        public static ConfigurationValidationResult WithWarnings(params string[] warnings)
        {
            return new ConfigurationValidationResult
            {
                IsValid = true,
                Warnings = warnings.ToList()
            };
        }

        /// <summary>
        /// Creates a successful validation result with information.
        /// </summary>
        /// <param name="information">Validation information.</param>
        /// <returns>Successful validation result with information.</returns>
        public static ConfigurationValidationResult WithInformation(params string[] information)
        {
            return new ConfigurationValidationResult
            {
                IsValid = true,
                Information = information.ToList()
            };
        }
    }

    /// <summary>
    /// Interface for configuration resolvers that can lookup parent configurations.
    /// </summary>
    public interface IConfigurationResolver
    {
        /// <summary>
        /// Resolves a configuration by ID.
        /// </summary>
        /// <param name="configurationId">Configuration ID to resolve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Resolved configuration or null if not found.</returns>
        Task<IConfigurationResource?> ResolveConfigurationAsync(uint configurationId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for configuration validators.
    /// </summary>
    public interface IConfigurationValidator
    {
        /// <summary>
        /// Validates a configuration value.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="context">Validation context.</param>
        /// <returns>Validation result.</returns>
        ConfigurationValidationResult Validate(object value, ConfigurationValidationContext context);
    }

    /// <summary>
    /// Context for configuration validation.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "DTO-style class needs mutable collections")]
    public class ConfigurationValidationContext
    {
        /// <summary>
        /// Gets or sets the property being validated.
        /// </summary>
        public ConfigurationProperty? Property { get; set; }

        /// <summary>
        /// Gets or sets the section being validated.
        /// </summary>
        public ConfigurationSection? Section { get; set; }

        /// <summary>
        /// Gets or sets the schema being validated against.
        /// </summary>
        public ConfigurationSchema? Schema { get; set; }

        /// <summary>
        /// Gets or sets the configuration key path.
        /// </summary>
        public string KeyPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional validation context data.
        /// </summary>
        public Dictionary<string, object> ContextData { get; set; } = new();
    }

    /// <summary>
    /// Validator for configuration value ranges.
    /// </summary>
    public class ConfigurationRangeValidator : IConfigurationValidator
    {
        private readonly object? _minValue;
        private readonly object? _maxValue;

        /// <summary>
        /// Initializes a new instance of the ConfigurationRangeValidator class.
        /// </summary>
        /// <param name="minValue">Minimum allowed value.</param>
        /// <param name="maxValue">Maximum allowed value.</param>
        public ConfigurationRangeValidator(object? minValue, object? maxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        /// <inheritdoc />
        public ConfigurationValidationResult Validate(object value, ConfigurationValidationContext context)
        {
            if (value is IComparable comparableValue)
            {
                var errors = new List<string>();

                if (_minValue != null && comparableValue.CompareTo(_minValue) < 0)
                {
                    errors.Add($"Value {value} is below minimum {_minValue} for {context.KeyPath}");
                }

                if (_maxValue != null && comparableValue.CompareTo(_maxValue) > 0)
                {
                    errors.Add($"Value {value} is above maximum {_maxValue} for {context.KeyPath}");
                }

                return errors.Count > 0
                    ? ConfigurationValidationResult.Failure(errors.ToArray())
                    : ConfigurationValidationResult.Success();
            }

            return ConfigurationValidationResult.Success();
        }
    }

    /// <summary>
    /// Validator for configuration enumeration values.
    /// </summary>
    public class ConfigurationEnumerationValidator : IConfigurationValidator
    {
        private readonly HashSet<object> _allowedValues;

        /// <summary>
        /// Initializes a new instance of the ConfigurationEnumerationValidator class.
        /// </summary>
        /// <param name="allowedValues">Allowed enumeration values.</param>
        public ConfigurationEnumerationValidator(IEnumerable<object> allowedValues)
        {
            _allowedValues = new HashSet<object>(allowedValues);
        }

        /// <inheritdoc />
        public ConfigurationValidationResult Validate(object value, ConfigurationValidationContext context)
        {
            if (!_allowedValues.Contains(value))
            {
                var allowedString = string.Join(", ", _allowedValues);
                return ConfigurationValidationResult.Failure(
                    $"Value '{value}' is not in allowed values: {allowedString} for {context.KeyPath}");
            }

            return ConfigurationValidationResult.Success();
        }
    }
}
