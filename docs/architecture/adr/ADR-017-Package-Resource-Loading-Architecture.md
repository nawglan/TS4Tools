# ADR-017: Package Resource Loading Architecture

**Status:** Proposed
**Date:** August 17, 2025
**Deciders:** Architecture Team, Performance Team, Senior Developers

## Context

The TS4Tools application's core functionality revolves around loading, parsing, and manipulating Sims 4 package files containing various resources. These operations must handle:

1. **Large Package Files**: Packages can range from MB to GB in size
2. **Memory Constraints**: Loading entire packages into memory may cause issues
3. **Concurrent Access**: Multiple operations may need to access the same package
4. **Performance Requirements**: Fast loading and resource extraction
5. **Resource Diversity**: Different resource types with varying loading requirements

The current implementation lacks a coherent strategy for package loading, leading to inconsistent performance and potential memory issues.

## Decision

We will implement a **hybrid package resource loading architecture** with the following components:

1. **Streaming-First Approach**: Default to streaming for large files with selective caching
2. **Intelligent Caching**: Cache frequently accessed resources and metadata
3. **Lazy Loading**: Load resources on-demand rather than upfront
4. **Concurrent Access Management**: Thread-safe access with resource pooling
5. **Extensible Resource Factories**: Plugin-based architecture for resource types

## Rationale

### Current Problems

#### Memory Management Issues
- Loading entire packages into memory causes OutOfMemory exceptions
- No consideration for package size when choosing loading strategy
- Memory usage grows unbounded with multiple open packages
- No memory pressure handling or cleanup

#### Performance Inconsistencies
- Some operations are fast, others unexpectedly slow
- No caching of frequently accessed data
- Redundant parsing of resource headers
- No optimization for common access patterns

#### Concurrency Challenges
- No thread-safe access to package resources
- Race conditions in resource loading
- No coordination between multiple readers
- Resource contention and blocking

### Benefits of Hybrid Approach

#### Optimal Memory Usage
- Streaming prevents memory exhaustion
- Selective caching improves performance
- Memory pressure monitoring and cleanup
- Configurable memory limits and thresholds

#### Predictable Performance
- Consistent loading patterns across resource types
- Caching of expensive operations
- Performance monitoring and optimization
- Scalable for packages of any size

#### Safe Concurrency
- Thread-safe resource access
- Resource pooling and lifecycle management
- Coordinated access to shared resources
- Deadlock prevention and timeout handling

## Architecture Design

### Package Loading Strategy Selection

```csharp
public interface IPackageLoadingStrategy
{
    bool CanHandle(PackageInfo packageInfo);
    Task<IPackage> LoadAsync(string packagePath, PackageLoadOptions options, CancellationToken cancellationToken);
    int Priority { get; }
}

public class PackageLoadingStrategySelector
{
    private readonly IEnumerable<IPackageLoadingStrategy> _strategies;
    private readonly ILogger<PackageLoadingStrategySelector> _logger;

    public PackageLoadingStrategySelector(
        IEnumerable<IPackageLoadingStrategy> strategies,
        ILogger<PackageLoadingStrategySelector> logger)
    {
        _strategies = strategies.OrderByDescending(s => s.Priority);
        _logger = logger;
    }

    public IPackageLoadingStrategy SelectStrategy(PackageInfo packageInfo)
    {
        foreach (var strategy in _strategies)
        {
            if (strategy.CanHandle(packageInfo))
            {
                _logger.LogDebug("Selected loading strategy {StrategyType} for package {PackageName}",
                    strategy.GetType().Name, packageInfo.Name);
                return strategy;
            }
        }

        throw new NotSupportedException($"No loading strategy available for package: {packageInfo.Name}");
    }
}

// Small package strategy - load entirely into memory
public class InMemoryLoadingStrategy : IPackageLoadingStrategy
{
    private const long MaxInMemorySize = 100 * 1024 * 1024; // 100MB
    
    public int Priority => 100;

    public bool CanHandle(PackageInfo packageInfo)
    {
        return packageInfo.SizeBytes <= MaxInMemorySize;
    }

    public async Task<IPackage> LoadAsync(string packagePath, PackageLoadOptions options, CancellationToken cancellationToken)
    {
        var data = await File.ReadAllBytesAsync(packagePath, cancellationToken);
        return new InMemoryPackage(packagePath, data, options);
    }
}

// Large package strategy - streaming with caching
public class StreamingLoadingStrategy : IPackageLoadingStrategy
{
    public int Priority => 50;

    public bool CanHandle(PackageInfo packageInfo)
    {
        return true; // Can handle any package
    }

    public async Task<IPackage> LoadAsync(string packagePath, PackageLoadOptions options, CancellationToken cancellationToken)
    {
        var stream = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 64 * 1024);
        return new StreamingPackage(packagePath, stream, options);
    }
}
```

