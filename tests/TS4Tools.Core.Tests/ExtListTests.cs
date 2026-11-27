using FluentAssertions;
using TS4Tools.Extensions;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// Tests for <see cref="ExtList"/>.
/// Expected values taken from legacy_references/Sims4Tools/s4pi Extras/Extensions/Extensions.txt.
/// </summary>
public class ExtListTests
{
    [Theory]
    [InlineData(0x220557DAu, "STBL", ".stbl")]
    [InlineData(0x01D10F34u, "MLOD", ".lod")]
    [InlineData(0x015A1849u, "GEOM", ".simgeom")]
    [InlineData(0x0166038Cu, "_KEY", ".keyname")]
    [InlineData(0x319E4F1Du, "COBJ", ".cobj")]
    [InlineData(0x73E93EEBu, "_XML", ".manifest")]
    public void KnownType_ReturnsLegacyTagAndExtension(uint type, string tag, string ext)
    {
        ExtList.IsKnown(type).Should().BeTrue();
        ExtList.GetTag(type).Should().Be(tag);
        ExtList.GetExtension(type).Should().Be(ext);
    }

    [Fact]
    public void UnknownType_ReturnsFallbackEntry()
    {
        ExtList.IsKnown(0xDEADBEEF).Should().BeFalse();
        ExtList.GetTag(0xDEADBEEF).Should().Be("UNKN");
        ExtList.GetExtension(0xDEADBEEF).Should().Be(".bnry");
    }

    [Fact]
    public void Parse_SkipsCommentsAndShortLines()
    {
        var result = ExtList.Parse(new StringReader("; comment\n0x00000001\n\n0x00000002  ABCD   .abc\n"));

        result.Should().ContainSingle();
        result["0x00000002"].Should().Equal("ABCD", ".abc");
    }

    [Fact]
    public void Parse_DuplicateKey_Throws()
    {
        var act = () => ExtList.Parse(new StringReader("0x00000001 A .a\n0x00000001 B .b\n"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*0x00000001*");
    }
}
