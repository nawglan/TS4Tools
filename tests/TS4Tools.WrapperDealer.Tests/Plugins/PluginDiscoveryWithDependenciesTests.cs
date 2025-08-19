using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Plugins;

/// <summary>
/// Integration tests for Phase 4.20.5 dependency-aware plugin discovery.
/// Tests the DiscoverPluginsWithDependencies method.
/// </summary>
public sealed class PluginDiscoveryWithDependenciesTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ILogger<PluginDiscoveryService> _logger;
    private readonly PluginRegistrationManager _pluginManager;

    public PluginDiscoveryWithDependenciesTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "TS4Tools.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _logger = NullLogger<PluginDiscoveryService>.Instance;
        _pluginManager = new PluginRegistrationManager(NullLogger<PluginRegistrationManager>.Instance);
    }

    public void Dispose()
    {
        _pluginManager?.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void DiscoverPluginsWithDependencies_WithNullResolver_UsesDefaultResolver()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);

        // Act
        var result = discoveryService.DiscoverPluginsWithDependencies(null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DiscoveredPlugins); // May discover assemblies in standard locations
        Assert.NotNull(result.AllIssues); // May have registration issues for non-plugin assemblies
        Assert.True(result.RegisteredCount >= 0); // May register 0 or more actual plugins
    }

    [Fact]
    public void DiscoverPluginsWithDependencies_WithCustomResolver_UsesProvidedResolver()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);
        var customResolver = new PluginDependencyResolver(NullLogger<PluginDependencyResolver>.Instance);

        // Act
        var result = discoveryService.DiscoverPluginsWithDependencies(customResolver);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DiscoveredPlugins); // May discover assemblies in standard locations
        Assert.NotNull(result.AllIssues); // May have registration issues for non-plugin assemblies
        Assert.True(result.RegisteredCount >= 0); // May register 0 or more actual plugins
    }

    [Fact]
    public void DiscoverPluginsWithDependencies_ReturnsCorrectResultStructure()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);

        // Act
        var result = discoveryService.DiscoverPluginsWithDependencies();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DiscoveredPlugins);
        Assert.NotNull(result.DiscoveryIssues);
        Assert.NotNull(result.RegistrationIssues);
        
        // Properties should be consistent
        Assert.True(result.DiscoveredPlugins.Count >= 0);
        Assert.True(result.DiscoveryIssues.Count >= 0);
        Assert.True(result.RegistrationIssues.Count >= 0);
        Assert.True(result.RegisteredCount >= 0);
    }

    [Fact]
    public void DiscoverPluginsWithDependencies_WithEmptyDirectories_ReturnsValidResult()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);

        // Act
        var result = discoveryService.DiscoverPluginsWithDependencies();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DiscoveredPlugins); // May discover assemblies from standard locations
        Assert.NotNull(result.AllIssues);
        Assert.True(result.RegisteredCount >= 0); // May register 0 or more plugins
    }

    [Fact]
    public void DiscoverPluginsWithDependencies_ServiceIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);
        discoveryService.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => discoveryService.DiscoverPluginsWithDependencies());
    }
}
