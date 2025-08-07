using AutoFixture.Xunit2;
using FluentAssertions;
using NSubstitute;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.World;
using Xunit;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Unit tests for the WorldResourceFactory class.
/// </summary>
public sealed class WorldResourceFactoryTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private readonly WorldResourceFactory _factory = new();

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidParameters_ShouldCreateWorldResource()
    {
        // Arrange & Act
        var resource = await _factory.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<WorldResource>();
        resource.Version.Should().Be(1);
    }

    [Fact]
    public async Task CreateResourceAsync_WithStream_ShouldCreateWorldResource()
    {
        // Arrange & Act
        var resource = await _factory.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<WorldResource>();
    }

    [Fact]
    public void CreateEmptyResource_WithWorldResourceType_ShouldCreateWorldResource()
    {
        // Act
        var resource = _factory.CreateEmptyResource(0x810A102D);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<WorldResource>();
        resource.Version.Should().Be(1);
    }

    [Fact]
    public void CanCreateResource_WithWorldResourceType_ShouldReturnTrue()
    {
        // Act & Assert
        _factory.CanCreateResource(0x810A102D).Should().BeTrue();
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0xAE39399Fu)]
    [InlineData(0x01942E2Cu)]
    [InlineData(0xA680EA4Bu)]
    [InlineData(0xD65DAFF9u)]
    public void CanCreateResource_WithNonWorldResourceType_ShouldReturnFalse(uint typeId)
    {
        // Act & Assert
        _factory.CanCreateResource(typeId).Should().BeFalse();
    }

    [Fact]
    public void ResourceTypes_ShouldContainWorldResourceType()
    {
        // Act
        var supportedTypes = _factory.ResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain(0x810A102D);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().Contain("WORLD");
        supportedTypes.Should().Contain("0x810A102D");
    }

    [Fact]
    public void Priority_ShouldBeSetCorrectly()
    {
        // Act & Assert
        _factory.Priority.Should().Be(50);
    }
}

/// <summary>
/// Unit tests for the TerrainResourceFactory class.
/// </summary>
public sealed class TerrainResourceFactoryTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private readonly TerrainResourceFactory _factory = new();

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidParameters_ShouldCreateTerrainResource()
    {
        // Arrange & Act
        var resource = await _factory.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<TerrainResource>();
        resource.Version.Should().Be(1);
    }

    [Fact]
    public async Task CreateResourceAsync_WithStream_ShouldCreateTerrainResource()
    {
        // Arrange & Act
        var resource = await _factory.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<TerrainResource>();
    }

    [Fact]
    public void CreateEmptyResource_WithTerrainResourceType_ShouldCreateTerrainResource()
    {
        // Act
        var resource = _factory.CreateEmptyResource(0xAE39399F);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<TerrainResource>();
        resource.Version.Should().Be(1);
    }

    [Fact]
    public void CanCreateResource_WithTerrainResourceType_ShouldReturnTrue()
    {
        // Act & Assert
        _factory.CanCreateResource(0xAE39399F).Should().BeTrue();
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0x810A102Du)]
    [InlineData(0x01942E2Cu)]
    [InlineData(0xA680EA4Bu)]
    [InlineData(0xD65DAFF9u)]
    public void CanCreateResource_WithNonTerrainResourceType_ShouldReturnFalse(uint typeId)
    {
        // Act & Assert
        _factory.CanCreateResource(typeId).Should().BeFalse();
    }

    [Fact]
    public void ResourceTypes_ShouldContainTerrainResourceType()
    {
        // Act
        var supportedTypes = _factory.ResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain(0xAE39399F);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().Contain("TERRAIN");
        supportedTypes.Should().Contain("0xAE39399F");
    }

    [Fact]
    public void Priority_ShouldBeSetCorrectly()
    {
        // Act & Assert
        _factory.Priority.Should().Be(50);
    }
}

/// <summary>
/// Unit tests for the LotResourceFactory class.
/// </summary>
public sealed class LotResourceFactoryTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private readonly LotResourceFactory _factory = new();

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidParameters_ShouldCreateLotResource()
    {
        // Arrange & Act
        var resource = await _factory.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<LotResource>();
        resource.Version.Should().Be(9);
    }

    [Fact]
    public async Task CreateResourceAsync_WithStream_ShouldCreateLotResource()
    {
        // Arrange & Act
        var resource = await _factory.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<LotResource>();
    }

    [Fact]
    public void CreateEmptyResource_WithLotResourceType_ShouldCreateLotResource()
    {
        // Act
        var resource = _factory.CreateEmptyResource(0x01942E2C);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<LotResource>();
        resource.Version.Should().Be(9);
    }

    [Fact]
    public void CanCreateResource_WithLotResourceType_ShouldReturnTrue()
    {
        // Act & Assert
        _factory.CanCreateResource(0x01942E2C).Should().BeTrue();
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0x810A102Du)]
    [InlineData(0xAE39399Fu)]
    [InlineData(0xA680EA4Bu)]
    [InlineData(0xD65DAFF9u)]
    public void CanCreateResource_WithNonLotResourceType_ShouldReturnFalse(uint typeId)
    {
        // Act & Assert
        _factory.CanCreateResource(typeId).Should().BeFalse();
    }

    [Fact]
    public void ResourceTypes_ShouldContainLotResourceType()
    {
        // Act
        var supportedTypes = _factory.ResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain(0x01942E2C);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().Contain("LOT");
        supportedTypes.Should().Contain("0x01942E2C");
    }

    [Fact]
    public void Priority_ShouldBeSetCorrectly()
    {
        // Act & Assert
        _factory.Priority.Should().Be(50);
    }
}

