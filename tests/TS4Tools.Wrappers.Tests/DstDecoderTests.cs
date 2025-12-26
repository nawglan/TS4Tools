using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="DstDecoder"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/DSTResource.cs
/// - DST is a shuffled DDS format used by Sims 4 for texture optimization
/// - DST1 = shuffled DXT1, DST5 = shuffled DXT5
/// - The shuffle splits blocks into sections for better compression
/// </summary>
public class DstDecoderTests
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
    public void GetFourCc_Dst1Header_ReturnsDst1()
    {
        // Arrange
        var header = CreateDdsHeader(DstDecoder.FourCcDst1);

        // Act
        var fourCc = DstDecoder.GetFourCc(header);

        // Assert
        fourCc.Should().Be(DstDecoder.FourCcDst1);
    }

    [Fact]
    public void GetFourCc_Dst5Header_ReturnsDst5()
    {
        // Arrange
        var header = CreateDdsHeader(DstDecoder.FourCcDst5);

        // Act
        var fourCc = DstDecoder.GetFourCc(header);

        // Assert
        fourCc.Should().Be(DstDecoder.FourCcDst5);
    }

    [Fact]
    public void GetFourCc_TooShort_ReturnsZero()
    {
        // Arrange
        var shortData = new byte[50];

        // Act
        var fourCc = DstDecoder.GetFourCc(shortData);

        // Assert
        fourCc.Should().Be(0);
    }

    [Fact]
    public void IsDst_Dst1Header_ReturnsTrue()
    {
        // Arrange
        var header = CreateDdsHeader(DstDecoder.FourCcDst1);

        // Act & Assert
        DstDecoder.IsDst(header).Should().BeTrue();
    }

    [Fact]
    public void IsDst_Dst5Header_ReturnsTrue()
    {
        // Arrange
        var header = CreateDdsHeader(DstDecoder.FourCcDst5);

        // Act & Assert
        DstDecoder.IsDst(header).Should().BeTrue();
    }

    [Fact]
    public void IsDst_Dxt1Header_ReturnsFalse()
    {
        // Arrange
        var header = CreateDdsHeader(DstDecoder.FourCcDxt1);

        // Act & Assert
        DstDecoder.IsDst(header).Should().BeFalse();
    }

    [Fact]
    public void UnshuffleToDds_Dst1_ProducesValidDxt1()
    {
        // Arrange - Create a minimal DST1 file (header + 8 bytes for one block)
        var header = CreateDdsHeader(DstDecoder.FourCcDst1, 4, 4);

        // DST1 shuffled data: first halves of all blocks, then second halves
        // For one block: [half1_block1] [half2_block1]
        var shuffledData = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };

        var dstData = new byte[header.Length + shuffledData.Length];
        header.CopyTo(dstData, 0);
        shuffledData.CopyTo(dstData, header.Length);

        // Act
        var ddsData = DstDecoder.UnshuffleToDds(dstData);

        // Assert
        ddsData.Should().NotBeNull();
        ddsData!.Length.Should().Be(dstData.Length);

        // FourCC should be changed to DXT1
        var newFourCc = DxtDecoder.GetFourCc(ddsData);
        newFourCc.Should().Be(DstDecoder.FourCcDxt1);

        // Data should be interleaved: [half1, half2] for each block
        // Input: [11 22 33 44] [55 66 77 88]
        // Output: [11 22 33 44 55 66 77 88]
        var blockData = ddsData.AsSpan(128);
        blockData[0].Should().Be(0x11);
        blockData[1].Should().Be(0x22);
        blockData[2].Should().Be(0x33);
        blockData[3].Should().Be(0x44);
        blockData[4].Should().Be(0x55);
        blockData[5].Should().Be(0x66);
        blockData[6].Should().Be(0x77);
        blockData[7].Should().Be(0x88);
    }

    [Fact]
    public void UnshuffleToDds_NonDst_ReturnsNull()
    {
        // Arrange
        var header = CreateDdsHeader(DstDecoder.FourCcDxt1);
        var data = new byte[header.Length + 8];
        header.CopyTo(data, 0);

        // Act
        var result = DstDecoder.UnshuffleToDds(data);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UnshuffleToDds_TooShort_ReturnsNull()
    {
        // Arrange
        var shortData = new byte[64];

        // Act
        var result = DstDecoder.UnshuffleToDds(shortData);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FourCcConstants_AreCorrect()
    {
        // Assert - verify the constants match expected values
        DstDecoder.FourCcDst1.Should().Be(0x31545344); // "DST1" in little-endian
        DstDecoder.FourCcDst5.Should().Be(0x35545344); // "DST5" in little-endian
        DstDecoder.FourCcDxt1.Should().Be(0x31545844); // "DXT1" in little-endian
        DstDecoder.FourCcDxt5.Should().Be(0x35545844); // "DXT5" in little-endian
    }
}
