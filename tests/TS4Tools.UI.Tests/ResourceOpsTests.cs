using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.Hashing;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Resource menu operations, checked against legacy s4pe behaviour.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1280-2091
/// (Resource menu) and legacy_references/Sims4Tools/s4pe/Import/Import.cs (import, paste, drop).
/// </remarks>
public sealed class ResourceOpsTests : IDisposable
{
    private const uint StblType = 0x220557DA;
    private const uint NameMapType = 0x0166038C;
    private static readonly ResourceKey StblKey = new(StblType, 0, 0x1111);
    private static readonly ResourceKey OtherKey = new(0x03B33DDF, 0, 0x2222);

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

    private string NewTempPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ts4tools-resops-{Guid.NewGuid():N}.package");
        _tempFiles.Add(path);
        return path;
    }

    private MainWindowViewModel CreateViewModel() =>
        new(_fileSystem, _dialogs, _ => null) { ResourceOpsDialogs = _ops };

    private static byte[] StblData(string value)
    {
        var stbl = new StblResource(StblKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(0x00000001, value);
        return stbl.Data.ToArray();
    }

    private static async Task<string> ReadStblValueAsync(DbpfPackage package, IResourceIndexEntry entry)
    {
        var stbl = new StblResource(entry.Key, await package.GetResourceDataAsync(entry));
        return stbl.Entries.Single().Value;
    }

    private static async Task<DbpfPackage> RoundTripAsync(DbpfPackage package)
    {
        var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);
        ms.Position = 0;
        return await DbpfPackage.OpenAsync(ms);
    }

    private static async Task SelectAsync(MainWindowViewModel vm, ResourceItemViewModel item)
    {
        vm.SelectedResource = item;
        await vm.PendingSelectionTask;
    }

    private static void MultiSelect(MainWindowViewModel vm, params ResourceItemViewModel[] items)
    {
        vm.SelectedResources.Clear();
        foreach (var item in items)
        {
            vm.SelectedResources.Add(item);
        }
    }

    private static void FillDetails(ResourceDetailsViewModel d, ResourceKey key, string name, bool compress = true)
    {
        d.SetKey(key);
        d.ResourceName = name;
        d.Settings.Compress = compress;
    }

    #region Duplicate keys

    [Fact]
    public async Task DuplicateKeys_AreDistinctRows_ThroughSelectEditCommitAndDelete()
    {
        var package = DbpfPackage.CreateNew();
        var first = package.AddResource(StblKey, StblData("first"))!;
        var second = package.AddResource(StblKey, StblData("second"), rejectDuplicates: false)!;

        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");

        vm.Resources.Should().HaveCount(2);
        var secondItem = vm.Resources.Single(r => ReferenceEquals(r.Entry, second));
        var firstItem = vm.Resources.Single(r => ReferenceEquals(r.Entry, first));

        // Selecting the second row loads the second entry, not the first with the same key
        await SelectAsync(vm, secondItem);
        var editor = vm.CurrentEditorViewModel.Should().BeOfType<StblEditorViewModel>().Subject;
        editor.Entries.Single().Value.Should().Be("second");

        editor.Entries.Single().Value = "edited";
        vm.CommitCommand.Execute(null);

        (await ReadStblValueAsync(package, second)).Should().Be("edited");
        (await ReadStblValueAsync(package, first)).Should().Be("first");

        // Deleting the first row only flags the first entry
        await SelectAsync(vm, firstItem);
        vm.ToggleDeletedCommand.Execute(null);
        first.IsDeleted.Should().BeTrue();
        second.IsDeleted.Should().BeFalse();
        secondItem.IsDeleted.Should().BeFalse();

        var reopened = await RoundTripAsync(package);
        reopened.Resources.Should().ContainSingle();
        (await ReadStblValueAsync(reopened, reopened.Resources[0])).Should().Be("edited");
    }

    #endregion

    #region Multi-selection

    [Fact]
    public async Task ToggleDeleted_MultiSelection_IsReversibleAndTriState()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("a"));
        package.AddResource(OtherKey, new byte[] { 1, 2, 3 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        var a = vm.Resources[0];
        var b = vm.Resources[1];

        MultiSelect(vm, a, b);
        vm.SelectionDeletedState.Should().BeFalse();

        vm.ToggleDeletedCommand.Execute(null);
        a.IsDeleted.Should().BeTrue();
        b.IsDeleted.Should().BeTrue();
        vm.SelectionDeletedState.Should().BeTrue();
        vm.IsSelectionDeleted.Should().BeTrue();
        vm.Resources.Should().HaveCount(2, "deleted resources stay listed until the package is saved");

        vm.ToggleDeletedCommand.Execute(null);
        a.IsDeleted.Should().BeFalse();
        b.IsDeleted.Should().BeFalse();

        // Mixed selection: indeterminate, and toggling marks all deleted (legacy target = state != Checked)
        MultiSelect(vm, a);
        vm.ToggleDeletedCommand.Execute(null);
        MultiSelect(vm, a, b);
        vm.SelectionDeletedState.Should().BeNull();
        vm.ToggleDeletedCommand.Execute(null);
        a.IsDeleted.Should().BeTrue();
        b.IsDeleted.Should().BeTrue();
        vm.IsPackageDirty.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleCompressed_MultiSelection_SetsAndClearsZlib()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("a"));
        package.AddResource(OtherKey, new byte[64]);
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        MultiSelect(vm, vm.Resources[0], vm.Resources[1]);

        vm.SelectionCompressedState.Should().BeFalse();
        vm.ToggleCompressedCommand.Execute(null);
        package.Resources.Should().OnlyContain(e => e.CompressionType == 0x5A42);
        vm.Resources.Should().OnlyContain(r => r.IsCompressed);
        vm.IsSelectionCompressed.Should().BeTrue();

        vm.ToggleCompressedCommand.Execute(null);
        package.Resources.Should().OnlyContain(e => e.CompressionType == 0);

        package.SetCompression(package.Resources[0], 0x5A42);
        vm.Resources[0].Refresh();
        vm.SelectionCompressedState.Should().BeNull();
    }

    [Fact]
    public async Task ToggleCompressed_PersistsThroughSaveAndReopen()
    {
        var data = new byte[4096];
        var package = DbpfPackage.CreateNew();
        package.AddResource(OtherKey, data);
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        await SelectAsync(vm, vm.Resources[0]);

        vm.ToggleCompressedCommand.Execute(null);

        var reopened = await RoundTripAsync(package);
        var entry = reopened.Resources.Single();
        entry.CompressionType.Should().Be(0x5A42);
        entry.FileSize.Should().BeLessThan(entry.MemorySize);
        (await reopened.GetResourceDataAsync(entry)).ToArray().Should().Equal(data);
    }

    [Fact]
    public async Task Duplicate_MultiSelection_AddsDistinctCopiesWithSameCompression()
    {
        var package = DbpfPackage.CreateNew();
        var a = package.AddResource(StblKey, StblData("a"))!;
        package.SetCompression(a, 0x5A42);
        package.AddResource(OtherKey, new byte[] { 9 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        MultiSelect(vm, vm.Resources[0], vm.Resources[1]);

        await vm.DuplicateResourceCommand.ExecuteAsync(null);

        package.Resources.Should().HaveCount(4);
        vm.Resources.Should().HaveCount(4);
        vm.Resources.Select(r => r.Entry).Should().OnlyHaveUniqueItems();
        package.Resources.Where(e => e.Key == StblKey).Should().HaveCount(2).And.OnlyContain(e => e.CompressionType == 0x5A42);
        vm.IsPackageDirty.Should().BeTrue();
    }

    [Fact]
    public async Task Copy_MultiSelection_PastesAllIntoAnotherPackage()
    {
        var source = DbpfPackage.CreateNew();
        source.AddResource(StblKey, StblData("a"));
        source.AddResource(OtherKey, new byte[] { 1, 2, 3 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(source, "");
        MultiSelect(vm, vm.Resources[0], vm.Resources[1]);

        await vm.CopyResourceCommand.ExecuteAsync(null);

        _ops.ClipboardPayload.Should().NotBeNull();
        _ops.ClipboardText.Should().Be("0x220557DA-0x00000000-0x0000000000001111\r\n0x03B33DDF-0x00000000-0x0000000000002222");

        var target = DbpfPackage.CreateNew();
        var vm2 = CreateViewModel();
        await vm2.AttachPackageAsync(target, "");
        await vm2.PasteResourceCommand.ExecuteAsync(null);

        _ops.BatchesShown.Should().ContainSingle().Which.Mode.Should().Be(ImportBatchMode.Paste);
        target.Resources.Select(e => e.Key).Should().BeEquivalentTo([StblKey, OtherKey]);
        (await target.GetResourceDataAsync(target.Find(OtherKey)!)).ToArray().Should().Equal(1, 2, 3);
        vm2.Resources.Should().HaveCount(2);
    }

    [Fact]
    public async Task Paste_Multiple_RejectDuplicates_KeepsExisting()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("a"));
        package.AddResource(OtherKey, new byte[] { 1 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        MultiSelect(vm, vm.Resources[0], vm.Resources[1]);
        await vm.CopyResourceCommand.ExecuteAsync(null);

        _ops.OnImportBatch = b => { b.Settings.Replace = false; return true; };
        await vm.PasteResourceCommand.ExecuteAsync(null);

        package.Resources.Should().HaveCount(2);

        // Replace: the originals are marked deleted (still listed) and the copies added
        _ops.OnImportBatch = _ => true;
        await vm.PasteResourceCommand.ExecuteAsync(null);
        package.Resources.Should().HaveCount(4);
        vm.Resources.Count(r => r.IsDeleted).Should().Be(2);
    }

    [Fact]
    public async Task Paste_Single_GoesThroughResourceDetails()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("a"));
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        await SelectAsync(vm, vm.Resources[0]);
        await vm.CopyResourceCommand.ExecuteAsync(null);

        var newKey = new ResourceKey(StblType, 0, 0x9999);
        _ops.OnResourceDetails = d =>
        {
            d.Key.Should().Be(StblKey);
            d.ShowFilename.Should().BeTrue();
            d.SetKey(newKey);
            return true;
        };
        await vm.PasteResourceCommand.ExecuteAsync(null);

        package.Find(newKey).Should().NotBeNull();
        vm.SelectedResource!.Key.Should().Be(newKey);
    }

    #endregion

    #region Add, details, names

    [Fact]
    public async Task AddResource_CreatesEmptyResourceWithNameAndCompression()
    {
        var package = DbpfPackage.CreateNew();
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        var key = new ResourceKey(StblType, 0x80000000, FNV64.GetHash("my_strings"));
        _ops.OnResourceDetails = d =>
        {
            d.ShowFilename.Should().BeFalse();
            d.Settings.UseName.Should().BeTrue();
            d.Settings.Compress.Should().BeTrue("legacy ImportSettings has Compress checked by default");
            FillDetails(d, key, "my_strings");
            return true;
        };

        await vm.AddResourceCommand.ExecuteAsync(null);

        var entry = package.Find(key);
        entry.Should().NotBeNull();
        entry!.CompressionType.Should().Be(0x5A42);
        // The new resource holds the data of a new, empty STBL wrapper
        new StblResource(key, await package.GetResourceDataAsync(entry)).Count.Should().Be(0);
        vm.SelectedResource!.Entry.Should().BeSameAs(entry);
        vm.SelectedResource.InstanceName.Should().Be("my_strings");

        var nameMapEntry = package.Find(e => e.Key.ResourceType == NameMapType);
        nameMapEntry.Should().NotBeNull();
        nameMapEntry!.Key.Should().Be(new ResourceKey(NameMapType, 0, 0));
        var names = new NameMapResource(nameMapEntry.Key, await package.GetResourceDataAsync(nameMapEntry));
        names[key.Instance].Should().Be("my_strings");
        vm.Resources.Should().Contain(r => r.Key.ResourceType == NameMapType);
    }

    [Fact]
    public async Task AddResource_RejectDuplicates_DoesNotAdd()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("a"));
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        _ops.OnResourceDetails = d =>
        {
            FillDetails(d, StblKey, "");
            d.Settings.Replace = false;
            return true;
        };

        await vm.AddResourceCommand.ExecuteAsync(null);

        package.Resources.Should().ContainSingle();
    }

    [Fact]
    public async Task ResourceDetails_AppliesKeyCompressionAndName()
    {
        var package = DbpfPackage.CreateNew();
        var entry = package.AddResource(StblKey, StblData("a"))!;
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        await SelectAsync(vm, vm.Resources[0]);
        var newKey = new ResourceKey(StblType, 1, 0x4242);

        _ops.OnResourceDetails = d =>
        {
            d.Settings.UseName.Should().BeFalse("the resource has no name yet (legacy useName = name present)");
            d.Settings.Compress.Should().BeFalse();
            d.Settings.UseName = true;
            FillDetails(d, newKey, "renamed", compress: true);
            return true;
        };
        await vm.ResourceDetailsCommand.ExecuteAsync(null);

        entry.Key.Should().Be(newKey, "the key is edited in place");
        entry.CompressionType.Should().Be(0x5A42);
        vm.Resources[0].Key.Should().Be(newKey);
        vm.Resources[0].InstanceName.Should().Be("renamed");
        vm.IsPackageDirty.Should().BeTrue();

        // A second rename without "Rename if present" keeps the existing name (legacy MergeNamemap replace=false)
        _ops.OnResourceDetails = d =>
        {
            d.Settings.UseName.Should().BeTrue();
            d.ResourceName.Should().Be("renamed");
            d.ResourceName = "other";
            return true;
        };
        await vm.ResourceDetailsCommand.ExecuteAsync(null);
        vm.Resources[0].InstanceName.Should().Be("renamed");

        _ops.OnResourceDetails = d =>
        {
            d.ResourceName = "other";
            d.Settings.AllowRename = true;
            return true;
        };
        await vm.ResourceDetailsCommand.ExecuteAsync(null);
        vm.Resources.Single(r => r.Entry == entry).InstanceName.Should().Be("other");
    }

    [Fact]
    public void ResourceDetails_HashButtons_MatchLegacyHashes()
    {
        var d = new ResourceDetailsViewModel(useName: true, displayFilename: false) { ResourceName = "a2o_test_clip" };

        d.ClipIidCommand.Execute(null);
        d.InstanceText.Should().Be($"0x{FNV64CLIP.GetHash("a2o_test_clip"):X16}");

        d.Fnv64Command.Execute(null);
        d.InstanceText.Should().Be($"0x{FNV64.GetHash("a2o_test_clip"):X16}");

        d.Fnv32Command.Execute(null);
        d.InstanceText.Should().Be($"0x{FNV32.GetHash("a2o_test_clip"):X16}");
    }

    [Fact]
    public void ResourceDetails_ParsesLegacyFieldFormats()
    {
        var d = new ResourceDetailsViewModel(useName: true, displayFilename: true);
        d.IsValid.Should().BeFalse();

        d.TypeText = "STBL 0x220557DA";
        d.GroupText = "0x80000000";
        d.InstanceText = "42";
        d.Key.Should().Be(new ResourceKey(StblType, 0x80000000, 42));
        d.OkCommand.CanExecute(null).Should().BeTrue();

        d.GroupText = "zz";
        d.IsValid.Should().BeFalse();
        d.OkCommand.CanExecute(null).Should().BeFalse();

        d.GroupText = "1";
        d.HighBitCommand.Execute(null);
        d.GroupText.Should().Be("0x80000001");
        d.InstanceText.Should().Be("0x800000000000002A");

        d.Filename = "/tmp/S4_220557DA_00000001_0000000000001234_Hello%2dWorld%%+STBL.stbl";
        d.Key.Should().Be(new ResourceKey(StblType, 1, 0x1234));
        d.ResourceName.Should().Be("Hello-World");
        d.TypeText.Should().Be("STBL 0x220557DA");

        d.TryPasteKey("0x03B33DDF-0x00000000-0x0000000000002222").Should().BeTrue();
        d.Key.Should().Be(OtherKey);
        d.KnownTypes.Should().Contain("STBL 0x220557DA");
    }

    #endregion

    #region Import

    [Fact]
    public async Task ImportFile_ReplaceDuplicates_MarksExistingDeleted()
    {
        var package = DbpfPackage.CreateNew();
        var existing = package.AddResource(StblKey, StblData("old"))!;
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        var file = "/import/" + Tgin.From(StblKey, "imported").ToFileName();
        _fileSystem.SetupFile(file, StblData("new"));

        _ops.ImportFiles.Enqueue([file]);
        _ops.OnResourceDetails = d =>
        {
            d.Key.Should().Be(StblKey);
            d.ResourceName.Should().Be("imported");
            d.Settings.Compress = false;
            return true;
        };
        await vm.ImportResourceCommand.ExecuteAsync(null);

        existing.IsDeleted.Should().BeTrue();
        vm.Resources.Single(r => r.Entry == existing).IsDeleted.Should().BeTrue();
        var added = package.Find(StblKey)!;
        added.Should().NotBeSameAs(existing);
        added.CompressionType.Should().Be(0);
        (await ReadStblValueAsync(package, added)).Should().Be("new");
        vm.SelectedResource!.Entry.Should().BeSameAs(added);
        vm.SelectedResource.InstanceName.Should().Be("imported");
    }

    [Fact]
    public async Task ImportFile_RejectDuplicates_AddsNothing()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("old"));
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        var file = "/import/" + Tgin.From(StblKey).ToFileName();
        _fileSystem.SetupFile(file, StblData("new"));
        _ops.OnResourceDetails = d => { d.Settings.Replace = false; return true; };

        await vm.ImportSingleFileAsync(file, "test");

        package.Resources.Should().ContainSingle();
        package.Resources[0].IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task ImportBatch_UsesTginNamesAndSettings()
    {
        var package = DbpfPackage.CreateNew();
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        var key1 = new ResourceKey(StblType, 0, 1);
        var key2 = new ResourceKey(0x03B33DDF, 0, 2);
        var file1 = "/batch/" + Tgin.From(key1, "one").ToFileName();
        var file2 = "/batch/" + Tgin.From(key2, "two").ToFileName();
        _fileSystem.SetupFile(file1, StblData("1"));
        _fileSystem.SetupFile(file2, new byte[512]);

        _ops.ImportFiles.Enqueue([file1, file2]);
        await vm.ImportResourceCommand.ExecuteAsync(null);

        var batch = _ops.BatchesShown.Should().ContainSingle().Subject;
        batch.Mode.Should().Be(ImportBatchMode.File);
        batch.Items.Should().Equal(file1, file2);
        package.Find(key1)!.CompressionType.Should().Be(0x5A42);
        package.Find(key2)!.CompressionType.Should().Be(0x5A42);
        vm.Resources.Single(r => r.Key == key1).InstanceName.Should().Be("one");
        vm.Resources.Single(r => r.Key == key2).InstanceName.Should().Be("two");
    }

    [Fact]
    public async Task ImportFromPackage_AllowsSettingsAndMergesNames()
    {
        var sourcePath = NewTempPath();
        var source = DbpfPackage.CreateNew();
        source.AddResource(StblKey, StblData("from package"));
        var names = new NameMapResource(new ResourceKey(NameMapType, 0, 7), ReadOnlyMemory<byte>.Empty) { { StblKey.Instance, "pkg_name" } };
        source.AddResource(names.Key, names.Data);
        await source.SaveAsAsync(sourcePath);
        await source.DisposeAsync();

        var package = DbpfPackage.CreateNew();
        var existing = package.AddResource(StblKey, StblData("mine"))!;
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");

        _ops.Packages.Enqueue([sourcePath]);
        _ops.OnImportBatch = b =>
        {
            b.Mode.Should().Be(ImportBatchMode.Package);
            b.Settings.Replace = false;
            return true;
        };
        await vm.ImportFromPackageCommand.ExecuteAsync(null);

        // Reject: the existing resource is kept; the name map is merged (not imported as a resource)
        package.Resources.Where(e => e.Key == StblKey).Should().ContainSingle().Which.Should().BeSameAs(existing);
        vm.Resources.Single(r => r.Entry == existing).InstanceName.Should().Be("pkg_name");
        package.Resources.Should().NotContain(e => e.Key == names.Key);

        _ops.Packages.Enqueue([sourcePath]);
        _ops.OnImportBatch = _ => true;
        await vm.ImportFromPackageCommand.ExecuteAsync(null);
        existing.IsDeleted.Should().BeTrue();
        (await ReadStblValueAsync(package, package.Find(StblKey)!)).Should().Be("from package");
    }

    [Fact]
    public async Task ReplaceFromPackages_ReplacesOnlySelected()
    {
        var sourcePath = NewTempPath();
        var source = DbpfPackage.CreateNew();
        source.AddResource(StblKey, StblData("replacement"));
        source.AddResource(OtherKey, new byte[] { 7 });
        await source.SaveAsAsync(sourcePath);
        await source.DisposeAsync();

        var package = DbpfPackage.CreateNew();
        var stbl = package.AddResource(StblKey, StblData("mine"))!;
        var other = package.AddResource(OtherKey, new byte[] { 1 })!;
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        await SelectAsync(vm, vm.Resources.Single(r => r.Entry == stbl));

        _ops.Packages.Enqueue([sourcePath]);
        _ops.OnImportBatch = b =>
        {
            b.Settings.ReplaceEnabled.Should().BeFalse();
            b.Settings.UseNameEnabled.Should().BeFalse();
            return true;
        };
        await vm.ReplaceFromPackagesCommand.ExecuteAsync(null);

        stbl.IsDeleted.Should().BeTrue();
        other.IsDeleted.Should().BeFalse();
        package.Resources.Where(e => e.Key == OtherKey).Should().ContainSingle();
        (await ReadStblValueAsync(package, package.Find(StblKey)!)).Should().Be("replacement");
    }

    [Fact]
    public async Task ReplaceResource_ReplacesSelectedEntryData()
    {
        var package = DbpfPackage.CreateNew();
        var entry = package.AddResource(OtherKey, new byte[] { 1 })!;
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        await SelectAsync(vm, vm.Resources[0]);
        _fileSystem.SetupFile("/r.bin", [5, 6, 7]);
        _ops.ReplaceFiles.Enqueue("/r.bin");

        await vm.ReplaceResourceCommand.ExecuteAsync(null);

        (await package.GetResourceDataAsync(entry)).ToArray().Should().Equal(5, 6, 7);
        vm.IsPackageDirty.Should().BeTrue();
    }

    #endregion

    #region Save and export

    [Fact]
    public async Task Save_DropsDeletedRowsAndRebindsTheRest()
    {
        var path = NewTempPath();
        var initial = DbpfPackage.CreateNew();
        initial.AddResource(StblKey, StblData("keep"));
        initial.AddResource(OtherKey, new byte[] { 1 });
        await initial.SaveAsAsync(path);
        await initial.DisposeAsync();

        var package = await DbpfPackage.OpenAsync(path, readWrite: true);
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, path);
        var keep = vm.Resources.Single(r => r.Key == StblKey);
        await SelectAsync(vm, vm.Resources.Single(r => r.Key == OtherKey));
        vm.ToggleDeletedCommand.Execute(null);
        await SelectAsync(vm, keep);

        await vm.SaveCommand.ExecuteAsync(null);

        vm.Resources.Should().ContainSingle().Which.Should().BeSameAs(keep);
        keep.Entry.Should().BeSameAs(package.Resources.Single());
        vm.SelectedResource.Should().BeSameAs(keep);
        vm.IsPackageDirty.Should().BeFalse();
        (await ReadStblValueAsync(package, keep.Entry!)).Should().Be("keep");
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task Export_MultiSelection_WritesTginNamedFilesToFolder()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("a"));
        package.AddResource(OtherKey, new byte[] { 4, 5 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        MultiSelect(vm, vm.Resources[0], vm.Resources[1]);
        _ops.ExportFolders.Enqueue("/out");

        await vm.ExportResourceCommand.ExecuteAsync(null);

        var stblFile = Path.Combine("/out", "S4_220557DA_00000000_0000000000001111%%+STBL.stbl");
        var otherFile = Path.Combine("/out", Tgin.From(OtherKey).ToFileName());
        _fileSystem.WrittenFiles.Should().ContainKey(stblFile);
        _fileSystem.WrittenFiles[otherFile].Should().Equal(4, 5);
    }

    [Fact]
    public async Task Export_Single_SuggestsTginNameWithResourceName()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("a"));
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        vm.Resources[0].InstanceName = "my-name";
        await SelectAsync(vm, vm.Resources[0]);
        _ops.ExportFiles.Enqueue("/out/x.stbl");

        await vm.ExportResourceCommand.ExecuteAsync(null);

        _ops.SuggestedExportNames.Should().Equal("S4_220557DA_00000000_0000000000001111_my%2dname%%+STBL.stbl");
        _fileSystem.WrittenFiles.Should().ContainKey("/out/x.stbl");
    }

    [Fact]
    public async Task Export_RemembersLastExportFolder_ForFileAndFolderPickers()
    {
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1773-1781 and 1798-1804
        // (both export dialogs start in LastExportPath and update it)
        var settings = SettingsService.Instance.Settings;
        var saved = settings.LastExportPath;
        try
        {
            settings.LastExportPath = "/start";
            var package = DbpfPackage.CreateNew();
            package.AddResource(StblKey, StblData("a"));
            package.AddResource(OtherKey, new byte[] { 4, 5 });
            var vm = CreateViewModel();
            await vm.AttachPackageAsync(package, "");

            await SelectAsync(vm, vm.Resources[0]);
            var exportFile = Path.Combine(Path.GetTempPath(), "ts4tools-exports", "x.stbl");
            _ops.ExportFiles.Enqueue(exportFile);
            await vm.ExportResourceCommand.ExecuteAsync(null);
            settings.LastExportPath.Should().Be(Path.GetDirectoryName(exportFile));

            MultiSelect(vm, vm.Resources[0], vm.Resources[1]);
            _ops.ExportFolders.Enqueue("/batch");
            await vm.ExportResourceCommand.ExecuteAsync(null);

            _ops.ExportStartFolders.Should().Equal("/start", Path.GetDirectoryName(exportFile));
            settings.LastExportPath.Should().Be("/batch");
            await vm.DisposeAsync();
        }
        finally
        {
            settings.LastExportPath = saved;
        }
    }

    [Fact]
    public async Task CopyResourceKey_UsesLegacyKeyFormat()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(StblKey, StblData("a"));
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        await SelectAsync(vm, vm.Resources[0]);

        await vm.CopyResourceKeyCommand.ExecuteAsync(null);

        _ops.ClipboardText.Should().Be("0x220557DA-0x00000000-0x0000000000001111");
    }

    [Fact]
    public async Task CommandLineImport_ImportsBatchIntoNewPackage()
    {
        // Legacy MainForm_Shown runs ImportBatch(cmdLineBatch, "-import"), creating a new package.
        var file = "/cmd/" + Tgin.From(OtherKey).ToFileName();
        _fileSystem.SetupFile(file, [1, 2]);
        var options = CommandLineOptions.Parse(["-import", file], _fileSystem.FileExists);
        options.ImportFiles.Should().Equal(file);
        var vm = CreateViewModel();

        await vm.OpenFromCommandLineAsync(options);

        vm.HasOpenPackage.Should().BeTrue();
        _ops.BatchesShown.Should().ContainSingle().Which.Title.Should().Be("-import");
        vm.Resources.Should().ContainSingle(r => r.Key == OtherKey);
        await vm.DisposeAsync();
    }

    [Fact]
    public async Task DuplicateKeys_ShowTheirOwnColumns()
    {
        var package = DbpfPackage.CreateNew();
        var small = package.AddResource(OtherKey, new byte[] { 1 })!;
        var big = package.AddResource(OtherKey, new byte[100], rejectDuplicates: false)!;
        package.SetCompression(big, 0x5A42);
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");

        vm.RebuildBrowserList();

        vm.Resources.Single(r => r.Entry == small).MemorySizeText.Should().Be("0x00000001");
        var bigRow = vm.Resources.Single(r => r.Entry == big);
        bigRow.MemorySizeText.Should().Be("0x00000064");
        bigRow.CompressedText.Should().Be("0x5A42");
    }

    #endregion

    #region Formats

    [Fact]
    public void Tgin_RoundTripsLegacyFileNames()
    {
        var tgin = new Tgin(StblType, 0x80000001, 0x00000000DEADBEEF, "a:b-c");
        var name = tgin.ToFileName();
        name.Should().Be("S4_220557DA_80000001_00000000DEADBEEF_a%3ab%2dc%%+STBL.stbl");
        Tgin.Parse("/some/dir/" + name).Should().Be(tgin);

        Tgin.Parse("S4_220557DA_00000000_0000000000000001.bin").Should().Be(new Tgin(StblType, 0, 1));
        Tgin.Parse("random.bin").Should().Be(new Tgin(0, 0, 0));
        Tgin.Parse("0x00000001!0x0000000000000002.trayitem").Key.Should().Be(new ResourceKey(0x2A8A5E22, 1, 2));
        new Tgin(0x12345678, 0, 0).ToFileName().Should().Be("S4_12345678_00000000_0000000000000000%%+UNKN.bnry");
    }

    [Fact]
    public void ClipboardFormat_RoundTripsAndRejectsGarbage()
    {
        IReadOnlyList<CopiedResource> resources =
        [
            new(new Tgin(1, 2, 3, "name"), [1, 2, 3]),
            new(new Tgin(4, 5, 6), []),
        ];

        var back = ResourceClipboardFormat.TryDeserialize(ResourceClipboardFormat.Serialize(resources));

        back.Should().NotBeNull();
        back!.Select(r => r.Tgin).Should().Equal(resources.Select(r => r.Tgin));
        back[0].Data.Should().Equal(1, 2, 3);
        ResourceClipboardFormat.TryDeserialize([1, 2, 3]).Should().BeNull();
        var truncated = ResourceClipboardFormat.Serialize(resources)[..20];
        ResourceClipboardFormat.TryDeserialize(truncated).Should().BeNull();
    }

    #endregion
}
