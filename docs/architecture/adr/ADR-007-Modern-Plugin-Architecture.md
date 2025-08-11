# ADR-007: Modern Plugin Architecture with Legacy Compatibility

**Status:** Accepted
**Date:** August 8, 2025
**Deciders:** Architecture Team, Community Representatives

## Context

The legacy Sims4Tools ecosystem relies heavily on a plugin architecture for extending resource type support. The existing system uses `AResourceHandler` base classes that community developers extend to add support for new or custom resource types. This plugin ecosystem is critical for the modding community.

The greenfield migration requires modernizing this plugin system while maintaining 100% backward compatibility with existing plugins. The current plugin system has several issues:

- Assembly loading problems (addressed in ADR-005)
- Lack of dependency injection support
- No lifecycle management
- Limited error handling and diagnostics
- No versioning or compatibility checking
- Synchronous-only APIs

## Decision

We will implement a **modern plugin architecture with a legacy compatibility layer**, enabling both new modern plugins and existing legacy plugins to work seamlessly together.

## Rationale

### Requirements Analysis

| Requirement | Priority | Legacy System | Modern System | Compatibility Impact |
|-------------|----------|---------------|---------------|----------------------|
| **Existing Plugin Support** | **CRITICAL** | âœ… Via AResourceHandler | âœ… Via adapter pattern | **MUST preserve 100%** |
| **Modern DI Integration** | **HIGH** | âŒ Static pattern | âœ… Constructor injection | **Additive only** |
| **Async/Await Support** | **HIGH** | âŒ Sync only | âœ… Async-first | **Additive only** |
| **Resource Type Discovery** | **MEDIUM** | âœ… Via registration | âœ… Improved discovery | **Enhanced, compatible** |
| **Error Handling** | **HIGH** | âš ï¸ Basic | âœ… Comprehensive | **Non-breaking improvement** |
| **Performance** | **HIGH** | âš ï¸ Moderate | âœ… Optimized | **Must not regress** |

### Plugin Ecosystem Impact

The Sims modding community has developed numerous plugins over the years. Breaking compatibility would:

- âŒ Fragment the community between old and new tools
- âŒ Require all plugin developers to rewrite their code
- âŒ Create adoption barriers for the new TS4Tools
- âŒ Risk losing valuable community contributions

## Architecture Design

### Modern Plugin Interface

```csharp
// New modern plugin interface (async-first, DI-friendly)
public interface IResourcePlugin
{
    string Name { get; }
    Version Version { get; }
    string Description { get; }
    IEnumerable<ResourceTypeInfo> SupportedTypes { get; }

    Task<bool> CanHandleAsync(ResourceTypeInfo resourceType);
    Task<IResource> CreateResourceAsync(ResourceCreationContext context);
    Task<ValidationResult> ValidateResourceAsync(IResource resource);
    Task InitializeAsync(IPluginContext context);
    Task ShutdownAsync();
}

public class ResourceCreationContext
{
    public string ResourceType { get; set; }
    public uint ResourceGroup { get; set; }
    public ulong ResourceInstance { get; set; }
    public Stream Data { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
    public ILogger Logger { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

public class ResourceTypeInfo
{
    public string TypeId { get; set; }         // e.g., "0x12345678"
    public string DisplayName { get; set; }    // e.g., "Deformer Map"
    public string Description { get; set; }
    public Version MinimumVersion { get; set; }
    public bool IsDeprecated { get; set; }
}
```

### Legacy Compatibility Layer