### Intelligent Caching System

```csharp
public interface IResourceCache
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task InvalidateAsync(string key);
    Task InvalidateByPatternAsync(string pattern);
    Task<CacheStatistics> GetStatisticsAsync();
}

public class MultiLevelResourceCache : IResourceCache
{
    private readonly IMemoryCache _level1Cache; // Fast, small capacity
    private readonly IDistributedCache _level2Cache; // Slower, larger capacity
    private readonly ILogger<MultiLevelResourceCache> _logger;
    private readonly CacheOptions _options;

    public MultiLevelResourceCache(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        IOptions<CacheOptions> options,
        ILogger<MultiLevelResourceCache> logger)
    {
        _level1Cache = memoryCache;
        _level2Cache = distributedCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        // Try L1 cache first
        if (_level1Cache.TryGetValue(key, out T? value))
        {
            _logger.LogTrace("Cache hit (L1): {Key}", key);
            return value;
        }

        // Try L2 cache
        var serializedValue = await _level2Cache.GetStringAsync(key);
        if (serializedValue != null)
        {
            value = JsonSerializer.Deserialize<T>(serializedValue);
            if (value != null)
            {
                // Promote to L1 cache
                _level1Cache.Set(key, value, TimeSpan.FromMinutes(_options.Level1ExpirationMinutes));
                _logger.LogTrace("Cache hit (L2): {Key}", key);
                return value;
            }
        }

        _logger.LogTrace("Cache miss: {Key}", key);
        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        expiration ??= TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

        // Set in L1 cache
        _level1Cache.Set(key, value, expiration.Value);

        // Set in L2 cache
        var serializedValue = JsonSerializer.Serialize(value);
        var l2Options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        await _level2Cache.SetStringAsync(key, serializedValue, l2Options);

        _logger.LogTrace("Cache set: {Key}", key);
    }

    public async Task InvalidateAsync(string key)
    {
        _level1Cache.Remove(key);
        await _level2Cache.RemoveAsync(key);
        _logger.LogTrace("Cache invalidated: {Key}", key);
    }

    public async Task InvalidateByPatternAsync(string pattern)
    {
        // Implementation depends on cache provider capabilities
        _logger.LogTrace("Cache pattern invalidated: {Pattern}", pattern);
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        // Return cache performance statistics
        return new CacheStatistics
        {
            Level1HitRatio = 0.0, // Calculate from metrics
            Level2HitRatio = 0.0,
            TotalEntries = 0,
            MemoryUsageBytes = 0
        };
    }
}

public class CacheOptions
{
    public int Level1ExpirationMinutes { get; set; } = 30;
    public int DefaultExpirationMinutes { get; set; } = 120;
    public long MaxMemoryUsageBytes { get; set; } = 256 * 1024 * 1024; // 256MB
}
```

### Lazy Loading Resource Implementation

```csharp
public interface ILazyResource<T> where T : class
{
    bool IsLoaded { get; }
    Task<T> GetValueAsync(CancellationToken cancellationToken = default);
    void Invalidate();
}

public class LazyResource<T> : ILazyResource<T> where T : class
{
    private readonly Func<CancellationToken, Task<T>> _factory;
    private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);
    private readonly ILogger<LazyResource<T>> _logger;
    private T? _value;
    private bool _isLoaded;

    public LazyResource(Func<CancellationToken, Task<T>> factory, ILogger<LazyResource<T>> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public bool IsLoaded => _isLoaded;

    public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded && _value != null)
            return _value;

        await _loadingSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isLoaded && _value != null)
                return _value;

            _logger.LogDebug("Loading lazy resource {ResourceType}", typeof(T).Name);
            
            _value = await _factory(cancellationToken);
            _isLoaded = true;

            _logger.LogDebug("Loaded lazy resource {ResourceType}", typeof(T).Name);
            return _value;
        }
        finally
        {
            _loadingSemaphore.Release();
        }
    }

    public void Invalidate()
    {
        _value = null;
        _isLoaded = false;
        _logger.LogDebug("Invalidated lazy resource {ResourceType}", typeof(T).Name);
    }
}

// Usage in package implementation
public class StreamingPackage : IPackage
{
    private readonly ILazyResource<PackageHeader> _header;
    private readonly ILazyResource<IReadOnlyList<ResourceEntry>> _resourceIndex;
    private readonly ConcurrentDictionary<ResourceKey, ILazyResource<IResource>> _resources = new();

    public StreamingPackage(string path, Stream stream, PackageLoadOptions options)
    {
        Path = path;
        Stream = stream;
        
        _header = new LazyResource<PackageHeader>(_ => LoadHeaderAsync(), _logger);
        _resourceIndex = new LazyResource<IReadOnlyList<ResourceEntry>>(_ => LoadResourceIndexAsync(), _logger);
    }

    public async Task<IResource> GetResourceAsync(ResourceKey key, CancellationToken cancellationToken = default)
    {
        var lazyResource = _resources.GetOrAdd(key, k => new LazyResource<IResource>(
            ct => LoadResourceAsync(k, ct), _logger));
            
        return await lazyResource.GetValueAsync(cancellationToken);
    }

    private async Task<IResource> LoadResourceAsync(ResourceKey key, CancellationToken cancellationToken)
    {
        var index = await _resourceIndex.GetValueAsync(cancellationToken);
        var entry = index.FirstOrDefault(e => e.Key == key);
        
        if (entry == null)
            throw new ResourceNotFoundException($"Resource not found: {key}");

        // Load resource data from stream
        Stream.Seek(entry.Offset, SeekOrigin.Begin);
        var data = new byte[entry.Size];
        await Stream.ReadExactlyAsync(data, cancellationToken);

        // Create resource using appropriate factory
        var factory = _resourceFactoryProvider.GetFactory(entry.ResourceType);
        return await factory.CreateAsync(data, key, cancellationToken);
    }
}
```

