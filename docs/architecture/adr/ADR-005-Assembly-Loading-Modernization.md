# ADR-005: Assembly Loading Modernization

**Status:** Accepted
**Date:** August 8, 2025
**Deciders:** Architecture Team, Senior Developers

## Context

The legacy Sims4Tools codebase contains a critical compatibility issue in `WrapperDealer.cs:85` where `Assembly.LoadFile()` is used for plugin loading. This pattern breaks completely in .NET 8+ due to changes in AssemblyLoadContext behavior, creating a blocking issue for migration.

The existing plugin system dynamically loads resource wrapper assemblies from the filesystem to extend package parsing capabilities. This functionality is essential for the modding community and must be preserved with identical external APIs.

## Decision

We will replace `Assembly.LoadFile()` with modern `AssemblyLoadContext` patterns while maintaining complete backward compatibility for existing plugins through an abstraction layer.

## Rationale

### Critical Issue Analysis

```csharp
// Current problematic code (WrapperDealer.cs:85)
Assembly dotNetDll = Assembly.LoadFile(path);  // BREAKS IN .NET 8+

// Why it breaks:
// 1. AssemblyLoadContext isolation changes behavior
// 2. Type identity issues across contexts
// 3. Plugin assembly resolution failures
// 4. Memory leak potential with unloadable contexts
```

### Root Cause

- **Legacy Pattern**: Direct file loading without context management
- **.NET Evolution**: AssemblyLoadContext becomes primary loading mechanism
- **Plugin Dependencies**: Complex cross-assembly type resolution required
- **Performance Impact**: Memory leaks from unmanaged assembly contexts

## Implementation Design

### Modern Assembly Loading Architecture

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

    public void UnloadContext(string contextName)
    {
        if (_contexts.TryRemove(contextName, out var context))
        {
            context.Unload();
        }
    }
}
```

### Backward Compatibility Layer

```csharp
// Legacy WrapperDealer compatibility (preserves existing API)
public static class WrapperDealer
{
    private static IAssemblyLoadContextManager _assemblyManager;

    static WrapperDealer()
    {
        _assemblyManager = ServiceProvider.GetService<IAssemblyLoadContextManager>();
    }

    // Existing method signature preserved exactly
    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie)
    {
        // Implementation uses modern loading internally
        return _resourceService.GetResource(APIversion, pkg, rie);
    }

    // Plugin discovery with modern loading
    internal static void LoadPluginAssemblies(string directory)
    {
        foreach (var assemblyPath in Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                // Modern assembly loading
                var assembly = _assemblyManager.LoadFromPath(assemblyPath);
                var types = _assemblyManager.GetTypesFromAssembly(assembly);

                // Existing registration logic preserved
                RegisterResourceHandlers(types);
            }
            catch (PluginLoadException ex)
            {
                // Graceful degradation - log but continue
                Logger.LogWarning("Plugin load failed: {Path} - {Error}", assemblyPath, ex.Message);
            }
        }
    }
}
```

### Plugin Compatibility Strategy

```csharp
// Adapter pattern for legacy plugin support
public class LegacyPluginAdapter
{
    private readonly IAssemblyLoadContextManager _assemblyManager;

    public async Task<IEnumerable<IResourceWrapper>> AdaptLegacyHandlersAsync(Assembly assembly)
    {
        var wrappers = new List<IResourceWrapper>();
        var types = _assemblyManager.GetTypesFromAssembly(assembly);

        foreach (var type in types)
        {
            if (IsLegacyResourceHandler(type))
            {
                // Create adapter that bridges legacy AResourceHandler to modern interface
                var adapter = new LegacyResourceHandlerAdapter(type);
                wrappers.Add(adapter);
            }
        }

        return wrappers;
    }

    private bool IsLegacyResourceHandler(Type type)
    {
        return type.IsSubclassOf(typeof(AResourceHandler)) &&
               !type.IsAbstract &&
               type.GetConstructor(Type.EmptyTypes) != null;
    }
}

public class LegacyResourceHandlerAdapter : IResourceWrapper
{
    private readonly AResourceHandler _legacyHandler;

    public LegacyResourceHandlerAdapter(Type handlerType)
    {
        _legacyHandler = (AResourceHandler)Activator.CreateInstance(handlerType);
    }

