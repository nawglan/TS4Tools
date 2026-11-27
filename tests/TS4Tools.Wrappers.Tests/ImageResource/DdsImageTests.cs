using FluentAssertions;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for <see cref="DdsImage"/>, <see cref="ColorHsva"/> and <see cref="HsvShift"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Extras/DDSPanel/DdsFile.cs (DdsFile: SetAlphaFromGreyscale lines 1392-1412,
///   DeleteAlphaChannel 1454-1466, GetGreyscaleFromAlpha 1468-1480, Resize 1070-1127, UseDXT/UseLuminance 1325-1362)
///   and RGBHSV.cs (ColorHSVA, HSVShift).
/// </summary>
public class DdsImageTests
{
    // ---- HSV ----

    [Theory]
    [InlineData(0xFFFF0000u, 0, 255, 255)]   // red
    [InlineData(0xFF00FF00u, 85, 255, 255)]  // green: hue 1/3 * 255
    [InlineData(0xFF0000FFu, 170, 255, 255)] // blue: hue 2/3 * 255
    [InlineData(0xFFFFFFFFu, 0, 0, 255)]     // white
    [InlineData(0xFF000000u, 0, 0, 0)]       // black
    [InlineData(0xFF808080u, 0, 0, 128)]     // grey
    public void ColorHsva_FromArgb_MatchesLegacyConversion(uint argb, int h, int s, int v)
    {
        var hsva = ColorHsva.FromArgb(argb);

        hsva.H.Should().Be((byte)h);
        hsva.S.Should().Be((byte)s);
        hsva.V.Should().Be((byte)v);
        hsva.A.Should().Be(0xFF);
    }

    [Fact]
    public void ColorHsva_FromArgb_PreservesAlpha()
    {
        ColorHsva.FromArgb(0x40FF0000).A.Should().Be(0x40);
        new ColorHsva(0, 255, 255, 0x40).ToArgb().Should().Be(0x40FF0000u);
    }

    [Theory]
    [InlineData(0xFFFF0000u)]
    [InlineData(0xFF00FF00u)]
    [InlineData(0xFF0000FFu)]
    [InlineData(0xFFFFFFFFu)]
    [InlineData(0xFF000000u)]
    [InlineData(0xFF808080u)]
    public void ColorHsva_PrimaryColours_RoundTrip(uint argb)
    {
        ColorHsva.FromArgb(argb).ToArgb().Should().Be(argb);
    }

    [Fact]
    public void ColorHsva_Shift_HueWrapsAroundNegative()
    {
        // red shifted by -1/3 hue lands on blue (hue 2/3)
        var shifted = ColorHsva.FromArgb(0xFFFF0000).Shift(new HsvShift(-1f / 3f, 0, 0));

        shifted.H.Should().BeInRange(169, 171);
    }

    [Fact]
    public void ColorHsva_Shift_SaturationAndValueClamp()
    {
        var shifted = ColorHsva.FromArgb(0xFF804020).Shift(new HsvShift(0, 5f, -5f));

        shifted.S.Should().Be(255);
        shifted.V.Should().Be(0);
    }

    [Fact]
    public void ColorHsva_Shift_EmptyShiftIsIdentityForHsv()
    {
        var hsva = new ColorHsva(10, 20, 30, 40);

        hsva.Shift(HsvShift.Empty).Should().Be(hsva);
        HsvShift.Empty.IsEmpty.Should().BeTrue();
        new HsvShift(0, 0.1f, 0).IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ColorHsva_ArrayConversions_UseShiftOnlyWhenNotEmpty()
    {
        uint[] argb = [0xFFFF0000, 0xFF00FF00];
        var hsva = ColorHsva.FromArgbArray(argb);

        ColorHsva.ToArgbArray(hsva).Should().Equal(argb);
        ColorHsva.ToArgbArray(hsva, new HsvShift(0, 0, -1f)).Should().Equal(0xFF000000u, 0xFF000000u);
    }

    [Fact]
    public void DdsImage_ApplyHsvShift_ChangesPixels()
    {
        var image = DdsImage.CreateSolid(0xFFFF0000, 2, 2);

        image.ApplyHsvShift(new HsvShift(1f / 3f, 0, 0));

        image.GetPixel(0, 0).Should().Be(0xFF00FF00u);
    }

    [Fact]
    public void DdsImage_ToRgba32_AppliesHsvShiftOnlyWhenSupported()
    {
        var image = DdsImage.CreateSolid(0xFFFF0000, 1, 1);
        image.HsvShift = new HsvShift(1f / 3f, 0, 0);

        image.ToRgba32().Should().Equal(255, 0, 0, 255); // HSV support off: shift ignored

        image.SupportsHsv = true;
        image.ToRgba32().Should().Equal(0, 255, 0, 255);
        image.ToRgba32(applyHsvShift: false).Should().Equal(255, 0, 0, 255);
        image.GetArgb()[0].Should().Be(0xFFFF0000u, "base pixels are not mutated by a display shift");
    }

    // ---- alpha ----

    [Fact]
    public void SetAlphaFromGreyscale_UsesAverageOfRgb()
    {
        var image = DdsImage.CreateSolid(0xFF112233, 2, 1);
        var grey = DdsImage.FromArgb([0xFF000000, 0xFF303030], 2, 1);

        image.SetAlphaFromGreyscale(grey);

        image.GetPixel(0, 0).Should().Be(0x00112233u);
        image.GetPixel(1, 0).Should().Be(0x30112233u);
        image.AlphaDepth.Should().Be(5, "DXT images use alpha depth 5");
    }

    [Fact]
    public void SetAlphaFromGreyscale_AveragesUnequalChannelsWithIntegerDivision()
    {
        var image = DdsImage.CreateSolid(0xFFFFFFFF, 1, 1);

        image.SetAlphaFromGreyscale(DdsImage.FromArgb([0xFF0A141E], 1, 1)); // (10 + 20 + 30) / 3 = 20

        (image.GetPixel(0, 0) >> 24).Should().Be(20u);
    }

    [Fact]
    public void SetAlphaFromGreyscale_TilesSmallerSource()
    {
        var image = DdsImage.CreateSolid(0xFF000000, 4, 1);
        var grey = DdsImage.FromArgb([0xFF101010, 0xFF202020], 2, 1);

        image.SetAlphaFromGreyscale(grey);

        image.GetArgb().Select(p => p >> 24).Should().Equal(0x10u, 0x20u, 0x10u, 0x20u);
    }

    [Fact]
    public void SetAlphaFromGreyscale_NonSquareImage_WritesEveryPixel()
    {
        // Legacy SetPixel indexes y*height+x, corrupting non-square images; this port must not
        var image = DdsImage.CreateSolid(0xFF000000, 5, 2);

        image.SetAlphaFromGreyscale(DdsImage.CreateSolid(0xFF404040, 1, 1));

        image.GetArgb().Should().OnlyContain(p => (p >> 24) == 0x40);
    }

    [Fact]
    public void SetAlphaFromGreyscale_NotDxt_UsesAlphaDepthEight()
    {
        var image = DdsImage.CreateSolid(0xFF000000, 1, 1);
        image.SetUseDxt(false);

        image.SetAlphaFromGreyscale(DdsImage.CreateSolid(0xFF808080, 1, 1));

        image.AlphaDepth.Should().Be(8);
    }

    [Fact]
    public void DeleteAlphaChannel_MakesOpaqueAndSetsAlphaDepth()
    {
        var image = DdsImage.CreateSolid(0x20112233, 2, 2);

        image.DeleteAlphaChannel();

        image.GetArgb().Should().OnlyContain(p => p == 0xFF112233u);
        image.AlphaDepth.Should().Be(1, "DXT without alpha becomes DXT1");
        image.SaveFormat.Should().Be(DdsFormat.Dxt1);

        image.SetUseDxt(false);
        image.DeleteAlphaChannel();
        image.AlphaDepth.Should().Be(0);
        image.SaveFormat.Should().Be(DdsFormat.R8G8B8);
    }

    [Fact]
    public void DeleteAlphaChannel_OnLuminanceMap_Throws()
    {
        var image = DdsImage.CreateSolid(0xFF000000, 1, 1);
        image.SetUseLuminance(true);

        ((Action)image.DeleteAlphaChannel).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetGreyscaleFromAlpha_ReplicatesAlphaIntoRgb()
    {
        var image = DdsImage.FromArgb([0x40112233, 0xFF000000], 2, 1);

        var grey = image.GetGreyscaleFromAlpha();

        grey.GetArgb().Should().Equal(0xFF404040u, 0xFFFFFFFFu);
    }

    [Fact]
    public void UseDxtAndLuminance_InteractAsInLegacy()
    {
        var image = DdsImage.CreateSolid(0, 1, 1);

        image.SetUseLuminance(true);
        image.UseDxt.Should().BeFalse();
        image.UseLuminance.Should().BeTrue();

        image.SetUseDxt(true);
        image.UseLuminance.Should().BeFalse();
        image.AlphaDepth.Should().Be(5);

        image.SetUseDxt(false);
        image.AlphaDepth.Should().Be(8);
    }

    // ---- construction / pixel access ----

    [Fact]
    public void FromArgb_ValidatesLength()
    {
        ((Action)(() => DdsImage.FromArgb(new uint[3], 2, 2))).Should().Throw<ArgumentException>();
        ((Action)(() => DdsImage.FromRgba32(new byte[3], 1, 1))).Should().Throw<ArgumentException>();
        ((Action)(() => DdsImage.CreateSolid(0, 0, 1))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => DdsImage.CreateSolid(0, 20000, 1))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetSetPixel_BoundsAndValues()
    {
        var image = DdsImage.CreateSolid(0xFF000000, 3, 2);

        image.SetPixel(2, 1, 0x80ABCDEF);

        image.GetPixel(2, 1).Should().Be(0x80ABCDEFu);
        image.GetArgb()[5].Should().Be(0x80ABCDEFu);
        ((Action)(() => image.GetPixel(3, 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => image.SetPixel(0, 2, 0))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetArgb_ReturnsCopy()
    {
        var image = DdsImage.CreateSolid(0xFF000000, 1, 1);

        image.GetArgb()[0] = 1;

        image.GetPixel(0, 0).Should().Be(0xFF000000u);
    }

    [Fact]
    public void Clone_IsIndependentAndKeepsFormat()
    {
        var image = DdsImage.CreateSolid(0xFF123456, 2, 2);
        image.SetUseDxt(false);
        image.AlphaDepth = 4;
        image.SupportsHsv = true;

        var copy = image.Clone();
        copy.SetPixel(0, 0, 0);

        image.GetPixel(0, 0).Should().Be(0xFF123456u);
        copy.UseDxt.Should().BeFalse();
        copy.AlphaDepth.Should().Be(4);
        copy.SupportsHsv.Should().BeTrue();
    }

    // ---- resize ----

    [Fact]
    public void Resize_SolidColour_StaysSolidAndKeepsAlpha()
    {
        var image = DdsImage.CreateSolid(0x80336699, 4, 4);

        var resized = image.Resize(8, 2);

        resized.Width.Should().Be(8);
        resized.Height.Should().Be(2);
        resized.GetArgb().Should().OnlyContain(p => p == 0x80336699u);
    }

    [Fact]
    public void Resize_ResizesColourAndAlphaIndependently()
    {
        // left half opaque white, right half transparent black: colour must not be premultiplied-darkened by alpha
        var image = DdsImage.FromArgb([0xFFFFFFFF, 0xFFFFFFFF, 0x00FFFFFF, 0x00FFFFFF], 4, 1);

        var resized = image.Resize(2, 1);

        resized.GetPixel(0, 0).Should().Be(0xFFFFFFFFu);
        resized.GetPixel(1, 0).Should().Be(0x00FFFFFFu, "colour channels keep their value, alpha is resized separately");
    }

    [Fact]
    public void Resize_PreservesFormatFlags()
    {
        var image = DdsImage.CreateSolid(0xFF000000, 4, 4);
        image.SetUseDxt(false);
        image.AlphaDepth = 1;

        var resized = image.Resize(2, 2);

        resized.UseDxt.Should().BeFalse();
        resized.AlphaDepth.Should().Be(8, "Resize re-applies alpha via SetAlphaFromGreyscale, as legacy does");
    }

    [Fact]
    public void Resize_Gradient_InterpolatesMonotonically()
    {
        uint[] row = [0xFF000000, 0xFF404040, 0xFF808080, 0xFFC0C0C0];
        var image = DdsImage.FromArgb(row, 4, 1);

        var resized = image.Resize(8, 1).GetArgb().Select(p => p & 0xFF).ToArray();

        resized.Should().BeInAscendingOrder();
        resized[0].Should().Be(0);
        resized[7].Should().Be(0xC0);
    }

    [Fact]
    public void Resize_LuminanceMap_RebuildsFromAlpha()
    {
        var image = DdsImage.FromArgb([0x80000000, 0x80000000], 2, 1);
        image.SetUseDxt(false);
        image.SetUseLuminance(true);

        var resized = image.Resize(4, 1);

        resized.UseLuminance.Should().BeTrue();
        resized.GetArgb().Should().OnlyContain(p => (p >> 24) == 0x80);
    }

    [Fact]
    public void Load_ThenResizeThenSave_ProducesValidDds()
    {
        var source = DdsImage.FromRgba32(DdsTestData.Gradient(16, 16), 16, 16).SaveToDds();

        var resized = DdsImage.Load(source).Resize(8, 8).SaveToDds();

        var reloaded = DdsImage.Load(resized);
        reloaded.Width.Should().Be(8);
        reloaded.Height.Should().Be(8);
    }
}
