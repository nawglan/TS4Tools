# WrapperDealer Performance Monitoring

This document describes the implementation and usage of the WrapperDealer performance monitoring system introduced in Phase 4.20.4.

## Overview

The WrapperDealer performance monitoring system provides comprehensive performance tracking and telemetry for all WrapperDealer operations. It's designed to:

- Monitor operation performance and success rates
- Detect performance regressions and anomalies
- Provide telemetry for optimization efforts
- Support legacy plugin compatibility while tracking performance impact

## Architecture

The monitoring system consists of several key components:

### Core Interfaces

- **`IWrapperDealerMetrics`**: Main interface for recording performance metrics
- **`ITelemetryService`**: Interface for reporting metrics to external systems

### Implementation Classes

- **`WrapperDealerMetrics`**: Thread-safe metrics collection with statistical analysis
- **`DefaultTelemetryService`**: Basic telemetry service that logs metrics
- **`WrapperDealerTelemetryService`**: Background service for automatic reporting

### Configuration

- **`TelemetryOptions`**: Configuration options for telemetry behavior
- **`ServiceCollectionExtensions`**: Dependency injection setup helpers

## Usage

### Basic Setup

```csharp
// In your application startup (Program.cs or Startup.cs)
var services = new ServiceCollection();

// Add monitoring with configuration
services.AddWrapperDealerMonitoring(options =>
{
    options.Enabled = true;
    options.ReportingInterval = TimeSpan.FromMinutes(5);
    options.ImmediateWarningReporting = true;
    options.IncludeResourceTypeNames = false; // For privacy
});

var serviceProvider = services.BuildServiceProvider();

// Initialize instrumentation
var metrics = serviceProvider.GetRequiredService<IWrapperDealerMetrics>();
WrapperDealerInstrumentation.Initialize(metrics);
```

### Configuration from appsettings.json

```json
{
  "WrapperDealer": {
    "Telemetry": {
      "Enabled": true,
      "ReportingInterval": "00:05:00",
      "ImmediateWarningReporting": true,
      "ApplicationInsightsConnectionString": null,
      "OpenTelemetryEndpoint": null,
      "CustomTelemetryEndpoint": null,
      "BatchSize": 100,
      "IncludeResourceTypeNames": false
    }
  }
}
```

### Recording Metrics

#### Manual Recording

```csharp
var metrics = serviceProvider.GetRequiredService<IWrapperDealerMetrics>();

// Record a GetResource operation
var stopwatch = Stopwatch.StartNew();
var resource = GetResource("0x319E4F1D");
stopwatch.Stop();
metrics.RecordGetResourceOperation("0x319E4F1D", stopwatch.Elapsed, resource != null);

// Record a TypeMap lookup
var stopwatch2 = Stopwatch.StartNew();
var found = TypeMap.ContainsKey("0x319E4F1D");
stopwatch2.Stop();
metrics.RecordTypeMapLookup("0x319E4F1D", stopwatch2.Elapsed, found);
```

#### Instrumented Operations

```csharp
// Use instrumentation helpers for automatic tracking
var resource = WrapperDealerInstrumentation.InstrumentGetResource("0x319E4F1D", () =>
{
    // Your resource retrieval logic here
    return GetResourceFromCache("0x319E4F1D");
});

var found = WrapperDealerInstrumentation.InstrumentTypeMapLookup("0x319E4F1D", () =>
{
    // Your lookup logic here
    return TypeMap.ContainsKey("0x319E4F1D");
});
```

### Retrieving Performance Data

