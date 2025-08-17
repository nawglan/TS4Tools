namespace TS4Tools.Resources.Images.Tests;

/// <summary>
/// Unit tests for DDS format structures and operations.
/// </summary>
public sealed class DdsFormatTests
{
    [Fact]
    public void DdsPixelFormat_CreateForFourCC_CreatesCorrectFormat()
    {
        // Act
        var pixelFormat = DdsPixelFormat.CreateForFourCC(DdsFourCC.DXT5);

        // Assert
        pixelFormat.Flags.Should().Be(DdsPixelFormatFlags.FourCC);
        pixelFormat.FourCC.Should().Be(DdsFourCC.DXT5);
        pixelFormat.RgbBitCount.Should().Be(0);
        pixelFormat.Size.Should().Be(32);
    }

    [Fact]
    public void DdsPixelFormat_CreateForRGBA32_CreatesCorrectFormat()
    {
        // Act
        var pixelFormat = DdsPixelFormat.CreateForRGBA32();

        // Assert
        pixelFormat.Flags.Should().Be(DdsPixelFormatFlags.RGBA);
        pixelFormat.FourCC.Should().Be(DdsFourCC.None);
        pixelFormat.RgbBitCount.Should().Be(32);
        pixelFormat.RedBitMask.Should().Be(0x00FF0000);
        pixelFormat.GreenBitMask.Should().Be(0x0000FF00);
        pixelFormat.BlueBitMask.Should().Be(0x000000FF);
        pixelFormat.AlphaBitMask.Should().Be(0xFF000000);
        pixelFormat.Size.Should().Be(32);
    }

    [Fact]
    public void DdsPixelFormat_CreateForRGB24_CreatesCorrectFormat()
    {
        // Act
        var pixelFormat = DdsPixelFormat.CreateForRGB24();

        // Assert
        pixelFormat.Flags.Should().Be(DdsPixelFormatFlags.RGB);
        pixelFormat.FourCC.Should().Be(DdsFourCC.None);
        pixelFormat.RgbBitCount.Should().Be(24);
        pixelFormat.RedBitMask.Should().Be(0x00FF0000);
        pixelFormat.GreenBitMask.Should().Be(0x0000FF00);
        pixelFormat.BlueBitMask.Should().Be(0x000000FF);
        pixelFormat.AlphaBitMask.Should().Be(0x00000000);
    }

    [Fact]
    public void DdsPixelFormat_CreateForLuminance8_CreatesCorrectFormat()
    {
        // Act
        var pixelFormat = DdsPixelFormat.CreateForLuminance8();

        // Assert
        pixelFormat.Flags.Should().Be(DdsPixelFormatFlags.Luminance);
        pixelFormat.FourCC.Should().Be(DdsFourCC.None);
        pixelFormat.RgbBitCount.Should().Be(8);
        pixelFormat.RedBitMask.Should().Be(0x000000FF);
        pixelFormat.GreenBitMask.Should().Be(0x00000000);
        pixelFormat.BlueBitMask.Should().Be(0x00000000);
        pixelFormat.AlphaBitMask.Should().Be(0x00000000);
    }

    [Fact]
    public void DdsHeader_CreateForRGBA32_CreatesValidHeader()
    {
        // Arrange
        uint width = 256;
        uint height = 128;
        uint mipMapCount = 9;

        // Act
        var header = DdsHeader.CreateForRGBA32(width, height, mipMapCount);

        // Assert
        header.Size.Should().Be(DdsHeader.HeaderSize);
        header.Flags.Should().HaveFlag(DdsFlags.Caps);
        header.Flags.Should().HaveFlag(DdsFlags.Height);
        header.Flags.Should().HaveFlag(DdsFlags.Width);
        header.Flags.Should().HaveFlag(DdsFlags.PixelFormat);
        header.Flags.Should().HaveFlag(DdsFlags.Pitch);
        header.Height.Should().Be(height);
        header.Width.Should().Be(width);
        header.PitchOrLinearSize.Should().Be(width * 4); // 4 bytes per pixel
        header.MipMapCount.Should().Be(mipMapCount);
        header.PixelFormat.Should().Be(DdsPixelFormat.CreateForRGBA32());
        header.Caps.Should().HaveFlag(DdsCaps.Texture);
        header.Caps.Should().HaveFlag(DdsCaps.MipMap);
    }

