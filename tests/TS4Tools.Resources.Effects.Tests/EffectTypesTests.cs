namespace TS4Tools.Resources.Effects.Tests;

public class EffectTypesTests
{
    [Fact]
    public void EffectType_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<EffectType>().Should().Contain(new[]
        {
            EffectType.None,
            EffectType.Particle,
            EffectType.Light,
            EffectType.ScreenSpace,
            EffectType.Water,
            EffectType.Fire,
            EffectType.Smoke,
            EffectType.Magic,
            EffectType.Weather,
            EffectType.Atmospheric,
            EffectType.PostProcess
        });
    }

    [Fact]
    public void BlendMode_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<BlendMode>().Should().Contain(new[]
        {
            BlendMode.Normal,
            BlendMode.Additive,
            BlendMode.Multiply,
            BlendMode.Screen,
            BlendMode.Overlay
        });
    }

    [Fact]
    public void EffectParameter_WithValidValues_ShouldInitializeCorrectly()
    {
        // Arrange
        const string name = "TestParameter";
        const string type = "float";
        const float value = 3.14f;

        // Act
        var parameter = new EffectParameter(name, type, value);

        // Assert
        parameter.Name.Should().Be(name);
        parameter.Type.Should().Be(type);
        parameter.Value.Should().Be(value);
    }

    [Fact]
    public void EffectParameter_WithNullName_ShouldAllowNull()
    {
        // Arrange, Act & Assert
        var parameter = new EffectParameter(null!, "float", 1.0f);
        parameter.Name.Should().BeNull();
    }

    [Fact]
    public void EffectParameter_WithNullType_ShouldAllowNull()
    {
        // Arrange, Act & Assert
        var parameter = new EffectParameter("test", null!, 1.0f);
        parameter.Type.Should().BeNull();
    }

    [Fact]
    public void EffectParameter_WithNullValue_ShouldAllowNull()
    {
        // Arrange, Act & Assert
        var parameter = new EffectParameter("test", "float", null!);
        parameter.Value.Should().BeNull();
    }

    [Fact]
    public void EffectParameter_WithDifferentValueTypes_ShouldWork()
    {
        // Arrange & Act
        var floatParam = new EffectParameter("floatParam", "float", 1.5f);
        var intParam = new EffectParameter("intParam", "int", 42);
        var stringParam = new EffectParameter("stringParam", "string", "test");
        var boolParam = new EffectParameter("boolParam", "bool", true);

        // Assert
        floatParam.Value.Should().Be(1.5f);
        intParam.Value.Should().Be(42);
        stringParam.Value.Should().Be("test");
        boolParam.Value.Should().Be(true);
    }

    [Fact]
    public void EffectParameter_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var param1 = new EffectParameter("test", "float", 1.0f);
        var param2 = new EffectParameter("test", "float", 1.0f);
        var param3 = new EffectParameter("test", "float", 2.0f);
        var param4 = new EffectParameter("different", "float", 1.0f);

        // Assert
        param1.Should().Be(param2);
        param1.Should().NotBe(param3);
        param1.Should().NotBe(param4);
    }

    [Fact]
    public void EffectParameter_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var param1 = new EffectParameter("test", "float", 1.0f);
        var param2 = new EffectParameter("test", "float", 1.0f);

        // Assert
        param1.GetHashCode().Should().Be(param2.GetHashCode());
    }

    [Fact]
    public void EffectParameter_ToString_ShouldReturnMeaningfulString()
    {
        // Arrange
        var parameter = new EffectParameter("TestParam", "float", 3.14f);

        // Act
        var result = parameter.ToString();

        // Assert
        result.Should().Contain("TestParam");
        result.Should().Contain("float");
        result.Should().Contain("3.14");
    }

    [Fact]
    public void EffectTexture_WithValidValues_ShouldInitializeCorrectly()
    {
        // Arrange
        const string textureName = "DiffuseMap";
        const uint textureIndex = 0;
        const uint uvIndex = 1;

        // Act
        var texture = new EffectTexture(textureName, textureIndex, uvIndex);

        // Assert
        texture.TextureName.Should().Be(textureName);
        texture.TextureIndex.Should().Be(textureIndex);
        texture.UvIndex.Should().Be(uvIndex);
    }

    [Fact]
    public void EffectTexture_WithNullTextureName_ShouldAllowNull()
    {
        // Arrange, Act & Assert
        var texture = new EffectTexture(null!, 0, 0);
        texture.TextureName.Should().BeNull();
    }

    [Fact]
    public void EffectTexture_WithZeroIndices_ShouldWork()
    {
        // Arrange, Act & Assert
        var texture = new EffectTexture("test", 0, 0);
        texture.TextureIndex.Should().Be(0u);
        texture.UvIndex.Should().Be(0u);
    }

    [Fact]
    public void EffectTexture_WithMaxIndices_ShouldWork()
    {
        // Arrange, Act & Assert
        var texture = new EffectTexture("test", uint.MaxValue, uint.MaxValue);
        texture.TextureIndex.Should().Be(uint.MaxValue);
        texture.UvIndex.Should().Be(uint.MaxValue);
    }

    [Fact]
    public void EffectTexture_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var texture1 = new EffectTexture("DiffuseMap", 0, 0);
        var texture2 = new EffectTexture("DiffuseMap", 0, 0);
        var texture3 = new EffectTexture("NormalMap", 0, 0);
        var texture4 = new EffectTexture("DiffuseMap", 1, 0);

        // Assert
        texture1.Should().Be(texture2);
        texture1.Should().NotBe(texture3);
        texture1.Should().NotBe(texture4);
    }

    [Fact]
    public void EffectTexture_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var texture1 = new EffectTexture("DiffuseMap", 0, 0);
        var texture2 = new EffectTexture("DiffuseMap", 0, 0);

        // Assert
        texture1.GetHashCode().Should().Be(texture2.GetHashCode());
    }

    [Fact]
    public void EffectTexture_ToString_ShouldReturnMeaningfulString()
    {
        // Arrange
        var texture = new EffectTexture("DiffuseMap", 0, 1);

        // Act
        var result = texture.ToString();

        // Assert
        result.Should().Contain("DiffuseMap");
        result.Should().Contain("0");
        result.Should().Contain("1");
    }

    [Theory]
    [InlineData(EffectType.None)]
    [InlineData(EffectType.Particle)]
    [InlineData(EffectType.Light)]
    [InlineData(EffectType.ScreenSpace)]
    [InlineData(EffectType.Water)]
    [InlineData(EffectType.Fire)]
    [InlineData(EffectType.Smoke)]
    [InlineData(EffectType.Magic)]
    [InlineData(EffectType.Weather)]
    [InlineData(EffectType.Atmospheric)]
    [InlineData(EffectType.PostProcess)]
    public void EffectType_AllValues_ShouldBeDefined(EffectType effectType)
    {
        // Act & Assert
        Enum.IsDefined(effectType).Should().BeTrue();
    }

    [Theory]
    [InlineData(BlendMode.Normal)]
    [InlineData(BlendMode.Additive)]
    [InlineData(BlendMode.Multiply)]
    [InlineData(BlendMode.Screen)]
    [InlineData(BlendMode.Overlay)]
    public void BlendMode_AllValues_ShouldBeDefined(BlendMode blendMode)
    {
        // Act & Assert
        Enum.IsDefined(blendMode).Should().BeTrue();
    }

    [Fact]
    public void EffectType_ShouldStartWithNone()
    {
        // Assert
        ((int)EffectType.None).Should().Be(0);
    }

    [Fact]
    public void BlendMode_ShouldStartWithNormal()
    {
        // Assert
        ((int)BlendMode.Normal).Should().Be(0);
    }
}
