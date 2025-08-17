# ADR-015: Logging and Observability Framework

**Status:** Proposed
**Date:** August 17, 2025
**Deciders:** Architecture Team, Operations Team, Senior Developers

## Context

The TS4Tools application processes large Sims 4 package files and performs complex resource operations that require comprehensive observability for:

1. **Debugging Production Issues**: Understanding failures in user environments
2. **Performance Monitoring**: Tracking operation performance and bottlenecks
3. **Usage Analytics**: Understanding how users interact with the application
4. **Security Monitoring**: Detecting potential security issues or misuse
5. **Operational Health**: Monitoring application health and resource usage

The current logging implementation is minimal and inconsistent, making production troubleshooting difficult and providing no insight into application performance or usage patterns.

## Decision

We will implement a **comprehensive logging and observability framework** with the following components:

1. **Structured Logging**: JSON-based structured logging with consistent schema
2. **Performance Monitoring**: Automatic tracking of operation durations and resource usage
3. **Correlation Tracking**: Request/operation correlation across the entire application
4. **Configurable Outputs**: File, console, and remote logging sinks
5. **Privacy-First Design**: No sensitive data logging with configurable data sanitization

## Rationale

### Current Problems

#### Insufficient Logging
- Minimal logging throughout the application
- No structured data for analysis
- No performance metrics collection
- No correlation between related operations

#### Poor Troubleshooting
- Generic error messages without context
- No visibility into operation performance
- Difficult to trace issues across components
- No proactive monitoring or alerting

#### No Operational Insight
- No understanding of feature usage
- No performance baselines or trends
- No capacity planning data
- No user behavior analytics

### Benefits of Comprehensive Observability

#### Improved Reliability
- Proactive issue detection and alerting
- Faster mean time to resolution (MTTR)
- Better understanding of failure patterns
- Performance regression detection

#### Better User Experience
- Performance optimization based on real usage
- Feature usage insights for prioritization
- Proactive issue communication
- Data-driven UX improvements

#### Operational Excellence
- Capacity planning and resource optimization
- SLA monitoring and reporting
- Incident response and post-mortem analysis
- Compliance and audit requirements

## Architecture Design

### Logging Framework Stack

```csharp
// Primary logging with Serilog for flexibility and performance
services.AddSerilog((serviceProvider, configuration) =>
{
    var appConfig = serviceProvider.GetRequiredService<IConfiguration>();
    
    configuration
        .ReadFrom.Configuration(appConfig)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TS4Tools")
        .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString())
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentUserName()
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.File(
            path: "logs/ts4tools-.json",
            formatter: new JsonFormatter(),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30)
        .WriteTo.Conditional(
            condition: evt => appConfig.GetValue<bool>("Logging:RemoteEnabled"),
            configureSink: sink => sink.Http(
                requestUri: appConfig.GetValue<string>("Logging:RemoteEndpoint")!,
                queueLimitBytes: 50_000_000));
});
```

### Structured Logging Schema

```csharp
public static class LogEventProperties
{
    // Operation tracking
    public const string OperationId = "OperationId";
    public const string OperationType = "OperationType";
    public const string Duration = "Duration";
    public const string Success = "Success";

    // Package operations
    public const string PackagePath = "PackagePath";
    public const string PackageSize = "PackageSize";
    public const string ResourceCount = "ResourceCount";

    // Resource operations
    public const string ResourceType = "ResourceType";
    public const string ResourceKey = "ResourceKey";
    public const string ResourceSize = "ResourceSize";

    // Performance metrics
    public const string MemoryUsed = "MemoryUsed";
    public const string ProcessingTime = "ProcessingTime";
    public const string ThreadId = "ThreadId";

    // User context (privacy-safe)
    public const string UserAction = "UserAction";
    public const string FeatureUsed = "FeatureUsed";
    public const string SessionId = "SessionId";
}

// Structured logging extensions
public static class LoggerExtensions
{
    public static IDisposable BeginOperation(this ILogger logger, string operationType, string? operationId = null)
    {
        operationId ??= Guid.NewGuid().ToString();
        
        return logger.BeginScope(new Dictionary<string, object>
        {
            [LogEventProperties.OperationId] = operationId,
            [LogEventProperties.OperationType] = operationType
        });
    }

    public static void LogPackageOperation(this ILogger logger, LogLevel level, string message, 
        string packagePath, long? packageSize = null, int? resourceCount = null, Exception? exception = null)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            [LogEventProperties.PackagePath] = Path.GetFileName(packagePath), // Privacy: filename only
            [LogEventProperties.PackageSize] = packageSize ?? 0,
            [LogEventProperties.ResourceCount] = resourceCount ?? 0
        });

        logger.Log(level, exception, message);
    }

    public static void LogPerformanceMetric(this ILogger logger, string operation, TimeSpan duration, 
        long? memoryUsed = null, bool success = true)
    {
        logger.LogInformation("Performance metric: {Operation} completed in {Duration}ms with {MemoryUsed} bytes memory",
            operation, duration.TotalMilliseconds, memoryUsed ?? 0);

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            [LogEventProperties.OperationType] = operation,
            [LogEventProperties.Duration] = duration.TotalMilliseconds,
            [LogEventProperties.MemoryUsed] = memoryUsed ?? 0,
            [LogEventProperties.Success] = success
        });
    }
}
```

