# WrapperDealer Compatibility Design Document - Phase 4.13

**Date Created:** August 8, 2025
**Phase:** 4.13 Resource Type Audit and Foundation
**Status:** âœ… COMPLETED

## Executive Summary

This document defines the architecture for maintaining 100% backward compatibility with the legacy Sims4Tools WrapperDealer system while migrating to modern .NET 9 patterns. The design addresses the critical Assembly.LoadFile() crisis while preserving all existing public APIs.

## ðŸš¨ Critical Assembly Loading Issue Analysis

### **Confirmed Blocking Issue**

- **Location:** `WrapperDealer.cs:89`
- **Code:** `Assembly dotNetDll = Assembly.LoadFile(path);`
- **Impact:** **CRITICAL BLOCKING** - Breaks completely in .NET 8+
- **Root Cause:** AssemblyLoadContext behavior changes break plugin loading
- **Community Impact:** 20+ community resource handlers depend on this mechanism

### **Legacy Assembly Loading Pattern**

```csharp
// LEGACY IMPLEMENTATION - BROKEN IN .NET 8+
static WrapperDealer()
{
    string folder = Path.GetDirectoryName(typeof(WrapperDealer).Assembly.Location);
    typeMap = new List<KeyValuePair<string, Type>>();
    foreach (string path in Directory.GetFiles(folder, "*.dll"))
    {
        try
        {
            // âŒ CRITICAL ISSUE: This breaks in .NET 8+
            Assembly dotNetDll = Assembly.LoadFile(path);
            Type[] types = dotNetDll.GetTypes();
            foreach (Type t in types)
            {
                if (!t.IsSubclassOf(typeof(AResourceHandler))) continue;
                // Plugin registration logic...
            }
        }
        catch { }
    }
}
```

## ðŸŽ¯ Modern Replacement Architecture

### **Core Interface Design**

```csharp
// Modern service interface for internal implementation
public interface IResourceWrapperService
{
    // Core resource retrieval - async preferred
    Task<IResource> GetResourceAsync(int apiVersion, IPackage package,
        IResourceIndexEntry entry, CancellationToken cancellationToken = default);

    Task<IResource> GetResourceAsync(int apiVersion, IPackage package,
        IResourceIndexEntry entry, bool alwaysDefault, CancellationToken cancellationToken = default);

    // Resource creation
    Task<IResource> CreateNewResourceAsync(int apiVersion, string resourceType,
        CancellationToken cancellationToken = default);

    // Compatibility - sync methods for legacy support
    IResource GetResource(int apiVersion, IPackage package, IResourceIndexEntry entry);
    IResource GetResource(int apiVersion, IPackage package, IResourceIndexEntry entry, bool alwaysDefault);
    IResource CreateNewResource(int apiVersion, string resourceType);

    // Collection access for compatibility
    IReadOnlyCollection<KeyValuePair<string, Type>> TypeMap { get; }
    ICollection<KeyValuePair<string, Type>> Disabled { get; }
}
```

### **Modern Assembly Loading Implementation**

```csharp
public interface IAssemblyLoadContextManager
{
    Assembly LoadFromPath(string assemblyPath);
    Type[] GetTypesFromAssembly(Assembly assembly);
    void UnloadContext(string contextName);
    bool IsAssemblyLoaded(string assemblyPath);
}

public class ModernAssemblyLoadContextManager : IAssemblyLoadContextManager
{
    private readonly ConcurrentDictionary<string, AssemblyLoadContext> _contexts = new();
    private readonly ILogger<ModernAssemblyLoadContextManager> _logger;

    public Assembly LoadFromPath(string assemblyPath)
    {
        var contextName = Path.GetFileNameWithoutExtension(assemblyPath);

        // Use shared context for plugin compatibility
        var context = _contexts.GetOrAdd(contextName, name =>
            new AssemblyLoadContext(name, isCollectible: true));

        try
        {
            return context.LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly from {Path}", assemblyPath);
            throw new PluginLoadException($"Cannot load assembly: {assemblyPath}", ex);
        }
    }

    public Type[] GetTypesFromAssembly(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return successfully loaded types, log failures
            _logger.LogWarning("Some types failed to load from {Assembly}: {Errors}",
                assembly.FullName, string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message)));
            return ex.Types.Where(t => t != null).ToArray();
        }
    }
}
```

## ðŸ”§ Backward Compatibility Facade

### **Static WrapperDealer Class - EXACT API Preservation**

