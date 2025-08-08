using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;
using Xunit;

namespace TS4Tools.Resources.Utility.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddUtilityResources_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddUtilityResources();

        // Assert
        result.Should().BeSameAs(services);

        using var serviceProvider = services.BuildServiceProvider();

        // Verify factories are registered
        serviceProvider.GetService<DataResourceFactory>().Should().NotBeNull();
        serviceProvider.GetService<ConfigResourceFactory>().Should().NotBeNull();
        serviceProvider.GetService<MetadataResourceFactory>().Should().NotBeNull();

        // Verify they're registered as IResourceFactory
        var resourceFactories = serviceProvider.GetServices<IResourceFactory>().ToList();
        resourceFactories.Should().HaveCountGreaterOrEqualTo(3);
        resourceFactories.Should().Contain(f => f is DataResourceFactory);
        resourceFactories.Should().Contain(f => f is ConfigResourceFactory);
        resourceFactories.Should().Contain(f => f is MetadataResourceFactory);
    }

    [Fact]
    public void AddUtilityResources_WithNullServiceCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        var act = () => services!.AddUtilityResources();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddUtilityResources_ShouldRegisterServicesAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUtilityResources();

        // Act
        using var serviceProvider = services.BuildServiceProvider();

        var dataFactory1 = serviceProvider.GetService<DataResourceFactory>();
        var dataFactory2 = serviceProvider.GetService<DataResourceFactory>();
        var configFactory1 = serviceProvider.GetService<ConfigResourceFactory>();
        var configFactory2 = serviceProvider.GetService<ConfigResourceFactory>();
        var metadataFactory1 = serviceProvider.GetService<MetadataResourceFactory>();
        var metadataFactory2 = serviceProvider.GetService<MetadataResourceFactory>();

        // Assert - Each service should be a new instance (transient)
        dataFactory1.Should().NotBeSameAs(dataFactory2);
        configFactory1.Should().NotBeSameAs(configFactory2);
        metadataFactory1.Should().NotBeSameAs(metadataFactory2);
    }

    [Fact]
    public void AddUtilityResources_WithExistingServices_ShouldNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUtilityResources();
        services.AddUtilityResources(); // Add again

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        // Should still work and not cause issues
        var dataFactory = serviceProvider.GetService<DataResourceFactory>();
        var configFactory = serviceProvider.GetService<ConfigResourceFactory>();
        var metadataFactory = serviceProvider.GetService<MetadataResourceFactory>();

        dataFactory.Should().NotBeNull();
        configFactory.Should().NotBeNull();
        metadataFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddUtilityResources_WithoutLogging_ShouldStillRegisterFactories()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUtilityResources();

        // Assert - Should register but may fail at runtime without logging
        var dataFactoryDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(DataResourceFactory));
        var configFactoryDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ConfigResourceFactory));
        var metadataFactoryDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(MetadataResourceFactory));

        dataFactoryDescriptor.Should().NotBeNull();
        configFactoryDescriptor.Should().NotBeNull();
        metadataFactoryDescriptor.Should().NotBeNull();

        dataFactoryDescriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
        configFactoryDescriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
        metadataFactoryDescriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddUtilityResources_ShouldRegisterCorrectServiceTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUtilityResources();

        // Assert
        var serviceDescriptors = services.ToList();

        // Check DataResourceFactory registrations
        serviceDescriptors.Should().Contain(s => s.ServiceType == typeof(DataResourceFactory) && s.Lifetime == ServiceLifetime.Transient);
        serviceDescriptors.Should().Contain(s => s.ServiceType == typeof(IResourceFactory) && s.ImplementationType == typeof(DataResourceFactory));

        // Check ConfigResourceFactory registrations
        serviceDescriptors.Should().Contain(s => s.ServiceType == typeof(ConfigResourceFactory) && s.Lifetime == ServiceLifetime.Transient);
        serviceDescriptors.Should().Contain(s => s.ServiceType == typeof(IResourceFactory) && s.ImplementationType == typeof(ConfigResourceFactory));

        // Check MetadataResourceFactory registrations
        serviceDescriptors.Should().Contain(s => s.ServiceType == typeof(MetadataResourceFactory) && s.Lifetime == ServiceLifetime.Transient);
        serviceDescriptors.Should().Contain(s => s.ServiceType == typeof(IResourceFactory) && s.ImplementationType == typeof(MetadataResourceFactory));
    }

    [Fact]
    public void AddUtilityResources_FactoriesShouldHaveCorrectDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUtilityResources();

        // Act
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Factories should be able to resolve without throwing
        var act1 = () => serviceProvider.GetRequiredService<DataResourceFactory>();
        var act2 = () => serviceProvider.GetRequiredService<ConfigResourceFactory>();
        var act3 = () => serviceProvider.GetRequiredService<MetadataResourceFactory>();

        act1.Should().NotThrow();
        act2.Should().NotThrow();
        act3.Should().NotThrow();

        // Verify they have proper logger dependencies
        var dataFactory = serviceProvider.GetService<DataResourceFactory>();
        var configFactory = serviceProvider.GetService<ConfigResourceFactory>();
        var metadataFactory = serviceProvider.GetService<MetadataResourceFactory>();

        dataFactory.Should().NotBeNull();
        configFactory.Should().NotBeNull();
        metadataFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddUtilityResources_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should allow method chaining
        var result = services
            .AddLogging()
            .AddUtilityResources();

        result.Should().BeSameAs(services);

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<DataResourceFactory>().Should().NotBeNull();
        serviceProvider.GetService<ConfigResourceFactory>().Should().NotBeNull();
        serviceProvider.GetService<MetadataResourceFactory>().Should().NotBeNull();
    }
}