    [Fact]
    public void DdsHeader_CreateForRGBA32_WithoutMipmaps_DoesNotSetMipmapFlags()
    {
        // Arrange
        uint width = 64;
        uint height = 64;

        // Act
        var header = DdsHeader.CreateForRGBA32(width, height, 1);

        // Assert
        header.Flags.Should().NotHaveFlag(DdsFlags.MipMapCount);
        header.Caps.Should().NotHaveFlag(DdsCaps.MipMap);
        header.MipMapCount.Should().Be(1);
    }

    [Fact]
    public void DdsHeader_CreateForCompressed_CreatesValidHeader()
    {
        // Arrange
        uint width = 512;
        uint height = 256;
        var fourCC = DdsFourCC.DXT5;
        uint linearSize = 65536;
        uint mipMapCount = 10;

        // Act
        var header = DdsHeader.CreateForCompressed(width, height, fourCC, linearSize, mipMapCount);

        // Assert
        header.Size.Should().Be(DdsHeader.HeaderSize);
        header.Flags.Should().HaveFlag(DdsFlags.Caps);
        header.Flags.Should().HaveFlag(DdsFlags.Height);
        header.Flags.Should().HaveFlag(DdsFlags.Width);
        header.Flags.Should().HaveFlag(DdsFlags.PixelFormat);
        header.Flags.Should().HaveFlag(DdsFlags.LinearSize);
        header.Flags.Should().HaveFlag(DdsFlags.MipMapCount);
        header.Height.Should().Be(height);
        header.Width.Should().Be(width);
        header.PitchOrLinearSize.Should().Be(linearSize);
        header.MipMapCount.Should().Be(mipMapCount);
        header.PixelFormat.Should().Be(DdsPixelFormat.CreateForFourCC(fourCC));
        header.Caps.Should().HaveFlag(DdsCaps.Texture);
        header.Caps.Should().HaveFlag(DdsCaps.MipMap);
    }

    [Fact]
    public void DdsHeader_CreateForCompressed_WithoutMipmaps_DoesNotSetMipmapFlags()
    {
        // Arrange
        uint width = 128;
        uint height = 128;
        var fourCC = DdsFourCC.DXT1;
        uint linearSize = 8192;

        // Act
        var header = DdsHeader.CreateForCompressed(width, height, fourCC, linearSize, 1);

        // Assert
        header.Flags.Should().NotHaveFlag(DdsFlags.MipMapCount);
        header.Caps.Should().NotHaveFlag(DdsCaps.MipMap);
        header.MipMapCount.Should().Be(1);
    }

    [Theory]
    [InlineData(DdsFourCC.DXT1)]
    [InlineData(DdsFourCC.DXT3)]
    [InlineData(DdsFourCC.DXT5)]
    [InlineData(DdsFourCC.DST1)]
    [InlineData(DdsFourCC.DST3)]
    [InlineData(DdsFourCC.DST5)]
    [InlineData(DdsFourCC.ATI1)]
    [InlineData(DdsFourCC.ATI2)]
    public void DdsFourCC_AllValues_HaveExpectedEnumValues(DdsFourCC fourCC)
    {
        // Act & Assert - Just verifying the enum values are properly defined
        fourCC.Should().BeDefined();
        ((uint)fourCC).Should().NotBe(0); // None is 0, all others should be non-zero
    }

    [Theory]
    [InlineData(DdsPixelFormatFlags.AlphaPixels)]
    [InlineData(DdsPixelFormatFlags.Alpha)]
    [InlineData(DdsPixelFormatFlags.FourCC)]
    [InlineData(DdsPixelFormatFlags.RGB)]
    [InlineData(DdsPixelFormatFlags.RGBA)]
    [InlineData(DdsPixelFormatFlags.Luminance)]
    [InlineData(DdsPixelFormatFlags.LuminanceAlpha)]
    public void DdsPixelFormatFlags_AllValues_HaveExpectedEnumValues(DdsPixelFormatFlags flags)
    {
        // Act & Assert - Just verifying the enum values are properly defined
        flags.Should().BeDefined();
    }

    [Fact]
    public void DdsPixelFormatFlags_RGBA_CombinesRGBAndAlphaPixels()
    {
        // Assert
        DdsPixelFormatFlags.RGBA.Should().Be(DdsPixelFormatFlags.RGB | DdsPixelFormatFlags.AlphaPixels);
    }

    [Fact]
    public void DdsPixelFormatFlags_LuminanceAlpha_CombinesLuminanceAndAlphaPixels()
    {
        // Assert
        DdsPixelFormatFlags.LuminanceAlpha.Should().Be(DdsPixelFormatFlags.Luminance | DdsPixelFormatFlags.AlphaPixels);
    }

