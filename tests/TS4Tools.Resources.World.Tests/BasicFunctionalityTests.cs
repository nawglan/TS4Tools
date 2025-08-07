using Xunit;
using TS4Tools.Resources.World;
using TS4Tools.Core.Package;
using FluentAssertions;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Basic functionality tests for World Building Wrapper resources to verify Phase 4.9 implementation.
/// These tests validate that all core resource types can be instantiated and basic operations work.
/// </summary>
public class BasicFunctionalityTests
{
    [Fact]
    public void WorldResource_CanBeCreated_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0, 1);

        // Act
        var resource = new WorldResource(key, 1);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1u);
        resource.ObjectManagers.Should().NotBeNull().And.BeEmpty();
        resource.SceneObjects.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void TerrainResource_CanBeCreated_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0, 1);

        // Act
        var resource = new TerrainResource(key, 1);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1u);
        resource.Width.Should().Be(0);
        resource.Height.Should().Be(0);
        resource.TerrainGeometryType.Should().Be(TerrainGeometryType.Quad);
    }

    [Fact]
    public void LotResource_CanBeCreated_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0, 1);

        // Act
        var resource = new LotResource(key, 1);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1u);
        resource.LotType.Should().Be(LotType.Residential);
        resource.LotPrice.Should().Be(0);
    }

    [Fact]
    public void NeighborhoodResource_CanBeCreated_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);

        // Act
        var resource = new NeighborhoodResource(key, 1);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1u);
        resource.Climate.Should().Be(ClimateType.Temperate);
        resource.Terrain.Should().Be(TerrainType.Flat);
        resource.Lots.Should().NotBeNull().And.BeEmpty();
        resource.LocalizedNames.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void AllResourceFactories_CanBeCreated_Successfully()
    {
        // Act & Assert
        var worldFactory = new WorldResourceFactory();
        worldFactory.Should().NotBeNull();

        var terrainFactory = new TerrainResourceFactory();
        terrainFactory.Should().NotBeNull();

        var lotFactory = new LotResourceFactory();
        lotFactory.Should().NotBeNull();

        var neighborhoodFactory = new NeighborhoodResourceFactory();
        neighborhoodFactory.Should().NotBeNull();
    }

    [Theory]
    [InlineData(TerrainType.Flat)]
    [InlineData(TerrainType.Desert)]
    [InlineData(TerrainType.Grassland)]
    [InlineData(TerrainType.Forest)]
    [InlineData(TerrainType.Beach)]
    [InlineData(TerrainType.Urban)]
    [InlineData(TerrainType.Mountain)]
    public void TerrainType_EnumValues_AreSupported(TerrainType terrainType)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);
        var resource = new NeighborhoodResource(key, 1);

        // Act & Assert
        resource.Terrain = terrainType;
        resource.Terrain.Should().Be(terrainType);
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

    [Fact]
    public void WorldResource_ObjectManager_CanBeAdded()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0, 1);
        var resource = new WorldResource(key, 1);
        var manager = new ObjectManager { Id = 1, Name = "TestManager", Position = new Vector3(1, 2, 3) };

        // Act
        resource.AddObjectManager(manager);

        // Assert
        resource.ObjectManagers.Should().HaveCount(1);
        resource.ObjectManagers[0].Should().Be(manager);
    }

    [Fact]
    public void NeighborhoodResource_AddLot_WorksCorrectly()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);
        var resource = new NeighborhoodResource(key, 1);
        var lot = new NeighborhoodLot { LotId = 123, PositionX = 10, PositionY = 20 };

        // Act
        resource.AddLot(lot);

        // Assert
        resource.Lots.Should().HaveCount(1);
        resource.Lots[0].Should().Be(lot);
    }
}
