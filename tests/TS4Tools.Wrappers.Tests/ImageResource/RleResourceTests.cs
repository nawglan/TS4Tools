using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="RleResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/RLEResource.cs
/// - Type IDs: 0x3453CF95, 0xBA856C78
/// - Contains RLE-compressed DXT5 texture data with multiple mip levels
/// </summary>
public class RleResourceTests
{
    private static readonly ResourceKey TestKey = new(RleResource.TypeId, 0, 0);
    private static readonly ResourceKey TestKeyAlt = new(RleResource.TypeIdAlternate, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        RleResource.TypeId.Should().Be(0x3453CF95);
        RleResource.TypeIdAlternate.Should().Be(0xBA856C78);
    }

    [Fact]
    public void FourCcConstants_AreCorrect()
    {
        RleResource.FourCcDxt5.Should().Be(0x35545844); // "DXT5"
        RleResource.FourCcRle2.Should().Be(0x32454C52); // "RLE2"
        RleResource.FourCcRles.Should().Be(0x53454C52); // "RLES"
        RleResource.DdsSignature.Should().Be(0x20534444); // "DDS "
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new RleResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(RleVersion.Rle2);
        resource.Width.Should().Be(0);
        resource.Height.Should().Be(0);
        resource.MipCount.Should().Be(0);
        resource.MipHeaders.Should().BeEmpty();
        resource.RawData.Length.Should().Be(0);
        resource.HasSpecular.Should().BeFalse();
    }

    [Fact]
    public void Parse_Rle2Header_ExtractsMetadata()
    {
        // Build a minimal RLE2 header: DXT5 + RLE2 + width + height + mipCount + reserved
        var data = new byte[16 + 20]; // Header + one mip header (5 * 4 bytes)

        // FourCC: DXT5
        data[0] = 0x44; data[1] = 0x58; data[2] = 0x54; data[3] = 0x35;
        // Version: RLE2
        data[4] = 0x52; data[5] = 0x4C; data[6] = 0x45; data[7] = 0x32;
        // Width: 256 (little-endian)
        data[8] = 0x00; data[9] = 0x01;
        // Height: 128
        data[10] = 0x80; data[11] = 0x00;
        // MipCount: 1
        data[12] = 0x01; data[13] = 0x00;
        // Reserved: 0
        data[14] = 0x00; data[15] = 0x00;

        // Mip header offsets (all pointing to end of headers for simplicity)
        int offset = 36; // After headers
        for (int i = 0; i < 5; i++)
        {
            data[16 + i * 4] = (byte)(offset & 0xFF);
            data[17 + i * 4] = (byte)((offset >> 8) & 0xFF);
            data[18 + i * 4] = (byte)((offset >> 16) & 0xFF);
            data[19 + i * 4] = (byte)((offset >> 24) & 0xFF);
        }

        var resource = new RleResource(TestKey, data);

        resource.Version.Should().Be(RleVersion.Rle2);
        resource.Width.Should().Be(256);
        resource.Height.Should().Be(128);
        resource.MipCount.Should().Be(1);
        resource.HasSpecular.Should().BeFalse();
    }

    [Fact]
    public void Parse_RlesHeader_DetectsSpecular()
    {
        // Build a minimal RLES header: DXT5 + RLES + width + height + mipCount + reserved
        var data = new byte[16 + 24]; // Header + one mip header (6 * 4 bytes for RLES)

        // FourCC: DXT5
        data[0] = 0x44; data[1] = 0x58; data[2] = 0x54; data[3] = 0x35;
        // Version: RLES
        data[4] = 0x52; data[5] = 0x4C; data[6] = 0x45; data[7] = 0x53;
        // Width: 64
        data[8] = 0x40; data[9] = 0x00;
        // Height: 64
        data[10] = 0x40; data[11] = 0x00;
        // MipCount: 1
        data[12] = 0x01; data[13] = 0x00;
        // Reserved
        data[14] = 0x00; data[15] = 0x00;

        // Mip header offsets (6 for RLES)
        int offset = 40; // After headers
        for (int i = 0; i < 6; i++)
        {
            data[16 + i * 4] = (byte)(offset & 0xFF);
            data[17 + i * 4] = (byte)((offset >> 8) & 0xFF);
            data[18 + i * 4] = (byte)((offset >> 16) & 0xFF);
            data[19 + i * 4] = (byte)((offset >> 24) & 0xFF);
        }

        var resource = new RleResource(TestKey, data);

        resource.Version.Should().Be(RleVersion.Rles);
        resource.Width.Should().Be(64);
        resource.Height.Should().Be(64);
        resource.MipCount.Should().Be(1);
        resource.HasSpecular.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_PreservesRawData()
    {
        // Create minimal valid data
        var originalData = new byte[16 + 20];
        originalData[0] = 0x44; originalData[1] = 0x58; originalData[2] = 0x54; originalData[3] = 0x35;
        originalData[4] = 0x52; originalData[5] = 0x4C; originalData[6] = 0x45; originalData[7] = 0x32;
        originalData[8] = 0x10; originalData[9] = 0x00; // 16 width
        originalData[10] = 0x10; originalData[11] = 0x00; // 16 height
        originalData[12] = 0x01; originalData[13] = 0x00; // 1 mip

        var resource = new RleResource(TestKey, originalData);
        var serialized = resource.Data.ToArray();

        serialized.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new RleResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<RleResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new RleResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as RleResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(RleVersion.Rle2);
        resource.Width.Should().Be(0);
        resource.Height.Should().Be(0);
    }

    [Fact]
    public void Factory_HandlesAlternateTypeId()
    {
        var factory = new RleResourceFactory();
        var resource = factory.Create(TestKeyAlt, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<RleResource>();
    }

    [Fact]
    public void RleVersion_EnumValues_AreCorrect()
    {
        ((uint)RleVersion.Rle2).Should().Be(0x32454C52);
        ((uint)RleVersion.Rles).Should().Be(0x53454C52);
    }

    [Fact]
    public void MipHeader_DefaultValues()
    {
        var header = new MipHeader();

        header.CommandOffset.Should().Be(0);
        header.Offset0.Should().Be(0);
        header.Offset1.Should().Be(0);
        header.Offset2.Should().Be(0);
        header.Offset3.Should().Be(0);
        header.Offset4.Should().Be(0);
    }

    [Fact]
    public void Parse_InvalidFourCc_StillPreservesRawData()
    {
        // Data that doesn't start with DXT5
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        var resource = new RleResource(TestKey, data);

        resource.RawData.ToArray().Should().BeEquivalentTo(data);
        resource.Width.Should().Be(0);
        resource.Height.Should().Be(0);
    }

    [Fact]
    public void ToDds_EmptyResource_ReturnsNull()
    {
        var resource = new RleResource(TestKey, ReadOnlyMemory<byte>.Empty);

        var dds = resource.ToDds();

        dds.Should().BeNull();
    }
}
