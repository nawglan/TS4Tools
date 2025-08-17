using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Specialized.Templates
{
    /// <summary>
    /// Implementation of template resources that support inheritance and instantiation.
    /// ComplateResource manages templated data with parameter substitution and inheritance chains.
    /// </summary>
    public class ComplateResource : IComplateResource, IDisposable
    {
        private readonly Dictionary<string, TemplateParameter> _parameters = new();
        private readonly Dictionary<string, object> _rawTemplateData = new();
        private readonly Dictionary<string, object> _resolvedData = new();
        private bool _isValidated;
        private bool _inheritanceResolved;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ComplateResource class.
        /// </summary>
        public ComplateResource()
        {
            TemplateName = string.Empty;
            Stream = new MemoryStream();
        }

        /// <summary>
        /// Initializes a new instance of the ComplateResource class with template data.
        /// </summary>
        /// <param name="templateName">Template name/identifier.</param>
        /// <param name="templateData">Initial template data.</param>
        /// <param name="parentTemplateId">Parent template ID if inheriting.</param>
        public ComplateResource(string templateName, IDictionary<string, object>? templateData = null, uint? parentTemplateId = null)
        {
            TemplateName = templateName ?? string.Empty;
            ParentTemplateId = parentTemplateId;
            Stream = new MemoryStream();

            if (templateData != null)
            {
                foreach (var kvp in templateData)
                {
                    _rawTemplateData[kvp.Key] = kvp.Value;
                }
            }
        }

        #region IComplateResource Implementation

        /// <inheritdoc />
        public string TemplateName { get; private set; }

        /// <inheritdoc />
        public uint? ParentTemplateId { get; private set; }

        /// <inheritdoc />
        public bool IsInheritable { get; private set; } = true;

        /// <inheritdoc />
        public bool IsValidated => _isValidated;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, TemplateParameter> Parameters => _parameters;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> ResolvedData => _resolvedData;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> RawTemplateData => _rawTemplateData;

        /// <inheritdoc />
        public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ComplateResource));

            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate template name
            if (string.IsNullOrWhiteSpace(TemplateName))
            {
                errors.Add("Template name cannot be empty");
            }

            // Validate parameters
            foreach (var parameter in _parameters.Values)
            {
                if (string.IsNullOrWhiteSpace(parameter.Name))
                {
                    errors.Add("Parameter name cannot be empty");
                    continue;
                }

                // Validate default value against parameter type
                if (parameter.DefaultValue != null &&
                    !parameter.ParameterType.IsAssignableFrom(parameter.DefaultValue.GetType()))
                {
                    errors.Add($"Parameter '{parameter.Name}' default value type mismatch");
                }

                // Validate constraints
                if (parameter.DefaultValue != null)
                {
                    foreach (var constraint in parameter.Constraints)
                    {
                        var constraintResult = constraint.Validate(parameter.DefaultValue, parameter.Name);
                        if (!constraintResult.IsValid)
                        {
                            errors.AddRange(constraintResult.Errors);
                        }
                        warnings.AddRange(constraintResult.Warnings);
                    }
                }
            }

            // Validate template data references
            await ValidateTemplateDataReferencesAsync(errors, warnings, cancellationToken);

            _isValidated = errors.Count == 0;

            return errors.Count == 0
                ? ValidationResult.WithWarnings(warnings.ToArray())
                : ValidationResult.Failure(errors.ToArray());
        }

        /// <inheritdoc />
        public async Task<TemplateInstance> InstantiateAsync(
            IDictionary<string, object> parameterValues,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ComplateResource));

            if (!_inheritanceResolved)
            {
                throw new InvalidOperationException("Template inheritance must be resolved before instantiation");
            }

            // Validate required parameters
            var missingRequired = _parameters.Values
                .Where(p => p.IsRequired && !parameterValues.ContainsKey(p.Name))
                .Select(p => p.Name)
                .ToList();

            if (missingRequired.Any())
            {
                throw new ArgumentException($"Missing required parameters: {string.Join(", ", missingRequired)}");
            }

            // Merge parameter values with defaults
            var resolvedValues = new Dictionary<string, object>();
            foreach (var parameter in _parameters.Values)
            {
                if (parameterValues.TryGetValue(parameter.Name, out var value))
                {
                    // Validate parameter value
                    await ValidateParameterValueAsync(parameter, value, cancellationToken);
                    resolvedValues[parameter.Name] = value;
                }
                else if (parameter.DefaultValue != null)
                {
                    resolvedValues[parameter.Name] = parameter.DefaultValue;
                }
            }

            // Instantiate template data with parameter substitution
            var instanceData = await SubstituteParametersAsync(_resolvedData, resolvedValues, cancellationToken);

            return new TemplateInstance
            {
                TemplateId = 0, // Use 0 as default template ID
                TemplateName = TemplateName,
                ParameterValues = resolvedValues,
                InstanceData = instanceData,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <inheritdoc />
        public async Task ResolveInheritanceAsync(
            ITemplateResolver templateResolver,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ComplateResource));
            ArgumentNullException.ThrowIfNull(templateResolver);

            _resolvedData.Clear();

            // Start with resolved data from inheritance chain
            if (ParentTemplateId.HasValue)
            {
                await ResolveParentTemplateAsync(templateResolver, cancellationToken);
            }

            // Merge raw template data over inherited data
            foreach (var kvp in _rawTemplateData)
            {
                _resolvedData[kvp.Key] = kvp.Value;
            }

            _inheritanceResolved = true;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<uint>> GetDependenciesAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ComplateResource));

            var dependencies = new HashSet<uint>();

            // Add parent template dependency
            if (ParentTemplateId.HasValue)
            {
                dependencies.Add(ParentTemplateId.Value);
            }

            // Extract template references from template data
            await ExtractTemplateReferencesAsync(_rawTemplateData, dependencies, cancellationToken);

            return dependencies;
        }

        /// <inheritdoc />
        public async Task SetParameterAsync(
            string name,
            TemplateParameter parameter,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ComplateResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(parameter);

            _parameters[name] = parameter;
            _isValidated = false; // Invalidate validation state
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task RemoveParameterAsync(string name, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ComplateResource));
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _parameters.Remove(name);
            _isValidated = false; // Invalidate validation state
            OnResourceChanged();

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task UpdateTemplateDataAsync(
            IDictionary<string, object> templateData,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ComplateResource));
            ArgumentNullException.ThrowIfNull(templateData);

            _rawTemplateData.Clear();
            foreach (var kvp in templateData)
            {
                _rawTemplateData[kvp.Key] = kvp.Value;
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
            "TemplateName", "ParentTemplateId", "IsInheritable", "IsValidated",
            "ParameterCount", "DataCount"
        };

        /// <inheritdoc />
        TypedValue IContentFields.this[int index]
        {
            get => index switch
            {
                0 => TypedValue.Create(TemplateName, "string"),
                1 => TypedValue.Create(ParentTemplateId, "uint?"),
                2 => TypedValue.Create(IsInheritable, "bool"),
                3 => TypedValue.Create(IsValidated, "bool"),
                4 => TypedValue.Create(_parameters.Count, "int"),
                5 => TypedValue.Create(_rawTemplateData.Count, "int"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (index)
                {
                    case 0:
                        TemplateName = value.Value?.ToString() ?? string.Empty;
                        break;
                    case 1:
                        ParentTemplateId = value.Value is uint parentId ? parentId : null;
                        break;
                    case 2:
                        IsInheritable = value.Value is bool inheritable && inheritable;
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
                "TemplateName" => TypedValue.Create(TemplateName, "string"),
                "ParentTemplateId" => TypedValue.Create(ParentTemplateId, "uint?"),
                "IsInheritable" => TypedValue.Create(IsInheritable, "bool"),
                "IsValidated" => TypedValue.Create(IsValidated, "bool"),
                "ParameterCount" => TypedValue.Create(_parameters.Count, "int"),
                "DataCount" => TypedValue.Create(_rawTemplateData.Count, "int"),
                _ => TypedValue.Create<object?>(null, "object")
            };
            set
            {
                switch (name)
                {
                    case "TemplateName":
                        TemplateName = value.Value?.ToString() ?? string.Empty;
                        break;
                    case "ParentTemplateId":
                        ParentTemplateId = value.Value is uint parentId ? parentId : null;
                        break;
                    case "IsInheritable":
                        IsInheritable = value.Value is bool inheritable && inheritable;
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

        private async Task ValidateTemplateDataReferencesAsync(
            List<string> errors,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            // Validate parameter references in template data
            foreach (var kvp in _rawTemplateData)
            {
                if (kvp.Value is string stringValue)
                {
                    // Check for parameter placeholders like ${paramName}
                    var parameterRefs = ExtractParameterReferences(stringValue);
                    foreach (var paramRef in parameterRefs)
                    {
                        if (!_parameters.ContainsKey(paramRef))
                        {
                            warnings.Add($"Template data references undefined parameter: {paramRef}");
                        }
                    }
                }
            }

            await Task.CompletedTask;
        }

        private static IEnumerable<string> ExtractParameterReferences(string text)
        {
            var references = new List<string>();
            var startIndex = 0;

            while (startIndex < text.Length)
            {
                var start = text.IndexOf("${", startIndex, StringComparison.Ordinal);
                if (start == -1) break;

                var end = text.IndexOf("}", start + 2, StringComparison.Ordinal);
                if (end == -1) break;

                var paramName = text.Substring(start + 2, end - start - 2);
                if (!string.IsNullOrWhiteSpace(paramName))
                {
                    references.Add(paramName.Trim());
                }

                startIndex = end + 1;
            }

            return references;
        }

        private async Task ResolveParentTemplateAsync(
            ITemplateResolver templateResolver,
            CancellationToken cancellationToken)
        {
            if (!ParentTemplateId.HasValue)
                return;

            var parentTemplate = await templateResolver.ResolveTemplateAsync(ParentTemplateId.Value, cancellationToken);
            if (parentTemplate == null)
            {
                throw new InvalidOperationException($"Parent template with ID {ParentTemplateId.Value} not found");
            }

            // Ensure parent template inheritance is resolved
            if (!parentTemplate.IsValidated)
            {
                await parentTemplate.ResolveInheritanceAsync(templateResolver, cancellationToken);
            }

            // Merge parent template data
            foreach (var kvp in parentTemplate.ResolvedData)
            {
                _resolvedData[kvp.Key] = kvp.Value;
            }

            // Inherit parameters from parent (don't override existing ones)
            foreach (var kvp in parentTemplate.Parameters)
            {
                if (!_parameters.ContainsKey(kvp.Key))
                {
                    _parameters[kvp.Key] = kvp.Value;
                }
            }
        }

        private async Task ValidateParameterValueAsync(
            TemplateParameter parameter,
            object value,
            CancellationToken cancellationToken)
        {
            // Type validation
            if (!parameter.ParameterType.IsAssignableFrom(value.GetType()))
            {
                throw new ArgumentException($"Parameter '{parameter.Name}' expects type {parameter.ParameterType.Name}, got {value.GetType().Name}");
            }

            // Constraint validation
            foreach (var constraint in parameter.Constraints)
            {
                var result = constraint.Validate(value, parameter.Name);
                if (!result.IsValid)
                {
                    throw new ArgumentException($"Parameter '{parameter.Name}' failed validation: {string.Join(", ", result.Errors)}");
                }
            }

            await Task.CompletedTask;
        }

        private async Task<Dictionary<string, object>> SubstituteParametersAsync(
            IReadOnlyDictionary<string, object> templateData,
            IReadOnlyDictionary<string, object> parameterValues,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in templateData)
            {
                result[kvp.Key] = await SubstituteValueAsync(kvp.Value, parameterValues, cancellationToken);
            }

            return result;
        }

        private async Task<object> SubstituteValueAsync(
            object value,
            IReadOnlyDictionary<string, object> parameterValues,
            CancellationToken cancellationToken)
        {
            if (value is string stringValue)
            {
                return SubstituteStringParameters(stringValue, parameterValues);
            }

            if (value is IDictionary<string, object> dictValue)
            {
                // Convert to readonly dictionary for recursion
                var readOnlyDict = dictValue.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return await SubstituteParametersAsync(readOnlyDict, parameterValues, cancellationToken);
            }

            return value;
        }

        private static string SubstituteStringParameters(
            string text,
            IReadOnlyDictionary<string, object> parameterValues)
        {
            var result = text;
            var startIndex = 0;

            while (startIndex < result.Length)
            {
                var start = result.IndexOf("${", startIndex, StringComparison.Ordinal);
                if (start == -1) break;

                var end = result.IndexOf("}", start + 2, StringComparison.Ordinal);
                if (end == -1) break;

                var paramName = result.Substring(start + 2, end - start - 2).Trim();
                if (parameterValues.TryGetValue(paramName, out var paramValue))
                {
                    var replacement = paramValue?.ToString() ?? string.Empty;
                    result = result.Substring(0, start) + replacement + result.Substring(end + 1);
                    startIndex = start + replacement.Length;
                }
                else
                {
                    startIndex = end + 1;
                }
            }

            return result;
        }

        private async Task ExtractTemplateReferencesAsync(
            IReadOnlyDictionary<string, object> data,
            HashSet<uint> references,
            CancellationToken cancellationToken)
        {
            foreach (var kvp in data)
            {
                if (kvp.Value is uint uintValue)
                {
                    // Assume uint values might be template references
                    references.Add(uintValue);
                }
                else if (kvp.Value is IDictionary<string, object> nestedDict)
                {
                    // Convert to readonly dictionary for recursion
                    var readOnlyNestedDict = nestedDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    await ExtractTemplateReferencesAsync(readOnlyNestedDict, references, cancellationToken);
                }
            }

            await Task.CompletedTask;
        }

        private byte[] SerializeToBytes()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream, Encoding.UTF8);

            // Write template header
            writer.Write(TemplateName);
            writer.Write(ParentTemplateId ?? 0);
            writer.Write(IsInheritable);

            // Write parameters
            writer.Write(_parameters.Count);
            foreach (var parameter in _parameters.Values)
            {
                WriteParameter(writer, parameter);
            }

            // Write template data
            writer.Write(_rawTemplateData.Count);
            foreach (var kvp in _rawTemplateData)
            {
                writer.Write(kvp.Key);
                WriteValue(writer, kvp.Value);
            }

            return memoryStream.ToArray();
        }

        private void OnResourceChanged()
        {
            ResourceChanged?.Invoke(this, EventArgs.Empty);
        }

        private static void WriteParameter(BinaryWriter writer, TemplateParameter parameter)
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
                default:
                    writer.Write((byte)0);
                    writer.Write(value.ToString() ?? string.Empty);
                    break;
            }
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
