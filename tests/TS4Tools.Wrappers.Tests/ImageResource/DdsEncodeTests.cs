using FluentAssertions;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="DxtEncoder"/> and the save path of <see cref="DdsImage"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DdsFile.cs (Dds.Write lines 653-700, DdsFile.Save lines 1292-1320)
///   and DdsSquish.cs (CompressImage). Legacy compresses with native libsquish; this port uses range fit, so tests assert
///   decode(encode(x)) is within block-compression tolerance rather than byte equality with legacy output.
/// </summary>
public class DdsEncodeTests
{
    private static int MaxChannelDiff(byte[] a, byte[] b, int channels = 4)
    {
        var max = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (i % 4 >= channels) continue;
            max = Math.Max(max, Math.Abs(a[i] - b[i]));
        }

        return max;
    }

    private static byte[] RoundTrip(byte[] rgba, int w, int h, DdsFormat format)
    {
        var blocks = DxtEncoder.Compress(rgba, w, h, format);
        return DxtDecoder.DecompressDds(DdsTestData.Build(format, w, h, blocks))!;
    }

    private static byte[] Solid(int w, int h, byte r, byte g, byte b, byte a)
    {
        var rgba = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            rgba[i * 4] = r; rgba[(i * 4) + 1] = g; rgba[(i * 4) + 2] = b; rgba[(i * 4) + 3] = a;
        }

        return rgba;
    }

    [Theory]
    [InlineData(DdsFormat.Dxt1, 8)]
    [InlineData(DdsFormat.Dxt3, 16)]
    [InlineData(DdsFormat.Dxt5, 16)]
    public void Compress_OutputSize_IsBlocksTimesBlockSize(DdsFormat format, int blockSize)
    {
        var blocks = DxtEncoder.Compress(DdsTestData.Gradient(9, 6), 9, 6, format);

        blocks.Length.Should().Be(3 * 2 * blockSize);
    }

    [Theory]
    [InlineData(DdsFormat.Dxt1)]
    [InlineData(DdsFormat.Dxt3)]
    [InlineData(DdsFormat.Dxt5)]
    public void Compress_SolidColour_RoundTripsWithinQuantisation(DdsFormat format)
    {
        var rgba = Solid(8, 8, 200, 100, 50, 255);

        var decoded = RoundTrip(rgba, 8, 8, format);

        MaxChannelDiff(rgba, decoded).Should().BeLessThanOrEqualTo(8, "565 quantisation of a single colour");
    }

    [Fact]
    public void Compress_TwoColourBlock_KeepsBothColoursDistinct()
    {
        var rgba = new byte[4 * 4 * 4];
        for (var i = 0; i < 16; i++)
        {
            var red = i % 2 == 0;
            rgba[i * 4] = (byte)(red ? 255 : 0);
            rgba[(i * 4) + 2] = (byte)(red ? 0 : 255);
            rgba[(i * 4) + 3] = 255;
        }

        var decoded = RoundTrip(rgba, 4, 4, DdsFormat.Dxt5);

        MaxChannelDiff(rgba, decoded).Should().BeLessThanOrEqualTo(8);
    }

    [Theory]
    [InlineData(DdsFormat.Dxt1)]
    [InlineData(DdsFormat.Dxt5)]
    public void Compress_Gradient_HasLowAverageError(DdsFormat format)
    {
        var rgba = DdsTestData.Gradient(16, 16);

        var decoded = RoundTrip(rgba, 16, 16, format);

        var total = 0L;
        for (var i = 0; i < rgba.Length; i++)
            total += Math.Abs(rgba[i] - decoded[i]);
        (total / (double)rgba.Length).Should().BeLessThan(9.0, "range fit is coarser than cluster fit on a 3-channel gradient");
    }

    [Fact]
    public void Compress_Dxt5_AlphaGradient_IsApproximated()
    {
        var rgba = DdsTestData.Gradient(8, 4);
        for (var i = 0; i < 32; i++)
            rgba[(i * 4) + 3] = (byte)(i * 255 / 31);

        var decoded = RoundTrip(rgba, 8, 4, DdsFormat.Dxt5);

        for (var i = 0; i < 32; i++)
            Math.Abs(decoded[(i * 4) + 3] - rgba[(i * 4) + 3]).Should().BeLessThanOrEqualTo(24, $"pixel {i}");
    }

    [Fact]
    public void Compress_Dxt5_ExtremeAlphaValues_UseFiveAlphaMode()
    {
        // Only 0 and 255: the 5-alpha layout (explicit 0/255 codes) represents these exactly
        var rgba = Solid(4, 4, 10, 20, 30, 0);
        for (var i = 0; i < 16; i += 2)
            rgba[(i * 4) + 3] = 255;

        var decoded = RoundTrip(rgba, 4, 4, DdsFormat.Dxt5);

        for (var i = 0; i < 16; i++)
            decoded[(i * 4) + 3].Should().Be(rgba[(i * 4) + 3]);
    }

    [Fact]
    public void Compress_Dxt3_AlphaQuantisedToFourBits()
    {
        var rgba = Solid(4, 4, 0, 0, 0, 0);
        for (var i = 0; i < 16; i++)
            rgba[(i * 4) + 3] = (byte)(i * 17); // exactly representable nibbles

        var decoded = RoundTrip(rgba, 4, 4, DdsFormat.Dxt3);

        for (var i = 0; i < 16; i++)
            decoded[(i * 4) + 3].Should().Be((byte)(i * 17));
    }

    [Fact]
    public void Compress_Dxt1_TransparentPixels_StayTransparent()
    {
        var rgba = Solid(4, 4, 255, 0, 0, 255);
        for (var i = 0; i < 16; i += 3)
            rgba[(i * 4) + 3] = 0;

        var decoded = RoundTrip(rgba, 4, 4, DdsFormat.Dxt1);

        for (var i = 0; i < 16; i++)
        {
            if (i % 3 == 0)
                decoded[(i * 4) + 3].Should().Be(0, $"pixel {i} had alpha < 128");
            else
            {
                decoded[(i * 4) + 3].Should().Be(255);
                decoded[i * 4].Should().BeGreaterThan(240);
            }
        }
    }

    [Fact]
    public void Compress_Dxt1_FullyTransparentBlock_DecodesToTransparent()
    {
        var decoded = RoundTrip(Solid(4, 4, 9, 9, 9, 0), 4, 4, DdsFormat.Dxt1);

        decoded.Where((_, i) => i % 4 == 3).Should().OnlyContain(a => a == 0);
    }

    [Fact]
    public void Compress_NonMultipleOfFour_ReplicatesEdgePixels()
    {
        var rgba = Solid(5, 3, 90, 180, 30, 255);

        var decoded = RoundTrip(rgba, 5, 3, DdsFormat.Dxt5);

        decoded.Length.Should().Be(5 * 3 * 4);
        MaxChannelDiff(rgba, decoded).Should().BeLessThanOrEqualTo(8);
    }

    [Fact]
    public void Compress_IsDeterministic()
    {
        var rgba = DdsTestData.Gradient(12, 12, 200);

        DxtEncoder.Compress(rgba, 12, 12, DdsFormat.Dxt5).Should().Equal(DxtEncoder.Compress(rgba, 12, 12, DdsFormat.Dxt5));
    }

    [Fact]
    public void Compress_ColourBlockOrdering_SatisfiesFourColourModeForDxt5()
    {
        // In DXT3/5 the decoder always uses four colours; the encoder must still emit c0 >= c1
        var blocks = DxtEncoder.Compress(DdsTestData.Gradient(4, 4), 4, 4, DdsFormat.Dxt5);

        var c0 = (ushort)(blocks[8] | (blocks[9] << 8));
        var c1 = (ushort)(blocks[10] | (blocks[11] << 8));
        c0.Should().BeGreaterThanOrEqualTo(c1);
    }

    [Fact]
    public void Compress_RejectsNonDxtFormat()
    {
        var act = () => DxtEncoder.Compress(new byte[64], 4, 4, DdsFormat.A8R8G8B8);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compress_RejectsShortBufferAndBadSize()
    {
        ((Action)(() => DxtEncoder.Compress(new byte[10], 4, 4, DdsFormat.Dxt1))).Should().Throw<ArgumentException>();
        ((Action)(() => DxtEncoder.Compress(new byte[64], 0, 4, DdsFormat.Dxt1))).Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---- header writing ----

    [Fact]
    public void SaveToDds_Dxt5_WritesLegacyHeader()
    {
        var image = DdsImage.FromRgba32(DdsTestData.Gradient(8, 4), 8, 4);

        var dds = image.SaveToDds();

        BitConverter.ToUInt32(dds, 0).Should().Be(0x20534444u);                // "DDS "
        BitConverter.ToUInt32(dds, 4).Should().Be(124u);                       // header size
        BitConverter.ToUInt32(dds, 8).Should().Be(0x00081007u);                // caps|height|width|pixelformat|linearsize
        BitConverter.ToUInt32(dds, 12).Should().Be(4u);                        // height
        BitConverter.ToUInt32(dds, 16).Should().Be(8u);                        // width
        BitConverter.ToUInt32(dds, 20).Should().Be(2u * 1 * 16);               // linear size = compressed size
        BitConverter.ToUInt32(dds, 28).Should().Be(1u);                        // mip count
        BitConverter.ToUInt32(dds, 76).Should().Be(32u);                       // pixel format size
        BitConverter.ToUInt32(dds, 80).Should().Be(4u);                        // DDPF_FOURCC
        BitConverter.ToUInt32(dds, 84).Should().Be(0x35545844u);               // DXT5
        BitConverter.ToUInt32(dds, 108).Should().Be(0x1000u);                  // DDSCAPS_TEXTURE
        dds.Length.Should().Be(128 + (2 * 16));
    }

    [Fact]
    public void SaveToDds_Uncompressed_WritesPitchAndMasks()
    {
        var image = DdsImage.FromRgba32(DdsTestData.Gradient(3, 2), 3, 2);

        var dds = image.SaveToDds(DdsFormat.A8R8G8B8);

        BitConverter.ToUInt32(dds, 8).Should().Be(0x0000100Fu);  // caps|height|width|pitch|pixelformat
        BitConverter.ToUInt32(dds, 20).Should().Be(12u);         // pitch
        BitConverter.ToUInt32(dds, 80).Should().Be(0x41u);       // DDPF_RGB | DDPF_ALPHAPIXELS
        BitConverter.ToUInt32(dds, 88).Should().Be(32u);
        BitConverter.ToUInt32(dds, 92).Should().Be(0x00FF0000u); // R mask
        BitConverter.ToUInt32(dds, 96).Should().Be(0x0000FF00u);
        BitConverter.ToUInt32(dds, 100).Should().Be(0x000000FFu);
        BitConverter.ToUInt32(dds, 104).Should().Be(0xFF000000u);
        dds.Length.Should().Be(128 + (3 * 2 * 4));
    }

    [Fact]
    public void SaveToDds_A8L8_WritesLuminanceFlag()
    {
        var dds = DdsImage.FromRgba32(DdsTestData.Gradient(2, 2), 2, 2).SaveToDds(DdsFormat.A8L8);

        BitConverter.ToUInt32(dds, 80).Should().Be(0x20001u); // luminance | alphapixels
        BitConverter.ToUInt32(dds, 88).Should().Be(16u);
    }

    [Fact]
    public void SaveToDds_DefaultFormatFollowsLegacySelection()
    {
        var image = DdsImage.FromRgba32(new byte[4 * 4 * 4], 4, 4);

        image.SaveFormat.Should().Be(DdsFormat.Dxt5); // new image: DXT, alpha depth 5

        image.AlphaDepth = 1;
        image.SaveFormat.Should().Be(DdsFormat.Dxt1);
        image.AlphaDepth = 3;
        image.SaveFormat.Should().Be(DdsFormat.Dxt3);

        image.SetUseDxt(false);
        image.AlphaDepth = 0;
        image.SaveFormat.Should().Be(DdsFormat.R8G8B8);
        image.AlphaDepth = 1;
        image.SaveFormat.Should().Be(DdsFormat.A1R5G5B5);
        image.AlphaDepth = 4;
        image.SaveFormat.Should().Be(DdsFormat.A4R4G4B4);
        image.AlphaDepth = 8;
        image.SaveFormat.Should().Be(DdsFormat.A8R8G8B8);

        image.SetUseLuminance(true);
        image.SaveFormat.Should().Be(DdsFormat.A8L8);
    }

    [Fact]
    public void SaveToDds_WithMipmaps_WritesFullChainAndHeaderFlags()
    {
        var image = DdsImage.FromRgba32(DdsTestData.Gradient(16, 8), 16, 8);

        var dds = image.SaveToDds(DdsFormat.Dxt5, generateMipmaps: true);

        // 16x8, 8x4, 4x2, 2x1, 1x1 = 5 levels; sizes in blocks: 8, 2, 1, 1, 1 (x16 bytes)
        BitConverter.ToUInt32(dds, 28).Should().Be(5u);
        (BitConverter.ToUInt32(dds, 8) & 0x20000).Should().Be(0x20000u, "DDSD_MIPMAPCOUNT");
        BitConverter.ToUInt32(dds, 108).Should().Be(0x1000u | 0x8u | 0x400000u);
        dds.Length.Should().Be(128 + ((8 + 2 + 1 + 1 + 1) * 16));
    }

    [Fact]
    public void SaveToDds_UncompressedMipmaps_UseOwnPitchPerLevel()
    {
        var image = DdsImage.FromRgba32(DdsTestData.Gradient(4, 4), 4, 4);

        var dds = image.SaveToDds(DdsFormat.A8R8G8B8, generateMipmaps: true);

        dds.Length.Should().Be(128 + ((16 + 4 + 1) * 4));
    }

    [Fact]
    public void SaveToDds_Mipmaps_BoxFilterAveragesPixels()
    {
        var image = DdsImage.FromArgb([0xFF000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFF000000], 2, 2);

        var dds = image.SaveToDds(DdsFormat.A8R8G8B8, generateMipmaps: true);

        // second level is a single pixel: average of 0,255,255,0 = 127.5 -> 128 (rounded)
        var level1 = dds.AsSpan(128 + 16, 4).ToArray();
        level1.Should().Equal(128, 128, 128, 255);
    }

    [Fact]
    public void SaveThenLoad_Dxt5_RoundTripsWithinTolerance()
    {
        var rgba = DdsTestData.Gradient(16, 16, 180);
        var saved = DdsImage.FromRgba32(rgba, 16, 16).SaveToDds();

        var loaded = DdsImage.Load(saved);

        loaded.Width.Should().Be(16);
        loaded.Height.Should().Be(16);
        loaded.UseDxt.Should().BeTrue();
        loaded.AlphaDepth.Should().Be(5);
        MaxChannelDiff(rgba, loaded.ToRgba32(), 3).Should().BeLessThanOrEqualTo(64);
    }

    [Theory]
    [InlineData(DdsFormat.A8R8G8B8, 0)]
    [InlineData(DdsFormat.A8B8G8R8, 0)]
    [InlineData(DdsFormat.X8R8G8B8, 0)]
    [InlineData(DdsFormat.X8B8G8R8, 0)]
    [InlineData(DdsFormat.R8G8B8, 0)]
    [InlineData(DdsFormat.R5G6B5, 8)]
    [InlineData(DdsFormat.A1R5G5B5, 8)]
    [InlineData(DdsFormat.A4R4G4B4, 17)]
    public void SaveThenLoad_Uncompressed_RoundTrips(DdsFormat format, int tolerance)
    {
        var rgba = DdsTestData.Gradient(7, 5);

        var loaded = DdsImage.Load(DdsImage.FromRgba32(rgba, 7, 5).SaveToDds(format));

        loaded.Width.Should().Be(7);
        loaded.Height.Should().Be(5);
        MaxChannelDiff(rgba, loaded.ToRgba32()).Should().BeLessThanOrEqualTo(tolerance);
    }

    [Fact]
    public void SaveThenLoad_A8L8_KeepsAlphaAndAverageLuminance()
    {
        var image = DdsImage.FromArgb([0x80304050, 0xFFFFFFFF], 2, 1);

        var loaded = DdsImage.Load(image.SaveToDds(DdsFormat.A8L8));

        loaded.UseLuminance.Should().BeTrue();
        var argb = loaded.GetArgb();
        argb[0].Should().Be(0x80000000u | (uint)(((0x30 + 0x40 + 0x50) / 3) * 0x010101));
        argb[1].Should().Be(0xFFFFFFFFu);
    }

    [Fact]
    public void Load_RejectsGarbageAndTruncatedData()
    {
        ((Action)(() => DdsImage.Load(new byte[64]))).Should().Throw<InvalidDataException>();

        var dds = DdsImage.FromRgba32(DdsTestData.Gradient(8, 8), 8, 8).SaveToDds();
        ((Action)(() => DdsImage.Load(dds.AsSpan(0, dds.Length - 1).ToArray()))).Should().Throw<InvalidDataException>();
        DdsImage.TryLoad(new byte[200]).Should().BeNull();
    }
}
