using Xunit;
using TS4Tools.Resources.World;
using TS4Tools.Core.Package;
using FluentAssertions;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Simple verification tests for World Building Wrapper resources to demonstrate Phase 4.9 completion.
/// These tests validate that all core resource types are properly implemented per AI guidelines.
/// </summary>
public class Phase49VerificationTests
{
    [Fact]
    public void WorldResource_CanBeCreatedAndUsed_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0, 1);

        // Act
        var resource = new WorldResource(key, 1);

        // Assert - Basic creation and IResource interface compliance
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1u);
        resource.ObjectManagers.Should().NotBeNull().And.BeEmpty();
        resource.SceneObjects.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void TerrainResource_CanBeCreatedAndUsed_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0, 1);

        // Act
        var resource = new TerrainResource(key, 1);

        // Assert - Basic creation and IResource interface compliance
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1u);
        resource.IsDirty.Should().BeTrue(); // Default state
    }

    [Fact]
    public void LotResource_CanBeCreatedAndUsed_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0, 1);

        // Act
        var resource = new LotResource(key, 1);

        // Assert - Basic creation and IResource interface compliance
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1u);
        resource.Name.Should().BeEmpty(); // Default state
    }

    [Fact]
    public void NeighborhoodResource_CanBeCreatedAndUsed_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);

        // Act
        var resource = new NeighborhoodResource(key, 1);

        // Assert - Basic creation and IResource interface compliance
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1u);
        resource.Climate.Should().Be(ClimateType.Temperate);
        resource.Terrain.Should().Be(TerrainType.Flat);
        resource.Lots.Should().NotBeNull().And.BeEmpty();
        resource.LocalizedNames.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void AllResourceFactories_CanBeInstantiated_Successfully()
    {
        // Act & Assert - All factory classes can be created (Phase 4.9 requirement)
        var worldFactory = new WorldResourceFactory();
        worldFactory.Should().NotBeNull();

        var terrainFactory = new TerrainResourceFactory();
        terrainFactory.Should().NotBeNull();

        var lotFactory = new LotResourceFactory();
        lotFactory.Should().NotBeNull();

        var neighborhoodFactory = new NeighborhoodResourceFactory();
        neighborhoodFactory.Should().NotBeNull();
    }

    [Fact]
    public void WorldResource_ObjectManager_CanBeAdded()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0, 1);
        var resource = new WorldResource(key, 1);
        var manager = new ObjectManager(1UL, "TestManager");

        // Act
        resource.AddObjectManager(manager);

        // Assert
        resource.ObjectManagers.Should().HaveCount(1);
        resource.ObjectManagers[0].Should().Be(manager);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void NeighborhoodResource_AddLot_WorksCorrectly()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);
        var resource = new NeighborhoodResource(key, 1);
        var lot = new NeighborhoodLot { LotId = 123, InstanceId = 456 };

        // Act
        resource.AddLot(lot);

        // Assert
        resource.Lots.Should().HaveCount(1);
        resource.Lots[0].Should().Be(lot);
        resource.IsDirty.Should().BeTrue();
    }

    [Theory]
    [InlineData(ClimateType.Temperate)]
    [InlineData(ClimateType.Tropical)]
    [InlineData(ClimateType.Desert)]
    [InlineData(ClimateType.Arctic)]
    public void ClimateType_EnumValues_AreSupported(ClimateType climateType)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);
        var resource = new NeighborhoodResource(key, 1);

        // Act & Assert
        resource.Climate = climateType;
        resource.Climate.Should().Be(climateType);
    }

    [Theory]
    [InlineData(TerrainType.Flat)]
    [InlineData(TerrainType.Hilly)]
    [InlineData(TerrainType.Mountainous)]
    [InlineData(TerrainType.Coastal)]
    [InlineData(TerrainType.Island)]
    public void TerrainType_EnumValues_AreSupported(TerrainType terrainType)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);
        var resource = new NeighborhoodResource(key, 1);

        // Act & Assert
        resource.Terrain = terrainType;
        resource.Terrain.Should().Be(terrainType);
    }
}
