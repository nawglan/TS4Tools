namespace TS4Tools.Resources.Effects.Tests;

public sealed class EffectResourceTests : IDisposable
{
    private readonly EffectResource _effectResource;

    public EffectResourceTests()
    {
        _effectResource = new EffectResource();
    }

    public void Dispose()
    {
        _effectResource?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var resource = new EffectResource();

        // Assert
        resource.Should().NotBeNull();
        resource.EffectType.Should().Be(EffectType.None);
        resource.BlendMode.Should().Be(BlendMode.Normal);
        resource.IsEnabled.Should().BeTrue();
        resource.Parameters.Should().NotBeNull().And.BeEmpty();
        resource.Textures.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new EffectResource(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void Constructor_WithStream_ShouldInitializeFromStream()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        var resource = new EffectResource(stream);
        resource.Should().NotBeNull();
    }

    [Fact]
    public void EffectType_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedType = EffectType.Particle;

        // Act
        _effectResource.EffectType = expectedType;

        // Assert
        _effectResource.EffectType.Should().Be(expectedType);
    }

    [Fact]
    public void BlendMode_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var expectedBlendMode = BlendMode.Additive;

        // Act
        _effectResource.BlendMode = expectedBlendMode;

        // Assert
        _effectResource.BlendMode.Should().Be(expectedBlendMode);
    }

    [Fact]
    public void Parameters_WhenInitialized_ShouldBeEmpty()
    {
        // Arrange & Act & Assert
        _effectResource.Parameters.Should().NotBeNull();
        _effectResource.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Textures_WhenInitialized_ShouldBeEmpty()
    {
        // Arrange & Act & Assert
        _effectResource.Textures.Should().NotBeNull();
        _effectResource.Textures.Should().BeEmpty();
    }

    [Fact]
    public void AddParameter_WithValidParameter_ShouldAddToCollection()
    {
        // Arrange
        var parameter = new EffectParameter("TestParam", "float", 1.0f);

        // Act
        _effectResource.AddParameter(parameter);

        // Assert
        _effectResource.Parameters.Should().Contain(parameter);
        _effectResource.Parameters.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveParameter_WithExistingParameter_ShouldRemoveFromCollection()
    {
        // Arrange
        var parameter = new EffectParameter("TestParam", "float", 1.0f);
        _effectResource.AddParameter(parameter);

        // Act
        var result = _effectResource.RemoveParameter(parameter);

        // Assert
        result.Should().BeTrue();
        _effectResource.Parameters.Should().NotContain(parameter);
        _effectResource.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void RemoveParameter_WithNonExistingParameter_ShouldReturnFalse()
    {
        // Arrange
        var parameter = new EffectParameter("TestParam", "float", 1.0f);

        // Act
        var result = _effectResource.RemoveParameter(parameter);

        // Assert
        result.Should().BeFalse();
        _effectResource.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void AddTexture_WithValidTexture_ShouldAddToCollection()
    {
        // Arrange
        var texture = new EffectTexture("DiffuseMap", 0, 0);

        // Act
        _effectResource.AddTexture(texture);

        // Assert
        _effectResource.Textures.Should().Contain(texture);
        _effectResource.Textures.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTexture_WithExistingTexture_ShouldRemoveFromCollection()
    {
        // Arrange
        var texture = new EffectTexture("DiffuseMap", 0, 0);
        _effectResource.AddTexture(texture);

        // Act
        var result = _effectResource.RemoveTexture(texture);

        // Assert
        result.Should().BeTrue();
        _effectResource.Textures.Should().NotContain(texture);
        _effectResource.Textures.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTexture_WithNonExistingTexture_ShouldReturnFalse()
    {
        // Arrange
        var texture = new EffectTexture("DiffuseMap", 0, 0);

        // Act
        var result = _effectResource.RemoveTexture(texture);

        // Assert
        result.Should().BeFalse();
        _effectResource.Textures.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        await FluentActions.Awaiting(() => _effectResource.LoadFromStreamAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public async Task LoadFromStreamAsync_WithEmptyStream_ShouldHandleGracefully()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await FluentActions.Awaiting(() => _effectResource.LoadFromStreamAsync(stream))
            .Should().NotThrowAsync();
    }
}
