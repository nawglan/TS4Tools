using FluentAssertions;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for DDS decoding of DXT3 and uncompressed formats.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DdsFile.cs lines 396-473 (fromDDS_* conversions),
///   lines 233-340 (pixel format detection from bit masks).
/// - Expected values are hand-derived from the legacy bit manipulations (the legacy A1R5G5B5 path has a precedence bug,
///   documented in DdsHeader.ToArgb; the tests assert the intended result).
/// </summary>
public class DdsDecodeTests
{
    [Theory]
    [InlineData(DdsFormat.A8R8G8B8, new byte[] { 0x33, 0x22, 0x11, 0x44 }, new byte[] { 0x11, 0x22, 0x33, 0x44 })]
    [InlineData(DdsFormat.X8R8G8B8, new byte[] { 0x33, 0x22, 0x11, 0x44 }, new byte[] { 0x11, 0x22, 0x33, 0xFF })]
    [InlineData(DdsFormat.A8B8G8R8, new byte[] { 0x11, 0x22, 0x33, 0x44 }, new byte[] { 0x11, 0x22, 0x33, 0x44 })]
    [InlineData(DdsFormat.X8B8G8R8, new byte[] { 0x11, 0x22, 0x33, 0x44 }, new byte[] { 0x11, 0x22, 0x33, 0xFF })]
    public void Decode_32BitFormats(DdsFormat format, byte[] stored, byte[] expectedRgba)
    {
        var dds = DdsTestData.Build(format, 1, 1, stored);

        DxtDecoder.DecompressDds(dds).Should().Equal(expectedRgba);
        DxtDecoder.GetFormat(dds).Should().Be(format);
    }

    [Fact]
    public void Decode_R8G8B8_TwoByOne()
    {
        // Stored as B, G, R per pixel; row pitch is 6 bytes
        var dds = DdsTestData.Build(DdsFormat.R8G8B8, 2, 1, [0x33, 0x22, 0x11, 0x00, 0x00, 0xFF]);

        DxtDecoder.DecompressDds(dds).Should().Equal(0x11, 0x22, 0x33, 0xFF, 0xFF, 0x00, 0x00, 0xFF);
    }

    [Theory]
    [InlineData(0xF800, new byte[] { 255, 0, 0, 255 })]
    [InlineData(0x07E0, new byte[] { 0, 255, 0, 255 })]
    [InlineData(0x001F, new byte[] { 0, 0, 255, 255 })]
    [InlineData(0x0000, new byte[] { 0, 0, 0, 255 })]
    [InlineData(0xFFFF, new byte[] { 255, 255, 255, 255 })]
    public void Decode_R5G6B5(int value, byte[] expected)
    {
        var dds = DdsTestData.Build(DdsFormat.R5G6B5, 1, 1, [(byte)(value & 0xFF), (byte)(value >> 8)]);

        DxtDecoder.DecompressDds(dds).Should().Equal(expected);
    }

    [Theory]
    [InlineData(0xFC00, new byte[] { 255, 0, 0, 255 })] // A=1, R=31
    [InlineData(0x7C00, new byte[] { 255, 0, 0, 0 })]   // A=0, R=31
    [InlineData(0x83E0, new byte[] { 0, 255, 0, 255 })]
    [InlineData(0x801F, new byte[] { 0, 0, 255, 255 })]
    public void Decode_A1R5G5B5(int value, byte[] expected)
    {
        var dds = DdsTestData.Build(DdsFormat.A1R5G5B5, 1, 1, [(byte)(value & 0xFF), (byte)(value >> 8)]);

        DxtDecoder.DecompressDds(dds).Should().Equal(expected);
    }

