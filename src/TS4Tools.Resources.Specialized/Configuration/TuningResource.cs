using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Specialized.Configuration
{
    /// <summary>
    /// Implementation of tuning resources that manage game parameters and configuration values.
    /// TuningResource provides flexible configuration management with inheritance and validation.
    /// </summary>
    public class TuningResource : ITuningResource, IDisposable
    {
        private readonly Dictionary<string, TuningParameter> _parameters = new();
        private readonly Dictionary<string, object> _rawTuningData = new();
        private readonly Dictionary<string, object> _tuningData = new();
        private bool _isValidated;
        private bool _inheritanceResolved;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the TuningResource class.
        /// </summary>
        public TuningResource()
        {
            TuningName = string.Empty;
            TuningCategory = string.Empty;
            TuningInstance = 0;
            Stream = new MemoryStream();
        }

        /// <summary>
        /// Initializes a new instance of the TuningResource class with tuning data.
        /// </summary>
        /// <param name="tuningName">Tuning name/identifier.</param>
        /// <param name="category">Tuning category.</param>
        /// <param name="instance">Tuning instance ID.</param>
        /// <param name="parentTuningId">Parent tuning ID if inheriting.</param>
        public TuningResource(string tuningName, string category, ulong instance, uint? parentTuningId = null)
        {
            TuningName = tuningName ?? string.Empty;
            TuningCategory = category ?? string.Empty;
            TuningInstance = instance;
            ParentTuningId = parentTuningId;
            Stream = new MemoryStream();
        }

        #region ITuningResource Implementation

        /// <inheritdoc />
        public string TuningName { get; private set; }

        /// <inheritdoc />
        public string TuningCategory { get; private set; }

        /// <inheritdoc />
        public ulong TuningInstance { get; private set; }

        /// <inheritdoc />
        public uint? ParentTuningId { get; private set; }

        /// <inheritdoc />
        public bool IsValidated => _isValidated;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, TuningParameter> Parameters => _parameters;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> TuningData => _tuningData;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> RawTuningData => _rawTuningData;

        /// <inheritdoc />
        public async Task<TuningValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TuningResource));

            var errors = new List<string>();
            var warnings = new List<string>();
            var information = new List<string>();

            // Validate tuning name
            if (string.IsNullOrWhiteSpace(TuningName))
            {
                errors.Add("Tuning name cannot be empty");
            }

            // Validate tuning category
            if (string.IsNullOrWhiteSpace(TuningCategory))
            {
                warnings.Add("Tuning category is not specified");
            }

            // Validate parameters
            foreach (var parameter in _parameters.Values)
            {
                if (string.IsNullOrWhiteSpace(parameter.Name))
                {
                    errors.Add("Parameter name cannot be empty");
                    continue;
                }

                // Check if required parameters have values
                if (parameter.IsRequired && !_tuningData.ContainsKey(parameter.Name))
                {
                    errors.Add($"Required parameter '{parameter.Name}' is missing");
                    continue;
                }

                // Validate parameter value if present
                if (_tuningData.TryGetValue(parameter.Name, out var value))
                {
                    var parameterValidation = await ValidateParameterValueAsync(parameter, value, cancellationToken);
                    if (!parameterValidation.IsValid)
                    {
                        errors.AddRange(parameterValidation.Errors);
                    }
                    warnings.AddRange(parameterValidation.Warnings);
                    information.AddRange(parameterValidation.Information);
                }
            }

            // Validate tuning data consistency
            await ValidateTuningDataConsistencyAsync(errors, warnings, information, cancellationToken);

            _isValidated = errors.Count == 0;

            if (errors.Count > 0)
            {
                var result = TuningValidationResult.Failure(errors.ToArray());
                result.Warnings = warnings;
                result.Information = information;
                return result;
            }

            if (warnings.Count > 0)
            {
                var result = TuningValidationResult.WithWarnings(warnings.ToArray());
                result.Information = information;
                return result;
            }

            return information.Count > 0
                ? TuningValidationResult.WithInformation(information.ToArray())
                : TuningValidationResult.Success();
        }

        /// <inheritdoc />
        public async Task ResolveInheritanceAsync(
            ITuningResolver tuningResolver,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TuningResource));
            ArgumentNullException.ThrowIfNull(tuningResolver);

            _tuningData.Clear();

            // Start with resolved data from inheritance chain
            if (ParentTuningId.HasValue)
            {
                await ResolveParentTuningAsync(tuningResolver, cancellationToken);
            }

            // Merge raw tuning data over inherited data
            foreach (var kvp in _rawTuningData)
            {
                _tuningData[kvp.Key] = kvp.Value;
            }

            _inheritanceResolved = true;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<uint>> GetDependenciesAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TuningResource));

            var dependencies = new HashSet<uint>();

            // Add parent tuning dependency
            if (ParentTuningId.HasValue)
            {
                dependencies.Add(ParentTuningId.Value);
            }

            // Extract tuning references from tuning data
            await ExtractTuningReferencesAsync(_rawTuningData, dependencies, cancellationToken);

            return dependencies;
        }

        /// <inheritdoc />
        public T GetParameter<T>(string parameterName, T defaultValue = default!) where T : notnull
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TuningResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

            if (_tuningData.TryGetValue(parameterName, out var value))
            {
                try
                {
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }

                    // Try to convert the value
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <inheritdoc />
        public async Task SetParameterAsync(string parameterName, object value, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TuningResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

            _rawTuningData[parameterName] = value;

            if (_inheritanceResolved)
            {
                _tuningData[parameterName] = value;
            }

            _isValidated = false; // Invalidate validation state
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task RemoveParameterAsync(string parameterName, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TuningResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

            _rawTuningData.Remove(parameterName);
            _tuningData.Remove(parameterName);

            _isValidated = false; // Invalidate validation state
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task AddParameterDefinitionAsync(TuningParameter parameter, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TuningResource));
            ArgumentNullException.ThrowIfNull(parameter);
            ArgumentException.ThrowIfNullOrWhiteSpace(parameter.Name);

            _parameters[parameter.Name] = parameter;
            _isValidated = false; // Invalidate validation state
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task UpdateTuningDataAsync(
            IDictionary<string, object> tuningData,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TuningResource));
            ArgumentNullException.ThrowIfNull(tuningData);

            _rawTuningData.Clear();
            foreach (var kvp in tuningData)
            {
                _rawTuningData[kvp.Key] = kvp.Value;
            }

            _isValidated = false; // Invalidate validation state
            _inheritanceResolved = false; // Require re-resolution
            OnResourceChanged();

            await Task.CompletedTask;
        }

        #endregion

        #region IResource Implementation

        /// <inheritdoc />
        public Stream Stream { get; private set; }

        /// <inheritdoc />
        public byte[] AsBytes => SerializeToBytes();

        /// <inheritdoc />
        public event EventHandler? ResourceChanged;

        /// <inheritdoc />
        public IReadOnlyList<string> ContentFields => new[]
        {
            "TuningName", "TuningCategory", "TuningInstance", "ParentTuningId",
            "IsValidated", "ParameterCount", "DataCount"
        };

        /// <inheritdoc />
        TypedValue IContentFields.this[int index]
        {
            get => index switch
            {
                0 => TypedValue.Create(TuningName, "string"),
                1 => TypedValue.Create(TuningCategory, "string"),
                2 => TypedValue.Create(TuningInstance, "ulong"),
                3 => TypedValue.Create(ParentTuningId, "uint?"),
                4 => TypedValue.Create(IsValidated, "bool"),
                5 => TypedValue.Create(_parameters.Count, "int"),
                6 => TypedValue.Create(_rawTuningData.Count, "int"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (index)
                {
                    case 0:
                        TuningName = value.Value?.ToString() ?? string.Empty;
                        break;
                    case 1:
                        TuningCategory = value.Value?.ToString() ?? string.Empty;
                        break;
                    case 2:
                        TuningInstance = value.Value is ulong instance ? instance : 0;
                        break;
                    case 3:
                        ParentTuningId = value.Value is uint parentId ? parentId : null;
                        break;
                }
                OnResourceChanged();
            }
        }

        /// <inheritdoc />
        TypedValue IContentFields.this[string name]
        {
            get => name switch
            {
                "TuningName" => TypedValue.Create(TuningName, "string"),
                "TuningCategory" => TypedValue.Create(TuningCategory, "string"),
                "TuningInstance" => TypedValue.Create(TuningInstance, "ulong"),
                "ParentTuningId" => TypedValue.Create(ParentTuningId, "uint?"),
                "IsValidated" => TypedValue.Create(IsValidated, "bool"),
                "ParameterCount" => TypedValue.Create(_parameters.Count, "int"),
                "DataCount" => TypedValue.Create(_rawTuningData.Count, "int"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (name)
                {
                    case "TuningName":
                        TuningName = value.Value?.ToString() ?? string.Empty;
                        break;
                    case "TuningCategory":
                        TuningCategory = value.Value?.ToString() ?? string.Empty;
                        break;
                    case "TuningInstance":
                        TuningInstance = value.Value is ulong instance ? instance : 0;
                        break;
                    case "ParentTuningId":
                        ParentTuningId = value.Value is uint parentId ? parentId : null;
                        break;
                }
                OnResourceChanged();
            }
        }

        /// <inheritdoc />
        public object? this[string index]
        {
            get => ((IContentFields)this)[index].Value;
            set => ((IContentFields)this)[index] = TypedValue.Create(value);
        }

        #endregion

        #region IApiVersion Implementation

        /// <inheritdoc />
        public int RecommendedApiVersion => 1;

        /// <inheritdoc />
        public int RequestedApiVersion { get; set; } = 1;

        #endregion

        #region Private Methods

        private async Task ValidateTuningDataConsistencyAsync(
            List<string> errors,
            List<string> warnings,
            List<string> information,
            CancellationToken cancellationToken)
        {
            // Check for orphaned tuning data (data without parameter definitions)
            foreach (var key in _tuningData.Keys)
            {
                if (!_parameters.ContainsKey(key))
                {
                    warnings.Add($"Tuning data '{key}' has no parameter definition");
                }
            }

            // Check for type mismatches
            foreach (var parameter in _parameters.Values)
            {
                if (_tuningData.TryGetValue(parameter.Name, out var value) && value != null)
                {
                    if (!parameter.ParameterType.IsAssignableFrom(value.GetType()))
                    {
                        errors.Add($"Parameter '{parameter.Name}' type mismatch: expected {parameter.ParameterType.Name}, got {value.GetType().Name}");
                    }
                }
            }

            await Task.CompletedTask;
        }

        private async Task<TuningValidationResult> ValidateParameterValueAsync(
            TuningParameter parameter,
            object value,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Type validation
            if (!parameter.ParameterType.IsAssignableFrom(value.GetType()))
            {
                errors.Add($"Parameter '{parameter.Name}' expects type {parameter.ParameterType.Name}, got {value.GetType().Name}");
            }

            // Range validation for numeric types
            if (parameter.MinValue != null || parameter.MaxValue != null)
            {
                if (value is IComparable comparableValue)
                {
                    if (parameter.MinValue != null && comparableValue.CompareTo(parameter.MinValue) < 0)
                    {
                        errors.Add($"Parameter '{parameter.Name}' value {value} is below minimum {parameter.MinValue}");
                    }
                    if (parameter.MaxValue != null && comparableValue.CompareTo(parameter.MaxValue) > 0)
                    {
                        errors.Add($"Parameter '{parameter.Name}' value {value} is above maximum {parameter.MaxValue}");
                    }
                }
            }

            // Enumeration validation
            if (parameter.AllowedValues.Any() && !parameter.AllowedValues.Contains(value))
            {
                var allowedString = string.Join(", ", parameter.AllowedValues);
                errors.Add($"Parameter '{parameter.Name}' value '{value}' is not in allowed values: {allowedString}");
            }

            // Custom validator validation
            foreach (var validator in parameter.Validators)
            {
                var validationResult = validator.Validate(value, parameter);
                if (!validationResult.IsValid)
                {
                    errors.AddRange(validationResult.Errors);
                }
                warnings.AddRange(validationResult.Warnings);
            }

            await Task.CompletedTask;

            if (errors.Count > 0)
            {
                var result = TuningValidationResult.Failure(errors.ToArray());
                result.Warnings = warnings;
                return result;
            }

            return warnings.Count > 0
                ? TuningValidationResult.WithWarnings(warnings.ToArray())
                : TuningValidationResult.Success();
        }

        private async Task ResolveParentTuningAsync(
            ITuningResolver tuningResolver,
            CancellationToken cancellationToken)
        {
            if (!ParentTuningId.HasValue)
                return;

            var parentTuning = await tuningResolver.ResolveTuningAsync(ParentTuningId.Value, cancellationToken);
            if (parentTuning == null)
            {
                throw new InvalidOperationException($"Parent tuning with ID {ParentTuningId.Value} not found");
            }

            // Ensure parent tuning inheritance is resolved
            if (!parentTuning.IsValidated)
            {
                await parentTuning.ResolveInheritanceAsync(tuningResolver, cancellationToken);
            }

            // Merge parent tuning data
            foreach (var kvp in parentTuning.TuningData)
            {
                _tuningData[kvp.Key] = kvp.Value;
            }

            // Inherit parameters from parent (don't override existing ones)
            foreach (var kvp in parentTuning.Parameters)
            {
                if (!_parameters.ContainsKey(kvp.Key))
                {
                    _parameters[kvp.Key] = kvp.Value;
                }
            }
        }

        private async Task ExtractTuningReferencesAsync(
            IReadOnlyDictionary<string, object> data,
            HashSet<uint> references,
            CancellationToken cancellationToken)
        {
            foreach (var kvp in data)
            {
                if (kvp.Value is uint uintValue)
                {
                    // Assume uint values might be tuning references
                    references.Add(uintValue);
                }
                else if (kvp.Value is IDictionary<string, object> nestedDict)
                {
                    // Convert to readonly dictionary for recursion
                    var readOnlyNestedDict = nestedDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    await ExtractTuningReferencesAsync(readOnlyNestedDict, references, cancellationToken);
                }
            }

            await Task.CompletedTask;
        }

        private byte[] SerializeToBytes()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream, Encoding.UTF8);

            // Write tuning header
            writer.Write(TuningName);
            writer.Write(TuningCategory);
            writer.Write(TuningInstance);
            writer.Write(ParentTuningId ?? 0);

            // Write parameters
            writer.Write(_parameters.Count);
            foreach (var parameter in _parameters.Values)
            {
                WriteParameter(writer, parameter);
            }

            // Write tuning data
            writer.Write(_rawTuningData.Count);
            foreach (var kvp in _rawTuningData)
            {
                writer.Write(kvp.Key);
                WriteValue(writer, kvp.Value);
            }

            return memoryStream.ToArray();
        }

        private static void WriteParameter(BinaryWriter writer, TuningParameter parameter)
        {
            writer.Write(parameter.Name);
            writer.Write(parameter.ParameterType.FullName ?? string.Empty);
            writer.Write(parameter.IsRequired);
            writer.Write(parameter.Description);

            // Write default value
            if (parameter.DefaultValue != null)
            {
                writer.Write(true);
                WriteValue(writer, parameter.DefaultValue);
            }
            else
            {
                writer.Write(false);
            }

            // Write min/max values
            if (parameter.MinValue != null)
            {
                writer.Write(true);
                WriteValue(writer, parameter.MinValue);
            }
            else
            {
                writer.Write(false);
            }

            if (parameter.MaxValue != null)
            {
                writer.Write(true);
                WriteValue(writer, parameter.MaxValue);
            }
            else
            {
                writer.Write(false);
            }

            // Write allowed values
            writer.Write(parameter.AllowedValues.Count);
            foreach (var allowedValue in parameter.AllowedValues)
            {
                WriteValue(writer, allowedValue);
            }
        }

        private static void WriteValue(BinaryWriter writer, object value)
        {
            switch (value)
            {
                case string s:
                    writer.Write((byte)1);
                    writer.Write(s);
                    break;
                case int i:
                    writer.Write((byte)2);
                    writer.Write(i);
                    break;
                case uint ui:
                    writer.Write((byte)3);
                    writer.Write(ui);
                    break;
                case bool b:
                    writer.Write((byte)4);
                    writer.Write(b);
                    break;
                case float f:
                    writer.Write((byte)5);
                    writer.Write(f);
                    break;
                case double d:
                    writer.Write((byte)6);
                    writer.Write(d);
                    break;
                case ulong ul:
                    writer.Write((byte)7);
                    writer.Write(ul);
                    break;
                default:
                    writer.Write((byte)0);
                    writer.Write(value.ToString() ?? string.Empty);
                    break;
            }
        }

        private void OnResourceChanged()
        {
            ResourceChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resource.
        /// </summary>
        /// <param name="disposing">Whether disposing from Dispose method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Stream?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
