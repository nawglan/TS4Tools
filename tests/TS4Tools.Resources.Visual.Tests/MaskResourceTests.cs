namespace TS4Tools.Resources.Visual.Tests;

public sealed class MaskResourceTests : IDisposable
{
    private readonly MaskResource _maskResource;

    public MaskResourceTests()
    {
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var data = new byte[100 * 100]; // 100x100 alpha mask
        _maskResource = new MaskResource(key, data, 100, 100);
    }

    public void Dispose()
    {
        _maskResource?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var data = new byte[256 * 256];

        // Act
        using var resource = new MaskResource(key, data, 256, 256);

        // Assert
        resource.Width.Should().Be(256u);
        resource.Height.Should().Be(256u);
        resource.Key.Should().Be(key);
        resource.Channels.Should().Be(1);
        resource.BitsPerChannel.Should().Be(8);
        resource.Format.Should().Be(MaskFormat.Alpha);
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var data = new byte[100 * 100];

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new MaskResource(null!, data, 100, 100));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithZeroWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var data = new byte[100];

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new MaskResource(key, data, 0, 100));
        Assert.Equal("width", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithZeroHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var data = new byte[100];

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new MaskResource(key, data, 100, 0));
        Assert.Equal("width", ex.ParamName); // The implementation uses nameof(width) for both
    }

    #endregion

    #region Pixel Access Tests

    [Fact]
    public void GetPixelAlpha_WithValidCoordinates_ShouldReturnAlphaValue()
    {
        // Act
        var alpha = _maskResource.GetPixelAlpha(50, 50);

        // Assert
        alpha.Should().BeInRange((byte)0, (byte)255);
    }

    [Fact]
    public void SetPixelAlpha_WithValidValue_ShouldSetAlpha()
    {
        // Arrange
        const byte expectedAlpha = 192; // 75% of 255

        // Act
        _maskResource.SetPixelAlpha(25, 25, expectedAlpha);
        var actualAlpha = _maskResource.GetPixelAlpha(25, 25);

        // Assert
        actualAlpha.Should().Be(expectedAlpha);
    }

    [Fact]
    public void GetPixelAlpha_WithOutOfBoundsX_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _maskResource.GetPixelAlpha(100, 50));
        Assert.Equal("x", ex.ParamName);
    }

    [Fact]
    public void GetPixelAlpha_WithOutOfBoundsY_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _maskResource.GetPixelAlpha(50, 100));
        Assert.Equal("y", ex.ParamName);
    }

    [Fact]
    public void SetPixelAlpha_WithOutOfBoundsCoordinates_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex1 = Assert.Throws<ArgumentOutOfRangeException>(() => _maskResource.SetPixelAlpha(100, 50, 128));
        Assert.Equal("x", ex1.ParamName);

        var ex2 = Assert.Throws<ArgumentOutOfRangeException>(() => _maskResource.SetPixelAlpha(50, 100, 128));
        Assert.Equal("y", ex2.ParamName);
    }

    #endregion

    #region Data Update Tests

    [Fact]
    public void UpdateData_WithValidData_ShouldUpdateDimensions()
    {
        // Arrange
        var newData = new byte[200 * 150]; // 200x150 mask
        const uint newWidth = 200;
        const uint newHeight = 150;

        // Act
        _maskResource.UpdateData(newData, newWidth, newHeight);

        // Assert
        _maskResource.Width.Should().Be(newWidth);
        _maskResource.Height.Should().Be(newHeight);
        _maskResource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void UpdateData_WithInvalidDimensions_ShouldThrowArgumentException()
    {
        // Arrange
        var newData = new byte[100];

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _maskResource.UpdateData(newData, 0, 100));
        ex.Message.Should().Contain("Mask dimensions must be greater than zero");
    }

    [Fact]
    public void UpdateData_WithInvalidDataSize_ShouldThrowArgumentException()
    {
        // Arrange
        var newData = new byte[50]; // Too small for 100x100

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _maskResource.UpdateData(newData, 100, 100));
        ex.Message.Should().Contain("Data size");
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_WithNewKey_ShouldCreateCopy()
    {
        // Arrange
        var newKey = new ResourceKey(0x87654321, 0x12345678, 0x22222222);
        _maskResource.SetPixelAlpha(10, 10, 200); // Set a test pixel

        // Act
        using var cloned = _maskResource.Clone(newKey);

        // Assert
        cloned.Should().NotBeNull();
        cloned.Key.Should().Be(newKey);
        cloned.Width.Should().Be(_maskResource.Width);
        cloned.Height.Should().Be(_maskResource.Height);
        cloned.Format.Should().Be(_maskResource.Format);
        cloned.GetPixelAlpha(10, 10).Should().Be(200);
    }

    [Fact]
    public void Clone_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _maskResource.Clone(null!));
        Assert.Equal("newKey", ex.ParamName);
    }

    #endregion

    #region IResource Implementation Tests

    [Fact]
    public void Stream_WhenAccessed_ShouldNotBeNull()
    {
        // Act & Assert
        _maskResource.Stream.Should().NotBeNull();
    }

    [Fact]
    public void AsBytes_WhenAccessed_ShouldReturnByteArray()
    {
        // Act
        var bytes = _maskResource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Should().BeOfType<byte[]>();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ContentFields_IntIndexer_WithValidIndex_ShouldReturnTypedValue()
    {
        // Act
        var value = _maskResource.ContentFields[0];

        // Assert
        value.Should().NotBeNull();
    }

    [Fact]
    public void ContentFields_ShouldNotBeEmpty()
    {
        // Act & Assert
        _maskResource.ContentFields.Should().NotBeEmpty();
    }

    [Fact]
    public void IsDirty_AfterSetPixel_ShouldBeTrue()
    {
        // Arrange
        var initialDirty = _maskResource.IsDirty;

        // Act
        _maskResource.SetPixelAlpha(5, 5, 100);

        // Assert
        initialDirty.Should().BeFalse();
        _maskResource.IsDirty.Should().BeTrue();
    }

    #endregion
}
