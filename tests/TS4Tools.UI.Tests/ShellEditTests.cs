using Avalonia.Controls;
using Avalonia.Media.Imaging;
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
/// Tests for the Edit menu preview actions: copy, open in the text editor and edit with the text editor.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1030-1092 (CanCopy, EditCopy),
/// lines 1165-1253 (CanOte, EditOte, p_Exited) and lines 3095-3172 (TextEdit).
/// </remarks>
public sealed class ShellEditTests
{
    private static readonly ResourceKey TextKey = new(0x03B33DDF, 0, 0x3333);
    private static readonly ResourceKey StblKey = new(0x220557DA, 0, 0x1111);

    private sealed class FakeClipboard : IPreviewClipboard
    {
        public List<string> Texts { get; } = [];

        public List<Bitmap> Bitmaps { get; } = [];

        public Task SetTextAsync(string text)
        {
            Texts.Add(text);
            return Task.CompletedTask;
        }

        public Task SetBitmapAsync(Bitmap bitmap)
        {
            Bitmaps.Add(bitmap);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLauncher(Func<string, string, Task>? onRun = null) : IProcessLauncher
    {
        public List<(string Command, string Arguments)> Runs { get; } = [];

        public Task<int> RunAsync(string command, string arguments, CancellationToken cancellationToken = default)
        {
            Runs.Add((command, arguments));
            return onRun == null ? Task.FromResult(0) : onRun(command, arguments).ContinueWith(_ => 0, TaskScheduler.Default);
        }
    }

    private static DbpfPackage CreatePackage()
    {
        var package = DbpfPackage.CreateNew();
        var text = new TextResource(TextKey, ReadOnlyMemory<byte>.Empty) { Text = "line one\nline two" };
        package.AddResource(TextKey, text.Data);
        var stbl = new StblResource(StblKey, ReadOnlyMemory<byte>.Empty);
        stbl.Add(0x10, "Hello");
        package.AddResource(StblKey, stbl.Data);
        return package;
    }

    private static async Task<(MainWindowViewModel Vm, MockFileSystemService Fs, FakeClipboard Clipboard)> CreateAsync(AppSettings? settings = null, IProcessLauncher? launcher = null, FakeDialogService? dialogs = null)
    {
        var fs = new MockFileSystemService();
        var clipboard = new FakeClipboard();
        var vm = new MainWindowViewModel(fs, dialogs ?? new FakeDialogService(), model => new Border { DataContext = model })
        {
            ShellSettings = settings ?? new AppSettings(),
            PreviewClipboard = clipboard,
            ProcessLauncher = launcher ?? new FakeLauncher()
        };
        await vm.AttachPackageAsync(CreatePackage(), "");
        return (vm, fs, clipboard);
    }

    private static async Task SelectAsync(MainWindowViewModel vm, ResourceKey key)
    {
        vm.SelectedResource = vm.Resources.Single(r => r.Key == key);
        await vm.PendingSelectionTask;
    }

    [Fact]
    public async Task CanCopyPreview_OnlyForTextOrImagePreviews()
    {
        var (vm, _, _) = await CreateAsync();
        vm.CanCopyPreview.Should().BeFalse("nothing is selected");

        await SelectAsync(vm, TextKey);
        vm.CanCopyPreview.Should().BeTrue();

        vm.OnAutoModeChanged(0);
        await Task.Delay(100);
        vm.CanCopyPreview.Should().BeFalse("auto off shows no preview");
    }

    [Fact]
    public async Task CopyPreview_CopiesTheSelectedText()
    {
        var (vm, _, clipboard) = await CreateAsync();
        vm.SelectedTextFinder = _ => "two";
        await SelectAsync(vm, TextKey);

        await vm.CopyPreviewCommand.ExecuteAsync(null);

        clipboard.Texts.Should().ContainSingle().Which.TrimEnd().Should().Be("two");
    }

    [Fact]
    public async Task CopyPreview_WithoutSelection_CopiesTheWholePreviewText()
    {
        var (vm, _, clipboard) = await CreateAsync();
        vm.SelectedTextFinder = _ => null;
        await SelectAsync(vm, TextKey);

        await vm.CopyPreviewCommand.ExecuteAsync(null);

        clipboard.Texts.Should().ContainSingle().Which.Should().Contain("line one").And.Contain("line two");
    }

    [Fact]
    public async Task CopyPreview_ForStbl_CopiesKeyValueLines()
    {
        var (vm, _, clipboard) = await CreateAsync();
        vm.SelectedTextFinder = _ => null;
        await SelectAsync(vm, StblKey);

        await vm.CopyPreviewCommand.ExecuteAsync(null);

        clipboard.Texts.Should().ContainSingle().Which.Should().Contain("0x0000000000000010: Hello");
    }

    [Fact]
    public async Task CopyPreview_WithNoPreview_CopiesNothing()
    {
        var (vm, _, clipboard) = await CreateAsync();

        await vm.CopyPreviewCommand.ExecuteAsync(null);

        clipboard.Texts.Should().BeEmpty();
        vm.StatusMessage.Should().Be("Nothing to copy");
    }

    [Fact]
    public async Task OpenInTextEditor_RequiresAConfiguredEditorAndTextPreview()
    {
        var (vm, _, _) = await CreateAsync(new AppSettings { TextEditorCommand = null });
        await SelectAsync(vm, TextKey);
        vm.HasTextEditor.Should().BeFalse();
        vm.CanOpenInTextEditor.Should().BeFalse();

        vm.ShellSettings.TextEditorCommand = "gedit";
        vm.LoadShellSettings();
        vm.CanOpenInTextEditor.Should().BeTrue();
    }

    [Fact]
    public async Task OpenInTextEditor_WritesPreviewTextToATempFile_RunsTheEditorAndCleansUp()
    {
        string? contentSeenByEditor = null;
        MockFileSystemService? fs = null;
        var launcher = new FakeLauncher((_, args) =>
        {
            contentSeenByEditor = fs!.WrittenTextFiles[args.Trim('"')];
            return Task.CompletedTask;
        });
        var (vm, mockFs, _) = await CreateAsync(new AppSettings { TextEditorCommand = "my-editor", TextEditorWantsQuotes = true }, launcher);
        fs = mockFs;
        await SelectAsync(vm, TextKey);

        await vm.OpenInTextEditorCommand.ExecuteAsync(null);
        await vm.LastTextEditorSession;

        launcher.Runs.Should().ContainSingle();
        launcher.Runs[0].Command.Should().Be("my-editor");
        launcher.Runs[0].Arguments.Should().StartWith("\"/tmp/mock").And.EndWith(".txt\"");
        contentSeenByEditor.Should().Contain("line one").And.Contain("line two");
        mockFs.OperationLog.Should().Contain(l => l.StartsWith("DeleteFile", StringComparison.Ordinal) && l.Contains(".txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OpenInTextEditor_EditorFailsToStart_ReportsAndStillCleansUp()
    {
        var launcher = new FakeLauncher((_, _) => throw new InvalidOperationException("no such editor"));
        var (vm, fs, _) = await CreateAsync(new AppSettings { TextEditorCommand = "missing-editor" }, launcher);
        await SelectAsync(vm, TextKey);

        await vm.OpenInTextEditorCommand.ExecuteAsync(null);
        await vm.LastTextEditorSession;

        vm.StatusMessage.Should().Contain("failed to start");
        fs.OperationLog.Should().Contain(l => l.StartsWith("DeleteFile", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditInTextEditor_ImportsTheChangedFile()
    {
        MockFileSystemService? fs = null;
        var launcher = new FakeLauncher(async (_, args) =>
        {
            await Task.Delay(5);
            await fs!.WriteAllBytesAsync(args, "<I n=\"edited\"/>"u8.ToArray());
        });
        var dialogs = new FakeDialogService();
        dialogs.CommitAnswers.Enqueue(CommitPromptResult.Commit);
        var (vm, mockFs, _) = await CreateAsync(new AppSettings { TextEditorCommand = "ed" }, launcher, dialogs);
        fs = mockFs;
        await SelectAsync(vm, TextKey);

        await vm.EditInTextEditorCommand.ExecuteAsync(null);

        // Legacy AfterEdit asks before replacing the resource
        dialogs.CommitPrompts.Should().ContainSingle();
        vm.StatusMessage.Should().Be("Resource updated from text editor");
        vm.IsPackageDirty.Should().BeTrue();
        launcher.Runs.Should().ContainSingle().Which.Command.Should().Be("ed");
    }

    [Fact]
    public async Task EditInTextEditor_CommitDeclined_KeepsTheResource()
    {
        MockFileSystemService? fs = null;
        var launcher = new FakeLauncher(async (_, args) =>
        {
            await Task.Delay(5);
            await fs!.WriteAllBytesAsync(args, "<I n=\"edited\"/>"u8.ToArray());
        });
        var dialogs = new FakeDialogService();
        dialogs.CommitAnswers.Enqueue(CommitPromptResult.Discard);
        var (vm, mockFs, _) = await CreateAsync(new AppSettings { TextEditorCommand = "ed" }, launcher, dialogs);
        fs = mockFs;
        await SelectAsync(vm, TextKey);
        var rows = vm.Resources.Count;

        await vm.EditInTextEditorCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Be("Text editor changes not committed");
        vm.Resources.Should().HaveCount(rows);
        vm.Resources.Should().OnlyContain(r => !r.IsDeleted);
    }

    [Fact]
    public async Task EditInTextEditor_UnchangedFile_LeavesThePackageAlone()
    {
        var (vm, _, _) = await CreateAsync(new AppSettings { TextEditorCommand = "ed" });
        await SelectAsync(vm, TextKey);

        await vm.EditInTextEditorCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Be("Text editor closed (no changes)");
        vm.IsPackageDirty.Should().BeFalse();
    }

    [Fact]
    public void PreviewText_ForTheTextBasedModels()
    {
        MainWindowViewModel.GetPreviewTextFor(null).Should().BeNull();
        MainWindowViewModel.GetPreviewTextFor(new object()).Should().BeNull();

        var hex = new HexViewerViewModel();
        hex.LoadData(new byte[] { 0xAB });
        MainWindowViewModel.GetPreviewTextFor(hex).Should().Contain("AB");
    }
}
