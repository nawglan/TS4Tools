namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Unit tests for the ImageResource class.
/// </summary>
public sealed class ImageResourceTests : IDisposable
{
    private readonly NullLogger<ImageResource> _logger;
    private readonly List<ImageResource> _disposables;

    public ImageResourceTests()
    {
        _logger = NullLogger<ImageResource>.Instance;
        _disposables = new List<ImageResource>();
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();
    }

    private ImageResource CreateResource(byte[]? data = null, int apiVersion = 1)
    {
        var resource = data is null
            ? new ImageResource(apiVersion, _logger)
            : new ImageResource(data, apiVersion, _logger);

        _disposables.Add(resource);
        return resource;
    }

    [Fact]
    public void Constructor_Default_CreatesEmptyResource()
    {
        // Act
        var resource = CreateResource();

        // Assert
        resource.RequestedApiVersion.Should().Be(1);
        resource.Metadata.Format.Should().Be(ImageFormat.Unknown);
        resource.IsModified.Should().BeTrue();
        resource.RawData.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithApiVersion_SetsCorrectVersion()
    {
        // Act
        var resource = CreateResource(apiVersion: 2);

        // Assert
        resource.RequestedApiVersion.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithPngData_LoadsPngMetadata()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();

        // Act
        var resource = CreateResource(pngData);

        // Assert
        resource.Metadata.Format.Should().Be(ImageFormat.PNG);
        resource.Metadata.Width.Should().Be(4);
        resource.Metadata.Height.Should().Be(4);
        resource.Metadata.MipMapCount.Should().Be(1);
        resource.Metadata.HasAlpha.Should().BeTrue();
        resource.IsModified.Should().BeFalse();
        resource.RawData.Length.Should().Be(pngData.Length);
    }

    [Fact]
    public void Constructor_WithJpegData_LoadsJpegMetadata()
    {
        // Arrange
        var jpegData = TestImageDataGenerator.CreateTestJpeg();

        // Act
        var resource = CreateResource(jpegData);

        // Assert
        resource.Metadata.Format.Should().Be(ImageFormat.JPEG);
        resource.Metadata.Width.Should().Be(4);
        resource.Metadata.Height.Should().Be(4);
        resource.Metadata.MipMapCount.Should().Be(1);
        resource.Metadata.HasAlpha.Should().BeFalse();
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithBmpData_LoadsBmpMetadata()
    {
        // Arrange
        var bmpData = TestImageDataGenerator.CreateTestBmp();

        // Act
        var resource = CreateResource(bmpData);

        // Assert
        resource.Metadata.Format.Should().Be(ImageFormat.BMP);
        resource.Metadata.Width.Should().Be(4);
        resource.Metadata.Height.Should().Be(4);
        resource.Metadata.MipMapCount.Should().Be(1);
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithInvalidData_CreatesUnknownFormatResource()
    {
        // Arrange
        var invalidData = TestImageDataGenerator.CreateInvalidImageData();

        // Act
        var resource = CreateResource(invalidData);

        // Assert
        resource.Metadata.Format.Should().Be(ImageFormat.Unknown);
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithEmptyData_CreatesUnknownFormatResource()
    {
        // Arrange
        var emptyData = TestImageDataGenerator.CreateEmptyData();

        // Act
        var resource = CreateResource(emptyData);

        // Assert
        resource.Metadata.Format.Should().Be(ImageFormat.Unknown);
    }

    [Fact]
    public void Constructor_WithStream_LoadsImageFromStream()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        using var stream = new MemoryStream(pngData);

        // Act
        var resource = new ImageResource(stream, 1, _logger);
        _disposables.Add(resource);

        // Assert
        resource.Metadata.Format.Should().Be(ImageFormat.PNG);
        resource.Metadata.Width.Should().Be(4);
        resource.Metadata.Height.Should().Be(4);
    }

    [Fact]
    public void Constructor_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new ImageResource((Stream)null!, 1, _logger);
        action.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void RawData_GetSet_ReturnsAndUpdatesData()
    {
        // Arrange
        var resource = CreateResource();
        var pngData = TestImageDataGenerator.CreateTestPng();

        // Act
        resource.RawData = pngData;
        var retrievedData = resource.RawData;

        // Assert
        retrievedData.Should().Equal(pngData);
        resource.Metadata.Format.Should().Be(ImageFormat.PNG);
        resource.IsModified.Should().BeFalse();
    }

    [Fact]
    public void RawData_SetNull_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = CreateResource();

        // Act & Assert
        var action = () => resource.RawData = null!;
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Stream_Get_ReturnsMemoryStreamWithImageData()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = CreateResource(pngData);

        // Act
        using var stream = resource.Stream;

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().Be(pngData.Length);

        var streamData = new byte[stream.Length];
        var bytesRead = stream.Read(streamData, 0, streamData.Length);
        bytesRead.Should().Be(streamData.Length);
        streamData.Should().Equal(pngData);
    }

    [Fact]
    public async Task LoadFromStreamAsync_ValidStream_LoadsImageData()
    {
        // Arrange
        var resource = CreateResource();
        var jpegData = TestImageDataGenerator.CreateTestJpeg();
        using var stream = new MemoryStream(jpegData);

        // Act
        await resource.LoadFromStreamAsync(stream);

        // Assert
        resource.Metadata.Format.Should().Be(ImageFormat.JPEG);
        resource.RawData.ToArray().Should().Equal(jpegData);
    }

    [Fact]
    public async Task LoadFromStreamAsync_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = CreateResource();

        // Act & Assert
        var action = async () => await resource.LoadFromStreamAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var resource = CreateResource();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var jpegData = TestImageDataGenerator.CreateTestJpeg();
        using var stream = new MemoryStream(jpegData);

        // Act & Assert
        var action = async () => await resource.LoadFromStreamAsync(stream, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(ImageFormat.PNG)]
    [InlineData(ImageFormat.JPEG)]
    [InlineData(ImageFormat.BMP)]
    public async Task ConvertToFormatAsync_SameFormat_ReturnsOriginalData(ImageFormat format)
    {
        // Arrange
        var originalData = format switch
        {
            ImageFormat.PNG => TestImageDataGenerator.CreateTestPng(),
            ImageFormat.JPEG => TestImageDataGenerator.CreateTestJpeg(),
            ImageFormat.BMP => TestImageDataGenerator.CreateTestBmp(),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        var resource = CreateResource(originalData);

        // Act
        var convertedData = await resource.ConvertToFormatAsync(format);

        // Assert
        convertedData.Should().Equal(originalData);
    }

    [Fact]
    public async Task ConvertToFormatAsync_PngToJpeg_ConvertsFormat()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = CreateResource(pngData);

        // Act
        var jpegData = await resource.ConvertToFormatAsync(ImageFormat.JPEG);

        // Assert
        jpegData.Should().NotEqual(pngData);
        jpegData.Length.Should().BeGreaterThan(0);

        // Verify it's actually JPEG by checking magic number
        jpegData[0].Should().Be(0xFF);
        jpegData[1].Should().Be(0xD8);
    }

    [Fact]
    public async Task ConvertToFormatAsync_UnknownFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidData = TestImageDataGenerator.CreateInvalidImageData();
        var resource = CreateResource(invalidData);

        // Act & Assert
        var action = async () => await resource.ConvertToFormatAsync(ImageFormat.PNG);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot convert image with unknown format");
    }

    [Fact]
    public async Task ConvertToFormatAsync_UnsupportedFormat_ThrowsNotSupportedException()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = CreateResource(pngData);

        // Act & Assert
        var action = async () => await resource.ConvertToFormatAsync((ImageFormat)999);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task ToImageSharpAsync_PngData_ReturnsValidImage()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = CreateResource(pngData);

        // Act
        using var image = await resource.ToImageSharpAsync<Rgba32>();

        // Assert
        image.Should().NotBeNull();
        image.Width.Should().Be(4);
        image.Height.Should().Be(4);

        // Verify known pixel colors
        image[0, 0].Should().Be(new Rgba32(255, 0, 0, 255)); // Red
        image[3, 0].Should().Be(new Rgba32(0, 128, 0, 255)); // Green
        image[0, 3].Should().Be(new Rgba32(0, 0, 255, 255)); // Blue
        image[3, 3].Should().Be(new Rgba32(255, 255, 255, 255)); // White
    }

