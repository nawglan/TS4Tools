# ADR-014: Error Handling and Exception Strategy

**Status:** Proposed
**Date:** August 17, 2025
**Deciders:** Architecture Team, Senior Developers

## Context

The TS4Tools application handles complex file operations, package parsing, and resource management that can fail in numerous ways. The current codebase lacks a consistent error handling strategy, leading to:

1. **Inconsistent Error Responses**: Different modules handle errors differently
1. **Poor User Experience**: Generic error messages that don't help users
1. **Debugging Difficulties**: Insufficient error context for troubleshooting
1. **Recovery Challenges**: No clear recovery mechanisms for transient failures
1. **Security Risks**: Error messages potentially exposing sensitive information

A consistent error handling strategy is critical for application reliability, user experience, and maintainability.

## Decision

We will implement a **comprehensive error handling and exception strategy** with the following components:

1. **Hierarchical Exception Design**: Domain-specific exceptions with consistent base classes
1. **Error Context Preservation**: Rich error information without security exposure
1. **Recovery Mechanisms**: Automatic retry and fallback strategies where appropriate
1. **User-Friendly Messaging**: Separate technical and user-facing error information
1. **Centralized Error Handling**: Consistent error processing across all application layers

## Rationale

### Current Problems

#### Inconsistent Error Handling

- Some methods throw exceptions, others return null/false
- No standard pattern for error information
- Mixed use of generic vs specific exceptions

#### Poor Error Context

- Stack traces without business context
- No correlation IDs for tracking errors across operations
- Missing information about operation state when errors occur

#### User Experience Issues

- Technical exceptions shown to users
- No actionable guidance for error resolution
- Errors not localized or user-friendly

### Benefits of Structured Approach

#### Consistency

- Predictable error handling patterns across all modules
- Standard error information structure
- Consistent logging and reporting

#### Maintainability

- Clear error handling responsibilities
- Easier debugging with rich context
- Standardized error testing patterns

#### User Experience

- User-friendly error messages
- Actionable error guidance
- Graceful degradation for non-critical failures

## Architecture Design

### Exception Hierarchy

```csharp
// Base exception for all TS4Tools exceptions
public abstract class TS4ToolsException : Exception
{
    public string OperationId { get; }
    public string Component { get; }
    public ErrorSeverity Severity { get; }
    public Dictionary<string, object> Context { get; }

    protected TS4ToolsException(
        string message,
        string component,
        ErrorSeverity severity = ErrorSeverity.Error,
        Exception? innerException = null)
        : base(message, innerException)
    {
        OperationId = Guid.NewGuid().ToString();
        Component = component;
        Severity = severity;
        Context = new Dictionary<string, object>();
    }

    public virtual string GetUserFriendlyMessage()
    {
        return "An error occurred while processing your request.";
    }
}

// Domain-specific exceptions
public class PackageException : TS4ToolsException
{
    public string? PackagePath { get; }
    public long? FileSize { get; }

    public PackageException(string message, string? packagePath = null)
        : base(message, "Package", ErrorSeverity.Error)
    {
        PackagePath = packagePath;
        if (packagePath != null && File.Exists(packagePath))
        {
            FileSize = new FileInfo(packagePath).Length;
            Context["PackagePath"] = packagePath;
            Context["FileSize"] = FileSize;
        }
    }

    public override string GetUserFriendlyMessage()
    {
        return PackagePath != null
            ? $"Unable to process package file: {Path.GetFileName(PackagePath)}"
            : "Unable to process package file.";
    }
}

public class ResourceException : TS4ToolsException
{
    public ResourceKey? ResourceKey { get; }
    public string? ResourceType { get; }

    public ResourceException(string message, ResourceKey? resourceKey = null, string? resourceType = null)
        : base(message, "Resource", ErrorSeverity.Error)
    {
        ResourceKey = resourceKey;
        ResourceType = resourceType;
        if (resourceKey != null)
        {
            Context["ResourceKey"] = resourceKey.ToString();
        }
        if (resourceType != null)
        {
            Context["ResourceType"] = resourceType;
        }
    }

    public override string GetUserFriendlyMessage()
    {
        return ResourceType != null
            ? $"Unable to process {ResourceType} resource."
            : "Unable to process resource.";
    }
}

public class ConfigurationException : TS4ToolsException
{
    public string? ConfigurationKey { get; }

    public ConfigurationException(string message, string? configurationKey = null)
        : base(message, "Configuration", ErrorSeverity.Critical)
    {
        ConfigurationKey = configurationKey;
        if (configurationKey != null)
        {
            Context["ConfigurationKey"] = configurationKey;
        }
    }

    public override string GetUserFriendlyMessage()
    {
        return "Application configuration error. Please check your settings.";
    }
}
```

### Error Severity Levels

