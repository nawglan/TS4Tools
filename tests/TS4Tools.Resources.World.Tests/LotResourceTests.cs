using AutoFixture.Xunit2;
using FluentAssertions;
using TS4Tools.Core.Package;
using TS4Tools.Resources.World;
using Xunit;

namespace TS4Tools.Resources.World.Tests;

/// <summary>
/// Unit tests for the LotResource class.
/// </summary>
public sealed class LotResourceTests : IDisposable
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
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);

        // Act
        var resource = new LotResource(key, version);
        _disposables.Add(resource);

        // Assert
        resource.Key.Should().Be(key);
        resource.Version.Should().Be(version);
        resource.IsDirty.Should().BeTrue();
        resource.IsEditable.Should().BeTrue();
        resource.Position.Should().Be(LotPosition.Origin);
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new LotResource(null!, 9);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Theory]
    [AutoData]
    public void Properties_SettingValues_ShouldUpdateCorrectly(
        ulong worldDescriptionInstanceId,
        uint lotId,
        uint simoleonPrice,
        sbyte lotSizeX,
        sbyte lotSizeZ,
        bool isEditable,
        ulong ambienceFileInstanceId,
        bool enabledForAutoTest,
        bool hasOverrideAmbience,
        ulong audioEffectFileInstanceId,
        bool disableBuildBuy,
        bool hideFromLotPicker,
        float x, float y, float z,
        float rotation)
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9);
        _disposables.Add(resource);
        var position = new LotPosition(x, y, z);

        // Act
        resource.WorldDescriptionInstanceId = worldDescriptionInstanceId;
        resource.LotId = lotId;
        resource.SimoleonPrice = simoleonPrice;
        resource.LotSizeX = lotSizeX;
        resource.LotSizeZ = lotSizeZ;
        resource.IsEditable = isEditable;
        resource.AmbienceFileInstanceId = ambienceFileInstanceId;
        resource.EnabledForAutoTest = enabledForAutoTest;
        resource.HasOverrideAmbience = hasOverrideAmbience;
        resource.AudioEffectFileInstanceId = audioEffectFileInstanceId;
        resource.DisableBuildBuy = disableBuildBuy;
        resource.HideFromLotPicker = hideFromLotPicker;
        resource.Position = position;
        resource.Rotation = rotation;

        // Assert
        resource.WorldDescriptionInstanceId.Should().Be(worldDescriptionInstanceId);
        resource.LotId.Should().Be(lotId);
        resource.SimoleonPrice.Should().Be(simoleonPrice);
        resource.LotSizeX.Should().Be(lotSizeX);
        resource.LotSizeZ.Should().Be(lotSizeZ);
        resource.IsEditable.Should().Be(isEditable);
        resource.AmbienceFileInstanceId.Should().Be(ambienceFileInstanceId);
        resource.EnabledForAutoTest.Should().Be(enabledForAutoTest);
        resource.HasOverrideAmbience.Should().Be(hasOverrideAmbience);
        resource.AudioEffectFileInstanceId.Should().Be(audioEffectFileInstanceId);
        resource.DisableBuildBuy.Should().Be(disableBuildBuy);
        resource.HideFromLotPicker.Should().Be(hideFromLotPicker);
        resource.Position.Should().Be(position);
        resource.Rotation.Should().Be(rotation);
    }

    [Theory]
    [InlineData(10, 20, 200)]
    [InlineData(5, 5, 25)]
    [InlineData(-10, -15, 150)] // Test with negative sizes (absolute value should be used)
    public void CalculateArea_WithValidSizes_ShouldReturnCorrectArea(sbyte sizeX, sbyte sizeZ, int expectedArea)
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9)
        {
            LotSizeX = sizeX,
            LotSizeZ = sizeZ
        };
        _disposables.Add(resource);

        // Act
        var area = resource.CalculateArea();

        // Assert
        area.Should().Be(expectedArea);
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnEmptyErrors()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9)
        {
            LotSizeX = 10,
            LotSizeZ = 20,
            LotId = 12345,
            WorldDescriptionInstanceId = 67890
        };
        _disposables.Add(resource);

        // Act
        var errors = resource.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidConfiguration_ShouldReturnErrors()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9)
        {
            LotSizeX = 0,
            LotSizeZ = -5,
            LotId = 0,
            WorldDescriptionInstanceId = 0
        };
        _disposables.Add(resource);

        // Act
        var errors = resource.Validate();

        // Assert
        errors.Should().HaveCount(4);
        errors.Should().Contain("Lot size X must be positive");
        errors.Should().Contain("Lot size Z must be positive");
        errors.Should().Contain("Lot ID cannot be zero");
        errors.Should().Contain("World description instance ID cannot be zero");
    }

    [Theory]
    [AutoData]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9);
        _disposables.Add(resource);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.LoadFromStreamAsync(null!));
    }

    [Theory]
    [AutoData]
    public async Task SaveToStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9);
        _disposables.Add(resource);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.SaveToStreamAsync(null!));
    }

    [Theory]
    [AutoData]
    public async Task SaveToStreamAsync_WithValidStream_ShouldSaveSuccessfully(
        ulong worldDescriptionInstanceId,
        uint lotId,
        uint simoleonPrice,
        sbyte lotSizeX,
        sbyte lotSizeZ)
    {
        // Ensure positive sizes for valid lot
        lotSizeX = (sbyte)Math.Abs(lotSizeX == 0 ? 1 : lotSizeX);
        lotSizeZ = (sbyte)Math.Abs(lotSizeZ == 0 ? 1 : lotSizeZ);

        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9)
        {
            WorldDescriptionInstanceId = worldDescriptionInstanceId,
            LotId = lotId,
            SimoleonPrice = simoleonPrice,
            LotSizeX = lotSizeX,
            LotSizeZ = lotSizeZ,
            Position = new LotPosition(1.5f, 2.5f, 3.5f),
            Rotation = 45.0f
        };
        _disposables.Add(resource);

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
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9);
        _disposables.Add(resource);

        // Create valid lot data
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((uint)9); // version
        writer.Write((ulong)12345); // worldDescriptionInstanceId
        writer.Write((uint)67890); // lotId
        writer.Write((uint)50000); // simoleonPrice
        writer.Write((sbyte)20); // lotSizeX
        writer.Write((sbyte)30); // lotSizeZ
        writer.Write((sbyte)1); // isEditable
        writer.Write((ulong)11111); // ambienceFileInstanceId
        writer.Write((byte)1); // enabledForAutoTest
        writer.Write((byte)0); // hasOverrideAmbience
        writer.Write((ulong)22222); // audioEffectFileInstanceId
        writer.Write((byte)0); // disableBuildBuy
        writer.Write((byte)0); // hideFromLotPicker
        writer.Write(10.5f); // position X
        writer.Write(20.5f); // position Y
        writer.Write(30.5f); // position Z
        writer.Write(90.0f); // rotation

        stream.Position = 0;

        // Act
        await resource.LoadFromStreamAsync(stream);

        // Assert
        resource.IsDirty.Should().BeFalse();
        resource.WorldDescriptionInstanceId.Should().Be(12345UL);
        resource.LotId.Should().Be(67890U);
        resource.SimoleonPrice.Should().Be(50000U);
        resource.LotSizeX.Should().Be(20);
        resource.LotSizeZ.Should().Be(30);
        resource.IsEditable.Should().BeTrue();
        resource.Position.X.Should().Be(10.5f);
        resource.Position.Y.Should().Be(20.5f);
        resource.Position.Z.Should().Be(30.5f);
        resource.Rotation.Should().Be(90.0f);
    }

    [Theory]
    [AutoData]
    public void ToString_ShouldReturnExpectedFormat(uint lotId, sbyte lotSizeX, sbyte lotSizeZ, uint simoleonPrice)
    {
        // Ensure positive sizes for valid display
        lotSizeX = (sbyte)Math.Abs(lotSizeX == 0 ? 1 : lotSizeX);
        lotSizeZ = (sbyte)Math.Abs(lotSizeZ == 0 ? 1 : lotSizeZ);

        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9)
        {
            LotId = lotId,
            LotSizeX = lotSizeX,
            LotSizeZ = lotSizeZ,
            SimoleonPrice = simoleonPrice
        };
        _disposables.Add(resource);

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain($"LotResource {lotId}");
        result.Should().Contain($"({lotSizeX}x{lotSizeZ})");
        result.Should().Contain("§");
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9);

        // Act & Assert
        Action act = () => resource.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9);

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
/// Unit tests for the LotPosition struct.
/// </summary>
public sealed class LotPositionTests
{
    [Theory]
    [AutoData]
    public void Constructor_WithCoordinates_ShouldInitializeCorrectly(float x, float y, float z)
    {
        // Act
        var position = new LotPosition(x, y, z);

        // Assert
        position.X.Should().Be(x);
        position.Y.Should().Be(y);
        position.Z.Should().Be(z);
    }

