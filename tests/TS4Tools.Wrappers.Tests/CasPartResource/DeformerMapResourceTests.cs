using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Tests for <see cref="DeformerMapResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs
/// - Type ID: 0xDB43E069
/// - Contains deformation map data with scan lines (compressed or uncompressed)
/// </summary>
public class DeformerMapResourceTests
{
    private static readonly ResourceKey TestKey = new(DeformerMapResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        DeformerMapResource.TypeId.Should().Be(0xDB43E069);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new DeformerMapResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(1);
        resource.DoubledWidth.Should().Be(0);
        resource.Height.Should().Be(0);
        resource.AgeGender.Should().Be(AgeGenderFlags.None);
        resource.Physique.Should().Be(Physiques.Heavy);
        resource.IsShapeOrNormals.Should().Be(ShapeOrNormals.Shape);
        resource.MinCol.Should().Be(0);
        resource.MaxCol.Should().Be(0);
        resource.MinRow.Should().Be(0);
        resource.MaxRow.Should().Be(0);
        resource.HasRobeChannel.Should().Be(RobeChannel.Present);
        resource.ScanLines.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_BasicFields_PreservesData()
    {
        var original = new DeformerMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 2;
        original.DoubledWidth = 512;
        original.Height = 1024;
        original.AgeGender = AgeGenderFlags.Adult | AgeGenderFlags.Female;
        original.Physique = Physiques.Fit;
        original.IsShapeOrNormals = ShapeOrNormals.Normals;
        original.MinCol = 10;
        original.MaxCol = 100;
        original.MinRow = 20;
        original.MaxRow = 200;
        original.HasRobeChannel = RobeChannel.Dropped;

        var data = original.Data.ToArray();
        var parsed = new DeformerMapResource(TestKey, data);

        parsed.Version.Should().Be(2);
        parsed.DoubledWidth.Should().Be(512);
        parsed.Height.Should().Be(1024);
        parsed.AgeGender.Should().Be(AgeGenderFlags.Adult | AgeGenderFlags.Female);
        parsed.Physique.Should().Be(Physiques.Fit);
        parsed.IsShapeOrNormals.Should().Be(ShapeOrNormals.Normals);
        parsed.MinCol.Should().Be(10);
        parsed.MaxCol.Should().Be(100);
        parsed.MinRow.Should().Be(20);
        parsed.MaxRow.Should().Be(200);
        parsed.HasRobeChannel.Should().Be(RobeChannel.Dropped);
    }

    [Fact]
    public void RoundTrip_UncompressedScanLine_PreservesData()
    {
        var original = new DeformerMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;
        original.MinCol = 0;
        original.MaxCol = 2; // 3 pixels wide
        original.MinRow = 0;
        original.MaxRow = 0; // 1 scan line
        original.HasRobeChannel = RobeChannel.Dropped;

        // Create uncompressed scan line (3 bytes per pixel, 3 pixels = 9 bytes)
        var scanLine = new ScanLine
        {
            ScanLineDataSize = 4 + 9, // header + pixel data
            IsCompressed = false,
            RobeChannel = RobeChannel.Dropped,
            UncompressedPixels = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 },
            Width = 3
        };
        original.ScanLines.Add(scanLine);

        var data = original.Data.ToArray();
        var parsed = new DeformerMapResource(TestKey, data);

        parsed.ScanLines.Should().HaveCount(1);
        parsed.ScanLines[0].IsCompressed.Should().BeFalse();
        parsed.ScanLines[0].RobeChannel.Should().Be(RobeChannel.Dropped);
        parsed.ScanLines[0].UncompressedPixels.Should().BeEquivalentTo(
            new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
    }

    [Fact]
    public void RoundTrip_UncompressedScanLineWithRobe_PreservesData()
    {
        var original = new DeformerMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;
        original.MinCol = 0;
        original.MaxCol = 1; // 2 pixels wide
        original.MinRow = 0;
        original.MaxRow = 0; // 1 scan line
        original.HasRobeChannel = RobeChannel.Present;

        // Create uncompressed scan line with robe (6 bytes per pixel, 2 pixels = 12 bytes)
        var scanLine = new ScanLine
        {
            ScanLineDataSize = 4 + 12, // header + pixel data
            IsCompressed = false,
            RobeChannel = RobeChannel.Present,
            UncompressedPixels = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 },
            Width = 2
        };
        original.ScanLines.Add(scanLine);

        var data = original.Data.ToArray();
        var parsed = new DeformerMapResource(TestKey, data);

        parsed.ScanLines.Should().HaveCount(1);
        parsed.ScanLines[0].IsCompressed.Should().BeFalse();
        parsed.ScanLines[0].RobeChannel.Should().Be(RobeChannel.Present);
        parsed.ScanLines[0].UncompressedPixels.Should().HaveCount(12);
    }