### Concurrent Access Management

```csharp
public interface IResourcePool<T> where T : class
{
    Task<IResourceLease<T>> AcquireAsync(string key, CancellationToken cancellationToken = default);
    Task<IResourceLease<T>> AcquireAsync(string key, Func<Task<T>> factory, CancellationToken cancellationToken = default);
}

public class ResourcePool<T> : IResourcePool<T> where T : class
{
    private readonly ConcurrentDictionary<string, ResourceEntry> _resources = new();
    private readonly SemaphoreSlim _acquisitionSemaphore;
    private readonly ILogger<ResourcePool<T>> _logger;
    private readonly ResourcePoolOptions _options;

    public ResourcePool(IOptions<ResourcePoolOptions> options, ILogger<ResourcePool<T>> logger)
    {
        _options = options.Value;
        _logger = logger;
        _acquisitionSemaphore = new SemaphoreSlim(_options.MaxConcurrentAcquisitions, _options.MaxConcurrentAcquisitions);
    }

    public async Task<IResourceLease<T>> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        return await AcquireAsync(key, () => throw new InvalidOperationException($"Resource not found: {key}"), cancellationToken);
    }

    public async Task<IResourceLease<T>> AcquireAsync(string key, Func<Task<T>> factory, CancellationToken cancellationToken = default)
    {
        await _acquisitionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            var entry = _resources.GetOrAdd(key, k => new ResourceEntry(k, factory));
            await entry.EnsureLoadedAsync(cancellationToken);
            
            var lease = new ResourceLease<T>(entry, this);
            entry.IncrementReferenceCount();
            
            _logger.LogTrace("Acquired resource lease: {Key}, RefCount: {RefCount}", key, entry.ReferenceCount);
            return lease;
        }
        finally
        {
            _acquisitionSemaphore.Release();
        }
    }

    internal void ReturnResource(ResourceEntry entry)
    {
        entry.DecrementReferenceCount();
        _logger.LogTrace("Returned resource lease: {Key}, RefCount: {RefCount}", entry.Key, entry.ReferenceCount);

        if (entry.ReferenceCount == 0 && entry.ShouldEvict(_options.MaxIdleTime))
        {
            if (_resources.TryRemove(entry.Key, out _))
            {
                entry.Dispose();
                _logger.LogTrace("Evicted idle resource: {Key}", entry.Key);
            }
        }
    }

    private class ResourceEntry : IDisposable
    {
        private readonly Func<Task<T>> _factory;
        private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);
        private T? _resource;
        private bool _isLoaded;
        private int _referenceCount;
        private DateTime _lastAccessed = DateTime.UtcNow;

        public string Key { get; }
        public int ReferenceCount => _referenceCount;

        public ResourceEntry(string key, Func<Task<T>> factory)
        {
            Key = key;
            _factory = factory;
        }

        public async Task EnsureLoadedAsync(CancellationToken cancellationToken)
        {
            if (_isLoaded) return;

            await _loadingSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isLoaded) return;

                _resource = await _factory();
                _isLoaded = true;
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }

        public T GetResource()
        {
            _lastAccessed = DateTime.UtcNow;
            return _resource ?? throw new InvalidOperationException("Resource not loaded");
        }

        public void IncrementReferenceCount() => Interlocked.Increment(ref _referenceCount);
        public void DecrementReferenceCount() => Interlocked.Decrement(ref _referenceCount);

        public bool ShouldEvict(TimeSpan maxIdleTime)
        {
            return DateTime.UtcNow - _lastAccessed > maxIdleTime;
        }

        public void Dispose()
        {
            if (_resource is IDisposable disposable)
                disposable.Dispose();
            _loadingSemaphore.Dispose();
        }
    }
}

public interface IResourceLease<T> : IDisposable where T : class
{
    T Resource { get; }
}

public class ResourceLease<T> : IResourceLease<T> where T : class
{
    private readonly ResourcePool<T>.ResourceEntry _entry;
    private readonly ResourcePool<T> _pool;
    private bool _disposed;

    public ResourceLease(ResourcePool<T>.ResourceEntry entry, ResourcePool<T> pool)
    {
        _entry = entry;
        _pool = pool;
    }

    public T Resource => _entry.GetResource();

    public void Dispose()
    {
        if (_disposed) return;

        _pool.ReturnResource(_entry);
        _disposed = true;
    }
}
```