    public async Task<IResource> CreateResourceAsync(string resourceType, Stream data)
    {
        // Bridge synchronous legacy API to modern async interface
        return await Task.Run(() =>
        {
            var constructor = _legacyHandler[resourceType]?.GetConstructor(
                new[] { typeof(int), typeof(Stream) });
            return (IResource)constructor?.Invoke(new object[] { 0, data });
        });
    }
}
```

## Migration Strategy

### Phase 1: Infrastructure (Month 1)

```csharp
// Step 1: Create abstraction without breaking changes
public interface IAssemblyLoadContextManager { /* ... */ }

// Step 2: Implement modern loading with legacy fallback
public class HybridAssemblyLoader : IAssemblyLoadContextManager
{
    public Assembly LoadFromPath(string assemblyPath)
    {
        #if NET48
        return Assembly.LoadFile(assemblyPath);  // Legacy support
        #else
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        #endif
    }
}
```

### Phase 2: Modern Implementation (Month 2)

```csharp
// Step 3: Full modern implementation
public class ModernAssemblyLoadContextManager : IAssemblyLoadContextManager
{
    // Full implementation with context management, error handling, etc.
}
```

### Phase 3: Integration & Testing (Month 3)

```csharp
// Step 4: Integration with dependency injection
services.AddSingleton<IAssemblyLoadContextManager, ModernAssemblyLoadContextManager>();

// Step 5: Comprehensive testing with real plugin assemblies
[Fact]
public async Task LoadLegacyPlugin_MaintainsCompatibility()
{
    var manager = new ModernAssemblyLoadContextManager();
    var assembly = manager.LoadFromPath("TestPlugin.dll");

    // Verify legacy AResourceHandler can be instantiated and used
    var types = manager.GetTypesFromAssembly(assembly);
    var handlerType = types.FirstOrDefault(t => t.IsSubclassOf(typeof(AResourceHandler)));

    Assert.NotNull(handlerType);
    var handler = Activator.CreateInstance(handlerType) as AResourceHandler;
    Assert.NotNull(handler);
}
```

## Error Handling & Diagnostics

### Comprehensive Error Recovery

```csharp
public class PluginLoadException : Exception
{
    public string AssemblyPath { get; }
    public LoadFailureReason Reason { get; }

    public PluginLoadException(string assemblyPath, LoadFailureReason reason, Exception innerException)
        : base($"Plugin load failed: {assemblyPath} - {reason}", innerException)
    {
        AssemblyPath = assemblyPath;
        Reason = reason;
    }
}

public enum LoadFailureReason
{
    FileNotFound,
    InvalidAssembly,
    DependencyMissing,
    SecurityException,
    IncompatibleFramework
}
```

### Diagnostic Support

```csharp
public interface IAssemblyLoadDiagnostics
{
    Task<AssemblyLoadReport> GenerateLoadReportAsync();
    Task<bool> ValidatePluginCompatibilityAsync(string assemblyPath);
    Task<IEnumerable<string>> GetLoadedAssemblyPathsAsync();
}

public class AssemblyLoadReport
{
    public int TotalAssembliesLoaded { get; set; }
    public int SuccessfulLoads { get; set; }
    public int FailedLoads { get; set; }
    public List<PluginLoadException> LoadErrors { get; set; }
    public TimeSpan TotalLoadTime { get; set; }
    public long MemoryUsageBytes { get; set; }
}
```

## Performance Considerations

### Memory Management

```csharp
public class OptimizedAssemblyLoadContextManager : IAssemblyLoadContextManager, IDisposable
{
    private readonly Timer _cleanupTimer;
    private readonly ConcurrentDictionary<string, WeakReference<AssemblyLoadContext>> _contexts = new();

    public OptimizedAssemblyLoadContextManager()
    {
        // Cleanup unused contexts every 5 minutes
        _cleanupTimer = new Timer(CleanupUnusedContexts, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    private void CleanupUnusedContexts(object state)
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in _contexts)
        {
            if (!kvp.Value.TryGetTarget(out var context))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _contexts.TryRemove(key, out _);
        }
    }
}
```

### Load Performance Optimization

```csharp
public class CachingAssemblyLoader : IAssemblyLoadContextManager
{
    private readonly ConcurrentDictionary<string, Assembly> _assemblyCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastModified = new();