```csharp
var metrics = serviceProvider.GetRequiredService<IWrapperDealerMetrics>();

// Get statistics for a specific operation type
var getResourceStats = metrics.GetPerformanceStats(WrapperDealerOperationType.GetResource);
Console.WriteLine($"Average GetResource time: {getResourceStats.AverageDuration.TotalMilliseconds:F1}ms");
Console.WriteLine($"Success rate: {getResourceStats.SuccessRate:F1}%");

// Get comprehensive summary
var summary = metrics.GetPerformanceSummary();
Console.WriteLine($"Total operations: {summary.TotalOperations}");
Console.WriteLine($"Overall success rate: {summary.OverallSuccessRate:F1}%");

// Check for performance warnings
foreach (var warning in summary.PerformanceWarnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

## Operation Types

The system tracks the following operation types:

- **`GetResource`**: Resource retrieval operations
- **`CreateResource`**: Resource creation operations  
- **`TypeMapLookup`**: Type mapping lookups
- **`PluginLoad`**: Plugin loading operations
- **`WrapperRegistration`**: Wrapper type registration

## Performance Thresholds

The system includes built-in performance thresholds that trigger warnings:

- **GetResource**: > 50ms average
- **CreateResource**: > 100ms average
- **TypeMapLookup**: > 10ms average
- **PluginLoad**: > 5 seconds average
- **WrapperRegistration**: > 200ms average

## Statistics Collected

For each operation type, the system collects:

- **Total Operations**: Number of operations performed
- **Success/Failure Counts**: Operation success tracking
- **Duration Statistics**: Min, max, average, P95, P99 percentiles
- **Operations per Second**: Throughput metrics
- **Resource Type Usage**: Most frequently accessed resource types

## Performance Warnings

The system automatically detects and reports:

- **Low Success Rates**: < 95% success rate
- **Slow Operations**: Operations exceeding thresholds
- **Performance Outliers**: P99 duration > 5x average duration

## Custom Telemetry Services

You can implement custom telemetry services for integration with external monitoring systems:

```csharp
public class ApplicationInsightsTelemetryService : ITelemetryService
{
    public async Task ReportPerformanceMetricsAsync(WrapperDealerPerformanceSummary summary, CancellationToken cancellationToken)
    {
        // Send metrics to Application Insights
    }
    
    // Implement other interface methods...
}

// Register custom service
services.AddWrapperDealerMonitoring<ApplicationInsightsTelemetryService>(configuration);
```

## Integration Points

### WrapperDealer Class Integration

The monitoring system should be integrated into the main WrapperDealer class:

```csharp
public class WrapperDealer
{
    private readonly IWrapperDealerMetrics? _metrics;
    
    public WrapperDealer(IWrapperDealerMetrics? metrics = null)
    {
        _metrics = metrics;
    }
    
    public T? GetResource<T>(uint type, uint group, ulong instance) where T : class
    {
        if (_metrics != null)
        {
            return WrapperDealerInstrumentation.InstrumentGetResource($"0x{type:X8}", () =>
            {
                // Existing GetResource logic
                return InternalGetResource<T>(type, group, instance);
            });
        }
        
        return InternalGetResource<T>(type, group, instance);
    }
}
```

## Privacy Considerations

The monitoring system includes privacy controls:

- **`IncludeResourceTypeNames`**: When false, only counts are reported, not resource type names
- **Sensitive Data Filtering**: No user data or file paths are collected
- **Configurable Reporting**: All telemetry can be disabled via configuration

## Performance Impact

The monitoring system is designed for minimal performance impact:

- **Thread-Safe Collections**: Concurrent data structures for high-performance access
- **Batched Reporting**: Metrics are batched to reduce I/O overhead
- **Configurable Intervals**: Reporting frequency can be adjusted based on needs
- **Optional Instrumentation**: Can be completely disabled with near-zero overhead

## Troubleshooting

### High Memory Usage

If memory usage is high, consider:

- Reducing `ReportingInterval` to flush data more frequently
- Enabling `BatchSize` limiting for large-scale operations
- Disabling detailed resource type tracking

### Missing Metrics

Check that:

- Monitoring is enabled in configuration
- `WrapperDealerInstrumentation.Initialize()` was called
- Services are properly registered in DI container

### Performance Warnings

Performance warnings indicate potential optimization opportunities:

- Review operation implementations for efficiency
- Check for resource contention or I/O bottlenecks
- Consider caching frequently accessed resources

## Next Steps

This monitoring foundation supports future enhancements:

- **Real-time Dashboards**: Web-based performance monitoring
- **Automated Alerts**: Integration with monitoring platforms
- **Performance Regression Testing**: Automated detection of performance degradation
- **Optimization Recommendations**: AI-powered performance optimization suggestions
