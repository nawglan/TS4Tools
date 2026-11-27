using FluentAssertions;
using SkiaSharp;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Thumbnails show the JPEG colour data combined with the ALFA alpha PNG.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ThumbnailResource.cs
/// lines 119-160 (TransformToPNG) and 195-234 (UpdateAlpha).
/// </remarks>
public sealed class ThumbnailCompositeTests
{
    private static readonly ResourceKey ThumbKey = new(0x3C2A8647, 0, 1);

    private static byte[] Jpeg(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        data.Should().NotBeNull();
        return data.ToArray();
    }

    /// <summary>A grey PNG whose pixels are the alpha values (R = G = B = alpha).</summary>
    private static byte[] AlphaPng(int width, int height, Func<int, int, byte> alphaAt)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var a = alphaAt(x, y);
                bitmap.SetPixel(x, y, new SKColor(a, a, a, 255));
            }
        }
        using var ms = new MemoryStream();
        bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
        return ms.ToArray();
    }

    /// <summary>Inserts the ALFA segment after the JFIF APP0 (SOI + 18-byte APP0), so "ALFA" lands at offset 24.</summary>
    private static byte[] JfifAlfa(byte[] jpeg, byte[] alphaPng)
    {
        using var ms = new MemoryStream();
        ms.Write(jpeg, 0, 20);
        var segmentLength = 2 + 4 + 4 + alphaPng.Length;
        ms.WriteByte(0xFF);
        ms.WriteByte(0xE0);
        ms.WriteByte((byte)(segmentLength >> 8));
        ms.WriteByte((byte)segmentLength);
        ms.Write("ALFA"u8);
        ms.WriteByte((byte)(alphaPng.Length >> 24));
        ms.WriteByte((byte)(alphaPng.Length >> 16));
        ms.WriteByte((byte)(alphaPng.Length >> 8));
        ms.WriteByte((byte)alphaPng.Length);
        ms.Write(alphaPng);
        ms.Write(jpeg, 20, jpeg.Length - 20);
        return ms.ToArray();
    }

    [Fact]
    public void Composite_UsesRgbOfTheJpegAndTheRedChannelOfTheAlphaImage()
    {
        var jpeg = Jpeg(8, 4, new SKColor(200, 100, 50));
        var alpha = AlphaPng(8, 4, (x, _) => x < 4 ? (byte)0 : (byte)255);

        var (rgba, width, height) = ThumbnailCompositor.Composite(jpeg, alpha);

        (width, height).Should().Be((8, 4));
        rgba.Length.Should().Be(8 * 4 * 4);
        rgba[3].Should().Be(0, "the left half is transparent");
        rgba[(7 * 4) + 3].Should().Be(255, "the right half is opaque");
        // Colour is the JPEG's (lossy, so allow a small tolerance)
        rgba[(7 * 4)].Should().BeInRange(195, 205);
        rgba[(7 * 4) + 1].Should().BeInRange(95, 105);
        rgba[(7 * 4) + 2].Should().BeInRange(45, 55);
    }

    [Fact]
    public void Composite_DifferentSizes_IsNotAProperThumbnail()
    {
        var jpeg = Jpeg(8, 4, SKColors.Red);
        var alpha = AlphaPng(4, 4, (_, _) => 255);

        var act = () => ThumbnailCompositor.Composite(jpeg, alpha);

        act.Should().Throw<InvalidDataException>().WithMessage("Not a proper TS4 Thumbnail image");
    }

    [Fact]
    public void Viewer_ThumbnailWithAlfa_DecodesAlphaIntoThePixels()
    {
        var data = JfifAlfa(Jpeg(8, 8, SKColors.Blue), AlphaPng(8, 8, (_, y) => (byte)(y * 32)));
        var resource = new ThumbnailResource(ThumbKey, data);
        resource.HasEmbeddedAlpha.Should().BeTrue();

        var vm = new ImageViewerViewModel();
        vm.LoadThumbnailResource(resource);

        vm.Width.Should().Be(8);
        vm.Height.Should().Be(8);
        vm.DecodedPixels.Should().NotBeNull();
        vm.DecodedPixels![3].Should().Be(0);
        vm.DecodedPixels[(7 * 8 * 4) + 3].Should().Be(224);
    }

    [Fact]
    public void Viewer_ThumbnailWithoutAlfa_KeepsThePlainJpeg()
    {
        var resource = new ThumbnailResource(ThumbKey, Jpeg(8, 8, SKColors.Blue));

        var vm = new ImageViewerViewModel();
        vm.LoadThumbnailResource(resource);

        resource.HasEmbeddedAlpha.Should().BeFalse();
        vm.DecodedPixels.Should().BeNull();
    }
}