```csharp
// Adapter that makes legacy AResourceHandler work with modern system
public class LegacyResourceHandlerAdapter : IResourcePlugin
{
    private readonly AResourceHandler _legacyHandler;
    private readonly ILogger<LegacyResourceHandlerAdapter> _logger;

    public LegacyResourceHandlerAdapter(AResourceHandler legacyHandler, ILogger<LegacyResourceHandlerAdapter> logger)
    {
        _legacyHandler = legacyHandler ?? throw new ArgumentNullException(nameof(legacyHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Extract metadata from legacy handler
        Name = _legacyHandler.GetType().Name;
        Version = GetVersionFromAssembly(_legacyHandler.GetType().Assembly);
        SupportedTypes = ExtractSupportedTypes(_legacyHandler);
    }

    public string Name { get; }
    public Version Version { get; }
    public string Description => $"Legacy plugin adapter for {Name}";
    public IEnumerable<ResourceTypeInfo> SupportedTypes { get; }

    public async Task<bool> CanHandleAsync(ResourceTypeInfo resourceType)
    {
        return await Task.Run(() =>
        {
            try
            {
                return _legacyHandler.ContainsKey(resourceType.TypeId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Legacy handler {Handler} failed CanHandle check for {Type}",
                    Name, resourceType.TypeId);
                return false;
            }
        });
    }

    public async Task<IResource> CreateResourceAsync(ResourceCreationContext context)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Bridge to legacy synchronous API
                var resourceType = _legacyHandler[context.ResourceType];
                if (resourceType == null)
                {
                    throw new PluginException($"Resource type {context.ResourceType} not supported by {Name}");
                }

                var constructor = resourceType.GetConstructor(new[] { typeof(int), typeof(Stream) });
                if (constructor == null)
                {
                    throw new PluginException($"Resource type {resourceType.Name} missing required constructor");
                }

                // Create resource using legacy pattern
                var resource = (IResource)constructor.Invoke(new object[] { 0, context.Data });

                _logger.LogDebug("Successfully created resource {Type} using legacy handler {Handler}",
                    context.ResourceType, Name);

                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Legacy handler {Handler} failed to create resource {Type}",
                    Name, context.ResourceType);
                throw new PluginException($"Legacy plugin {Name} failed to create resource", ex);
            }
        });
    }

    public async Task<ValidationResult> ValidateResourceAsync(IResource resource)
    {
        // Legacy handlers don't have validation - assume valid
        return await Task.FromResult(ValidationResult.Success());
    }

    public async Task InitializeAsync(IPluginContext context)
    {
        // Legacy handlers don't have lifecycle - no-op
        _logger.LogDebug("Legacy handler {Handler} initialized", Name);
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        // Legacy handlers don't have lifecycle - no-op
        _logger.LogDebug("Legacy handler {Handler} shutdown", Name);
        await Task.CompletedTask;
    }

    private IEnumerable<ResourceTypeInfo> ExtractSupportedTypes(AResourceHandler handler)
    {
        var supportedTypes = new List<ResourceTypeInfo>();

        try
        {
            // Use reflection to extract supported resource types
            foreach (var key in handler.Keys)
            {
                supportedTypes.Add(new ResourceTypeInfo
                {
                    TypeId = key,
                    DisplayName = GetDisplayNameForType(key),
                    Description = $"Handled by legacy plugin {Name}",
                    MinimumVersion = Version
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract supported types from legacy handler {Handler}", Name);
        }

        return supportedTypes;
    }
}
```

### Modern Plugin Manager

