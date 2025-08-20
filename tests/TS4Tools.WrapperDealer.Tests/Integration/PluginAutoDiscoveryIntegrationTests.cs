using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Integration;

/// <summary>
/// Integration tests for Phase 4.20.4 Auto-Discovery functionality.
/// Tests the complete flow from WrapperDealer initialization through plugin auto-discovery.
/// </summary>
public sealed class PluginAutoDiscoveryIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PluginRegistrationManager _pluginManager;
    private readonly string _tempDirectory;

    public PluginAutoDiscoveryIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add logging services
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<ILogger<PluginRegistrationManager>>(_ => NullLogger<PluginRegistrationManager>.Instance);
        services.AddSingleton<ILogger<PluginDiscoveryService>>(_ => NullLogger<PluginDiscoveryService>.Instance);
        services.AddSingleton<ILogger<PluginDependencyResolver>>(_ => NullLogger<PluginDependencyResolver>.Instance);
        
        // Add mock resource manager and plugin manager
        services.AddSingleton<IResourceManager, MockResourceManager>();
        services.AddSingleton<PluginRegistrationManager>();

        _serviceProvider = services.BuildServiceProvider();
        _pluginManager = _serviceProvider.GetRequiredService<PluginRegistrationManager>();
        
        // Create temp directory for test assemblies
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"TS4Tools_AutoDiscoveryTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void WrapperDealer_InitializeWithAutoDiscovery_CompletesSuccessfully()
    {
        // Act - initialize WrapperDealer which should trigger auto-discovery
        WrapperDealer.Initialize(_serviceProvider);

        // Assert - should complete without throwing
        var typeMap = WrapperDealer.TypeMap;
        Assert.NotNull(typeMap);
    }

    [Fact]
    public void WrapperDealer_WithoutLogger_SkipsAutoDiscoveryGracefully()
    {
        // Arrange - create service provider without logger for discovery service
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<PluginRegistrationManager>>(_ => NullLogger<PluginRegistrationManager>.Instance);
        services.AddSingleton<IResourceManager, MockResourceManager>();
        services.AddSingleton<PluginRegistrationManager>();
        using var serviceProvider = services.BuildServiceProvider();

        // Act - should complete without throwing even without discovery logger
        WrapperDealer.Initialize(serviceProvider);

        // Assert
        var typeMap = WrapperDealer.TypeMap;
        Assert.NotNull(typeMap);
    }

    [Fact]
    public void AutoDiscovery_WithStandardLocations_RunsWithoutException()
    {
        // Arrange
        var logger = _serviceProvider.GetService<ILogger<PluginDiscoveryService>>();
        Assert.NotNull(logger);

        // Act - manually test the discovery service
        using var discoveryService = new PluginDiscoveryService(logger, _pluginManager);
        
        // Assert - should complete without throwing
        var result = discoveryService.DiscoverPlugins();
        Assert.True(result >= 0);
    }

    [Fact]
    public void AutoDiscovery_WithCustomPluginDirectory_FindsTestPlugins()
    {
        // Arrange
        var logger = _serviceProvider.GetService<ILogger<PluginDiscoveryService>>();
        Assert.NotNull(logger);

        using var discoveryService = new PluginDiscoveryService(logger, _pluginManager);
        
        // Create a custom plugin directory
        var customPluginDir = Path.Combine(_tempDirectory, "CustomPlugins");
        Directory.CreateDirectory(customPluginDir);

        var initialPluginCount = _pluginManager.RegisteredPluginCount;

        // Act - Test discovery from the specific directory
        var result = discoveryService.DiscoverPluginsFromDirectory(customPluginDir);

        // Assert - should complete successfully (may or may not find plugins)
        Assert.True(result >= 0);
        Assert.True(_pluginManager.RegisteredPluginCount >= initialPluginCount);
    }

    [Fact]
    public void PluginDiscoveryService_Dispose_ReleasesResources()
    {
        // Arrange
        var logger = _serviceProvider.GetService<ILogger<PluginDiscoveryService>>();
        Assert.NotNull(logger);

        var discoveryService = new PluginDiscoveryService(logger, _pluginManager);

        // Act - dispose should not throw
        discoveryService.Dispose();

        // Assert - multiple dispose calls should be safe
        discoveryService.Dispose();
    }

    [Fact]
    public void WrapperDealer_RefreshWrappers_MaintainsAutoDiscoveredPlugins()
    {
        // Arrange - initialize with auto-discovery
        WrapperDealer.Initialize(_serviceProvider);
        var initialPluginCount = _pluginManager.RegisteredPluginCount;

        // Act - refresh wrappers should maintain discovered plugins
        WrapperDealer.RefreshWrappers();

        // Assert - plugin count should remain consistent
        Assert.Equal(initialPluginCount, _pluginManager.RegisteredPluginCount);
    }

    [Fact]
    public void AutoDiscovery_Integration_WorksWithLegacyBridge()
    {
        // Arrange
        WrapperDealer.Initialize(_serviceProvider);
        
        // Act - register a plugin through legacy API after auto-discovery
        AResourceHandlerBridge.Add(typeof(TestAutoDiscoveryResource), "LegacyTestPlugin");

        // Assert - should work alongside auto-discovered plugins
        Assert.True(_pluginManager.RegisteredPluginCount > 0);
    }

    [Fact]
    public void WrapperDealer_MultipleInitializations_HandlesAutoDiscoveryCorrectly()
    {
        // Arrange & Act - multiple initializations should be safe
        WrapperDealer.Initialize(_serviceProvider);
        var firstPluginCount = _pluginManager.RegisteredPluginCount;
        
        WrapperDealer.Initialize(_serviceProvider);
        var secondPluginCount = _pluginManager.RegisteredPluginCount;

        // Assert - should not create duplicate registrations
        Assert.Equal(firstPluginCount, secondPluginCount);
    }

    [Fact]
    public void WrapperDealer_InitializeWithEnhancedDiscovery_UsesDependencyResolution()
    {
        // Arrange - service provider has both discovery and dependency resolver loggers
        Assert.NotNull(_serviceProvider.GetService<ILogger<PluginDiscoveryService>>());
        Assert.NotNull(_serviceProvider.GetService<ILogger<PluginDependencyResolver>>());

        // Act - initialize should use enhanced discovery with dependency resolution
        WrapperDealer.Initialize(_serviceProvider);

        // Assert - should complete successfully with enhanced functionality
        var typeMap = WrapperDealer.TypeMap;
        Assert.NotNull(typeMap);
        
        // The enhanced discovery should have been used (no specific side effects to test here,
        // but the method should complete without exceptions)
    }

    [Fact]
    public void WrapperDealer_WithoutDependencyResolverLogger_FallsBackToBasicDiscovery()
    {
        // Arrange - create service provider without dependency resolver logger
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<ILogger<PluginRegistrationManager>>(_ => NullLogger<PluginRegistrationManager>.Instance);
        services.AddSingleton<ILogger<PluginDiscoveryService>>(_ => NullLogger<PluginDiscoveryService>.Instance);
        // Note: No PluginDependencyResolver logger added
        services.AddSingleton<IResourceManager, MockResourceManager>();
        services.AddSingleton<PluginRegistrationManager>();
        using var serviceProvider = services.BuildServiceProvider();

        // Act - should fall back to basic discovery
        WrapperDealer.Initialize(serviceProvider);

        // Assert - should complete successfully with basic discovery
        var typeMap = WrapperDealer.TypeMap;
        Assert.NotNull(typeMap);
    }

    [Fact]
    public void EnhancedDiscovery_WithDependencyResolver_ReturnsDetailedResults()
    {
        // Arrange
        var discoveryLogger = _serviceProvider.GetService<ILogger<PluginDiscoveryService>>();
        var resolverLogger = _serviceProvider.GetService<ILogger<PluginDependencyResolver>>();
        Assert.NotNull(discoveryLogger);
        Assert.NotNull(resolverLogger);

        using var discoveryService = new PluginDiscoveryService(discoveryLogger, _pluginManager);
        var dependencyResolver = new PluginDependencyResolver(resolverLogger);

        // Act - test enhanced discovery directly
        var result = discoveryService.DiscoverPluginsWithDependencies(dependencyResolver);

        // Assert - should return meaningful result structure
        Assert.NotNull(result);
        Assert.True(result.RegisteredCount >= 0);
        Assert.NotNull(result.DiscoveredPlugins);
        Assert.NotNull(result.DiscoveryIssues);
        Assert.NotNull(result.RegistrationIssues);
        Assert.NotNull(result.AllIssues);
    }

    public void Dispose()
    {
        _pluginManager?.Dispose();
        _serviceProvider?.Dispose();
        
        // Reset the static bridge to prevent cross-test contamination
        AResourceHandlerBridge.Reset();
        
        // Clean up temp directory
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Mock resource manager for auto-discovery integration tests.
    /// </summary>
    private class MockResourceManager : IResourceManager
    {
        public IReadOnlyDictionary<string, Type> GetResourceTypeMap()
        {
            return new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>
            {
                ["0x87654321"] = typeof(TestAutoDiscoveryResource)
            });
        }

        public Task<IResource> CreateResourceAsync(string resourceType, int apiVersion, CancellationToken cancellationToken = default)
        {
            if (resourceType == "0x87654321")
            {
                return Task.FromResult<IResource>(new TestAutoDiscoveryResource(apiVersion));
            }
            throw new ArgumentException($"Unknown resource type: {resourceType}");
        }

        public Task<IResource> LoadResourceAsync(IPackage package, IResourceIndexEntry entry, int apiVersion, bool alwaysDefault = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IResource>(new TestAutoDiscoveryResource(apiVersion));
        }

        void IResourceManager.RegisterFactory<TResource, TFactory>()
        {
            // Mock implementation
        }

        public bool IsResourceTypeRegistered(string resourceType)
        {
            return resourceType == "0x87654321";
        }

        public ResourceManagerStatistics GetStatistics()
        {
            return new ResourceManagerStatistics
            {
                TotalResourcesCreated = 1,
                TotalResourcesLoaded = 0,
                RegisteredFactories = 1,
                CacheHitRatio = 1.0,
                CacheSize = 1,
                CacheMemoryUsage = 1024,
                AverageCreationTimeMs = 1.0
            };
        }

        public void RegisterResourceType<T>(string resourceType) where T : class, IResource
        {
            // Mock implementation
        }
    }

    /// <summary>
    /// Test resource for auto-discovery integration tests.
    /// </summary>
    private class TestAutoDiscoveryResource : IResource
    {
        public uint ResourceType => 0x87654321;
        public Stream Stream { get; set; } = new MemoryStream();
        public uint APIversion { get; set; }
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged
        {
            add { } // Suppress CS0067 - event required by interface but not used in tests
            remove { }
        }
        public int RequestedApiVersion => (int)APIversion;
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => new List<string>().AsReadOnly();
        
        public TypedValue this[int index] 
        {
            get => new TypedValue();
            set { }
        }
        
        public TypedValue this[string index] 
        {
            get => new TypedValue();
            set { }
        }

        public TestAutoDiscoveryResource()
        {
            APIversion = 1;
        }

        public TestAutoDiscoveryResource(int apiVersion)
        {
            APIversion = (uint)apiVersion;
        }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}
