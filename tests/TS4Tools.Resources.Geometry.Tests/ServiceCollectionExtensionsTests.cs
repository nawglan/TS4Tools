using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Resources.Geometry;
using TS4Tools.Resources.Geometry.DependencyInjection;

namespace TS4Tools.Resources.Geometry.Tests;

/// <summary>
/// Unit tests for ServiceCollectionExtensions.
/// Tests dependency injection registration for geometry resources.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGeometryResources_WithValidServices_RegistersAllFactories()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGeometryResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        serviceProvider.GetService<GeometryResourceFactory>().Should().NotBeNull();
        serviceProvider.GetService<MeshResourceFactory>().Should().NotBeNull();
    }

    [Fact]
    public void AddGeometryResources_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddGeometryResources();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddGeometryResources_RegistersFactoriesAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGeometryResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var factory1 = serviceProvider.GetService<GeometryResourceFactory>();
        var factory2 = serviceProvider.GetService<GeometryResourceFactory>();
        
        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void AddGeometryResources_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGeometryResources();
        services.AddGeometryResources(); // Should not throw

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<GeometryResourceFactory>().Should().NotBeNull();
    }

    [Fact]
    public void AddGeometryResources_WithExistingLogger_UsesProvidedLogger()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => { });
        services.AddSingleton<ILoggerFactory>(loggerFactory);

        // Act
        services.AddGeometryResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<GeometryResourceFactory>();
        
        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddGeometryResources_RegistersCorrectServiceTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGeometryResources();

        // Assert
        var serviceDescriptors = services.ToList();
        
        serviceDescriptors.Should().Contain(sd => 
            sd.ServiceType == typeof(GeometryResourceFactory) && 
            sd.Lifetime == ServiceLifetime.Singleton);
            
        serviceDescriptors.Should().Contain(sd => 
            sd.ServiceType == typeof(MeshResourceFactory) && 
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGeometryResources_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddGeometryResources();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddGeometryResources_AllowsFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
            .AddLogging()
            .AddGeometryResources();

        // Assert
        result.Should().BeSameAs(services);
        
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<GeometryResourceFactory>().Should().NotBeNull();
        serviceProvider.GetService<MeshResourceFactory>().Should().NotBeNull();
    }
}
