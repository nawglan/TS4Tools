using FluentAssertions;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for DST import (shuffle), the inverse of <see cref="DstDecoder.UnshuffleToDds"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/DSTResource.cs
///   ImportToDST (lines 82-134), Shuffle (138-196), Unshuffle (198-270).
/// - DST1: all first 4-byte halves of the blocks, then all second halves.
/// - DST5: stream of 2-byte alpha endpoints, then 4-byte colour endpoints, then 6-byte alpha indices, then 4-byte colour indices.
/// - DXT3 has no DST3 sample in legacy ("No sample yet") and is rejected.
/// </summary>
public class DstImportTests
{
    private static byte[] DdsOf(DdsFormat format, int blocks)
    {
        var size = format == DdsFormat.Dxt1 ? 8 : 16;
        var width = blocks * 4;
        return DdsTestData.Build(format, width, 4, DdsTestData.Noise(blocks * size, blocks));
    }

    [Theory]
    [InlineData(DdsFormat.Dxt1, 1)]
    [InlineData(DdsFormat.Dxt1, 7)]
    [InlineData(DdsFormat.Dxt5, 1)]
    [InlineData(DdsFormat.Dxt5, 9)]
    public void ImportToDst_ThenUnshuffle_RestoresOriginal(DdsFormat format, int blocks)
    {
        var dds = DdsOf(format, blocks);

        var dst = DstDecoder.ImportToDst(dds);
        var back = DstDecoder.UnshuffleToDds(dst)!;

        DstDecoder.IsDst(dst).Should().BeTrue();
        back.Should().Equal(dds);
    }

    [Fact]
    public void ImportToDst_Dxt1_SeparatesHalvesLikeLegacyShuffle()
    {
        var body = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 16, 17, 18 };
        var dds = DdsTestData.Build(DdsFormat.Dxt1, 8, 4, body);

        var dst = DstDecoder.ImportToDst(dds);

        BitConverter.ToUInt32(dst, 84).Should().Be(DstDecoder.FourCcDst1);
        dst[128..].Should().Equal(1, 2, 3, 4, 11, 12, 13, 14, 5, 6, 7, 8, 15, 16, 17, 18);
        dst[..84].Should().Equal(dds[..84], "the header is preserved apart from the FourCC");
    }

    [Fact]
    public void ImportToDst_Dxt5_OrdersStreamsAlphaEndpointsColourEndpointsAlphaIndicesColourIndices()
    {
        // Two blocks: bytes 0-15 and 16-31 labelled by position
        var body = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var dds = DdsTestData.Build(DdsFormat.Dxt5, 8, 4, body);

        var dst = DstDecoder.ImportToDst(dds)[128..];

        // stream 0: alpha endpoints (bytes 0-1, 16-17)
        dst[..4].Should().Equal(0, 1, 16, 17);
        // stream 2: colour endpoints (bytes 8-11, 24-27)
        dst[4..12].Should().Equal(8, 9, 10, 11, 24, 25, 26, 27);
        // stream 1: alpha indices (bytes 2-7, 18-23)
        dst[12..24].Should().Equal(2, 3, 4, 5, 6, 7, 18, 19, 20, 21, 22, 23);
        // stream 3: colour indices (bytes 12-15, 28-31)
        dst[24..].Should().Equal(12, 13, 14, 15, 28, 29, 30, 31);
    }

    [Fact]
    public void ImportToDst_AlreadyShuffled_PassesThroughUnchanged()
    {
        var dst = DstDecoder.ImportToDst(DdsOf(DdsFormat.Dxt5, 3));

        DstDecoder.ImportToDst(dst).Should().Equal(dst);
    }

    [Fact]
    public void ImportToDst_Dxt3_IsNotSupported()
    {
        var act = () => DstDecoder.ImportToDst(DdsOf(DdsFormat.Dxt3, 2));

        act.Should().Throw<NotSupportedException>().WithMessage("*DXT3*");
    }

    [Fact]
    public void ImportToDst_UncompressedFormat_IsNotSupported()
    {
        var act = () => DstDecoder.ImportToDst(DdsTestData.Build(DdsFormat.A8R8G8B8, 1, 1, new byte[4]));

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ImportToDst_RejectsNonDdsAndPartialBlocks()
    {
        ((Action)(() => DstDecoder.ImportToDst(new byte[100]))).Should().Throw<InvalidDataException>();
        ((Action)(() => DstDecoder.ImportToDst(new byte[200]))).Should().Throw<InvalidDataException>();

        var partial = DdsTestData.Build(DdsFormat.Dxt5, 4, 4, new byte[20]);
        ((Action)(() => DstDecoder.ImportToDst(partial))).Should().Throw<InvalidDataException>().WithMessage("*whole number*");

        var empty = DdsTestData.Build(DdsFormat.Dxt1, 4, 4, []);
        ((Action)(() => DstDecoder.ImportToDst(empty))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ImageResource_ImportToDst_ConvertsAndRoundTripsThroughToDds()
    {
        var resource = new ImageResource(new ResourceKey(0x00B2D882, 0, 0), ReadOnlyMemory<byte>.Empty);
        var dds = DdsOf(DdsFormat.Dxt5, 4);
        var raised = 0;
        resource.Changed += (_, _) => raised++;

        resource.ImportToDst(dds);

        raised.Should().Be(1);
        resource.Format.Should().Be(ImageFormat.Dst);
        resource.DxtFormat.Should().Be(DxtFormat.Dxt5);
        resource.Width.Should().Be(16);
        resource.Height.Should().Be(4);
        resource.ToDds().Should().Equal(dds);
        resource.Data.ToArray().Should().Equal(DstDecoder.ImportToDst(dds));
    }

    [Fact]
    public void ImageResource_ToDds_ForPlainDdsReturnsCopyAndForPngReturnsNull()
    {
        var dds = DdsOf(DdsFormat.Dxt1, 2);
        var resource = new ImageResource(new ResourceKey(0x00B2D882, 0, 0), dds);

        resource.ToDds().Should().Equal(dds);

        var png = new ImageResource(new ResourceKey(0x00B00000, 0, 0),
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 });
        png.ToDds().Should().BeNull();
    }

    [Fact]
    public void ImageResource_DxtFormat_RecognisesDxt3()
    {
        var resource = new ImageResource(new ResourceKey(0x00B2D882, 0, 0), DdsOf(DdsFormat.Dxt3, 1));

        resource.DxtFormat.Should().Be(DxtFormat.Dxt3);
        resource.GetDecodedPixels().Should().NotBeNull();
    }
}
