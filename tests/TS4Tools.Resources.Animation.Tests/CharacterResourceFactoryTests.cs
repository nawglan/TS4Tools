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
        factory.ApiVersion.Should().NotBeNullOrEmpty();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new CharacterResourceFactory(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void ApiVersion_ShouldReturnExpectedVersion()
    {
        // Act
        var version = _factory.ApiVersion;

        // Assert
        version.Should().NotBeNullOrEmpty();
        version.Should().MatchRegex(@"\d+\.\d+");
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
    public void CanHandle_WithSupportedResourceType_ShouldReturnTrue()
    {
        // Arrange
        const string resourceType = "CASP";

        // Act
        var result = _factory.CanHandle(resourceType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WithUnsupportedResourceType_ShouldReturnFalse()
    {
        // Arrange
        const string resourceType = "UNKNOWN";

        // Act
        var result = _factory.CanHandle(resourceType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WithNullResourceType_ShouldReturnFalse()
    {
        // Act
        var result = _factory.CanHandle(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WithEmptyResourceType_ShouldReturnFalse()
    {
        // Act
        var result = _factory.CanHandle(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateResourceAsync_WithSupportedType_ShouldReturnCharacterResource()
    {
        // Arrange
        const string resourceType = "CASP";
        using var stream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(resourceType, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CharacterResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithUnsupportedType_ShouldThrowNotSupportedException()
    {
        // Arrange
        const string resourceType = "UNKNOWN";
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(resourceType, stream))
            .Should().ThrowAsync<NotSupportedException>()
            .WithMessage("Resource type 'UNKNOWN' is not supported by CharacterResourceFactory");
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        const string resourceType = "CASP";

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(resourceType, null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullResourceType_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(null!, stream))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("resourceType");
    }

    [Fact]
    public async Task CreateResourceAsync_WithEmptyResourceType_ShouldThrowArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(string.Empty, stream))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("resourceType");
    }

    [Theory]
    [InlineData("CASP")]
    [InlineData("OUTF")]
    [InlineData("BOND")]
    [InlineData("SKIN")]
    public async Task CreateResourceAsync_WithAllSupportedTypes_ShouldReturnValidResources(string resourceType)
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(resourceType, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<CharacterResource>();
        resource.Should().BeAssignableTo<ICharacterResource>();
    }
}
