using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Specialized.Configuration
{
    /// <summary>
    /// Interface for tuning resources that manage game parameters and configuration values.
    /// TuningResource provides flexible configuration management with inheritance and validation.
    /// </summary>
    public interface ITuningResource : IResource
    {
        /// <summary>
        /// Gets the tuning name/identifier.
        /// </summary>
        string TuningName { get; }

        /// <summary>
        /// Gets the tuning category (e.g., "Sim", "Object", "Interaction").
        /// </summary>
        string TuningCategory { get; }

        /// <summary>
        /// Gets the tuning instance ID for referencing in the game.
        /// </summary>
        ulong TuningInstance { get; }

        /// <summary>
        /// Gets the parent tuning resource ID if this tuning inherits from another.
        /// Returns null if this is a root tuning.
        /// </summary>
        uint? ParentTuningId { get; }

        /// <summary>
        /// Gets whether this tuning has been validated and is ready for use.
        /// </summary>
        bool IsValidated { get; }

        /// <summary>
        /// Gets the collection of tuning parameters and their metadata.
        /// </summary>
        IReadOnlyDictionary<string, TuningParameter> Parameters { get; }

        /// <summary>
        /// Gets the resolved tuning data after inheritance and parameter resolution.
        /// </summary>
        IReadOnlyDictionary<string, object> TuningData { get; }

        /// <summary>
        /// Validates the tuning structure and parameter values.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Validation result with any errors or warnings.</returns>
        Task<TuningValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the tuning inheritance chain and merges data from parent tunings.
        /// </summary>
        /// <param name="tuningResolver">Resolver for loading parent tunings.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the resolution operation.</returns>
        Task ResolveInheritanceAsync(
            ITuningResolver tuningResolver,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tuning dependencies (parent tunings and referenced tunings).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Collection of tuning resource IDs that this tuning depends on.</returns>
        Task<IReadOnlyCollection<uint>> GetDependenciesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the value of a tuning parameter.
        /// </summary>
        /// <typeparam name="T">Expected type of the parameter value.</typeparam>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="defaultValue">Default value if parameter is not found.</param>
        /// <returns>Parameter value or default value.</returns>
        T GetParameter<T>(string parameterName, T defaultValue = default!) where T : notnull;

        /// <summary>
        /// Sets the value of a tuning parameter.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">Parameter value.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the operation.</returns>
        Task SetParameterAsync(string parameterName, object value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a tuning parameter.
        /// </summary>
        /// <param name="parameterName">Name of the parameter to remove.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the operation.</returns>
        Task RemoveParameterAsync(string parameterName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates a tuning parameter definition.
        /// </summary>
        /// <param name="parameter">Parameter definition to add or update.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the operation.</returns>
        Task AddParameterDefinitionAsync(TuningParameter parameter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the raw tuning data before inheritance resolution.
        /// </summary>
        IReadOnlyDictionary<string, object> RawTuningData { get; }

        /// <summary>
        /// Updates the raw tuning data.
        /// </summary>
        /// <param name="tuningData">New tuning data.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the operation.</returns>
        Task UpdateTuningDataAsync(
            IDictionary<string, object> tuningData,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a tuning parameter with type information and constraints.
    /// </summary>
    public class TuningParameter
    {
        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter type.
        /// </summary>
        public Type ParameterType { get; set; } = typeof(object);

        /// <summary>
        /// Gets or sets the default value for the parameter.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets whether this parameter is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the parameter description for documentation.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the minimum value (for numeric types).
        /// </summary>
        public object? MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value (for numeric types).
        /// </summary>
        public object? MaxValue { get; set; }

        /// <summary>
        /// Gets or sets allowed values (for enumerated types).
        /// </summary>
        public IReadOnlyList<object> AllowedValues { get; set; } = Array.Empty<object>();

        /// <summary>
        /// Gets or sets validation rules for the parameter value.
        /// </summary>
        public IReadOnlyList<ITuningParameterValidator> Validators { get; set; } = Array.Empty<ITuningParameterValidator>();
    }

    /// <summary>
    /// Represents the result of a tuning validation operation.
    /// </summary>
    public class TuningValidationResult
    {
        /// <summary>
        /// Gets or sets whether validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets validation error messages.
        /// </summary>
        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets validation warning messages.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets validation information messages.
        /// </summary>
        public IReadOnlyList<string> Information { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static TuningValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with error messages.
        /// </summary>
        /// <param name="errors">Error messages.</param>
        public static TuningValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = errors.ToArray()
        };

        /// <summary>
        /// Creates a validation result with warnings.
        /// </summary>
        /// <param name="warnings">Warning messages.</param>
        public static TuningValidationResult WithWarnings(params string[] warnings) => new()
        {
            IsValid = true,
            Warnings = warnings.ToArray()
        };

        /// <summary>
        /// Creates a validation result with information messages.
        /// </summary>
        /// <param name="information">Information messages.</param>
        public static TuningValidationResult WithInformation(params string[] information) => new()
        {
            IsValid = true,
            Information = information.ToArray()
        };
    }

    /// <summary>
    /// Interface for tuning resolution during inheritance processing.
    /// </summary>
    public interface ITuningResolver
    {
        /// <summary>
        /// Resolves a tuning by its resource ID.
        /// </summary>
        /// <param name="tuningId">Tuning resource ID.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The resolved tuning resource.</returns>
        Task<ITuningResource?> ResolveTuningAsync(uint tuningId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for tuning parameter validators.
    /// </summary>
    public interface ITuningParameterValidator
    {
        /// <summary>
        /// Validates a parameter value.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="parameter">Parameter definition.</param>
        /// <returns>Validation result.</returns>
        TuningValidationResult Validate(object? value, TuningParameter parameter);
    }

    /// <summary>
    /// Range validator for numeric tuning parameters.
    /// </summary>
    public class NumericRangeValidator : ITuningParameterValidator
    {
        private readonly IComparable _minValue;
        private readonly IComparable _maxValue;

        /// <summary>
        /// Initializes a new instance of the NumericRangeValidator class.
        /// </summary>
        /// <param name="minValue">Minimum allowed value.</param>
        /// <param name="maxValue">Maximum allowed value.</param>
        public NumericRangeValidator(IComparable minValue, IComparable maxValue)
        {
            _minValue = minValue ?? throw new ArgumentNullException(nameof(minValue));
            _maxValue = maxValue ?? throw new ArgumentNullException(nameof(maxValue));
        }

        /// <inheritdoc />
        public TuningValidationResult Validate(object? value, TuningParameter parameter)
        {
            if (value == null)
            {
                return parameter.IsRequired
                    ? TuningValidationResult.Failure($"Parameter '{parameter.Name}' is required")
                    : TuningValidationResult.Success();
            }

            if (value is not IComparable comparableValue)
            {
                return TuningValidationResult.Failure($"Parameter '{parameter.Name}' value is not comparable");
            }

            if (comparableValue.CompareTo(_minValue) < 0)
            {
                return TuningValidationResult.Failure($"Parameter '{parameter.Name}' value {value} is below minimum {_minValue}");
            }

            if (comparableValue.CompareTo(_maxValue) > 0)
            {
                return TuningValidationResult.Failure($"Parameter '{parameter.Name}' value {value} is above maximum {_maxValue}");
            }

            return TuningValidationResult.Success();
        }
    }

    /// <summary>
    /// Enumeration validator for tuning parameters with allowed values.
    /// </summary>
    public class EnumerationValidator : ITuningParameterValidator
    {
        private readonly IReadOnlySet<object> _allowedValues;

        /// <summary>
        /// Initializes a new instance of the EnumerationValidator class.
        /// </summary>
        /// <param name="allowedValues">Collection of allowed values.</param>
        public EnumerationValidator(IEnumerable<object> allowedValues)
        {
            ArgumentNullException.ThrowIfNull(allowedValues);
            _allowedValues = allowedValues.ToHashSet();
        }

        /// <inheritdoc />
        public TuningValidationResult Validate(object? value, TuningParameter parameter)
        {
            if (value == null)
            {
                return parameter.IsRequired
                    ? TuningValidationResult.Failure($"Parameter '{parameter.Name}' is required")
                    : TuningValidationResult.Success();
            }

            if (!_allowedValues.Contains(value))
            {
                var allowedString = string.Join(", ", _allowedValues);
                return TuningValidationResult.Failure($"Parameter '{parameter.Name}' value '{value}' is not in allowed values: {allowedString}");
            }

            return TuningValidationResult.Success();
        }
    }
}
