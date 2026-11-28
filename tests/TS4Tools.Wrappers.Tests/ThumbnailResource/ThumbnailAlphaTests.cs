using FluentAssertions;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Alpha compositing helpers. Source: ThumbnailResource.cs lines 138-142, 252-256, 311-316 - the alpha of the
/// result is the R channel of the decoded ALFA PNG; mismatched dimensions are rejected.
/// </summary>
public class ThumbnailAlphaTests
{
    [Fact]
    public void ApplyAlpha_UsesRedChannelOfAlphaImage_AndKeepsColour()
    {
        byte[] rgba = [10, 20, 30, 255, 40, 50, 60, 255];
        byte[] alpha = [7, 99, 99, 255, 200, 1, 2, 3];

        ThumbnailResource.ApplyAlpha(rgba, alpha);

        rgba.Should().Equal(10, 20, 30, 7, 40, 50, 60, 200);
    }

    [Fact]
    public void ApplyAlpha_SizeMismatch_Throws()
    {
        var act = () => ThumbnailResource.ApplyAlpha(new byte[8], new byte[4]);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ExtractAlphaPlane_ThenApplyAlphaPlane_MatchesApplyAlpha()
    {
        byte[] alpha = [7, 0, 0, 255, 200, 1, 2, 3];
        byte[] direct = [1, 2, 3, 255, 4, 5, 6, 255];
        byte[] viaPlane = (byte[])direct.Clone();

        var plane = ThumbnailResource.ExtractAlphaPlane(alpha);
        ThumbnailResource.ApplyAlpha(direct, alpha);
        ThumbnailResource.ApplyAlphaPlane(viaPlane, plane);

        plane.Should().Equal(7, 200);
        viaPlane.Should().Equal(direct);
    }

    [Fact]
    public void ApplyAlphaPlane_WrongLength_Throws()
    {
        var act = () => ThumbnailResource.ApplyAlphaPlane(new byte[8], new byte[3]);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void GetAlphaPngData_ExposesEmbeddedPngBytes()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];
        var data = new byte[24 + 8 + png.Length];
        BitConverter.GetBytes(0x41464C41u).CopyTo(data, 24);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(28), png.Length);
        png.CopyTo(data, 32);

        var thumb = new ThumbnailResource(new ResourceKey(0x3C1AF1F2, 0, 1), data);

        thumb.HasEmbeddedAlpha.Should().BeTrue();
        thumb.GetAlphaPngData().ToArray().Should().Equal(png);
    }

    /// <summary>
    /// Legacy ThumbnailResource.cs lines 122-125 only uses the ALFA block when the PNG fits in the data. A length near
    /// int.MaxValue used to overflow "offset + 8 + length" to a negative number, pass the check and claim alpha.
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MaxValue - 20)]
    public void HugeAlfaLength_IsIgnored(int length)
    {
        var data = new byte[24 + 8 + 16];
        BitConverter.GetBytes(0x41464C41u).CopyTo(data, 24);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(28), length);

        var thumb = new ThumbnailResource(new ResourceKey(0x3C1AF1F2, 0, 1), data);

        thumb.HasEmbeddedAlpha.Should().BeFalse();
        thumb.GetAlphaPngData().IsEmpty.Should().BeTrue();
    }
}
