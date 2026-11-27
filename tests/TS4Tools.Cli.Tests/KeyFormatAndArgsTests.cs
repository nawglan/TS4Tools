using FluentAssertions;
using Xunit;

namespace TS4Tools.Cli.Tests;

/// <summary>
/// Key text formats, unpack file names and command-line errors.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 182-217 (0xT-0xG-0xI and (key:)T:G:I formats)
/// - Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/TGIN.cs lines 105-186 (S4_T_G_I export names)
/// - Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs lines 32-82 (extension per type, "*" fallback)
/// </summary>
public class KeyFormatAndArgsTests
{
    private static readonly ResourceKey Key = new(0x220557DA, 0x80000000, 0x0123456789ABCDEF);

    [Theory]
    [InlineData("220557DA:80000000:0123456789ABCDEF")]
    [InlineData("key:220557da:80000000:0123456789abcdef")]
    [InlineData("0x220557DA-0x80000000-0x0123456789ABCDEF")]
    [InlineData("T:0x220557DA G:0x80000000 I:0x0123456789ABCDEF")]
    [InlineData("220557DA_80000000_0123456789ABCDEF")]
    [InlineData("220557DA_80000000_0123456789ABCDEF.stbl")]
    public void ParseKey_AcceptsAllFormats(string text) => KeyFormat.ParseKey(text).Should().Be(Key);

    [Theory]
    [InlineData("")]
    [InlineData("220557DA:80000000")]
    [InlineData("xyz")]
    [InlineData("220557DA:80000000:0123456789ABCDEFAB")]
    public void ParseKey_Invalid_ThrowsUsage(string text)
    {
        var act = () => KeyFormat.ParseKey(text);
        act.Should().Throw<UsageException>();
    }

    [Fact]
    public void Compact_IsParseable() =>
        KeyFormat.ParseKey(KeyFormat.Compact(Key)).Should().Be(Key);

    [Fact]
    public void FileName_UsesExtListExtensionAndDuplicateSuffix()
    {
        KeyFormat.FileName(Key, 0).Should().Be("220557DA_80000000_0123456789ABCDEF.stbl");
        KeyFormat.FileName(Key, 2).Should().Be("220557DA_80000000_0123456789ABCDEF.2.stbl");
        KeyFormat.FileName(new ResourceKey(0xDEADBEEF, 1, 2), 0).Should().Be("DEADBEEF_00000001_0000000000000002.bnry");
    }

    [Theory]
    [InlineData("220557DA_80000000_0123456789ABCDEF.stbl", 0)]
    [InlineData("220557DA_80000000_0123456789ABCDEF.12.stbl", 12)]
    [InlineData("220557DA_80000000_0123456789ABCDEF", 0)]
    [InlineData("220557da_80000000_0123456789abcdef.anything.json", 0)]
    [InlineData("S4_220557DA_80000000_0123456789ABCDEF.stbl", 0)]
    public void TryParseFileName_Valid(string name, int dup)
    {
        KeyFormat.TryParseFileName(name, out var key, out int d).Should().BeTrue();
        key.Should().Be(Key);
        d.Should().Be(dup);
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("220557DA_80000000_0123456789ABCDE.stbl")]
    [InlineData("220557DA-80000000-0123456789ABCDEF.stbl")]
    [InlineData("220557DA_80000000_0123456789ABCDEFX")]
    public void TryParseFileName_Invalid(string name) =>
        KeyFormat.TryParseFileName(name, out _, out _).Should().BeFalse();

    [Fact]
    public void FileName_RoundTripsThroughParser()
    {
        for (int dup = 0; dup < 3; dup++)
        {
            KeyFormat.TryParseFileName(KeyFormat.FileName(Key, dup), out var key, out int d).Should().BeTrue();
            key.Should().Be(Key);
            d.Should().Be(dup);
        }
    }

    [Theory]
    [InlineData("0x220557DA,034AEECB", new uint[] { 0x220557DA, 0x034AEECB })]
    [InlineData(" 0x1 ", new uint[] { 1 })]
    public void ParseTypeList_Valid(string text, uint[] expected) =>
        KeyFormat.ParseTypeList(text).Should().BeEquivalentTo(expected);