### Performance Monitoring Integration

```csharp
public interface IPerformanceMonitor
{
    IDisposable StartOperation(string operationName, Dictionary<string, object>? context = null);
    void RecordMetric(string metricName, double value, Dictionary<string, object>? tags = null);
    void RecordMemoryUsage(string operation, long bytes);
    Task<PerformanceReport> GenerateReportAsync(TimeSpan period);
}

public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly IMetricsCollector _metricsCollector;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger, IMetricsCollector metricsCollector)
    {
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public IDisposable StartOperation(string operationName, Dictionary<string, object>? context = null)
    {
        return new PerformanceOperation(operationName, _logger, _metricsCollector, context);
    }

    public void RecordMetric(string metricName, double value, Dictionary<string, object>? tags = null)
    {
        _metricsCollector.RecordValue(metricName, value, tags);
        
        _logger.LogDebug("Metric recorded: {MetricName} = {Value}", metricName, value);
    }

    public void RecordMemoryUsage(string operation, long bytes)
    {
        _metricsCollector.RecordValue("memory_usage", bytes, new Dictionary<string, object>
        {
            ["operation"] = operation
        });

        _logger.LogDebug("Memory usage recorded: {Operation} used {Bytes} bytes", operation, bytes);
    }

    public async Task<PerformanceReport> GenerateReportAsync(TimeSpan period)
    {
        return await _metricsCollector.GenerateReportAsync(period);
    }
}

public class PerformanceOperation : IDisposable
{
    private readonly string _operationName;
    private readonly ILogger _logger;
    private readonly IMetricsCollector _metricsCollector;
    private readonly Dictionary<string, object>? _context;
    private readonly Stopwatch _stopwatch;
    private readonly long _startMemory;
    private bool _disposed;

    public PerformanceOperation(string operationName, ILogger logger, IMetricsCollector metricsCollector, 
        Dictionary<string, object>? context)
    {
        _operationName = operationName;
        _logger = logger;
        _metricsCollector = metricsCollector;
        _context = context;
        _stopwatch = Stopwatch.StartNew();
        _startMemory = GC.GetTotalMemory(false);

        _logger.LogDebug("Started operation: {OperationName}", operationName);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _stopwatch.Stop();
        var endMemory = GC.GetTotalMemory(false);
        var memoryDelta = endMemory - _startMemory;

        var tags = new Dictionary<string, object>
        {
            ["operation"] = _operationName
        };

        if (_context != null)
        {
            foreach (var kvp in _context)
            {
                tags[kvp.Key] = kvp.Value;
            }
        }

        _metricsCollector.RecordValue("operation_duration", _stopwatch.Elapsed.TotalMilliseconds, tags);
        _metricsCollector.RecordValue("operation_memory_delta", memoryDelta, tags);

        _logger.LogDebug("Completed operation: {OperationName} in {Duration}ms, memory delta: {MemoryDelta} bytes",
            _operationName, _stopwatch.Elapsed.TotalMilliseconds, memoryDelta);

        _disposed = true;
    }
}
```

### Correlation and Context Tracking