```csharp
public enum ErrorSeverity
{
    /// <summary>
    /// Informational message, operation continues
    /// </summary>
    Information = 0,

    /// <summary>
    /// Warning condition, operation continues with potential issues
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error condition, current operation fails but application continues
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical error, application functionality significantly impaired
    /// </summary>
    Critical = 3,

    /// <summary>
    /// Fatal error, application cannot continue
    /// </summary>
    Fatal = 4
}
```

### Result Pattern for Non-Exception Cases

```csharp
// Generic result pattern for operations that can fail
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorSeverity Severity { get; }
    public Dictionary<string, object>? Context { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Severity = ErrorSeverity.Information;
    }

    private Result(string error, ErrorSeverity severity = ErrorSeverity.Error, Dictionary<string, object>? context = null)
    {
        IsSuccess = false;
        Error = error;
        Severity = severity;
        Context = context;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, ErrorSeverity severity = ErrorSeverity.Error) => new(error, severity);

    public static implicit operator Result<T>(T value) => Success(value);
}

// Non-generic result for operations that don't return values
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ErrorSeverity Severity { get; }

    private Result(bool isSuccess, string? error = null, ErrorSeverity severity = ErrorSeverity.Information)
    {
        IsSuccess = isSuccess;
        Error = error;
        Severity = severity;
    }

    public static Result Success() => new(true);
    public static Result Failure(string error, ErrorSeverity severity = ErrorSeverity.Error) => new(false, error, severity);
}
```

### Centralized Error Handling Service

```csharp
public interface IErrorHandler
{
    Task<Result> HandleAsync(Exception exception, string operation, CancellationToken cancellationToken = default);
    Result<T> Handle<T>(Func<T> operation, string operationName);
    Task<Result<T>> HandleAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default);
    void ReportError(TS4ToolsException exception);
}

public class ErrorHandler : IErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;
    private readonly IErrorReporter _errorReporter;

    public ErrorHandler(ILogger<ErrorHandler> logger, IErrorReporter errorReporter)
    {
        _logger = logger;
        _errorReporter = errorReporter;
    }

    public async Task<Result> HandleAsync(Exception exception, string operation, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString();

        _logger.LogError(exception, 
            "Operation {Operation} failed. OperationId: {OperationId}", 
            operation, operationId);

        if (exception is TS4ToolsException ts4Exception)
        {
            await _errorReporter.ReportAsync(ts4Exception, cancellationToken);
            return Result.Failure(ts4Exception.GetUserFriendlyMessage(), ts4Exception.Severity);
        }

        // Handle unexpected exceptions
        var wrappedException = new UnexpectedErrorException(
            $"Unexpected error in operation: {operation}", 
            exception);

        await _errorReporter.ReportAsync(wrappedException, cancellationToken);
        return Result.Failure("An unexpected error occurred. Please try again.", ErrorSeverity.Error);
    }

    public Result<T> Handle<T>(Func<T> operation, string operationName)
    {
        try
        {
            var result = operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            // Use Task.Run to avoid deadlocks when calling async from sync context
            var handleResult = Task.Run(async () => await HandleAsync(ex, operationName).ConfigureAwait(false)).GetAwaiter().GetResult();
            return Result<T>.Failure(handleResult.Error!, handleResult.Severity);
        }
    }

    public async Task<Result<T>> HandleAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            var handleResult = await HandleAsync(ex, operationName, cancellationToken);
            return Result<T>.Failure(handleResult.Error!, handleResult.Severity);
        }
    }

    public void ReportError(TS4ToolsException exception)
    {
        _logger.LogError(exception, 
            "Error in component {Component}. OperationId: {OperationId}, Severity: {Severity}", 
            exception.Component, exception.OperationId, exception.Severity);

        _ = Task.Run(async () => await _errorReporter.ReportAsync(exception));
    }
}
```

### Retry and Recovery Mechanisms

```csharp
public interface IRetryPolicy
{
    Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default);
}

public class RetryPolicy : IRetryPolicy
{
    private readonly ILogger<RetryPolicy> _logger;
    private readonly RetryPolicyOptions _options;

    public RetryPolicy(ILogger<RetryPolicy> logger, IOptions<RetryPolicyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        var maxAttempts = _options.MaxAttempts;
        var baseDelay = _options.BaseDelay;

        while (attempts < maxAttempts)
        {
            attempts++;

            try
            {
                var result = await operation();
                return Result<T>.Success(result);
            }
            catch (Exception ex) when (IsRetriableException(ex) && attempts < maxAttempts)
            {
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempts - 1));
                
                _logger.LogWarning(ex, 
                    "Operation {OperationName} failed on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms", 
                    operationName, attempts, maxAttempts, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Operation {OperationName} failed permanently after {Attempts} attempts", 
                    operationName, attempts);

                return Result<T>.Failure($"Operation failed after {attempts} attempts: {ex.Message}");
            }
        }

        return Result<T>.Failure($"Operation failed after {maxAttempts} attempts");
    }

    private static bool IsRetriableException(Exception exception)
    {
        return exception switch
        {
            IOException => true,
            TimeoutException => true,
            HttpRequestException => true,
            PackageException pkg when pkg.Severity <= ErrorSeverity.Warning => true,
            _ => false
        };
    }
}

public class RetryPolicyOptions
{
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
}
```