    [Theory]
    [InlineData("zz")]
    [InlineData(",")]
    [InlineData("0x123456789")]
    public void ParseTypeList_Invalid(string text)
    {
        var act = () => KeyFormat.ParseTypeList(text);
        act.Should().Throw<UsageException>();
    }

    [Fact]
    public async Task NoArguments_IsUsageError()
    {
        var run = await CliRunner.RunAsync();
        run.ExitCode.Should().Be(2);
        run.Err.Should().Contain("Usage:");
    }

    [Fact]
    public async Task Help_ListsEveryCommand()
    {
        var run = await CliRunner.RunAsync("--help");
        run.ExitCode.Should().Be(0);
        foreach (var name in new[] { "info", "list", "unpack", "pack", "scan", "extract", "replace", "compare" })
            run.Out.Should().Contain("  " + name);
    }

    [Theory]
    [InlineData("info")]
    [InlineData("list")]
    [InlineData("unpack")]
    [InlineData("pack")]
    [InlineData("scan")]
    [InlineData("extract")]
    [InlineData("replace")]
    [InlineData("compare")]
    public async Task CommandHelp_ExitsZero(string command)
    {
        var run = await CliRunner.RunAsync(command, "--help");
        run.ExitCode.Should().Be(0);
        run.Out.Should().StartWith("Usage: ts4tools " + command);
        (await CliRunner.RunAsync("help", command)).Out.Should().Be(run.Out);
    }

    [Theory]
    [InlineData("frobnicate")]
    [InlineData("info")]
    [InlineData("info", "a", "b")]
    [InlineData("list", "a.package", "--bogus")]
    [InlineData("list", "a.package", "--type")]
    [InlineData("list", "a.package", "--type", "nothex")]
    [InlineData("unpack", "a.package")]
    [InlineData("pack", "dir")]
    [InlineData("scan")]
    [InlineData("scan", "x", "--parallel", "0")]
    [InlineData("scan", "x", "--types", "q")]
    [InlineData("extract", "a.package", "badkey", "out")]
    [InlineData("info", "a.package", "--json=yes")]
    [InlineData("replace", "a.package", "k")]
    public async Task BadArguments_ExitTwo(params string[] args)
    {
        var run = await CliRunner.RunAsync(args);
        run.ExitCode.Should().Be(2, run.Err);
        run.Err.Should().StartWith("error: ");
    }

    [Fact]
    public async Task MissingPackage_ExitsOne()
    {
        var run = await CliRunner.RunAsync("info", Path.Combine(AppContext.BaseDirectory, "does-not-exist.package"));
        run.ExitCode.Should().Be(1);
        run.Err.Should().Contain("not found");
    }

    [Fact]
    public async Task NotAPackage_ExitsOne()
    {
        using var scratch = new Scratch();
        string path = scratch.PathOf("junk.package");
        await File.WriteAllTextAsync(path, "this is not a DBPF file at all, but it is long enough to have a header....................................");
        var run = await CliRunner.RunAsync("info", path);
        run.ExitCode.Should().Be(1);
        run.Err.Should().StartWith("error: ");
    }

    [Fact]
    public void ParsedArgs_InlineValueAndDoubleDash()
    {
        var spec = new CommandSpec("t", "t", "t", "", 0, int.MaxValue, ["--flag"], ["--opt"]);
        var parsed = ParsedArgs.Parse(spec, ["a", "--opt=v", "--flag", "--", "--not-an-option"]);
        parsed.Get("--opt").Should().Be("v");
        parsed.Has("--flag").Should().BeTrue();
        parsed.Positionals.Should().Equal("a", "--not-an-option");
    }

    [Fact]
    public void ParsedArgs_RepeatedValueOption_Throws()
    {
        var spec = new CommandSpec("t", "t", "t", "", 0, 1, [], ["--opt"]);
        var act = () => ParsedArgs.Parse(spec, ["--opt", "1", "--opt", "2"]);
        act.Should().Throw<UsageException>();
    }
}
