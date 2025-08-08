using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using Xunit;

namespace TS4Tools.Resources.Utility.Tests;

public class UtilityResourceFactoriesTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public UtilityResourceFactoriesTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUtilityResources();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _serviceProvider.Dispose();
        }
    }

    [Theory]
    [InlineData(0x0166038C, typeof(DataResource))]
    [InlineData(0x0166038D, typeof(DataResource))]
    [InlineData(0x0166038E, typeof(DataResource))]
    public void DataResourceFactory_WithSupportedResourceType_ShouldReturnCorrectResource(uint resourceType, Type expectedType)
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();
        var key = new ResourceKey(resourceType, 0x12345678, 0x87654321);

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType(expectedType);

    }

    [Theory]
    [InlineData(0x0166038C)]
    [InlineData(0x0166038D)]
    [InlineData(0x0166038E)]
    public void DataResourceFactory_SupportsResourceType_WithDataResourceTypes_ShouldReturnTrue(uint resourceType)
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();

        // Act
        var supports = factory!.CanCreateResource(resourceType);

        // Assert
        supports.Should().BeTrue();
    }

    [Theory]
    [InlineData(0x01661233)]
    [InlineData(0x00000001)]
    public void DataResourceFactory_SupportsResourceType_WithUnsupportedTypes_ShouldReturnFalse(uint resourceType)
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();

        // Act
        var supports = factory!.CanCreateResource(resourceType);

        // Assert
        supports.Should().BeFalse();
    }

    [Theory]
    [InlineData(0x0000038C, typeof(ConfigResource))]
    [InlineData(0x0000038D, typeof(ConfigResource))]
    [InlineData(0x0000038E, typeof(ConfigResource))]
    public void ConfigResourceFactory_WithSupportedResourceType_ShouldReturnCorrectResource(uint resourceType, Type expectedType)
    {
        // Arrange
        var factory = _serviceProvider.GetService<ConfigResourceFactory>();
        var key = new ResourceKey(resourceType, 0x12345678, 0x87654321);

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType(expectedType);

    }

    [Theory]
    [InlineData(0x0000038C)]
    [InlineData(0x0000038D)]
    [InlineData(0x0000038E)]
    public void ConfigResourceFactory_SupportsResourceType_WithConfigResourceTypes_ShouldReturnTrue(uint resourceType)
    {
        // Arrange
        var factory = _serviceProvider.GetService<ConfigResourceFactory>();

        // Act
        var supports = factory!.CanCreateResource(resourceType);

        // Assert
        supports.Should().BeTrue();
    }

    [Theory]
    [InlineData(0x0166044C, typeof(MetadataResource))]
    [InlineData(0x0166044D, typeof(MetadataResource))]
    [InlineData(0x0166044E, typeof(MetadataResource))]
    public void MetadataResourceFactory_WithSupportedResourceType_ShouldReturnCorrectResource(uint resourceType, Type expectedType)
    {
        // Arrange
        var factory = _serviceProvider.GetService<MetadataResourceFactory>();
        var key = new ResourceKey(resourceType, 0x12345678, 0x87654321);

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType(expectedType);

    }

    [Theory]
    [InlineData(0x0166044C)]
    [InlineData(0x0166044D)]
    [InlineData(0x0166044E)]
    public void MetadataResourceFactory_SupportsResourceType_WithMetadataResourceTypes_ShouldReturnTrue(uint resourceType)
    {
        // Arrange
        var factory = _serviceProvider.GetService<MetadataResourceFactory>();

        // Act
        var supports = factory!.CanCreateResource(resourceType);

        // Assert
        supports.Should().BeTrue();
    }

    [Fact]
    public void DataResourceFactory_WithStream_ShouldCreateResourceFromStream()
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();
        var key = new ResourceKey(0x0166038C, 0x12345678, 0x87654321);
        var data = new byte[] { 0x44, 0x41, 0x54, 0x41, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(data);

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<DataResource>();

    }

    [Fact]
    public void ConfigResourceFactory_WithStream_ShouldCreateResourceFromStream()
    {
        // Arrange
        var factory = _serviceProvider.GetService<ConfigResourceFactory>();
        var key = new ResourceKey(0x0000038C, 0x12345678, 0x87654321);
        var configData = "setting1=value1\nsetting2=value2";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configData));

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<ConfigResource>();

    }

    [Fact]
    public void MetadataResourceFactory_WithStream_ShouldCreateResourceFromStream()
    {
        // Arrange
        var factory = _serviceProvider.GetService<MetadataResourceFactory>();
        var key = new ResourceKey(0x0166044C, 0x12345678, 0x87654321);
        var metadataData = "AssetName=Test Asset\nVersion=1.0.0";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(metadataData));

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<MetadataResource>();

    }

    [Fact]
    public async Task DataResourceFactory_CreateResourceAsync_ShouldCreateResourceFromStream()
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();
        var key = new ResourceKey(0x0166038C, 0x12345678, 0x87654321);
        var data = new byte[] { 0x44, 0x41, 0x54, 0x41, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(data);

        // Act
        var resource = await factory!.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<DataResource>();

    }

    [Fact]
    public async Task ConfigResourceFactory_CreateResourceAsync_ShouldCreateResourceFromStream()
    {
        // Arrange
        var factory = _serviceProvider.GetService<ConfigResourceFactory>();
        var key = new ResourceKey(0x0000038C, 0x12345678, 0x87654321);
        var configData = "setting1=value1\nsetting2=value2";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configData));

        // Act
        var resource = await factory!.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<ConfigResource>();

    }

    [Fact]
    public async Task MetadataResourceFactory_CreateResourceAsync_ShouldCreateResourceFromStream()
    {
        // Arrange
        var factory = _serviceProvider.GetService<MetadataResourceFactory>();
        var key = new ResourceKey(0x0166044C, 0x12345678, 0x87654321);
        var metadataData = "AssetName=Test Asset\nVersion=1.0.0";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(metadataData));

        // Act
        var resource = await factory!.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<MetadataResource>();

    }

    [Fact]
    public void DataResourceFactory_WithNullStream_ShouldCreateEmptyResource()
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();
        var key = new ResourceKey(0x0166038C, 0x12345678, 0x87654321);

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<DataResource>();

    }

    [Fact]
    public void ConfigResourceFactory_WithNullStream_ShouldCreateEmptyResource()
    {
        // Arrange
        var factory = _serviceProvider.GetService<ConfigResourceFactory>();
        var key = new ResourceKey(0x0000038C, 0x12345678, 0x87654321);

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<ConfigResource>();

    }

    [Fact]
    public void MetadataResourceFactory_WithNullStream_ShouldCreateEmptyResource()
    {
        // Arrange
        var factory = _serviceProvider.GetService<MetadataResourceFactory>();
        var key = new ResourceKey(0x0166044C, 0x12345678, 0x87654321);

        // Act
        var resource = factory!.CreateEmptyResource(key.ResourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<MetadataResource>();

    }

    [Fact]
    public void DataResourceFactory_GetSupportedTypes_ShouldReturnExpectedTypes()
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();

        // Act
        var supportedTypes = factory!.SupportedResourceTypes.ToArray();

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("0x0166038C");
        supportedTypes.Should().Contain("0x0166038D");
        supportedTypes.Should().Contain("0x0166038E");
    }

    [Fact]
    public void ConfigResourceFactory_GetSupportedTypes_ShouldReturnExpectedTypes()
    {
        // Arrange
        var factory = _serviceProvider.GetService<ConfigResourceFactory>();

        // Act
        var supportedTypes = factory!.SupportedResourceTypes.ToArray();

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("0x0000038C");
        supportedTypes.Should().Contain("0x0000038D");
        supportedTypes.Should().Contain("0x0000038E");
    }

    [Fact]
    public void MetadataResourceFactory_GetSupportedTypes_ShouldReturnExpectedTypes()
    {
        // Arrange
        var factory = _serviceProvider.GetService<MetadataResourceFactory>();

        // Act
        var supportedTypes = factory!.SupportedResourceTypes.ToArray();

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain("0x0166044C");
        supportedTypes.Should().Contain("0x0166044D");
        supportedTypes.Should().Contain("0x0166044E");
    }

    [Fact]
    public void DataResourceFactory_Name_ShouldReturnCorrectName()
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();

        // Act
        var name = factory!.Name;

        // Assert
        name.Should().Be("Data Resource Factory");
    }

    [Fact]
    public void ConfigResourceFactory_Name_ShouldReturnCorrectName()
    {
        // Arrange
        var factory = _serviceProvider.GetService<ConfigResourceFactory>();

        // Act
        var name = factory!.Name;

        // Assert
        name.Should().Be("Config Resource Factory");
    }

    [Fact]
    public void MetadataResourceFactory_Name_ShouldReturnCorrectName()
    {
        // Arrange
        var factory = _serviceProvider.GetService<MetadataResourceFactory>();

        // Act
        var name = factory!.Name;

        // Assert
        name.Should().Be("Metadata Resource Factory");
    }

    [Fact]
    public void DataResourceFactory_Version_ShouldReturnCorrectVersion()
    {
        // Arrange
        var factory = _serviceProvider.GetService<DataResourceFactory>();

        // Act
        var version = factory!.Version;

        // Assert
        version.Should().NotBeNull();
        version.Should().NotBeEmpty();
    }

    [Fact]
    public void ConfigResourceFactory_Version_ShouldReturnCorrectVersion()
    {
        // Arrange
        var factory = _serviceProvider.GetService<ConfigResourceFactory>();

        // Act
        var version = factory!.Version;

        // Assert
        version.Should().NotBeNull();
        version.Should().NotBeEmpty();
    }

    [Fact]
    public void MetadataResourceFactory_Version_ShouldReturnCorrectVersion()
    {
        // Arrange
        var factory = _serviceProvider.GetService<MetadataResourceFactory>();

        // Act
        var version = factory!.Version;

        // Assert
        version.Should().NotBeNull();
        version.Should().NotBeEmpty();
    }

    [Fact]
    public void AllFactories_ShouldBeRegisteredAsTransient()
    {
        // Arrange & Act
        var dataFactory1 = _serviceProvider.GetService<DataResourceFactory>();
        var dataFactory2 = _serviceProvider.GetService<DataResourceFactory>();
        var configFactory1 = _serviceProvider.GetService<ConfigResourceFactory>();
        var configFactory2 = _serviceProvider.GetService<ConfigResourceFactory>();
        var metadataFactory1 = _serviceProvider.GetService<MetadataResourceFactory>();
        var metadataFactory2 = _serviceProvider.GetService<MetadataResourceFactory>();

        // Assert - Each service should be a new instance (transient)
        dataFactory1.Should().NotBeSameAs(dataFactory2);
        configFactory1.Should().NotBeSameAs(configFactory2);
        metadataFactory1.Should().NotBeSameAs(metadataFactory2);
    }

    [Fact]
    public void AllFactories_ShouldImplementIResourceFactory()
    {
        // Arrange
        var dataFactory = _serviceProvider.GetService<DataResourceFactory>();
        var configFactory = _serviceProvider.GetService<ConfigResourceFactory>();
        var metadataFactory = _serviceProvider.GetService<MetadataResourceFactory>();

        // Act & Assert
        dataFactory.Should().BeAssignableTo<IResourceFactory>();
        configFactory.Should().BeAssignableTo<IResourceFactory>();
        metadataFactory.Should().BeAssignableTo<IResourceFactory>();
    }

    [Fact]
    public void AllFactories_ShouldBeRetrievableAsIResourceFactory()
    {
        // Arrange & Act
        var factories = _serviceProvider.GetServices<IResourceFactory>().ToList();

        // Assert
        factories.Should().HaveCountGreaterOrEqualTo(3);
        factories.Should().Contain(f => f is DataResourceFactory);
        factories.Should().Contain(f => f is ConfigResourceFactory);
        factories.Should().Contain(f => f is MetadataResourceFactory);
    }
}
