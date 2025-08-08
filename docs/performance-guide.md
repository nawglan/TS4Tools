# TS4Tools Performance Guide

## Overview

TS4Tools is designed for high performance with modern .NET 9 features. This guide provides recommendations for optimizing performance in different scenarios.

## Memory Optimization

### `Span<T>` and `Memory<T>` Usage

TS4Tools leverages `Span<T>` and `Memory<T>` for zero-allocation operations:

```csharp
// ✅ Good - Zero allocation string hashing
ReadOnlySpan<char> resourceName = "MyResource".AsSpan();
var hash = FNVHash.Calculate(resourceName);

// ❌ Avoid - Creates substring allocations
var hash = FNVHash.Calculate(fullName.Substring(0, 10));

// ✅ Good - Slice without allocation  
ReadOnlySpan<char> slice = fullName.AsSpan(0, 10);
var hash = FNVHash.Calculate(slice);
```

### Efficient Resource Access

Choose the right access pattern based on resource size:

```csharp
// ✅ For large resources - use streams
using var stream = resource.Stream;
var buffer = new byte[8192];
int bytesRead;
while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
{
    ProcessChunk(buffer.AsSpan(0, bytesRead));
}

// ✅ For small resources - byte arrays are fine
if (resource.Stream.Length < 1024 * 1024) // < 1MB
{
    var data = resource.AsBytes();
    ProcessSmallResource(data);
}

// ❌ Avoid - Loading large resources into memory
var allBytes = resource.AsBytes(); // Could be gigabytes!
```

### Object Pooling

Use object pooling for frequently created objects:

```csharp
public class ResourceProcessorPool
{
    private readonly ObjectPool<ResourceProcessor> _pool;
    
    public ResourceProcessorPool(ObjectPool<ResourceProcessor> pool)
    {
        _pool = pool;
    }
    
    public async Task ProcessResourceAsync(IResource resource)
    {
        var processor = _pool.Get();
        try
        {
            await processor.ProcessAsync(resource);
        }
        finally
        {
            _pool.Return(processor);
        }
    }
}

// Registration
services.AddSingleton<ObjectPool<ResourceProcessor>>(provider =>
{
    var policy = new DefaultPooledObjectPolicy<ResourceProcessor>();
    return new DefaultObjectPool<ResourceProcessor>(policy);
});
```

## I/O Performance

### Async Best Practices

Always use async methods for I/O operations:

```csharp
// ✅ Good - Async file operations
var package = await packageFactory.LoadFromFileAsync(path, cancellationToken);
await package.SaveAsync(cancellationToken);

// ❌ Avoid - Blocking on async operations
var package = packageFactory.LoadFromFileAsync(path).Result; // Deadlock risk!

// ✅ Good - ConfigureAwait(false) in libraries
await fileStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
```

### Parallel Processing

Process multiple packages in parallel:

```csharp
public async Task ProcessPackagesAsync(IEnumerable<string> packagePaths)
{
    var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    var tasks = packagePaths.Select(async path =>
    {
        await semaphore.WaitAsync();
        try
        {
            await ProcessSinglePackageAsync(path);
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    await Task.WhenAll(tasks);
}
```

### Stream Optimization

Use appropriate buffer sizes and minimize stream operations:

```csharp
// ✅ Good - Optimal buffer size for your scenario
const int OptimalBufferSize = 64 * 1024; // 64KB
var buffer = new byte[OptimalBufferSize];

// ✅ Good - Buffered stream for many small operations
using var fileStream = File.OpenRead(packagePath);
using var bufferedStream = new BufferedStream(fileStream, OptimalBufferSize);

// ✅ Good - Minimize Position changes
stream.Seek(targetPosition, SeekOrigin.Begin);
var data = new byte[dataLength];
await stream.ReadExactlyAsync(data);
```

## Caching Strategies

### Resource Caching Configuration

Configure caching based on your usage patterns:

```json
{
  "TS4Tools": {
    "EnableResourceCaching": true,
    "MaxCacheSize": 5000,
    "ResourceCacheTimeout": "00:15:00"
  }
}
```

### Custom Cache Implementation

Implement specialized caching for your use case:

```csharp
public class LRUResourceCache
{
    private readonly int _maxSize;
    private readonly Dictionary<IResourceKey, CacheItem> _cache = new();
    private readonly LinkedList<IResourceKey> _accessOrder = new();
    private readonly object _lock = new();
    
    public LRUResourceCache(int maxSize)
    {
        _maxSize = maxSize;
    }
    
    public bool TryGet(IResourceKey key, out IResource? resource)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                // Move to front (most recently used)
                _accessOrder.Remove(item.Node);
                item.Node = _accessOrder.AddFirst(key);
                
                resource = item.Resource;
                return true;
            }
            
            resource = null;
            return false;
        }
    }
    
    public void Add(IResourceKey key, IResource resource)
    {
        lock (_lock)
        {
            if (_cache.ContainsKey(key))
                return;
            
            // Remove least recently used if at capacity
            while (_cache.Count >= _maxSize && _accessOrder.Last != null)
            {
                var lru = _accessOrder.Last.Value;
                _accessOrder.RemoveLast();
                _cache.Remove(lru);
            }
            
            var node = _accessOrder.AddFirst(key);
            _cache[key] = new CacheItem(resource, node);
        }
    }
    
    private record CacheItem(IResource Resource, LinkedListNode<IResourceKey> Node);
}
```