### Memory Pressure Management

```csharp
public interface IMemoryPressureManager
{
    event EventHandler<MemoryPressureEventArgs> MemoryPressureDetected;
    void StartMonitoring();
    void StopMonitoring();
    Task HandleMemoryPressureAsync();
}

public class MemoryPressureManager : IMemoryPressureManager, IDisposable
{
    public event EventHandler<MemoryPressureEventArgs>? MemoryPressureDetected;

    private readonly Timer _monitoringTimer;
    private readonly ILogger<MemoryPressureManager> _logger;
    private readonly MemoryPressureOptions _options;
    private readonly List<IMemoryPressureHandler> _handlers;

    public MemoryPressureManager(
        IOptions<MemoryPressureOptions> options,
        IEnumerable<IMemoryPressureHandler> handlers,
        ILogger<MemoryPressureManager> logger)
    {
        _options = options.Value;
        _handlers = handlers.ToList();
        _logger = logger;
        _monitoringTimer = new Timer(CheckMemoryPressure, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void StartMonitoring()
    {
        _monitoringTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_options.MonitoringIntervalSeconds));
        _logger.LogInformation("Memory pressure monitoring started");
    }

    public void StopMonitoring()
    {
        _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _logger.LogInformation("Memory pressure monitoring stopped");
    }

    private void CheckMemoryPressure(object? state)
    {
        var totalMemory = GC.GetTotalMemory(false);
        var memoryPressure = totalMemory / (double)_options.MaxMemoryUsageBytes;

        if (memoryPressure > _options.HighPressureThreshold)
        {
            var eventArgs = new MemoryPressureEventArgs(totalMemory, memoryPressure, MemoryPressureLevel.High);
            MemoryPressureDetected?.Invoke(this, eventArgs);
            
            _ = Task.Run(HandleMemoryPressureAsync);
        }
        else if (memoryPressure > _options.MediumPressureThreshold)
        {
            var eventArgs = new MemoryPressureEventArgs(totalMemory, memoryPressure, MemoryPressureLevel.Medium);
            MemoryPressureDetected?.Invoke(this, eventArgs);
        }
    }

    public async Task HandleMemoryPressureAsync()
    {
        _logger.LogWarning("Handling memory pressure");

        foreach (var handler in _handlers.OrderBy(h => h.Priority))
        {
            try
            {
                var freedBytes = await handler.HandleMemoryPressureAsync();
                _logger.LogInformation("Memory pressure handler {HandlerType} freed {BytesFreed} bytes",
                    handler.GetType().Name, freedBytes);

                // Check if pressure is resolved
                var currentMemory = GC.GetTotalMemory(true);
                var currentPressure = currentMemory / (double)_options.MaxMemoryUsageBytes;
                
                if (currentPressure < _options.MediumPressureThreshold)
                {
                    _logger.LogInformation("Memory pressure resolved");
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory pressure handler {HandlerType} failed",
                    handler.GetType().Name);
            }
        }
    }

    public void Dispose()
    {
        _monitoringTimer.Dispose();
    }
}

public interface IMemoryPressureHandler
{
    int Priority { get; }
    Task<long> HandleMemoryPressureAsync();
}

public class CacheMemoryPressureHandler : IMemoryPressureHandler
{
    public int Priority => 100; // High priority

    private readonly IResourceCache _cache;
    private readonly ILogger<CacheMemoryPressureHandler> _logger;

    public CacheMemoryPressureHandler(IResourceCache cache, ILogger<CacheMemoryPressureHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<long> HandleMemoryPressureAsync()
    {
        // Implement cache cleanup logic
        var stats = await _cache.GetStatisticsAsync();
        var beforeMemory = stats.MemoryUsageBytes;

        // Clear half of the cache entries (LRU)
        await ClearOldestCacheEntriesAsync(0.5);

        var afterStats = await _cache.GetStatisticsAsync();
        var freedBytes = beforeMemory - afterStats.MemoryUsageBytes;

        _logger.LogInformation("Cache cleanup freed {FreedBytes} bytes", freedBytes);
        return freedBytes;
    }

    private async Task ClearOldestCacheEntriesAsync(double percentage)
    {
        // Implementation depends on cache provider
        await Task.CompletedTask;
    }
}
```

