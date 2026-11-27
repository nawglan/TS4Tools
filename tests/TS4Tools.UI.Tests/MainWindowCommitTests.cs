using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests that editor changes are committed back into the package and that commit/save
/// prompts follow legacy s4pe behaviour.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/MainForm.cs
/// lines 3060-3073 (Commit replaces the resource and marks the package dirty),
/// lines 2609-2642 (Commit changes? Yes/No/Cancel on selection change),
/// lines 628-683 (Save now? Yes/No/Cancel when the package is closed),
/// lines 867-901 (Save As).
/// </remarks>
public sealed class MainWindowCommitTests : IDisposable
{
    private static readonly ResourceKey StblKey = new(0x220557DA, 0, 0x1111);
    private static readonly ResourceKey NameMapKey = new(0x0166038C, 0, 0x2222);
    private static readonly ResourceKey TextKey = new(0x03B33DDF, 0, 0x3333);

    private readonly List<string> _tempFiles = [];
    private readonly FakeDialogService _dialogs = new();
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
        var path = Path.Combine(Path.GetTempPath(), $"ts4tools-ui-test-{Guid.NewGuid():N}.package");
        _tempFiles.Add(path);
        return path;
    }

    private static DbpfPackage CreatePackage()
    {
        var package = DbpfPackage.CreateNew();

        var stbl = new StblResource(StblKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(0x00000001, "Hello");
        package.AddResource(StblKey, stbl.Data);

        var nameMap = new NameMapResource(NameMapKey, ReadOnlyMemory<byte>.Empty);
        nameMap.Add(0x0000000000000042, "original_name");
        package.AddResource(NameMapKey, nameMap.Data);

        var text = new TextResource(TextKey, ReadOnlyMemory<byte>.Empty) { Text = "<I n=\"original\"/>" };
        package.AddResource(TextKey, text.Data);

        return package;
    }

    private MainWindowViewModel CreateViewModel()
        => new(_fileSystem, _dialogs, _ => null);

    private static async Task<MainWindowViewModel> AttachAsync(MainWindowViewModel vm, DbpfPackage package, string path = "")
    {
        await vm.AttachPackageAsync(package, path);
        return vm;
    }

    private static async Task SelectAsync(MainWindowViewModel vm, ResourceKey key)
    {
        vm.SelectedResource = vm.Resources.Single(r => r.Key == key);
        await vm.PendingSelectionTask;
    }

    private static async Task<DbpfPackage> RoundTripAsync(DbpfPackage package)
    {
        var ms = new MemoryStream();
        await package.SaveToStreamAsync(ms);
        ms.Position = 0;
        return await DbpfPackage.OpenAsync(ms);
    }

    private static async Task<T> ReadAsync<T>(DbpfPackage package, ResourceKey key, Func<ResourceKey, ReadOnlyMemory<byte>, T> factory)
    {
        var entry = package.Find(key);
        entry.Should().NotBeNull();
        var data = await package.GetResourceDataAsync(entry!);
        return factory(key, data);
    }

    private static void EditStbl(MainWindowViewModel vm, string value)
    {
        var editor = vm.CurrentEditorViewModel.Should().BeOfType<StblEditorViewModel>().Subject;
        editor.Entries.Single().Value = value;
    }

    #region Commit persists edits

    [Fact]
    public async Task Commit_StblEdit_IsPersistedInPackage()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, StblKey);

        EditStbl(vm, "World");

        vm.IsResourceDirty.Should().BeTrue();
        vm.CommitCommand.CanExecute(null).Should().BeTrue();

        vm.CommitCommand.Execute(null);

        vm.IsResourceDirty.Should().BeFalse();
        vm.IsPackageDirty.Should().BeTrue();

        await using var reopened = await RoundTripAsync(package);
        var stbl = await ReadAsync(reopened, StblKey, (k, d) => new StblResource(k, d));
        stbl[0x00000001].Should().Be("World");
    }

    [Fact]
    public async Task Commit_NameMapAddedEntry_IsPersistedInPackage()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, NameMapKey);

        var editor = vm.CurrentEditorViewModel.Should().BeOfType<NameMapEditorViewModel>().Subject;
        editor.NewHashText = "0x0000000000000099";
        editor.NewNameText = "added_name";
        editor.AddEntryCommand.Execute(null);
        editor.Entries.Single(e => e.Hash == 0x42).Name = "renamed";

        vm.IsResourceDirty.Should().BeTrue();
        vm.CommitCommand.Execute(null);

        await using var reopened = await RoundTripAsync(package);
        var nameMap = await ReadAsync(reopened, NameMapKey, (k, d) => new NameMapResource(k, d));
        nameMap[0x99].Should().Be("added_name");
        nameMap[0x42].Should().Be("renamed");
    }

    [Fact]
    public async Task Commit_TextEditWithoutApply_IsPersistedInPackage()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, TextKey);

        var editor = vm.CurrentEditorViewModel.Should().BeOfType<TextEditorViewModel>().Subject;
        editor.Text = "<I n=\"edited\"/>";

        vm.IsResourceDirty.Should().BeTrue();
        vm.CommitCommand.Execute(null);

        await using var reopened = await RoundTripAsync(package);
        var text = await ReadAsync(reopened, TextKey, (k, d) => new TextResource(k, d));
        text.Text.Should().Be("<I n=\"edited\"/>");
    }

    [Fact]
    public async Task Commit_PropertyGridEdit_MarksDirtyAndIsPersisted()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, TextKey);
        vm.TogglePropertyGridCommand.Execute(null);

        var grid = vm.CurrentPropertyGrid;
        grid.Should().NotBeNull();
        grid!.Properties.Single(p => p.Name == nameof(TextResource.Text)).Value = "<I n=\"from-grid\"/>";

        vm.IsResourceDirty.Should().BeTrue();
        vm.CommitCommand.Execute(null);

        await using var reopened = await RoundTripAsync(package);
        var text = await ReadAsync(reopened, TextKey, (k, d) => new TextResource(k, d));
        text.Text.Should().Be("<I n=\"from-grid\"/>");
    }

    [Fact]
    public async Task Commit_UpdatesListItemSize()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, StblKey);
        var item = vm.SelectedResource!;

        EditStbl(vm, new string('x', 500));
        vm.CommitCommand.Execute(null);

        var entry = package.Find(StblKey)!;
        item.FileSize.Should().Be(entry.MemorySize);
        item.FileSize.Should().BeGreaterThan(500);
    }

    [Fact]
    public async Task Commit_WithoutEdits_CannotExecute()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await SelectAsync(vm, StblKey);

        vm.IsResourceDirty.Should().BeFalse();
        vm.CommitCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task Edit_WithoutCommit_DoesNotChangePackage()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, StblKey);

        EditStbl(vm, "Uncommitted");

        vm.IsPackageDirty.Should().BeFalse();
        var stbl = await ReadAsync(package, StblKey, (k, d) => new StblResource(k, d));
        stbl[0x00000001].Should().Be("Hello");
    }

    #endregion

    #region Selection change prompts

    [Fact]
    public async Task SelectionChange_WithoutEdits_DoesNotPrompt()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await SelectAsync(vm, StblKey);
        await SelectAsync(vm, TextKey);

        _dialogs.CommitPrompts.Should().BeEmpty();
        vm.CurrentEditorViewModel.Should().BeOfType<TextEditorViewModel>();
    }

    [Fact]
    public async Task SelectionChange_WithEdits_Commit_CommitsAndMovesOn()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "Committed on switch");

        _dialogs.CommitAnswers.Enqueue(CommitPromptResult.Commit);
        await SelectAsync(vm, TextKey);

        _dialogs.CommitPrompts.Should().ContainSingle()
            .Which.Should().Be($"TGI {StblKey.ResourceType:X8}-{StblKey.ResourceGroup:X8}-{StblKey.Instance:X16}");
        vm.SelectedResource!.Key.Should().Be(TextKey);
        vm.CurrentEditorViewModel.Should().BeOfType<TextEditorViewModel>();
        vm.IsResourceDirty.Should().BeFalse();
        vm.IsPackageDirty.Should().BeTrue();

        var stbl = await ReadAsync(package, StblKey, (k, d) => new StblResource(k, d));
        stbl[0x00000001].Should().Be("Committed on switch");
    }

    [Fact]
    public async Task SelectionChange_WithEdits_Discard_DropsEditsAndMovesOn()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "Discarded");

        _dialogs.CommitAnswers.Enqueue(CommitPromptResult.Discard);
        await SelectAsync(vm, TextKey);

        vm.SelectedResource!.Key.Should().Be(TextKey);
        vm.IsResourceDirty.Should().BeFalse();
        vm.IsPackageDirty.Should().BeFalse();

        var stbl = await ReadAsync(package, StblKey, (k, d) => new StblResource(k, d));
        stbl[0x00000001].Should().Be("Hello");
    }

    [Fact]
    public async Task SelectionChange_WithEdits_Cancel_KeepsSelectionAndEdits()
    {
        var package = CreatePackage();
        var vm = await AttachAsync(CreateViewModel(), package);
        await SelectAsync(vm, StblKey);
        var stblItem = vm.SelectedResource;
        var stblEditor = vm.CurrentEditorViewModel;
        EditStbl(vm, "Still editing");

        _dialogs.CommitAnswers.Enqueue(CommitPromptResult.Cancel);
        await SelectAsync(vm, TextKey);

        vm.SelectedResource.Should().BeSameAs(stblItem);
        vm.CurrentEditorViewModel.Should().BeSameAs(stblEditor);
        vm.IsResourceDirty.Should().BeTrue();

        // The edit can still be committed afterwards.
        vm.CommitCommand.Execute(null);
        var stbl = await ReadAsync(package, StblKey, (k, d) => new StblResource(k, d));
        stbl[0x00000001].Should().Be("Still editing");
    }

    #endregion

    #region Close / exit prompts

    [Fact]
    public async Task ConfirmClose_CleanPackage_DoesNotPrompt()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());

        (await vm.ConfirmClosePackageAsync()).Should().BeTrue();

        _dialogs.CommitPrompts.Should().BeEmpty();
        _dialogs.SavePromptCount.Should().Be(0);
    }

    [Fact]
    public async Task ClosePackage_DirtyPackage_Cancel_KeepsPackageOpen()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "x");
        vm.CommitCommand.Execute(null);

        _dialogs.SaveAnswers.Enqueue(SavePromptResult.Cancel);
        await vm.ClosePackageCommand.ExecuteAsync(null);

        _dialogs.SavePromptCount.Should().Be(1);
        vm.HasOpenPackage.Should().BeTrue();
        vm.IsPackageDirty.Should().BeTrue();
    }

    [Fact]
    public async Task ClosePackage_DirtyPackage_DontSave_Closes()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "x");
        vm.CommitCommand.Execute(null);

        _dialogs.SaveAnswers.Enqueue(SavePromptResult.DontSave);
        await vm.ClosePackageCommand.ExecuteAsync(null);

        vm.HasOpenPackage.Should().BeFalse();
        vm.HasUnsavedChanges.Should().BeFalse();
        vm.Title.Should().Be("TS4Tools");
    }

    [Fact]
    public async Task ClosePackage_UncommittedEdit_CancelOnCommitPrompt_KeepsPackageOpen()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "x");

        _dialogs.CommitAnswers.Enqueue(CommitPromptResult.Cancel);
        await vm.ClosePackageCommand.ExecuteAsync(null);

        _dialogs.SavePromptCount.Should().Be(0);
        vm.HasOpenPackage.Should().BeTrue();
        vm.IsResourceDirty.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmClose_UncommittedEditOnNewPackage_CommitThenSave_WritesFileViaSaveAs()
    {
        var path = NewTempPath();
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "Saved on exit");

        _dialogs.CommitAnswers.Enqueue(CommitPromptResult.Commit);
        _dialogs.SaveAnswers.Enqueue(SavePromptResult.Save);
        _dialogs.SavePaths.Enqueue(path);

        (await vm.ConfirmClosePackageAsync()).Should().BeTrue();

        _dialogs.SavePathRequests.Should().ContainSingle();
        await using var saved = await DbpfPackage.OpenAsync(path);
        var stbl = await ReadAsync(saved, StblKey, (k, d) => new StblResource(k, d));
        stbl[0x00000001].Should().Be("Saved on exit");
    }

    [Fact]
    public async Task ConfirmClose_SaveAsCancelled_ReturnsFalse()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await vm.NewPackageCommand.ExecuteAsync(null); // new packages start dirty (legacy FileNew)

        _dialogs.SaveAnswers.Enqueue(SavePromptResult.Save);
        _dialogs.SavePaths.Enqueue(null);

        (await vm.ConfirmClosePackageAsync()).Should().BeFalse();
        vm.HasOpenPackage.Should().BeTrue();
    }

    #endregion

    #region Save

    [Fact]
    public async Task Save_OpenedFile_CommitsPendingEditAndWritesToDisk()
    {
        var path = NewTempPath();
        await using (var initial = CreatePackage())
        {
            await initial.SaveAsAsync(path);
        }
        _fileSystem.SetupFile(path, []); // make the recent-file existence check pass

        var vm = CreateViewModel();
        await vm.OpenRecentFileCommand.ExecuteAsync(path);
        vm.HasOpenPackage.Should().BeTrue();
        vm.IsReadOnly.Should().BeFalse();

        await SelectAsync(vm, StblKey);
        EditStbl(vm, "Saved to disk");

        _dialogs.CommitAnswers.Enqueue(CommitPromptResult.Commit);
        await vm.SaveCommand.ExecuteAsync(null);

        vm.IsResourceDirty.Should().BeFalse();
        vm.IsPackageDirty.Should().BeFalse();
        vm.Title.Should().NotContain("*");
        await vm.DisposeAsync();

        await using var reopened = await DbpfPackage.OpenAsync(path);
        var stbl = await ReadAsync(reopened, StblKey, (k, d) => new StblResource(k, d));
        stbl[0x00000001].Should().Be("Saved to disk");
    }

    [Fact]
    public async Task SaveAs_NewPackage_ReopensFromNewFile()
    {
        var path = NewTempPath();
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "first");
        vm.CommitCommand.Execute(null);

        _dialogs.SavePaths.Enqueue(path);
        await vm.SaveAsCommand.ExecuteAsync(null);

        vm.PackagePath.Should().Be(path);
        vm.IsPackageDirty.Should().BeFalse();

        // A later edit + Save must go to the new file.
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "second");
        vm.CommitCommand.Execute(null);
        await vm.SaveCommand.ExecuteAsync(null);
        await vm.DisposeAsync();

        await using var reopened = await DbpfPackage.OpenAsync(path);
        var stbl = await ReadAsync(reopened, StblKey, (k, d) => new StblResource(k, d));
        stbl[0x00000001].Should().Be("second");
    }

    [Fact]
    public async Task OpenReadOnly_Edits_AreNotCommittable()
    {
        var path = NewTempPath();
        await using (var initial = CreatePackage())
        {
            await initial.SaveAsAsync(path);
        }
        _fileSystem.SetupFile(path, []);

        var vm = CreateViewModel();
        await vm.OpenBookmarkCommand.ExecuteAsync("0:" + path);
        vm.IsReadOnly.Should().BeTrue();
        vm.Title.Should().Contain("[Read Only]");

        await SelectAsync(vm, StblKey);
        EditStbl(vm, "nope");

        vm.IsResourceDirty.Should().BeFalse();
        vm.HasUnsavedChanges.Should().BeFalse();
        await vm.DisposeAsync();
    }

    #endregion

    #region Dirty tracking

    [Fact]
    public async Task AttachedPackage_StartsClean()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage(), "/mods/test.package");

        vm.IsPackageDirty.Should().BeFalse();
        vm.HasUnsavedChanges.Should().BeFalse();
        vm.CanSave.Should().BeFalse();
        vm.Title.Should().Be("TS4Tools - test.package");
    }

    [Fact]
    public async Task Commit_MarksPackageDirtyAndTitle()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage(), "/mods/test.package");
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "x");

        vm.HasUnsavedChanges.Should().BeTrue();
        vm.IsPackageDirty.Should().BeFalse();

        vm.CommitCommand.Execute(null);

        vm.IsPackageDirty.Should().BeTrue();
        vm.CanSave.Should().BeTrue();
        vm.Title.Should().Be("TS4Tools - test.package*");
    }

    [Fact]
    public async Task ToggleDeleted_MarksPackageDirty_WithoutCommitPrompt()
    {
        // Legacy ResourceIsDeleted (MainForm.cs lines 1574-1582) only flags the entry: it stays listed.
        var vm = await AttachAsync(CreateViewModel(), CreatePackage(), "/mods/test.package");
        await SelectAsync(vm, StblKey);
        EditStbl(vm, "x");

        vm.ToggleDeletedCommand.Execute(null);
        await vm.PendingSelectionTask;

        _dialogs.CommitPrompts.Should().BeEmpty();
        vm.IsPackageDirty.Should().BeTrue();
        vm.Resources.Should().ContainSingle(r => r.Key == StblKey).Which.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task NewPackage_IsDirty()
    {
        var vm = CreateViewModel();

        await vm.NewPackageCommand.ExecuteAsync(null);

        vm.IsPackageDirty.Should().BeTrue();
        vm.Title.Should().Be("TS4Tools - [New Package]*");
    }

    [Fact]
    public async Task PropertyChanged_IsRaisedForDirtyFlags()
    {
        var vm = await AttachAsync(CreateViewModel(), CreatePackage());
        await SelectAsync(vm, StblKey);
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        EditStbl(vm, "x");
        vm.CommitCommand.Execute(null);

        raised.Should().Contain(nameof(MainWindowViewModel.IsResourceDirty));
        raised.Should().Contain(nameof(MainWindowViewModel.IsPackageDirty));
        raised.Should().Contain(nameof(MainWindowViewModel.Title));
    }

    #endregion
}
