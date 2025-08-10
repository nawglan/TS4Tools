using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Text.DependencyInjection;
using Xunit;

namespace TS4Tools.Resources.Text.Tests.DependencyInjection;

/// <summary>
/// Tests for text resource dependency injection extensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Tests that AddTextResourceServices properly registers the text resource services with a valid service collection.
    /// </summary>
    [Fact]
    public void AddTextResourceServices_WithValidServiceCollection_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services

        // Act
        var result = services.AddTextResourceServices();

        // Assert
        result.Should().BeSameAs(services); // Should return same collection for chaining

        // Verify service registration
        var serviceProvider = services.BuildServiceProvider();
        var factories = serviceProvider.GetServices<IResourceFactory<ITextResource>>().ToList();

        factories.Should().ContainSingle(f => f is TextResourceFactory);
    }

    /// <summary>
    /// Tests that AddTextResourceServices throws ArgumentNullException when passed a null service collection.
    /// </summary>
    [Fact]
    public void AddTextResourceServices_WithNullServiceCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => TS4Tools.Resources.Text.DependencyInjection.ServiceCollectionExtensions.AddTextResourceServices(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Tests that AddTextResourceServices can be called multiple times and registers multiple factory instances.
    /// </summary>
    [Fact]
    public void AddTextResourceServices_WhenCalledMultipleTimes_ShouldRegisterMultipleFactories()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services

        // Act
        services.AddTextResourceServices();
        services.AddTextResourceServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factories = serviceProvider.GetServices<IResourceFactory<ITextResource>>().ToList();

        // Should have two instances (one per registration)
        var textFactories = factories.Where(f => f is TextResourceFactory).ToList();
        textFactories.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that the text resource factory is registered as a singleton in the service collection.
    /// </summary>
    [Fact]
    public void AddTextResourceServices_ShouldRegisterFactoryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        services.AddTextResourceServices();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var factory1 = serviceProvider.GetServices<IResourceFactory<ITextResource>>()
            .OfType<TextResourceFactory>()
            .First();
        var factory2 = serviceProvider.GetServices<IResourceFactory<ITextResource>>()
            .OfType<TextResourceFactory>()
            .First();

        // Assert
        factory1.Should().BeSameAs(factory2); // Singleton should return same instance
    }

    /// <summary>
    /// Tests that adding text resource services does not interfere with other existing services in the collection.
    /// </summary>
    [Fact]
    public void AddTextResourceServices_WithExistingServices_ShouldNotInterruptOtherServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services
        services.AddSingleton<ITestService, TestService>();

        // Act
        services.AddTextResourceServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Original service should still be available
        var testService = serviceProvider.GetService<ITestService>();
        testService.Should().NotBeNull().And.BeOfType<TestService>();

        // Text resource factory should also be available
        var textFactory = serviceProvider.GetServices<IResourceFactory<ITextResource>>()
            .OfType<TextResourceFactory>()
            .FirstOrDefault();
        textFactory.Should().NotBeNull();
    }

    // Test helper interfaces and classes
    private interface ITestService
    {
    }

    private sealed class TestService : ITestService
    {
    }
}
