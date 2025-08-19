using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Plugins;

/// <summary>
/// Tests for Phase 4.20.5 plugin attributes and metadata functionality.
/// Validates enhanced plugin metadata extraction and dependency management.
/// </summary>
public sealed class PluginAttributesTests
{
    [Fact]
    public void PluginInfoAttribute_WithBasicInfo_InitializesCorrectly()
    {
        // Arrange & Act
        var attribute = new PluginInfoAttribute("Test Plugin", "1.0.0")
        {
            Description = "A test plugin",
            Author = "Test Author",
            Website = "https://example.com",
            License = "MIT",
            MinimumTS4ToolsVersion = "1.0.0",
            MaximumTS4ToolsVersion = "2.0.0",
            SupportedResourceTypes = "0x12345678,0x87654321",
            Tags = "test,example,demo",
            IsStable = true,
            IsExperimental = false
        };

        // Assert
        Assert.Equal("Test Plugin", attribute.Name);
        Assert.Equal("1.0.0", attribute.Version);
        Assert.Equal("A test plugin", attribute.Description);
        Assert.Equal("Test Author", attribute.Author);
        Assert.Equal("https://example.com", attribute.Website);
        Assert.Equal("MIT", attribute.License);
        Assert.Equal("1.0.0", attribute.MinimumTS4ToolsVersion);
        Assert.Equal("2.0.0", attribute.MaximumTS4ToolsVersion);
        Assert.True(attribute.IsStable);
        Assert.False(attribute.IsExperimental);
    }

