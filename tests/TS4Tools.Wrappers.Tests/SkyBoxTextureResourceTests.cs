using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="SkyBoxTextureResource"/>.
///
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/SkyBoxTextureResource.cs
///
/// Format:
/// - Version: uint32
/// - TextureCount: int32
/// - Textures[]: SkyBoxTexture
///   - Type: int32 (enum)
///   - TGIKey: TGI (16 bytes)
///   - TexturePathLength: int32 (Unicode char count)
///   - TexturePath: string (Unicode)
/// </summary>
public class SkyBoxTextureResourceTests
{
    private static readonly ResourceKey TestKey = new(0x71A449C9, 0, 0);

    /// <summary>
    /// Creates valid SkyBoxTexture resource data.
    /// </summary>
    private static byte[] CreateValidData(uint version, params (SkyBoxTextureType type, ResourceKey tgi, string path)[] textures)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version
        writer.Write(version);

        // Texture count
        writer.Write(textures.Length);

        // Textures
        foreach (var (type, tgi, path) in textures)
        {
            // Type
            writer.Write((int)type);

            // TGI (Type, Group, Instance)
            writer.Write(tgi.ResourceType);
            writer.Write(tgi.ResourceGroup);
            writer.Write(tgi.Instance);

            // String length (char count)
            writer.Write(path.Length);

            // Unicode string
            writer.Write(System.Text.Encoding.Unicode.GetBytes(path));
        }

