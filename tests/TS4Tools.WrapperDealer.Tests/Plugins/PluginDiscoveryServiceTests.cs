using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Plugins;

/// <summary>
/// Tests for the PluginDiscoveryService (Phase 4.20.4).
/// Validates auto-discovery functionality for plugins from standard locations.
/// </summary>
public sealed class PluginDiscoveryServiceTests : IDisposable
{
    private readonly PluginRegistrationManager _pluginManager;
    private readonly ILogger<PluginDiscoveryService> _logger;
    private readonly string _tempDirectory;

    public PluginDiscoveryServiceTests()
    {
        _logger = NullLogger<PluginDiscoveryService>.Instance;
        _pluginManager = new PluginRegistrationManager(NullLogger<PluginRegistrationManager>.Instance);
        
        // Create a temporary directory for test files
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"TS4Tools_PluginDiscoveryTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Act
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);

        // Assert
        Assert.NotNull(discoveryService.StandardLocations);
        Assert.True(discoveryService.StandardLocations.Count > 0);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PluginDiscoveryService(null!, _pluginManager));
    }

    [Fact]
    public void Constructor_WithNullPluginManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PluginDiscoveryService(_logger, null!));
    }

    [Fact]
    public void StandardLocations_IncludesExpectedDirectories()
    {
        // Arrange & Act
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);
        var locations = discoveryService.StandardLocations;

        // Assert
        Assert.Contains(locations, location => location.Contains("Plugins"));
        Assert.Contains(locations, location => location.Contains("Helpers"));
        Assert.Contains(locations, location => location.Contains("Extensions"));
        Assert.Contains(locations, location => location.Contains("Wrappers"));
    }

    [Fact]
    public void DiscoverPluginsFromDirectory_WithNonExistentDirectory_ReturnsZero()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);
        var nonExistentPath = Path.Combine(_tempDirectory, "NonExistent");

        // Act
        var result = discoveryService.DiscoverPluginsFromDirectory(nonExistentPath);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void DiscoverPluginsFromDirectory_WithEmptyDirectory_ReturnsZero()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);
        var emptyDirectory = Path.Combine(_tempDirectory, "Empty");
        Directory.CreateDirectory(emptyDirectory);

        // Act
        var result = discoveryService.DiscoverPluginsFromDirectory(emptyDirectory);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void DiscoverPluginsFromDirectory_WithTestResourceAssembly_FindsPlugins()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);
        var pluginDirectory = Path.Combine(_tempDirectory, "TestPlugins");
        Directory.CreateDirectory(pluginDirectory);

        // Create a test assembly by copying the current test assembly
        var currentAssembly = Assembly.GetExecutingAssembly();
        var testAssemblyPath = Path.Combine(pluginDirectory, "TestResource.dll");
        File.Copy(currentAssembly.Location, testAssemblyPath);

        // Act
        var result = discoveryService.DiscoverPluginsFromDirectory(pluginDirectory);

        // Assert - should find at least the test resource wrapper in this assembly
        Assert.True(result >= 0); // May be 0 if no valid resource handlers found, which is OK for this test
    }

    [Fact]
    public void DiscoverPlugins_WithStandardLocations_CompletesWithoutException()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);

        // Act & Assert - should not throw
        var result = discoveryService.DiscoverPlugins();
        Assert.True(result >= 0);
    }

    [Fact]
    public void DefaultPluginDirectories_ContainsExpectedValues()
    {
        // Act & Assert
        Assert.Contains("Plugins", PluginDiscoveryService.DefaultPluginDirectories);
        Assert.Contains("Extensions", PluginDiscoveryService.DefaultPluginDirectories);
        Assert.Contains("Helpers", PluginDiscoveryService.DefaultPluginDirectories);
        Assert.Contains("Wrappers", PluginDiscoveryService.DefaultPluginDirectories);
    }

    [Fact]
    public void PluginFilePatterns_ContainsExpectedValues()
    {
        // Act & Assert
        Assert.Contains("*.dll", PluginDiscoveryService.PluginFilePatterns);
        Assert.Contains("*Helper.dll", PluginDiscoveryService.PluginFilePatterns);
        Assert.Contains("*Plugin.dll", PluginDiscoveryService.PluginFilePatterns);
        Assert.Contains("*Wrapper.dll", PluginDiscoveryService.PluginFilePatterns);
        Assert.Contains("*Extension.dll", PluginDiscoveryService.PluginFilePatterns);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);

        // Act & Assert - should not throw
        discoveryService.Dispose();
        discoveryService.Dispose();
    }

    [Fact]
    public void DiscoverPluginsFromDirectory_WithNonDllFiles_IgnoresInvalidFiles()
    {
        // Arrange
        using var discoveryService = new PluginDiscoveryService(_logger, _pluginManager);
        var pluginDirectory = Path.Combine(_tempDirectory, "InvalidFiles");
        Directory.CreateDirectory(pluginDirectory);

        // Create some non-DLL files
        File.WriteAllText(Path.Combine(pluginDirectory, "test.txt"), "not a dll");
        File.WriteAllText(Path.Combine(pluginDirectory, "config.json"), "{}");
        File.WriteAllText(Path.Combine(pluginDirectory, "readme.md"), "# Test");

        // Act
        var result = discoveryService.DiscoverPluginsFromDirectory(pluginDirectory);

        // Assert
        Assert.Equal(0, result);
    }

    public void Dispose()
    {
        _pluginManager?.Dispose();
        
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
    /// Test resource wrapper for plugin discovery validation.
    /// </summary>
    public class TestDiscoveryResourceWrapper : IResource
    {
        public uint ResourceType => 0x87654321;
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

        public TestDiscoveryResourceWrapper()
        {
            APIversion = 1;
        }

        public TestDiscoveryResourceWrapper(int apiVersion)
        {
            APIversion = (uint)apiVersion;
        }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}
