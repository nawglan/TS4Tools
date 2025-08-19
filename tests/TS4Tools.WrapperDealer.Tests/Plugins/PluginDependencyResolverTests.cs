using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Plugins;

/// <summary>
/// Tests for the PluginDependencyResolver (Phase 4.20.5).
/// Validates dependency resolution and plugin loading order determination.
/// </summary>
public sealed class PluginDependencyResolverTests
{
    private readonly PluginDependencyResolver _resolver;

    public PluginDependencyResolverTests()
    {
        var logger = NullLogger<PluginDependencyResolver>.Instance;
        _resolver = new PluginDependencyResolver(logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PluginDependencyResolver(null!));
    }

    [Fact]
    public void ResolveDependencies_WithNoDependencies_ReturnsAllPlugins()
    {
        // Arrange
        var plugins = new List<PluginMetadata>
        {
            CreatePluginMetadata("Plugin A", "1.0.0"),
            CreatePluginMetadata("Plugin B", "1.0.0"),
            CreatePluginMetadata("Plugin C", "1.0.0")
        };

        // Act
        var result = _resolver.ResolveDependencies(plugins);

        // Assert
        Assert.Equal(3, result.OrderedPlugins.Count);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ResolveDependencies_WithSimpleDependency_OrdersPluginsCorrectly()
    {
        // Arrange
        var pluginA = CreatePluginMetadata("Plugin A", "1.0.0");
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0", 
            dependencies: new[] { CreateDependency("Plugin A", "1.0.0") });

        var plugins = new List<PluginMetadata> { pluginB, pluginA }; // Intentionally out of order

        // Act
        var result = _resolver.ResolveDependencies(plugins);

        // Assert
        Assert.Equal(2, result.OrderedPlugins.Count);
        Assert.Empty(result.Issues);
        
        // Plugin A should come before Plugin B
        var pluginAIndex = result.OrderedPlugins.ToList().FindIndex(p => GetPluginName(p) == "Plugin A");
        var pluginBIndex = result.OrderedPlugins.ToList().FindIndex(p => GetPluginName(p) == "Plugin B");
        Assert.True(pluginAIndex < pluginBIndex);
    }

    [Fact]
    public void ResolveDependencies_WithMissingDependency_ReportsIssue()
    {
        // Arrange
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin A", "1.0.0") });

        var plugins = new List<PluginMetadata> { pluginB };

        // Act
        var result = _resolver.ResolveDependencies(plugins);

        // Assert
        Assert.Empty(result.OrderedPlugins);
        Assert.Single(result.Issues);
        Assert.Equal(DependencyIssueType.Missing, result.Issues[0].IssueType);
        Assert.Equal("Plugin B", result.Issues[0].PluginName);
        Assert.Equal("Plugin A", result.Issues[0].DependencyName);
    }

    [Fact]
    public void ResolveDependencies_WithVersionMismatch_ReportsIssue()
    {
        // Arrange
        var pluginA = CreatePluginMetadata("Plugin A", "0.9.0");
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin A", "1.0.0") });

        var plugins = new List<PluginMetadata> { pluginA, pluginB };

        // Act
        var result = _resolver.ResolveDependencies(plugins);

        // Assert
        Assert.Single(result.OrderedPlugins); // Only Plugin A should be loaded
        Assert.Single(result.Issues);
        Assert.Equal(DependencyIssueType.VersionMismatch, result.Issues[0].IssueType);
        Assert.Equal("Plugin B", result.Issues[0].PluginName);
        Assert.Equal("Plugin A", result.Issues[0].DependencyName);
    }

    [Fact]
    public void ResolveDependencies_WithOptionalDependency_LoadsBothPlugins()
    {
        // Arrange
        var pluginA = CreatePluginMetadata("Plugin A", "1.0.0");
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin C", "1.0.0", isOptional: true) });

        var plugins = new List<PluginMetadata> { pluginA, pluginB };

        // Act
        var result = _resolver.ResolveDependencies(plugins);

        // Assert
        Assert.Equal(2, result.OrderedPlugins.Count);
        Assert.Empty(result.Issues); // Optional dependencies don't create issues
    }

    [Fact]
    public void ResolveDependencies_WithCircularDependency_ReportsIssue()
    {
        // Arrange
        var pluginA = CreatePluginMetadata("Plugin A", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin B", "1.0.0") });
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin A", "1.0.0") });

        var plugins = new List<PluginMetadata> { pluginA, pluginB };

        // Act
        var result = _resolver.ResolveDependencies(plugins);

        // Assert
        Assert.Empty(result.OrderedPlugins);
        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.IssueType == DependencyIssueType.CircularDependency);
    }