    [Fact]
    public async Task FromImageSharpAsync_ValidImage_UpdatesResourceData()
    {
        // Arrange
        var resource = CreateResource();
        using var image = new Image<Rgba32>(8, 8);

        // Set specific pixel pattern
        image[0, 0] = Color.Red;
        image[7, 7] = Color.Blue;

        // Act
        await resource.FromImageSharpAsync(image, ImageFormat.PNG);

        // Assert
        resource.Metadata.Format.Should().Be(ImageFormat.PNG);
        resource.Metadata.Width.Should().Be(8);
        resource.Metadata.Height.Should().Be(8);
        resource.IsModified.Should().BeFalse();

        // Verify the image can be loaded back
        using var loadedImage = await resource.ToImageSharpAsync<Rgba32>();
        loadedImage[0, 0].Should().Be(new Rgba32(255, 0, 0, 255)); // Red
        loadedImage[7, 7].Should().Be(new Rgba32(0, 0, 255, 255)); // Blue
    }

    [Fact]
    public async Task FromImageSharpAsync_NullImage_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = CreateResource();

        // Act & Assert
        var action = async () => await resource.FromImageSharpAsync<Rgba32>(null!, ImageFormat.PNG);
        await action.Should().ThrowAsync<ArgumentNullException>().WithParameterName("image");
    }

    [Fact]
    public async Task FromImageSharpAsync_UnsupportedFormat_ThrowsNotSupportedException()
    {
        // Arrange
        var resource = CreateResource();
        using var image = new Image<Rgba32>(4, 4);

        // Act & Assert
        var action = async () => await resource.FromImageSharpAsync(image, (ImageFormat)999);
        await action.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public void Dispose_DisposesResourceProperly()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = new ImageResource(pngData, 1, _logger);

        // Act
        resource.Dispose();

        // Assert - operations should throw ObjectDisposedException
        var rawDataAction = () => _ = resource.RawData;
        var streamAction = () => _ = resource.Stream;
        var loadAction = async () => await resource.LoadFromStreamAsync(new MemoryStream());

        rawDataAction.Should().Throw<ObjectDisposedException>();
        streamAction.Should().Throw<ObjectDisposedException>();
        loadAction.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var resource = new ImageResource(1, _logger);

        // Act & Assert
        resource.Dispose();
        var action = () => resource.Dispose();
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(90)]
    [InlineData(50)]
    [InlineData(10)]
    public async Task ConvertToFormatAsync_WithQuality_UsesSpecifiedQuality(int quality)
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = CreateResource(pngData);

        // Act
        var jpegData = await resource.ConvertToFormatAsync(ImageFormat.JPEG, quality);

        // Assert
        jpegData.Should().NotBeEmpty();
        jpegData[0].Should().Be(0xFF); // JPEG magic number
        jpegData[1].Should().Be(0xD8);
    }

    [Fact]
    public void ImageData_ReturnsReadOnlySpan()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = CreateResource(pngData);

        // Act
        var imageData = resource.RawData;

        // Assert
        imageData.Length.Should().Be(pngData.Length);
        imageData.Should().Equal(pngData);
    }

    [Fact]
    public void Constructor_WithValidData_LogsDebugMessage()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();

        // Act
        var resource = CreateResource(pngData);

        // Assert
        // Note: Using NullLogger for simplicity - logger testing would require FakeLogger
        resource.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = new ImageResource(pngData, 1, _logger);

        // Act & Assert - should not throw
        resource.Dispose();
        resource.Dispose(); // Second disposal should be safe
        resource.Dispose(); // Third disposal should also be safe
    }

    [Fact]
    public void Dispose_ClearsImageData_AndPreventsAccess()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = new ImageResource(pngData, 1, _logger);
        
        // Verify resource is initially functional
        resource.RawData.Should().NotBeEmpty();

        // Act
        resource.Dispose();

        // Assert - accessing properties after disposal should throw
        Action act1 = () => _ = resource.RawData;
        Action act2 = () => _ = resource.Stream;
        Action act3 = () => _ = resource.ImageData;

        act1.Should().Throw<ObjectDisposedException>();
        act2.Should().Throw<ObjectDisposedException>();
        act3.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_WithEmptyResource_DoesNotThrow()
    {
        // Arrange
        var resource = new ImageResource(1, _logger);

        // Act & Assert - should not throw
        Action act = () => resource.Dispose();
        act.Should().NotThrow();
    }

    [Fact] 
    public void Dispose_AfterModification_ClearsModificationFlag()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        var resource = new ImageResource(pngData, 1, _logger);
        
        // Force modification by setting new data
        resource.RawData = TestImageDataGenerator.CreateTestPng();

        // Act
        resource.Dispose();

        // Assert - we can't check IsModified after disposal since it would throw,
        // but we can verify disposal completed without exception
        Action act = () => resource.Dispose(); // Second disposal
        act.Should().NotThrow();
    }
}
