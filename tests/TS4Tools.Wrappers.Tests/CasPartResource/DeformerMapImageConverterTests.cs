using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Tests for <see cref="DeformerMapImageConverter"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/DeformerMapResource.cs ToBitMap (lines 296-488)
///   and s4pe Helpers/DMAPImageHelper/Program.cs (/export = Skin, /exportRobe = Robe).
/// - Uncompressed scan lines hold 6 bytes per pixel with a robe channel (skin RGB then robe RGB), otherwise 3.
/// - Compressed scan lines hold NumIndexes (pixelPos, dataPos) pairs and RLE runs of [length][pixelSize bytes];
///   lookup step is 1 + width / (NumIndexes - 1).
/// - ISCOPY copies skin bytes into robe, DROPPED yields zeros.
/// </summary>
public class DeformerMapImageConverterTests
{
    private static DeformerMapResource NewMap(uint width, uint height)
    {
        var map = new DeformerMapResource(new ResourceKey(DeformerMapResource.TypeId, 0, 0), ReadOnlyMemory<byte>.Empty)
        {
            MinCol = 0,
            MaxCol = width - 1,
            MinRow = 0,
            MaxRow = height - 1,
        };
        return map;
    }

    private static ScanLine Uncompressed(RobeChannel robe, params byte[] pixels) => new()
    {
        IsCompressed = false,
        RobeChannel = robe,
        UncompressedPixels = pixels,
    };

