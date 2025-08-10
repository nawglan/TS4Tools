using Microsoft.Extensions.Logging.Abstractions;

namespace TS4Tools.Resources.Effects.Tests;

public class EffectResourceFactoryTests
{
    private readonly ILogger<EffectResourceFactory> _mockLogger;
    private readonly EffectResourceFactory _factory;

    public EffectResourceFactoryTests()
    {
        _mockLogger = NullLogger<EffectResourceFactory>.Instance;
        _factory = new EffectResourceFactory(_mockLogger);
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var factory = new EffectResourceFactory(_mockLogger);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new EffectResourceFactory(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidApiVersion_ShouldCreateEffectResource()
    {
        // Arrange
        int apiVersion = 1;

        // Act
        var result = await _factory.CreateResourceAsync(apiVersion);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<EffectResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidApiVersionAndStream_ShouldCreateEffectResource()
    {
        // Arrange
        int apiVersion = 1;
        using var stream = new MemoryStream();

        // Act
        var result = await _factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<EffectResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidApiVersion_ShouldThrowArgumentException()
    {
        // Arrange
        int invalidApiVersion = 0; // Invalid: must be > 0

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(invalidApiVersion))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("apiVersion");
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().NotBeNull();
        supportedTypes.Should().Contain("RSLT");
        supportedTypes.Should().Contain("MATD");
        supportedTypes.Should().Contain("EFCT");
        supportedTypes.Should().Contain("SHAD");
        supportedTypes.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("RSLT")]
    [InlineData("MATD")]
    [InlineData("EFCT")]
    [InlineData("SHAD")]
    public void SupportedResourceTypes_ShouldContainExpectedType(string typeId)
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().Contain(typeId);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldHaveExpectedCount()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().HaveCount(4);
    }

    [Fact]
    public async Task CreateResourceAsync_ShouldCreateNewInstanceEachTime()
    {
        // Arrange
        int apiVersion = 1;

        // Act
        var result1 = await _factory.CreateResourceAsync(apiVersion);
        var result2 = await _factory.CreateResourceAsync(apiVersion);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().NotBeSameAs(result2);
    }

    [Fact]
    public void Priority_ShouldReturnExpectedValue()
    {
        // Act
        var priority = _factory.Priority;

        // Assert
        priority.Should().Be(50);
    }
}
