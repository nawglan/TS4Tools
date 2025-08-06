using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public sealed class BasicAnimationResourceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public BasicAnimationResourceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddAnimationResources();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ServiceRegistration_ShouldRegisterFactories()
    {
        // Act
        var factories = _serviceProvider.GetServices<IResourceFactory>();

        // Assert
        factories.Should().NotBeEmpty();
        factories.Should().Contain(f => f is AnimationResourceFactory);
        factories.Should().Contain(f => f is CharacterResourceFactory);
        factories.Should().Contain(f => f is RigResourceFactory);
    }

    [Fact]
    public void AnimationResource_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var resource = new AnimationResource();

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<IAnimationResource>();
    }

    [Fact]
    public void CharacterResource_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var resource = new CharacterResource();

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<ICharacterResource>();
    }

    [Fact]
    public void RigResource_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var resource = new RigResource();

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<IRigResource>();
    }

    [Fact]
    public async Task AnimationResourceFactory_CreateResourceAsync_ShouldReturnValidResource()
    {
        // Arrange
        var factories = _serviceProvider.GetServices<IResourceFactory>();
        var animationFactory = factories.OfType<AnimationResourceFactory>().First();

        // Act
        var resource = await animationFactory.CreateResourceAsync(1);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<IAnimationResource>();
    }

    [Fact]
    public async Task CharacterResourceFactory_CreateResourceAsync_ShouldReturnValidResource()
    {
        // Arrange
        var factories = _serviceProvider.GetServices<IResourceFactory>();
        var characterFactory = factories.OfType<CharacterResourceFactory>().First();

        // Act
        var resource = await characterFactory.CreateResourceAsync(1);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<ICharacterResource>();
    }

    [Fact]
    public async Task RigResourceFactory_CreateResourceAsync_ShouldReturnValidResource()
    {
        // Arrange
        var factories = _serviceProvider.GetServices<IResourceFactory>();
        var rigFactory = factories.OfType<RigResourceFactory>().First();

        // Act
        var resource = await rigFactory.CreateResourceAsync(1);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeAssignableTo<IRigResource>();
    }

    [Fact]
    public void AnimationResourceFactory_SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Arrange
        var factories = _serviceProvider.GetServices<IResourceFactory>();
        var animationFactory = factories.OfType<AnimationResourceFactory>().First();

        // Act
        var supportedTypes = animationFactory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("CLIP");
        supportedTypes.Should().Contain("ANIM");
    }

    [Fact]
    public void CharacterResourceFactory_SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Arrange
        var factories = _serviceProvider.GetServices<IResourceFactory>();
        var characterFactory = factories.OfType<CharacterResourceFactory>().First();

        // Act
        var supportedTypes = characterFactory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("CASP");
    }

    [Fact]
    public void RigResourceFactory_SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Arrange
        var factories = _serviceProvider.GetServices<IResourceFactory>();
        var rigFactory = factories.OfType<RigResourceFactory>().First();

        // Act
        var supportedTypes = rigFactory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("RIGS");
        supportedTypes.Should().Contain("BOND");
        supportedTypes.Should().Contain("SKEL");
    }

    [Theory]
    [InlineData(AnimationType.Clip, 1)]
    [InlineData(AnimationType.Pose, 2)]
    public void AnimationType_Values_ShouldHaveExpectedValues(AnimationType type, int expectedValue)
    {
        // Act & Assert
        ((int)type).Should().Be(expectedValue);
    }

    [Fact]
    public void CharacterType_Values_ShouldBeGreaterThanZero()
    {
        // Act & Assert
        ((int)CharacterType.CasPart).Should().BeGreaterThan(0);
        ((int)CharacterType.Outfit).Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(AgeCategory.Baby, 1)]
    [InlineData(AgeCategory.Toddler, 2)]
    public void AgeCategory_Values_ShouldFollowExpectedOrder(AgeCategory age, int expectedValue)
    {
        // Act & Assert
        ((int)age).Should().Be(expectedValue);
    }
}