```csharp
public interface ICorrelationContext
{
    string CorrelationId { get; }
    string? UserId { get; }
    string? SessionId { get; }
    Dictionary<string, object> Properties { get; }
    IDisposable BeginScope(string operationType);
}

public class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; } = Guid.NewGuid().ToString();
    public string? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public Dictionary<string, object> Properties { get; } = new();

    public void SetUser(string userId)
    {
        UserId = userId;
        Properties["UserId"] = userId;
    }

    public void SetSession(string sessionId)
    {
        SessionId = sessionId;
        Properties["SessionId"] = sessionId;
    }

    public IDisposable BeginScope(string operationType)
    {
        Properties["OperationType"] = operationType;
        Properties["ScopeStart"] = DateTimeOffset.UtcNow;
        
        return new OperationScope(this, operationType);
    }
}

public class OperationScope : IDisposable
{
    private readonly ICorrelationContext _context;
    private readonly string _operationType;
    private bool _disposed;

    public OperationScope(ICorrelationContext context, string operationType)
    {
        _context = context;
        _operationType = operationType;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _context.Properties.Remove("OperationType");
        _context.Properties.Remove("ScopeStart");
        _disposed = true;
    }
}
```

### Privacy and Data Sanitization

```csharp
public interface IDataSanitizer
{
    string SanitizePath(string path);
    string SanitizeUserData(string data);
    Dictionary<string, object> SanitizeLogContext(Dictionary<string, object> context);
}

public class DataSanitizer : IDataSanitizer
{
    private readonly DataSanitizationOptions _options;

    public DataSanitizer(IOptions<DataSanitizationOptions> options)
    {
        _options = options.Value;
    }

    public string SanitizePath(string path)
    {
        if (!_options.IncludeFilePaths)
            return "[REDACTED_PATH]";

        // Return filename only, not full path
        return Path.GetFileName(path);
    }

    public string SanitizeUserData(string data)
    {
        if (!_options.IncludeUserData)
            return "[REDACTED]";

        // Hash or truncate sensitive data
        if (data.Length > _options.MaxUserDataLength)
            return data[.._options.MaxUserDataLength] + "...";

        return data;
    }

    public Dictionary<string, object> SanitizeLogContext(Dictionary<string, object> context)
    {
        var sanitized = new Dictionary<string, object>();

        foreach (var kvp in context)
        {
            sanitized[kvp.Key] = kvp.Key.ToLowerInvariant() switch
            {
                "path" or "filepath" or "packagepath" => SanitizePath(kvp.Value.ToString() ?? ""),
                "username" or "userid" => SanitizeUserData(kvp.Value.ToString() ?? ""),
                _ => kvp.Value
            };
        }

        return sanitized;
    }
}

public class DataSanitizationOptions
{
    public bool IncludeFilePaths { get; set; } = true;
    public bool IncludeUserData { get; set; } = false;
    public int MaxUserDataLength { get; set; } = 50;
    public List<string> SensitiveKeys { get; set; } = new() { "password", "token", "key", "secret" };
}
```

### Configuration Schema

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TS4Tools": "Debug",
      "Microsoft": "Warning",
      "System": "Warning"
    },
    "File": {
      "Enabled": true,
      "Path": "logs/ts4tools-.json",
      "RetainedFileCount": 30,
      "FileSizeLimitMB": 100
    },
    "Console": {
      "Enabled": true,
      "IncludeScopes": true
    },
    "Remote": {
      "Enabled": false,
      "Endpoint": "https://logs.example.com/api/logs",
      "ApiKey": "your-api-key-here",
      "BatchSize": 100,
      "BufferSize": 10000
    },
    "Performance": {
      "Enabled": true,
      "MinDurationMs": 100,
      "IncludeMemoryMetrics": true
    },
    "Privacy": {
      "IncludeFilePaths": true,
      "IncludeUserData": false,
      "MaxUserDataLength": 50
    }
  }
}
```

## Implementation Guidelines

### Logging Best Practices

```csharp
// Good: Structured logging with context
using var operation = _performanceMonitor.StartOperation("PackageLoad", new Dictionary<string, object>
{
    ["PackageSize"] = fileInfo.Length,
    ["UserId"] = _correlationContext.UserId
});

