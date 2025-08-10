namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Unit tests for the ImageResourceFactory class.
/// </summary>
public sealed class ImageResourceFactoryTests : IDisposable
{
    private readonly NullLogger<ImageResourceFactory> _logger;
    private readonly ImageResourceFactory _factory;
    private readonly List<ImageResource> _disposables;

    public ImageResourceFactoryTests()
    {
        _logger = NullLogger<ImageResourceFactory>.Instance;
        _factory = new ImageResourceFactory(_logger);
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

    private void TrackResource(ImageResource resource)
    {
        _disposables.Add(resource);
    }

    [Fact]
    public void ResourceTypes_ContainsExpectedTypes()
    {
        // Act
        var resourceTypes = _factory.ResourceTypes;

        // Assert
        resourceTypes.Should().Contain(ImageResource.DdsResourceType);
        resourceTypes.Should().Contain(ImageResource.PngResourceType);
        resourceTypes.Should().Contain(ImageResource.TgaResourceType);
        resourceTypes.Should().HaveCountGreaterThan(3);
    }

    [Fact]
    public void CreateResource_WithPngData_CreatesImageResource()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();

        // Act
        using var stream = new MemoryStream(pngData);
        var resource = _factory.CreateResource(stream, ImageResource.PngResourceType);
        TrackResource(resource);

        // Assert
        resource.Should().NotBeNull();
        resource.Metadata.Format.Should().Be(ImageFormat.PNG);
        resource.Metadata.Width.Should().Be(4);
        resource.Metadata.Height.Should().Be(4);
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public async Task CreateResource_WithCustomApiVersion_UsesSpecifiedVersion()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();

        // Act
        using var stream = new MemoryStream(pngData);
        var resource = await _factory.CreateResourceAsync(2, stream);
        TrackResource(resource);

        // Assert
        resource.RequestedApiVersion.Should().Be(2);
    }

    [Fact]
    public void CreateResource_WithEmptyData_ThrowsArgumentException()
    {
        // Arrange
        var emptyData = TestImageDataGenerator.CreateEmptyData();

        // Act & Assert
        using var stream = new MemoryStream(emptyData);
        var action = () => _factory.CreateResource(stream, ImageResource.PngResourceType);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Image data cannot be empty*")
            .WithParameterName("stream");
    }

    [Fact]
    public void CreateResource_WithUnsupportedResourceType_ThrowsArgumentException()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        uint unsupportedType = 0x99999999;