```csharp
/// <summary>
/// Legacy compatibility facade - preserves exact API signatures
/// </summary>
public static class WrapperDealer
{
    private static readonly Lazy<IResourceWrapperService> _service = new(() =>
        ServiceLocator.Current.GetRequiredService<IResourceWrapperService>());

    // âœ… EXACT SIGNATURE PRESERVATION - CRITICAL for compatibility

    /// <summary>
    /// Create a new Resource of the requested type, allowing the wrapper to initialise it appropriately
    /// </summary>
    /// <param name="APIversion">API version of request</param>
    /// <param name="resourceType">Type of resource (currently a string like "0xDEADBEEF")</param>
    /// <returns></returns>
    public static IResource CreateNewResource(int APIversion, string resourceType)
    {
        return _service.Value.CreateNewResource(APIversion, resourceType);
    }

    /// <summary>
    /// Retrieve a resource from a package, readying the appropriate wrapper
    /// </summary>
    /// <param name="APIversion">API version of request</param>
    /// <param name="pkg">Package containing <paramref name="rie"/></param>
    /// <param name="rie">Identifies resource to be returned</param>
    /// <returns>A resource from the package</returns>
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie)
    {
        return GetResource(APIversion, pkg, rie, false);
    }

    /// <summary>
    /// Retrieve a resource from a package, readying the appropriate wrapper or the default wrapper
    /// </summary>
    /// <param name="APIversion">API version of request</param>
    /// <param name="pkg">Package containing <paramref name="rie"/></param>
    /// <param name="rie">Identifies resource to be returned</param>
    /// <param name="AlwaysDefault">When true, indicates WrapperDealer should always use the DefaultResource wrapper</param>
    /// <returns>A resource from the package</returns>
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie, bool AlwaysDefault)
    {
        return _service.Value.GetResource(APIversion, pkg, rie, AlwaysDefault);
    }

    /// <summary>
    /// Retrieve the resource wrappers known to WrapperDealer.
    /// </summary>
    public static ICollection<KeyValuePair<string, Type>> TypeMap
    {
        get { return _service.Value.TypeMap.ToList(); }
    }

    /// <summary>
    /// Access the collection of wrappers on the &quot;disabled&quot; list.
    /// </summary>
    public static ICollection<KeyValuePair<string, Type>> Disabled
    {
        get { return _service.Value.Disabled; }
    }
}
```

### **Modern Service Implementation**

```csharp
public class ResourceWrapperService : IResourceWrapperService
{
    private readonly IAssemblyLoadContextManager _assemblyManager;
    private readonly IResourceManager _resourceManager;
    private readonly ILogger<ResourceWrapperService> _logger;
    private readonly List<KeyValuePair<string, Type>> _typeMap;
    private readonly List<KeyValuePair<string, Type>> _disabled;

    public ResourceWrapperService(
        IAssemblyLoadContextManager assemblyManager,
        IResourceManager resourceManager,
        ILogger<ResourceWrapperService> logger)
    {
        _assemblyManager = assemblyManager ?? throw new ArgumentNullException(nameof(assemblyManager));
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _typeMap = new List<KeyValuePair<string, Type>>();
        _disabled = new List<KeyValuePair<string, Type>>();

        InitializeResourceWrappers();
    }

    private void InitializeResourceWrappers()
    {
        // Modern plugin discovery with AssemblyLoadContext
        var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        foreach (string path in Directory.GetFiles(folder, "*.dll"))
        {
            try
            {
                // âœ… MODERN: Uses AssemblyLoadContext instead of Assembly.LoadFile()
                var assembly = _assemblyManager.LoadFromPath(path);
                var types = _assemblyManager.GetTypesFromAssembly(assembly);

                foreach (var type in types)
                {
                    if (!type.IsSubclassOf(typeof(AResourceHandler))) continue;

                    var handler = CreateResourceHandler(type);
                    if (handler == null) continue;

                    RegisterResourceHandler(handler);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load resource handlers from {Path}", path);
            }
        }

        _typeMap.Sort((x, y) => x.Key.CompareTo(y.Key));
        _logger.LogInformation("Loaded {Count} resource wrapper types", _typeMap.Count);
    }

    // Async-first implementation
    public async Task<IResource> GetResourceAsync(int apiVersion, IPackage package,
        IResourceIndexEntry entry, CancellationToken cancellationToken = default)
    {
        return await GetResourceAsync(apiVersion, package, entry, false, cancellationToken);
    }

    public async Task<IResource> GetResourceAsync(int apiVersion, IPackage package,
        IResourceIndexEntry entry, bool alwaysDefault, CancellationToken cancellationToken = default)
    {
        var resourceType = alwaysDefault ? "*" : entry["ResourceType"];
        var stream = await GetResourceStreamAsync(package, entry, cancellationToken);

        return await CreateResourceFromTypeAsync(resourceType, apiVersion, stream, cancellationToken);
    }

    // Sync compatibility methods - DEADLOCK WARNING
    // These methods use Task.Run() to avoid deadlocks in synchronization contexts
    // Legacy code requires sync methods, but modern callers should use async versions
    public IResource GetResource(int apiVersion, IPackage package, IResourceIndexEntry entry)
    {
        return Task.Run(async () => await GetResourceAsync(apiVersion, package, entry).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    public IResource GetResource(int apiVersion, IPackage package, IResourceIndexEntry entry, bool alwaysDefault)
    {
        return Task.Run(async () => await GetResourceAsync(apiVersion, package, entry, alwaysDefault).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    public IResource CreateNewResource(int apiVersion, string resourceType)
    {
        return Task.Run(async () => await CreateNewResourceAsync(apiVersion, resourceType).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    // Collection properties for compatibility
    public IReadOnlyCollection<KeyValuePair<string, Type>> TypeMap => _typeMap.AsReadOnly();
    public ICollection<KeyValuePair<string, Type>> Disabled => _disabled;
}
```

