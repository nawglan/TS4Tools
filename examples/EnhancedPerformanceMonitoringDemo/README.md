# Enhanced Plugin Performance Monitoring System

## Overview

The Enhanced Plugin Performance Monitoring System provides comprehensive performance analysis and diagnostics for the TS4Tools WrapperDealer plugin ecosystem. This system extends the existing basic monitoring with detailed profiling, trend analysis, health scoring, and automated performance issue detection.

## Key Features

### 🔍 **Comprehensive Performance Profiling**
- **Detailed Timing Analysis**: Tracks response times with percentile calculations (50th, 95th, 99th)
- **Memory Allocation Tracking**: Monitors memory usage patterns per plugin operation
- **Operation Type Breakdown**: Categorizes performance by operation type (wrapper creation, resource reading, etc.)
- **Resource Type Analysis**: Identifies performance hotspots by resource type (SimData, DST, GEOM, etc.)

### 📊 **Advanced Analytics**
- **Performance Trend Detection**: Identifies improving, stable, or degrading performance patterns
- **Plugin Health Scoring**: Comprehensive 0-100 health score based on multiple performance factors
- **Failure Rate Analysis**: Tracks and analyzes operation failure patterns
- **System-wide Metrics**: Aggregated performance statistics across all plugins

### 🚨 **Intelligent Alerting**
- **Real-time Issue Detection**: Immediate alerts for critical performance problems
- **Configurable Thresholds**: Customizable performance warning and error thresholds
- **Performance Recommendations**: Automated suggestions for optimization opportunities
- **Health Score Warnings**: Alerts when plugin health scores drop below acceptable levels

### 📈 **Rich Reporting & Export**
- **JSON Export**: Complete performance data export for external analysis tools
- **Performance Rankings**: Plugin health rankings for quick identification of issues
- **Resource Hotspot Identification**: Highlights resource types causing performance bottlenecks
- **Trend Visualization Ready**: Data structured for easy integration with charting tools

## Quick Start

### 1. Basic Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using TS4Tools.WrapperDealer.Monitoring;

// Configure services with enhanced monitoring
var services = new ServiceCollection();
services.AddEnhancedPerformanceMonitoring(PerformanceMonitoringProfile.Production);
var serviceProvider = services.BuildServiceProvider();

// Get the monitoring service
var monitoring = serviceProvider.GetRequiredService<IEnhancedPerformanceMonitoringService>();
```

### 2. Instrument Plugin Operations

```csharp
// Track a plugin operation with automatic performance measurement
var result = monitoring.TrackOperation(
    pluginName: "SimDataResourcePlugin",
    operationType: PluginOperationType.WrapperCreation,
    resourceType: "SimData",
    action: () => CreateSimDataWrapper());

// Track operations with void return
monitoring.TrackOperation(
    pluginName: "ImageResourcePlugin", 
    operationType: PluginOperationType.ResourceReading,
    resourceType: "DST",
    action: () => ReadImageData());
```

### 3. Analyze Performance Data

```csharp
// Get system-wide performance summary
var summary = monitoring.GetSystemSummary();
Console.WriteLine($"Success Rate: {summary.OverallSuccessRate:F1}%");
Console.WriteLine($"Avg Response Time: {summary.SystemAverageResponseTime:F1}ms");

// Get plugin-specific analysis
var pluginAnalysis = monitoring.GetPluginAnalysis("SimDataResourcePlugin");
if (pluginAnalysis != null)
{
    Console.WriteLine($"Plugin Operations: {pluginAnalysis.TotalOperations}");
    Console.WriteLine($"Failure Rate: {pluginAnalysis.FailureRate:F2}%");
    Console.WriteLine($"P95 Response Time: {pluginAnalysis.P95ResponseTime.TotalMilliseconds:F1}ms");
}

// Get plugin health rankings
var healthRankings = monitoring.Profiler.GetPluginHealthRankings();
foreach (var ranking in healthRankings.Take(5))
{
    Console.WriteLine($"{ranking.PluginName}: {ranking.HealthScore:F1}/100");
}
```

## Configuration Profiles

### Development Profile
- **High Detail**: Maximum data retention and detailed tracking
- **Low Thresholds**: Sensitive performance issue detection
- **Real-time Analysis**: Immediate performance feedback
- **Memory Tracking**: Full memory allocation monitoring

```csharp
services.AddEnhancedPerformanceMonitoring(PerformanceMonitoringProfile.Development);
```

### Production Profile  
- **Balanced Performance**: Optimized for production workloads
- **Reasonable Thresholds**: Production-appropriate performance expectations
- **Regular Analysis**: Periodic performance health checks
- **Essential Tracking**: Core metrics without excessive overhead

```csharp
services.AddEnhancedPerformanceMonitoring(PerformanceMonitoringProfile.Production);
```

### High Throughput Profile
- **Minimal Overhead**: Optimized for high-volume scenarios
- **Relaxed Thresholds**: Appropriate for high-load environments  
- **Reduced Tracking**: Essential metrics only
- **Efficient Storage**: Optimized memory usage

```csharp
services.AddEnhancedPerformanceMonitoring(PerformanceMonitoringProfile.HighThroughput);
```

### Custom Configuration

```csharp
services.AddEnhancedPerformanceMonitoring(options =>
{
    options.MaxSamplesPerPlugin = 25_000;
    options.AnalysisWindow = TimeSpan.FromHours(2);
    options.OperationTimeoutThreshold = TimeSpan.FromSeconds(3);
    options.FailureRateWarningThreshold = 0.05; // 5%
    options.EnableImmediateAnalysis = true;
});
```

## Performance Monitoring Aspects

Use the monitoring aspect pattern for automatic instrumentation:

```csharp
// Create monitored operation wrappers
var monitoredWrapper = monitoring.MonitoredOperation(
    "PluginName", 
    PluginOperationType.WrapperCreation,
    "ResourceType",
    () => CreateWrapper());

