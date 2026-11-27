using FluentAssertions;
using TS4Tools.FileTable;
using Xunit;
using static TS4Tools.Core.Tests.FileTable.FileTableTestSupport;

namespace TS4Tools.Core.Tests.FileTable;

/// <summary>
/// LEGACY ANALYSIS: legacy_references/Sims4Tools/s4pi Extras/Filetable/ (FileTable.cs, GameFolders.cs,
/// RK.cs, SpecificResource.cs, PathPackageTuple.cs).
/// </summary>
public class FileTableTests
{
    private static ResourceKey K(uint t, uint g, ulong i) => new(t, g, i);

    // ---- RK (RK.cs lines 60-107) ----

    [Fact]
    public void RK_IgnoresResourceGroupVersionBits()
    {
        RK.AreEquivalent(K(1, 0x08000005, 9), K(1, 0x00000005, 9)).Should().BeTrue();
        RK.AreEquivalent(K(1, 5, 9), K(1, 6, 9)).Should().BeFalse();
    }

    [Fact]
    public void RK_MasksInstanceForType736884F1Only()
    {
        RK.AreEquivalent(K(0x736884F1, 0, 0x08000007), K(0x736884F1, 0, 7)).Should().BeTrue();
        RK.AreEquivalent(K(0x736884F1, 0, 0x1_0800_0007), K(0x736884F1, 0, 7)).Should().BeFalse();
        RK.AreEquivalent(K(2, 0, 0x08000007), K(2, 0, 7)).Should().BeFalse();
    }

