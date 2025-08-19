using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Plugins;

/// <summary>
/// Tests for the plugin discovery system.
/// Validates automatic plugin scanning, metadata extraction, and compatibility checking.
/// </summary>
public class PluginDiscoveryTests
{
    private readonly ILogger _logger = new NullLogger<PluginDiscoveryTests>();

    [Fact]
    public void DiscoverPlugins_WithNullDirectory_ShouldThrow()
    {
        // Act & Assert
        var action = () => PluginDiscovery.DiscoverPlugins(null!, _logger);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DiscoverPlugins_WithEmptyDirectory_ShouldThrow()
    {
        // Act & Assert
        var action = () => PluginDiscovery.DiscoverPlugins("", _logger);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DiscoverPlugins_WithNonExistentDirectory_ShouldThrow()
    {
        // Act & Assert
        var action = () => PluginDiscovery.DiscoverPlugins("/non/existent/path", _logger);
        action.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void DiscoverPlugins_WithEmptyDirectory_ShouldReturnEmptyList()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var pluginDir = Path.Combine(tempDir, $"TS4Tools_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(pluginDir);

        try
        {
            // Act
            var result = PluginDiscovery.DiscoverPlugins(pluginDir, _logger);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(pluginDir))
            {
                Directory.Delete(pluginDir, true);
            }
        }
    }

    [Fact]
    public void DiscoverPlugins_WithTestAssembly_ShouldFindAssembly()
    {
        // Arrange - Use the current test assembly as a "plugin"
        var testAssemblyPath = typeof(PluginDiscoveryTests).Assembly.Location;
        var testDir = Path.GetDirectoryName(testAssemblyPath)!;

        // Act
        var result = PluginDiscovery.DiscoverPlugins(testDir, _logger);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().Contain(path => Path.GetFileName(path).Contains("TS4Tools.WrapperDealer.Tests"));
    }

    [Fact]
    public void DiscoverPluginsWithMetadata_WithNullLogger_ShouldThrow()
    {
        // Arrange
        var tempDir = Path.GetTempPath();

        // Act & Assert
        var action = () => PluginDiscovery.DiscoverPluginsWithMetadata(tempDir, null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DiscoverPluginsWithMetadata_WithTestAssembly_ShouldReturnMetadata()
    {
        // Arrange - Use the current test assembly
        var testAssemblyPath = typeof(PluginDiscoveryTests).Assembly.Location;
        var testDir = Path.GetDirectoryName(testAssemblyPath)!;

        // Act
        var result = PluginDiscovery.DiscoverPluginsWithMetadata(testDir, _logger);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        
        var testAssemblyMetadata = result.FirstOrDefault(m => 
            m.AssemblyName.Contains("TS4Tools.WrapperDealer.Tests"));
        
        testAssemblyMetadata.Should().NotBeNull();
        testAssemblyMetadata!.FilePath.Should().Be(testAssemblyPath);
        testAssemblyMetadata.Version.Should().NotBeNull();
        testAssemblyMetadata.IsCompatible.Should().BeTrue(); // Test assembly should be compatible
    }
}