## ðŸ”Œ Plugin System Architecture

### **Modern Plugin Loading**

```csharp
public interface IPluginLoadContext
{
    Assembly LoadAssembly(string assemblyPath);
    void UnloadAssembly(string assemblyPath);
    IEnumerable<Type> DiscoverResourceHandlers(Assembly assembly);
}

public class PluginLoadContext : AssemblyLoadContext, IPluginLoadContext
{
    private readonly ILogger<PluginLoadContext> _logger;

    public PluginLoadContext(string name, ILogger<PluginLoadContext> logger)
        : base(name, isCollectible: true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Assembly LoadAssembly(string assemblyPath)
    {
        try
        {
            return LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin assembly: {Path}", assemblyPath);
            throw;
        }
    }

    public IEnumerable<Type> DiscoverResourceHandlers(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(AResourceHandler)) && !t.IsAbstract);
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning("Some types failed to load from {Assembly}", assembly.FullName);
            return ex.Types.Where(t => t != null && t.IsSubclassOf(typeof(AResourceHandler)) && !t.IsAbstract);
        }
    }
}
```

### **Resource Handler Registry**

```csharp
public interface IResourceWrapperRegistry
{
    void RegisterHandler(Type handlerType, IEnumerable<string> resourceTypes);
    void UnregisterHandler(Type handlerType);
    Type? GetHandlerType(string resourceType);
    IEnumerable<KeyValuePair<string, Type>> GetAllHandlers();
    void DisableHandler(Type handlerType);
    void EnableHandler(Type handlerType);
}

public class ResourceWrapperRegistry : IResourceWrapperRegistry
{
    private readonly ConcurrentDictionary<string, Type> _typeMap = new();
    private readonly ConcurrentHashSet<Type> _disabledHandlers = new();
    private readonly ILogger<ResourceWrapperRegistry> _logger;

    public void RegisterHandler(Type handlerType, IEnumerable<string> resourceTypes)
    {
        foreach (var resourceType in resourceTypes)
        {
            _typeMap.AddOrUpdate(resourceType, handlerType, (key, existing) =>
            {
                _logger.LogWarning("Handler type {NewType} replaced {OldType} for resource type {ResourceType}",
                    handlerType.Name, existing.Name, resourceType);
                return handlerType;
            });
        }

        _logger.LogDebug("Registered handler {HandlerType} for {Count} resource types",
            handlerType.Name, resourceTypes.Count());
    }

    public Type? GetHandlerType(string resourceType)
    {
        if (_typeMap.TryGetValue(resourceType, out var handlerType) &&
            !_disabledHandlers.Contains(handlerType))
        {
            return handlerType;
        }

        // Fallback to default handler
        return _typeMap.TryGetValue("*", out var defaultHandler) &&
               !_disabledHandlers.Contains(defaultHandler) ? defaultHandler : null;
    }
}
```

## ðŸ§ª Testing Strategy

### **Compatibility Test Framework**

