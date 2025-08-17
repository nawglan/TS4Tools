using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Resources.Scripts.Tests;

/// <summary>
/// Fixed unit tests for ScriptResourceFactory functionality.
/// </summary>
public sealed class ScriptResourceFactoryTests_Fixed
{
    private readonly ILogger<ScriptResourceFactory> _logger;
    private readonly ILogger<ScriptResource> _scriptLogger;
    private readonly ScriptResourceFactory _factory;

    public ScriptResourceFactoryTests_Fixed()
    {
        _logger = Substitute.For<ILogger<ScriptResourceFactory>>();
        _scriptLogger = Substitute.For<ILogger<ScriptResource>>();
        _factory = new ScriptResourceFactory(_logger, _scriptLogger);
    }

    [Fact]
    public void Constructor_WithLogger_ShouldCreateFactory()
    {
        // Assert
        _factory.Should().NotBeNull();
    }

    [Fact]
    public void CanCreateResource_WithScriptResourceType_ShouldReturnTrue()
    {
        // Act
        var canCreate = _factory.CanCreateResource(ScriptResourceFactory.ScriptResourceType);

        // Assert
        canCreate.Should().BeTrue();
    }

    [Fact]
    public void CanCreateResource_WithOtherResourceType_ShouldReturnFalse()
    {
        // Act
        var canCreate = _factory.CanCreateResource(0x12345678);

        // Assert
        canCreate.Should().BeFalse();
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidId_ShouldReturnResource()
    {
        // Arrange
        var instance = 1;
        var stream = new MemoryStream(CreateTestScriptData());

        // Act
        var resource = await _factory.CreateResourceAsync(instance, stream);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            // Factory creates ResourceKey with (0, ScriptResourceType, 0) pattern
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
        }
    }

    [Fact]
    public void CreateResource_WithValidStream_ShouldReturnResource()
    {
        // Arrange
        var stream = new MemoryStream(CreateTestScriptData());
        var resourceType = ScriptResourceFactory.ScriptResourceType;

        // Act
        var resource = _factory.CreateResource(stream, resourceType);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            // Factory creates ResourceKey with (0, ScriptResourceType, 0) pattern
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
        }
    }

    [Fact]
    public void CreateEmptyResource_WithValidType_ShouldReturnResource()
    {
        // Arrange
        var resourceType = ScriptResourceFactory.ScriptResourceType;

        // Act
        var resource = _factory.CreateEmptyResource(resourceType);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            // Factory creates ResourceKey with (0, ScriptResourceType, 0) pattern
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
        }
    }

    [Fact]
    public void CreateEmptyResource_WithInvalidType_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidType = 0x12345678U;

        // Act & Assert
        // CreateEmptyResource throws ArgumentException for unsupported types
        _factory.Invoking(f => f.CreateEmptyResource(invalidType))
            .Should().Throw<ArgumentException>()
            .WithMessage("Resource type 0x12345678 is not supported*");
    }

    private static byte[] CreateTestScriptData()
    {
        // Create a minimal test script resource data
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write version
        writer.Write((byte)1);

        // Write unknown2
        writer.Write(0x2BC4F79FU);

        // Write MD5 sum (64 bytes)
        writer.Write(new byte[64]);

        // Write entry count
        writer.Write((ushort)0);

        return stream.ToArray();
    }
}
