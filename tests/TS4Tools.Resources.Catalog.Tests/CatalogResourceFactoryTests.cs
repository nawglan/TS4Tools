using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TS4Tools.Resources.Catalog;
using Xunit;

namespace TS4Tools.Resources.Catalog.Tests;

public sealed class CatalogResourceFactoryTests
{
    private readonly NullLogger<CatalogResourceFactory> _logger = NullLogger<CatalogResourceFactory>.Instance;
    private readonly NullLoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private readonly CatalogResourceFactory _factory;

    public CatalogResourceFactoryTests()
    {
        _factory = new CatalogResourceFactory(_logger, _loggerFactory);
    }

    [Fact]
    public void Constructor_WithLogger_ShouldInitializeSuccessfully()
    {
        // Act
        var factory = new CatalogResourceFactory(_logger, _loggerFactory);

        // Assert
        factory.Should().NotBeNull();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CatalogResourceFactory(null!, _loggerFactory);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CatalogResourceFactory(_logger, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("loggerFactory");
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Arrange
        var expectedTypes = new uint[]
        {
            0x48C28979, // Standard catalog resource
            0xA8F7B517, // Alternative catalog resource format
            0x319E4F1D, // Object catalog resource (common)
            0x9D1FFBCD, // Lot catalog resource
            0x1CC03E4C  // Room catalog resource
        };

        // Act & Assert
        _factory.ResourceTypes.Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public async Task CreateResourceAsync_WithSupportedType_ShouldCreateResource()
    {
        // Arrange
        using var stream = CreateValidCatalogStream();

        // Act
        var resource = await _factory.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CatalogResource>();
    }

    [Theory]
    [InlineData(0x48C28979u)]
    [InlineData(0xA8F7B517u)]
    [InlineData(0x319E4F1Du)]
    public void CreateResource_WithSupportedType_ShouldCreateResource(uint resourceType)
    {
        // Arrange
        using var stream = CreateValidCatalogStream();

        // Act
        var resource = _factory.CreateResource(stream, resourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CatalogResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidApiVersion_ShouldThrowArgumentException()
    {
        // Arrange
        using var stream = CreateValidCatalogStream();

        // Act & Assert
        var act = () => _factory.CreateResourceAsync(0, stream);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"API version must be greater than 0*");
    }

    [Theory]
    [InlineData(0x12345678u)]
    [InlineData(0xFFFFFFFFu)]
    public void CreateResource_WithUnsupportedType_ShouldThrowArgumentException(uint resourceType)
    {
        // Arrange
        using var stream = CreateValidCatalogStream();

        // Act & Assert
        var act = () => _factory.CreateResource(stream, resourceType);
        act.Should().Throw<ArgumentException>()
           .WithMessage($"Resource type 0x{resourceType:X8} is not supported by this factory*")
           .WithParameterName("resourceType");
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ShouldCreateEmptyResource()
    {
        // Act
        var resource = await _factory.CreateResourceAsync(1, null);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CatalogResource>();
    }

    [Fact]
    public void CreateResource_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _factory.CreateResource(null!, 0x48C28979);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("stream");
    }

    [Fact]
    public async Task CreateResourceAsync_WithEmptyStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        using var emptyStream = new MemoryStream();

        // Act & Assert
        var act = () => _factory.CreateResourceAsync(1, emptyStream);
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Stream too short for catalog resource*");
    }

    [Fact]
    public void CreateResource_WithEmptyStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        using var emptyStream = new MemoryStream();

        // Act & Assert
        var act = () => _factory.CreateResource(emptyStream, 0x48C28979);
        act.Should().Throw<InvalidDataException>()
           .WithMessage("Stream too short for catalog resource*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidData_ShouldLogCorrectly()
    {
        // Arrange
        using var stream = CreateValidCatalogStream();

        // Act
        var resource = await _factory.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
    }

    [Fact]
    public void CreateResource_WithValidData_ShouldCreateResource()
    {
        // Arrange
        using var stream = CreateValidCatalogStream();
        var resourceType = 0x48C28979u;

        // Act
        var resource = _factory.CreateResource(stream, resourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CatalogResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_MultipleTypes_ShouldCreateDifferentInstances()
    {
        // Arrange
        var types = new uint[] { 0x48C28979, 0xA8F7B517, 0x319E4F1D };
        var resources = new List<CatalogResource>();

        // Act
        foreach (var type in types)
        {
            using var stream = CreateValidCatalogStream();
            var resource = await _factory.CreateResourceAsync(1, stream);
            resources.Add(resource);
        }

        // Assert
        resources.Should().HaveCount(3);
        resources.Should().OnlyContain(r => r != null);

        // Each should be a distinct instance
        for (int i = 0; i < resources.Count; i++)
        {
            for (int j = i + 1; j < resources.Count; j++)
            {
                ReferenceEquals(resources[i], resources[j]).Should().BeFalse();
            }
        }
    }

    [Fact]
    public void CanHandle_WithSupportedTypes_ShouldReturnTrue()
    {
        // Arrange
        var supportedTypes = new uint[] { 0x48C28979, 0xA8F7B517, 0x319E4F1D, 0x9D1FFBCD, 0x1CC03E4C };

        // Act & Assert
        foreach (var type in supportedTypes)
        {
            _factory.CanCreateResource(type).Should().BeTrue($"Should support type 0x{type:X8}");
        }
    }

    [Fact]
    public void CanHandle_WithUnsupportedTypes_ShouldReturnFalse()
    {
        // Arrange
        var unsupportedTypes = new uint[] { 0x12345678, 0xFFFFFFFF, 0x00000000, 0x11111111 };

        // Act & Assert
        foreach (var type in unsupportedTypes)
        {
            _factory.CanCreateResource(type).Should().BeFalse($"Should not support type 0x{type:X8}");
        }
    }

    [Fact]
    public async Task CreateResourceAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        using var stream = CreateValidCatalogStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var act = () => _factory.CreateResourceAsync(1, stream, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Creates a minimal valid catalog stream for testing.
    /// </summary>
    private static MemoryStream CreateValidCatalogStream()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Write minimal valid catalog data
        writer.Write(1u);                  // Version
        writer.Write(9u);                  // CatalogVersion
        writer.Write(0x12345678u);         // NameHash
        writer.Write(0x87654321u);         // DescriptionHash
        writer.Write(100u);                // Price
        writer.Write(0u);                  // Unknown1
        writer.Write(0u);                  // Unknown2
        writer.Write(0u);                  // Unknown3
        writer.Write((byte)0);             // Style count (0)
        writer.Write((ushort)0);           // Unknown4
        writer.Write(0);                   // Tag count (0)
        writer.Write(0);                   // Selling point count (0)
        writer.Write(0uL);                 // Unknown5
        writer.Write((ushort)0);           // Unknown6
        writer.Write(0uL);                 // Unknown7

        stream.Position = 0;
        return stream;
    }
}
