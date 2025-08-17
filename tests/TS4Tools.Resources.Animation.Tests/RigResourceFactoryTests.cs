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
        factory.SupportedResourceTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => { var _ = new RigResourceFactory(null!); };
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainRigTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("RIGS");
    }

    [Fact]
    public void SupportedResourceTypes_WithSupportedResourceType_ShouldReturnTrue()
    {
        // Arrange
        const string resourceType = "RIGS";

        // Act
        var isSupported = _factory.SupportedResourceTypes.Contains(resourceType);

        // Assert
        isSupported.Should().BeTrue();
    }

    [Fact]
    public void SupportedResourceTypes_WithUnsupportedResourceType_ShouldReturnFalse()
    {
        // Arrange
        const string resourceType = "UNKNOWN";

        // Act
        var isSupported = _factory.SupportedResourceTypes.Contains(resourceType);

        // Assert
        isSupported.Should().BeFalse();
    }

    [Fact]
    public void SupportedResourceTypes_WithNullResourceType_ShouldNotContainNull()
    {
        // Act
        var containsNull = _factory.SupportedResourceTypes.Contains(null!);

        // Assert
        containsNull.Should().BeFalse();
    }

    [Fact]
    public void SupportedResourceTypes_WithEmptyResourceType_ShouldReturnFalse()
    {
        // Act
        var containsEmpty = _factory.SupportedResourceTypes.Contains(string.Empty);

        // Assert
        containsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task CreateResourceAsync_WithSupportedType_ShouldReturnRigResource()
    {
        // Arrange
        const int apiVersion = 1;
        using var stream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<RigResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithUnsupportedApiVersion_ShouldThrowArgumentException()
    {
        // Arrange
        const int apiVersion = 999; // Unsupported version
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(apiVersion, stream))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("apiVersion");
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
        resource.Should().BeOfType<RigResource>();
    }

    [Fact]
    public async Task CreateResourceAsync_WithZeroApiVersion_ShouldThrowArgumentException()
    {
        // Arrange
        const int apiVersion = 0;
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(apiVersion, stream))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("apiVersion");
    }

    [Fact]
    public async Task CreateResourceAsync_WithNegativeApiVersion_ShouldThrowArgumentException()
    {
        // Arrange
        const int apiVersion = -1;
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _factory.CreateResourceAsync(apiVersion, stream))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("apiVersion");
    }

    [Theory]
    [InlineData(1)]
    public async Task CreateResourceAsync_WithAllSupportedVersions_ShouldReturnValidResources(int apiVersion)
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var resource = await _factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<RigResource>();
        resource.Should().BeAssignableTo<IRigResource>();
    }
}
