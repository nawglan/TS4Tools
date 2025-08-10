namespace TS4Tools.Resources.Visual.Tests;

public class VisualResourceFactoriesTests
{
    #region MaskResourceFactory Tests

    [Fact]
    public void MaskResourceFactory_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var factory = new MaskResourceFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
        factory.SupportedResourceTypes.Should().Contain("MASK");
    }

    [Fact]
    public async Task MaskResourceFactory_CreateResourceAsync_WithValidApiVersion_ShouldReturnMaskResource()
    {
        // Arrange
        var factory = new MaskResourceFactory();
        const int apiVersion = 1;

        // Act
        var resource = await factory.CreateResourceAsync(apiVersion);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<MaskResource>();
        resource.Width.Should().Be(100);
        resource.Height.Should().Be(100);
    }

    [Fact]
    public async Task MaskResourceFactory_CreateResourceAsync_WithStream_ShouldReturnMaskResource()
    {
        // Arrange
        var factory = new MaskResourceFactory();
        const int apiVersion = 1;
        using var stream = new MemoryStream(new byte[1000]);

        // Act
        var resource = await factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<MaskResource>();
    }

    #endregion

    #region ThumbnailResourceFactory Tests

    [Fact]
    public void ThumbnailResourceFactory_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var factory = new ThumbnailResourceFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
        factory.SupportedResourceTypes.Should().Contain("THUM");
    }

    [Fact]
    public async Task ThumbnailResourceFactory_CreateResourceAsync_WithValidApiVersion_ShouldReturnThumbnailResource()
    {
        // Arrange
        var factory = new ThumbnailResourceFactory();
        const int apiVersion = 1;

        // Act
        var resource = await factory.CreateResourceAsync(apiVersion);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<ThumbnailResource>();
        resource.Width.Should().Be(100);
        resource.Height.Should().Be(100);
        resource.Format.Should().Be(ThumbnailFormat.PNG);
    }

    [Fact]
    public async Task ThumbnailResourceFactory_CreateResourceAsync_WithStream_ShouldReturnThumbnailResource()
    {
        // Arrange
        var factory = new ThumbnailResourceFactory();
        const int apiVersion = 1;
        using var stream = new MemoryStream(new byte[100]);

        // Act
        var resource = await factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<ThumbnailResource>();
    }

    #endregion

    #region MaterialResourceFactory Tests

    [Fact]
    public void MaterialResourceFactory_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var factory = new MaterialResourceFactory();

        // Assert
        factory.Should().NotBeNull();
        factory.SupportedResourceTypes.Should().NotBeEmpty();
        factory.SupportedResourceTypes.Should().Contain("MATD");
    }

    [Fact]
    public async Task MaterialResourceFactory_CreateResourceAsync_WithValidApiVersion_ShouldReturnMaterialResource()
    {
        // Arrange
        var factory = new MaterialResourceFactory();
        const int apiVersion = 1;

        // Act
        var resource = await factory.CreateResourceAsync(apiVersion);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<MaterialResource>();
        resource.Name.Should().Be("DefaultMaterial");
        resource.Type.Should().Be(MaterialType.Standard);
    }

    [Fact]
    public async Task MaterialResourceFactory_CreateResourceAsync_WithStream_ShouldReturnMaterialResource()
    {
        // Arrange
        var factory = new MaterialResourceFactory();
        const int apiVersion = 1;
        using var stream = new MemoryStream(new byte[50]);

        // Act
        var resource = await factory.CreateResourceAsync(apiVersion, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<MaterialResource>();
    }

    [Fact]
    public async Task MaterialResourceFactory_CreateResourceAsync_WithUnsupportedApiVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = new MaterialResourceFactory();
        const int apiVersion = 999; // Unsupported version

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => factory.CreateResourceAsync(apiVersion));
        ex!.ParamName.Should().Be("apiVersion");
    }

    #endregion
}