```csharp
public interface IPluginManager
{
    Task<IEnumerable<IResourcePlugin>> DiscoverPluginsAsync(string directory);
    Task<IResourcePlugin> GetPluginForResourceTypeAsync(string resourceType);
    Task<IEnumerable<IResourcePlugin>> GetAllPluginsAsync();
    Task<bool> RegisterPluginAsync(IResourcePlugin plugin);
    Task<bool> UnregisterPluginAsync(string pluginName);
    Task InitializeAllPluginsAsync();
    Task ShutdownAllPluginsAsync();
}

public class ModernPluginManager : IPluginManager
{
    private readonly IAssemblyLoadContextManager _assemblyLoader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModernPluginManager> _logger;
    private readonly ConcurrentDictionary<string, IResourcePlugin> _plugins = new();
    private readonly ConcurrentDictionary<string, List<IResourcePlugin>> _typeToPlugins = new();

    public ModernPluginManager(
        IAssemblyLoadContextManager assemblyLoader,
        IServiceProvider serviceProvider,
        ILogger<ModernPluginManager> logger)
    {
        _assemblyLoader = assemblyLoader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<IResourcePlugin>> DiscoverPluginsAsync(string directory)
    {
        var discoveredPlugins = new List<IResourcePlugin>();

        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Plugin directory does not exist: {Directory}", directory);
            return discoveredPlugins;
        }

        var assemblyFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);
        _logger.LogInformation("Scanning {Count} assemblies for plugins in {Directory}",
            assemblyFiles.Length, directory);

        var discoveryTasks = assemblyFiles.Select(async assemblyPath =>
        {
            try
            {
                return await DiscoverPluginsInAssemblyAsync(assemblyPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover plugins in assembly: {Assembly}", assemblyPath);
                return Enumerable.Empty<IResourcePlugin>();
            }
        });

        var results = await Task.WhenAll(discoveryTasks);
        discoveredPlugins.AddRange(results.SelectMany(r => r));

        _logger.LogInformation("Discovered {Count} plugins total", discoveredPlugins.Count);
        return discoveredPlugins;
    }

    private async Task<IEnumerable<IResourcePlugin>> DiscoverPluginsInAssemblyAsync(string assemblyPath)
    {
        var plugins = new List<IResourcePlugin>();

        try
        {
            var assembly = _assemblyLoader.LoadFromPath(assemblyPath);
            var types = _assemblyLoader.GetTypesFromAssembly(assembly);

            // Look for modern plugins first
            var modernPlugins = await DiscoverModernPluginsAsync(types);
            plugins.AddRange(modernPlugins);

            // Look for legacy resource handlers
            var legacyPlugins = await DiscoverLegacyPluginsAsync(types);
            plugins.AddRange(legacyPlugins);

            if (plugins.Any())
            {
                _logger.LogDebug("Found {Count} plugins in {Assembly}: {PluginNames}",
                    plugins.Count, Path.GetFileName(assemblyPath),
                    string.Join(", ", plugins.Select(p => p.Name)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering plugins in assembly: {Assembly}", assemblyPath);
        }

        return plugins;
    }

    private async Task<IEnumerable<IResourcePlugin>> DiscoverModernPluginsAsync(Type[] types)
    {
        var plugins = new List<IResourcePlugin>();

        var modernPluginTypes = types
            .Where(t => typeof(IResourcePlugin).IsAssignableFrom(t))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null || HasServiceConstructor(t));

        foreach (var pluginType in modernPluginTypes)
        {
            try
            {
                var plugin = CreateModernPlugin(pluginType);
                plugins.Add(plugin);

                _logger.LogDebug("Discovered modern plugin: {Plugin} v{Version}",
                    plugin.Name, plugin.Version);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create modern plugin: {Type}", pluginType.Name);
            }
        }

        return plugins;
    }

    private async Task<IEnumerable<IResourcePlugin>> DiscoverLegacyPluginsAsync(Type[] types)
    {
        var plugins = new List<IResourcePlugin>();

        var legacyHandlerTypes = types
            .Where(t => t.IsSubclassOf(typeof(AResourceHandler)))
            .Where(t => !t.IsAbstract)
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var handlerType in legacyHandlerTypes)
        {
            try
            {
                var handler = (AResourceHandler)Activator.CreateInstance(handlerType);
                var adapter = new LegacyResourceHandlerAdapter(handler,
                    _serviceProvider.GetRequiredService<ILogger<LegacyResourceHandlerAdapter>>());

                plugins.Add(adapter);

                _logger.LogDebug("Discovered legacy plugin: {Plugin} v{Version} with {TypeCount} types",
                    adapter.Name, adapter.Version, adapter.SupportedTypes.Count());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create legacy plugin adapter: {Type}", handlerType.Name);
            }
        }

        return plugins;
    }

    public async Task<IResourcePlugin> GetPluginForResourceTypeAsync(string resourceType)
    {
        if (_typeToPlugins.TryGetValue(resourceType, out var candidates))
        {
            // Return first plugin that can handle the type
            foreach (var plugin in candidates)
            {
                try
                {
                    var resourceTypeInfo = new ResourceTypeInfo { TypeId = resourceType };
                    if (await plugin.CanHandleAsync(resourceTypeInfo))
                    {
                        return plugin;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Plugin {Plugin} failed CanHandle check for {Type}",
                        plugin.Name, resourceType);
                }
            }
        }

        _logger.LogDebug("No plugin found for resource type: {Type}", resourceType);
        return null;
    }
}
```