        return ms.ToArray();
    }

    [Fact]
    public void Parse_ValidData_ExtractsFields()
    {
        // Arrange
        var tgi = new ResourceKey(0x00B2D882, 0x12345678, 0xABCDEF0123456789);
        var data = CreateValidData(1, (SkyBoxTextureType.Clouds, tgi, "textures/clouds.dds"));

        // Act
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(1);
        resource.Textures.Should().HaveCount(1);
        resource.Textures[0].Type.Should().Be(SkyBoxTextureType.Clouds);
        resource.Textures[0].TgiKey.Should().Be(tgi);
        resource.Textures[0].TexturePath.Should().Be("textures/clouds.dds");
    }

    [Fact]
    public void Parse_MultipleTextures_ExtractsAll()
    {
        // Arrange
        var tgi1 = new ResourceKey(0x00B2D882, 0x00000000, 0x0000000000000001);
        var tgi2 = new ResourceKey(0x00B2D882, 0x00000000, 0x0000000000000002);
        var tgi3 = new ResourceKey(0x00B2D882, 0x00000000, 0x0000000000000003);
        var data = CreateValidData(2,
            (SkyBoxTextureType.Sun, tgi1, "sun.dds"),
            (SkyBoxTextureType.Moon, tgi2, "moon.dds"),
            (SkyBoxTextureType.Stars, tgi3, "stars.dds"));

        // Act
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(2);
        resource.Textures.Should().HaveCount(3);

        resource.Textures[0].Type.Should().Be(SkyBoxTextureType.Sun);
        resource.Textures[0].TgiKey.Should().Be(tgi1);
        resource.Textures[0].TexturePath.Should().Be("sun.dds");

        resource.Textures[1].Type.Should().Be(SkyBoxTextureType.Moon);
        resource.Textures[1].TgiKey.Should().Be(tgi2);
        resource.Textures[1].TexturePath.Should().Be("moon.dds");

        resource.Textures[2].Type.Should().Be(SkyBoxTextureType.Stars);
        resource.Textures[2].TgiKey.Should().Be(tgi3);
        resource.Textures[2].TexturePath.Should().Be("stars.dds");
    }

    [Fact]
    public void Parse_EmptyList_ExtractsEmptyArray()
    {
        // Arrange - 0 textures
        var data = CreateValidData(1);

        // Act
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Assert
        resource.Version.Should().Be(1);
        resource.Textures.Should().BeEmpty();
    }

    [Fact]
    public void Parse_TruncatedData_ThrowsInvalidDataException()
    {
        // Arrange - only 4 bytes (missing texture count)
        var data = new byte[] { 0x01, 0x00, 0x00, 0x00 };

        // Act
        Action act = () => _ = new SkyBoxTextureResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Fact]
    public void Parse_InvalidTextureCount_ThrowsInvalidDataException()
    {
        // Arrange - negative texture count
        var data = new byte[]
        {
            0x01, 0x00, 0x00, 0x00, // Version
            0xFF, 0xFF, 0xFF, 0xFF  // TextureCount = -1
        };

        // Act
        Action act = () => _ = new SkyBoxTextureResource(TestKey, data);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*Invalid texture count*");
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesData()
    {
        // Arrange
        var tgi = new ResourceKey(0x00B2D882, 0x12345678, 0x0123456789ABCDEF);
        var data = CreateValidData(5, (SkyBoxTextureType.CubeMap, tgi, "cubemap.dds"));
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Act
        var serialized = resource.Data.ToArray();

        // Assert
        serialized.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void Serialize_ModifiedTextures_ProducesValidData()
    {
        // Arrange
        var tgi = new ResourceKey(0x00B2D882, 0x00000000, 0x0000000000000001);
        var data = CreateValidData(1, (SkyBoxTextureType.Clouds, tgi, "old.dds"));
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Act - modify
        var newTgi = new ResourceKey(0x00B2D882, 0x00000000, 0x0000000000000099);
        resource.Textures = [new SkyBoxTexture { Type = SkyBoxTextureType.Sun, TgiKey = newTgi, TexturePath = "new.dds" }];

        var serialized = resource.Data;
        var reloaded = new SkyBoxTextureResource(TestKey, serialized);

        // Assert
        reloaded.Textures.Should().HaveCount(1);
        reloaded.Textures[0].Type.Should().Be(SkyBoxTextureType.Sun);
        reloaded.Textures[0].TgiKey.Should().Be(newTgi);
        reloaded.Textures[0].TexturePath.Should().Be("new.dds");
    }

    [Fact]
    public void Textures_Set_MarksAsDirty()
    {
        // Arrange
        var tgi = new ResourceKey(0x00B2D882, 0x00000000, 0x0000000000000001);
        var data = CreateValidData(1, (SkyBoxTextureType.Clouds, tgi, "test.dds"));
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Act
        resource.Textures = [new SkyBoxTexture { Type = SkyBoxTextureType.Moon, TgiKey = tgi, TexturePath = "moon.dds" }];

        // Assert
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Version_Set_MarksAsDirty()
    {
        // Arrange
        var data = CreateValidData(1);
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Act
        resource.Version = 99;

        // Assert
        resource.Version.Should().Be(99);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void EmptyData_InitializesDefaults()
    {
        // Arrange & Act
        var resource = new SkyBoxTextureResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.Version.Should().Be(1);
        resource.Textures.Should().BeEmpty();
    }

    [Theory]
    [InlineData(SkyBoxTextureType.Clouds)]
    [InlineData(SkyBoxTextureType.Sun)]
    [InlineData(SkyBoxTextureType.SunHalo)]
    [InlineData(SkyBoxTextureType.Moon)]
    [InlineData(SkyBoxTextureType.Stars)]
    [InlineData(SkyBoxTextureType.CubeMap)]
    [InlineData(SkyBoxTextureType.None)]
    public void Parse_AllTextureTypes_ParsesCorrectly(SkyBoxTextureType expectedType)
    {
        // Arrange
        var tgi = new ResourceKey(0x00B2D882, 0, 0);
        var data = CreateValidData(1, (expectedType, tgi, "test.dds"));

        // Act
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Assert
        resource.Textures[0].Type.Should().Be(expectedType);
    }

    [Fact]
    public void SkyBoxTexture_Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var tgi = new ResourceKey(0x00B2D882, 0x12345678, 0xABCDEF0123456789);
        var texture1 = new SkyBoxTexture { Type = SkyBoxTextureType.Clouds, TgiKey = tgi, TexturePath = "test.dds" };
        var texture2 = new SkyBoxTexture { Type = SkyBoxTextureType.Clouds, TgiKey = tgi, TexturePath = "test.dds" };

        // Act & Assert
        texture1.Equals(texture2).Should().BeTrue();
        texture1.GetHashCode().Should().Be(texture2.GetHashCode());
    }

    [Fact]
    public void SkyBoxTexture_Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var tgi = new ResourceKey(0x00B2D882, 0, 0);
        var texture1 = new SkyBoxTexture { Type = SkyBoxTextureType.Clouds, TgiKey = tgi, TexturePath = "test.dds" };
        var texture2 = new SkyBoxTexture { Type = SkyBoxTextureType.Sun, TgiKey = tgi, TexturePath = "test.dds" };

        // Act & Assert
        texture1.Equals(texture2).Should().BeFalse();
    }

    [Fact]
    public void Parse_UnicodeTexturePath_ParsesCorrectly()
    {
        // Arrange - Unicode characters in path
        var tgi = new ResourceKey(0x00B2D882, 0, 0);
        var unicodePath = "textures/skybox_\u00E9toile.dds"; // French "étoile" (star)
        var data = CreateValidData(1, (SkyBoxTextureType.Stars, tgi, unicodePath));

        // Act
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Assert
        resource.Textures[0].TexturePath.Should().Be(unicodePath);
    }

    [Fact]
    public void Parse_EmptyTexturePath_ParsesCorrectly()
    {
        // Arrange
        var tgi = new ResourceKey(0x00B2D882, 0, 0);
        var data = CreateValidData(1, (SkyBoxTextureType.Clouds, tgi, ""));

        // Act
        var resource = new SkyBoxTextureResource(TestKey, data);

        // Assert
        resource.Textures[0].TexturePath.Should().BeEmpty();
    }
}
