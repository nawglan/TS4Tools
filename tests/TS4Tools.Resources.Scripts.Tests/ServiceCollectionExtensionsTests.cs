using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using Xunit;

namespace TS4Tools.Resources.Scripts.Tests;

/// <summary>
/// Unit tests for service collection extensions.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddScriptResources_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ServiceCollectionExtensions.AddScriptResources(null!));
    }

    [Fact]
    public void AddScriptResources_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddScriptResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify factory registration
        var concreteFactory = serviceProvider.GetService<ScriptResourceFactory>();
        concreteFactory.Should().NotBeNull();

        var genericFactory = serviceProvider.GetService<IResourceFactory<IScriptResource>>();
        genericFactory.Should().NotBeNull();
        genericFactory.Should().BeSameAs(concreteFactory);

        var baseFactory = serviceProvider.GetService<IResourceFactory>();
        baseFactory.Should().NotBeNull();
        baseFactory.Should().BeSameAs(concreteFactory);
    }

    [Fact]
    public void AddScriptResources_CalledMultipleTimes_ShouldNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddScriptResources();
        services.AddScriptResources(); // Call again

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var factories = serviceProvider.GetServices<ScriptResourceFactory>();
        factories.Should().HaveCount(1);
    }

    [Fact]
    public void AddScriptResources_WithExistingServices_ShouldIntegrateCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Add some other service first
        services.AddSingleton<string>("test-service");

        // Act
        services.AddScriptResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var factory = serviceProvider.GetService<ScriptResourceFactory>();
        factory.Should().NotBeNull();
        
        var testService = serviceProvider.GetService<string>();
        testService.Should().Be("test-service");
    }

    [Fact]
    public void AddScriptResources_ShouldReturnServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddScriptResources();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void RegisteredFactory_ShouldHaveCorrectConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScriptResources();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ScriptResourceFactory>();

        // Assert
        factory.Priority.Should().Be(100);
        factory.SupportedResourceTypes.Should().Contain($"0x{ScriptResourceFactory.ScriptResourceType:X8}");
        factory.CanCreateResource(ScriptResourceFactory.ScriptResourceType).Should().BeTrue();
        factory.CanCreateResource(0x12345678).Should().BeFalse();
    }

    [Fact]
    public void RegisteredFactory_ShouldCreateValidResources()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScriptResources();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResourceFactory<IScriptResource>>();

        // Act
        using var emptyStream = new MemoryStream();
        var resource = factory.CreateResource(emptyStream, ScriptResourceFactory.ScriptResourceType);

        // Assert
        using (resource)
        {
            resource.Should().NotBeNull();
            resource.ResourceKey.Should().Be(new ResourceKey(0, ScriptResourceFactory.ScriptResourceType, 0));
            resource.Should().BeOfType<ScriptResource>();
        }
    }
}