### Plugin Context and Services

```csharp
public interface IPluginContext
{
    IServiceProvider ServiceProvider { get; }
    IConfiguration Configuration { get; }
    ILogger Logger { get; }
    string PluginDirectory { get; }
    Version HostVersion { get; }
    IDictionary<string, object> Properties { get; }
}

public class PluginContext : IPluginContext
{
    public PluginContext(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger,
        string pluginDirectory,
        Version hostVersion)
    {
        ServiceProvider = serviceProvider;
        Configuration = configuration;
        Logger = logger;
        PluginDirectory = pluginDirectory;
        HostVersion = hostVersion;
        Properties = new ConcurrentDictionary<string, object>();
    }

    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
    public string PluginDirectory { get; }
    public Version HostVersion { get; }
    public IDictionary<string, object> Properties { get; }
}

// Example modern plugin implementation
public class ExampleModernResourcePlugin : IResourcePlugin
{
    private readonly ILogger<ExampleModernResourcePlugin> _logger;
    private readonly IResourceSerializer _serializer;

    // Modern constructor with DI
    public ExampleModernResourcePlugin(
        ILogger<ExampleModernResourcePlugin> logger,
        IResourceSerializer serializer)
    {
        _logger = logger;
        _serializer = serializer;
    }

    public string Name => "Example Modern Plugin";
    public Version Version => new Version(2, 0, 0);
    public string Description => "Example of modern plugin architecture";

    public IEnumerable<ResourceTypeInfo> SupportedTypes => new[]
    {
        new ResourceTypeInfo
        {
            TypeId = "0x12345678",
            DisplayName = "Example Resource",
            Description = "Example resource type handled by modern plugin",
            MinimumVersion = new Version(1, 0, 0)
        }
    };

    public async Task<bool> CanHandleAsync(ResourceTypeInfo resourceType)
    {
        return SupportedTypes.Any(t => t.TypeId.Equals(resourceType.TypeId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IResource> CreateResourceAsync(ResourceCreationContext context)
    {
        _logger.LogDebug("Creating resource {Type} for instance {Instance}",
            context.ResourceType, context.ResourceInstance);

        try
        {
            // Modern async implementation
            var resourceData = await _serializer.DeserializeAsync(context.Data, context.CancellationToken);
            return new ExampleResource(resourceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create resource {Type}", context.ResourceType);
            throw new PluginException($"Failed to create {context.ResourceType} resource", ex);
        }
    }

    public async Task<ValidationResult> ValidateResourceAsync(IResource resource)
    {
        if (resource is ExampleResource exampleResource)
        {
            return await ValidateExampleResourceAsync(exampleResource);
        }

        return ValidationResult.Error("Resource type not supported for validation");
    }

    public async Task InitializeAsync(IPluginContext context)
    {
        _logger.LogInformation("Initializing {Plugin} v{Version}", Name, Version);

        // Plugin-specific initialization
        await LoadConfigurationAsync(context.Configuration);
        await ValidateEnvironmentAsync(context);

        _logger.LogInformation("{Plugin} initialized successfully", Name);
    }

    public async Task ShutdownAsync()
    {
        _logger.LogInformation("Shutting down {Plugin}", Name);

        // Cleanup resources
        await FlushPendingOperationsAsync();

        _logger.LogInformation("{Plugin} shutdown complete", Name);
    }
}
```

## Migration Strategy

### Phase 1: Infrastructure (Month 1)