## Benchmarking

### BenchmarkDotNet Integration

TS4Tools includes BenchmarkDotNet for performance testing:

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class PackageLoadingBenchmarks
{
    private readonly string _packagePath = "test-package.package";
    private IPackageFactory _packageFactory = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddTS4ToolsCore();
        var provider = services.BuildServiceProvider();
        _packageFactory = provider.GetRequiredService<IPackageFactory>();
    }
    
    [Benchmark]
    public async Task LoadPackageAsync()
    {
        using var package = await _packageFactory.LoadFromFileAsync(_packagePath);
        _ = package.ResourceIndex.Count;
    }
    
    [Benchmark]
    public async Task LoadAndProcessResourcesAsync()
    {
        using var package = await _packageFactory.LoadFromFileAsync(_packagePath);
        
        foreach (var entry in package.ResourceIndex.Take(100))
        {
            var resource = package.GetResource(entry.Key);
            if (resource != null)
            {
                _ = resource.AsBytes().Length;
            }
        }
    }
}
```

### Running Benchmarks

```bash
cd benchmarks/TS4Tools.Benchmarks
dotnet run -c Release --framework net9.0
```

### Performance Monitoring

Monitor performance in production:

```csharp
public class PerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly IMetrics _metrics;
    
    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger, IMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }
    
    public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
    {
        using var activity = Activity.StartActivity(operationName);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            
            _metrics.CreateCounter<long>("ts4tools.operations")
                .Add(1, new("operation", operationName), new("status", "success"));
                
            return result;
        }
        catch (Exception ex)
        {
            _metrics.CreateCounter<long>("ts4tools.operations")
                .Add(1, new("operation", operationName), new("status", "error"));
                
            _logger.LogError(ex, "Operation {OperationName} failed after {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            _metrics.CreateHistogram<double>("ts4tools.operation_duration")
                .Record(stopwatch.Elapsed.TotalMilliseconds, new("operation", operationName));
                
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("Slow operation {OperationName} took {ElapsedMs}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

## Memory Profiling

### Tracking Memory Usage

Monitor memory allocation patterns:

```csharp
public static class MemoryTracker
{
    public static void LogMemoryUsage(string operation, ILogger logger)
    {
        var beforeGC = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterGC = GC.GetTotalMemory(false);
        
        logger.LogInformation(
            "Memory usage after {Operation}: {BeforeGC:N0} bytes before GC, {AfterGC:N0} bytes after GC",
            operation, beforeGC, afterGC);
    }
    
    public static async Task<T> TrackAllocationAsync<T>(Func<Task<T>> operation, ILogger logger, string operationName)
    {
        var initialMemory = GC.GetTotalMemory(true);
        
        var result = await operation();
        
        var finalMemory = GC.GetTotalMemory(false);
        var allocated = finalMemory - initialMemory;
        
        logger.LogDebug("Operation {OperationName} allocated {AllocatedBytes:N0} bytes", 
            operationName, allocated);
            
        return result;
    }
}
```

### Detecting Memory Leaks

Use `WeakReference` to detect leaks:

```csharp
public class LeakDetector
{
    private readonly ConcurrentBag<WeakReference> _trackedObjects = new();
    private readonly Timer _checkTimer;
    
    public LeakDetector()
    {
        _checkTimer = new Timer(CheckForLeaks, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    public void Track(object obj)
    {
        _trackedObjects.Add(new WeakReference(obj));
    }
    
    private void CheckForLeaks(object? state)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var aliveCount = _trackedObjects.Count(wr => wr.IsAlive);
        var totalCount = _trackedObjects.Count;
        
        if (aliveCount > totalCount * 0.5) // More than 50% still alive
        {
            Console.WriteLine($"Potential memory leak: {aliveCount}/{totalCount} objects still alive");
        }
    }
}
```

## Resource-Specific Optimizations

### Large Package Handling

Optimize for large packages (>100MB):

```csharp
public class LargePackageProcessor
{
    private readonly IPackageFactory _packageFactory;
    
    public async Task ProcessLargePackageAsync(string packagePath, 
        Func<IResourceKey, bool> resourceFilter)
    {
        using var package = await _packageFactory.LoadFromFileAsync(packagePath);
        
        // Process resources in batches to avoid memory pressure
        const int batchSize = 50;
        var filteredResources = package.ResourceIndex.Where(kvp => resourceFilter(kvp.Key));
        
        await foreach (var batch in filteredResources.Chunk(batchSize).ToAsyncEnumerable())
        {
            await ProcessResourceBatchAsync(package, batch);
            
            // Force garbage collection between batches for large packages
            if (package.ResourceIndex.Count > 10000)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
    
    private async Task ProcessResourceBatchAsync(IPackage package, 
        IEnumerable<KeyValuePair<IResourceKey, IResourceIndexEntry>> batch)
    {
        var tasks = batch.Select(async kvp =>
        {
            var resource = package.GetResource(kvp.Key);
            if (resource != null)
            {
                await ProcessSingleResourceAsync(resource);
            }
        });
        
        await Task.WhenAll(tasks);
    }
}
```

### String Optimization

Optimize string operations:

```csharp
public static class StringOptimizations
{
    // Use string interning for repeated strings
    private static readonly ConcurrentDictionary<string, string> InternedStrings = new();
    
    public static string Intern(string value)
    {
        return InternedStrings.GetOrAdd(value, string.Intern);
    }
    
    // Use StringBuilder for multiple concatenations
    public static string BuildResourcePath(IResourceKey key)
    {
        var sb = new StringBuilder(32);
        sb.Append("T-");
        sb.Append(key.ResourceType.ToString("X8"));
        sb.Append("_G-");
        sb.Append(key.ResourceGroup.ToString("X8"));
        sb.Append("_I-");
        sb.Append(key.Instance.ToString("X16"));
        return sb.ToString();
    }
    
    // Use Span<T> for parsing
    public static bool TryParseResourceKey(ReadOnlySpan<char> input, out IResourceKey key)
    {
        key = null!;
        
        if (input.Length < 30) return false;
        
        var typeSpan = input.Slice(2, 8);  // Skip "T-"
        var groupSpan = input.Slice(13, 8); // Skip "_G-"
        var instanceSpan = input.Slice(24, 16); // Skip "_I-"
        
        if (uint.TryParse(typeSpan, NumberStyles.HexNumber, null, out var type) &&
            uint.TryParse(groupSpan, NumberStyles.HexNumber, null, out var group) &&
            ulong.TryParse(instanceSpan, NumberStyles.HexNumber, null, out var instance))
        {
            key = new ResourceKey(type, group, instance);
            return true;
        }
        
        return false;
    }
}
```

## Performance Monitoring Tools

### Application Insights Integration

```csharp
public static class ApplicationInsightsExtensions
{
    public static IServiceCollection AddTS4ToolsApplicationInsights(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddApplicationInsightsTelemetry(configuration);
        services.AddSingleton<ITelemetryInitializer, TS4ToolsTelemetryInitializer>();
        
        return services;
    }
}

public class TS4ToolsTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Component.Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
        telemetry.Context.GlobalProperties["Application"] = "TS4Tools";
    }
}
```

### Custom Performance Counters

```csharp
public class PerformanceCounters
{
    private readonly Counter<long> _packagesLoaded;
    private readonly Counter<long> _resourcesProcessed;
    private readonly Histogram<double> _packageLoadTime;
    private readonly Histogram<double> _resourceProcessTime;
    
    public PerformanceCounters(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("TS4Tools");
        
        _packagesLoaded = meter.CreateCounter<long>("packages_loaded_total");
        _resourcesProcessed = meter.CreateCounter<long>("resources_processed_total");
        _packageLoadTime = meter.CreateHistogram<double>("package_load_duration_ms");
        _resourceProcessTime = meter.CreateHistogram<double>("resource_process_duration_ms");
    }
    
    public void RecordPackageLoaded(double loadTimeMs)
    {
        _packagesLoaded.Add(1);
        _packageLoadTime.Record(loadTimeMs);
    }
    
    public void RecordResourceProcessed(double processTimeMs, string resourceType)
    {
        _resourcesProcessed.Add(1, new("resource_type", resourceType));
        _resourceProcessTime.Record(processTimeMs, new("resource_type", resourceType));
    }
}
```

## Best Practices Summary

1. **Use async/await**: All I/O operations should be async
2. **Leverage `Span<T>`**: Use for string and binary operations to avoid allocations
3. **Stream for large data**: Use streams instead of byte arrays for large resources
4. **Parallel processing**: Process multiple packages concurrently with proper throttling
5. **Monitor memory**: Track allocations and detect leaks early
6. **Cache intelligently**: Cache frequently accessed resources with appropriate TTL
7. **Measure performance**: Use benchmarks to validate optimizations
8. **Profile regularly**: Use tools like PerfView or Application Insights to identify bottlenecks

Following these guidelines will ensure optimal performance when working with TS4Tools in production scenarios.
