/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
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

#pragma warning disable CA1305 // Specify IFormatProvider
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
#pragma warning disable CA2254 // Template should be a static expression

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.World;
using TS4Tools.Resources.World.DependencyInjection;
using Xunit;

namespace TS4Tools.Tests.GoldenMaster;

/// <summary>
/// Phase 4.17 specific Golden Master tests for World and Environment resources.
/// Tests the Critical Foundation requirements including factory registration,
/// DI integration, and resource creation validation.
/// </summary>
[Collection("GoldenMaster")]
public sealed class Phase417WorldResourceGoldenMasterTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IResourceManager _resourceManager;
    private readonly ILogger<Phase417WorldResourceGoldenMasterTests> _logger;

    public Phase417WorldResourceGoldenMasterTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddTS4ToolsResourceServices();
        services.AddWorldResources();

        _serviceProvider = services.BuildServiceProvider();

        // Initialize ResourceWrapperRegistry to discover and register factories with ResourceManager
        var registry = _serviceProvider.GetRequiredService<IResourceWrapperRegistry>();
        var initTask = registry.DiscoverAndRegisterFactoriesAsync();
        initTask.GetAwaiter().GetResult(); // Synchronously wait since constructors can't be async

        _resourceManager = _serviceProvider.GetRequiredService<IResourceManager>();
        _logger = _serviceProvider.GetRequiredService<ILogger<Phase417WorldResourceGoldenMasterTests>>();
    }

    /// <summary>
    /// Phase 4.17.0 - Critical Foundation: Assembly Loading Validation
    /// Verifies that all World resource factories are properly registered and accessible.
    /// </summary>
    [Fact]
    public void WorldResourceFactories_ShouldBeRegisteredInDI()
    {
        // Act & Assert - All factories should be resolvable from DI
        var worldFactory = _serviceProvider.GetService<WorldResourceFactory>();
        var terrainFactory = _serviceProvider.GetService<TerrainResourceFactory>();
        var lotFactory = _serviceProvider.GetService<LotResourceFactory>();
        var neighborhoodFactory = _serviceProvider.GetService<NeighborhoodResourceFactory>();
        var lotDescFactory = _serviceProvider.GetService<LotDescriptionResourceFactory>();
        var regionDescFactory = _serviceProvider.GetService<RegionDescriptionResourceFactory>();

        worldFactory.Should().NotBeNull("WorldResourceFactory should be registered in DI");
        terrainFactory.Should().NotBeNull("TerrainResourceFactory should be registered in DI");
        lotFactory.Should().NotBeNull("LotResourceFactory should be registered in DI");
        neighborhoodFactory.Should().NotBeNull("NeighborhoodResourceFactory should be registered in DI");
        lotDescFactory.Should().NotBeNull("LotDescriptionResourceFactory should be registered in DI");
        regionDescFactory.Should().NotBeNull("RegionDescriptionResourceFactory should be registered in DI");
    }

    /// <summary>
    /// Phase 4.17.0 - Critical Foundation: Factory Registration
    /// Verifies that world resources can be created through the resource manager.
    /// </summary>
    [Theory]
    [InlineData("0x810A102D", "World Resource")]
    [InlineData("0xAE39399F", "Terrain Resource")]
    [InlineData("0x01942E2C", "Lot Resource")]
    [InlineData("0xD65DAFF9", "Neighborhood Resource")]
    [InlineData("0xA680EA4B", "Region Description Resource")]
    [InlineData("0xC9C81B9B", "Lot Description Resource")]
    [InlineData("0x39006E00", "Region Description Resource")]
    public async Task ResourceManager_CanCreateWorldResources(string resourceTypeId, string description)
    {
        // Act
        var resource = await _resourceManager.CreateResourceAsync(resourceTypeId, 1);

        // Assert
        resource.Should().NotBeNull($"{description} should be creatable via ResourceManager");
        resource!.RequestedApiVersion.Should().BeGreaterThan(0, "Resource should have valid API version");
        resource.ContentFields.Should().NotBeEmpty("Resource should have content fields");

        _logger.LogInformation("Successfully created {ResourceType}: {Description}", resourceTypeId, description);
    }

    /// <summary>
    /// Phase 4.17.0 - Critical Foundation: Resource Type Validation
    /// Verifies that world resources have correct resource keys and properties.
    /// </summary>
    [Theory]
    [InlineData(typeof(WorldResource), 0x810A102DU)]
    [InlineData(typeof(TerrainResource), 0xAE39399FU)]
    [InlineData(typeof(LotResource), 0x01942E2CU)]
    [InlineData(typeof(NeighborhoodResource), 0xA680EA4BU)] // Default to region desc type
    public async Task WorldResource_ShouldHaveCorrectResourceKey(Type resourceType, uint expectedResourceType)
    {
        // Arrange
        var factory = _serviceProvider.GetServices<IResourceFactory>()
            .FirstOrDefault(f => f.GetType().Name.StartsWith(resourceType.Name.Replace("Resource", "ResourceFactory"), StringComparison.Ordinal));

        factory.Should().NotBeNull($"Factory for {resourceType.Name} should be registered");

        // Act
        var resource = await factory!.CreateResourceAsync(1);

        // Assert
        resource.Should().NotBeNull("Resource should be created successfully");
        resource.Should().BeOfType(resourceType, $"Factory should create resource of type {resourceType.Name}");

        // Cast to specific type to access properties
        if (resource is WorldResource worldRes)
        {
            worldRes.Key.ResourceType.Should().Be(expectedResourceType,
                $"{resourceType.Name} should have resource type 0x{expectedResourceType:X8}");
        }
        else if (resource is TerrainResource terrainRes)
        {
            terrainRes.Key.ResourceType.Should().Be(expectedResourceType,
                $"{resourceType.Name} should have resource type 0x{expectedResourceType:X8}");
        }
        else if (resource is LotResource lotRes)
        {
            lotRes.Key.ResourceType.Should().Be(expectedResourceType,
                $"{resourceType.Name} should have resource type 0x{expectedResourceType:X8}");
        }
        else if (resource is NeighborhoodResource neighborhoodRes)
        {
            neighborhoodRes.Key.ResourceType.Should().Be(expectedResourceType,
                $"{resourceType.Name} should have resource type 0x{expectedResourceType:X8}");
        }
    }

    /// <summary>
    /// Phase 4.17.0 - Critical Foundation: Byte-Perfect Validation
    /// Verifies that world resources can perform round-trip serialization without data loss.
    /// </summary>
    [Theory]
    [InlineData(typeof(WorldResourceFactory))]
    [InlineData(typeof(TerrainResourceFactory))]
    [InlineData(typeof(LotResourceFactory))]
    [InlineData(typeof(NeighborhoodResourceFactory))]
    [InlineData(typeof(LotDescriptionResourceFactory))]
    [InlineData(typeof(RegionDescriptionResourceFactory))]
    public async Task WorldResourceFactory_RoundTripSerialization_ShouldPreserveData(Type factoryType)
    {
        // Arrange
        var factory = _serviceProvider.GetService(factoryType) as IResourceFactory;
        factory.Should().NotBeNull($"{factoryType.Name} should be registered in DI");

        // Act
        var originalResource = await factory!.CreateResourceAsync(1);
        originalResource.Should().NotBeNull("Resource should be created successfully");

        // Serialize to bytes
        var originalBytes = originalResource!.AsBytes;
        originalBytes.Should().NotBeEmpty("Resource should serialize to non-empty byte array");

        // Deserialize back
        using var stream = new MemoryStream(originalBytes);
        var deserializedResource = await factory.CreateResourceAsync(1, stream);

        // Assert
        deserializedResource.Should().NotBeNull("Resource should deserialize successfully");
        deserializedResource.Should().BeOfType(originalResource.GetType(), "Deserialized resource should be same type");

        var deserializedBytes = deserializedResource.AsBytes;
        deserializedBytes.Should().Equal(originalBytes,
            $"{factoryType.Name} should preserve byte-perfect round-trip serialization");

        _logger.LogInformation("Round-trip serialization validated for {FactoryType}", factoryType.Name);
    }

    /// <summary>
    /// Phase 4.17.0 - Critical Foundation: Performance Baseline
    /// Establishes baseline performance metrics for world resource creation.
    /// </summary>
    [Fact]
    public async Task WorldResourceCreation_PerformanceBaseline()
    {
        // Arrange
        const int iterations = 100;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Create resources multiple times to measure performance
        for (int i = 0; i < iterations; i++)
        {
            var worldResource = await _resourceManager.CreateResourceAsync("0x810A102D", 1);
            var terrainResource = await _resourceManager.CreateResourceAsync("0xAE39399F", 1);
            var lotResource = await _resourceManager.CreateResourceAsync("0x01942E2C", 1);

            // Dispose resources to avoid memory leaks in test
            (worldResource as IDisposable)?.Dispose();
            (terrainResource as IDisposable)?.Dispose();
            (lotResource as IDisposable)?.Dispose();
        }

        stopwatch.Stop();

        // Assert - Performance should be reasonable
        var totalMs = stopwatch.ElapsedMilliseconds;
        var avgMsPerResource = totalMs / (double)(iterations * 3);

        _logger.LogInformation("Performance baseline: {TotalMs}ms for {Operations} operations, avg {AvgMs:F2}ms per resource",
            totalMs, iterations * 3, avgMsPerResource);

        // Performance gate: Each resource creation should be under 10ms on average
        avgMsPerResource.Should().BeLessThan(10.0,
            "World resource creation should be performant (< 10ms per resource on average)");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
