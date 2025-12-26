using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="DxtDecoder"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DdsSquish.cs
/// - DXT1 (BC1): 8 bytes per 4x4 block, 1-bit alpha
/// - DXT5 (BC3): 16 bytes per 4x4 block, interpolated alpha
/// - Block decompression produces RGBA32 output (4 bytes per pixel)
/// </summary>
public class DxtDecoderTests
{
    /// <summary>
    /// Creates a minimal DDS header with specified FourCC.
    /// </summary>
    private static byte[] CreateDdsHeader(uint fourCc, int width = 4, int height = 4)
    {
        var header = new byte[128];

        // Magic "DDS "
        header[0] = 0x44;
        header[1] = 0x44;
        header[2] = 0x53;
        header[3] = 0x20;

        // Header size (124)
        header[4] = 124;

        // Height (little-endian)
        header[12] = (byte)(height & 0xFF);
        header[13] = (byte)((height >> 8) & 0xFF);
        header[14] = (byte)((height >> 16) & 0xFF);
        header[15] = (byte)((height >> 24) & 0xFF);

        // Width (little-endian)
        header[16] = (byte)(width & 0xFF);
        header[17] = (byte)((width >> 8) & 0xFF);
        header[18] = (byte)((width >> 16) & 0xFF);
        header[19] = (byte)((width >> 24) & 0xFF);

        // Pixel format size (32)
        header[76] = 32;

        // Pixel format flags (fourCC)
        header[80] = 0x04;

        // FourCC (little-endian)
        header[84] = (byte)(fourCc & 0xFF);
        header[85] = (byte)((fourCc >> 8) & 0xFF);
        header[86] = (byte)((fourCc >> 16) & 0xFF);
        header[87] = (byte)((fourCc >> 24) & 0xFF);

        return header;
    }

    [Fact]
    public void GetFourCc_Dxt1Header_ReturnsDxt1()
    {
        // Arrange
        var header = CreateDdsHeader(DxtDecoder.FourCcDxt1);

        // Act
        var fourCc = DxtDecoder.GetFourCc(header);

        // Assert
        fourCc.Should().Be(DxtDecoder.FourCcDxt1);
    }

    [Fact]
    public void GetFourCc_Dxt5Header_ReturnsDxt5()
    {
        // Arrange
        var header = CreateDdsHeader(DxtDecoder.FourCcDxt5);

        // Act
        var fourCc = DxtDecoder.GetFourCc(header);

        // Assert
        fourCc.Should().Be(DxtDecoder.FourCcDxt5);
    }

    [Fact]
    public void GetDimensions_ValidHeader_ReturnsDimensions()
    {
        // Arrange
        var header = CreateDdsHeader(DxtDecoder.FourCcDxt1, 256, 128);

        // Act
        var (width, height) = DxtDecoder.GetDimensions(header);

        // Assert
        width.Should().Be(256);
        height.Should().Be(128);
    }

    [Fact]
    public void GetDimensions_TooShort_ReturnsZero()
    {
        // Arrange
        var shortData = new byte[10];

        // Act
        var (width, height) = DxtDecoder.GetDimensions(shortData);

        // Assert
        width.Should().Be(0);
        height.Should().Be(0);
    }

    [Fact]
    public void DecompressDds_Dxt1_ProducesCorrectSize()
    {
        // Arrange - Create a 4x4 DXT1 texture (one block)
        var header = CreateDdsHeader(DxtDecoder.FourCcDxt1, 4, 4);
        var blockData = new byte[8]; // One DXT1 block

        // Fill with a simple solid color block
        // Color0 = 0xFFFF (white in RGB565), Color1 = 0x0000 (black)
        // All pixels use color0 (index 0 = 0b00 for all pixels)
        blockData[0] = 0xFF; // color0 low
        blockData[1] = 0xFF; // color0 high (white)
        blockData[2] = 0x00; // color1 low
        blockData[3] = 0x00; // color1 high (black)
        blockData[4] = 0x00; // indices row 0 (all 0s)
        blockData[5] = 0x00; // indices row 1
        blockData[6] = 0x00; // indices row 2
        blockData[7] = 0x00; // indices row 3

        var ddsData = new byte[header.Length + blockData.Length];
        header.CopyTo(ddsData, 0);
        blockData.CopyTo(ddsData, header.Length);

        // Act
        var pixels = DxtDecoder.DecompressDds(ddsData);

        // Assert
        pixels.Should().NotBeNull();
        pixels!.Length.Should().Be(4 * 4 * 4); // 4x4 pixels, 4 bytes each (RGBA)
    }

    [Fact]
    public void DecompressDds_Dxt1SolidWhite_AllPixelsWhite()
    {
        // Arrange - Create a 4x4 DXT1 texture with solid white
        var header = CreateDdsHeader(DxtDecoder.FourCcDxt1, 4, 4);
        var blockData = new byte[8];

        // Color0 = 0xFFFF (white), Color1 = 0x0000, all indices = 0 (use color0)
        blockData[0] = 0xFF;
        blockData[1] = 0xFF;
        blockData[2] = 0x00;
        blockData[3] = 0x00;
        blockData[4] = 0x00;
        blockData[5] = 0x00;
        blockData[6] = 0x00;
        blockData[7] = 0x00;

        var ddsData = new byte[header.Length + blockData.Length];
        header.CopyTo(ddsData, 0);
        blockData.CopyTo(ddsData, header.Length);

        // Act
        var pixels = DxtDecoder.DecompressDds(ddsData);

        // Assert
        pixels.Should().NotBeNull();

        // Check first pixel is white (255, 255, 255, 255)
        pixels![0].Should().Be(255); // R
        pixels[1].Should().Be(255); // G
        pixels[2].Should().Be(255); // B
        pixels[3].Should().Be(255); // A
    }

