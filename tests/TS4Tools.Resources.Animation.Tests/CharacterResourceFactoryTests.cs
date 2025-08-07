using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public sealed class CharacterResourceFactoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly CharacterResourceFactory _factory;

    public CharacterResourceFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddAnimationResources();
        _serviceProvider = services.BuildServiceProvider();
        _factory = _serviceProvider.GetRequiredService<CharacterResourceFactory>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeCorrectly()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<CharacterResourceFactory>>();

        // Act
        var factory = new CharacterResourceFactory(logger);

        // Assert
        factory.Should().NotBeNull();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
        factory.Priority.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new CharacterResourceFactory(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Priority_ShouldReturnExpectedValue()
    {
        // Act
        var priority = _factory.Priority;

        // Assert
        priority.Should().BeGreaterThan(0);
        priority.Should().BeLessThan(1000);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainCharacterTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("CASP");
        supportedTypes.Should().Contain("OUTF");
        supportedTypes.Should().Contain("BOND");
        supportedTypes.Should().Contain("SKIN");
    }

    [Fact]
    public void SupportedResourceTypes_WithSupportedResourceType_ShouldContainCASP()
    {
        // Arrange
        const string resourceType = "CASP";

        // Act
        var result = _factory.SupportedResourceTypes.Contains(resourceType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportedResourceTypes_WithUnsupportedResourceType_ShouldNotContainUnknown()
    {
        // Arrange
        const string resourceType = "UNKNOWN";

        // Act
        var result = _factory.SupportedResourceTypes.Contains(resourceType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SupportedResourceTypes_ShouldNotContainNullValues()
    {
        // Act
        var result = _factory.SupportedResourceTypes;

        // Assert
        result.Should().NotContainNulls();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void SupportedResourceTypes_ShouldNotContainEmptyValues()
    {
        // Act
        var result = _factory.SupportedResourceTypes;

        // Assert
        result.Should().NotContain(string.Empty);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateResourceAsync_WithSupportedType_ShouldReturnCharacterResource()
    {
        // Arrange
        const int apiVersion = 1;
        using var stream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CharacterResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithUnsupportedType_ShouldThrowNotSupportedException()
    {
        // Arrange
        const int apiVersion = 999; // Unsupported version
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(apiVersion, stream))
            .Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*API version*not supported*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ShouldCreateValidResource()
    {
        // Arrange
        const int apiVersion = 1;

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, null);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CharacterResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidApiVersion_ShouldThrowNotSupportedException()
    {
        // Arrange
        const int apiVersion = -1; // Invalid API version
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(apiVersion, stream))
            .Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*API version*not supported*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithZeroApiVersion_ShouldThrowNotSupportedException()
    {
        // Arrange
        const int apiVersion = 0; // Invalid API version
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(apiVersion, stream))
            .Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*API version*not supported*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CreateResourceAsync_WithValidApiVersions_ShouldReturnValidResources(int apiVersion)
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CharacterResource>();
        resource.Should().BeAssignableTo<ICharacterResource>();
    }
}