```csharp
// Step 1: Create modern interfaces without breaking existing code
public interface IResourcePlugin { /* ... */ }
public interface IPluginManager { /* ... */ }

// Step 2: Implement legacy compatibility layer
public class LegacyResourceHandlerAdapter : IResourcePlugin { /* ... */ }

// Step 3: Create hybrid plugin manager that supports both
public class HybridPluginManager : IPluginManager
{
    // Supports both legacy AResourceHandler and modern IResourcePlugin
}
```

### Phase 2: Integration (Month 2)

```csharp
// Step 4: Update existing WrapperDealer to use plugin manager
public static class WrapperDealer
{
    private static IPluginManager _pluginManager;

    public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie)
    {
        // Use plugin manager internally while preserving external API
        var plugin = await _pluginManager.GetPluginForResourceTypeAsync(rie.ResourceType.ToString("X8"));
        if (plugin != null)
        {
            var context = new ResourceCreationContext { /* ... */ };
            return await plugin.CreateResourceAsync(context);
        }

        // Fallback to default behavior
        return CreateDefaultResource(APIversion, pkg, rie);
    }
}
```

### Phase 3: Modern Plugin Development (Month 3)

```csharp
// Step 5: Enable modern plugin development
services.AddSingleton<IPluginManager, ModernPluginManager>();

// Step 6: Provide modern plugin template and documentation
public class TemplateModernPlugin : IResourcePlugin
{
    // Template implementation for community developers
}
```

## Performance Considerations

### Plugin Loading Optimization

```csharp
public class OptimizedPluginManager : IPluginManager
{
    private readonly Lazy<Task<IEnumerable<IResourcePlugin>>> _lazyPlugins;

    public OptimizedPluginManager(/* dependencies */)
    {
        // Lazy loading of plugins to improve startup time
        _lazyPlugins = new Lazy<Task<IEnumerable<IResourcePlugin>>>(
            async () => await DiscoverAllPluginsAsync());
    }

    public async Task<IResourcePlugin> GetPluginForResourceTypeAsync(string resourceType)
    {
        // Use cache-first lookup for performance
        if (_typeToPluginCache.TryGetValue(resourceType, out var cachedPlugin))
        {
            return cachedPlugin;
        }

        var plugins = await _lazyPlugins.Value;
        var plugin = plugins.FirstOrDefault(p => p.SupportedTypes.Any(t => t.TypeId == resourceType));

        if (plugin != null)
        {
            _typeToPluginCache[resourceType] = plugin;
        }

        return plugin;
    }
}
```

### Memory Management

```csharp
public class MemoryEfficientPluginManager : IPluginManager, IDisposable
{
    private readonly ConcurrentBag<WeakReference<IResourcePlugin>> _plugins = new();
    private readonly Timer _cleanupTimer;

    public MemoryEfficientPluginManager()
    {
        // Periodic cleanup of unreferenced plugins
        _cleanupTimer = new Timer(CleanupUnreferencedPlugins, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    private void CleanupUnreferencedPlugins(object state)
    {
        var toRemove = new List<WeakReference<IResourcePlugin>>();

        foreach (var weakRef in _plugins)
        {
            if (!weakRef.TryGetTarget(out _))
            {
                toRemove.Add(weakRef);
            }
        }

        foreach (var weak in toRemove)
        {
            _plugins.TryTake(out weak);
        }

        if (toRemove.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} unreferenced plugin references", toRemove.Count);
        }
    }
}
```

## Testing Strategy

### Plugin Compatibility Testing

