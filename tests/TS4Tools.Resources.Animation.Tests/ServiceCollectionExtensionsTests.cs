using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Resources.Animation;
using Xunit;

namespace TS4Tools.Resources.Animation.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAnimationResources_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAnimationResources();
        using var provider = services.BuildServiceProvider();

        // Assert - Check that all factories are registered
        provider.GetService<AnimationResourceFactory>().Should().NotBeNull();
        provider.GetService<CharacterResourceFactory>().Should().NotBeNull();
        provider.GetService<RigResourceFactory>().Should().NotBeNull();
    }

    [Fact]
    public void AddAnimationResources_ShouldRegisterFactoriesAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnimationResources();
        using var provider = services.BuildServiceProvider();

        // Act
        var factory1 = provider.GetService<AnimationResourceFactory>();
        var factory2 = provider.GetService<AnimationResourceFactory>();

        // Assert
        factory1.Should().NotBeNull();
        factory2.Should().NotBeNull();
        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void AddAnimationResources_WithoutLogging_ShouldThrowOnCreation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAnimationResources();

        // Act & Assert
        Action act = () =>
        {
            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<AnimationResourceFactory>();
        };

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddAnimationResources_ShouldAllowMultipleCalls()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAnimationResources();
        services.AddAnimationResources(); // Second call should not cause issues

        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<AnimationResourceFactory>().Should().NotBeNull();
        provider.GetService<CharacterResourceFactory>().Should().NotBeNull();
        provider.GetService<RigResourceFactory>().Should().NotBeNull();
    }

    [Fact]
    public void AddAnimationResources_ShouldRegisterFactoriesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnimationResources();

        // Act
        var animationDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(AnimationResourceFactory));
        var characterDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(CharacterResourceFactory));
        var rigDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(RigResourceFactory));

        // Assert
        animationDescriptor.Should().NotBeNull();
        animationDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        
        characterDescriptor.Should().NotBeNull();
        characterDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        
        rigDescriptor.Should().NotBeNull();
        rigDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddAnimationResources_RegisteredFactories_ShouldHaveCorrectApiVersions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnimationResources();
        using var provider = services.BuildServiceProvider();

        // Act
        var animationFactory = provider.GetRequiredService<AnimationResourceFactory>();
        var characterFactory = provider.GetRequiredService<CharacterResourceFactory>();
        var rigFactory = provider.GetRequiredService<RigResourceFactory>();

        // Assert
        animationFactory.ApiVersion.Should().NotBeNullOrEmpty();
        characterFactory.ApiVersion.Should().NotBeNullOrEmpty();
        rigFactory.ApiVersion.Should().NotBeNullOrEmpty();
        
        // All should have the same API version
        animationFactory.ApiVersion.Should().Be(characterFactory.ApiVersion);
        characterFactory.ApiVersion.Should().Be(rigFactory.ApiVersion);
    }

    [Fact]
    public void AddAnimationResources_RegisteredFactories_ShouldHaveNonEmptySupportedTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnimationResources();
        using var provider = services.BuildServiceProvider();

        // Act
        var animationFactory = provider.GetRequiredService<AnimationResourceFactory>();
        var characterFactory = provider.GetRequiredService<CharacterResourceFactory>();
        var rigFactory = provider.GetRequiredService<RigResourceFactory>();

        // Assert
        animationFactory.SupportedResourceTypes.Should().NotBeEmpty();
        characterFactory.SupportedResourceTypes.Should().NotBeEmpty();
        rigFactory.SupportedResourceTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void AddAnimationResources_ShouldWorkWithExistingServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILogger<ServiceCollectionExtensionsTests>>(
            provider => provider.GetRequiredService<ILogger<ServiceCollectionExtensionsTests>>());

        // Act
        services.AddAnimationResources();
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<AnimationResourceFactory>().Should().NotBeNull();
        provider.GetService<ILogger<ServiceCollectionExtensionsTests>>().Should().NotBeNull();
    }
}
