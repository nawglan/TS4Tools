namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Represents the result of a catalog resource validation operation.
/// </summary>
public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors;
    private readonly List<ValidationWarning> _warnings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    public ValidationResult()
    {
        _errors = [];
        _warnings = [];
    }

    /// <summary>
    /// Gets a value indicating whether the validation was successful (no errors).
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Gets the collection of validation warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings => _warnings.AsReadOnly();

    /// <summary>
    /// Gets the total number of issues (errors + warnings).
    /// </summary>
    public int TotalIssues => _errors.Count + _warnings.Count;

    /// <summary>
    /// Adds a validation error to the result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="value">The value that caused the validation failure.</param>
    public void AddError(string message, string? propertyName = null, object? value = null)
    {
        _errors.Add(new ValidationError(message, propertyName, value));
    }

    /// <summary>
    /// Adds a validation warning to the result.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="propertyName">The name of the property that triggered the warning.</param>
    /// <param name="value">The value that caused the warning.</param>
    public void AddWarning(string message, string? propertyName = null, object? value = null)
    {
        _warnings.Add(new ValidationWarning(message, propertyName, value));
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">The validation result to merge.</param>
    public void Merge(ValidationResult other)
    {
        ArgumentNullException.ThrowIfNull(other);

        _errors.AddRange(other._errors);
        _warnings.AddRange(other._warnings);
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result with no errors or warnings.</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a validation result with a single error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="value">The value that caused the validation failure.</param>
    /// <returns>A validation result containing the specified error.</returns>
    public static ValidationResult Error(string message, string? propertyName = null, object? value = null)
    {
        var result = new ValidationResult();
        result.AddError(message, propertyName, value);
        return result;
    }
}

/// <summary>
/// Represents a validation error.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="PropertyName">The name of the property that failed validation.</param>
/// <param name="Value">The value that caused the validation failure.</param>
public sealed record ValidationError(string Message, string? PropertyName = null, object? Value = null);

/// <summary>
/// Represents a validation warning.
/// </summary>
/// <param name="Message">The warning message.</param>
/// <param name="PropertyName">The name of the property that triggered the warning.</param>
/// <param name="Value">The value that caused the warning.</param>
public sealed record ValidationWarning(string Message, string? PropertyName = null, object? Value = null);

/// <summary>
/// Interface for catalog resource validation rules.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Gets the name of the validation rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Gets the description of what this rule validates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Validates the specified catalog resource.
    /// </summary>
    /// <param name="resource">The catalog resource to validate.</param>
    /// <param name="cancellationToken">Token to cancel the validation operation.</param>
    /// <returns>A task containing the validation result.</returns>
    Task<ValidationResult> ValidateAsync(ICatalogResource resource, CancellationToken cancellationToken = default);
}
