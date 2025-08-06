/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Modern dependency injection-based resource manager.
/// Replaces the legacy s4pi.WrapperDealer with async operations, caching, and metrics.
/// </summary>
internal sealed class ResourceManager : IResourceManager, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ResourceManagerOptions> _optionsMonitor;
    private readonly ILogger<ResourceManager> _logger;
    private readonly ConcurrentDictionary<string, object> _resourceFactories;
    private readonly ConcurrentDictionary<string, object> _factoryRegistrations;
    private readonly Timer? _cacheCleanupTimer;
    
    // Performance tracking
    private long _totalResourcesCreated;
    private long _totalResourcesLoaded;
    private long _totalCacheHits;
    private long _totalCacheRequests;
    private readonly List<double> _creationTimes = new();
    private readonly List<double> _loadTimes = new();
    private readonly object _metricsLock = new();
    
    // Resource cache - using weak references to allow GC cleanup
    private readonly ConcurrentDictionary<string, WeakReference<IResource>> _resourceCache = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager"/> class.
    /// </summary>
    public ResourceManager(
        IServiceProvider serviceProvider,
        IOptionsMonitor<ResourceManagerOptions> optionsMonitor,
        ILogger<ResourceManager> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _resourceFactories = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _factoryRegistrations = new ConcurrentDictionary<string, object>();
        
        // Register default factory
        var defaultFactory = new DefaultResourceFactory();
        foreach (var resourceType in defaultFactory.SupportedResourceTypes)
        {
            _resourceFactories.TryAdd(resourceType, defaultFactory);
        }
        
        // Register default factory in the registrations dictionary for statistics
        var defaultRegistrationKey = $"{typeof(IResource).FullName}:{typeof(DefaultResourceFactory).FullName}";
        _factoryRegistrations[defaultRegistrationKey] = defaultFactory;
        
        // Setup cache cleanup timer if caching is enabled
        var options = _optionsMonitor.CurrentValue;
        if (options.EnableCaching)
        {
            _cacheCleanupTimer = new Timer(CleanupCache, null, 
                TimeSpan.FromMinutes(options.CacheExpirationMinutes), 
                TimeSpan.FromMinutes(options.CacheExpirationMinutes));
        }
        
        _logger.LogInformation("ResourceManager initialized with caching: {CachingEnabled}", options.EnableCaching);
    }
    
    /// <inheritdoc />
    public async Task<IResource> CreateResourceAsync(string resourceType, int apiVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        if (apiVersion < 1) throw new ArgumentException("API version must be greater than 0", nameof(apiVersion));
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Try to get specific factory, fallback to default ("*")
            var factory = GetFactoryForResourceType(resourceType);
            
            var resource = await factory.CreateResourceAsync(apiVersion, null, cancellationToken);
            
            Interlocked.Increment(ref _totalResourcesCreated);
            RecordCreationTime(stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Created resource of type {ResourceType} using factory {FactoryType}", 
                resourceType, factory.GetType().Name);
            
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create resource of type {ResourceType}", resourceType);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<IResource> LoadResourceAsync(IPackage package, IResourceIndexEntry resourceIndexEntry, int apiVersion, bool forceDefaultWrapper = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(resourceIndexEntry);
        
        var stopwatch = Stopwatch.StartNew();
        var resourceType = forceDefaultWrapper ? "*" : resourceIndexEntry["ResourceType"].ToString() ?? "*";
        
        try
        {
            // Check cache first
            var cacheKey = GenerateCacheKey(package, resourceIndexEntry);
            if (_optionsMonitor.CurrentValue.EnableCaching && TryGetFromCache(cacheKey, out var cachedResource) && cachedResource != null)
            {
                Interlocked.Increment(ref _totalCacheHits);
                Interlocked.Increment(ref _totalCacheRequests);
                _logger.LogDebug("Cache hit for resource {ResourceType} from package", resourceType);
                return cachedResource;
            }
            
            Interlocked.Increment(ref _totalCacheRequests);
            
            // Load resource data from package
            using var resourceStream = await package.GetResourceStreamAsync(resourceIndexEntry, cancellationToken);
            
            // Get appropriate factory
            var factory = GetFactoryForResourceType(resourceType);
            
            // Create resource instance
            var resource = await factory.CreateResourceAsync(apiVersion, resourceStream, cancellationToken);
            
            // Cache the resource if caching is enabled
            if (_optionsMonitor.CurrentValue.EnableCaching)
            {
                CacheResource(cacheKey, resource);
            }
            
            Interlocked.Increment(ref _totalResourcesLoaded);
            RecordLoadTime(stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogDebug("Loaded resource of type {ResourceType} from package using factory {FactoryType}", 
                resourceType, factory.GetType().Name);
            
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load resource of type {ResourceType} from package", resourceType);
            throw;
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyDictionary<string, Type> GetResourceTypeMap()
    {
        var typeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var kvp in _resourceFactories)
        {
            typeMap[kvp.Key] = kvp.Value.GetType();
        }
        
        return typeMap;
    }
    
    /// <inheritdoc />
    public void RegisterFactory<TResource, TFactory>() 
        where TResource : IResource 
        where TFactory : class, IResourceFactory<TResource>
    {
        var factoryType = typeof(TFactory);
        var resourceType = typeof(TResource);
        
        // Check if already registered
        var registrationKey = $"{resourceType.FullName}:{factoryType.FullName}";
        if (_factoryRegistrations.ContainsKey(registrationKey))
        {
            _logger.LogWarning("Factory {FactoryType} for resource {ResourceType} is already registered", 
                factoryType.Name, resourceType.Name);
            return;
        }
        
        try
        {
            var factory = _serviceProvider.GetService<TFactory>() ?? 
                         ActivatorUtilities.CreateInstance<TFactory>(_serviceProvider);
            
            // Register for all supported resource types
            foreach (var supportedType in factory.SupportedResourceTypes)
            {
                // Use priority to handle conflicts - higher priority wins
                if (_resourceFactories.TryGetValue(supportedType, out var existingObj) && existingObj is IResourceFactory existing)
                {
                    if (factory.Priority > existing.Priority)
                    {
                        _resourceFactories[supportedType] = factory;
                        _logger.LogInformation("Replaced factory for resource type {ResourceType} with higher priority factory {FactoryType}", 
                            supportedType, factoryType.Name);
                    }
                    else
                    {
                        _logger.LogDebug("Skipped registering factory {FactoryType} for resource type {ResourceType} due to lower priority", 
                            factoryType.Name, supportedType);
                    }
                }
                else
                {
                    _resourceFactories[supportedType] = factory;
                    _logger.LogInformation("Registered factory {FactoryType} for resource type {ResourceType}", 
                        factoryType.Name, supportedType);
                }
            }
            
            _factoryRegistrations[registrationKey] = factory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register factory {FactoryType} for resource {ResourceType}", 
                factoryType.Name, resourceType.Name);
            throw;
        }
    }
    
    /// <inheritdoc />
    public ResourceManagerStatistics GetStatistics()
    {
        lock (_metricsLock)
        {
            var cacheHitRatio = _totalCacheRequests > 0 ? (double)_totalCacheHits / _totalCacheRequests : 0.0;
            var avgCreationTime = _creationTimes.Count > 0 ? _creationTimes.Average() : 0.0;
            var avgLoadTime = _loadTimes.Count > 0 ? _loadTimes.Average() : 0.0;
            
            return new ResourceManagerStatistics
            {
                TotalResourcesCreated = _totalResourcesCreated,
                TotalResourcesLoaded = _totalResourcesLoaded,
                RegisteredFactories = _factoryRegistrations.Count,
                CacheHitRatio = cacheHitRatio,
                CacheSize = _resourceCache.Count,
                CacheMemoryUsage = EstimateCacheMemoryUsage(),
                AverageCreationTimeMs = avgCreationTime,
                AverageLoadTimeMs = avgLoadTime
            };
        }
    }
    
    private IResourceFactory GetFactoryForResourceType(string resourceType)
    {
        // Try exact match first
        if (_resourceFactories.TryGetValue(resourceType, out var factoryObj))
        {
            if (factoryObj is IResourceFactory factory)
            {
                return factory;
            }
        }
        
        // Fallback to default factory
        if (_resourceFactories.TryGetValue("*", out var defaultFactoryObj) && defaultFactoryObj is IResourceFactory defaultFactory)
        {
            return defaultFactory;
        }
        
        var options = _optionsMonitor.CurrentValue;
        if (options.ThrowOnMissingHandler)
        {
            throw new InvalidOperationException($"No resource factory found for resource type '{resourceType}' and no default factory available");
        }
        
        // This should not happen since we register default factory in constructor
        throw new InvalidOperationException("Critical error: No default resource factory available");
    }
    
    private static string GenerateCacheKey(IPackage package, IResourceIndexEntry entry)
    {
        // Generate a unique cache key based on package and resource entry
        var resourceKey = entry as IResourceKey;
        return $"{package.GetHashCode()}:{resourceKey?.Instance:X8}:{resourceKey?.ResourceType:X8}:{resourceKey?.ResourceGroup:X8}";
    }
    
    private bool TryGetFromCache(string cacheKey, out IResource? resource)
    {
        resource = null;
        
        if (!_resourceCache.TryGetValue(cacheKey, out var weakRef))
        {
            return false;
        }
        
        if (!weakRef.TryGetTarget(out resource))
        {
            // Resource was garbage collected, remove from cache
            _resourceCache.TryRemove(cacheKey, out _);
            resource = null;
            return false;
        }
        
        return true;
    }
    
    private void CacheResource(string cacheKey, IResource resource)
    {
        var options = _optionsMonitor.CurrentValue;
        
        // Check cache size limits
        if (_resourceCache.Count >= options.MaxCacheSize)
        {
            // Remove some old entries
            CleanupCache(null);
        }
        
        _resourceCache[cacheKey] = new WeakReference<IResource>(resource);
    }
    
    private void CleanupCache(object? state)
    {
        try
        {
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _resourceCache)
            {
                if (!kvp.Value.TryGetTarget(out _))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _resourceCache.TryRemove(key, out _);
            }
            
            _logger.LogDebug("Cache cleanup removed {Count} expired entries", keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cache cleanup");
        }
    }
    
    private void RecordCreationTime(double milliseconds)
    {
        if (!_optionsMonitor.CurrentValue.EnableMetrics) return;
        
        lock (_metricsLock)
        {
            _creationTimes.Add(milliseconds);
            // Keep only last 1000 measurements to prevent unbounded growth
            if (_creationTimes.Count > 1000)
            {
                _creationTimes.RemoveAt(0);
            }
        }
    }
    
    private void RecordLoadTime(double milliseconds)
    {
        if (!_optionsMonitor.CurrentValue.EnableMetrics) return;
        
        lock (_metricsLock)
        {
            _loadTimes.Add(milliseconds);
            // Keep only last 1000 measurements to prevent unbounded growth
            if (_loadTimes.Count > 1000)
            {
                _loadTimes.RemoveAt(0);
            }
        }
    }
    
    private long EstimateCacheMemoryUsage()
    {
        // Rough estimation - in a real implementation you might want more accurate tracking
        return _resourceCache.Count * 1024; // Assume 1KB per cached resource entry overhead
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        _cacheCleanupTimer?.Dispose();
        _resourceCache.Clear();
        _logger.LogInformation("ResourceManager disposed");
    }
}