    [Fact]
    public void ResolveDependencies_WithComplexDependencyChain_OrdersCorrectly()
    {
        // Arrange
        // D depends on C, C depends on B, B depends on A
        var pluginA = CreatePluginMetadata("Plugin A", "1.0.0");
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin A", "1.0.0") });
        var pluginC = CreatePluginMetadata("Plugin C", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin B", "1.0.0") });
        var pluginD = CreatePluginMetadata("Plugin D", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin C", "1.0.0") });

        // Add in random order
        var plugins = new List<PluginMetadata> { pluginD, pluginA, pluginC, pluginB };

        // Act
        var result = _resolver.ResolveDependencies(plugins);

        // Assert
        Assert.Equal(4, result.OrderedPlugins.Count);
        Assert.Empty(result.Issues);

        var orderedNames = result.OrderedPlugins.Select(GetPluginName).ToList();
        var indexA = orderedNames.IndexOf("Plugin A");
        var indexB = orderedNames.IndexOf("Plugin B");
        var indexC = orderedNames.IndexOf("Plugin C");
        var indexD = orderedNames.IndexOf("Plugin D");

        Assert.True(indexA < indexB);
        Assert.True(indexB < indexC);
        Assert.True(indexC < indexD);
    }

    [Fact]
    public void ValidatePluginDependencies_WithSatisfiedDependencies_ReturnsNoIssues()
    {
        // Arrange
        var pluginA = CreatePluginMetadata("Plugin A", "1.5.0");
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin A", "1.0.0", "2.0.0") });

        var availablePlugins = new List<PluginMetadata> { pluginA };

        // Act
        var issues = _resolver.ValidatePluginDependencies(pluginB, availablePlugins);

        // Assert
        Assert.Empty(issues);
    }

    [Fact]
    public void ValidatePluginDependencies_WithUnsatisfiedDependencies_ReturnsIssues()
    {
        // Arrange
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin A", "1.0.0") });

        var availablePlugins = new List<PluginMetadata>();

        // Act
        var issues = _resolver.ValidatePluginDependencies(pluginB, availablePlugins);

        // Assert
        Assert.Single(issues);
        Assert.Equal(DependencyIssueType.Missing, issues[0].IssueType);
    }

    [Fact]
    public void ResolveDependencies_WithMultipleVersionsOfSamePlugin_UsesHighestCompatibleVersion()
    {
        // Arrange
        var pluginA_v1 = CreatePluginMetadata("Plugin A", "1.0.0");
        var pluginA_v2 = CreatePluginMetadata("Plugin A", "2.0.0");
        var pluginB = CreatePluginMetadata("Plugin B", "1.0.0",
            dependencies: new[] { CreateDependency("Plugin A", "1.5.0", "2.5.0") });

        var plugins = new List<PluginMetadata> { pluginA_v1, pluginA_v2, pluginB };

        // Act
        var result = _resolver.ResolveDependencies(plugins);

        // Assert
        Assert.Equal(2, result.OrderedPlugins.Count);
        Assert.Empty(result.Issues);

        // Should use Plugin A v2.0.0 as it's the highest version that satisfies the dependency
        var loadedPluginA = result.OrderedPlugins.First(p => GetPluginName(p) == "Plugin A");
        Assert.Equal(new Version(2, 0, 0), loadedPluginA.Version);
    }

    private static PluginMetadata CreatePluginMetadata(
        string name,
        string version,
        PluginDependency[]? dependencies = null,
        PluginResourceCompatibility[]? resourceCompatibilities = null)
    {
        var pluginInfo = new PluginInfo
        {
            Name = name,
            Version = version,
            Description = $"Test plugin {name}",
            Author = "Test Author",
            IsStable = true,
            IsExperimental = false,
            SupportedResourceTypes = Array.Empty<string>(),
            Tags = Array.Empty<string>()
        };

        return new PluginMetadata
        {
            FilePath = $"/test/{name}.dll",
            AssemblyName = name,
            AssemblyFullName = $"{name}, Version={version}, Culture=neutral, PublicKeyToken=null",
            Version = System.Version.Parse(version),
            TargetFramework = ".NET 9.0",
            IsSigned = false,
            FileSizeBytes = 1024,
            LastModified = DateTime.UtcNow,
            IsCompatible = true,
            CompatibilityWarnings = Array.Empty<string>(),
            IncompatibilityReason = null,
            PotentialResourceHandlers = Array.Empty<string>(),
            PluginInfo = pluginInfo,
            Dependencies = dependencies ?? Array.Empty<PluginDependency>(),
            ResourceCompatibilities = resourceCompatibilities ?? Array.Empty<PluginResourceCompatibility>()
        };
    }

    private static PluginDependency CreateDependency(
        string pluginName,
        string minimumVersion,
        string? maximumVersion = null,
        bool isOptional = false)
    {
        return new PluginDependency
        {
            PluginName = pluginName,
            MinimumVersion = minimumVersion,
            MaximumVersion = maximumVersion,
            IsOptional = isOptional
        };
    }

    private static string GetPluginName(PluginMetadata plugin)
    {
        return plugin.PluginInfo?.Name ?? plugin.AssemblyName;
    }
}
