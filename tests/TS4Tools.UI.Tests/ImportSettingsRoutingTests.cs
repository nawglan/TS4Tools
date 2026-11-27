using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.Hashing;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// DDS-as-DST, thumbnail, DBC and Merge Packages imports go through the import settings
/// (duplicate handling, compress, names) and the DBC import merges string tables.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 133-162,
/// 197-303 (ResourceImportAsDbc, DupsOnly), 314-503 (ImportPackagesCommon).
/// </remarks>
public sealed class ImportSettingsRoutingTests : IDisposable
{
    private const uint StblType = 0x220557DA;
    private const uint XmlType = 0x03B33DDF;
    private const uint ManifestType = 0x73E93EEB;
    private const uint NameMapType = 0x0166038C;
    private const uint ThumbType = 0x3C2A8647;
    private static readonly ResourceKey StblKey = new(StblType, 0, 0x1111);
    private static readonly ResourceKey XmlKey = new(XmlType, 0, 0x2222);
    private static readonly ResourceKey OtherKey = new(0x00AA0001, 0, 0x3333);

    private readonly List<string> _tempFiles = [];
    private readonly FakeDialogService _dialogs = new();
    private readonly FakeResourceOpsDialogService _ops = new();
    private readonly MockFileSystemService _fileSystem = new();

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }

    private MainWindowViewModel CreateViewModel() =>
        new(_fileSystem, _dialogs, _ => null) { ResourceOpsDialogs = _ops };

    private string NewTempPath(string extension = "package")
    {
        var path = Path.Combine(Path.GetTempPath(), $"ts4tools-route-{Guid.NewGuid():N}.{extension}");
        _tempFiles.Add(path);
        return path;
    }

    private static byte[] Stbl(params (uint Key, string Value)[] entries)
    {
        var stbl = new StblResource(StblKey, ReadOnlyMemory<byte>.Empty);
        foreach (var (key, value) in entries)
        {
            stbl.Add(key, value);
        }
        return stbl.Data.ToArray();
    }

    private async Task<string> SavePackageAsync(params (ResourceKey Key, byte[] Data)[] resources)
    {
        var package = DbpfPackage.CreateNew();
        foreach (var (key, data) in resources)
        {
            package.AddResource(key, data, rejectDuplicates: false);
        }
        var path = NewTempPath();
        await package.SaveAsAsync(path);
        package.Dispose();
        return path;
    }

    private static async Task<Dictionary<uint, string>> StblValuesAsync(DbpfPackage package, IResourceIndexEntry entry)
    {
        var stbl = new StblResource(entry.Key, await package.GetResourceDataAsync(entry));
        return stbl.Entries.ToDictionary(e => e.KeyHash, e => e.Value);
    }

    #region DBC

    [Fact]
    public async Task ImportDbc_MergesDuplicateStringTables_FirstValueWins()
    {
        var first = await SavePackageAsync((StblKey, Stbl((1, "a"), (2, "b"))));
        var second = await SavePackageAsync((StblKey, Stbl((2, "B2"), (3, "c"))));
        var package = DbpfPackage.CreateNew();
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        SettingsService.Instance.Settings.PromptDbcAutosave = true;
        _ops.YesNoCancelAnswer = YesNoCancelResult.No;
        _ops.Packages.Enqueue([first, second]);

        await vm.ImportDbcCommand.ExecuteAsync(null);

        _ops.DbcWarningsShown.Should().Be(1);
        var live = package.FindAll(e => e.Key == StblKey).ToList();
        live.Should().ContainSingle();
        (await StblValuesAsync(package, live[0])).Should().BeEquivalentTo(
            new Dictionary<uint, string> { [1] = "a", [2] = "b", [3] = "c" });

        // The superseded tables stay listed, flagged deleted
        package.Resources.Count(e => e.Key == StblKey && e.IsDeleted).Should().Be(2);
        vm.Resources.Count(r => r.Key == StblKey && r.IsDeleted).Should().Be(2);
    }

    [Fact]
    public async Task ImportDbc_DropsSims3PackManifests_AndReportsDuplicateXml()
    {
        var first = await SavePackageAsync((new ResourceKey(ManifestType, 0, 1), new byte[] { 1 }), (XmlKey, new byte[] { 1 }));
        var second = await SavePackageAsync((XmlKey, new byte[] { 2 }));
        var package = DbpfPackage.CreateNew();
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        SettingsService.Instance.Settings.PromptDbcAutosave = true;
        _ops.Packages.Enqueue([first, second]);

        await vm.ImportDbcCommand.ExecuteAsync(null);

        package.FindAll(e => e.Key.ResourceType == ManifestType).Should().BeEmpty();
        // Both XML versions are kept (Allow) and the user is told to merge them by hand
        package.FindAll(e => e.Key == XmlKey).Should().HaveCount(2);
        _ops.Messages.Should().Contain("Manual merge of XML files required.");
    }

    [Fact]
    public async Task ImportDbc_ReplacesDuplicatesOfOtherTypes()
    {
        var first = await SavePackageAsync((OtherKey, new byte[] { 1 }));
        var second = await SavePackageAsync((OtherKey, new byte[] { 2 }));
        var package = DbpfPackage.CreateNew();
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        SettingsService.Instance.Settings.PromptDbcAutosave = true;
        _ops.Packages.Enqueue([first, second]);

        await vm.ImportDbcCommand.ExecuteAsync(null);

        var live = package.FindAll(e => e.Key == OtherKey).ToList();
        live.Should().ContainSingle();
        (await package.GetResourceDataAsync(live[0])).ToArray().Should().Equal(2);
    }

    [Fact]
    public async Task ImportDbc_WarningDeclined_ImportsNothing()
    {
        var source = await SavePackageAsync((OtherKey, new byte[] { 1 }));
        var package = DbpfPackage.CreateNew();
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        _ops.DbcWarningAnswer = false;
        _ops.Packages.Enqueue([source]);

        await vm.ImportDbcCommand.ExecuteAsync(null);

        package.Resources.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportDbc_AutoSavesWithoutAsking_UnlessPromptSettingIsOn()
    {
        var source = await SavePackageAsync((OtherKey, new byte[] { 1 }));
        var target = NewTempPath();
        var initial = DbpfPackage.CreateNew();
        await initial.SaveAsAsync(target);
        await initial.DisposeAsync();
        var package = await DbpfPackage.OpenAsync(target, readWrite: true);
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, target);
        SettingsService.Instance.Settings.PromptDbcAutosave = false;
        _ops.YesNoCancelAnswer = YesNoCancelResult.Cancel; // would abort if the prompt were shown
        _ops.Packages.Enqueue([source]);

        await vm.ImportDbcCommand.ExecuteAsync(null);

        // The package was saved after the import (without asking); the clean up that follows is not saved (legacy)
        await using var onDisk = await DbpfPackage.OpenAsync(target);
        onDisk.Resources.Should().ContainSingle().Which.Key.Should().Be(OtherKey);
    }

    #endregion

    #region Merge, thumbnails, DDS as DST

    [Fact]
    public async Task MergePackages_UsesImportSettings_RejectDuplicates()
    {
        var source = await SavePackageAsync((OtherKey, new byte[] { 9 }), (XmlKey, new byte[] { 5 }));
        var package = DbpfPackage.CreateNew();
        package.AddResource(OtherKey, new byte[] { 1 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        _ops.OnImportBatch = b => { b.Settings.Replace = false; b.Settings.Compress = false; return true; };
        _ops.YesNoCancelAnswer = YesNoCancelResult.No;
        _ops.Packages.Enqueue([source]);

        await vm.MergePackagesCommand.ExecuteAsync(null);

        _ops.BatchesShown.Should().ContainSingle().Which.Mode.Should().Be(ImportBatchMode.Package);
        var existing = package.Find(OtherKey)!;
        (await package.GetResourceDataAsync(existing)).ToArray().Should().Equal(1);
        package.Find(XmlKey)!.CompressionType.Should().Be(0);
    }

    [Fact]
    public async Task ImportThumbnails_HonoursSettingsAndBatchEdits()
    {
        var package = DbpfPackage.CreateNew();
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        _ops.OnImportBatch = b =>
        {
            b.Settings.Compress = false;
            b.Items.Remove("skip.png");
            return true;
        };
        _ops.ThumbnailFiles = [("one.png", new byte[] { 1 }), ("skip.png", new byte[] { 2 })];

        await vm.ImportThumbnailCommand.ExecuteAsync(null);

        package.FindAll(e => e.Key.ResourceType == ThumbType).Should().ContainSingle();
        var entry = package.FindAll(e => e.Key.ResourceType == ThumbType).Single();
        entry.Key.Should().Be(new ResourceKey(0x3C2A8647, 0, FnvHash.Fnv64("one")));
        entry.CompressionType.Should().Be(0);
        // UseName defaults on, so the name lands in a name map
        package.FindAll(e => e.Key.ResourceType == NameMapType).Should().ContainSingle();
    }

    [Fact]
    public async Task ImportThumbnails_RejectDuplicates_KeepsExisting()
    {
        var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x3C2A8647, 0, FnvHash.Fnv64("one"));
        package.AddResource(key, new byte[] { 7 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        _ops.OnImportBatch = b => { b.Settings.Replace = false; return true; };

        await vm.ImportThumbnailsAsync([("one.png", new byte[] { 1 })]);

        (await package.GetResourceDataAsync(package.Find(key)!)).ToArray().Should().Equal(7);
    }

    [Fact]
    public async Task ImportDdsAsDst_UsesResourceDetailsKeyAndSettings()
    {
        var ddsPath = NewTempPath("dds");
        _fileSystem.SetupFile(ddsPath, DdsImage.CreateSolid(0xFF336699, 8, 8).SaveToDds());
        var package = DbpfPackage.CreateNew();
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        var custom = new ResourceKey(0x00B2D882, 0x10, 0x99);
        _ops.OnResourceDetails = d => { d.SetKey(custom); d.Settings.Compress = false; return true; };
        _ops.ImportFiles.Enqueue([ddsPath]);

        await vm.ImportDdsAsDstCommand.ExecuteAsync(null);

        _ops.DetailsShown.Should().ContainSingle();
        var entry = package.Find(custom)!;
        entry.Should().NotBeNull();
        entry.CompressionType.Should().Be(0);
        DstDecoder.IsDst((await package.GetResourceDataAsync(entry)).Span).Should().BeTrue();
    }

    [Fact]
    public async Task ImportDdsAsDst_RejectDuplicate_LeavesExisting()
    {
        var ddsPath = NewTempPath("dds");
        _fileSystem.SetupFile(ddsPath, DdsImage.CreateSolid(0xFF336699, 8, 8).SaveToDds());
        var key = new ResourceKey(0x00B2D882, 0, FnvHash.Fnv64(Path.GetFileNameWithoutExtension(ddsPath)));
        var package = DbpfPackage.CreateNew();
        package.AddResource(key, new byte[] { 7 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        _ops.OnResourceDetails = d => { d.Settings.Replace = false; return true; };

        await vm.ImportDdsAsDstFileAsync(ddsPath);

        (await package.GetResourceDataAsync(package.Find(key)!)).ToArray().Should().Equal(7);
    }

    #endregion
}
