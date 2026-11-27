using FluentAssertions;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// ImageResource format detection. Source: DSTResource.cs lines 54-72 - DST data starts with "DDS " but
/// carries a shuffled DST1/DST5 FourCC, so it must not be reported as plain DDS.
/// </summary>
public class ImageResourceFormatTests
{
    private static readonly ResourceKey Key = new(0x00B2D882, 0, 1);

    [Fact]
    public void Dds_ReportsDds_AndIsNotUnshuffled()
    {
        var dds = DdsTestData.Build(DdsFormat.Dxt1, 8, 4, DdsTestData.Noise(16, 1));

        var image = new ImageResource(Key, dds);

        image.Format.Should().Be(ImageFormat.Dds);
        image.DxtFormat.Should().Be(DxtFormat.Dxt1);
        image.ToDds().Should().Equal(dds);
    }

    [Theory]
    [InlineData(DdsFormat.Dxt1, DxtFormat.Dxt1)]
    [InlineData(DdsFormat.Dxt5, DxtFormat.Dxt5)]
    public void Dst_ReportsDst_WithDimensionsAndUnshuffle(DdsFormat format, DxtFormat dxt)
    {
        var size = format == DdsFormat.Dxt1 ? 8 : 16;
        var dds = DdsTestData.Build(format, 8, 4, DdsTestData.Noise(2 * size, 3));
        var dst = DstDecoder.ImportToDst(dds);

        var image = new ImageResource(Key, dst);

        image.Format.Should().Be(ImageFormat.Dst);
        image.DxtFormat.Should().Be(dxt);
        image.Width.Should().Be(8);
        image.Height.Should().Be(4);
        image.ToDds().Should().Equal(dds);
        image.GetDecodedPixels().Should().NotBeNull();
    }

    [Fact]
    public void ImportToDst_ThenReparse_StillReportsDst()
    {
        var image = new ImageResource(Key, DdsTestData.Build(DdsFormat.Dxt1, 4, 4, DdsTestData.Noise(8, 2)));
        image.ImportToDst(DdsTestData.Build(DdsFormat.Dxt1, 4, 4, DdsTestData.Noise(8, 2)));

        image.Format.Should().Be(ImageFormat.Dst);
        new ImageResource(Key, image.ImageData).Format.Should().Be(ImageFormat.Dst);
    }
}