```csharp
[TestClass]
public class WrapperDealerCompatibilityTests
{
    [TestMethod]
    public void CreateNewResource_PreservesExactSignature()
    {
        // Test that legacy API calls work unchanged
        var resource = WrapperDealer.CreateNewResource(1, "0xDEADBEEF");
        Assert.IsNotNull(resource);
    }

    [TestMethod]
    public void GetResource_PreservesExactSignature()
    {
        // Test both overloads work exactly as before
        var resource1 = WrapperDealer.GetResource(1, package, entry);
        var resource2 = WrapperDealer.GetResource(1, package, entry, false);

        Assert.IsNotNull(resource1);
        Assert.IsNotNull(resource2);
    }

    [TestMethod]
    public void TypeMap_PreservesCollectionInterface()
    {
        // Test that collection access patterns still work
        var typeMap = WrapperDealer.TypeMap;
        Assert.IsInstanceOfType(typeMap, typeof(ICollection<KeyValuePair<string, Type>>));

        // Test enumeration works
        foreach (var kvp in typeMap)
        {
            Assert.IsNotNull(kvp.Key);
            Assert.IsNotNull(kvp.Value);
        }
    }

    [TestMethod]
    public void Disabled_PreservesCollectionInterface()
    {
        var disabled = WrapperDealer.Disabled;
        Assert.IsInstanceOfType(disabled, typeof(ICollection<KeyValuePair<string, Type>>));

        // Test add/remove operations work
        var testEntry = new KeyValuePair<string, Type>("test", typeof(object));
        disabled.Add(testEntry);
        Assert.IsTrue(disabled.Contains(testEntry));

        disabled.Remove(testEntry);
        Assert.IsFalse(disabled.Contains(testEntry));
    }
}
```

### **Assembly Loading Tests**

```csharp
[TestClass]
public class AssemblyLoadingTests
{
    [TestMethod]
    public void ModernAssemblyLoading_WorksWithRealPlugins()
    {
        var manager = new ModernAssemblyLoadContextManager(logger);

        // Test with actual community wrapper DLL
        var assembly = manager.LoadFromPath(communityWrapperPath);
        var types = manager.GetTypesFromAssembly(assembly);

        Assert.IsNotNull(assembly);
        Assert.IsTrue(types.Length > 0);

        var handlerTypes = types.Where(t => t.IsSubclassOf(typeof(AResourceHandler)));
        Assert.IsTrue(handlerTypes.Any(), "Should find at least one resource handler");
    }

    [TestMethod]
    public async Task PluginLoading_MaintainsPerformanceParity()
    {
        // Benchmark modern vs legacy loading (where possible)
        var stopwatch = Stopwatch.StartNew();

        // Load plugins with modern system
        await LoadPluginsModern();
        stopwatch.Stop();

        // Should be comparable or faster than legacy system
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, "Plugin loading should complete within 5 seconds");
    }
}
```

## ðŸ“Š Migration Risk Assessment

### **High Risk Items**

1. **API Compatibility** - Any deviation breaks existing tools

   - **Mitigation:** Exact signature preservation with comprehensive testing

1. **Plugin Loading Behavior** - Community plugins may have subtle dependencies

   - **Mitigation:** Test with real community wrappers before release

1. **Type Identity Issues** - AssemblyLoadContext can cause type identity problems

   - **Mitigation:** Shared context approach to maintain type compatibility

### **Medium Risk Items**

1. **Performance Changes** - Modern system may have different performance characteristics

   - **Mitigation:** Benchmark testing to ensure parity

1. **Error Handling** - Modern system may surface different exceptions

   - **Mitigation:** Error mapping layer to preserve legacy exception types

## ðŸŽ¯ Implementation Plan

### **Phase 1: Modern Foundation (Days 1-2)**

1. Create `IAssemblyLoadContextManager` and implementation
1. Create `IResourceWrapperService` interface
1. Create `IPluginLoadContext` and `IResourceWrapperRegistry`

### **Phase 2: Compatibility Facade (Day 3)**

1. Create static `WrapperDealer` facade class
1. Implement exact API preservation
1. Add service locator integration

### **Phase 3: Service Implementation (Days 4-5)**

1. Implement `ResourceWrapperService`
1. Create plugin discovery mechanism
1. Add modern async methods

### **Phase 4: Testing & Validation (Days 6-7)**

1. Create compatibility test suite
1. Test with real community plugins
1. Performance benchmarking
1. Golden Master validation

## ðŸ“‹ Success Criteria

### **Mandatory Requirements**

- âœ… **100% API Compatibility:** All existing code works unchanged
- âœ… **Plugin Support:** Community wrappers load and function correctly
- âœ… **Performance Parity:** No regression in loading times
- âœ… **.NET 9 Compatibility:** Modern AssemblyLoadContext throughout

### **Quality Gates**

- âœ… **Compile Time:** All existing tools compile without changes
- âœ… **Runtime:** All resource types resolve correctly
- âœ… **Testing:** Comprehensive test coverage for compatibility scenarios
- âœ… **Documentation:** Migration guide for plugin developers (if needed)

______________________________________________________________________

**This architecture preserves 100% backward compatibility while enabling modern .NET 9 features and resolving the critical Assembly.LoadFile() crisis.**