    public Assembly LoadFromPath(string assemblyPath)
    {
        var lastWrite = File.GetLastWriteTime(assemblyPath);

        if (_assemblyCache.TryGetValue(assemblyPath, out var cached) &&
            _lastModified.TryGetValue(assemblyPath, out var cachedTime) &&
            cachedTime >= lastWrite)
        {
            return cached; // Return cached assembly if file hasn't changed
        }

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        _assemblyCache[assemblyPath] = assembly;
        _lastModified[assemblyPath] = lastWrite;

        return assembly;
    }
}
```

## Testing Strategy

### Unit Tests

```csharp
[Theory]
[InlineData("ValidPlugin.dll")]
[InlineData("LegacyResourceHandler.dll")]
public async Task LoadFromPath_ValidAssembly_ReturnsAssembly(string fileName)
{
    var testPath = Path.Combine(TestDataDirectory, fileName);
    var manager = new ModernAssemblyLoadContextManager();

    var assembly = manager.LoadFromPath(testPath);

    Assert.NotNull(assembly);
    Assert.Equal(Path.GetFileNameWithoutExtension(fileName), assembly.GetName().Name);
}

[Fact]
public void LoadFromPath_InvalidPath_ThrowsPluginLoadException()
{
    var manager = new ModernAssemblyLoadContextManager();

    var exception = Assert.Throws<PluginLoadException>(() =>
        manager.LoadFromPath("NonexistentFile.dll"));

    Assert.Equal(LoadFailureReason.FileNotFound, exception.Reason);
}
```

### Integration Tests

```csharp
[Fact]
public async Task FullWorkflow_LoadAndExecutePlugin_MaintainsCompatibility()
{
    // Test complete workflow: load assembly -> instantiate handler -> process resource
    var manager = new ModernAssemblyLoadContextManager();
    var adapter = new LegacyPluginAdapter(manager);

    var assembly = manager.LoadFromPath("TestResourceHandler.dll");
    var wrappers = await adapter.AdaptLegacyHandlersAsync(assembly);

    Assert.Single(wrappers);

    var wrapper = wrappers.First();
    using var testStream = new MemoryStream(TestResourceData);
    var resource = await wrapper.CreateResourceAsync("0x12345678", testStream);

    Assert.NotNull(resource);
    Assert.IsAssignableFrom<IResource>(resource);
}
```

## Security Considerations

### Assembly Validation

```csharp
public class SecureAssemblyLoader : IAssemblyLoadContextManager
{
    private readonly IAssemblyValidator _validator;

    public Assembly LoadFromPath(string assemblyPath)
    {
        // Validate assembly before loading
        var validationResult = _validator.ValidateAssembly(assemblyPath);
        if (!validationResult.IsValid)
        {
            throw new SecurityException($"Assembly validation failed: {validationResult.ErrorMessage}");
        }

        return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
    }
}

public interface IAssemblyValidator
{
    AssemblyValidationResult ValidateAssembly(string assemblyPath);
}

public class AssemblyValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
    public SecurityRisk RiskLevel { get; set; }
}
```

## Consequences

### Positive

- **Compatibility Preserved**: All existing plugins continue working
- **Modern Architecture**: Leverages .NET 9 assembly loading best practices
- **Performance Improved**: Better memory management and caching
- **Diagnostics Enhanced**: Comprehensive error reporting and monitoring
- **Security Improved**: Assembly validation and sandboxing capabilities

### Challenges

- **Complexity Added**: More sophisticated loading logic
- **Testing Overhead**: Must validate with all existing plugins
- **Migration Risk**: Critical path component requiring careful validation
- **Performance Monitoring**: Need to ensure loading performance doesn't regress

### Mitigation Strategies

- **Comprehensive Testing**: Test suite with real plugin assemblies
- **Gradual Rollout**: Feature flag for modern vs legacy loading
- **Fallback Mechanism**: Automatic fallback to compatible mode
- **Community Beta**: Early testing with plugin developers
- **Performance Benchmarks**: Continuous monitoring of load times

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Plugin Compatibility** | 100% | Automated test suite with existing plugins |
| **Load Performance** | â‰¤ +5% vs original | Assembly load time benchmarks |
| **Memory Usage** | â‰¤ original | Memory profiling during plugin loading |
| **Error Rate** | \<1% | Production telemetry |

## Related Decisions

- ADR-001: .NET 9 Framework (enables modern assembly loading)
- ADR-002: Dependency Injection (provides service abstraction)
- ADR-004: Greenfield Migration Strategy (context for this modernization)
- ADR-007: Plugin Architecture Modernization