```csharp
[TestClass]
public class PluginCompatibilityTests
{
    private IPluginManager _pluginManager;
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAssemblyLoadContextManager, ModernAssemblyLoadContextManager>();
        services.AddSingleton<IPluginManager, ModernPluginManager>();
        _serviceProvider = services.BuildServiceProvider();
        _pluginManager = _serviceProvider.GetRequiredService<IPluginManager>();
    }

    [TestMethod]
    public async Task LegacyPlugin_LoadsCorrectly()
    {
        // Arrange
        var legacyPluginPath = Path.Combine(TestDataDirectory, "LegacyTestPlugin.dll");

        // Act
        var plugins = await _pluginManager.DiscoverPluginsAsync(Path.GetDirectoryName(legacyPluginPath));

        // Assert
        Assert.IsTrue(plugins.Any(), "Should discover at least one plugin");
        var legacyPlugin = plugins.FirstOrDefault(p => p.Name.Contains("LegacyTest"));
        Assert.IsNotNull(legacyPlugin, "Should find legacy test plugin");
        Assert.IsInstanceOfType(legacyPlugin, typeof(LegacyResourceHandlerAdapter));
    }

    [TestMethod]
    public async Task ModernPlugin_LoadsCorrectly()
    {
        // Arrange
        var modernPluginPath = Path.Combine(TestDataDirectory, "ModernTestPlugin.dll");

        // Act
        var plugins = await _pluginManager.DiscoverPluginsAsync(Path.GetDirectoryName(modernPluginPath));

        // Assert
        var modernPlugin = plugins.FirstOrDefault(p => p.Name.Contains("ModernTest"));
        Assert.IsNotNull(modernPlugin, "Should find modern test plugin");
        Assert.IsTrue(modernPlugin.GetType().GetInterfaces().Contains(typeof(IResourcePlugin)));
    }

    [TestMethod]
    public async Task BothPluginTypes_CanCoexist()
    {
        // Arrange
        var pluginDirectory = TestDataDirectory;

        // Act
        var plugins = await _pluginManager.DiscoverPluginsAsync(pluginDirectory);
        await _pluginManager.InitializeAllPluginsAsync();

        // Assert
        var legacyCount = plugins.Count(p => p is LegacyResourceHandlerAdapter);
        var modernCount = plugins.Count(p => !(p is LegacyResourceHandlerAdapter));

        Assert.IsTrue(legacyCount > 0, "Should have legacy plugins");
        Assert.IsTrue(modernCount > 0, "Should have modern plugins");

        // Verify both types work
        foreach (var plugin in plugins)
        {
            var supportedTypes = plugin.SupportedTypes.ToList();
            Assert.IsTrue(supportedTypes.Any(), $"Plugin {plugin.Name} should support some types");
        }
    }

    [TestMethod]
    public async Task PluginResourceCreation_ProducesIdenticalResults()
    {
        // Arrange
        var testResourceType = "0x12345678";
        var testData = GetTestResourceData();

        var legacyHandler = new TestLegacyResourceHandler();
        var legacyAdapter = new LegacyResourceHandlerAdapter(legacyHandler,
            _serviceProvider.GetRequiredService<ILogger<LegacyResourceHandlerAdapter>>());

        var modernPlugin = new TestModernResourcePlugin();

        // Act
        var legacyResult = await legacyAdapter.CreateResourceAsync(new ResourceCreationContext
        {
            ResourceType = testResourceType,
            Data = new MemoryStream(testData)
        });

        var modernResult = await modernPlugin.CreateResourceAsync(new ResourceCreationContext
        {
            ResourceType = testResourceType,
            Data = new MemoryStream(testData)
        });

        // Assert
        Assert.AreEqual(legacyResult.GetType(), modernResult.GetType());
        CollectionAssert.AreEqual(legacyResult.AsBytes, modernResult.AsBytes);
    }
}
```

### Performance Testing

```csharp
[TestClass]
public class PluginPerformanceTests
{
    [TestMethod]
    [DataRow(10)]
    [DataRow(100)]
    [DataRow(1000)]
    public async Task PluginDiscovery_PerformanceAcceptable(int pluginCount)
    {
        // Arrange
        var testDirectory = CreateTestPluginDirectory(pluginCount);
        var pluginManager = CreatePluginManager();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var plugins = await pluginManager.DiscoverPluginsAsync(testDirectory);
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(pluginCount, plugins.Count());
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < pluginCount * 10,
            $"Plugin discovery took too long: {stopwatch.ElapsedMilliseconds}ms for {pluginCount} plugins");
    }

    [TestMethod]
    public async Task PluginResourceCreation_PerformanceComparable()
    {
        // Arrange
        var legacyHandler = new TestLegacyResourceHandler();
        var modernPlugin = new TestModernResourcePlugin();
        var testData = GetLargeTestResourceData();

        // Act - Legacy
        var legacyStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var legacyResult = await CreateResourceWithLegacyHandler(legacyHandler, testData);
        }
        legacyStopwatch.Stop();

        // Act - Modern
        var modernStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var modernResult = await CreateResourceWithModernPlugin(modernPlugin, testData);
        }
        modernStopwatch.Stop();

        // Assert - Modern should be no more than 20% slower than legacy
        var slowdownRatio = (double)modernStopwatch.ElapsedMilliseconds / legacyStopwatch.ElapsedMilliseconds;
        Assert.IsTrue(slowdownRatio <= 1.2,
            $"Modern plugin too slow: {slowdownRatio:P0} vs legacy");
    }
}
```

