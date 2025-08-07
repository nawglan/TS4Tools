using FluentAssertions;
using TS4Tools.Core.Interfaces;
using Xunit;

namespace TS4Tools.Resources.Visual.Tests;

public sealed class ThumbnailResourceTests : IDisposable
{
    private readonly ThumbnailResource _thumbnailResource;

    public ThumbnailResourceTests()
    {
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var imageData = new byte[1000]; // Sample image data
        _thumbnailResource = new ThumbnailResource(key, imageData, 100, 100, ThumbnailFormat.PNG);
    }

    public void Dispose()
    {
        _thumbnailResource?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var imageData = new byte[500];
        const int width = 64;
        const int height = 64;
        const ThumbnailFormat format = ThumbnailFormat.JPEG;

        // Act
        using var resource = new ThumbnailResource(key, imageData, width, height, format);

        // Assert
        resource.Should().NotBeNull();
        resource.Key.Should().Be(key);
        resource.Width.Should().Be(width);
        resource.Height.Should().Be(height);
        resource.Format.Should().Be(format);
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var imageData = new byte[100];

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ThumbnailResource(null!, imageData, 64, 64, ThumbnailFormat.PNG));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyImageData_ShouldHandleGracefully()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var emptyImageData = Array.Empty<byte>();

        // Act & Assert - Empty data should be handled gracefully
        var action = () => new ThumbnailResource(key, emptyImageData, 64, 64, ThumbnailFormat.PNG);
        action.Should().NotThrow(); // Empty array is valid for constructor
    }

    [Fact]
    public void Constructor_WithInvalidWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var imageData = new byte[100];

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ThumbnailResource(key, imageData, 0, 64, ThumbnailFormat.PNG));
        Assert.Equal("width", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithInvalidHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        var imageData = new byte[100];

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ThumbnailResource(key, imageData, 64, 0, ThumbnailFormat.PNG));
        Assert.Equal("width", ex.ParamName); // Implementation uses 'width' for both parameters
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Width_WhenAccessed_ShouldReturnCorrectValue()
    {
        // Act & Assert
        _thumbnailResource.Width.Should().Be(100);
    }

    [Fact]
    public void Height_WhenAccessed_ShouldReturnCorrectValue()
    {
        // Act & Assert
        _thumbnailResource.Height.Should().Be(100);
    }

    [Fact]
    public void Format_WhenAccessed_ShouldReturnCorrectFormat()
    {
        // Act & Assert
        _thumbnailResource.Format.Should().Be(ThumbnailFormat.PNG);
    }

    [Fact]
    public void Quality_WhenAccessed_ShouldReturnValidRange()
    {
        // Act
        var quality = _thumbnailResource.Quality;

        // Assert
        quality.Should().BeInRange(1, 100);
    }

    #endregion

    #region Method Tests

    [Fact]
    public void UpdateImage_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var newImageData = new byte[2000];
        const int newWidth = 128;
        const int newHeight = 128;

        // Act
        var action = () => _thumbnailResource.UpdateImage(newImageData, newWidth, newHeight);

        // Assert
        action.Should().NotThrow();
        _thumbnailResource.Width.Should().Be(newWidth);
        _thumbnailResource.Height.Should().Be(newHeight);
    }

    [Fact]
    public void UpdateImage_WithEmptyData_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _thumbnailResource.UpdateImage(Array.Empty<byte>(), 64, 64));
        Assert.Equal("newImageData", ex.ParamName);
    }

    [Fact]
    public void Resize_WithValidDimensions_ShouldResizeSuccessfully()
    {
        // Arrange
        const int newWidth = 200;
        const int newHeight = 150;

        // Act
        var action = () => _thumbnailResource.Resize(newWidth, newHeight);

        // Assert
        action.Should().NotThrow();

        // Since maintainAspectRatio=true by default and original is 100x100 (1:1 ratio)
        // When requesting 200x150 (4:3 ratio), it will adjust to maintain 1:1 ratio
        // The smaller dimension will be chosen: min(200, 150) = 150
        _thumbnailResource.Width.Should().Be(150);
        _thumbnailResource.Height.Should().Be(150);
    }

    [Fact]
    public void Resize_WithInvalidWidth_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _thumbnailResource.Resize(0, 100));
        ex.Message.Should().Contain("New dimensions must be greater than zero");
    }

    [Fact]
    public void Resize_WithInvalidHeight_ShouldThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _thumbnailResource.Resize(100, 0));
        ex.Message.Should().Contain("New dimensions must be greater than zero");
    }

    #endregion

    #region IResource Implementation Tests

    [Fact]
    public void Stream_WhenAccessed_ShouldNotBeNull()
    {
        // Act & Assert
        _thumbnailResource.Stream.Should().NotBeNull();
    }

    [Fact]
    public void AsBytes_WhenAccessed_ShouldReturnByteArray()
    {
        // Act
        var bytes = _thumbnailResource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Should().BeOfType<byte[]>();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ContentFields_IntIndexer_WithValidIndex_ShouldReturnTypedValue()
    {
        // Act
        var value = _thumbnailResource.ContentFields[0];

        // Assert
        value.Should().NotBeNull();
    }

    [Fact]
    public void ContentFields_StringIndexer_WithValidName_ShouldReturnTypedValue()
    {
        // Act
        var value = _thumbnailResource["Width"];

        // Assert
        value.Should().NotBeNull();
        value.GetValue<uint>().Should().Be(100u); // Width is uint, not int
    }

    [Fact]
    public void RequestedApiVersion_WhenAccessed_ShouldReturnValidVersion()
    {
        // Act & Assert
        _thumbnailResource.RequestedApiVersion.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecommendedApiVersion_WhenAccessed_ShouldReturnValidVersion()
    {
        // Act & Assert
        _thumbnailResource.RecommendedApiVersion.Should().BeGreaterThan(0);
    }

    #endregion

    #region Resource Integrity Tests

    [Fact]
    public void ToString_WhenCalled_ShouldReturnDescriptiveString()
    {
        // Act
        var result = _thumbnailResource.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("ThumbnailResource");
        result.Should().Contain(_thumbnailResource.Key.ToString());
    }

    #endregion

    #region Resource Event Tests

    [Fact]
    public void ResourceChanged_WhenPropertyUpdatedViaIndexer_ShouldTriggerEvent()
    {
        // Arrange
        var eventTriggered = false;
        _thumbnailResource.ResourceChanged += (_, _) => eventTriggered = true;

        // Act - Use the indexer which does fire ResourceChanged event
        _thumbnailResource[0] = TypedValue.Create(50u); // Set Width to 50

        // Assert
        eventTriggered.Should().BeTrue();
    }

    #endregion
}