    [Fact]
    public void Decode_A4R4G4B4_ExpandsNibblesBySpreading()
    {
        // A=0xF R=0x1 G=0x8 B=0x0 -> 0xFF, 0x11, 0x88, 0x00
        var dds = DdsTestData.Build(DdsFormat.A4R4G4B4, 1, 1, [0x80, 0xF1]);

        DxtDecoder.DecompressDds(dds).Should().Equal(0x11, 0x88, 0x00, 0xFF);
    }

    [Fact]
    public void Decode_A8L8_ReplicatesLuminance()
    {
        var dds = DdsTestData.Build(DdsFormat.A8L8, 1, 1, [0x80, 0x40]);

        DxtDecoder.DecompressDds(dds).Should().Equal(0x80, 0x80, 0x80, 0x40);
        DxtDecoder.GetFormat(dds).Should().Be(DdsFormat.A8L8);
    }

    [Fact]
    public void Decode_UncompressedRowPitch_UsesDeclaredPitchOverWidth()
    {
        // 3 pixels of R8G8B8 (9 bytes) but the file declares a 12 byte pitch (padded rows)
        var bytes = new List<byte>(DdsTestData.Build(DdsFormat.R8G8B8, 3, 2, []));
        bytes.RemoveRange(128, bytes.Count - 128);
        var span = bytes.ToArray();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(span.AsSpan(20), 12);
        var body = new byte[24];
        body[0] = 0x03; body[1] = 0x02; body[2] = 0x01;           // row 0, pixel 0: R=1 G=2 B=3
        body[12] = 0x06; body[13] = 0x05; body[14] = 0x04;        // row 1, pixel 0: R=4 G=5 B=6
        var dds = span.Concat(body).ToArray();

        var rgba = DxtDecoder.DecompressDds(dds)!;

        rgba.AsSpan(0, 4).ToArray().Should().Equal(1, 2, 3, 255);
        rgba.AsSpan(12, 4).ToArray().Should().Equal(4, 5, 6, 255);
    }

    [Fact]
    public void Decode_UncompressedTruncatedData_ReturnsNull()
    {
        var dds = DdsTestData.Build(DdsFormat.A8R8G8B8, 4, 4, new byte[10]);

        DxtDecoder.DecompressDds(dds).Should().BeNull();
    }

    [Fact]
    public void Decode_UncompressedWithExtraMipData_ReadsOnlyBaseLevel()
    {
        var dds = DdsTestData.Build(DdsFormat.A8R8G8B8, 2, 1, new byte[8 + 4], mips: 2);

        DxtDecoder.DecompressDds(dds)!.Length.Should().Be(8);
    }

    [Fact]
    public void Decode_Dxt3_ExplicitAlphaNibbles()
    {
        var block = new byte[16];
        // alpha nibbles: pixel0 = 0xA, pixel1 = 0x5, pixel2..15 = 0xF
        block[0] = 0x5A;
        for (var i = 1; i < 8; i++)
            block[i] = 0xFF;
        // colour: c0 = red (0xF800), c1 = black, all indices 0
        block[8] = 0x00; block[9] = 0xF8;

        var rgba = DxtDecoder.DecompressDds(DdsTestData.Build(DdsFormat.Dxt3, 4, 4, block))!;

        rgba[..4].Should().Equal(255, 0, 0, 0xAA);
        rgba[4..8].Should().Equal(255, 0, 0, 0x55);
        rgba[8..12].Should().Equal(255, 0, 0, 0xFF);
        rgba[60..64].Should().Equal(255, 0, 0, 0xFF);
    }

    [Fact]
    public void Decode_Dxt3_IgnoresColourOrderAndAlwaysUsesFourColours()
    {
        // c0 < c1 would be 3-colour mode in DXT1, but DXT3 always has four colours.
        var block = new byte[16];
        Array.Fill(block, (byte)0xFF, 0, 8);
        block[8] = 0x00; block[9] = 0x00;    // c0 = black
        block[10] = 0x1F; block[11] = 0x00;  // c1 = blue (0x001F)
        block[12] = 0xFF; block[13] = 0xFF; block[14] = 0xFF; block[15] = 0xFF; // every index = 3 (2/3 c1)

        var rgba = DxtDecoder.DecompressDds(DdsTestData.Build(DdsFormat.Dxt3, 4, 4, block))!;

        rgba[2].Should().BeGreaterThan(100, "index 3 is 1/3 c0 + 2/3 c1, not transparent black");
        rgba[3].Should().Be(0xFF);
    }

