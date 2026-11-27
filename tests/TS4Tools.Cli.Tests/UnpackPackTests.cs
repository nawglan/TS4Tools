using FluentAssertions;
using TS4Tools.Package;
using Xunit;

namespace TS4Tools.Cli.Tests;

/// <summary>
/// unpack -> pack round trips on synthetic packages.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 96-147 (SaveAs: header written verbatim, index
///   rebuilt; dirty resources compressed only when Compressed != 0 and labelled 0x5A42 only if smaller)
/// - Source: legacy_references/Sims4Tools/s4pi/Package/Package.cs lines 649-673 (GetResource: decompressed bytes exported)
/// - Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1866-1880 (ExportFile writes the decompressed stream)
/// </summary>
public class UnpackPackTests
{
    private static readonly ResourceKey StblKey = new(0x220557DA, 0x80000000, 0x0123456789ABCDEF);
    private static readonly ResourceKey DupKey = new(0x12345678, 0, 0xAA);
    private static readonly ResourceKey PlainKey = new(0x0333406C, 0x00000001, 0xFEDCBA9876543210);
    private static readonly ResourceKey DeletedKey = new(0x81CA1A10, 0, 0x99);

    private static List<TestResource> SampleResources() =>
    [
        new(StblKey, TestPackages.Compressible(4000)),                                // compressed (0x5A42)
        new(DupKey, TestPackages.Compressible(3000, 5)),                              // duplicate #1
        new(PlainKey, TestPackages.Random(500, 1), Compression: 0x0000),              // stored
        new(DupKey, TestPackages.Random(200, 2), Compression: 0x0000, Unknown2: 3),   // duplicate #2
        new(DeletedKey, [], Compression: 0xFFE0),                                     // deletion record
        new(DupKey, TestPackages.Compressible(100, 3)),                               // duplicate #3
    ];

    private static void SampleHeader(DbpfPackage p)
    {
        p.UserVersionMinor = 12000;
        p.CreationTime = 42;
        p.UpdatedTime = 43;
        var unused5 = new byte[DbpfPackage.Unused5Length];
        unused5[12] = 0xFF;
        unused5[13] = 0xFF;
        p.Unused5 = unused5;
    }

    private static async Task<(string Original, string Dir)> UnpackSampleAsync(Scratch scratch)
    {
        string original = await TestPackages.CreateAsync(scratch.PathOf("original.package"), SampleResources(), SampleHeader);
        string dir = scratch.PathOf("unpacked");
        var run = await CliRunner.RunAsync("unpack", original, dir);
        run.ExitCode.Should().Be(0, run.Err);
        return (original, dir);
    }