## Implementation Guidelines

### Service Registration

```csharp
public static class PackageLoadingServiceExtensions
{
    public static IServiceCollection AddPackageLoading(this IServiceCollection services, IConfiguration configuration)
    {
        // Loading strategies
        services.AddTransient<IPackageLoadingStrategy, InMemoryLoadingStrategy>();
        services.AddTransient<IPackageLoadingStrategy, StreamingLoadingStrategy>();
        services.AddSingleton<PackageLoadingStrategySelector>();

        // Caching
        services.Configure<CacheOptions>(configuration.GetSection("PackageLoading:Cache"));
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        services.AddSingleton<IResourceCache, MultiLevelResourceCache>();

        // Resource pooling
        services.Configure<ResourcePoolOptions>(configuration.GetSection("PackageLoading:ResourcePool"));
        services.AddSingleton(typeof(IResourcePool<>), typeof(ResourcePool<>));

        // Memory pressure management
        services.Configure<MemoryPressureOptions>(configuration.GetSection("PackageLoading:MemoryPressure"));
        services.AddSingleton<IMemoryPressureManager, MemoryPressureManager>();
        services.AddTransient<IMemoryPressureHandler, CacheMemoryPressureHandler>();

        return services;
    }
}
```

## Migration Strategy

### Phase 1: Foundation (Week 1)
1. Implement loading strategy selection framework
2. Create streaming and in-memory loading strategies
3. Add basic caching infrastructure
4. Update package interfaces for async loading

### Phase 2: Lazy Loading (Week 2)
1. Implement lazy resource loading
2. Convert existing package implementations
3. Add resource pooling and lifecycle management
4. Implement memory pressure monitoring

### Phase 3: Optimization (Week 3)
1. Add intelligent caching with eviction policies
2. Implement concurrent access optimization
3. Add performance monitoring and metrics
4. Tune caching and pooling parameters

### Phase 4: Production Readiness (Week 4)
1. Add comprehensive error handling and recovery
2. Implement configuration and monitoring
3. Add operational tooling and diagnostics
4. Performance testing and optimization

## Success Criteria

### Performance Metrics
- [ ] Package loading time scales linearly with package size
- [ ] Memory usage remains bounded regardless of package size
- [ ] Concurrent access performance scales with available cores
- [ ] Cache hit rates > 80% for frequently accessed resources

### Reliability Metrics
- [ ] Zero OutOfMemory exceptions during normal operation
- [ ] Graceful degradation under memory pressure
- [ ] Thread-safe access with no race conditions
- [ ] Automatic recovery from transient failures

### Operational Metrics
- [ ] Memory pressure detection and handling
- [ ] Performance monitoring and alerting
- [ ] Resource lifecycle tracking and cleanup
- [ ] Configuration and tuning capabilities

## Consequences

### Positive
- **Scalability**: Handles packages of any size efficiently
- **Performance**: Intelligent caching and lazy loading
- **Reliability**: Memory pressure handling and thread safety
- **Maintainability**: Clear separation of concerns and extensible design
- **Observability**: Comprehensive monitoring and diagnostics

### Negative
- **Complexity**: Additional infrastructure and coordination overhead
- **Memory Usage**: Caching requires additional memory allocation
- **Latency**: Lazy loading may introduce delays for first access
- **Dependencies**: Additional dependencies on caching infrastructure

### Mitigation Strategies
- Provide configuration options for different usage patterns
- Implement adaptive caching based on available memory
- Add warm-up procedures for critical resources
- Create fallback mechanisms for cache failures

## Related ADRs
- ADR-002: Dependency Injection (service registration patterns)
- ADR-014: Error Handling and Exception Strategy (error handling in loading)
- ADR-015: Logging and Observability Framework (performance monitoring)
- ADR-016: Configuration Management Strategy (loading configuration)
