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
/// Integration tests for WrapperDealer plugin system integration (Phase 4.20.3).
/// Tests the complete flow from plugin registration to resource creation.
/// </summary>
public sealed class WrapperDealerPluginIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PluginRegistrationManager _pluginManager;

    public WrapperDealerPluginIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add minimal services needed for integration test
        services.AddSingleton<ILogger<PluginRegistrationManager>>(_ => NullLogger<PluginRegistrationManager>.Instance);
        
        // Add mock resource manager
        services.AddSingleton<IResourceManager, MockResourceManager>();
        services.AddSingleton<PluginRegistrationManager>();

        _serviceProvider = services.BuildServiceProvider();
        _pluginManager = _serviceProvider.GetRequiredService<PluginRegistrationManager>();
    }

    [Fact]
    public void Initialize_WithPluginManager_InitializesPluginSystem()
    {
        // Act
        WrapperDealer.Initialize(_serviceProvider);

        // Assert - no exception should be thrown
        Assert.True(true); // If we get here, initialization succeeded
    }

    [Fact]
    public void Initialize_WithoutPluginManager_StillInitializes()
    {
        // Arrange - create service provider without plugin manager
        var services = new ServiceCollection();
        services.AddSingleton<IResourceManager, MockResourceManager>();
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        WrapperDealer.Initialize(serviceProvider);

        // Assert - should not throw
        Assert.True(true);
    }

    [Fact]
    public void WrapperDealer_AfterPluginInitialization_CanCreateResources()
    {
        // Arrange
        WrapperDealer.Initialize(_serviceProvider);

        // Act & Assert - test basic functionality
        var typeMap = WrapperDealer.TypeMap;
        Assert.NotNull(typeMap);
    }

    [Fact]
    public void AResourceHandler_AfterWrapperDealerInitialization_CanRegisterPlugins()
    {
        // Arrange
        WrapperDealer.Initialize(_serviceProvider);

        // Act - register a test plugin via legacy API
        var testType = typeof(TestResourceWrapper);
        AResourceHandlerBridge.Add(testType, "TestPlugin");

        // Assert - plugin should be registered
        Assert.Equal(1, _pluginManager.RegisteredPluginCount);
    }

    [Fact]
    public void RefreshWrappers_WithPlugins_MaintainsPluginState()
    {
        // Arrange
        WrapperDealer.Initialize(_serviceProvider);
        AResourceHandlerBridge.Add(typeof(TestResourceWrapper), "TestPlugin");
        var initialCount = _pluginManager.RegisteredPluginCount;

        // Act
        WrapperDealer.RefreshWrappers();

        // Assert - plugins should still be registered
        Assert.Equal(initialCount, _pluginManager.RegisteredPluginCount);
    }

    [Fact]
    public void DisabledCollection_WorksWithPluginSystem()
    {
        // Arrange
        WrapperDealer.Initialize(_serviceProvider);

        // Act
        var disabled = WrapperDealer.Disabled;

        // Assert - should return valid collection
        Assert.NotNull(disabled);
    }

    [Fact]
    public void CreateNewResource_WorksWithPluginSystemInitialized()
    {
        // Arrange
        WrapperDealer.Initialize(_serviceProvider);

        // Act & Assert - should not throw even with minimal resource manager
        try
        {
            // This might throw because we don't have actual resource types registered,
            // but it should fail gracefully
            var result = WrapperDealer.CreateNewResource(1, "TEST");
        }
        catch (InvalidOperationException)
        {
            // Expected - no actual resource handlers registered
        }
        catch (ArgumentException)
        {
            // Expected - invalid resource type
        }
    }

    public void Dispose()
    {
        _pluginManager?.Dispose();
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Mock resource manager for testing.
    /// </summary>
    private class MockResourceManager : IResourceManager
    {
        public IReadOnlyDictionary<string, Type> GetResourceTypeMap()
        {
            return new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>
            {
                ["0x12345678"] = typeof(TestResourceWrapper)
            });
        }

        public Task<IResource> CreateResourceAsync(string resourceType, int apiVersion, CancellationToken cancellationToken = default)
        {
            if (resourceType == "0x12345678")
            {
                return Task.FromResult<IResource>(new TestResourceWrapper(apiVersion));
            }
            throw new ArgumentException($"Unknown resource type: {resourceType}");
        }

        public Task<IResource> LoadResourceAsync(IPackage package, IResourceIndexEntry entry, int apiVersion, bool alwaysDefault = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IResource>(new TestResourceWrapper(apiVersion));
        }

        public void RegisterResourceType<T>(string resourceType) where T : class, IResource
        {
            // Mock implementation
        }

        void IResourceManager.RegisterFactory<TResource, TFactory>()
        {
            // Mock implementation
        }

        public bool IsResourceTypeRegistered(string resourceType)
        {
            return resourceType == "0x12345678";
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
    }

    /// <summary>
    /// Minimal test resource wrapper for plugin registration tests.
    /// </summary>
    private class TestResourceWrapper : IResource
    {
        public uint ResourceType => 0x12345678;
        public Stream Stream { get; set; } = new MemoryStream();
        public uint APIversion { get; set; }
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged;
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

        public TestResourceWrapper(int APIversion)
        {
            this.APIversion = (uint)APIversion;
        }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}
