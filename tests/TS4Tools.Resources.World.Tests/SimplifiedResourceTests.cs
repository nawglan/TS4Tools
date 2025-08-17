using FluentAssertions;
using TS4Tools.Core.Package;
using TS4Tools.Resources.World;
using Xunit;

namespace TS4Tools.Resources.World.Tests;

public class SimplifiedResourceTests
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
        resource.IsEditable.Should().BeTrue();
        resource.ObjectManagers.Should().BeEmpty();
        resource.SceneObjects.Should().BeEmpty();
    }

    [Fact]
    public void WorldResource_CanAddObjectManager_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0, 1);
        var resource = new WorldResource(key, 1);
        var manager = new ObjectManager(12345UL, "TestManager");

        // Act
        resource.AddObjectManager(manager);

        // Assert
        resource.ObjectManagers.Should().HaveCount(1);
        resource.ObjectManagers[0].Should().Be(manager);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void WorldResource_CanRemoveObjectManager_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0, 1);
        var resource = new WorldResource(key, 1);
        var manager = new ObjectManager(12345UL, "TestManager");
        resource.AddObjectManager(manager);

        // Act
        var result = resource.RemoveObjectManager(manager);

        // Assert
        result.Should().BeTrue();
        resource.ObjectManagers.Should().BeEmpty();
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
        resource.LayerIndexCount.Should().Be(0u);
        resource.Vertices.Should().BeEmpty();
        resource.Passes.Should().BeEmpty();
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
        resource.IsEditable.Should().BeTrue();
        resource.LotId.Should().Be(0u);
        resource.SimoleonPrice.Should().Be(0u);
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
        resource.WorldName.Should().Be(string.Empty);
        resource.Lots.Should().BeEmpty();
        resource.LocalizedNames.Should().BeEmpty();
    }

    [Fact]
    public void NeighborhoodResource_CanSetClimateAndTerrain_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);
        var resource = new NeighborhoodResource(key, 1);

        // Act
        resource.Climate = ClimateType.Desert;
        resource.Terrain = TerrainType.Desert;

        // Assert
        resource.Climate.Should().Be(ClimateType.Desert);
        resource.Terrain.Should().Be(TerrainType.Desert);
    }

    [Fact]
    public void NeighborhoodResource_CanAddLot_Successfully()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0, 1);
        var resource = new NeighborhoodResource(key, 1);
        var lot = new NeighborhoodLot
        {
            LotId = 1,
            InstanceId = 12345UL,
            X = 10.0f,
            Y = 0.0f,
            Z = 20.0f,
            Rotation = 90.0f
        };

        // Act
        resource.AddLot(lot);

        // Assert
        resource.Lots.Should().HaveCount(1);
        resource.Lots[0].Should().Be(lot);
    }
}
