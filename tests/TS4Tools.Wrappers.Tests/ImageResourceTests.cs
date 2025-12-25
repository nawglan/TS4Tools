using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class ImageResourceTests
{
    private static readonly ResourceKey PngKey = new(0x00B00000, 0, 0);
    private static readonly ResourceKey DdsKey = new(0x00B2D882, 0, 0);

    // Minimal valid PNG: 1x1 red pixel
    private static readonly byte[] MinimalPng =
    [
        // PNG signature
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        // IHDR chunk
        0x00, 0x00, 0x00, 0x0D, // length
        0x49, 0x48, 0x44, 0x52, // "IHDR"
        0x00, 0x00, 0x00, 0x01, // width = 1
        0x00, 0x00, 0x00, 0x01, // height = 1
        0x08, // bit depth
        0x02, // color type (RGB)
        0x00, // compression
        0x00, // filter
        0x00, // interlace
        0x90, 0x77, 0x53, 0xDE, // CRC
        // IDAT chunk (minimal)
        0x00, 0x00, 0x00, 0x0C, // length
        0x49, 0x44, 0x41, 0x54, // "IDAT"
        0x08, 0xD7, 0x63, 0xF8, 0xFF, 0xFF, 0x3F, 0x00, 0x05, 0xFE, 0x02, 0xFE,
        0xA3, 0x36, 0xC1, 0xD4, // CRC
        // IEND chunk
        0x00, 0x00, 0x00, 0x00, // length
        0x49, 0x45, 0x4E, 0x44, // "IEND"
        0xAE, 0x42, 0x60, 0x82  // CRC
    ];

    // Minimal DDS header
    private static readonly byte[] MinimalDds =
    [
        // Magic "DDS "
        0x44, 0x44, 0x53, 0x20,
        // dwSize (124)
        0x7C, 0x00, 0x00, 0x00,
        // dwFlags
        0x07, 0x10, 0x00, 0x00,
        // dwHeight (64)
        0x40, 0x00, 0x00, 0x00,
        // dwWidth (64)
        0x40, 0x00, 0x00, 0x00,
        // Rest of header (zeros for simplicity)
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00
    ];

    [Fact]
    public void CreateEmpty_HasUnknownFormat()
    {
        var resource = new ImageResource(PngKey, ReadOnlyMemory<byte>.Empty);

        resource.Format.Should().Be(ImageFormat.Unknown);
        resource.Width.Should().Be(0);
        resource.Height.Should().Be(0);
        resource.DataLength.Should().Be(0);
    }

    [Fact]
    public void Parse_Png_DetectsFormat()
    {
        var resource = new ImageResource(PngKey, MinimalPng);

        resource.Format.Should().Be(ImageFormat.Png);
    }

    [Fact]
    public void Parse_Png_ReadsDimensions()
    {
        var resource = new ImageResource(PngKey, MinimalPng);

        resource.Width.Should().Be(1);
        resource.Height.Should().Be(1);
    }

    [Fact]
    public void Parse_Dds_DetectsFormat()
    {
        var resource = new ImageResource(DdsKey, MinimalDds);

        resource.Format.Should().Be(ImageFormat.Dds);
    }

    [Fact]
    public void Parse_Dds_ReadsDimensions()
    {
        var resource = new ImageResource(DdsKey, MinimalDds);

        resource.Width.Should().Be(64);
        resource.Height.Should().Be(64);
    }

    [Fact]
    public void Parse_Dst_DetectsByResourceType()
    {
        // DST files might not have standard headers, but should be detected by resource type
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        var resource = new ImageResource(DdsKey, data);

        resource.Format.Should().Be(ImageFormat.Dst);
    }

    [Fact]
    public void Parse_UnknownFormat_SetsUnknown()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        var resource = new ImageResource(PngKey, data);

        resource.Format.Should().Be(ImageFormat.Unknown);
    }

    [Fact]
    public void ImageData_ReturnsOriginalBytes()
    {
        var resource = new ImageResource(PngKey, MinimalPng);

        resource.ImageData.ToArray().Should().BeEquivalentTo(MinimalPng);
    }

    [Fact]
    public void SetImageData_UpdatesData()
    {
        var resource = new ImageResource(PngKey, ReadOnlyMemory<byte>.Empty);

        resource.SetImageData(MinimalPng);

        resource.Format.Should().Be(ImageFormat.Png);
        resource.DataLength.Should().Be(MinimalPng.Length);
        resource.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new ImageResource(PngKey, MinimalPng);

        var serialized = original.Data;
        var parsed = new ImageResource(PngKey, serialized);

        parsed.Format.Should().Be(original.Format);
        parsed.Width.Should().Be(original.Width);
        parsed.Height.Should().Be(original.Height);
        parsed.ImageData.ToArray().Should().BeEquivalentTo(original.ImageData.ToArray());
    }

    [Fact]
    public void DataLength_ReturnsCorrectSize()
    {
        var resource = new ImageResource(PngKey, MinimalPng);

        resource.DataLength.Should().Be(MinimalPng.Length);
    }

    [Fact]
    public void Parse_TooShort_SetsUnknownFormat()
    {
        var data = new byte[] { 0x89, 0x50, 0x4E }; // Incomplete PNG signature

        var resource = new ImageResource(PngKey, data);

        resource.Format.Should().Be(ImageFormat.Unknown);
    }
}
