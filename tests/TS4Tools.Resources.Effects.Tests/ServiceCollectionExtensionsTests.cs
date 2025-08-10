using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Effects.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEffectResources_WithValidServiceCollection_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEffectResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetService<IResourceFactory>();
        factory.Should().NotBeNull();
        factory.Should().BeOfType<EffectResourceFactory>();
    }

    [Fact]
    public void AddEffectResources_WithNullServiceCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Action act = () => services!.AddEffectResources();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEffectResources_ShouldRegisterFactoryAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEffectResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var factory1 = serviceProvider.GetService<IResourceFactory>();
        var factory2 = serviceProvider.GetService<IResourceFactory>();

        factory1.Should().NotBeSameAs(factory2); // Transient should create new instances
    }

    [Fact]
    public void AddEffectResources_ShouldAllowMultipleCalls()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        services.AddEffectResources();
        services.AddEffectResources(); // Should not throw

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IResourceFactory>();
        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddEffectResources_WithExistingLogging_ShouldUseExistingLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEffectResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetService<IResourceFactory>();
        factory.Should().NotBeNull();

        // Verify logging is available
        var logger = serviceProvider.GetService<ILogger<EffectResourceFactory>>();
        logger.Should().NotBeNull();
    }

    [Fact]
    public void AddEffectResources_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEffectResources();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEffectResources_RegisteredFactory_ShouldCreateEffectResources()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEffectResources();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResourceFactory>();

        // Act
        var supportedTypes = factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().Contain("RSLT");
    }

    [Fact]
    public void AddEffectResources_RegisteredFactory_ShouldSupportExpectedResourceTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEffectResources();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResourceFactory>();

        // Act & Assert
        factory.SupportedResourceTypes.Should().Contain("RSLT");
        factory.SupportedResourceTypes.Should().Contain("MATD");
        factory.SupportedResourceTypes.Should().Contain("EFCT");
        factory.SupportedResourceTypes.Should().Contain("SHAD");
    }

    [Fact]
    public void AddEffectResources_WithoutLogging_ShouldStillRegisterFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add basic logging support

        // Act
        services.AddEffectResources();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IResourceFactory>();
        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddEffectResources_ServiceDescriptor_ShouldHaveCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEffectResources();

        // Assert
        var serviceDescriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(IResourceFactory));

        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEffectResources_ServiceDescriptor_ShouldHaveCorrectImplementationType()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEffectResources();

        // Assert
        var serviceDescriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(IResourceFactory));

        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.ImplementationType.Should().Be(typeof(EffectResourceFactory));
    }
}
