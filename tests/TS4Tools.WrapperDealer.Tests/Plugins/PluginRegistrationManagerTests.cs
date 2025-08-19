using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Interfaces;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Plugins;

/// <summary>
/// Tests for the plugin registration system.
/// Validates plugin registration, resource handler management, and legacy compatibility.
/// </summary>
public sealed class PluginRegistrationManagerTests : IDisposable
{
    private readonly ILogger<PluginRegistrationManager> _logger = new NullLogger<PluginRegistrationManager>();
    private readonly List<PluginRegistrationManager> _managers = new();

    public void Dispose()
    {
        foreach (var manager in _managers)
        {
            manager?.Dispose();
        }
        _managers.Clear();
    }

    private PluginRegistrationManager CreateManager()
    {
        var manager = new PluginRegistrationManager(_logger);
        _managers.Add(manager);
        return manager;
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act & Assert
        var action = () => new PluginRegistrationManager(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisteredPluginCount_Initially_ShouldBeZero()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        manager.RegisteredPluginCount.Should().Be(0);
        manager.RegisteredHandlerCount.Should().Be(0);
    }

    [Fact]
    public void RegisterPlugin_WithNullMetadata_ShouldThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var action = () => manager.RegisterPlugin(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterPlugin_WithIncompatiblePlugin_ShouldReturnFalse()
    {
        // Arrange
        var manager = CreateManager();
        var incompatibleMetadata = new PluginMetadata
        {
            FilePath = "/fake/path/plugin.dll",
            AssemblyName = "IncompatiblePlugin",
            AssemblyFullName = "IncompatiblePlugin, Version=1.0.0.0",
            Version = new Version(1, 0, 0, 0),
            TargetFramework = ".NETFramework,Version=v4.5",
            IsSigned = false,
            FileSizeBytes = 1024,
            LastModified = DateTime.UtcNow,
            IsCompatible = false,
            IncompatibilityReason = "Test incompatibility",
            CompatibilityWarnings = Array.Empty<string>(),
            PotentialResourceHandlers = Array.Empty<string>()
        };

        // Act
        var result = manager.RegisterPlugin(incompatibleMetadata);

        // Assert
        result.Should().BeFalse();
        manager.RegisteredPluginCount.Should().Be(0);
    }

    [Fact]
    public void RegisterPlugin_WithCompatibleTestAssembly_ShouldSucceed()
    {
        // Arrange
        var manager = CreateManager();
        var testAssemblyPath = typeof(PluginRegistrationManagerTests).Assembly.Location;
        var metadata = new PluginMetadata
        {
            FilePath = testAssemblyPath,
            AssemblyName = "TS4Tools.WrapperDealer.Tests",
            AssemblyFullName = "TS4Tools.WrapperDealer.Tests, Version=1.0.0.0",
            Version = new Version(1, 0, 0, 0),
            TargetFramework = ".NET 9.0",
            IsSigned = false,
            FileSizeBytes = new FileInfo(testAssemblyPath).Length,
            LastModified = DateTime.UtcNow,
            IsCompatible = true,
            IncompatibilityReason = null,
            CompatibilityWarnings = Array.Empty<string>(),
            PotentialResourceHandlers = new[] { nameof(TestResourceHandler) }
        };

        // Act
        var result = manager.RegisterPlugin(metadata);

        // Assert
        result.Should().BeTrue();
        manager.RegisteredPluginCount.Should().Be(1);
    }

    [Fact]
    public void UnregisterPlugin_WithNullName_ShouldThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var action = () => manager.UnregisterPlugin(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UnregisterPlugin_WithNonExistentPlugin_ShouldReturnFalse()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = manager.UnregisterPlugin("NonExistentPlugin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetResourceHandler_WithNullResourceType_ShouldThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var action = () => manager.GetResourceHandler(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetResourceHandler_WithNonExistentType_ShouldReturnNull()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = manager.GetResourceHandler("0xNONEXIST");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetSupportedResourceTypes_Initially_ShouldBeEmpty()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = manager.GetSupportedResourceTypes();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetRegisteredPlugins_Initially_ShouldBeEmpty()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var result = manager.GetRegisteredPlugins();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void RegisterPlugins_WithNullCollection_ShouldThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var action = () => manager.RegisterPlugins(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterPlugins_WithEmptyCollection_ShouldReturnZero()
    {
        // Arrange
        var manager = CreateManager();
        var emptyPlugins = Array.Empty<PluginMetadata>();

        // Act
        var result = manager.RegisterPlugins(emptyPlugins);

        // Assert
        result.Should().Be(0);
        manager.RegisteredPluginCount.Should().Be(0);
    }

    [Fact]
    public void Dispose_AfterDisposal_OperationsShouldThrow()
    {
        // Arrange
        var manager = new PluginRegistrationManager(_logger);
        var metadata = CreateTestPluginMetadata();

        // Act
        manager.Dispose();

        // Assert
        var registerAction = () => manager.RegisterPlugin(metadata);
        registerAction.Should().Throw<ObjectDisposedException>();

        var unregisterAction = () => manager.UnregisterPlugin("TestPlugin");
        unregisterAction.Should().Throw<ObjectDisposedException>();

        var getHandlerAction = () => manager.GetResourceHandler("0x12345678");
        getHandlerAction.Should().Throw<ObjectDisposedException>();

        var getSupportedAction = () => manager.GetSupportedResourceTypes();
        getSupportedAction.Should().Throw<ObjectDisposedException>();

        var getRegisteredAction = () => manager.GetRegisteredPlugins();
        getRegisteredAction.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var manager = new PluginRegistrationManager(_logger);

        // Act & Assert
        manager.Dispose();
        var action = () => manager.Dispose();
        action.Should().NotThrow();
    }

    private static PluginMetadata CreateTestPluginMetadata()
    {
        return new PluginMetadata
        {
            FilePath = "/fake/path/test.dll",
            AssemblyName = "TestPlugin",
            AssemblyFullName = "TestPlugin, Version=1.0.0.0",
            Version = new Version(1, 0, 0, 0),
            TargetFramework = ".NET 9.0",
            IsSigned = false,
            FileSizeBytes = 1024,
            LastModified = DateTime.UtcNow,
            IsCompatible = true,
            IncompatibilityReason = null,
            CompatibilityWarnings = Array.Empty<string>(),
            PotentialResourceHandlers = new[] { "TestResourceHandler" }
        };
    }
}

/// <summary>
/// Test resource handler for testing purposes.
/// </summary>
public sealed class TestResourceHandler : IResource
{
    public Stream Stream => new MemoryStream();
    public byte[] AsBytes => Array.Empty<byte>();
#pragma warning disable CS0067 // Event is never used - this is expected for a test stub
    public event EventHandler? ResourceChanged;
#pragma warning restore CS0067
    public int RequestedApiVersion => 1;
    public int RecommendedApiVersion => 1;
    public IReadOnlyList<string> ContentFields => Array.Empty<string>();

    public TypedValue this[int index]
    {
        get => new TypedValue();
        set { }
    }

    public TypedValue this[string name]
    {
        get => new TypedValue();
        set { }
    }

    public void Dispose()
    {
        // No resources to dispose for test handler
    }
}