_logger.LogInformation("Loading package {PackageName} of size {PackageSize} bytes",
    dataSanitizer.SanitizePath(packagePath),
    fileInfo.Length);

try
{
    var package = await LoadPackageAsync(packagePath);
    
    _logger.LogInformation("Successfully loaded package {PackageName} with {ResourceCount} resources",
        dataSanitizer.SanitizePath(packagePath),
        package.Resources.Count);

    return package;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to load package {PackageName}",
        dataSanitizer.SanitizePath(packagePath));
    throw;
}

// Good: Performance tracking
using var _ = _logger.BeginOperation("ResourceExtraction");
var stopwatch = Stopwatch.StartNew();

try
{
    var result = ExtractResource(resourceKey);
    
    _logger.LogPerformanceMetric("ResourceExtraction", stopwatch.Elapsed, 
        GC.GetTotalMemory(false), success: true);
    
    return result;
}
catch (Exception ex)
{
    _logger.LogPerformanceMetric("ResourceExtraction", stopwatch.Elapsed, 
        GC.GetTotalMemory(false), success: false);
    throw;
}
```

### Service Registration

```csharp
public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        // Core logging
        services.AddSerilog();
        
        // Performance monitoring
        services.Configure<PerformanceMonitorOptions>(configuration.GetSection("Logging:Performance"));
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
        services.AddScoped<IPerformanceMonitor, PerformanceMonitor>();
        
        // Correlation and context
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        
        // Privacy and sanitization
        services.Configure<DataSanitizationOptions>(configuration.GetSection("Logging:Privacy"));
        services.AddSingleton<IDataSanitizer, DataSanitizer>();

        return services;
    }
}
```

## Migration Strategy

### Phase 1: Foundation Setup (Week 1)
1. Install and configure Serilog with structured logging
2. Implement basic performance monitoring infrastructure
3. Create correlation context and sanitization services
4. Update service registration and configuration

### Phase 2: Core Integration (Week 2)
1. Add logging to package operations
2. Implement performance tracking for resource operations
3. Add correlation tracking across operation boundaries
4. Create basic monitoring dashboard

### Phase 3: Advanced Features (Week 3)
1. Implement remote logging capabilities
2. Add comprehensive performance metrics
3. Create alerting and monitoring rules
4. Add user behavior analytics (privacy-safe)

### Phase 4: Optimization and Monitoring (Week 4)
1. Performance optimization based on metrics
2. Fine-tune logging levels and sampling
3. Implement log analysis and trending
4. Create operational runbooks and alerts

## Success Criteria

### Technical Metrics
- [ ] All critical operations have performance tracking
- [ ] Structured logging implemented across all components
- [ ] Log correlation works across operation boundaries
- [ ] Privacy-safe data sanitization in place

### Operational Metrics
- [ ] Mean time to detect (MTTD) issues < 5 minutes
- [ ] Mean time to resolution (MTTR) reduced by 50%
- [ ] Performance regression detection automated
- [ ] Proactive alerting for critical issues

### Privacy and Compliance
- [ ] No sensitive user data in logs
- [ ] Configurable data retention policies
- [ ] Audit trail for log access and modification
- [ ] GDPR/privacy compliance verification

## Consequences

### Positive
- **Improved Debugging**: Rich context for production issue resolution
- **Performance Optimization**: Data-driven performance improvements
- **Proactive Monitoring**: Early detection of issues and regressions
- **User Insights**: Understanding of feature usage and user behavior
- **Operational Excellence**: Better capacity planning and resource utilization

### Negative
- **Performance Overhead**: Additional CPU and memory usage for logging
- **Storage Requirements**: Increased disk space for log files
- **Complexity**: Additional configuration and maintenance overhead
- **Privacy Concerns**: Need for careful data handling and sanitization

### Mitigation Strategies
- Implement configurable log levels and sampling
- Use asynchronous logging to minimize performance impact
- Implement log rotation and cleanup policies
- Provide comprehensive privacy controls and data sanitization

## Related ADRs
- ADR-002: Dependency Injection (service registration patterns)
- ADR-014: Error Handling and Exception Strategy (error logging integration)
- ADR-013: Static Analysis and Code Quality (logging standards enforcement)