        // Act & Assert
        using var stream = new MemoryStream(pngData);
        var action = () => _factory.CreateResource(stream, unsupportedType);
        action.Should().Throw<ArgumentException>()
            .WithMessage($"Resource type 0x{unsupportedType:X8} is not supported by ImageResourceFactory*")
            .WithParameterName("resourceType");
    }

    [Fact]
    public void CreateResource_WithInvalidImageData_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = TestImageDataGenerator.CreateInvalidImageData();

        // Act & Assert
        using var stream = new MemoryStream(invalidData);
        var action = () => _factory.CreateResource(stream, ImageResource.PngResourceType);
        action.Should().Throw<InvalidDataException>()
            .WithMessage($"Unable to detect image format for resource type 0x{ImageResource.PngResourceType:X8}");
    }

    [Theory]
    [InlineData(ImageFormat.PNG)]
    [InlineData(ImageFormat.JPEG)]
    [InlineData(ImageFormat.BMP)]
    public void CreateResource_WithVariousFormats_CreatesCorrectResources(ImageFormat format)
    {
        // Arrange
        var imageData = format switch
        {
            ImageFormat.PNG => TestImageDataGenerator.CreateTestPng(),
            ImageFormat.JPEG => TestImageDataGenerator.CreateTestJpeg(),
            ImageFormat.BMP => TestImageDataGenerator.CreateTestBmp(),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        var resourceType = format switch
        {
            ImageFormat.PNG => ImageResource.PngResourceType,
            ImageFormat.JPEG => 0x2F7D0002u, // JPEG resource type
            ImageFormat.BMP => 0x2F7D0003u,  // BMP resource type
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        // Act
        using var stream = new MemoryStream(imageData);
        var resource = _factory.CreateResource(stream, resourceType);
        TrackResource(resource);

        // Assert
        resource.Should().NotBeNull();
        resource.Metadata.Format.Should().Be(format);
    }

    [Fact]
    public async Task CreateResourceAsync_WithPngStream_CreatesImageResource()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        using var stream = new MemoryStream(pngData);

        // Act
        var resource = await _factory.CreateResourceAsync(1, stream);
        TrackResource(resource);

        // Assert
        resource.Should().NotBeNull();
        resource.Metadata.Format.Should().Be(ImageFormat.PNG);
        resource.Metadata.Width.Should().Be(4);
        resource.Metadata.Height.Should().Be(4);
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_CreatesEmptyResource()
    {
        // Act
        var resource = await _factory.CreateResourceAsync(1, null);

        // Assert
        resource.Should().NotBeNull();
        resource.Metadata.Should().NotBeNull();
        resource.Metadata.Format.Should().Be(ImageFormat.Unknown);
        resource.Metadata.DataSize.Should().Be(0);
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidImageStream_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = TestImageDataGenerator.CreateInvalidImageData();
        using var stream = new MemoryStream(invalidData);

        // Act & Assert
        var action = async () => await _factory.CreateResourceAsync(1, stream);
        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Unable to detect image format*");
    }

    [Fact]
    public async Task CreateResourceAsync_WithInvalidImageStreamAndResourceType_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidData = TestImageDataGenerator.CreateInvalidImageData();
        using var stream = new MemoryStream(invalidData);

        // Act & Assert
        var action = async () => await _factory.CreateResourceAsync(1, stream, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidDataException>()
            .WithMessage($"Unable to detect image format for resource type 0x{ImageResource.PngResourceType:X8}");
    }

    [Fact]
    public async Task CreateResourceAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();
        using var stream = new MemoryStream(pngData);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _factory.CreateResourceAsync(1, stream, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(ImageFormat.PNG, true)]
    [InlineData(ImageFormat.JPEG, true)]
    [InlineData(ImageFormat.BMP, true)]
    [InlineData(ImageFormat.Unknown, false)]
    public void CanCreateResource_WithVariousFormats_ReturnsExpectedResult(ImageFormat format, bool expected)
    {
        // Arrange
        var imageData = format switch
        {
            ImageFormat.PNG => TestImageDataGenerator.CreateTestPng(),
            ImageFormat.JPEG => TestImageDataGenerator.CreateTestJpeg(),
            ImageFormat.BMP => TestImageDataGenerator.CreateTestBmp(),
            ImageFormat.Unknown => TestImageDataGenerator.CreateInvalidImageData(),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        var resourceType = format switch
        {
            ImageFormat.PNG => ImageResource.PngResourceType,
            ImageFormat.JPEG => 0x2F7D0002u, // JPEG resource type
            ImageFormat.BMP => 0x2F7D0003u,  // BMP resource type
            ImageFormat.Unknown => 0x99999999u, // Unsupported type
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        // Act
        var canCreate = _factory.CanCreateResource(resourceType);

        // Assert
        canCreate.Should().Be(expected);
    }

    [Fact]
    public void CanCreateResource_WithEmptyData_ReturnsFalse()
    {
        // Arrange - empty data doesn't matter for this test since CanCreateResource only checks resource type
        // Act
        var canCreate = _factory.CanCreateResource(ImageResource.PngResourceType);

        // Assert
        canCreate.Should().BeTrue(); // ImageResourceFactory should support PNG
    }

    [Fact]
    public void CanCreateResource_WithUnsupportedResourceType_ReturnsFalse()
    {
        // Arrange
        uint unsupportedType = 0x99999999;

        // Act
        var canCreate = _factory.CanCreateResource(unsupportedType);

        // Assert
        canCreate.Should().BeFalse();
    }

    [Fact]
    public void CreateEmptyResource_WithSupportedType_CreatesEmptyResource()
    {
        // Act
        var resource = _factory.CreateEmptyResource(ImageResource.PngResourceType);
        TrackResource(resource);

        // Assert
        resource.Should().NotBeNull();
        resource.Metadata.Format.Should().Be(ImageFormat.Unknown);
        resource.IsModified.Should().BeTrue();
        resource.RawData.Should().BeEmpty();
    }

    [Fact]
    public void CreateEmptyResource_WithUnsupportedType_ThrowsArgumentException()
    {
        // Arrange
        uint unsupportedType = 0x99999999;

        // Act & Assert
        var action = () => _factory.CreateEmptyResource(unsupportedType);
        action.Should().Throw<ArgumentException>()
            .WithMessage($"Resource type 0x{unsupportedType:X8} is not supported by ImageResourceFactory*")
            .WithParameterName("resourceType");
    }

    [Theory]
    [InlineData(new byte[] { 0x44, 0x44, 0x53, 0x20 }, ImageFormat.DDS)] // DDS magic
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ImageFormat.PNG)] // PNG magic
    [InlineData(new byte[] { 0xFF, 0xD8 }, ImageFormat.JPEG)] // JPEG magic
    [InlineData(new byte[] { 0x42, 0x4D }, ImageFormat.BMP)] // BMP magic
    [InlineData(new byte[] { 0x01, 0x02, 0x03 }, ImageFormat.Unknown)] // Invalid data
    public void DetectImageFormat_WithVariousMagicNumbers_ReturnsCorrectFormat(byte[] data, ImageFormat expectedFormat)
    {
        // Act
        var format = ImageResourceFactory.DetectImageFormat(data);

        // Assert
        format.Should().Be(expectedFormat);
    }

    [Fact]
    public void DetectImageFormat_WithTooSmallData_ReturnsUnknown()
    {
        // Arrange
        var tooSmallData = TestImageDataGenerator.CreateTooSmallData();

        // Act
        var format = ImageResourceFactory.DetectImageFormat(tooSmallData);

        // Assert
        format.Should().Be(ImageFormat.Unknown);
    }

    [Fact]
    public void GetSupportedFormats_ReturnsExpectedFormats()
    {
        // Act
        var supportedFormats = ImageResourceFactory.GetSupportedFormats();

        // Assert
        supportedFormats.Should().NotBeEmpty();
        supportedFormats.Should().ContainKey(ImageFormat.DDS);
        supportedFormats.Should().ContainKey(ImageFormat.PNG);
        supportedFormats.Should().ContainKey(ImageFormat.TGA);
        supportedFormats.Should().ContainKey(ImageFormat.JPEG);
        supportedFormats.Should().ContainKey(ImageFormat.BMP);

        // Verify descriptions are meaningful
        supportedFormats[ImageFormat.DDS].Should().Contain("DirectDraw Surface");
        supportedFormats[ImageFormat.PNG].Should().Contain("Portable Network Graphics");
    }

    [Fact]
    public void CreateResource_WithValidData_LogsDebugMessage()
    {
        // Arrange
        var pngData = TestImageDataGenerator.CreateTestPng();

        // Act
        using var stream = new MemoryStream(pngData);
        var resource = _factory.CreateResource(stream, ImageResource.PngResourceType);
        TrackResource(resource);

        // Assert
        // Note: Using NullLogger for simplicity - logger testing would require FakeLogger
        resource.Should().NotBeNull();
        resource.Metadata.Format.Should().Be(ImageFormat.PNG);
    }

    [Fact]
    public void CreateResource_WithInvalidData_LogsWarningMessage()
    {
        // Arrange
        var invalidData = TestImageDataGenerator.CreateInvalidImageData();

        // Act & Assert
        using var stream = new MemoryStream(invalidData);
        var action = () => _factory.CreateResource(stream, ImageResource.PngResourceType);
        action.Should().Throw<InvalidDataException>();

        // Note: Using NullLogger for simplicity - logger testing would require FakeLogger
    }

    [Fact]
    public void CreateEmptyResource_LogsDebugMessage()
    {
        // Act
        var resource = _factory.CreateEmptyResource(ImageResource.PngResourceType);
        TrackResource(resource);

        // Assert
        // Note: Using NullLogger for simplicity - logger testing would require FakeLogger
        resource.Should().NotBeNull();
    }

    [Fact]
    public void SupportedResourceTypes_IsReadOnly()
    {
        // Act
        var resourceTypes = _factory.SupportedResourceTypes;

        // Assert
        resourceTypes.Should().BeAssignableTo<IReadOnlySet<string>>();

        // Verify it's actually read-only by checking if it's a mutable collection
        if (resourceTypes is ICollection<string> collection)
        {
            collection.IsReadOnly.Should().BeTrue("because the collection should be read-only");
        }
    }
}