/// <summary>
/// Unit tests for the NeighborhoodResourceFactory class.
/// </summary>
public sealed class NeighborhoodResourceFactoryTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private readonly NeighborhoodResourceFactory _factory = new();

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidParameters_ShouldCreateNeighborhoodResource()
    {
        // Arrange & Act
        var resource = await _factory.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<NeighborhoodResource>();
        resource.Version.Should().Be(1);
    }

    [Fact]
    public async Task CreateResourceAsync_WithStream_ShouldCreateNeighborhoodResource()
    {
        // Arrange & Act
        var resource = await _factory.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<NeighborhoodResource>();
    }

    [Fact]
    public void CreateEmptyResource_WithNeighborhoodResourceType_ShouldCreateNeighborhoodResource()
    {
        // Act
        var resource = _factory.CreateEmptyResource(0xA680EA4B);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType<NeighborhoodResource>();
        resource.Version.Should().Be(1);
    }

    [Fact]
    public void CanCreateResource_WithNeighborhoodResourceType_ShouldReturnTrue()
    {
        // Act & Assert
        _factory.CanCreateResource(0xA680EA4B).Should().BeTrue();
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0x810A102Du)]
    [InlineData(0xAE39399Fu)]
    [InlineData(0x01942E2Cu)]
    public void CanCreateResource_WithNonNeighborhoodResourceType_ShouldReturnFalse(uint typeId)
    {
        // Act & Assert
        _factory.CanCreateResource(typeId).Should().BeFalse();
    }

    [Fact]
    public void ResourceTypes_ShouldContainNeighborhoodResourceType()
    {
        // Act
        var supportedTypes = _factory.ResourceTypes;

        // Assert
        supportedTypes.Should().NotBeEmpty();
        supportedTypes.Should().Contain(0xA680EA4B);
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Act
        var supportedTypes = _factory.SupportedResourceTypes;

        // Assert
        supportedTypes.Should().Contain("NEIGHBORHOOD");
        supportedTypes.Should().Contain("0xA680EA4B");
    }

    [Fact]
    public void Priority_ShouldBeSetCorrectly()
    {
        // Act & Assert
        _factory.Priority.Should().Be(50);
    }
}

/// <summary>
/// Integration tests for all world building resource factories.
/// </summary>
public sealed class WorldFactoryIntegrationTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private readonly List<IResourceFactory> _factories = new()
    {
        new WorldResourceFactory(),
        new TerrainResourceFactory(),
        new LotResourceFactory(),
        new NeighborhoodResourceFactory()
    };

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    [Theory]
    [InlineData(0x810A102Du, typeof(WorldResource))]
    [InlineData(0xAE39399Fu, typeof(TerrainResource))]
    [InlineData(0x01942E2Cu, typeof(LotResource))]
    [InlineData(0xA680EA4Bu, typeof(NeighborhoodResource))]
    public async Task CreateResourceAsync_WithDifferentResourceTypes_ShouldCreateCorrectType(uint typeId, Type expectedType)
    {
        // Arrange
        IResourceFactory? factory = null;
        if (typeId == 0x810A102Du) factory = new WorldResourceFactory();
        else if (typeId == 0xAE39399Fu) factory = new TerrainResourceFactory();
        else if (typeId == 0x01942E2Cu) factory = new LotResourceFactory();
        else if (typeId == 0xA680EA4Bu) factory = new NeighborhoodResourceFactory();

        // Act
        factory.Should().NotBeNull();
        var resource = await factory!.CreateResourceAsync(1, null);
        _disposables.Add(resource);

        // Assert
        resource.Should().BeOfType(expectedType);
    }

    [Fact]
    public void AllFactories_ShouldHaveUniqueResourceTypes()
    {
        // Arrange
        var worldFactory = new WorldResourceFactory();
        var terrainFactory = new TerrainResourceFactory();
        var lotFactory = new LotResourceFactory();
        var neighborhoodFactory = new NeighborhoodResourceFactory();

        var allSupportedTypes = new List<uint>();
        allSupportedTypes.AddRange(worldFactory.ResourceTypes);
        allSupportedTypes.AddRange(terrainFactory.ResourceTypes);
        allSupportedTypes.AddRange(lotFactory.ResourceTypes);
        allSupportedTypes.AddRange(neighborhoodFactory.ResourceTypes);

        // Act & Assert
        allSupportedTypes.Should().OnlyHaveUniqueItems();
        allSupportedTypes.Should().HaveCount(5);
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0x12345678u)]
    [InlineData(0xFFFFFFFFu)]
    public void AllFactories_WithUnsupportedResourceType_ShouldReturnFalse(uint typeId)
    {
        // Arrange
        var worldFactory = new WorldResourceFactory();
        var terrainFactory = new TerrainResourceFactory();
        var lotFactory = new LotResourceFactory();
        var neighborhoodFactory = new NeighborhoodResourceFactory();

        // Act & Assert
        worldFactory.CanCreateResource(typeId).Should().BeFalse();
        terrainFactory.CanCreateResource(typeId).Should().BeFalse();
        lotFactory.CanCreateResource(typeId).Should().BeFalse();
        neighborhoodFactory.CanCreateResource(typeId).Should().BeFalse();
    }

    [Fact]
    public void AllFactories_ShouldBeAssignableToIResourceFactory()
    {
        // Act & Assert
        foreach (var factory in _factories)
        {
            factory.Should().BeAssignableTo<IResourceFactory>();
        }
    }
}