    [Fact]
    public void PluginInfoAttribute_GetParsedVersion_ParsesVersionCorrectly()
    {
        // Arrange
        var attribute = new PluginInfoAttribute("Test Plugin", "1.2.3.4");

        // Act
        var version = attribute.GetParsedVersion();

        // Assert
        Assert.NotNull(version);
        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Build);
        Assert.Equal(4, version.Revision);
    }

    [Fact]
    public void PluginInfoAttribute_GetParsedVersion_InvalidVersion_ReturnsNull()
    {
        // Arrange
        var attribute = new PluginInfoAttribute("Test Plugin", "invalid-version");

        // Act
        var version = attribute.GetParsedVersion();

        // Assert
        Assert.Null(version);
    }

    [Fact]
    public void PluginInfoAttribute_GetSupportedResourceTypes_ParsesCommaSeparatedList()
    {
        // Arrange
        var attribute = new PluginInfoAttribute("Test Plugin", "1.0.0")
        {
            SupportedResourceTypes = "0x12345678, 0x87654321 , 0xABCDEF01"
        };

        // Act
        var resourceTypes = attribute.GetSupportedResourceTypes();

        // Assert
        Assert.Equal(3, resourceTypes.Count);
        Assert.Contains("0x12345678", resourceTypes);
        Assert.Contains("0x87654321", resourceTypes);
        Assert.Contains("0xABCDEF01", resourceTypes);
    }

    [Fact]
    public void PluginInfoAttribute_GetSupportedResourceTypes_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var attribute = new PluginInfoAttribute("Test Plugin", "1.0.0")
        {
            SupportedResourceTypes = ""
        };

        // Act
        var resourceTypes = attribute.GetSupportedResourceTypes();

        // Assert
        Assert.Empty(resourceTypes);
    }

    [Fact]
    public void PluginInfoAttribute_GetTags_ParsesCommaSeparatedList()
    {
        // Arrange
        var attribute = new PluginInfoAttribute("Test Plugin", "1.0.0")
        {
            Tags = "Test, Example , Demo"
        };

        // Act
        var tags = attribute.GetTags();

        // Assert
        Assert.Equal(3, tags.Count);
        Assert.Contains("test", tags);
        Assert.Contains("example", tags);
        Assert.Contains("demo", tags);
    }

    [Fact]
    public void PluginDependencyAttribute_WithBasicDependency_InitializesCorrectly()
    {
        // Arrange & Act
        var attribute = new PluginDependencyAttribute("Required Plugin", "1.5.0")
        {
            MaximumVersion = "2.0.0",
            IsOptional = false
        };

        // Assert
        Assert.Equal("Required Plugin", attribute.PluginName);
        Assert.Equal("1.5.0", attribute.MinimumVersion);
        Assert.Equal("2.0.0", attribute.MaximumVersion);
        Assert.False(attribute.IsOptional);
    }

    [Fact]
    public void PluginDependencyAttribute_IsSatisfiedBy_VersionInRange_ReturnsTrue()
    {
        // Arrange
        var attribute = new PluginDependencyAttribute("Test Plugin", "1.0.0")
        {
            MaximumVersion = "2.0.0"
        };

        // Act & Assert
        Assert.True(attribute.IsSatisfiedBy(new Version(1, 5, 0)));
        Assert.True(attribute.IsSatisfiedBy(new Version(1, 0, 0)));
        Assert.True(attribute.IsSatisfiedBy(new Version(2, 0, 0)));
    }

    [Fact]
    public void PluginDependencyAttribute_IsSatisfiedBy_VersionOutOfRange_ReturnsFalse()
    {
        // Arrange
        var attribute = new PluginDependencyAttribute("Test Plugin", "1.0.0")
        {
            MaximumVersion = "2.0.0"
        };

        // Act & Assert
        Assert.False(attribute.IsSatisfiedBy(new Version(0, 9, 0)));
        Assert.False(attribute.IsSatisfiedBy(new Version(2, 1, 0)));
    }

    [Fact]
    public void PluginDependencyAttribute_IsSatisfiedBy_OnlyMinimumVersion_ChecksMinimum()
    {
        // Arrange
        var attribute = new PluginDependencyAttribute("Test Plugin", "1.0.0");

        // Act & Assert
        Assert.True(attribute.IsSatisfiedBy(new Version(1, 0, 0)));
        Assert.True(attribute.IsSatisfiedBy(new Version(2, 0, 0)));
        Assert.False(attribute.IsSatisfiedBy(new Version(0, 9, 0)));
    }

    [Fact]
    public void PluginResourceCompatibilityAttribute_WithBasicCompatibility_InitializesCorrectly()
    {
        // Arrange & Act
        var attribute = new PluginResourceCompatibilityAttribute("0x12345678")
        {
            SupportedVersionRange = "1.0-2.0",
            ReplacesDefaultHandler = true,
            Priority = 10
        };

        // Assert
        Assert.Equal("0x12345678", attribute.ResourceType);
        Assert.Equal("1.0-2.0", attribute.SupportedVersionRange);
        Assert.True(attribute.ReplacesDefaultHandler);
        Assert.Equal(10, attribute.Priority);
    }

    [Fact]
    public void PluginInfo_FromAttribute_ExtractsAllProperties()
    {
        // Arrange
        var attribute = new PluginInfoAttribute("Test Plugin", "1.0.0")
        {
            Description = "A test plugin",
            Author = "Test Author",
            Website = "https://example.com",
            License = "MIT",
            SupportedResourceTypes = "0x12345678,0x87654321",
            Tags = "test,example",
            IsStable = true,
            IsExperimental = false
        };

        // Act
        var pluginInfo = PluginInfo.FromAttribute(attribute);

        // Assert
        Assert.Equal("Test Plugin", pluginInfo.Name);
        Assert.Equal("1.0.0", pluginInfo.Version);
        Assert.Equal("A test plugin", pluginInfo.Description);
        Assert.Equal("Test Author", pluginInfo.Author);
        Assert.Equal("https://example.com", pluginInfo.Website);
        Assert.Equal("MIT", pluginInfo.License);
        Assert.Equal(2, pluginInfo.SupportedResourceTypes.Count);
        Assert.Equal(2, pluginInfo.Tags.Count);
        Assert.True(pluginInfo.IsStable);
        Assert.False(pluginInfo.IsExperimental);
    }

    [Fact]
    public void PluginDependency_FromAttribute_ExtractsAllProperties()
    {
        // Arrange
        var attribute = new PluginDependencyAttribute("Required Plugin", "1.0.0")
        {
            MaximumVersion = "2.0.0",
            IsOptional = true
        };

        // Act
        var dependency = PluginDependency.FromAttribute(attribute);

        // Assert
        Assert.Equal("Required Plugin", dependency.PluginName);
        Assert.Equal("1.0.0", dependency.MinimumVersion);
        Assert.Equal("2.0.0", dependency.MaximumVersion);
        Assert.True(dependency.IsOptional);
    }

    [Fact]
    public void PluginDependency_IsSatisfiedBy_ValidatesVersions()
    {
        // Arrange
        var dependency = new PluginDependency
        {
            PluginName = "Test Plugin",
            MinimumVersion = "1.0.0",
            MaximumVersion = "2.0.0",
            IsOptional = false
        };

        // Act & Assert
        Assert.True(dependency.IsSatisfiedBy(new Version(1, 5, 0)));
        Assert.False(dependency.IsSatisfiedBy(new Version(0, 9, 0)));
        Assert.False(dependency.IsSatisfiedBy(new Version(2, 1, 0)));
    }

    [Fact]
    public void PluginResourceCompatibility_FromAttribute_ExtractsAllProperties()
    {
        // Arrange
        var attribute = new PluginResourceCompatibilityAttribute("0x12345678")
        {
            SupportedVersionRange = "1.0-2.0",
            ReplacesDefaultHandler = true,
            Priority = 5
        };

        // Act
        var compatibility = PluginResourceCompatibility.FromAttribute(attribute);

        // Assert
        Assert.Equal("0x12345678", compatibility.ResourceType);
        Assert.Equal("1.0-2.0", compatibility.SupportedVersionRange);
        Assert.True(compatibility.ReplacesDefaultHandler);
        Assert.Equal(5, compatibility.Priority);
    }
}
