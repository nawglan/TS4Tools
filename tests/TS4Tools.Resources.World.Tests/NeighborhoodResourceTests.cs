using AutoFixture.Xunit2;
using FluentAssertions;
using TS4Tools.Core.Package;
using TS4Tools.Resources.World;
using Xunit;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Unit tests for the NeighborhoodResource class.
/// </summary>
public sealed class NeighborhoodResourceTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

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
    [AutoData]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly(uint version)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);

        // Act
        var resource = new NeighborhoodResource(key, version);
        _disposables.Add(resource);

        // Assert
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(version);
        resource.IsDirty.Should().BeTrue();
        resource.IsEditable.Should().BeTrue();
        resource.ClimateType.Should().Be(ClimateType.Temperate);
        resource.TerrainType.Should().Be(TerrainType.Desert);
        resource.LocalizedNames.Should().BeEmpty();
        resource.LotInstanceIds.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new NeighborhoodResource(null!, 9);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Theory]
    [AutoData]
    public void Properties_SettingValues_ShouldUpdateCorrectly(
        ulong worldDescriptionInstanceId,
        uint neighborhoodId,
        bool isVacationWorld,
        bool isUniversityWorld,
        bool hasTimeOverride,
        byte timeOverrideHour,
        byte timeOverrideMinute,
        ClimateType climateType,
        TerrainType terrainType,
        bool isEditable)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act
        resource.WorldDescriptionInstanceId = worldDescriptionInstanceId;
        resource.NeighborhoodId = neighborhoodId;
        resource.IsVacationWorld = isVacationWorld;
        resource.IsUniversityWorld = isUniversityWorld;
        resource.HasTimeOverride = hasTimeOverride;
        resource.TimeOverrideHour = timeOverrideHour;
        resource.TimeOverrideMinute = timeOverrideMinute;
        resource.ClimateType = climateType;
        resource.TerrainType = terrainType;
        resource.IsEditable = isEditable;

        // Assert
        resource.WorldDescriptionInstanceId.Should().Be(worldDescriptionInstanceId);
        resource.NeighborhoodId.Should().Be(neighborhoodId);
        resource.IsVacationWorld.Should().Be(isVacationWorld);
        resource.IsUniversityWorld.Should().Be(isUniversityWorld);
        resource.HasTimeOverride.Should().Be(hasTimeOverride);
        resource.TimeOverrideHour.Should().Be(timeOverrideHour);
        resource.TimeOverrideMinute.Should().Be(timeOverrideMinute);
        resource.ClimateType.Should().Be(climateType);
        resource.TerrainType.Should().Be(terrainType);
        resource.IsEditable.Should().Be(isEditable);
    }

    [Theory]
    [InlineData("English", "Test Neighborhood")]
    [InlineData("French", "Quartier de Test")]
    [InlineData("Spanish", "Vecindario de Prueba")]
    public void AddLocalizedName_WithValidLanguageAndName_ShouldAddSuccessfully(string language, string name)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act
        resource.AddLocalizedName(language, name);

        // Assert
        resource.LocalizedNames.Should().ContainKey(language);
        resource.LocalizedNames[language].Should().Be(name);
        resource.IsDirty.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "Valid Name")]
    [InlineData("", "Valid Name")]
    [InlineData("   ", "Valid Name")]
    [InlineData("Valid Language", null)]
    [InlineData("Valid Language", "")]
    [InlineData("Valid Language", "   ")]
    public void AddLocalizedName_WithInvalidParameters_ShouldThrowArgumentException(string language, string name)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act & Assert
        Action act = () => resource.AddLocalizedName(language, name);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveLocalizedName_WithExistingLanguage_ShouldRemoveSuccessfully()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);
        resource.AddLocalizedName("English", "Test Neighborhood");

        // Act
        var result = resource.RemoveLocalizedName("English");

        // Assert
        result.Should().BeTrue();
        resource.LocalizedNames.Should().NotContainKey("English");
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RemoveLocalizedName_WithNonExistingLanguage_ShouldReturnFalse()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act
        var result = resource.RemoveLocalizedName("NonExistent");

        // Assert
        result.Should().BeFalse();
        resource.IsDirty.Should().BeTrue(); // Constructor sets IsDirty to true
    }

    [Theory]
    [AutoData]
    public void AddLot_WithValidInstanceId_ShouldAddSuccessfully(ulong instanceId)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act
        resource.AddLot(instanceId);

        // Assert
        resource.LotInstanceIds.Should().Contain(instanceId);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void AddLot_WithZeroInstanceId_ShouldThrowArgumentException()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act & Assert
        Action act = () => resource.AddLot(0);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Instance ID cannot be zero*");
    }

    [Theory]
    [AutoData]
    public void RemoveLot_WithExistingInstanceId_ShouldRemoveSuccessfully(ulong instanceId)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);
        resource.AddLot(instanceId);

        // Act
        var result = resource.RemoveLot(instanceId);

        // Assert
        result.Should().BeTrue();
        resource.LotInstanceIds.Should().NotContain(instanceId);
        resource.IsDirty.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void RemoveLot_WithNonExistingInstanceId_ShouldReturnFalse(ulong instanceId)
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act
        var result = resource.RemoveLot(instanceId);

        // Assert
        result.Should().BeFalse();
        resource.IsDirty.Should().BeTrue(); // Constructor sets IsDirty to true
    }

    [Fact]
    public void ClearAllLots_WithExistingLots_ShouldRemoveAll()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);
        resource.AddLot(12345);
        resource.AddLot(67890);
        resource.AddLot(11111);

        // Act
        resource.ClearAllLots();

        // Assert
        resource.LotInstanceIds.Should().BeEmpty();
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnEmptyErrors()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9)
        {
            NeighborhoodId = 12345,
            WorldDescriptionInstanceId = 67890,
            ClimateType = ClimateType.Tropical,
            TerrainType = TerrainType.Grassland
        };
        _disposables.Add(resource);
        resource.AddLocalizedName("English", "Test Neighborhood");

        // Act
        var errors = resource.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidConfiguration_ShouldReturnErrors()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9)
        {
            NeighborhoodId = 0,
            WorldDescriptionInstanceId = 0,
            ClimateType = (ClimateType)999, // Invalid enum value
            TerrainType = (TerrainType)999 // Invalid enum value
        };
        _disposables.Add(resource);

        // Act
        var errors = resource.Validate();

        // Assert
        errors.Should().HaveCount(4);
        errors.Should().Contain("Neighborhood ID cannot be zero");
        errors.Should().Contain("World description instance ID cannot be zero");
        errors.Should().Contain("Climate type is not valid");
        errors.Should().Contain("Terrain type is not valid");
    }

    [Theory]
    [AutoData]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.LoadFromStreamAsync(null!));
    }

    [Theory]
    [AutoData]
    public async Task SaveToStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.SaveToStreamAsync(null!));
    }

    [Theory]
    [AutoData]
    public async Task SaveToStreamAsync_WithValidStream_ShouldSaveSuccessfully(
        ulong worldDescriptionInstanceId,
        uint neighborhoodId,
        bool isVacationWorld,
        bool isUniversityWorld)
    {
        // Ensure valid configuration
        neighborhoodId = neighborhoodId == 0 ? 1 : neighborhoodId;
        worldDescriptionInstanceId = worldDescriptionInstanceId == 0 ? 1 : worldDescriptionInstanceId;

        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9)
        {
            WorldDescriptionInstanceId = worldDescriptionInstanceId,
            NeighborhoodId = neighborhoodId,
            IsVacationWorld = isVacationWorld,
            IsUniversityWorld = isUniversityWorld,
            ClimateType = ClimateType.Arctic,
            TerrainType = TerrainType.Mountain
        };
        _disposables.Add(resource);
        resource.AddLocalizedName("English", "Test Neighborhood");
        resource.AddLot(12345);

        using var stream = new MemoryStream();

        // Act
        await resource.SaveToStreamAsync(stream);

        // Assert
        resource.IsDirty.Should().BeFalse();
        stream.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [AutoData]
    public async Task LoadFromStreamAsync_WithValidStream_ShouldLoadSuccessfully()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);
        _disposables.Add(resource);

        // Create valid neighborhood data
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((uint)9); // version
        writer.Write((ulong)12345); // worldDescriptionInstanceId
        writer.Write((uint)67890); // neighborhoodId
        writer.Write((byte)1); // isVacationWorld
        writer.Write((byte)0); // isUniversityWorld
        writer.Write((byte)1); // hasTimeOverride
        writer.Write((byte)10); // timeOverrideHour
        writer.Write((byte)30); // timeOverrideMinute
        writer.Write((int)ClimateType.Tropical); // climateType
        writer.Write((int)TerrainType.Grassland); // terrainType
        writer.Write((byte)1); // isEditable

        // Localized names
        writer.Write((uint)2); // count
        writer.Write("English");
        writer.Write("Test Neighborhood");
        writer.Write("French");
        writer.Write("Quartier de Test");

        // Lot instance IDs
        writer.Write((uint)3); // count
        writer.Write((ulong)11111);
        writer.Write((ulong)22222);
        writer.Write((ulong)33333);

        stream.Position = 0;

        // Act
        await resource.LoadFromStreamAsync(stream);

        // Assert
        resource.IsDirty.Should().BeFalse();
        resource.WorldDescriptionInstanceId.Should().Be(12345UL);
        resource.NeighborhoodId.Should().Be(67890U);
        resource.IsVacationWorld.Should().BeTrue();
        resource.IsUniversityWorld.Should().BeFalse();
        resource.HasTimeOverride.Should().BeTrue();
        resource.TimeOverrideHour.Should().Be(10);
        resource.TimeOverrideMinute.Should().Be(30);
        resource.ClimateType.Should().Be(ClimateType.Tropical);
        resource.TerrainType.Should().Be(TerrainType.Grassland);
        resource.IsEditable.Should().BeTrue();
        resource.LocalizedNames.Should().HaveCount(2);
        resource.LocalizedNames["English"].Should().Be("Test Neighborhood");
        resource.LocalizedNames["French"].Should().Be("Quartier de Test");
        resource.LotInstanceIds.Should().HaveCount(3);
        resource.LotInstanceIds.Should().Contain(11111UL);
        resource.LotInstanceIds.Should().Contain(22222UL);
        resource.LotInstanceIds.Should().Contain(33333UL);
    }

    [Theory]
    [AutoData]
    public void ToString_ShouldReturnExpectedFormat(uint neighborhoodId)
    {
        // Ensure valid ID for display
        neighborhoodId = neighborhoodId == 0 ? 1 : neighborhoodId;

        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9)
        {
            NeighborhoodId = neighborhoodId,
            ClimateType = ClimateType.Temperate,
            TerrainType = TerrainType.Grassland
        };
        _disposables.Add(resource);
        resource.AddLocalizedName("English", "Test Neighborhood");

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain($"NeighborhoodResource {neighborhoodId}");
        result.Should().Contain("Test Neighborhood");
        result.Should().Contain("Temperate");
        result.Should().Contain("Grassland");
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);

        // Act & Assert
        Action act = () => resource.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 9);

        // Act & Assert
        Action act = () =>
        {
            resource.Dispose();
            resource.Dispose();
            resource.Dispose();
        };

        act.Should().NotThrow();
    }
}

