using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Resources.Scripts.Tests;

/// <summary>
/// Unit tests for ScriptResourceFactory functionality.
/// </summary>
public sealed class ScriptResourceFactoryTests
{
    private readonly ILogger<ScriptResourceFactory> _factoryLogger;
    private readonly ILogger<ScriptResource> _scriptLogger;
    private readonly ScriptResourceFactory _factory;

    public ScriptResourceFactoryTests()
    {
        _factoryLogger = Substitute.For<ILogger<ScriptResourceFactory>>();
        _scriptLogger = Substitute.For<ILogger<ScriptResource>>();
        _factory = new ScriptResourceFactory(_factoryLogger, _scriptLogger);
    }

    [Fact]
    public void Constructor_WithNullFactoryLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ScriptResourceFactory(null!, _scriptLogger));
    }

    [Fact]
    public void Constructor_WithNullScriptLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ScriptResourceFactory(_factoryLogger, null!));
    }

    [Fact]
    public void Priority_ShouldReturnExpectedValue()
    {
        // Act
        var priority = _factory.Priority;

        // Assert
        priority.Should().Be(100);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainScriptType()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().Contain($"0x{ScriptResourceFactory.ScriptResourceType:X8}");
        supportedTypes.Should().HaveCount(1);
    }

    [Fact]
    public void CanCreateResource_WithSupportedType_ShouldReturnTrue()
    {
        // Act
        var canCreate = _factory.CanCreateResource(ScriptResourceFactory.ScriptResourceType);

        // Assert
        canCreate.Should().BeTrue();
    }

    [Fact]
    public void CanCreateResource_WithUnsupportedType_ShouldReturnFalse()
    {
        // Act
        var canCreate = _factory.CanCreateResource(0x12345678);

        // Assert
        canCreate.Should().BeFalse();
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ShouldCreateEmptyResource()
    {
        // Act
        var resource = await _factory.CreateResourceAsync(1, null!);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            // Factory handles null streams by creating empty resources
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
            resource.AssemblyData.Length.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateResourceAsync_WithEmptyStream_ShouldReturnEmptyResource()
    {
        // Arrange
        using var emptyStream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(1, emptyStream);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
            resource.Version.Should().Be(1);
            resource.AssemblyData.Length.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidData_ShouldReturnValidResource()
    {
        // Arrange
        var testData = CreateTestScriptData();
        using var dataStream = new MemoryStream(testData);

        // Act
        var resource = await _factory.CreateResourceAsync(1, dataStream);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
            resource.Version.Should().Be(1);
        }
    }

    [Fact]
    public void CreateResource_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _factory.Invoking(f => f.CreateResource(null!, ScriptResourceFactory.ScriptResourceType))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public void CreateResource_WithEmptyStream_ShouldReturnEmptyResource()
    {
        // Arrange
        using var emptyStream = new MemoryStream();

        // Act
        var resource = _factory.CreateResource(emptyStream, ScriptResourceFactory.ScriptResourceType);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
            resource.Version.Should().Be(1);
            resource.AssemblyData.Length.Should().Be(0);
        }
    }

    [Fact]
    public void CreateResource_WithValidData_ShouldReturnValidResource()
    {
        // Arrange
        var testData = CreateTestScriptData();
        using var dataStream = new MemoryStream(testData);

        // Act
        var resource = _factory.CreateResource(dataStream, ScriptResourceFactory.ScriptResourceType);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
            resource.Version.Should().Be(1);
        }
    }

    [Fact]
    public async Task CreateResourceAsync_WithCorruptedData_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var corruptedData = new byte[] { 0xFF, 0xFF }; // Minimal corrupted data
        using var dataStream = new MemoryStream(corruptedData);

        // Act & Assert
        // Factory wraps parsing exceptions in InvalidOperationException
        await _factory.Invoking(f => f.CreateResourceAsync(1, dataStream))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to create script resource*");
    }

    [Fact]
    public void CreateResource_WithCorruptedData_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var corruptedData = new byte[] { 0xFF, 0xFF }; // Minimal corrupted data
        using var dataStream = new MemoryStream(corruptedData);

        // Act & Assert
        // Factory wraps parsing exceptions in InvalidOperationException
        _factory.Invoking(f => f.CreateResource(dataStream, ScriptResourceFactory.ScriptResourceType))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create script resource*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithLargeData_ShouldHandleEfficiently()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1MB of data
        Array.Fill<byte>(largeData, 0x42);
        largeData[0] = 1; // Valid version
        using var dataStream = new MemoryStream(largeData);

        // Act
        var resource = await _factory.CreateResourceAsync(1, dataStream);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
        }
    }

    [Fact]
    public async Task CreateResourceAsync_WithCancellation_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testData = CreateTestScriptData();
        using var dataStream = new MemoryStream(testData);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        // Factory wraps TaskCanceledException in InvalidOperationException
        var exception = await _factory.Invoking(f => f.CreateResourceAsync(1, dataStream, cts.Token))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to create script resource*");

        exception.And.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    /// <summary>
    /// Creates test script resource data for testing.
    /// </summary>
    private static byte[] CreateTestScriptData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write version
        writer.Write((byte)1);

        // Write unknown2
        writer.Write(0x2BC4F79Fu);

        // Write MD5 sum (64 bytes)
        writer.Write(new byte[64]);

        // Write entry count (0 entries)
        writer.Write((ushort)0);

        return stream.ToArray();
    }
}