    [Theory]
    [InlineData("0x00000001-0x00000002-0x0000000000000003")]
    [InlineData("00000001:00000002:0000000000000003")]
    [InlineData("key:00000001:00000002:3")]
    public void RK_TryParse_AcceptsLegacyFormats(string text)
    {
        RK.TryParse(text, out var k).Should().BeTrue();
        k.Should().Be(K(1, 2, 3));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0x1-2-0x3")]
    [InlineData("1:2")]
    [InlineData("zz:1:2")]
    public void RK_TryParse_RejectsInvalid(string? text)
    {
        RK.TryParse(text, out _).Should().BeFalse();
        if (text is not null) FluentActions.Invoking(() => RK.Parse(text)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RK_Format_MatchesLegacyString() =>
        RK.Format(K(1, 2, 3)).Should().Be("0x00000001-0x00000002-0x0000000000000003");

    // ---- GameFolders (GameFolders.cs) ----

    [Fact]
    public void GameFolders_EmbeddedXml_LoadsAllGames()
    {
        var gf = new GameFolders();
        gf.Games.Should().HaveCount(22);
        var b = gf.ByName("base")!;
        b.Priority.Should().Be(99);
        b.RGVersion.Should().Be(0);
        b.Suppressed.Should().BeNull();
        gf.ByRGVersion(1)!.Name.Should().Be("WA");
        gf.ByRGVersion(1)!.Priority.Should().Be(1);
        gf.ByName("nope").Should().BeNull();
        gf.ByRGVersion(999).Should().BeNull();
    }

    [Fact]
    public void GameFolders_EPsDisabled_NeverDisablesBase()
    {
        var gf = new GameFolders();
        gf.IsEnabled(gf.ByName("WA")!).Should().BeTrue(); // not suppressed by default
        gf.EPsDisabled = "base;WA";
        gf.IsEnabled(gf.ByName("base")!).Should().BeTrue();
        gf.IsEnabled(gf.ByName("WA")!).Should().BeFalse();
        gf.EPsDisabled.Should().Be("WA");
    }

    [Fact]
    public void GameFolders_InstallDirs_DropsInvalidEntries()
    {
        using var tmp = new TempDir();
        var gf = new GameFolders();
        gf.InstallDirs = $"WA={tmp.Path};Bogus={tmp.Path};HEL={tmp.Combine("missing")};malformed";
        gf.InstallDirs.Should().Be($"WA={tmp.Path}");
        gf.ByName("WA")!.UserInstallDir.Should().Be(tmp.Path);
        gf.ByName("HEL")!.UserInstallDir.Should().Be(gf.ByName("HEL")!.InstallDir);
        gf.InstallDirs = null!;
        gf.InstallDirs.Should().BeEmpty();
    }

    [Fact]
    public void GameFolders_SetInstallDir_FallsBackWhenDirectoryDisappears()
    {
        var tmp = new TempDir();
        var gf = new GameFolders();
        var wa = gf.ByName("WA")!;
        gf.SetInstallDir(wa, tmp.Path);
        wa.UserInstallDir.Should().Be(tmp.Path);
        tmp.Dispose();
        wa.UserInstallDir.Should().Be(wa.InstallDir);
        gf.SetInstallDir(wa, null);
    }

    [Fact]
    public void GameFolders_RegistryReader_OverridesDefaultInstallDir()
    {
        using var tmp = new TempDir();
        var doc = Definition("/nonexistent-root/", ("base", 0, 99, "Base", "b.package"));
        var gf = new GameFolders(doc, guid => tmp.Path);
        gf.Games[0].InstallDir.Should().Be(tmp.Path);
    }

    [Fact]
    public void GameFolders_DefaultInstallDir_CombinesRootFolder()
    {
        using var tmp = new TempDir();
        var gf = new GameFolders(Definition(tmp.Path, ("base", 0, 99, "Base", "b.package")), _ => null);
        gf.Games[0].InstallDir.Should().Be(Path.GetFullPath(tmp.Combine("Base")));
        gf.RootFolder = tmp.Combine("other");
        gf.Games[0].InstallDir.Should().Be(Path.GetFullPath(tmp.Combine("other", "Base")));
    }

    [Fact]
    public void GameFolders_InvalidDefinition_Throws() =>
        FluentActions.Invoking(() => new GameFolders(new XDocument(new XElement("other")))).Should().Throw<InvalidDataException>();

    [Fact]
    public void GameFolders_DefaultModsFolder_EndsWithMods() =>
        GameFolders.DefaultModsFolder()!.Should().EndWith(Path.Combine("The Sims 4", "Mods"));

    // ---- Game package discovery ----

    [Fact]
    public async Task Game_GameContent_ListsOnlyExistingPackages()
    {
        using var tmp = new TempDir();
        await WritePackageAsync(tmp.Combine("Base", "b.package"));
        var gf = new GameFolders(Definition(tmp.Path, ("base", 0, 99, "Base", "b.package")), _ => null);
        gf.Games[0].GameContent.Select(p => p.Path).Should().Equal(Path.Combine(tmp.Combine("Base"), "b.package"));
        gf.Games[0].DDSImages.Should().BeEmpty();      // dds package file absent
        gf.Games[0].Thumbnails.Should().BeEmpty();     // no packages listed
    }

    [Fact]
    public void Game_MissingInstallDir_YieldsNothing()
    {
        using var tmp = new TempDir();
        var gf = new GameFolders(Definition(tmp.Path, ("base", 0, 99, "Nowhere", "b.package")), _ => null);
        gf.Games[0].GameContent.Should().BeEmpty();
    }

    // ---- GameFileTable ----

    private static async Task<(TempDir tmp, GameFolders gf)> TwoGamesAsync()
    {
        var tmp = new TempDir();
        await WritePackageAsync(tmp.Combine("Base", "b.package"), (K(1, 0, 1), [1]), (K(1, 0, 2), [2]));
        await WritePackageAsync(tmp.Combine("EP1", "e.package"), (K(1, 0, 2), [22]), (K(1, 0, 3), [33]));
        // Definition order: EP1 first, but base has the higher priority and must be searched first.
        var gf = new GameFolders(Definition(tmp.Path,
            ("EP1", 1, 1, "EP1", "e.package"),
            ("base", 0, 99, "Base", "b.package")), _ => null);
        return (tmp, gf);
    }

    [Fact]
    public async Task VendorContent_IsOrderedByGamePriority_AndLookupHonoursIt()
    {
        var (tmp, gf) = await TwoGamesAsync();
        using var _ = tmp;
        await using var ft = new GameFileTable(gf) { FileTableEnabled = true };

        ft.GameContent.Select(p => Path.GetFileName(p.Path)).Should().Equal("b.package", "e.package");
        ft.IsOK.Should().BeTrue();

        var hit = await ft.FindAsync(K(1, 0, 2));
        hit.Should().NotBeNull();
        Path.GetFileName(hit!.PathPackage.Path).Should().Be("b.package");
        hit.PPSource.Should().Be("fb0");
        (await hit.GetDataAsync()).ToArray().Should().Equal([2]);

        var onlyInEp = await ft.FindAsync(K(1, 0, 3));
        Path.GetFileName(onlyInEp!.PathPackage.Path).Should().Be("e.package");

        (await ft.FindAsync(K(1, 0, 99))).Should().BeNull();
    }

    [Fact]
    public async Task DisabledGame_IsExcluded_AndTableDisabledMeansEmpty()
    {
        var (tmp, gf) = await TwoGamesAsync();
        using var _ = tmp;
        await using var ft = new GameFileTable(gf);
        ft.GameContent.Should().BeEmpty();
        ft.IsOK.Should().BeFalse();

        ft.FileTableEnabled = true;
        gf.EPsDisabled = "EP1";
        await ft.ResetAsync();           // also clears enabled flags
        ft.FileTableEnabled = true;
        ft.GameContent.Select(p => Path.GetFileName(p.Path)).Should().Equal("b.package");
    }

    [Fact]
    public async Task Lookup_UsesLegacyKeyEquivalence()
    {
        var (tmp, gf) = await TwoGamesAsync();
        using var _ = tmp;
        await using var ft = new GameFileTable(gf) { FileTableEnabled = true };
        var hit = await ft.FindAsync(K(1, 0x08000000, 1)); // group version bits ignored
        hit.Should().NotBeNull();
        hit!.RequestedKey.Should().Be(K(1, 0x08000000, 1));
        hit.Entry.Key.Should().Be(K(1, 0, 1));
        hit.RGVsn.Should().Be("");
    }

    [Fact]
    public async Task SearchOrder_CurrentFirst_ThenCustomContent_ThenVendor()
    {
        var (tmp, gf) = await TwoGamesAsync();
        using var _ = tmp;
        var modsDir = tmp.Combine("Mods");
        await WritePackageAsync(Path.Combine(modsDir, "cc.package"), (K(1, 0, 2), [200]));
        var currentPath = await WritePackageAsync(tmp.Combine("cur.package"), (K(1, 0, 2), [100]));

        await using var ft = new GameFileTable(gf) { FileTableEnabled = true, CustomContentPath = modsDir };
        await using var current = new PathPackageTuple(currentPath);

        // cc not enabled, no current: vendor wins
        (await (await ft.FindAsync(K(1, 0, 2)))!.GetDataAsync()).ToArray().Should().Equal([2]);

        ft.CustomContentEnabled = true;
        var cc = await ft.FindAsync(K(1, 0, 2));
        (await cc!.GetDataAsync()).ToArray().Should().Equal([200]);
        ft.GetSource(cc.PathPackage).Should().Be("cc");

        ft.Current = current;
        var cur = await ft.FindAsync(K(1, 0, 2));
        (await cur!.GetDataAsync()).ToArray().Should().Equal([100]);
        ft.GetSource(current).Should().Be("current");
        ft.GameContent.Select(p => Path.GetFileName(p.Path)).Should().Equal("cur.package", "cc.package", "b.package", "e.package");
        ft.GetSource(new PathPackageTuple(tmp.Combine("elsewhere.package"))).Should().Be("unk");
    }

    [Fact]
    public async Task CustomContent_IsDepthFirst_PackageOnly_CaseInsensitive()
    {
        using var tmp = new TempDir();
        await WritePackageAsync(tmp.Combine("z.package"));
        await WritePackageAsync(tmp.Combine("a", "deep", "d.package"));
        await WritePackageAsync(tmp.Combine("b", "b.PACKAGE"));
        await File.WriteAllTextAsync(tmp.Combine("note.txt"), "x");
        await File.WriteAllTextAsync(tmp.Combine("a", "x.ts4script"), "x");

        await using var ft = new GameFileTable(new GameFolders()) { CustomContentPath = tmp.Path };
        ft.CustomContent.Select(p => Path.GetFileName(p.Path)).Should().Equal("d.package", "b.PACKAGE", "z.package");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/definitely/not/here")]
    public async Task CustomContent_MissingOrInvalidPath_IsEmpty(string? path)
    {
        await using var ft = new GameFileTable(new GameFolders()) { CustomContentPath = path, CustomContentEnabled = true };
        ft.CustomContent.Should().BeEmpty();
        (await ft.FindAsync(K(1, 0, 1))).Should().BeNull();
    }

    [Fact]
    public async Task CustomContent_CorruptPackage_IsSkippedDuringLookup()
    {
        using var tmp = new TempDir();
        await File.WriteAllBytesAsync(tmp.Combine("a-bad.package"), new byte[200]);
        await File.WriteAllBytesAsync(tmp.Combine("a-short.package"), [1, 2, 3]);
        await WritePackageAsync(tmp.Combine("b-good.package"), (K(5, 0, 5), [5]));

        await using var ft = new GameFileTable(new GameFolders()) { CustomContentPath = tmp.Path, CustomContentEnabled = true };
        ft.CustomContent.Should().HaveCount(3);
        var hit = await ft.FindAsync(K(5, 0, 5));
        Path.GetFileName(hit!.PathPackage.Path).Should().Be("b-good.package");
    }

    [Fact]
    public async Task ChangingCustomContentPath_RefreshesList()
    {
        using var tmp = new TempDir();
        await WritePackageAsync(tmp.Combine("one", "1.package"));
        await WritePackageAsync(tmp.Combine("two", "2.package"));
        await using var ft = new GameFileTable(new GameFolders()) { CustomContentPath = tmp.Combine("one") };
        ft.CustomContent.Should().ContainSingle().Which.Path.Should().EndWith("1.package");
        ft.CustomContentPath = tmp.Combine("two");
        ft.CustomContent.Should().ContainSingle().Which.Path.Should().EndWith("2.package");
    }

    [Fact]
    public async Task PathPackageTuple_OpensLazily_AndDisposeCloses()
    {
        using var tmp = new TempDir();
        var path = await WritePackageAsync(tmp.Combine("p.package"), (K(1, 1, 1), [9]));
        var ppt = new PathPackageTuple(path);
        ppt.OpenedPackage.Should().BeNull();

        var pkg = await ppt.GetPackageAsync();
        pkg.IsReadOnly.Should().BeTrue();
        (await ppt.GetPackageAsync()).Should().BeSameAs(pkg);

        (await ppt.FindAsync(e => e.Key.Instance == 1)).Should().NotBeNull();
        (await ppt.FindAsync(e => e.Key.Instance == 2)).Should().BeNull();
        (await ppt.FindAllAsync(_ => true)).Should().HaveCount(1);

        await ppt.DisposeAsync();
        ppt.OpenedPackage.Should().BeNull();
        await FluentActions.Awaiting(async () => await ppt.GetPackageAsync()).Should().ThrowAsync<ObjectDisposedException>();
        File.Delete(path); // file handle released
    }

    [Fact]
    public async Task PathPackageTuple_Missing_TryGetReturnsNull_GetThrows()
    {
        using var tmp = new TempDir();
        await using var ppt = new PathPackageTuple(tmp.Combine("nope.package"));
        (await ppt.TryGetPackageAsync()).Should().BeNull();
        (await ppt.FindAsync(_ => true)).Should().BeNull();
        await FluentActions.Awaiting(async () => await ppt.GetPackageAsync()).Should().ThrowAsync<IOException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void PathPackageTuple_InvalidPath_Throws(string? path) =>
        FluentActions.Invoking(() => new PathPackageTuple(path!)).Should().Throw<ArgumentException>();

    [Fact]
    public void PathPackageTuple_EqualityIsByFullPath()
    {
        var a = new PathPackageTuple("some/dir/../x.package");
        var b = new PathPackageTuple("some/x.package");
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Equals(new PathPackageTuple("other.package")).Should().BeFalse();
    }

    [Fact]
    public async Task SpecificResource_RejectsEntryFromAnotherPackage_AndResolvesRgVersion()
    {
        var (tmp, gf) = await TwoGamesAsync();
        using var _ = tmp;
        await using var a = new PathPackageTuple(tmp.Combine("Base", "b.package"));
        await using var b = new PathPackageTuple(tmp.Combine("EP1", "e.package"));
        var pa = await a.GetPackageAsync();
        var entry = pa.Find(K(1, 0, 1))!;
        await b.GetPackageAsync();

        FluentActions.Invoking(() => new SpecificResource(b, entry)).Should().Throw<ArgumentException>();

        var sr = new SpecificResource(a, pa.Find(K(1, 0, 1))!, gameFolders: gf);
        sr.PPSource.Should().Be("unk");
        sr.LongName.Should().Contain("0x00000001-0x00000000-0x0000000000000001").And.Contain("b.package");
        sr.RGVsn.Should().Be("");

        var rg1 = await WritePackageAsync(tmp.Combine("rg.package"), (K(1, 0x08000000, 1), [1]), (K(1, 0x50000000, 1), [1]));
        await using var c = new PathPackageTuple(rg1);
        var pc = await c.GetPackageAsync();
        new SpecificResource(c, pc.Find(K(1, 0x08000000, 1))!, gameFolders: gf).RGVsn.Should().Be("EP1");
        new SpecificResource(c, pc.Find(K(1, 0x50000000, 1))!, gameFolders: gf).RGVsn.Should().Be("Unk");
    }

    [Fact]
    public async Task IsEqual_ComparesByPathInOrder()
    {
        var a = new PathPackageTuple("a.package");
        var b = new PathPackageTuple("b.package");
        GameFileTable.IsEqual(null, null).Should().BeTrue();
        GameFileTable.IsEqual(null, [a]).Should().BeFalse();
        GameFileTable.IsEqual([a, b], [a, b]).Should().BeTrue();
        GameFileTable.IsEqual([a, b], [b, a]).Should().BeFalse();
        GameFileTable.IsEqual([a], [a, b]).Should().BeFalse();
        await a.DisposeAsync();
    }

    [Fact]
    public async Task Reset_DisposesOpenedPackages_ButNotCurrent()
    {
        var (tmp, gf) = await TwoGamesAsync();
        using var _ = tmp;
        var currentPath = await WritePackageAsync(tmp.Combine("cur.package"), (K(1, 0, 1), [7]));
        await using var current = new PathPackageTuple(currentPath);
        var ft = new GameFileTable(gf) { FileTableEnabled = true, Current = current };
        var hit = await ft.FindAsync(K(1, 0, 2));       // opens current, then b.package
        var vendorPpt = hit!.PathPackage;
        vendorPpt.OpenedPackage.Should().NotBeNull();

        await ft.ResetAsync();
        vendorPpt.OpenedPackage.Should().BeNull();
        current.OpenedPackage.Should().NotBeNull();
        ft.Current.Should().BeNull();
        ft.FileTableEnabled.Should().BeFalse();
    }
}