    [Theory]
    [InlineData(DdsFlags.Caps)]
    [InlineData(DdsFlags.Height)]
    [InlineData(DdsFlags.Width)]
    [InlineData(DdsFlags.Pitch)]
    [InlineData(DdsFlags.PixelFormat)]
    [InlineData(DdsFlags.MipMapCount)]
    [InlineData(DdsFlags.LinearSize)]
    [InlineData(DdsFlags.Depth)]
    public void DdsFlags_AllValues_HaveExpectedEnumValues(DdsFlags flags)
    {
        // Act & Assert - Just verifying the enum values are properly defined
        flags.Should().BeDefined();
    }

    [Theory]
    [InlineData(DdsCaps.Complex)]
    [InlineData(DdsCaps.Texture)]
    [InlineData(DdsCaps.MipMap)]
    public void DdsCaps_AllValues_HaveExpectedEnumValues(DdsCaps caps)
    {
        // Act & Assert - Just verifying the enum values are properly defined
        caps.Should().BeDefined();
    }

    [Theory]
    [InlineData(DdsCaps2.Cubemap)]
    [InlineData(DdsCaps2.CubemapPositiveX)]
    [InlineData(DdsCaps2.CubemapNegativeX)]
    [InlineData(DdsCaps2.CubemapPositiveY)]
    [InlineData(DdsCaps2.CubemapNegativeY)]
    [InlineData(DdsCaps2.CubemapPositiveZ)]
    [InlineData(DdsCaps2.CubemapNegativeZ)]
    [InlineData(DdsCaps2.Volume)]
    public void DdsCaps2_AllValues_HaveExpectedEnumValues(DdsCaps2 caps2)
    {
        // Act & Assert - Just verifying the enum values are properly defined
        caps2.Should().BeDefined();
    }

    [Fact]
    public void DdsHeader_Constants_HaveExpectedValues()
    {
        // Assert
        DdsHeader.DdsMagic.Should().Be(0x20534444); // "DDS " in little-endian
        DdsHeader.HeaderSize.Should().Be(124);
    }

    [Fact]
    public void DdsPixelFormat_DefaultValues_AreCorrect()
    {
        // Act
        var pixelFormat = new DdsPixelFormat();

        // Assert
        pixelFormat.Size.Should().Be(32);
        pixelFormat.Flags.Should().Be(0);
        pixelFormat.FourCC.Should().Be(DdsFourCC.None);
        pixelFormat.RgbBitCount.Should().Be(0);
        pixelFormat.RedBitMask.Should().Be(0);
        pixelFormat.GreenBitMask.Should().Be(0);
        pixelFormat.BlueBitMask.Should().Be(0);
        pixelFormat.AlphaBitMask.Should().Be(0);
    }

    [Fact]
    public void DdsHeader_DefaultValues_AreCorrect()
    {
        // Act
        var header = new DdsHeader();

        // Assert
        header.Size.Should().Be(DdsHeader.HeaderSize);
        header.Flags.Should().Be(0);
        header.Height.Should().Be(0);
        header.Width.Should().Be(0);
        header.PitchOrLinearSize.Should().Be(0);
        header.Depth.Should().Be(0);
        header.MipMapCount.Should().Be(0);
        header.Reserved1.Should().NotBeNull();
        header.PixelFormat.Should().Be(new DdsPixelFormat());
        header.Caps.Should().Be(0);
        header.Caps2.Should().Be(0);
        header.Caps3.Should().Be(0);
        header.Caps4.Should().Be(0);
        header.Reserved2.Should().Be(0);
    }

    [Fact]
    public void DdsPixelFormat_RecordStruct_SupportsValueSemantics()
    {
        // Arrange
        var format1 = DdsPixelFormat.CreateForRGBA32();
        var format2 = DdsPixelFormat.CreateForRGBA32();
        var format3 = DdsPixelFormat.CreateForRGB24();

        // Assert
        format1.Should().Be(format2); // Same configuration should be equal
        format1.Should().NotBe(format3); // Different configuration should not be equal
        format1.GetHashCode().Should().Be(format2.GetHashCode()); // Same hash code for equal objects
    }

    [Fact]
    public void DdsHeader_RecordStruct_SupportsValueSemantics()
    {
        // Arrange
        var header1 = DdsHeader.CreateForRGBA32(256, 256, 1);
        var header2 = DdsHeader.CreateForRGBA32(256, 256, 1);
        var header3 = DdsHeader.CreateForRGBA32(128, 128, 1);

        // Assert
        header1.Should().Be(header2); // Same configuration should be equal
        header1.Should().NotBe(header3); // Different configuration should not be equal
        header1.GetHashCode().Should().Be(header2.GetHashCode()); // Same hash code for equal objects
    }
}
