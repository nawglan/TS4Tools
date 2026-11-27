using FluentAssertions;
using TS4Tools.Cli.Scan;
using TS4Tools.Resources;
using Xunit;

namespace TS4Tools.Cli.Tests;

/// <summary>
/// Scan classification on synthetic packages.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/WrapperDealer/WrapperDealer.cs lines 53-67 (wrapper by type, default otherwise)
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPartResource.cs lines 134-186 (newest CASP layout is
///   version 37; TS4Tools refuses newer versions with UnsupportedResourceVersionException)
/// </summary>
public class ScanTests
{
    private const uint CaspType = 0x034AEECB;
    private const uint StblType = 0x220557DA;
    private const uint UnwrappedType = 0x7E57AB1E;

    private static byte[] CaspWithVersion(uint version)
    {
        var data = new byte[64];
        BinaryPrimitives.WriteUInt32LittleEndian(data, version);
        return data;
    }

    private static byte[] ValidStbl() => ScanEngine.CreateRegistry().CreateNewResource(new ResourceKey(StblType, 0, 1)).Data.ToArray();

    private static async Task<string> CorpusAsync(Scratch scratch)
    {
        string dir = scratch.PathOf("corpus", "sub");
        Directory.CreateDirectory(dir);
        await TestPackages.CreateAsync(Path.Combine(dir, "a.package"),
        [
            new(new ResourceKey(UnwrappedType, 0, 1), TestPackages.Compressible(100)),
            new(new ResourceKey(UnwrappedType, 0, 2), TestPackages.Compressible(100)),
            new(new ResourceKey(CaspType, 0, 1), CaspWithVersion(0x33)),
            new(new ResourceKey(CaspType, 0, 2), CaspWithVersion(0x33)),
            new(new ResourceKey(CaspType, 0, 3), CaspWithVersion(0x2A)),
            new(new ResourceKey(StblType, 0, 1), ValidStbl()),
            new(new ResourceKey(StblType, 0, 2), Encoding.ASCII.GetBytes("XXXXnot a string table at all")),
            new(new ResourceKey(StblType, 0, 3), [], Compression: 0xFFE0),
        ]);
        await File.WriteAllTextAsync(Path.Combine(dir, "broken.package"), "not a package");
        return scratch.PathOf("corpus");
    }

    [Fact]
    public async Task Scan_ClassifiesEachResource()
    {
        using var scratch = new Scratch();
        string corpus = await CorpusAsync(scratch);

        var report = await ScanEngine.ScanAsync([corpus], new ScanOptions(Roundtrip: true, Parallelism: 2, Types: null),
            ScanEngine.CreateRegistry(), null, CancellationToken.None);

        report.PackagesScanned.Should().Be(2);
        report.FailedPackages.Should().ContainSingle().Which.Package.Should().Be("sub/broken.package");

        var unwrapped = report.Types[UnwrappedType];
        unwrapped.Wrapper.Should().Be("(default)");
        unwrapped.Counts[(int)ScanClass.Unwrapped].Should().Be(2);
        unwrapped.Samples[(int)ScanClass.Unwrapped].Select(s => s.Key.Instance).Should().Equal(1UL, 2UL);
        unwrapped.Samples[(int)ScanClass.Unwrapped][0].Package.Should().Be("sub/a.package");

        var casp = report.Types[CaspType];
        casp.Counts[(int)ScanClass.UnsupportedVersion].Should().Be(3);
        casp.Versions.Should().BeEquivalentTo(new Dictionary<(string, uint, uint), long>
        {
            [("CASP", 0x33u, 37u)] = 2,
            [("CASP", 0x2Au, 37u)] = 1,
        });

        var stbl = report.Types[StblType];
        stbl.Wrapper.Should().NotBe("(default)");
        stbl.Counts[(int)ScanClass.Ok].Should().Be(1);
        stbl.Counts[(int)ScanClass.ParseError].Should().Be(1);
        stbl.Counts[(int)ScanClass.Empty].Should().Be(1);
        stbl.Samples[(int)ScanClass.ParseError].Single().Detail.Should().Contain("magic");
        stbl.Messages.Should().ContainSingle();

        report.HasProblems.Should().BeTrue();
        report.Resources.Should().Be(8);
    }

    [Fact]
    public async Task Scan_TypeFilter_OnlyCountsThoseTypes()
    {
        using var scratch = new Scratch();
        string corpus = await CorpusAsync(scratch);

        var report = await ScanEngine.ScanAsync([corpus], new ScanOptions(false, 1, new HashSet<uint> { CaspType }),
            ScanEngine.CreateRegistry(), null, CancellationToken.None);

        report.Types.Keys.Should().Equal(CaspType);
    }

    [Fact]
    public async Task Scan_Roundtrip_DetectsMismatch()
    {
        using var scratch = new Scratch();
        string path = await TestPackages.CreateAsync(scratch.PathOf("r.package"),
            [new(new ResourceKey(0x11112222, 0, 1), [1, 2, 3, 4])]);
        var registry = new ResourceHandlerRegistry();
        registry.Register(0x11112222, new AppendingFactory());

        var withRoundtrip = await ScanEngine.ScanAsync([path], new ScanOptions(true, 1, null), registry, null, CancellationToken.None);
        var stats = withRoundtrip.Types[0x11112222];
        stats.Counts[(int)ScanClass.RoundtripMismatch].Should().Be(1);
        stats.Samples[(int)ScanClass.RoundtripMismatch].Single().Detail.Should().Contain("first difference at 0x4");
        stats.Wrapper.Should().Be("Appending");

        var without = await ScanEngine.ScanAsync([path], new ScanOptions(false, 1, null), registry, null, CancellationToken.None);
        without.Types[0x11112222].Counts[(int)ScanClass.Ok].Should().Be(1);
        without.HasProblems.Should().BeFalse();
    }

    [Fact]
    public async Task ScanCommand_WritesReportsAndExitsOneOnProblems()
    {
        using var scratch = new Scratch();
        string corpus = await CorpusAsync(scratch);
        string json = scratch.PathOf("out", "report.json");
        string md = scratch.PathOf("out", "report.md");

        var run = await CliRunner.RunAsync("scan", corpus, "--json", json, "--markdown", md, "--quiet", "--roundtrip");
        run.ExitCode.Should().Be(1);
        run.Out.Should().Contain("0x034AEECB").And.Contain("CASP version 0x33 (max 0x25): 2");
        run.Out.Should().Contain("sub/broken.package");

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(json));
        var root = doc.RootElement;
        root.GetProperty("packagesScanned").GetInt32().Should().Be(2);
        root.GetProperty("failedPackages").GetArrayLength().Should().Be(1);
        root.GetProperty("totals").GetProperty("unsupported-version").GetInt64().Should().Be(3);
        var casp = root.GetProperty("types").EnumerateArray().Single(t => t.GetProperty("type").GetString() == "0x034AEECB");
        casp.GetProperty("tag").GetString().Should().Be("CASP");
        casp.GetProperty("counts").GetProperty("unsupported-version").GetInt64().Should().Be(3);
        casp.GetProperty("unsupportedVersions").GetArrayLength().Should().Be(2);
        casp.GetProperty("samples").GetProperty("unsupported-version").GetArrayLength().Should().Be(3);
        casp.GetProperty("samples").GetProperty("unsupported-version")[0].GetProperty("key").GetString()
            .Should().Be("034AEECB:00000000:0000000000000001");

        string markdown = await File.ReadAllTextAsync(md);
        markdown.Should().Contain("## Unsupported versions").And.Contain("| `0x034AEECB` | CASP | CASP | 0x33 (51) | 0x25 (37) | 2 |");
        markdown.Should().NotContain(scratch.Root, "paths are shown relative to the scanned folder");
    }

    [Fact]
    public async Task ScanCommand_AllOk_ExitsZero()
    {
        using var scratch = new Scratch();
        string path = await TestPackages.CreateAsync(scratch.PathOf("ok.package"), [new(new ResourceKey(StblType, 0, 1), ValidStbl())]);
        var run = await CliRunner.RunAsync("scan", path, "--quiet", "--roundtrip", "--all");
        run.ExitCode.Should().Be(0, run.Out);
        run.Out.Should().Contain("0x220557DA");
    }

    [Fact]
    public async Task ScanCommand_MissingPath_ExitsOne()
    {
        var run = await CliRunner.RunAsync("scan", Path.Combine(AppContext.BaseDirectory, "no-such-dir"), "--quiet");
        run.ExitCode.Should().Be(1);
        run.Err.Should().Contain("path not found");
    }

    [Fact]
    public void Normalise_GroupsNumbers() =>
        ScanEngine.Normalise("Unexpected end of data at entry 12 (offset 0x1F)").Should().Be("Unexpected end of data at entry # (offset #)");

    private sealed class AppendingFactory : IResourceFactory
    {
        public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data) => new DefaultResource(key, (byte[])[.. data.ToArray(), 0xFF]);

        public IResource CreateEmpty(ResourceKey key) => new DefaultResource(key, ReadOnlyMemory<byte>.Empty);
    }
}