    [Fact]
    public void DecompressDds_Dxt5_ProducesCorrectSize()
    {
        // Arrange - Create a 4x4 DXT5 texture (one block)
        var header = CreateDdsHeader(DxtDecoder.FourCcDxt5, 4, 4);
        var blockData = new byte[16]; // One DXT5 block

        // Simple alpha block + color block
        blockData[0] = 0xFF; // alpha0 = 255
        blockData[1] = 0x00; // alpha1 = 0
        // Alpha indices (all 0s = use alpha0)
        blockData[2] = 0x00;
        blockData[3] = 0x00;
        blockData[4] = 0x00;
        blockData[5] = 0x00;
        blockData[6] = 0x00;
        blockData[7] = 0x00;
        // Color block (white)
        blockData[8] = 0xFF;
        blockData[9] = 0xFF;
        blockData[10] = 0x00;
        blockData[11] = 0x00;
        blockData[12] = 0x00;
        blockData[13] = 0x00;
        blockData[14] = 0x00;
        blockData[15] = 0x00;

        var ddsData = new byte[header.Length + blockData.Length];
        header.CopyTo(ddsData, 0);
        blockData.CopyTo(ddsData, header.Length);

        // Act
        var pixels = DxtDecoder.DecompressDds(ddsData);

        // Assert
        pixels.Should().NotBeNull();
        pixels!.Length.Should().Be(4 * 4 * 4); // 4x4 pixels, 4 bytes each (RGBA)
    }

    [Fact]
    public void DecompressDds_LargerTexture_HandlesMultipleBlocks()
    {
        // Arrange - Create an 8x8 DXT1 texture (4 blocks)
        var header = CreateDdsHeader(DxtDecoder.FourCcDxt1, 8, 8);
        var blockData = new byte[8 * 4]; // Four DXT1 blocks

        // Fill all blocks with white
        for (var i = 0; i < 4; i++)
        {
            blockData[i * 8 + 0] = 0xFF;
            blockData[i * 8 + 1] = 0xFF;
            blockData[i * 8 + 2] = 0x00;
            blockData[i * 8 + 3] = 0x00;
        }

        var ddsData = new byte[header.Length + blockData.Length];
        header.CopyTo(ddsData, 0);
        blockData.CopyTo(ddsData, header.Length);

        // Act
        var pixels = DxtDecoder.DecompressDds(ddsData);

        // Assert
        pixels.Should().NotBeNull();
        pixels!.Length.Should().Be(8 * 8 * 4); // 8x8 pixels, 4 bytes each
    }

    [Fact]
    public void DecompressDds_UnsupportedFormat_ReturnsNull()
    {
        // Arrange - Create a header with unknown FourCC
        var header = CreateDdsHeader(0x12345678, 4, 4);
        var ddsData = new byte[header.Length + 8];
        header.CopyTo(ddsData, 0);

        // Act
        var pixels = DxtDecoder.DecompressDds(ddsData);

        // Assert
        pixels.Should().BeNull();
    }

    [Fact]
    public void DecompressDds_TooShort_ReturnsNull()
    {
        // Arrange
        var shortData = new byte[64];

        // Act
        var pixels = DxtDecoder.DecompressDds(shortData);

        // Assert
        pixels.Should().BeNull();
    }

    [Fact]
    public void DecompressDxt1_NonPowerOfTwo_HandlesEdgeBlocks()
    {
        // Arrange - Create a 5x5 texture (needs 2x2 blocks, partial edge coverage)
        var header = CreateDdsHeader(DxtDecoder.FourCcDxt1, 5, 5);
        var blockData = new byte[8 * 4]; // Four DXT1 blocks (2x2 grid)

        // Fill all blocks with white
        for (var i = 0; i < 4; i++)
        {
            blockData[i * 8 + 0] = 0xFF;
            blockData[i * 8 + 1] = 0xFF;
        }

        var ddsData = new byte[header.Length + blockData.Length];
        header.CopyTo(ddsData, 0);
        blockData.CopyTo(ddsData, header.Length);

        // Act
        var pixels = DxtDecoder.DecompressDds(ddsData);

        // Assert
        pixels.Should().NotBeNull();
        pixels!.Length.Should().Be(5 * 5 * 4); // 5x5 pixels, 4 bytes each
    }

    [Fact]
    public void FourCcConstants_AreCorrect()
    {
        // Assert
        DxtDecoder.FourCcDxt1.Should().Be(0x31545844); // "DXT1" in little-endian
        DxtDecoder.FourCcDxt5.Should().Be(0x35545844); // "DXT5" in little-endian
    }
}