// Execute with automatic performance tracking
var result = monitoredWrapper();
```

## Integration with Existing WrapperDealer

The enhanced monitoring system seamlessly integrates with the existing WrapperDealer infrastructure:

```csharp
public class EnhancedWrapperDealer : IWrapperDealer
{
    private readonly IEnhancedPerformanceMonitoringService _monitoring;
    private readonly IOriginalWrapperDealer _originalDealer;

    public IResourceWrapper WrapperForType(uint resourceType)
    {
        return _monitoring.TrackOperation(
            pluginName: "WrapperDealer",
            operationType: PluginOperationType.WrapperCreation,
            resourceType: $"0x{resourceType:X8}",
            action: () => _originalDealer.WrapperForType(resourceType));
    }
}
```

## Background Monitoring Service

Set up continuous performance monitoring:

```csharp
public class PerformanceMonitoringBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var summary = _monitoringService.GetSystemSummary();
            
            // Log performance health
            _logger.LogInformation("System Health: {PluginCount} plugins, {SuccessRate:F1}% success",
                summary.TotalPlugins, summary.OverallSuccessRate);
            
            // Alert on critical issues
            foreach (var issue in summary.TopPerformanceIssues.Where(i => i.Severity == PerformanceIssueSeverity.Critical))
            {
                _logger.LogError("CRITICAL: {Plugin} - {Description}", issue.PluginName, issue.Description);
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## Export and Reporting

Export comprehensive performance data:

```csharp
// Export with raw data for detailed analysis
var exportData = monitoring.Profiler.ExportPerformanceData(includeRawData: true);

// Serialize to JSON for external tools
var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
{ 
    WriteIndented = true 
});

await File.WriteAllTextAsync("performance_report.json", json);
```

## Performance Impact

The enhanced monitoring system is designed for minimal performance impact:

- **Typical Overhead**: < 1ms per operation in production profiles
- **Memory Usage**: Configurable sample retention limits
- **Thread Safety**: Lock-free data collection for high-concurrency scenarios
- **Async Processing**: Background analysis doesn't block plugin operations

## Thread Safety

All components are fully thread-safe for concurrent plugin operations:

- **Lock-free Collection**: Performance data collection uses atomic operations
- **Concurrent Analysis**: Thread-safe analysis generation with caching
- **Safe Aggregation**: Concurrent access to summary statistics

## Best Practices

### 1. **Choose Appropriate Profiles**
- Use **Development** profile during plugin development and testing
- Use **Production** profile for live environments
- Use **HighThroughput** for high-volume batch processing
- Use **Minimal** for resource-constrained environments

### 2. **Monitor Key Metrics**
- Focus on **P95/P99 response times** rather than just averages
- Monitor **failure rate trends** for early problem detection
- Track **memory allocation patterns** for memory leaks
- Watch **health score trends** for gradual performance degradation

### 3. **Set Meaningful Thresholds**
- Adjust **timeout thresholds** based on expected operation complexity
- Set **failure rate thresholds** appropriate for your quality standards
- Configure **memory thresholds** based on available system resources

### 4. **Regular Health Checks**
- Implement **automated health monitoring** with the background service
- Set up **alerting** for critical performance issues
- Schedule **regular performance reviews** using exported data

### 5. **Performance Optimization**
- Use monitoring data to **identify bottlenecks** in plugin operations
- Focus optimization efforts on **highest-impact issues** (high volume + slow operations)
- Monitor **performance improvements** after optimization changes

## Architecture Integration

The enhanced monitoring system integrates with existing TS4Tools architecture:

```
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────┐
│   WrapperDealer │────│  Enhanced Monitoring │────│ Plugin Profiler │
│                 │    │      Service         │    │                 │
└─────────────────┘    └──────────────────────┘    └─────────────────┘
         │                        │                          │
         │                        │                          │
         ▼                        ▼                          ▼
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────┐
│ Plugin Registry │    │  Performance Models  │    │ Analysis Engine │
│                 │    │                      │    │                 │
└─────────────────┘    └──────────────────────┘    └─────────────────┘
```

This enhanced performance monitoring system provides the comprehensive observability needed to maintain and optimize a complex plugin ecosystem while preserving the high-performance characteristics of the TS4Tools platform.