## Implementation Guidelines

### Exception Throwing Rules

1. **Use specific exceptions** for known error conditions
1. **Include context** relevant to troubleshooting
1. **Don't expose sensitive information** in exception messages
1. **Use Result pattern** for expected failure cases
1. **Log before throwing** critical exceptions

### Error Handling Patterns

```csharp
// Good: Specific exception with context
public async Task<Package> LoadPackageAsync(string path)
{
    if (string.IsNullOrEmpty(path))
        throw new ArgumentException("Package path cannot be null or empty", nameof(path));

    if (!File.Exists(path))
        throw new PackageException($"Package file not found", path);

    try
    {
        // Package loading logic
        return await LoadPackageInternalAsync(path);
    }
    catch (Exception ex) when (!(ex is PackageException))
    {
        throw new PackageException($"Failed to load package due to unexpected error", path, ex);
    }
}

// Good: Result pattern for expected failures
public Result<ResourceData> ParseResourceData(byte[] data, ResourceKey key)
{
    if (data == null || data.Length == 0)
        return Result<ResourceData>.Failure("Resource data is empty", ErrorSeverity.Warning);

    try
    {
        var resource = ParseResourceInternal(data, key);
        return Result<ResourceData>.Success(resource);
    }
    catch (FormatException ex)
    {
        return Result<ResourceData>.Failure($"Invalid resource format: {ex.Message}", ErrorSeverity.Error);
    }
}
```

### Service Registration

```csharp
public static class ErrorHandlingServiceExtensions
{
    public static IServiceCollection AddErrorHandling(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RetryPolicyOptions>(configuration.GetSection("RetryPolicy"));
        services.AddScoped<IErrorHandler, ErrorHandler>();
        services.AddScoped<IRetryPolicy, RetryPolicy>();
        services.AddScoped<IErrorReporter, ErrorReporter>();

        return services;
    }
}
```

## Migration Strategy

### Phase 1: Foundation (Week 1)

1. Implement base exception hierarchy
1. Create error handling service interfaces
1. Add Result pattern infrastructure
1. Update DI registration

### Phase 2: Core Integration (Week 2-3)

1. Convert Package operations to use new error handling
1. Update Resource operations
1. Implement retry policies for appropriate operations
1. Add comprehensive error logging

### Phase 3: Application Integration (Week 4)

1. Update UI layers to handle Result patterns
1. Implement user-friendly error display
1. Add error reporting and telemetry
1. Update all unit tests

### Phase 4: Legacy Cleanup (Week 5)

1. Remove old error handling patterns
1. Ensure all public APIs use consistent error handling
1. Update documentation and examples
1. Performance optimization and monitoring

## Success Criteria

### Technical Metrics

- [ ] All public APIs use consistent error handling patterns
- [ ] Error handling unit test coverage > 90%
- [ ] All exceptions include relevant context information
- [ ] Error logging follows structured format

### User Experience Metrics

- [ ] User-friendly error messages for all error conditions
- [ ] Error recovery succeeds for transient failures
- [ ] Error reporting provides actionable guidance
- [ ] Application gracefully handles all error scenarios

### Operational Metrics

- [ ] Error correlation and tracking across operations
- [ ] Comprehensive error monitoring and alerting
- [ ] Error trend analysis and reporting
- [ ] Reduced support requests due to unclear errors

## Consequences

### Positive

- **Consistency**: Uniform error handling across application
- **Debuggability**: Rich error context for troubleshooting
- **User Experience**: Clear, actionable error messages
- **Reliability**: Automatic recovery for transient failures
- **Maintainability**: Standardized error testing patterns

### Negative

- **Initial Complexity**: More code required for error handling
- **Performance Overhead**: Additional logging and context creation
- **Learning Curve**: Team needs to adopt new patterns
- **Migration Effort**: Significant refactoring of existing code

### Mitigation Strategies

- Provide comprehensive documentation and examples
- Implement error handling utilities to reduce boilerplate
- Use code analyzers to enforce error handling patterns
- Create training materials for development team

## Related ADRs

- ADR-002: Dependency Injection (service registration)
- ADR-009: Testing Framework Standardization (error testing patterns)
- ADR-013: Static Analysis and Code Quality (error handling rules)