    [Fact]
    public void ToImage_EmptyMap_ReturnsNull()
    {
        var map = NewMap(1, 1);
        map.MaxCol = 0;

        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin).Should().BeNull();
        DeformerMapImageConverter.ToBitmap(map, DeformerMapImageType.Skin).Should().BeNull();
    }

    [Fact]
    public void ToImage_SingleColumnMap_IsTreatedAsEmptyLikeLegacy()
    {
        // Legacy tests MaxCol == 0 to mean "no image", so a one column map (MinCol = MaxCol = 0) cannot be exported
        var map = NewMap(1, 1);
        map.ScanLines = [Uncompressed(RobeChannel.Dropped, 1, 2, 3)];

        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin).Should().BeNull();
    }

    [Fact]
    public void ToImage_UncompressedWithRobe_SplitsSkinAndRobe()
    {
        var map = NewMap(2, 1);
        map.HasRobeChannel = RobeChannel.Present;
        map.ScanLines = [Uncompressed(RobeChannel.Present, 1, 2, 3, 10, 20, 30, 4, 5, 6, 40, 50, 60)];

        var skin = DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin)!;
        var robe = DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Robe)!;

        skin.Width.Should().Be(2);
        skin.Height.Should().Be(1);
        skin.Pixels.Should().Equal(1, 2, 3, 4, 5, 6);
        robe.Pixels.Should().Equal(10, 20, 30, 40, 50, 60);
    }

    [Fact]
    public void ToImage_RobeDropped_GivesZeroRobe()
    {
        var map = NewMap(2, 1);
        map.ScanLines = [Uncompressed(RobeChannel.Dropped, 1, 2, 3, 4, 5, 6)];

        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin)!.Pixels.Should().Equal(1, 2, 3, 4, 5, 6);
        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Robe)!.Pixels.Should().Equal(0, 0, 0, 0, 0, 0);
    }

    [Fact]
    public void ToImage_RobeIsCopy_RepeatsSkin()
    {
        var map = NewMap(2, 1);
        map.ScanLines = [Uncompressed(RobeChannel.IsCopy, 1, 2, 3, 4, 5, 6)];

        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Robe)!.Pixels.Should().Equal(1, 2, 3, 4, 5, 6);
    }

    [Fact]
    public void ToImage_MixedRobeChannelsPerScanLine_UseEachLinesPixelSize()
    {
        var map = NewMap(2, 2);
        map.ScanLines =
        [
            Uncompressed(RobeChannel.Present, 1, 2, 3, 7, 8, 9, 1, 2, 3, 7, 8, 9),
            Uncompressed(RobeChannel.Dropped, 4, 5, 6, 4, 5, 6),
        ];

        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin)!.Pixels.Should().Equal(1, 2, 3, 1, 2, 3, 4, 5, 6, 4, 5, 6);
        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Robe)!.Pixels.Should().Equal(7, 8, 9, 7, 8, 9, 0, 0, 0, 0, 0, 0);
    }

    [Fact]
    public void ToImage_CompressedScanLine_DecodesRunsThroughIndexTable()
    {
        // width 4, 2 indexes -> step = 1 + 4 / 1 = 5; every pixel starts at index 0
        var map = NewMap(4, 1);
        map.HasRobeChannel = RobeChannel.Dropped;
        map.ScanLines =
        [
            new ScanLine
            {
                IsCompressed = true,
                RobeChannel = RobeChannel.Dropped,
                NumIndexes = 2,
                PixelPosIndexes = [0, 0],
                DataPosIndexes = [0, 0],
                // runs: 2 x (1,2,3) then 2 x (9,8,7)
                RleArrayOfPixels = [2, 1, 2, 3, 2, 9, 8, 7],
            },
        ];

        var skin = DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin)!;

        skin.Pixels.Should().Equal(1, 2, 3, 1, 2, 3, 9, 8, 7, 9, 8, 7);
    }

    [Fact]
    public void ToImage_CompressedWithRobe_UsesSixByteRuns()
    {
        var map = NewMap(2, 1);
        map.ScanLines =
        [
            new ScanLine
            {
                IsCompressed = true,
                RobeChannel = RobeChannel.Present,
                NumIndexes = 2,
                PixelPosIndexes = [0, 1],
                DataPosIndexes = [0, 1],
                // two runs of one pixel: 6 bytes + length byte each (pixelsize + 1 = 7)
                RleArrayOfPixels = [1, 1, 2, 3, 11, 12, 13, 1, 4, 5, 6, 14, 15, 16],
            },
        ];

        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin)!.Pixels.Should().Equal(1, 2, 3, 4, 5, 6);
        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Robe)!.Pixels.Should().Equal(11, 12, 13, 14, 15, 16);
    }

    [Fact]
    public void ToImage_InvalidData_ThrowsInvalidData()
    {
        var tooFewLines = NewMap(2, 3);
        tooFewLines.ScanLines = [Uncompressed(RobeChannel.Dropped, 1, 2, 3)];
        ((Action)(() => DeformerMapImageConverter.ToImage(tooFewLines, DeformerMapImageType.Skin))).Should().Throw<InvalidDataException>();

        var shortPixels = NewMap(2, 1);
        shortPixels.ScanLines = [Uncompressed(RobeChannel.Dropped, 1, 2, 3)];
        ((Action)(() => DeformerMapImageConverter.ToImage(shortPixels, DeformerMapImageType.Skin))).Should().Throw<InvalidDataException>();

        var oneIndex = NewMap(2, 1);
        oneIndex.ScanLines = [new ScanLine { IsCompressed = true, RobeChannel = RobeChannel.Dropped, NumIndexes = 1, PixelPosIndexes = [0], DataPosIndexes = [0], RleArrayOfPixels = [2, 1, 2, 3] }];
        ((Action)(() => DeformerMapImageConverter.ToImage(oneIndex, DeformerMapImageType.Skin))).Should().Throw<InvalidDataException>("legacy would divide by zero");

        var badDataPos = NewMap(2, 1);
        badDataPos.ScanLines = [new ScanLine { IsCompressed = true, RobeChannel = RobeChannel.Dropped, NumIndexes = 2, PixelPosIndexes = [0, 0], DataPosIndexes = [50, 0], RleArrayOfPixels = [2, 1, 2, 3] }];
        ((Action)(() => DeformerMapImageConverter.ToImage(badDataPos, DeformerMapImageType.Skin))).Should().Throw<InvalidDataException>();

        var runawayRuns = NewMap(4, 1);
        runawayRuns.ScanLines = [new ScanLine { IsCompressed = true, RobeChannel = RobeChannel.Dropped, NumIndexes = 2, PixelPosIndexes = [0, 0], DataPosIndexes = [0, 0], RleArrayOfPixels = [1, 1, 2, 3] }];
        ((Action)(() => DeformerMapImageConverter.ToImage(runawayRuns, DeformerMapImageType.Skin))).Should().Throw<InvalidDataException>("the run list ends before the width is covered");

        var tooBig = NewMap(1, 1);
        tooBig.MaxCol = 100_000;
        ((Action)(() => DeformerMapImageConverter.ToImage(tooBig, DeformerMapImageType.Skin))).Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ToBitmap_WritesPaddedTopDownRows()
    {
        var map = NewMap(3, 2);
        map.ScanLines =
        [
            Uncompressed(RobeChannel.Dropped, 1, 2, 3, 4, 5, 6, 7, 8, 9),
            Uncompressed(RobeChannel.Dropped, 11, 12, 13, 14, 15, 16, 17, 18, 19),
        ];

        var bmp = DeformerMapImageConverter.ToBitmap(map, DeformerMapImageType.Skin)!;

        // 3 pixels * 3 bytes = 9 bytes per row, padded to 12
        bmp.Length.Should().Be(54 + (12 * 2));
        BitConverter.ToUInt16(bmp, 0).Should().Be(0x4D42);
        BitConverter.ToInt32(bmp, 2).Should().Be(bmp.Length);
        BitConverter.ToInt32(bmp, 10).Should().Be(54);
        BitConverter.ToInt32(bmp, 14).Should().Be(40);
        BitConverter.ToInt32(bmp, 18).Should().Be(3);
        BitConverter.ToInt32(bmp, 22).Should().Be(2);
        BitConverter.ToUInt16(bmp, 26).Should().Be(1);
        BitConverter.ToUInt16(bmp, 28).Should().Be(24);
        bmp[54..63].Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9);
        bmp[63..66].Should().Equal(0, 0, 0);
        bmp[66..75].Should().Equal(11, 12, 13, 14, 15, 16, 17, 18, 19);
    }

    [Fact]
    public void ToBitmap_FourPixelWidth_NeedsNoPadding()
    {
        var map = NewMap(4, 1);
        map.ScanLines = [Uncompressed(RobeChannel.Dropped, new byte[12])];

        DeformerMapImageConverter.ToBitmap(map, DeformerMapImageType.Skin)!.Length.Should().Be(54 + 12);
    }

    [Fact]
    public void ImportFromImage_RoundTripsThroughToImage()
    {
        var map = NewMap(1, 1);
        var skin = Enumerable.Range(0, 3 * 2 * 3).Select(i => (byte)i).ToArray();
        var robe = Enumerable.Range(100, 3 * 2 * 3).Select(i => (byte)i).ToArray();

        DeformerMapImageConverter.ImportFromImage(map, skin, robe, 3, 2);

        map.MinCol.Should().Be(0u);
        map.MaxCol.Should().Be(2u);
        map.MaxRow.Should().Be(1u);
        map.HasRobeChannel.Should().Be(RobeChannel.Present);
        map.ScanLines.Should().HaveCount(2);
        map.ScanLines[0].UncompressedPixels.Should().HaveCount(18);
        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin)!.Pixels.Should().Equal(skin);
        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Robe)!.Pixels.Should().Equal(robe);
    }

    [Fact]
    public void ImportFromImage_WithoutRobe_MarksRobeDropped()
    {
        var map = NewMap(1, 1);

        DeformerMapImageConverter.ImportFromImage(map, new byte[2 * 1 * 3], default, 2, 1);

        map.HasRobeChannel.Should().Be(RobeChannel.Dropped);
        map.ScanLines[0].RobeChannel.Should().Be(RobeChannel.Dropped);
        map.ScanLines[0].UncompressedPixels.Should().HaveCount(6);
        DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Robe)!.Pixels.Should().OnlyContain(b => b == 0);
    }

    [Fact]
    public void ImportFromImage_SurvivesResourceSerialization()
    {
        var map = NewMap(1, 1);
        var skin = Enumerable.Range(0, 5 * 3 * 3).Select(i => (byte)(i * 3)).ToArray();
        DeformerMapImageConverter.ImportFromImage(map, skin, default, 5, 3);

        var reloaded = new DeformerMapResource(map.Key, map.Data.ToArray());

        DeformerMapImageConverter.ToImage(reloaded, DeformerMapImageType.Skin)!.Pixels.Should().Equal(skin);
    }

    [Fact]
    public void ImportFromImage_ValidatesArguments()
    {
        var map = NewMap(1, 1);

        ((Action)(() => DeformerMapImageConverter.ImportFromImage(map, new byte[5], default, 2, 1))).Should().Throw<ArgumentException>();
        ((Action)(() => DeformerMapImageConverter.ImportFromImage(map, new byte[6], new byte[3], 2, 1))).Should().Throw<ArgumentException>();
        ((Action)(() => DeformerMapImageConverter.ImportFromImage(map, new byte[6], default, 0, 1))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => DeformerMapImageConverter.ImportFromImage(map, new byte[6], default, 9000, 1))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => DeformerMapImageConverter.ImportFromImage(null!, new byte[6], default, 2, 1))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImportFromBitmap_RoundTripsToBitmapOutput()
    {
        var map = NewMap(3, 2);
        map.ScanLines =
        [
            Uncompressed(RobeChannel.Dropped, 1, 2, 3, 4, 5, 6, 7, 8, 9),
            Uncompressed(RobeChannel.Dropped, 11, 12, 13, 14, 15, 16, 17, 18, 19),
        ];
        var bmp = DeformerMapImageConverter.ToBitmap(map, DeformerMapImageType.Skin)!;

        var imported = NewMap(1, 1);
        DeformerMapImageConverter.ImportFromBitmap(imported, bmp);

        DeformerMapImageConverter.ToImage(imported, DeformerMapImageType.Skin)!.Pixels
            .Should().Equal(DeformerMapImageConverter.ToImage(map, DeformerMapImageType.Skin)!.Pixels);
    }

    [Fact]
    public void ImportFromBitmap_RejectsUnsupportedBitmaps()
    {
        var map = NewMap(1, 1);
        var source = NewMap(2, 1);
        source.ScanLines = [Uncompressed(RobeChannel.Dropped, new byte[6])];
        var bmp = DeformerMapImageConverter.ToBitmap(source, DeformerMapImageType.Skin)!;

        ((Action)(() => DeformerMapImageConverter.ImportFromBitmap(map, new byte[10]))).Should().Throw<InvalidDataException>();

        var notBm = (byte[])bmp.Clone();
        notBm[0] = 0;
        ((Action)(() => DeformerMapImageConverter.ImportFromBitmap(map, notBm))).Should().Throw<InvalidDataException>();

        var bits32 = (byte[])bmp.Clone();
        bits32[28] = 32;
        ((Action)(() => DeformerMapImageConverter.ImportFromBitmap(map, bits32))).Should().Throw<InvalidDataException>();

        var truncated = bmp[..^6];
        ((Action)(() => DeformerMapImageConverter.ImportFromBitmap(map, truncated))).Should().Throw<InvalidDataException>();

        var negative = (byte[])bmp.Clone();
        BitConverter.GetBytes(-1).CopyTo(negative, 22);
        DeformerMapImageConverter.ImportFromBitmap(map, negative); // negative height = top-down: same rows, accepted
        map.MaxRow.Should().Be(0u);
    }
}
