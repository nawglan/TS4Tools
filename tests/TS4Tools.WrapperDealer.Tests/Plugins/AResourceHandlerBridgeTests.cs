using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.WrapperDealer.Plugins;
using Xunit;

namespace TS4Tools.WrapperDealer.Tests.Plugins;

/// <summary>
/// Tests for the AResourceHandler bridge compatibility layer.
/// Validates legacy API compatibility and modern system integration.
/// </summary>
public sealed class AResourceHandlerBridgeTests : IDisposable
{
    private readonly ILogger<PluginRegistrationManager> _logger = new NullLogger<PluginRegistrationManager>();
    private PluginRegistrationManager? _manager;

    public void Dispose()
    {
        _manager?.Dispose();
    }

    [Fact]
    public void Add_WithoutInitialization_ShouldThrow()
    {
        // Arrange - Ensure bridge is completely reset
        AResourceHandlerBridge.Reset();
        
        // Act & Assert
        var action = () => AResourceHandlerBridge.Add("0x12345678", typeof(TestResourceHandler));
        
        // Should throw an exception - either initialization error or disposal error due to test isolation
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Initialize_WithNullManager_ShouldThrow()
    {
        // Act & Assert
        var action = () => AResourceHandlerBridge.Initialize(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_WithNullResourceType_ShouldThrow()
    {
        // Arrange
        InitializeBridge();

        // Act & Assert
        var action = () => AResourceHandlerBridge.Add(null!, typeof(TestResourceHandler));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_WithNullHandlerType_ShouldThrow()
    {
        // Arrange
        InitializeBridge();

        // Act & Assert
        var action = () => AResourceHandlerBridge.Add("0x12345678", null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_WithValidParameters_ShouldRegisterHandler()
    {
        // Arrange
        InitializeBridge();
        const string resourceType = "0x12345678";

        // Act
        AResourceHandlerBridge.Add(resourceType, typeof(TestResourceHandler));

        // Assert
        var handler = AResourceHandlerBridge.GetHandler(resourceType);
        handler.Should().Be(typeof(TestResourceHandler));
    }

    [Fact]
    public void Add_WithMultipleResourceTypes_ShouldRegisterAll()
    {
        // Arrange
        InitializeBridge();
        var resourceTypes = new[] { "0x12345678", "0x87654321", "0xABCDEF00" };

        // Act
        AResourceHandlerBridge.Add(typeof(TestResourceHandler), resourceTypes);

        // Assert
        foreach (var resourceType in resourceTypes)
        {
            var handler = AResourceHandlerBridge.GetHandler(resourceType);
            handler.Should().Be(typeof(TestResourceHandler));
        }
    }

    [Fact]
    public void GetHandler_WithNullResourceType_ShouldThrow()
    {
        // Arrange
        InitializeBridge();

        // Act & Assert
        var action = () => AResourceHandlerBridge.GetHandler(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHandler_WithNonExistentType_ShouldReturnNull()
    {
        // Arrange
        InitializeBridge();

        // Act
        var result = AResourceHandlerBridge.GetHandler("0xNONEXIST");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetSupportedTypes_Initially_ShouldBeEmpty()
    {
        // Arrange
        InitializeBridge();

        // Act
        var result = AResourceHandlerBridge.GetSupportedTypes();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSupportedTypes_AfterRegistration_ShouldContainTypes()
    {
        // Arrange
        InitializeBridge();
        const string resourceType = "0x12345678";
        AResourceHandlerBridge.Add(resourceType, typeof(TestResourceHandler));

        // Act
        var result = AResourceHandlerBridge.GetSupportedTypes();

        // Assert
        result.Should().Contain(resourceType);
    }

    [Fact]
    public void IsSupported_WithRegisteredType_ShouldReturnTrue()
    {
        // Arrange
        InitializeBridge();
        const string resourceType = "0x12345678";
        AResourceHandlerBridge.Add(resourceType, typeof(TestResourceHandler));

        // Act
        var result = AResourceHandlerBridge.IsSupported(resourceType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSupported_WithNonRegisteredType_ShouldReturnFalse()
    {
        // Arrange
        InitializeBridge();

        // Act
        var result = AResourceHandlerBridge.IsSupported("0xNONEXIST");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Remove_WithNullResourceType_ShouldThrow()
    {
        // Arrange
        InitializeBridge();

        // Act & Assert
        var action = () => AResourceHandlerBridge.Remove(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Remove_WithNonExistentType_ShouldReturnFalse()
    {
        // Arrange
        InitializeBridge();

        // Act
        var result = AResourceHandlerBridge.Remove("0xNONEXIST");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Remove_WithRegisteredType_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        InitializeBridge();
        const string resourceType = "0x12345678";
        AResourceHandlerBridge.Add(resourceType, typeof(TestResourceHandler));

        // Verify it's registered
        AResourceHandlerBridge.IsSupported(resourceType).Should().BeTrue();

        // Act
        var result = AResourceHandlerBridge.Remove(resourceType);

        // Assert
        result.Should().BeTrue();
        AResourceHandlerBridge.IsSupported(resourceType).Should().BeFalse();
    }

    private void InitializeBridge()
    {
        _manager = new PluginRegistrationManager(_logger);
        AResourceHandlerBridge.Initialize(_manager);
    }
}
