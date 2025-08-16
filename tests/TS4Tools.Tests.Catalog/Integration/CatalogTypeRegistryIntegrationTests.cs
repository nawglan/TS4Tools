using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using TS4Tools.Resources.Catalog;
using TS4Tools.Core.Resources;

namespace TS4Tools.Tests.Catalog.Integration;

/// <summary>
/// Integration tests for catalog type registry and cross-catalog compatibility.
/// Validates the automatic discovery and factory registration system.
/// </summary>
public sealed class CatalogTypeRegistryIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly CatalogTypeRegistry _registry;
    private readonly ILogger<CatalogTypeRegistryIntegrationTests> _logger;

    public CatalogTypeRegistryIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddCatalogResources();

        _serviceProvider = services.BuildServiceProvider();
        _registry = _serviceProvider.GetRequiredService<CatalogTypeRegistry>();
        _logger = _serviceProvider.GetRequiredService<ILogger<CatalogTypeRegistryIntegrationTests>>();
    }

    [Fact]
    public async Task DiscoverTypesFromCurrentDomainAsync_ShouldFindCatalogResources()
    {
        // Act
        await _registry.DiscoverTypesFromCurrentDomainAsync();

        // Assert
        var registeredTypes = _registry.RegisteredTypes;
        Assert.NotEmpty(registeredTypes);

        // Verify specific catalog types are found
        var catalogTagType = registeredTypes.FirstOrDefault(t => t.ResourceType == typeof(CatalogTagResource));
        Assert.NotNull(catalogTagType);
        Assert.Equal(CatalogType.Custom, catalogTagType.CatalogType);
        Assert.Contains(0xCAAAD4B0u, catalogTagType.SupportedResourceTypes);

        var objectCatalogType = registeredTypes.FirstOrDefault(t => t.ResourceType == typeof(ObjectCatalogResource));
        Assert.NotNull(objectCatalogType);
        Assert.Equal(CatalogType.Object, objectCatalogType.CatalogType);
        Assert.Contains(0x319E4F1Du, objectCatalogType.SupportedResourceTypes);
    }

    [Fact]
    public void GetTypeInfo_WithValidResourceType_ShouldReturnCorrectInfo()
    {
        // Arrange
        var typeInfo = new CatalogTypeInfo(
            typeof(CatalogTagResource),
            [0xCAAAD4B0],
            CatalogType.Custom,
            200,
            "Test Catalog Tag Resource");

        _registry.RegisterType(typeInfo);

        // Act
        var result = _registry.GetTypeInfo(0xCAAAD4B0);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(CatalogTagResource), result.ResourceType);
        Assert.Equal(CatalogType.Custom, result.CatalogType);
        Assert.Equal(200, result.Priority);
    }

    [Fact]
    public void GetTypeInfo_WithInvalidResourceType_ShouldReturnNull()
    {
        // Act
        var result = _registry.GetTypeInfo(0xDEADBEEF);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsSupported_WithRegisteredType_ShouldReturnTrue()
    {
        // Arrange
        var typeInfo = new CatalogTypeInfo(
            typeof(CatalogTagResource),
            [0xCAAAD4B0],
            CatalogType.Custom,
            200,
            "Test Catalog Tag Resource");

        _registry.RegisterType(typeInfo);

        // Act & Assert
        Assert.True(_registry.IsSupported(0xCAAAD4B0));
        Assert.False(_registry.IsSupported(0xDEADBEEF));
    }

    [Fact]
    public void RegisterFactory_ShouldMaintainPriorityOrder()
    {
        // Arrange
        var factory1 = new TestResourceFactory("Factory1", 100);
        var factory2 = new TestResourceFactory("Factory2", 200);
        var factory3 = new TestResourceFactory("Factory3", 150);

        // Act
        _registry.RegisterFactory(factory1);
        _registry.RegisterFactory(factory2);
        _registry.RegisterFactory(factory3);

        // Assert
        var factories = _registry.RegisteredFactories.ToList();
        Assert.Equal(3, factories.Count);

        // Should be ordered by priority (highest first)
        Assert.Equal(200, factories[0].Priority);
        Assert.Equal(150, factories[1].Priority);
        Assert.Equal(100, factories[2].Priority);
    }

    [Theory]
    [InlineData(0xCAAAD4B0)] // Catalog Tag Resource
    [InlineData(0x319E4F1D)] // Object Catalog Resource
    public async Task CrossResourceCompatibility_ShouldLoadCorrectResourceType(uint resourceTypeId)
    {
        // Arrange
        await _registry.DiscoverTypesFromCurrentDomainAsync();

        // Act
        var typeInfo = _registry.GetTypeInfo(resourceTypeId);
        var factory = _registry.GetFactory(resourceTypeId);

        // Assert
        Assert.NotNull(typeInfo);
        // Factory may be null since we're not testing with actual factory registration
        // but type info should be found from discovery

        var isSupported = _registry.IsSupported(resourceTypeId);
        Assert.True(isSupported);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// Test implementation of IResourceFactory for testing priority ordering.
    /// </summary>
    private sealed class TestResourceFactory : IResourceFactory
    {
        public TestResourceFactory(string name, int priority)
        {
            Name = name;
            Priority = priority;
            SupportedResourceTypes = new HashSet<string> { "TEST" };
        }

        public string Name { get; }
        public int Priority { get; }
        public IReadOnlySet<string> SupportedResourceTypes { get; }

        public Task<TS4Tools.Core.Interfaces.IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("This is a test factory");
        }
    }
}