    [Fact]
    public void RoundTrip_CompressedScanLine_PreservesData()
    {
        var original = new DeformerMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;
        original.MinCol = 0;
        original.MaxCol = 9; // 10 pixels wide
        original.MinRow = 0;
        original.MaxRow = 0; // 1 scan line
        original.HasRobeChannel = RobeChannel.Dropped;

        // Create compressed scan line with 2 indexes
        // RLE data: [runLen, r, g, b] pairs
        var scanLine = new ScanLine
        {
            IsCompressed = true,
            RobeChannel = RobeChannel.Dropped,
            NumIndexes = 2,
            PixelPosIndexes = new ushort[] { 0, 5 },
            DataPosIndexes = new ushort[] { 0, 1 },
            RleArrayOfPixels = new byte[] { 5, 100, 150, 200, 5, 50, 75, 100 }, // Two runs of 5 pixels each
            Width = 10
        };
        // Calculate size: header(4) + numIndexes(1) + indexes(2*2*2=8) + rleData(8) = 21
        scanLine.ScanLineDataSize = 4 + 1 + 8 + 8;
        original.ScanLines.Add(scanLine);

        var data = original.Data.ToArray();
        var parsed = new DeformerMapResource(TestKey, data);

        parsed.ScanLines.Should().HaveCount(1);
        var parsedLine = parsed.ScanLines[0];
        parsedLine.IsCompressed.Should().BeTrue();
        parsedLine.RobeChannel.Should().Be(RobeChannel.Dropped);
        parsedLine.NumIndexes.Should().Be(2);
        parsedLine.PixelPosIndexes.Should().BeEquivalentTo(new ushort[] { 0, 5 });
        parsedLine.DataPosIndexes.Should().BeEquivalentTo(new ushort[] { 0, 1 });
        parsedLine.RleArrayOfPixels.Should().BeEquivalentTo(new byte[] { 5, 100, 150, 200, 5, 50, 75, 100 });
    }

    [Fact]
    public void RoundTrip_MultipleScanLines_PreservesData()
    {
        var original = new DeformerMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;
        original.MinCol = 0;
        original.MaxCol = 1; // 2 pixels wide
        original.MinRow = 0;
        original.MaxRow = 2; // 3 scan lines
        original.HasRobeChannel = RobeChannel.Dropped;

        for (int i = 0; i < 3; i++)
        {
            var scanLine = new ScanLine
            {
                ScanLineDataSize = 4 + 6, // header + 2 pixels * 3 bytes
                IsCompressed = false,
                RobeChannel = RobeChannel.Dropped,
                UncompressedPixels = new byte[] { (byte)(i * 10), (byte)(i * 10 + 1), (byte)(i * 10 + 2),
                                                   (byte)(i * 10 + 3), (byte)(i * 10 + 4), (byte)(i * 10 + 5) },
                Width = 2
            };
            original.ScanLines.Add(scanLine);
        }

        var data = original.Data.ToArray();
        var parsed = new DeformerMapResource(TestKey, data);

        parsed.ScanLines.Should().HaveCount(3);
        parsed.ScanLines[0].UncompressedPixels[0].Should().Be(0);
        parsed.ScanLines[1].UncompressedPixels[0].Should().Be(10);
        parsed.ScanLines[2].UncompressedPixels[0].Should().Be(20);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new DeformerMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;
        original.DoubledWidth = 256;
        original.Height = 512;
        original.AgeGender = AgeGenderFlags.Teen | AgeGenderFlags.Male;
        original.Physique = Physiques.Lean;
        original.IsShapeOrNormals = ShapeOrNormals.Shape;
        original.MinCol = 5;
        original.MaxCol = 7; // 3 pixels wide
        original.MinRow = 10;
        original.MaxRow = 11; // 2 scan lines
        original.HasRobeChannel = RobeChannel.IsCopy;

        // Add two uncompressed scan lines
        for (int i = 0; i < 2; i++)
        {
            var scanLine = new ScanLine
            {
                ScanLineDataSize = 4 + 9, // header + 3 pixels * 3 bytes
                IsCompressed = false,
                RobeChannel = RobeChannel.IsCopy,
                UncompressedPixels = new byte[] { 11, 22, 33, 44, 55, 66, 77, 88, 99 },
                Width = 3
            };
            original.ScanLines.Add(scanLine);
        }

        var data1 = original.Data.ToArray();
        var parsed = new DeformerMapResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new DeformerMapResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<DeformerMapResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new DeformerMapResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as DeformerMapResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(1);
    }

    [Fact]
    public void Physiques_EnumValues_AreCorrect()
    {
        ((byte)Physiques.Heavy).Should().Be(0);
        ((byte)Physiques.Fit).Should().Be(1);
        ((byte)Physiques.Lean).Should().Be(2);
        ((byte)Physiques.Bony).Should().Be(3);
        ((byte)Physiques.Pregnant).Should().Be(4);
        ((byte)Physiques.HipsWide).Should().Be(5);
        ((byte)Physiques.HipsNarrow).Should().Be(6);
        ((byte)Physiques.WaistWide).Should().Be(7);
        ((byte)Physiques.WaistNarrow).Should().Be(8);
        ((byte)Physiques.Ignore).Should().Be(9);
        ((byte)Physiques.Average).Should().Be(100);
    }

    [Fact]
    public void ShapeOrNormals_EnumValues_AreCorrect()
    {
        ((byte)ShapeOrNormals.Shape).Should().Be(0);
        ((byte)ShapeOrNormals.Normals).Should().Be(1);
    }

    [Fact]
    public void RobeChannel_EnumValues_AreCorrect()
    {
        ((byte)RobeChannel.Present).Should().Be(0);
        ((byte)RobeChannel.Dropped).Should().Be(1);
        ((byte)RobeChannel.IsCopy).Should().Be(2);
    }
}
