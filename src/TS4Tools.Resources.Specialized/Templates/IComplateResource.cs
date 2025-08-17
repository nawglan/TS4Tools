using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Specialized.Templates
{
    /// <summary>
    /// Interface for template resources that support inheritance and instantiation.
    /// ComplateResource represents templated data that can be inherited and instantiated
    /// to create specific content instances.
    /// </summary>
    public interface IComplateResource : IResource
    {
        /// <summary>
        /// Gets the template name/identifier.
        /// </summary>
        string TemplateName { get; }

        /// <summary>
        /// Gets the parent template resource ID if this template inherits from another.
        /// Returns null if this is a root template.
        /// </summary>
        uint? ParentTemplateId { get; }

        /// <summary>
        /// Gets whether this template can be inherited by other templates.
        /// </summary>
        bool IsInheritable { get; }

        /// <summary>
        /// Gets whether this template has been validated and is ready for use.
        /// </summary>
        bool IsValidated { get; }

        /// <summary>
        /// Gets the collection of template parameters and their metadata.
        /// </summary>
        IReadOnlyDictionary<string, TemplateParameter> Parameters { get; }

        /// <summary>
        /// Gets the resolved template data after inheritance and parameter resolution.
        /// </summary>
        IReadOnlyDictionary<string, object> ResolvedData { get; }

        /// <summary>
        /// Validates the template structure and inheritance chain.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Validation result with any errors or warnings.</returns>
        Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates the template with the provided parameter values.
        /// </summary>
        /// <param name="parameterValues">Parameter values to use for instantiation.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The instantiated template data.</returns>
        Task<TemplateInstance> InstantiateAsync(
            IDictionary<string, object> parameterValues,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the template inheritance chain and merges data from parent templates.
        /// </summary>
        /// <param name="templateResolver">Resolver for loading parent templates.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the resolution operation.</returns>
        Task ResolveInheritanceAsync(
            ITemplateResolver templateResolver,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all template dependencies (parent templates and referenced templates).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Collection of template resource IDs that this template depends on.</returns>
        Task<IReadOnlyCollection<uint>> GetDependenciesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates a template parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="parameter">Parameter definition.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the operation.</returns>
        Task SetParameterAsync(
            string name,
            TemplateParameter parameter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a template parameter.
        /// </summary>
        /// <param name="name">Parameter name to remove.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the operation.</returns>
        Task RemoveParameterAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the template data before inheritance resolution (raw template data).
        /// </summary>
        IReadOnlyDictionary<string, object> RawTemplateData { get; }

        /// <summary>
        /// Updates the raw template data.
        /// </summary>
        /// <param name="templateData">New template data.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the operation.</returns>
        Task UpdateTemplateDataAsync(
            IDictionary<string, object> templateData,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a template parameter with type information and validation rules.
    /// </summary>
    public class TemplateParameter
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
        /// Gets or sets whether this parameter is required for template instantiation.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the parameter description for documentation.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets validation constraints for the parameter value.
        /// </summary>
        public IReadOnlyList<IParameterConstraint> Constraints { get; set; } = Array.Empty<IParameterConstraint>();
    }

    /// <summary>
    /// Represents an instantiated template with resolved parameter values.
    /// </summary>
    public class TemplateInstance
    {
        /// <summary>
        /// Gets or sets the template ID that was instantiated.
        /// </summary>
        public uint TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the template name.
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resolved parameter values used for instantiation.
        /// </summary>
        public IReadOnlyDictionary<string, object> ParameterValues { get; set; } =
            new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the final instantiated data.
        /// </summary>
        public IReadOnlyDictionary<string, object> InstanceData { get; set; } =
            new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the timestamp when this instance was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Interface for template resolution during inheritance processing.
    /// </summary>
    public interface ITemplateResolver
    {
        /// <summary>
        /// Resolves a template by its resource ID.
        /// </summary>
        /// <param name="templateId">Template resource ID.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The resolved template resource.</returns>
        Task<IComplateResource?> ResolveTemplateAsync(uint templateId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for parameter value constraints.
    /// </summary>
    public interface IParameterConstraint
    {
        /// <summary>
        /// Validates a parameter value against this constraint.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="parameterName">Name of the parameter being validated.</param>
        /// <returns>Validation result.</returns>
        ValidationResult Validate(object? value, string parameterName);
    }

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
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
        /// Creates a successful validation result.
        /// </summary>
        public static ValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with error messages.
        /// </summary>
        /// <param name="errors">Error messages.</param>
        public static ValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = errors
        };

        /// <summary>
        /// Creates a validation result with warnings.
        /// </summary>
        /// <param name="warnings">Warning messages.</param>
        public static ValidationResult WithWarnings(params string[] warnings) => new()
        {
            IsValid = true,
            Warnings = warnings
        };
    }
}
