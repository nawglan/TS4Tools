using FluentAssertions;
using Xunit;

namespace TS4Tools.Resources.Visual.Tests;

public sealed class MaterialResourceTests : IDisposable
{
    private readonly MaterialResource _materialResource;

    public MaterialResourceTests()
    {
        _materialResource = new MaterialResource(
            new ResourceKey(0x12345678, 0x87654321, 0x11111111),
            "TestMaterial"
        );
    }

    public void Dispose()
    {
        _materialResource?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        const string materialName = "TestMaterial";

        // Act
        using var resource = new MaterialResource(key, materialName);

        // Assert
        resource.Name.Should().Be(materialName);
        resource.Type.Should().Be(MaterialType.Standard);
        resource.Key.Should().Be(key);
        resource.Properties.Should().NotBeNull().And.BeEmpty();
        resource.Textures.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Constructor_WithMaterialType_ShouldSetType()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);
        const string materialName = "MetallicMaterial";
        const MaterialType materialType = MaterialType.Metal;

        // Act
        using var resource = new MaterialResource(key, materialName, materialType);

        // Assert
        resource.Name.Should().Be(materialName);
        resource.Type.Should().Be(materialType);
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new MaterialResource(null!, "TestMaterial"));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new MaterialResource(key, null!));
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var key = new ResourceKey(0x12345678, 0x87654321, 0x11111111);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new MaterialResource(key, ""));
        Assert.Equal("name", ex.ParamName);
    }

    #endregion

    #region Property Management Tests

    [Fact]
    public void SetProperty_WithValidProperty_ShouldAddProperty()
    {
        // Arrange
        const string propertyName = "Roughness";
        const float propertyValue = 0.5f;

        // Act
        _materialResource.SetProperty(propertyName, propertyValue);

        // Assert
        _materialResource.Properties.Should().ContainKey(propertyName);
        var property = _materialResource.GetProperty(propertyName);
        property.Should().NotBeNull();
        property!.Value.Should().Be(propertyValue);
    }

    [Fact]
    public void GetProperty_WithExistingProperty_ShouldReturnProperty()
    {
        // Arrange
        const string propertyName = "Metallic";
        const float propertyValue = 0.8f;
        _materialResource.SetProperty(propertyName, propertyValue);

        // Act
        var result = _materialResource.GetProperty(propertyName);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(propertyValue);
    }

    [Fact]
    public void GetProperty_WithNonExistentProperty_ShouldReturnNull()
    {
        // Act
        var result = _materialResource.GetProperty("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RemoveProperty_WithExistingProperty_ShouldReturnTrue()
    {
        // Arrange
        const string propertyName = "TestProperty";
        _materialResource.SetProperty(propertyName, 1.0f);

        // Act
        var result = _materialResource.RemoveProperty(propertyName);

        // Assert
        result.Should().BeTrue();
        _materialResource.GetProperty(propertyName).Should().BeNull();
    }

    #endregion

    #region Texture Management Tests

    [Fact]
    public void AddTexture_WithValidTexture_ShouldAddToCollection()
    {
        // Arrange
        var textureKey = new ResourceKey(0x12345678, 0x87654321, 0x22222222);
        var texture = new MaterialTexture(textureKey, MaterialTextureType.Diffuse);

        // Act
        _materialResource.AddTexture(texture);

        // Assert
        _materialResource.Textures.Should().HaveCount(1);
        _materialResource.Textures.Should().Contain(texture);
    }

    [Fact]
    public void RemoveTexture_WithExistingTexture_ShouldReturnTrue()
    {
        // Arrange
        var textureKey = new ResourceKey(0x12345678, 0x87654321, 0x22222222);
        var texture = new MaterialTexture(textureKey, MaterialTextureType.Normal);
        _materialResource.AddTexture(texture);

        // Act
        var result = _materialResource.RemoveTexture(texture);

        // Assert
        result.Should().BeTrue();
        _materialResource.Textures.Should().NotContain(texture);
    }

    [Fact]
    public void ClearTextures_ShouldRemoveAllTextures()
    {
        // Arrange
        var texture1 = new MaterialTexture(new ResourceKey(0x12345678, 0x87654321, 0x22222222), MaterialTextureType.Diffuse);
        var texture2 = new MaterialTexture(new ResourceKey(0x12345678, 0x87654321, 0x33333333), MaterialTextureType.Normal);
        _materialResource.AddTexture(texture1);
        _materialResource.AddTexture(texture2);

        // Act
        _materialResource.ClearTextures();

        // Assert
        _materialResource.Textures.Should().BeEmpty();
    }

    #endregion

    #region IResource Implementation Tests

    [Fact]
    public void Stream_WhenAccessed_ShouldNotBeNull()
    {
        // Act & Assert
        _materialResource.Stream.Should().NotBeNull();
    }

    [Fact]
    public void AsBytes_WhenAccessed_ShouldReturnByteArray()
    {
        // Act
        var bytes = _materialResource.AsBytes;

        // Assert
        bytes.Should().NotBeNull();
        bytes.Should().BeOfType<byte[]>();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Validate_WithValidMaterial_ShouldReturnNoIssues()
    {
        // Act
        var issues = _materialResource.Validate();

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Clone_WithNewKey_ShouldCreateCopy()
    {
        // Arrange
        var newKey = new ResourceKey(0x87654321, 0x12345678, 0x44444444);
        _materialResource.SetProperty("TestProp", 0.7f);

        // Act
        using var cloned = _materialResource.Clone(newKey);

        // Assert
        cloned.Should().NotBeNull();
        cloned.Key.Should().Be(newKey);
        cloned.Name.Should().Be(_materialResource.Name);
        cloned.Type.Should().Be(_materialResource.Type);
        cloned.Properties.Should().HaveCount(_materialResource.Properties.Count);
    }

    #endregion
}