## Documentation and Community Support

### Plugin Developer Guide

```markdown

# TS4Tools Plugin Development Guide

## Modern Plugin Development (Recommended)

### Creating a Modern Plugin

```csharp
public class MyResourcePlugin : IResourcePlugin
{
    public string Name => "My Resource Plugin";
    public Version Version => new Version(1, 0, 0);

    // Implement required methods...
}
```

### Dependency Injection Support

Modern plugins can use constructor injection:

```csharp
public class MyResourcePlugin : IResourcePlugin
{
    private readonly ILogger<MyResourcePlugin> _logger;
    private readonly IResourceValidator _validator;

    public MyResourcePlugin(ILogger<MyResourcePlugin> logger, IResourceValidator validator)
    {
        _logger = logger;
        _validator = validator;
    }
}
```

## Legacy Plugin Compatibility

Existing plugins using `AResourceHandler` continue to work without changes:

```csharp
public class MyLegacyHandler : AResourceHandler
{
    public MyLegacyHandler()
    {
        this.Add(typeof(MyResource), new List<string>(new[] { "0x12345678" }));
    }
}
```

## Migration Path

1. **Phase 1**: Continue using legacy plugins as-is
2. **Phase 2**: Gradually migrate to modern plugin interfaces
3. **Phase 3**: Take advantage of new features (async, DI, validation)

```

## Consequences

### Positive

- **100% Backward Compatibility**: All existing plugins continue working
- **Modern Development Experience**: New plugins can use modern .NET patterns
- **Performance Improvements**: Better memory management and async support
- **Enhanced Diagnostics**: Comprehensive logging and error handling
- **Community Continuity**: No disruption to existing plugin ecosystem
- **Future-Proof Architecture**: Extensible design for future enhancements

### Challenges

- **Increased Complexity**: Two plugin systems to maintain
- **Adapter Overhead**: Small performance cost for legacy plugin adapters
- **Testing Complexity**: Must validate both modern and legacy plugins
- **Documentation Burden**: Must document both old and new approaches

### Mitigation Strategies

- **Comprehensive Testing**: Automated test suite covering both plugin types
- **Performance Monitoring**: Continuous benchmarking of adapter overhead
- **Clear Migration Path**: Step-by-step guide for upgrading plugins
- **Community Support**: Active support for plugin developers during transition
- **Deprecation Timeline**: Clear timeline for eventual legacy support sunset

## Success Metrics

| Metric | Target | Measurement Method |
|--------|--------|--------------------|
| **Legacy Plugin Compatibility** | 100% | Automated test suite with existing plugins |
| **Performance Overhead** | â‰¤ 5% | Benchmark comparison of legacy vs adapted |
| **Plugin Discovery Time** | â‰¤ 2x legacy | Startup time measurement |
| **Modern Plugin Adoption** | 25% within 6 months | Plugin repository analysis |
| **Community Satisfaction** | 90%+ positive feedback | Developer surveys |

## Related Decisions

- ADR-005: Assembly Loading Modernization (enables plugin loading)
- ADR-002: Dependency Injection (enables modern plugin DI)
- ADR-004: Greenfield Migration Strategy (provides context)
- ADR-006: Golden Master Testing Strategy (validates plugin compatibility)

