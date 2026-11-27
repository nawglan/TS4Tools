using FluentAssertions;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for RLE import (DDS to RLE2/RLES), RLE export and RLES specular extraction.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/RLEResource.cs
///   ImportToRLE (lines 400-684), ToDDS (105-293), ToBlock4DDS (295-398).
/// - Block classes: transparent (op 0) = no pixel with alpha != 0; opaque (op 2) = every alpha 255; translucent (op 1) = rest.
/// - Layout written by import: 16 byte header, mip headers (20 bytes RLE2, 24 bytes RLES), then the command stream and
///   streams 2, 3, 0, 1 (and 4 for RLES) in that order.
/// - The synthetic block patterns below are the exact byte patterns legacy ToDDS emits for transparent/opaque RLE2 blocks, so
///   RLE2 round trips are byte-exact.
/// </summary>
public class RleImportTests
{
    private static readonly byte[] TransparentHalf = [0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] OpaqueAlpha = [0x00, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

    private static byte[] TransparentBlock() => [.. TransparentHalf, .. TransparentHalf];

    /// <summary>An RLE2-exact opaque block with the supplied 8 colour bytes.</summary>
    private static byte[] OpaqueBlock(int seed) => [.. OpaqueAlpha, .. DdsTestData.Noise(8, seed)];

    /// <summary>A translucent block: alpha endpoints 128/128, all indices 0 -> alpha 128 everywhere.</summary>
    private static byte[] TranslucentBlock(int seed) => [128, 128, 0, 0, 0, 0, 0, 0, .. DdsTestData.Noise(8, seed)];

    private static byte[] BuildDds(int width, int height, int mips, params byte[][] blocks)
    {
        var body = blocks.SelectMany(b => b).ToArray();
        return DdsTestData.Build(DdsFormat.Dxt5, width, height, body, mips);
    }

    private static byte[] Body(byte[] dds) => dds[128..];

    private static RleResource Create(byte[] rle) => new(new ResourceKey(RleResource.TypeId, 0, 0), rle);

    [Fact]
    public void ImportToRle_Rle2_WritesHeaderAndMipTable()
    {
        var dds = BuildDds(8, 8, 2,
            TransparentBlock(), OpaqueBlock(1), TranslucentBlock(2), TransparentBlock(), // level 0: 2x2 blocks
            OpaqueBlock(3));                                                             // level 1: one block

        var rle = RleResource.EncodeFromDds(dds);

        BitConverter.ToUInt32(rle, 0).Should().Be(RleResource.FourCcDxt5);
        BitConverter.ToUInt32(rle, 4).Should().Be(RleResource.FourCcRle2);
        BitConverter.ToUInt16(rle, 8).Should().Be(8);
        BitConverter.ToUInt16(rle, 10).Should().Be(8);
        BitConverter.ToUInt16(rle, 12).Should().Be(2);
        BitConverter.ToUInt16(rle, 14).Should().Be(0);

        var resource = Create(rle);
        resource.Version.Should().Be(RleVersion.Rle2);
        resource.MipCount.Should().Be(2);
        resource.MipHeaders.Should().HaveCount(3, "two mips plus the sentinel");
        resource.MipHeaders[0].CommandOffset.Should().Be(16 + (20 * 2));
    }

    [Fact]
    public void ImportToRle_Rle2_CommandsClassifyRuns()
    {
        var dds = BuildDds(16, 4, 1,
            TransparentBlock(), TransparentBlock(), OpaqueBlock(1), TranslucentBlock(2));

        var resource = Create(RleResource.EncodeFromDds(dds));

        var commandOffset = resource.MipHeaders[0].CommandOffset;
        var raw = resource.RawData.Span;
        var c0 = BitConverter.ToUInt16(raw[commandOffset..]);
        var c1 = BitConverter.ToUInt16(raw[(commandOffset + 2)..]);
        var c2 = BitConverter.ToUInt16(raw[(commandOffset + 4)..]);

        (c0 & 3).Should().Be(0); (c0 >> 2).Should().Be(2);   // 2 transparent
        (c1 & 3).Should().Be(2); (c1 >> 2).Should().Be(1);   // 1 opaque
        (c2 & 3).Should().Be(1); (c2 >> 2).Should().Be(1);   // 1 translucent
        resource.MipHeaders[1].CommandOffset.Should().Be(commandOffset + 6, "stream 2 (colour endpoints) follows the commands");
    }

    [Fact]
    public void RoundTrip_Rle2_IsByteExact()
    {
        var dds = BuildDds(8, 8, 2,
            TransparentBlock(), OpaqueBlock(1), TranslucentBlock(2), TransparentBlock(),
            OpaqueBlock(3));

        var resource = Create(RleResource.EncodeFromDds(dds));
        var back = resource.ToDds()!;

        back.Length.Should().Be(dds.Length);
        Body(back).Should().Equal(Body(dds));
        BitConverter.ToUInt32(back, 84).Should().Be(RleResource.FourCcDxt5);
        BitConverter.ToUInt32(back, 28).Should().Be(2u);
    }

    [Fact]
    public void RoundTrip_Rle2_RealCompressedImage()
    {
        // Encode a real image through the DXT5 compressor, with an alpha ramp creating all three block classes
        var rgba = DdsTestData.Gradient(32, 32);
        for (var y = 0; y < 32; y++)
        {
            for (var x = 0; x < 32; x++)
                rgba[((y * 32) + x) * 4 + 3] = (byte)(x < 8 ? 0 : x < 16 ? 255 : (x * 8));
        }

        var dds = DdsImage.FromRgba32(rgba, 32, 32).SaveToDds(DdsFormat.Dxt5, generateMipmaps: true);

        var resource = Create(RleResource.EncodeFromDds(dds));

        resource.MipCount.Should().Be(6);
        var back = resource.ToDds()!;
        back.Length.Should().Be(dds.Length);

        // Compare decoded base-level pixels: alpha is exact for translucent and opaque blocks; fully transparent blocks lose colour
        var a = DxtDecoder.DecompressDds(dds)!;
        var b = DxtDecoder.DecompressDds(back)!;
        for (var i = 3; i < a.Length; i += 4)
            b[i].Should().Be(a[i], $"alpha of pixel {i / 4}");
    }

    [Fact]
    public void RoundTrip_Rles_IsByteExact_IncludingOpaqueBlocks()
    {
        var dds = BuildDds(8, 8, 2,
            TransparentBlock(), OpaqueBlock(1), TranslucentBlock(2), OpaqueBlock(9),
            TranslucentBlock(3));

        var resource = Create(RleResource.EncodeFromDds(dds, RleVersion.Rles));

        resource.Version.Should().Be(RleVersion.Rles);
        resource.HasSpecular.Should().BeTrue();
        resource.MipHeaders[0].CommandOffset.Should().Be(16 + (24 * 2));
        Body(resource.ToDds()!).Should().Equal(Body(dds));
    }

    [Fact]
    public void ImportToRle_Rles_Stream4_HasSixteenFFPerTranslucentBlock()
    {
        var dds = BuildDds(8, 4, 1, TranslucentBlock(1), TranslucentBlock(2));

        var raw = RleResource.EncodeFromDds(dds, RleVersion.Rles);
        var resource = Create(raw);

        var stream4 = raw.AsSpan(resource.MipHeaders[0].Offset4).ToArray();
        stream4.Should().HaveCount(32);
        stream4.Should().OnlyContain(b => b == 0xFF);
    }

    [Fact]
    public void ToBlock4Dds_Rles_MapsOperationsAsLegacy()
    {
        var dds = BuildDds(16, 4, 1,
            TransparentBlock(), OpaqueBlock(1), TranslucentBlock(2), TransparentBlock());
        var resource = Create(RleResource.EncodeFromDds(dds, RleVersion.Rles));

        var block4 = resource.ToBlock4Dds()!;

        var body = Body(block4);
        body.Should().HaveCount(64);
        body[..16].Should().Equal(TransparentBlock());
        body[16..32].Should().Equal([.. OpaqueAlpha, .. OpaqueAlpha]);
        body[32..48].Should().Equal(Enumerable.Repeat((byte)0xFF, 16), "translucent blocks yield stream 4 bytes");
        body[48..64].Should().Equal(TransparentBlock());
    }

    [Fact]
    public void ToBlock4Dds_Rle2_ReturnsNull()
    {
        var dds = BuildDds(4, 4, 1, OpaqueBlock(1));

        Create(RleResource.EncodeFromDds(dds)).ToBlock4Dds().Should().BeNull();
    }

    [Fact]
    public void ImportToRle_Instance_ReplacesDataAndMarksDirty()
    {
        var resource = Create([]);
        var raised = 0;
        resource.Changed += (_, _) => raised++;
        var dds = BuildDds(4, 4, 1, TranslucentBlock(1));

        resource.ImportToRle(dds, RleVersion.Rles);

        raised.Should().Be(1);
        resource.IsDirty.Should().BeTrue();
        resource.Version.Should().Be(RleVersion.Rles);
        resource.Width.Should().Be(4);
        resource.Data.ToArray().Should().Equal(resource.RawData.ToArray());
        Body(resource.ToDds()!).Should().Equal(Body(dds));
    }

    [Fact]
    public void ImportToRle_LongRun_SplitsAtMaxCommandCount()
    {
        // 16384 transparent blocks in one row (65532 + 3 = 4 * 16384 wide) exceeds the 0x3FFF command limit
        const int width = 65535;
        var blocks = (width + 3) / 4;
        blocks.Should().Be(16384);
        var dds = DdsTestData.Build(DdsFormat.Dxt5, width, 4, [], 1).ToList();
        for (var i = 0; i < blocks; i++)
            dds.AddRange(TransparentBlock());

        var rle = RleResource.EncodeFromDds([.. dds]);
        var resource = Create(rle);

        var offset = resource.MipHeaders[0].CommandOffset;
        var first = BitConverter.ToUInt16(rle, offset);
        var second = BitConverter.ToUInt16(rle, offset + 2);
        (first >> 2).Should().Be(0x3FFF);
        (second >> 2).Should().Be(1);
        resource.ToDds()!.Length.Should().Be(dds.Count);
    }

    [Fact]
    public void ImportToRle_ZeroMipCount_IsTreatedAsOneLevel()
    {
        var dds = BuildDds(4, 4, 1, OpaqueBlock(1));
        BitConverter.GetBytes(0).CopyTo(dds, 28);

        var resource = Create(RleResource.EncodeFromDds(dds));

        resource.MipCount.Should().Be(1);
    }

    [Fact]
    public void ImportToRle_RejectsInvalidInput()
    {
        var good = BuildDds(4, 4, 1, OpaqueBlock(1));

        ((Action)(() => RleResource.EncodeFromDds(new byte[50]))).Should().Throw<InvalidDataException>();

        var badMagic = (byte[])good.Clone();
        badMagic[0] = 0;
        ((Action)(() => RleResource.EncodeFromDds(badMagic))).Should().Throw<InvalidDataException>();

        var dxt1 = DdsTestData.Build(DdsFormat.Dxt1, 4, 4, new byte[8]);
        ((Action)(() => RleResource.EncodeFromDds(dxt1))).Should().Throw<InvalidDataException>().WithMessage("*DXT5*");

        var truncated = good[..^1];
        ((Action)(() => RleResource.EncodeFromDds(truncated))).Should().Throw<InvalidDataException>().WithMessage("*truncated*");

        var tooManyMips = (byte[])good.Clone();
        BitConverter.GetBytes(17).CopyTo(tooManyMips, 28);
        ((Action)(() => RleResource.EncodeFromDds(tooManyMips))).Should().Throw<InvalidDataException>();

        var hugeWidth = (byte[])good.Clone();
        BitConverter.GetBytes(70000).CopyTo(hugeWidth, 16);
        ((Action)(() => RleResource.EncodeFromDds(hugeWidth))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ToDds_InconsistentStreamOffsets_Throws()
    {
        var dds = BuildDds(4, 4, 1, OpaqueBlock(1));
        var rle = RleResource.EncodeFromDds(dds);
        // append garbage to stream 1 region so the end offset no longer matches the sentinel
        var corrupted = rle.Concat(new byte[6]).ToArray();
        var resource = Create(corrupted);

        // sentinel Offset1 = data length (now 6 larger) while the opaque block consumed nothing from stream 1
        ((Action)(() => resource.ToDds())).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ToDds_TruncatedStreams_ThrowsInsteadOfDroppingBlocks()
    {
        var dds = BuildDds(4, 4, 1, TranslucentBlock(1));
        var rle = RleResource.EncodeFromDds(dds);
        // claim 5 translucent blocks in the command word
        var command = (ushort)((5 << 2) | 1);
        var offset = BitConverter.ToInt32(rle, 16);
        BitConverter.GetBytes(command).CopyTo(rle, offset);

        var resource = Create(rle);

        ((Action)(() => resource.ToDds())).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ToDds_UnsupportedOperation_Throws()
    {
        var rle = RleResource.EncodeFromDds(BuildDds(4, 4, 1, OpaqueBlock(1)));
        var offset = BitConverter.ToInt32(rle, 16);
        BitConverter.GetBytes((ushort)((1 << 2) | 3)).CopyTo(rle, offset);

        ((Action)(() => Create(rle).ToDds())).Should().Throw<InvalidDataException>().WithMessage("*operation*");
    }

    [Fact]
    public void Parse_OffsetOutsideResource_Throws()
    {
        var rle = RleResource.EncodeFromDds(BuildDds(4, 4, 1, OpaqueBlock(1)));
        BitConverter.GetBytes(rle.Length + 100).CopyTo(rle, 16);

        ((Action)(() => Create(rle))).Should().Throw<InvalidDataException>();

        var negative = RleResource.EncodeFromDds(BuildDds(4, 4, 1, OpaqueBlock(1)));
        BitConverter.GetBytes(-4).CopyTo(negative, 20);
        ((Action)(() => Create(negative))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ToDds_CommandBytesExpandBeyondLimit_Throws()
    {
        // A small file whose commands claim far more output than the guard allows
        var rle = RleResource.EncodeFromDds(BuildDds(4, 4, 1, TransparentBlock()));
        var offset = BitConverter.ToInt32(rle, 16);
        var many = new List<byte>(rle);
        var next = BitConverter.ToInt32(rle, 20); // stream 2 start = end of commands
        // insert 1100 commands of 0x3FFF transparent blocks (16 bytes each = 262 KB) -> > 256 MB total
        var commands = new byte[1100 * 2];
        for (var i = 0; i < 1100; i++)
            BitConverter.GetBytes((ushort)(0x3FFF << 2)).CopyTo(commands, i * 2);
        many.RemoveRange(offset, next - offset);
        many.InsertRange(offset, commands);
        var grown = commands.Length - (next - offset);
        // shift the stream offsets (stream 2, 3, 0, 1) of the single mip header past the longer command stream
        var data = many.ToArray();
        for (var h = 1; h < 5; h++)
            BitConverter.GetBytes(BitConverter.ToInt32(data, 16 + (h * 4)) + grown).CopyTo(data, 16 + (h * 4));

        ((Action)(() => Create(data).ToDds())).Should().Throw<InvalidDataException>().WithMessage("*maximum*");
    }
}