    [Fact]
    public void Origin_ShouldReturnZeroCoordinates()
    {
        // Act
        var origin = LotPosition.Origin;

        // Assert
        origin.X.Should().Be(0f);
        origin.Y.Should().Be(0f);
        origin.Z.Should().Be(0f);
    }

    [Theory]
    [InlineData(0f, 0f, 0f, 3f, 4f, 0f, 5f)] // 3-4-5 triangle in XY plane
    [InlineData(1f, 1f, 1f, 4f, 5f, 13f, 15f)] // 3D distance
    public void DistanceTo_WithKnownPositions_ShouldReturnCorrectDistance(
        float x1, float y1, float z1,
        float x2, float y2, float z2,
        float expectedDistance)
    {
        // Arrange
        var position1 = new LotPosition(x1, y1, z1);
        var position2 = new LotPosition(x2, y2, z2);

        // Act
        var distance = position1.DistanceTo(position2);

        // Assert
        distance.Should().BeApproximately(expectedDistance, 0.001f);
    }

    [Theory]
    [AutoData]
    public void ToString_ShouldReturnExpectedFormat(float x, float y, float z)
    {
        // Arrange
        var position = new LotPosition(x, y, z);

        // Act
        var result = position.ToString();

        // Assert
        result.Should().Contain(x.ToString("F2"));
        result.Should().Contain(y.ToString("F2"));
        result.Should().Contain(z.ToString("F2"));
        result.Should().MatchRegex(@"\(-?\d+\.\d{2}, -?\d+\.\d{2}, -?\d+\.\d{2}\)");
    }
}
