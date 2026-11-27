using FluentAssertions;
using Xunit;

namespace TS4Tools.Core.Tests;

/// <summary>
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 182-217
///   - "0x{X8}-0x{X8}-0x{X16}": exactly 3 '-' parts, each must start with "0x"
///   - "(key:){X8}:{X8}:{X16}": 3 parts, or 4 with a leading "key"; hex without prefix
/// </summary>
public class ResourceKeyLegacyParseTests
{
    private static readonly ResourceKey Expected = new(0x220557DA, 0x80000000, 0x0123456789ABCDEF);

    [Theory]
    [InlineData("T:0x220557DA G:0x80000000 I:0x0123456789ABCDEF")]
    [InlineData("0x220557DA-0x80000000-0x0123456789ABCDEF")]
    [InlineData("0x220557da-0x80000000-0x0123456789abcdef")]
    [InlineData("  0X220557DA-0X80000000-0X0123456789ABCDEF  ")]
    [InlineData("220557DA:80000000:0123456789ABCDEF")]
    [InlineData("key:220557DA:80000000:0123456789ABCDEF")]
    [InlineData("KEY:220557da:80000000:0123456789abcdef")]
    public void TryParse_AcceptsAllFormats(string text)
    {
        ResourceKey.TryParse(text, out var key).Should().BeTrue();
        key.Should().Be(Expected);
        ResourceKey.Parse(text).Should().Be(Expected);
    }

    [Theory]
    [InlineData("220557DA-0x80000000-0x0123456789ABCDEF")] // missing 0x
    [InlineData("0x220557DA-0x80000000")]
    [InlineData("0x220557DA:0x80000000:0x0123456789ABCDEF")] // 0x not allowed in ':' form
    [InlineData("foo:220557DA:80000000:0123456789ABCDEF")]
    [InlineData("220557DA:80000000")]
    [InlineData("0x1220557DA-0x80000000-0x0")] // type overflows uint
    [InlineData("")]
    public void TryParse_RejectsInvalid(string text)
    {
        ResourceKey.TryParse(text, out _).Should().BeFalse();
        var act = () => ResourceKey.Parse(text);
        act.Should().Throw<FormatException>();
    }
}