/// <summary>
/// Unit tests for the ClimateType enum.
/// </summary>
public sealed class ClimateTypeTests
{
    [Theory]
    [InlineData(ClimateType.Temperate, "Temperate")]
    [InlineData(ClimateType.Tropical, "Tropical")]
    [InlineData(ClimateType.Arctic, "Arctic")]
    [InlineData(ClimateType.Desert, "Desert")]
    [InlineData(ClimateType.Mediterranean, "Mediterranean")]
    public void ToString_ShouldReturnEnumName(ClimateType climateType, string expectedName)
    {
        // Act
        var result = climateType.ToString();

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public void AllEnumValues_ShouldBeValid()
    {
        // Arrange
        var expectedValues = new[]
        {
            ClimateType.Temperate,
            ClimateType.Tropical,
            ClimateType.Arctic,
            ClimateType.Desert,
            ClimateType.Mediterranean
        };

        // Act
        var actualValues = Enum.GetValues<ClimateType>();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
    }
}

/// <summary>
/// Unit tests for the TerrainType enum.
/// </summary>
public sealed class TerrainTypeTests
{
    [Theory]
    [InlineData(TerrainType.Desert, "Desert")]
    [InlineData(TerrainType.Grassland, "Grassland")]
    [InlineData(TerrainType.Forest, "Forest")]
    [InlineData(TerrainType.Mountain, "Mountain")]
    [InlineData(TerrainType.Beach, "Beach")]
    [InlineData(TerrainType.Urban, "Urban")]
    public void ToString_ShouldReturnEnumName(TerrainType terrainType, string expectedName)
    {
        // Act
        var result = terrainType.ToString();

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public void AllEnumValues_ShouldBeValid()
    {
        // Arrange
        var expectedValues = new[]
        {
            TerrainType.Desert,
            TerrainType.Grassland,
            TerrainType.Forest,
            TerrainType.Mountain,
            TerrainType.Beach,
            TerrainType.Urban
        };

        // Act
        var actualValues = Enum.GetValues<TerrainType>();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
    }
}
