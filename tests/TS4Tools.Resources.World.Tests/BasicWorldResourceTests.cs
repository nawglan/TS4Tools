using FluentAssertions;
using TS4Tools.Core.Package;
using TS4Tools.Resources.World;
using Xunit;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Basic tests for Phase 4.9 World Building Resources implementation
/// </summary>
public sealed class BasicWorldResourceTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    [Fact]
    public void WorldResource_Creation_ShouldWork()
    {
        // Arrange
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);

        // Act
        var resource = new WorldResource(key, 1);
        _disposables.Add(resource);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1U);
    }

    [Fact]
    public void TerrainResource_Creation_ShouldWork()
    {
        // Arrange
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);

        // Act
        var resource = new TerrainResource(key, 1);
        _disposables.Add(resource);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1U);
    }

    [Fact]
    public void LotResource_Creation_ShouldWork()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);

        // Act
        var resource = new LotResource(key, 9);
        _disposables.Add(resource);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(9U);
    }

    [Fact]
    public void NeighborhoodResource_Creation_ShouldWork()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);

        // Act
        var resource = new NeighborhoodResource(key, 1);
        _disposables.Add(resource);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(1U);
    }

    [Fact]
    public void WorldResourceFactory_Creation_ShouldWork()
    {
        // Arrange & Act
        var factory = new WorldResourceFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.CanCreateResource(0x810A102D).Should().BeTrue();
        factory.ResourceTypes.Should().Contain(0x810A102D);
    }

    [Fact]
    public void TerrainResourceFactory_Creation_ShouldWork()
    {
        // Arrange & Act
        var factory = new TerrainResourceFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.CanCreateResource(0xAE39399F).Should().BeTrue();
        factory.ResourceTypes.Should().Contain(0xAE39399F);
    }

    [Fact]
    public void LotResourceFactory_Creation_ShouldWork()
    {
        // Arrange & Act
        var factory = new LotResourceFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.CanCreateResource(0x01942E2C).Should().BeTrue();
        factory.ResourceTypes.Should().Contain(0x01942E2C);
    }

    [Fact]
    public void NeighborhoodResourceFactory_Creation_ShouldWork()
    {
        // Arrange & Act
        var factory = new NeighborhoodResourceFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.CanCreateResource(0xA680EA4B).Should().BeTrue();
        factory.ResourceTypes.Should().Contain(0xA680EA4B);
    }

    [Fact]
    public async Task AllFactories_CreateResourceAsync_ShouldWork()
    {
        // Arrange
        var worldFactory = new WorldResourceFactory();
        var terrainFactory = new TerrainResourceFactory();
        var lotFactory = new LotResourceFactory();
        var neighborhoodFactory = new NeighborhoodResourceFactory();

        // Act & Assert
        var worldResource = await worldFactory.CreateResourceAsync(1, null);
        _disposables.Add(worldResource);
        worldResource.Should().BeOfType<WorldResource>();

        var terrainResource = await terrainFactory.CreateResourceAsync(1, null);
        _disposables.Add(terrainResource);
        terrainResource.Should().BeOfType<TerrainResource>();

        var lotResource = await lotFactory.CreateResourceAsync(1, null);
        _disposables.Add(lotResource);
        lotResource.Should().BeOfType<LotResource>();

        var neighborhoodResource = await neighborhoodFactory.CreateResourceAsync(1, null);
        _disposables.Add(neighborhoodResource);
        neighborhoodResource.Should().BeOfType<NeighborhoodResource>();
    }

    [Fact]
    public void AllResources_ImplementIResourceInterface()
    {
        // Arrange
        var worldKey = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var terrainKey = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var lotKey = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var neighborhoodKey = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);

        // Act
        var worldResource = new WorldResource(worldKey, 1);
        var terrainResource = new TerrainResource(terrainKey, 1);
        var lotResource = new LotResource(lotKey, 9);
        var neighborhoodResource = new NeighborhoodResource(neighborhoodKey, 1);

        _disposables.AddRange(new[] { worldResource, terrainResource, lotResource, neighborhoodResource });

        // Assert
        worldResource.Should().BeAssignableTo<IResource>();
        terrainResource.Should().BeAssignableTo<IResource>();
        lotResource.Should().BeAssignableTo<IResource>();
        neighborhoodResource.Should().BeAssignableTo<IResource>();

        // Test IResource properties
        worldResource.Stream.Should().NotBeNull();
        worldResource.AsBytes.Should().NotBeNull();
        worldResource.ContentFields.Should().NotBeEmpty();

        terrainResource.Stream.Should().NotBeNull();
        terrainResource.AsBytes.Should().NotBeNull();
        terrainResource.ContentFields.Should().NotBeEmpty();

        lotResource.Stream.Should().NotBeNull();
        lotResource.AsBytes.Should().NotBeNull();
        lotResource.ContentFields.Should().NotBeEmpty();

        neighborhoodResource.Stream.Should().NotBeNull();
        neighborhoodResource.AsBytes.Should().NotBeNull();
        neighborhoodResource.ContentFields.Should().NotBeEmpty();
    }
}
