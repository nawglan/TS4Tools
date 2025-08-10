/***************************************************************************
 *  Copyright (C        var resourceTypes = new[]
        {
            "0x220557DA", // StringTable
            "0x00B2D882", // DDS Image  
            "PNG",        // PNG Image
            "0x2F7D0004", // TGA Image
            "0x319E4F1D", // Catalog
            "0x03B33DDF"  // Text (valid TextResource type)
        };4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Resources.Catalog;
using TS4Tools.Resources.Images;
using TS4Tools.Resources.Strings;
using TS4Tools.Resources.Text;

namespace TS4Tools.Core.Resources.Tests;

/// <summary>
/// Integration tests that verify all resource wrappers work together correctly
/// through the ResourceManager and ResourceWrapperRegistry system.
/// </summary>
public sealed class Phase41IntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHost _host;
    private readonly IResourceManager _resourceManager;
    private readonly IResourceWrapperRegistry _registry;

    public Phase41IntegrationTests()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddTS4ToolsCore(new ConfigurationBuilder().Build());
            });

        _host = builder.Build();
        _serviceProvider = _host.Services;
        _resourceManager = _serviceProvider.GetRequiredService<IResourceManager>();
        _registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
    }

    [Fact]
    public async Task FullIntegration_RegistryAndResourceManager_WorkTogether()
    {
        // Arrange - Initialize the registry
        var registryResult = await _registry.DiscoverAndRegisterFactoriesAsync();

        // Assert registry setup
        registryResult.IsSuccess.Should().BeTrue();
        registryResult.SuccessfulRegistrations.Should().NotBeEmpty();

        // Act & Assert - Test each registered factory through ResourceManager

        // Test String Table Resource
        var stringResource = await _resourceManager.CreateResourceAsync("0x220557DA", 1);
        stringResource.Should().NotBeNull();
        stringResource.Should().BeOfType<StringTableResource>();

        // Test Image Resource (DDS)
        var imageResource = await _resourceManager.CreateResourceAsync("0x00B2D882", 1);
        imageResource.Should().NotBeNull();
        imageResource.Should().BeOfType<ImageResource>();

        // Test Catalog Resource
        var catalogResource = await _resourceManager.CreateResourceAsync("0x319E4F1D", 1);
        catalogResource.Should().NotBeNull();
        catalogResource.Should().BeOfType<CatalogResource>();

        // Test Text Resource
        var textResource = await _resourceManager.CreateResourceAsync("0x03B33DDF", 1);
        textResource.Should().NotBeNull();
        textResource.Should().BeOfType<TextResource>();
    }

    [Fact]
    public async Task CrossWrapperCompatibility_AllResourceTypes_CreateSuccessfully()
    {
        // Arrange
        await _registry.DiscoverAndRegisterFactoriesAsync();
        var resourceTypes = new[]
        {
            "0x220557DA", // StringTable
            "0x00B2D882", // DDS Image  
            "PNG",        // PNG Image
            "0x2F7D0004", // TGA Image
            "0x319E4F1D", // Catalog
            "0x03B33DDF"  // Text (valid TextResource type)
        };

        var createdResources = new List<Core.Interfaces.IResource>();

        // Act
        foreach (var resourceType in resourceTypes)
        {
            var resource = await _resourceManager.CreateResourceAsync(resourceType, 1);
            createdResources.Add(resource);
        }

        // Assert
        createdResources.Should().HaveCount(resourceTypes.Length);
        createdResources.Should().OnlyContain(r => r != null);

        // Verify specific types
        createdResources.Should().ContainSingle(r => r is StringTableResource);
        createdResources.Count(r => r is ImageResource).Should().BeGreaterOrEqualTo(3); // DDS, PNG, TGA
        createdResources.Should().ContainSingle(r => r is CatalogResource);
        createdResources.Should().ContainSingle(r => r is TextResource);
    }

    [Fact]
    public async Task PriorityBasedFactorySelection_HigherPriorityWins()
    {
        // Arrange
        await _registry.DiscoverAndRegisterFactoriesAsync();
        var factories = _registry.GetRegisteredFactories();

        // Act - Get factory priorities
        var catalogFactoryInfo = factories.Values
            .FirstOrDefault(f => f.FactoryName == "CatalogResourceFactory");
        var stringFactoryInfo = factories.Values
            .FirstOrDefault(f => f.FactoryName == "StringTableResourceFactory");
        var imageFactoryInfo = factories.Values
            .FirstOrDefault(f => f.FactoryName == "ImageResourceFactory");
        var textFactoryInfo = factories.Values
            .FirstOrDefault(f => f.FactoryName == "TextResourceFactory");

        // Assert priority ordering
        catalogFactoryInfo.Should().NotBeNull();
        stringFactoryInfo.Should().NotBeNull();
        imageFactoryInfo.Should().NotBeNull();
        textFactoryInfo.Should().NotBeNull();

        // Verify expected priority order: String (100) >= Image (100) > Text (50) > Catalog (10)
        stringFactoryInfo!.Priority.Should().BeGreaterOrEqualTo(imageFactoryInfo!.Priority);
        imageFactoryInfo.Priority.Should().BeGreaterThan(textFactoryInfo!.Priority);
        textFactoryInfo.Priority.Should().BeGreaterThan(catalogFactoryInfo!.Priority);
    }

    [Fact]
    public async Task ResourceTypeMapping_AllSupportedTypes_AreRecognized()
    {
        // Arrange
        await _registry.DiscoverAndRegisterFactoriesAsync();

        // Act & Assert - Test known resource types
        _registry.SupportsResourceType("0x220557DA").Should().BeTrue(); // String Table
        _registry.SupportsResourceType("0x00B2D882").Should().BeTrue(); // DDS
        _registry.SupportsResourceType("0x2F7D0004").Should().BeTrue(); // PNG
        _registry.SupportsResourceType("0x2F7D0005").Should().BeTrue(); // TGA
        _registry.SupportsResourceType("0x319E4F1D").Should().BeTrue(); // Catalog
        _registry.SupportsResourceType("0x0069453E").Should().BeTrue(); // Text (one of many text types)

        // Test unsupported type
        _registry.SupportsResourceType("0xDEADBEEF").Should().BeFalse();
    }

    [Fact]
    public async Task FactoryStatistics_AfterCreation_ShowCorrectUsage()
    {
        // Arrange
        await _registry.DiscoverAndRegisterFactoriesAsync();

        // Act - Create resources to generate usage statistics
        await _resourceManager.CreateResourceAsync("0x220557DA", 1); // String
        await _resourceManager.CreateResourceAsync("0x00B2D882", 1); // Image
        await _resourceManager.CreateResourceAsync("0x319E4F1D", 1); // Catalog

        // Get statistics
        var registryStats = _registry.GetStatistics();
        var managerStats = _resourceManager.GetStatistics();

        // Assert
        registryStats.TotalRegisteredFactories.Should().BeGreaterThan(0);
        managerStats.TotalResourcesCreated.Should().BeGreaterOrEqualTo(3);

        // Verify factory utilization is being tracked
        registryStats.FactoryUtilizationRatio.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task ErrorHandling_WithInvalidResourceType_UsesDefaultFactory()
    {
        // Arrange
        await _registry.DiscoverAndRegisterFactoriesAsync();

        // Act - Try to create resource with unsupported type
        var resource = await _resourceManager.CreateResourceAsync("0xDEADBEEF", 1);

        // Assert - Should fall back to default factory
        resource.Should().NotBeNull();
        resource.Should().BeOfType<DefaultResource>();
    }

    [Fact]
    public async Task ConcurrentResourceCreation_MultipleTypes_WorksCorrectly()
    {
        // Arrange
        await _registry.DiscoverAndRegisterFactoriesAsync();
        var resourceTypes = new[]
        {
            "0x220557DA", // String
            "0x00B2D882", // DDS
            "0x319E4F1D", // Catalog
            "0x03B33DDF"  // Text (valid TextResource type)
        };

        // Act - Create resources concurrently
        var tasks = resourceTypes.Select(async type =>
        {
            var resource = await _resourceManager.CreateResourceAsync(type, 1);
            return new { Type = type, Resource = resource };
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(resourceTypes.Length);
        results.Should().OnlyContain(r => r.Resource != null);

        // Verify each type was created correctly
        var stringResult = results.First(r => r.Type == "0x220557DA");
        stringResult.Resource.Should().BeOfType<StringTableResource>();

        var imageResult = results.First(r => r.Type == "0x00B2D882");
        imageResult.Resource.Should().BeOfType<ImageResource>();

        var catalogResult = results.First(r => r.Type == "0x319E4F1D");
        catalogResult.Resource.Should().BeOfType<CatalogResource>();

        var textResult = results.First(r => r.Type == "0x03B33DDF");
        textResult.Resource.Should().BeOfType<TextResource>();
    }

    [Fact]
    public void ServiceLifetime_Singletons_AreSameInstance()
    {
        // Arrange & Act
        var registry1 = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        var registry2 = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        var manager1 = _serviceProvider.GetRequiredService<IResourceManager>();
        var manager2 = _serviceProvider.GetRequiredService<IResourceManager>();

        // Assert - Services should be singletons
        registry1.Should().BeSameAs(registry2);
        manager1.Should().BeSameAs(manager2);
    }

    [Fact]
    public async Task Phase41Completion_AllComponents_IntegrateCorrectly()
    {
        // This test verifies that Phase 4.1 (Resource Wrapper Integration) is complete

        // Arrange & Act
        var registryResult = await _registry.DiscoverAndRegisterFactoriesAsync();
        var managerStats = _resourceManager.GetStatistics();
        var registryStats = _registry.GetStatistics();

        // Assert Phase 4.1 completion criteria

        // 1. Registry successfully discovers and registers all factories
        registryResult.IsSuccess.Should().BeTrue();
        registryResult.SuccessfulRegistrations.Should().Contain("StringTableResourceFactory");
        registryResult.SuccessfulRegistrations.Should().Contain("ImageResourceFactory");
        registryResult.SuccessfulRegistrations.Should().Contain("CatalogResourceFactory");
        registryResult.SuccessfulRegistrations.Should().Contain("TextResourceFactory");

        // 2. ResourceManager has registered factories
        managerStats.RegisteredFactories.Should().BeGreaterThan(0);

        // 3. All resource types are supported
        _registry.SupportsResourceType("0x220557DA").Should().BeTrue(); // String
        _registry.SupportsResourceType("0x00B2D882").Should().BeTrue(); // Image
        _registry.SupportsResourceType("0x319E4F1D").Should().BeTrue(); // Catalog
        _registry.SupportsResourceType("0x0069453E").Should().BeTrue(); // Text (one of many text types)

        // 4. Priority-based resolution works
        var factories = _registry.GetRegisteredFactories();
        factories.Values.Should().OnlyContain(f => f.Priority > 0);

        // 5. Performance monitoring is active
        registryStats.TotalRegisteredFactories.Should().BeGreaterThan(0);
        registryStats.FactoryUtilizationRatio.Should().BeInRange(0.0, 1.0);

        // 6. Cross-wrapper compatibility verified by creating each resource type
        var stringResource = await _resourceManager.CreateResourceAsync("0x220557DA", 1);
        var imageResource = await _resourceManager.CreateResourceAsync("0x00B2D882", 1);
        var catalogResource = await _resourceManager.CreateResourceAsync("0x319E4F1D", 1);
        var textResource = await _resourceManager.CreateResourceAsync("0x03B33DDF", 1);

        stringResource.Should().BeOfType<StringTableResource>();
        imageResource.Should().BeOfType<ImageResource>();
        catalogResource.Should().BeOfType<CatalogResource>();
        textResource.Should().BeOfType<TextResource>();
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}
