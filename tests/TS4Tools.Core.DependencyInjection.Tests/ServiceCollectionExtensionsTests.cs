using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.System.Platform;
using TS4Tools.Extensions.ResourceTypes;
using TS4Tools.Extensions.Utilities;
using TS4Tools.Resources.Common.CatalogTags;
using Xunit;

namespace TS4Tools.Core.DependencyInjection.Tests;

/// <summary>
/// Tests for core dependency injection extension methods.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Tests that AddTS4ToolsPlatformServices properly registers platform services.
    /// </summary>
    [Fact]
    public void AddTS4ToolsPlatformServices_WithValidServiceCollection_ShouldRegisterPlatformServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddTS4ToolsPlatformServices();

        // Assert
        result.Should().BeSameAs(services); // Should return same collection for chaining

        // Verify service registration
        var serviceProvider = services.BuildServiceProvider();
        var platformService = serviceProvider.GetService<IPlatformService>();

        platformService.Should().NotBeNull();
        platformService.Should().BeOfType<PlatformService>();
    }

    /// <summary>
    /// Tests that AddTS4ToolsPlatformServices throws ArgumentNullException when passed a null service collection.
    /// </summary>
    [Fact]
    public void AddTS4ToolsPlatformServices_WithNullServiceCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddTS4ToolsPlatformServices(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Tests that AddTS4ToolsPlatformServices registers IPlatformService as singleton.
    /// </summary>
    [Fact]
    public void AddTS4ToolsPlatformServices_ShouldRegisterPlatformServiceAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTS4ToolsPlatformServices();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var platformService1 = serviceProvider.GetService<IPlatformService>();
        var platformService2 = serviceProvider.GetService<IPlatformService>();

        // Assert
        platformService1.Should().BeSameAs(platformService2); // Singleton should return same instance
    }

    /// <summary>
    /// Tests that AddTS4ToolsExtensions properly registers extension services.
    /// </summary>
    [Fact]
    public void AddTS4ToolsExtensions_WithValidServiceCollection_ShouldRegisterExtensionServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services required by the implementations

        // Act
        var result = services.AddTS4ToolsExtensions();

        // Assert
        result.Should().BeSameAs(services); // Should return same collection for chaining

        // Verify service registration by checking the service descriptors
        var serviceDescriptors = services.ToList();
        
        serviceDescriptors.Should().Contain(d => d.ServiceType == typeof(IResourceTypeRegistry) && d.ImplementationType == typeof(ResourceTypeRegistry));
        serviceDescriptors.Should().Contain(d => d.ServiceType == typeof(IFileNameService) && d.ImplementationType == typeof(FileNameService));
    }

    /// <summary>
    /// Tests that AddTS4ToolsExtensions throws ArgumentNullException when passed a null service collection.
    /// </summary>
    [Fact]
    public void AddTS4ToolsExtensions_WithNullServiceCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddTS4ToolsExtensions(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Tests that AddTS4ToolsResourceCommon properly registers resource common services.
    /// </summary>
    [Fact]
    public void AddTS4ToolsResourceCommon_WithValidServiceCollection_ShouldRegisterResourceCommonServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services required by the implementations

        // Act
        var result = services.AddTS4ToolsResourceCommon();

        // Assert
        result.Should().BeSameAs(services); // Should return same collection for chaining

        // Verify service registration by checking the service descriptors
        var serviceDescriptors = services.ToList();
        serviceDescriptors.Should().Contain(d => d.ServiceType == typeof(CatalogTagRegistry) && d.ImplementationType == typeof(CatalogTagRegistry));
    }

    /// <summary>
    /// Tests that AddTS4ToolsResourceCommon throws ArgumentNullException when passed a null service collection.
    /// </summary>
    [Fact]
    public void AddTS4ToolsResourceCommon_WithNullServiceCollection_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddTS4ToolsResourceCommon(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Tests that AddTS4ToolsCore properly registers core services with valid configuration.
    /// </summary>
    [Fact]
    public void AddTS4ToolsCore_WithValidParameters_ShouldRegisterCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = Substitute.For<IConfiguration>();

        // Act
        var result = services.AddTS4ToolsCore(configuration);

        // Assert
        result.Should().BeSameAs(services); // Should return same collection for chaining

        // Verify that logging services are registered
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that AddTS4ToolsCore throws ArgumentNullException when passed null services.
    /// </summary>
    [Fact]
    public void AddTS4ToolsCore_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddTS4ToolsCore(null!, configuration);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Tests that AddTS4ToolsCore throws ArgumentNullException when passed null configuration.
    /// </summary>
    [Fact]
    public void AddTS4ToolsCore_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddTS4ToolsCore(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    /// <summary>
    /// Tests that AddTS4ToolsServices properly configures a host application builder.
    /// </summary>
    [Fact]
    public void AddTS4ToolsServices_WithValidBuilder_ShouldConfigureServices()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        // Act
        var result = builder.AddTS4ToolsServices();

        // Assert
        result.Should().BeSameAs(builder); // Should return same builder for chaining

        // Verify that services are configured
        var app = builder.Build();
        var loggerFactory = app.Services.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that AddTS4ToolsServices throws ArgumentNullException when passed null builder.
    /// </summary>
    [Fact]
    public void AddTS4ToolsServices_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddTS4ToolsServices(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    /// <summary>
    /// Tests that multiple registrations work correctly together.
    /// </summary>
    [Fact]
    public void MultipleRegistrations_ShouldWorkTogether()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services required by the implementations

        // Act
        services.AddTS4ToolsPlatformServices()
                .AddTS4ToolsExtensions()
                .AddTS4ToolsResourceCommon();

        // Assert
        var serviceDescriptors = services.ToList();

        // Verify all services are registered
        serviceDescriptors.Should().Contain(d => d.ServiceType == typeof(IPlatformService) && d.ImplementationType == typeof(PlatformService));
        serviceDescriptors.Should().Contain(d => d.ServiceType == typeof(IResourceTypeRegistry) && d.ImplementationType == typeof(ResourceTypeRegistry));
        serviceDescriptors.Should().Contain(d => d.ServiceType == typeof(IFileNameService) && d.ImplementationType == typeof(FileNameService));
        serviceDescriptors.Should().Contain(d => d.ServiceType == typeof(CatalogTagRegistry) && d.ImplementationType == typeof(CatalogTagRegistry));
    }
}