    [Fact]
    public async Task Unpack_WritesManifestAndFileNames()
    {
        using var scratch = new Scratch();
        var (_, dir) = await UnpackSampleAsync(scratch);

        var files = Directory.GetFiles(Path.Combine(dir, "resources")).Select(Path.GetFileName).Order(StringComparer.Ordinal);
        files.Should().Equal(
            "0333406C_00000001_FEDCBA9876543210.bnry".Replace(".bnry", Extensions.ExtList.GetExtension(0x0333406C), StringComparison.Ordinal),
            "12345678_00000000_00000000000000AA.1.bnry",
            "12345678_00000000_00000000000000AA.2.bnry",
            "12345678_00000000_00000000000000AA.bnry",
            "220557DA_80000000_0123456789ABCDEF.stbl",
            "81CA1A10_00000000_0000000000000099.mtbl");

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dir, "manifest.json")));
        var root = doc.RootElement;
        root.GetProperty("formatVersion").GetInt32().Should().Be(1);
        root.GetProperty("source").GetString().Should().Be("original.package");
        var header = root.GetProperty("header");
        header.GetProperty("major").GetInt32().Should().Be(2);
        header.GetProperty("minor").GetInt32().Should().Be(1);
        header.GetProperty("userVersionMinor").GetInt32().Should().Be(12000);
        header.GetProperty("creationTime").GetInt32().Should().Be(42);
        header.GetProperty("unused5").GetString().Should().Be("000000000000000000000000FFFF0000000000000000000000000000");

        var resources = root.GetProperty("resources").EnumerateArray().ToList();
        resources.Should().HaveCount(6);
        resources[0].GetProperty("type").GetString().Should().Be("0x220557DA");
        resources[0].GetProperty("group").GetString().Should().Be("0x80000000");
        resources[0].GetProperty("instance").GetString().Should().Be("0x0123456789ABCDEF");
        resources[0].GetProperty("file").GetString().Should().Be("resources/220557DA_80000000_0123456789ABCDEF.stbl");
        resources[0].GetProperty("compressionType").GetString().Should().Be("0x5A42");
        resources[0].GetProperty("sha256").GetString().Should()
            .Be(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(TestPackages.Compressible(4000))));
        resources[2].GetProperty("compressionType").GetString().Should().Be("0x0000");
        resources[3].GetProperty("unknown2").GetInt32().Should().Be(3);
        resources[3].GetProperty("file").GetString().Should().Be("resources/12345678_00000000_00000000000000AA.1.bnry");
        resources[4].GetProperty("compressionType").GetString().Should().Be("0xFFE0");

        // The written file holds the decompressed bytes.
        (await File.ReadAllBytesAsync(Path.Combine(dir, "resources", "220557DA_80000000_0123456789ABCDEF.stbl")))
            .Should().Equal(TestPackages.Compressible(4000));
    }

    [Fact]
    public async Task UnpackThenPack_Unedited_IsResourceForResourceIdentical()
    {
        using var scratch = new Scratch();
        var (original, dir) = await UnpackSampleAsync(scratch);
        string rebuilt = scratch.PathOf("rebuilt.package");

        var run = await CliRunner.RunAsync("pack", dir, rebuilt);
        run.ExitCode.Should().Be(0, run.Err);
        run.Out.Should().Contain("0 edited, 0 added, 0 dropped");

        await using var a = await DbpfPackage.OpenAsync(original);
        await using var b = await DbpfPackage.OpenAsync(rebuilt);
        TestPackages.HeaderFields(b).Should().Be(TestPackages.HeaderFields(a));
        b.ResourceCount.Should().Be(a.ResourceCount);
        for (int i = 0; i < a.ResourceCount; i++)
        {
            var x = a.Resources[i];
            var y = b.Resources[i];
            y.Key.Should().Be(x.Key);
            y.CompressionType.Should().Be(x.CompressionType);
            DbpfPackage.GetUnknown2(y).Should().Be(DbpfPackage.GetUnknown2(x));
            (await b.GetResourceDataAsync(y)).ToArray().Should().Equal((await a.GetResourceDataAsync(x)).ToArray());
        }

        var compare = await CliRunner.RunAsync("compare", original, rebuilt);
        compare.ExitCode.Should().Be(0, compare.Out);
        compare.Out.Should().Contain("IDENTICAL");
    }

    [Fact]
    public async Task Pack_PicksUpEditedAddedAndDeletedFiles()
    {
        using var scratch = new Scratch();
        var (_, dir) = await UnpackSampleAsync(scratch);
        string res = Path.Combine(dir, "resources");

        byte[] edited = Encoding.UTF8.GetBytes("edited content, edited content, edited content");
        await File.WriteAllBytesAsync(Path.Combine(res, "0333406C_00000001_FEDCBA9876543210" + Extensions.ExtList.GetExtension(0x0333406C)), edited);
        File.Delete(Path.Combine(res, "12345678_00000000_00000000000000AA.1.bnry"));
        var addedKey = new ResourceKey(0x545AC67A, 0x00ABCDEF, 0x1122334455667788);
        byte[] added = TestPackages.Compressible(2048);
        await File.WriteAllBytesAsync(Path.Combine(res, "545AC67A_00ABCDEF_1122334455667788.data"), added);
        await File.WriteAllTextAsync(Path.Combine(res, "notes.txt"), "not a resource");

        string rebuilt = scratch.PathOf("rebuilt.package");
        var run = await CliRunner.RunAsync("pack", dir, rebuilt);
        run.ExitCode.Should().Be(0, run.Err);
        run.Out.Should().Contain("1 edited, 1 added, 1 dropped");
        run.Err.Should().Contain("dropped").And.Contain("notes.txt");

        await using var p = await DbpfPackage.OpenAsync(rebuilt);
        p.Resources.Select(e => e.Key).Should().Equal(StblKey, DupKey, PlainKey, DeletedKey, DupKey, addedKey);
        (await p.GetResourceDataAsync(p.Resources[2])).ToArray().Should().Equal(edited);
        p.Resources[2].CompressionType.Should().Be(0x0000, "the manifest label is kept for edited files");
        (await p.GetResourceDataAsync(p.Resources[4])).ToArray().Should().Equal(TestPackages.Compressible(100, 3));
        (await p.GetResourceDataAsync(p.Resources[5])).ToArray().Should().Equal(added);
        p.Resources[5].CompressionType.Should().Be(0x5A42, "new files are compressed by default");
    }

    [Fact]
    public async Task Pack_NoCompressNew_StoresAddedFilesUncompressed()
    {
        using var scratch = new Scratch();
        var (_, dir) = await UnpackSampleAsync(scratch);
        await File.WriteAllBytesAsync(Path.Combine(dir, "resources", "545AC67A_00000000_0000000000000001.data"), TestPackages.Compressible(2048));

        string rebuilt = scratch.PathOf("rebuilt.package");
        (await CliRunner.RunAsync("pack", dir, rebuilt, "--no-compress-new")).ExitCode.Should().Be(0);

        await using var p = await DbpfPackage.OpenAsync(rebuilt);
        p.Resources[^1].CompressionType.Should().Be(0x0000);
        p.Resources[^1].FileSize.Should().Be(2048);
    }

    [Fact]
    public async Task Pack_WithoutManifest_PacksFilesByName()
    {
        using var scratch = new Scratch();
        string dir = scratch.PathOf("loose");
        Directory.CreateDirectory(Path.Combine(dir, "resources"));
        await File.WriteAllBytesAsync(Path.Combine(dir, "resources", "220557DA_00000000_0000000000000002.stbl"), [1, 2, 3]);
        await File.WriteAllBytesAsync(Path.Combine(dir, "resources", "220557DA_00000000_0000000000000001.stbl"), [4, 5, 6]);
        await File.WriteAllBytesAsync(Path.Combine(dir, "resources", "220557DA_00000000_0000000000000001.1.stbl"), [7]);

        string output = scratch.PathOf("out.package");
        var run = await CliRunner.RunAsync("pack", dir, output);
        run.ExitCode.Should().Be(0, run.Err);

        await using var p = await DbpfPackage.OpenAsync(output);
        p.Resources.Select(e => e.Key.Instance).Should().Equal(1UL, 1UL, 2UL);
        (await p.GetResourceDataAsync(p.Resources[1])).ToArray().Should().Equal(7);
        p.UserVersionMinor.Should().Be(0);
    }

    [Fact]
    public async Task Unpack_NonEmptyOutDir_RefusedUnlessForce()
    {
        using var scratch = new Scratch();
        var (original, dir) = await UnpackSampleAsync(scratch);
        string keep = Path.Combine(dir, "keep.txt");
        await File.WriteAllTextAsync(keep, "user file");
        await File.WriteAllTextAsync(Path.Combine(dir, "resources", "stale.bnry"), "stale");

        var refused = await CliRunner.RunAsync("unpack", original, dir);
        refused.ExitCode.Should().Be(1);
        refused.Err.Should().Contain("--force");

        var forced = await CliRunner.RunAsync("unpack", original, dir, "--force");
        forced.ExitCode.Should().Be(0, forced.Err);
        File.Exists(keep).Should().BeTrue("only manifest.json and resources/ are replaced");
        File.Exists(Path.Combine(dir, "resources", "stale.bnry")).Should().BeFalse();
    }

    [Fact]
    public async Task Pack_ExistingOutput_RefusedUnlessForce()
    {
        using var scratch = new Scratch();
        var (_, dir) = await UnpackSampleAsync(scratch);
        string output = scratch.PathOf("out.package");
        await File.WriteAllTextAsync(output, "existing");

        (await CliRunner.RunAsync("pack", dir, output)).ExitCode.Should().Be(1);
        (await File.ReadAllTextAsync(output)).Should().Be("existing");
        (await CliRunner.RunAsync("pack", dir, output, "--force")).ExitCode.Should().Be(0);
        await using var p = await DbpfPackage.OpenAsync(output);
        p.ResourceCount.Should().Be(6);
    }

    [Fact]
    public async Task Pack_ManifestPathOutsideDirectory_Fails()
    {
        using var scratch = new Scratch();
        var (_, dir) = await UnpackSampleAsync(scratch);
        string manifestPath = Path.Combine(dir, "manifest.json");
        string json = await File.ReadAllTextAsync(manifestPath);
        await File.WriteAllTextAsync(manifestPath,
            json.Replace("resources/220557DA_80000000_0123456789ABCDEF.stbl", "../original.package", StringComparison.Ordinal));

        var run = await CliRunner.RunAsync("pack", dir, scratch.PathOf("out.package"));
        run.ExitCode.Should().Be(1);
        run.Err.Should().Contain("outside");
    }

    [Fact]
    public async Task Pack_UnsupportedManifestVersion_Fails()
    {
        using var scratch = new Scratch();
        var (_, dir) = await UnpackSampleAsync(scratch);
        string manifestPath = Path.Combine(dir, "manifest.json");
        string json = await File.ReadAllTextAsync(manifestPath);
        await File.WriteAllTextAsync(manifestPath, json.Replace("\"formatVersion\": 1", "\"formatVersion\": 9", StringComparison.Ordinal));

        var run = await CliRunner.RunAsync("pack", dir, scratch.PathOf("out.package"));
        run.ExitCode.Should().Be(1);
        run.Err.Should().Contain("formatVersion");
    }

    [Fact]
    public async Task ExtractAndReplace_RoundTrip()
    {
        using var scratch = new Scratch();
        string original = await TestPackages.CreateAsync(scratch.PathOf("p.package"), SampleResources(), SampleHeader);
        string extracted = scratch.PathOf("stbl.bin");

        var ex = await CliRunner.RunAsync("extract", original, "220557DA:80000000:0123456789ABCDEF", extracted);
        ex.ExitCode.Should().Be(0, ex.Err);
        (await File.ReadAllBytesAsync(extracted)).Should().Equal(TestPackages.Compressible(4000));

        byte[] replacement = TestPackages.Compressible(999, 11);
        await File.WriteAllBytesAsync(extracted, replacement);
        string copy = scratch.PathOf("copy.package");
        var rep = await CliRunner.RunAsync("replace", original, "0x220557DA-0x80000000-0x0123456789ABCDEF", extracted, "--output", copy);
        rep.ExitCode.Should().Be(0, rep.Err);

        await using (var p = await DbpfPackage.OpenAsync(copy))
        {
            (await p.GetResourceDataAsync(p.Find(StblKey)!)).ToArray().Should().Equal(replacement);
            p.Find(StblKey)!.CompressionType.Should().Be(0x5A42);
            p.UserVersionMinor.Should().Be(12000);
        }
        await using (var untouched = await DbpfPackage.OpenAsync(original))
            (await untouched.GetResourceDataAsync(untouched.Find(StblKey)!)).ToArray().Should().Equal(TestPackages.Compressible(4000));

        // In place.
        var inPlace = await CliRunner.RunAsync("replace", original, "T:0x220557DA G:0x80000000 I:0x0123456789ABCDEF", extracted);
        inPlace.ExitCode.Should().Be(0, inPlace.Err);
        await using (var p = await DbpfPackage.OpenAsync(original))
            (await p.GetResourceDataAsync(p.Find(StblKey)!)).ToArray().Should().Equal(replacement);
    }

    [Fact]
    public async Task Extract_MissingKey_Fails()
    {
        using var scratch = new Scratch();
        string original = await TestPackages.CreateAsync(scratch.PathOf("p.package"), SampleResources());
        var run = await CliRunner.RunAsync("extract", original, "00000001:00000002:0000000000000003", scratch.PathOf("x"));
        run.ExitCode.Should().Be(1);
        run.Err.Should().Contain("not found");
    }

    [Fact]
    public async Task InfoAndList_Json()
    {
        using var scratch = new Scratch();
        string original = await TestPackages.CreateAsync(scratch.PathOf("p.package"), SampleResources(), SampleHeader);

        var info = await CliRunner.RunAsync("info", original, "--json");
        info.ExitCode.Should().Be(0);
        using (var doc = JsonDocument.Parse(info.Out))
        {
            doc.RootElement.GetProperty("resourceCount").GetInt32().Should().Be(6);
            doc.RootElement.GetProperty("userVersionMinor").GetInt32().Should().Be(12000);
        }

        var list = await CliRunner.RunAsync("list", original, "--type", "0x12345678", "--json");
        list.ExitCode.Should().Be(0);
        using (var doc = JsonDocument.Parse(list.Out))
        {
            var items = doc.RootElement.EnumerateArray().ToList();
            items.Should().HaveCount(3);
            items[0].GetProperty("instance").GetString().Should().Be("0x00000000000000AA");
            items[0].GetProperty("memSize").GetInt64().Should().Be(3000);
            items[0].GetProperty("compressionType").GetString().Should().Be("0x5A42");
            items[0].GetProperty("tag").GetString().Should().NotBeNullOrEmpty();
            items[0].TryGetProperty("fileSize", out _).Should().BeTrue();
            items[0].TryGetProperty("group", out _).Should().BeTrue();
        }

        var text = await CliRunner.RunAsync("list", original);
        var lines = text.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(6);
        lines[0].Should().StartWith("220557DA 80000000 0123456789ABCDEF STBL 4000 0x5A42");
    }
}
