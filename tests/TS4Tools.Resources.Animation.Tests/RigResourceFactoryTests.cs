using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public sealed class RigResourceFactoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly RigResourceFactory _factory;

    public RigResourceFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddAnimationResources();
        _serviceProvider = services.BuildServiceProvider();
        _factory = _serviceProvider.GetRequiredService<RigResourceFactory>();
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
        var logger = _serviceProvider.GetRequiredService<ILogger<RigResourceFactory>>();

        // Act
        var factory = new RigResourceFactory(logger);

        // Assert
        factory.Should().NotBeNull();
        factory.ApiVersion.Should().NotBeNullOrEmpty();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new RigResourceFactory(null!);
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
    public void SupportedResourceTypes_ShouldContainRigTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("RIG");
        supportedTypes.Should().Contain("SKEL");
        supportedTypes.Should().Contain("BOND");
    }

    [Fact]
    public void CanHandle_WithSupportedResourceType_ShouldReturnTrue()
    {
        // Arrange
        const string resourceType = "RIG";

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
    public async Task CreateResourceAsync_WithSupportedType_ShouldReturnRigResource()
    {
        // Arrange
        const string resourceType = "RIG";
        using var stream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(resourceType, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<RigResource>();
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
            .WithMessage("Resource type 'UNKNOWN' is not supported by RigResourceFactory");
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        const string resourceType = "RIG";

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
    [InlineData("RIG")]
    [InlineData("SKEL")]
    [InlineData("BOND")]
    public async Task CreateResourceAsync_WithAllSupportedTypes_ShouldReturnValidResources(string resourceType)
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(resourceType, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<RigResource>();
        resource.Should().BeAssignableTo<IRigResource>();
    }
}