    [Fact]
    public void Decode_NonMultipleOfFourDimensions_Dxt3()
    {
        var block = new byte[16];
        Array.Fill(block, (byte)0xFF, 0, 8);
        block[8] = 0x00; block[9] = 0xF8;

        var rgba = DxtDecoder.DecompressDds(DdsTestData.Build(DdsFormat.Dxt3, 3, 2, block))!;

        rgba.Length.Should().Be(3 * 2 * 4);
        rgba[..4].Should().Equal(255, 0, 0, 255);
    }

    [Fact]
    public void GetFormat_RecognisesDxtFourCcs()
    {
        DxtDecoder.GetFormat(DdsTestData.Build(DdsFormat.Dxt1, 4, 4, new byte[8])).Should().Be(DdsFormat.Dxt1);
        DxtDecoder.GetFormat(DdsTestData.Build(DdsFormat.Dxt3, 4, 4, new byte[16])).Should().Be(DdsFormat.Dxt3);
        DxtDecoder.GetFormat(DdsTestData.Build(DdsFormat.Dxt5, 4, 4, new byte[16])).Should().Be(DdsFormat.Dxt5);
    }

    [Fact]
    public void Decode_UnsupportedFourCc_ReturnsNull()
    {
        var dds = DdsTestData.Build(DdsFormat.Dxt1, 4, 4, new byte[8]);
        dds[84] = (byte)'B'; dds[85] = (byte)'C'; dds[86] = (byte)'7'; dds[87] = 0;

        DxtDecoder.DecompressDds(dds).Should().BeNull();
        DxtDecoder.GetFormat(dds).Should().BeNull();
    }

    [Fact]
    public void Decode_BadMagic_ReturnsNullForUncompressed()
    {
        var dds = DdsTestData.Build(DdsFormat.A8R8G8B8, 1, 1, new byte[4]);
        dds[0] = 0;

        DxtDecoder.DecompressDds(dds).Should().BeNull();
    }

    [Fact]
    public void Decode_OversizedDimensions_ReturnsNull()
    {
        var dds = DdsTestData.Build(DdsFormat.Dxt1, 4, 4, new byte[8]);
        BitConverter.GetBytes(60000).CopyTo(dds, 12);
        BitConverter.GetBytes(60000).CopyTo(dds, 16);

        DxtDecoder.DecompressDds(dds).Should().BeNull("a 3.6 billion pixel image must not be allocated");
    }

    [Fact]
    public void Decode_MasksInferFormatWhenFlagsAreWrong()
    {
        // Legacy "corrects" headers: flags say nothing useful but bit masks describe A8R8G8B8
        var dds = DdsTestData.Build(DdsFormat.A8R8G8B8, 1, 1, [0x33, 0x22, 0x11, 0x44]);
        BitConverter.GetBytes(0u).CopyTo(dds, 80); // pixel format flags cleared

        DxtDecoder.DecompressDds(dds).Should().Equal(0x11, 0x22, 0x33, 0x44);
    }

    [Fact]
    public void Decode_UncompressedWithZeroBitCount_ComputesFromMasks()
    {
        var dds = DdsTestData.Build(DdsFormat.A8R8G8B8, 1, 1, [0x33, 0x22, 0x11, 0x44]);
        BitConverter.GetBytes(0u).CopyTo(dds, 88); // RGBBitCount = 0

        DxtDecoder.DecompressDds(dds).Should().Equal(0x11, 0x22, 0x33, 0x44);
    }
}
