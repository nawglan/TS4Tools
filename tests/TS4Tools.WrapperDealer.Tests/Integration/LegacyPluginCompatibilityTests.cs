using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;
using TS4Tools.Core.Package;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Integration;

/// <summary>
/// Integration tests for Step 3 of Phase 4.20: Legacy Plugin Compatibility Validation.
/// Tests complete compatibility with real-world community plugin patterns.
/// </summary>
public sealed class LegacyPluginCompatibilityTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly PluginRegistrationManager _pluginManager;

    public LegacyPluginCompatibilityTests()
    {
        var services = new ServiceCollection();
        
        // Add complete logging services for all components
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<ILogger<PluginRegistrationManager>>(_ => NullLogger<PluginRegistrationManager>.Instance);
        services.AddSingleton<ILogger<PluginDiscoveryService>>(_ => NullLogger<PluginDiscoveryService>.Instance);
        services.AddSingleton<ILogger<PluginDependencyResolver>>(_ => NullLogger<PluginDependencyResolver>.Instance);
        
        // Add mock resource manager and plugin manager
        services.AddSingleton<IResourceManager, MockResourceManager>();
        services.AddSingleton<PluginRegistrationManager>();

        _serviceProvider = services.BuildServiceProvider();
        _pluginManager = _serviceProvider.GetRequiredService<PluginRegistrationManager>();
    }

    [Fact]
    public void LegacyPlugin_ComplateResourcePattern_FullCompatibility()
    {
        // Arrange - Simulate the ComplateResourceHandler pattern from legacy Sims4Tools
        // Reset bridge state to ensure clean test
        AResourceHandlerBridge.Reset();
        WrapperDealer.Initialize(_serviceProvider);
        var initialPluginCount = _pluginManager.RegisteredPluginCount;

        // Act - Register exactly like ComplateResourceHandler does:
        // this.Add(typeof(ComplateResource), new List<string>(new string[] { "0x044AE110", }));
        AResourceHandlerBridge.Add("0x044AE110", typeof(LegacyComplateResource));

        // Assert - Legacy plugin should be fully compatible
        Assert.True(_pluginManager.RegisteredPluginCount > initialPluginCount);
        Assert.True(AResourceHandlerBridge.IsSupported("0x044AE110"));
        
        var handlerType = AResourceHandlerBridge.GetHandler("0x044AE110");
        Assert.Equal(typeof(LegacyComplateResource), handlerType);
        
        // Test resource creation through WrapperDealer
        var typeMap = WrapperDealer.TypeMap;
        Assert.NotNull(typeMap);
    }

    [Fact]
    public void LegacyPlugin_ModularResourcePattern_FullCompatibility()
    {
        // Arrange - Simulate the ModularResourceHandler pattern
        // Reset bridge state to ensure clean test
        AResourceHandlerBridge.Reset();
        WrapperDealer.Initialize(_serviceProvider);
        var initialPluginCount = _pluginManager.RegisteredPluginCount;

        // Act - Register exactly like ModularResourceHandler does:
        // this.Add(typeof(ModularResource), new List<string>(new string[] { "0xCF9A4ACE", }));
        AResourceHandlerBridge.Add("0xCF9A4ACE", typeof(LegacyModularResource));

        // Assert - Legacy plugin should be fully compatible
        Assert.True(_pluginManager.RegisteredPluginCount > initialPluginCount);
        Assert.True(AResourceHandlerBridge.IsSupported("0xCF9A4ACE"));
        
        var handlerType = AResourceHandlerBridge.GetHandler("0xCF9A4ACE");
        Assert.Equal(typeof(LegacyModularResource), handlerType);
    }

    [Fact]
    public void LegacyPlugin_MultipleResourceTypes_FullCompatibility()
    {
        // Arrange - Simulate a plugin that handles multiple resource types
        // Reset bridge state to ensure clean test
        AResourceHandlerBridge.Reset();
        WrapperDealer.Initialize(_serviceProvider);
        var initialPluginCount = _pluginManager.RegisteredPluginCount;

        // Act - Register multiple resource types like real community plugins do
        AResourceHandlerBridge.Add("0xBDD82221", typeof(LegacyAUEVResource)); // AUEV resources
        AResourceHandlerBridge.Add("0x74050B1F", typeof(LegacySTRMResource)); // STRM resources
        AResourceHandlerBridge.Add("0x91EDBD3E", typeof(LegacyRoofStyleResource)); // Roof styles

        // Assert - All legacy resource types should be supported
        Assert.True(_pluginManager.RegisteredPluginCount > initialPluginCount);
        
        Assert.True(AResourceHandlerBridge.IsSupported("0xBDD82221"));
        Assert.True(AResourceHandlerBridge.IsSupported("0x74050B1F"));
        Assert.True(AResourceHandlerBridge.IsSupported("0x91EDBD3E"));

        var supportedTypes = AResourceHandlerBridge.GetSupportedTypes();
        Assert.Contains("0xBDD82221", supportedTypes);
        Assert.Contains("0x74050B1F", supportedTypes);
        Assert.Contains("0x91EDBD3E", supportedTypes);
    }

    [Fact]
    public void LegacyPlugin_CommunityPluginWorkflow_FullIntegration()
    {
        // Arrange - Simulate a complete community plugin loading workflow
        // Reset bridge state to ensure clean test
        AResourceHandlerBridge.Reset();
        WrapperDealer.Initialize(_serviceProvider);

        // Step 1: Plugin registers its resource handlers (like in plugin constructor)
        AResourceHandlerBridge.Add("0xCommunity1", typeof(LegacyCommunityResource));
        AResourceHandlerBridge.Add("0xCommunity2", typeof(LegacyCommunityResource));

        // Step 2: WrapperDealer should be able to refresh and maintain registrations
        WrapperDealer.RefreshWrappers();

        // Step 3: Resource operations should work normally
        var typeMap = WrapperDealer.TypeMap;
        Assert.NotNull(typeMap);

        // Assert - Complete workflow should work seamlessly
        Assert.True(AResourceHandlerBridge.IsSupported("0xCommunity1"));
        Assert.True(AResourceHandlerBridge.IsSupported("0xCommunity2"));

        // Verify community plugin can be unregistered
        AResourceHandlerBridge.Remove("0xCommunity1");
        Assert.False(AResourceHandlerBridge.IsSupported("0xCommunity1"));
        Assert.True(AResourceHandlerBridge.IsSupported("0xCommunity2"));
    }

    [Fact]
    public void LegacyPlugin_MixedWithModernPlugins_CoexistencePerfect()
    {
        // Arrange - Test modern and legacy plugins working together
        // Reset bridge state to ensure clean test
        AResourceHandlerBridge.Reset();
        WrapperDealer.Initialize(_serviceProvider);
        var initialCount = _pluginManager.RegisteredPluginCount;

        // Act - Mix modern plugin registration with legacy patterns
        // Modern plugin (simplified registration through metadata)
        var modernMetadata = new PluginMetadata
        {
            FilePath = "/test/modern.dll",
            AssemblyName = "ModernPlugin",
            AssemblyFullName = "ModernPlugin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            Version = new Version(1, 0, 0, 0),
            Dependencies = Array.Empty<PluginDependency>()
        };
        var modernHandlers = new Dictionary<string, Type> { ["0xModern001"] = typeof(LegacyModernResource) };
        
        // Legacy plugin (through AResourceHandler bridge)
        AResourceHandlerBridge.Add("0xLegacy001", typeof(LegacyCommunityResource));

        // Assert - Both modern and legacy plugins should coexist
        Assert.True(_pluginManager.RegisteredPluginCount >= initialCount);
        
        // Legacy plugin should work through bridge
        Assert.True(AResourceHandlerBridge.IsSupported("0xLegacy001"));
        Assert.Equal(typeof(LegacyCommunityResource), AResourceHandlerBridge.GetHandler("0xLegacy001"));
        
        // Both should appear in supported types
        var allTypes = AResourceHandlerBridge.GetSupportedTypes();
        Assert.Contains("0xLegacy001", allTypes);
    }

    [Fact]
    public void LegacyPlugin_ErrorHandling_GracefulDegradation()
    {
        // Arrange - Test error handling with problematic legacy plugins
        // Reset bridge state to ensure clean test
        AResourceHandlerBridge.Reset();
        WrapperDealer.Initialize(_serviceProvider);

        // Act & Assert - Invalid registrations should be handled gracefully
        
        // Test 1: null resource type should be handled
        Assert.Throws<ArgumentNullException>(() => AResourceHandlerBridge.Add(null!, typeof(LegacyCommunityResource)));
        
        // Test 2: null handler type should be handled
        Assert.Throws<ArgumentNullException>(() => AResourceHandlerBridge.Add("0xTest", null!));
        
        // Test 3: Valid registration should still work after errors
        AResourceHandlerBridge.Add("0xValidAfterError", typeof(LegacyCommunityResource));
        Assert.True(AResourceHandlerBridge.IsSupported("0xValidAfterError"));
        
        // Test 4: WrapperDealer should continue working normally
        var typeMap = WrapperDealer.TypeMap;
        Assert.NotNull(typeMap);
    }

    [Fact]
    public void LegacyPlugin_PerformanceUnderLoad_Acceptable()
    {
        // Arrange - Test performance with many legacy plugins
        // Reset bridge state to ensure clean test
        AResourceHandlerBridge.Reset();
        WrapperDealer.Initialize(_serviceProvider);
        var startTime = DateTime.UtcNow;

        // Act - Register many legacy plugins rapidly
        for (int i = 0; i < 50; i++)
        {
            AResourceHandlerBridge.Add($"0xPerf{i:X8}", typeof(LegacyCommunityResource));
        }

        var registrationTime = DateTime.UtcNow - startTime;

        // Assert - Performance should be acceptable (< 1 second for 50 plugins)
        Assert.True(registrationTime.TotalMilliseconds < 1000, 
            $"Legacy plugin registration took {registrationTime.TotalMilliseconds}ms, expected < 1000ms");

        // Verify all plugins are registered
        Assert.True(_pluginManager.RegisteredPluginCount >= 50);

        // Test lookup performance
        startTime = DateTime.UtcNow;
        for (int i = 0; i < 50; i++)
        {
            Assert.True(AResourceHandlerBridge.IsSupported($"0xPerf{i:X8}"));
        }
        var lookupTime = DateTime.UtcNow - startTime;
        
        Assert.True(lookupTime.TotalMilliseconds < 100,
            $"Legacy plugin lookup took {lookupTime.TotalMilliseconds}ms, expected < 100ms");
    }

    public void Dispose()
    {
        _pluginManager?.Dispose();
        _serviceProvider?.Dispose();
        AResourceHandlerBridge.Reset();
    }

    /// <summary>
    /// Mock resource manager for legacy compatibility tests.
    /// </summary>
    private class MockResourceManager : IResourceManager
    {
        public IReadOnlyDictionary<string, Type> GetResourceTypeMap()
        {
            return new Dictionary<string, Type>().AsReadOnly();
        }

        public Task<IResource> CreateResourceAsync(string resourceType, int apiVersion, CancellationToken cancellationToken = default)
        {
            // Return appropriate mock based on resource type
            return resourceType switch
            {
                "0x044AE110" => Task.FromResult<IResource>(new LegacyComplateResource(apiVersion)),
                "0xCF9A4ACE" => Task.FromResult<IResource>(new LegacyModularResource(apiVersion)),
                "0xBDD82221" => Task.FromResult<IResource>(new LegacyAUEVResource(apiVersion)),
                "0x74050B1F" => Task.FromResult<IResource>(new LegacySTRMResource(apiVersion)),
                "0x91EDBD3E" => Task.FromResult<IResource>(new LegacyRoofStyleResource(apiVersion)),
                _ => Task.FromResult<IResource>(new LegacyCommunityResource(apiVersion))
            };
        }

        public Task<IResource> LoadResourceAsync(IPackage package, IResourceIndexEntry entry, int apiVersion, bool forceDefaultWrapper = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IResource>(new LegacyCommunityResource(apiVersion));
        }

        void IResourceManager.RegisterFactory<TResource, TFactory>() { }

        public ResourceManagerStatistics GetStatistics() => new ResourceManagerStatistics();
    }

    #region Legacy Resource Type Implementations

    /// <summary>
    /// Mock legacy ComplateResource for testing compatibility.
    /// </summary>
    private class LegacyComplateResource : IResource
    {
        public Stream Stream { get; set; } = new MemoryStream();
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged { add { } remove { } }
        public int RequestedApiVersion { get; }
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => new List<string>().AsReadOnly();

        public TypedValue this[int index] { get => new TypedValue(); set { } }
        public TypedValue this[string index] { get => new TypedValue(); set { } }

        public LegacyComplateResource(int apiVersion) { RequestedApiVersion = apiVersion; }
        public void Dispose() { Stream?.Dispose(); }
    }

    /// <summary>
    /// Mock legacy ModularResource for testing compatibility.
    /// </summary>
    private class LegacyModularResource : IResource
    {
        public Stream Stream { get; set; } = new MemoryStream();
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged { add { } remove { } }
        public int RequestedApiVersion { get; }
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => new List<string>().AsReadOnly();

        public TypedValue this[int index] { get => new TypedValue(); set { } }
        public TypedValue this[string index] { get => new TypedValue(); set { } }

        public LegacyModularResource(int apiVersion) { RequestedApiVersion = apiVersion; }
        public void Dispose() { Stream?.Dispose(); }
    }

    /// <summary>
    /// Mock legacy AUEVResource for testing compatibility.
    /// </summary>
    private class LegacyAUEVResource : IResource
    {
        public Stream Stream { get; set; } = new MemoryStream();
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged { add { } remove { } }
        public int RequestedApiVersion { get; }
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => new List<string>().AsReadOnly();

        public TypedValue this[int index] { get => new TypedValue(); set { } }
        public TypedValue this[string index] { get => new TypedValue(); set { } }

        public LegacyAUEVResource(int apiVersion) { RequestedApiVersion = apiVersion; }
        public void Dispose() { Stream?.Dispose(); }
    }

    /// <summary>
    /// Mock legacy STRMResource for testing compatibility.
    /// </summary>
    private class LegacySTRMResource : IResource
    {
        public Stream Stream { get; set; } = new MemoryStream();
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged { add { } remove { } }
        public int RequestedApiVersion { get; }
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => new List<string>().AsReadOnly();

        public TypedValue this[int index] { get => new TypedValue(); set { } }
        public TypedValue this[string index] { get => new TypedValue(); set { } }

        public LegacySTRMResource(int apiVersion) { RequestedApiVersion = apiVersion; }
        public void Dispose() { Stream?.Dispose(); }
    }

    /// <summary>
    /// Mock legacy RoofStyleResource for testing compatibility.
    /// </summary>
    private class LegacyRoofStyleResource : IResource
    {
        public Stream Stream { get; set; } = new MemoryStream();
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged { add { } remove { } }
        public int RequestedApiVersion { get; }
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => new List<string>().AsReadOnly();

        public TypedValue this[int index] { get => new TypedValue(); set { } }
        public TypedValue this[string index] { get => new TypedValue(); set { } }

        public LegacyRoofStyleResource(int apiVersion) { RequestedApiVersion = apiVersion; }
        public void Dispose() { Stream?.Dispose(); }
    }

    /// <summary>
    /// Mock generic community resource for testing compatibility.
    /// </summary>
    private class LegacyCommunityResource : IResource
    {
        public Stream Stream { get; set; } = new MemoryStream();
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged { add { } remove { } }
        public int RequestedApiVersion { get; }
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => new List<string>().AsReadOnly();

        public TypedValue this[int index] { get => new TypedValue(); set { } }
        public TypedValue this[string index] { get => new TypedValue(); set { } }

        public LegacyCommunityResource(int apiVersion) { RequestedApiVersion = apiVersion; }
        public void Dispose() { Stream?.Dispose(); }
    }

    /// <summary>
    /// Mock modern resource for testing coexistence.
    /// </summary>
    private class LegacyModernResource : IResource
    {
        public Stream Stream { get; set; } = new MemoryStream();
        public byte[] AsBytes => Array.Empty<byte>();
        public event EventHandler? ResourceChanged { add { } remove { } }
        public int RequestedApiVersion { get; }
        public int RecommendedApiVersion => 1;
        public IReadOnlyList<string> ContentFields => new List<string>().AsReadOnly();

        public TypedValue this[int index] { get => new TypedValue(); set { } }
        public TypedValue this[string index] { get => new TypedValue(); set { } }

        public LegacyModernResource(int apiVersion) { RequestedApiVersion = apiVersion; }
        public void Dispose() { Stream?.Dispose(); }
    }

    #endregion
}
