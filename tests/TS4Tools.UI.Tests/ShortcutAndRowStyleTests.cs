using Avalonia.Controls;
using Avalonia.Input;
using TS4Tools.Package;
using FluentAssertions;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.Views.Controls;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Menu key gestures and the deleted-row style.
/// Legacy: MainForm.cs lines 1282-1324 (shortcut keys) and BrowserWidget.cs line 713 (strikeout).
/// </summary>
public sealed class ShortcutAndRowStyleTests
{
    private static MainWindowViewModel CreateViewModel() =>
        new(new MockFileSystemService(), new FakeDialogService(), _ => null) { ResourceOpsDialogs = new FakeResourceOpsDialogService() };

    private static async Task<MainWindowViewModel> CreateOpenViewModelAsync()
    {
        var package = DbpfPackage.CreateNew();
        package.AddResource(new ResourceKey(0x220557DA, 0, 1), new byte[] { 1, 2, 3 });
        var vm = CreateViewModel();
        await vm.AttachPackageAsync(package, "");
        vm.SelectedResource = vm.Resources[0];
        await vm.PendingSelectionTask;
        vm.ToggleCompressedCommand.Execute(null); // makes the package dirty so Save is enabled
        return vm;
    }

    [Theory]
    [InlineData(Key.C, KeyModifiers.Control, nameof(MainWindowViewModel.CopyResourceCommand))]
    [InlineData(Key.V, KeyModifiers.Control, nameof(MainWindowViewModel.PasteResourceCommand))]
    [InlineData(Key.D, KeyModifiers.Control, nameof(MainWindowViewModel.DuplicateResourceCommand))]
    [InlineData(Key.A, KeyModifiers.Control, nameof(MainWindowViewModel.SelectAllCommand))]
    [InlineData(Key.C, KeyModifiers.Control | KeyModifiers.Shift, nameof(MainWindowViewModel.CopyResourceKeyCommand))]
    [InlineData(Key.N, KeyModifiers.Control, nameof(MainWindowViewModel.NewPackageCommand))]
    [InlineData(Key.O, KeyModifiers.Control, nameof(MainWindowViewModel.OpenPackageCommand))]
    [InlineData(Key.S, KeyModifiers.Control, nameof(MainWindowViewModel.SaveCommand))]
    [InlineData(Key.S, KeyModifiers.Control | KeyModifiers.Shift, nameof(MainWindowViewModel.SaveAsCommand))]
    [InlineData(Key.F, KeyModifiers.Control, nameof(MainWindowViewModel.SearchCommand))]
    [InlineData(Key.F5, KeyModifiers.None, nameof(MainWindowViewModel.RefreshCommand))]
    [InlineData(Key.M, KeyModifiers.Control | KeyModifiers.Shift, nameof(MainWindowViewModel.ImportResourceCommand))]
    [InlineData(Key.X, KeyModifiers.Control | KeyModifiers.Shift, nameof(MainWindowViewModel.ExportResourceCommand))]
    [InlineData(Key.B, KeyModifiers.Control | KeyModifiers.Shift, nameof(MainWindowViewModel.AddCurrentToBookmarksCommand))]
    public async Task Gesture_ResolvesToMenuCommand(Key key, KeyModifiers modifiers, string commandName)
    {
        var vm = await CreateOpenViewModelAsync();
        var expected = typeof(MainWindowViewModel).GetProperty(commandName)!.GetValue(vm);

        MainWindowShortcuts.Resolve(key, modifiers, vm, null).Should().BeSameAs(expected);
    }

    [Theory]
    [InlineData(Key.C, KeyModifiers.Control)]
    [InlineData(Key.V, KeyModifiers.Control)]
    [InlineData(Key.D, KeyModifiers.Control)]
    [InlineData(Key.A, KeyModifiers.Control)]
    public async Task BrowserShortcuts_DoNotFireInTextInput(Key key, KeyModifiers modifiers)
    {
        var vm = await CreateOpenViewModelAsync();

        MainWindowShortcuts.Resolve(key, modifiers, vm, new TextBox()).Should().BeNull();
        MainWindowShortcuts.Resolve(key, modifiers, vm, new DataGrid()).Should().NotBeNull();
    }

    [Fact]
    public async Task GlobalShortcuts_StillFireInTextInput()
    {
        var vm = await CreateOpenViewModelAsync();

        MainWindowShortcuts.Resolve(Key.S, KeyModifiers.Control, vm, new TextBox()).Should().BeSameAs(vm.SaveCommand);
    }

    [Fact]
    public void UnboundKeys_ResolveToNothing()
    {
        var vm = CreateViewModel();

        MainWindowShortcuts.Resolve(Key.Q, KeyModifiers.Control, vm, null).Should().BeNull();
        MainWindowShortcuts.Resolve(Key.C, KeyModifiers.None, vm, null).Should().BeNull();
    }

    [Fact]
    public async Task TryExecute_SkipsDisabledCommands()
    {
        var vm = CreateViewModel();

        // No package is open, so Save cannot execute and the key stays unhandled
        (await MainWindowShortcuts.TryExecuteAsync(Key.S, KeyModifiers.Control, vm, null)).Should().BeFalse();
    }

    [Fact]
    public void DeletedRow_GetsDeletedClass_AndClearsIt()
    {
        var row = new Border();

        DeletedRowStyler.Apply(row, true);
        row.Classes.Should().Contain(DeletedRowStyler.DeletedClass);

        DeletedRowStyler.Apply(row, false);
        row.Classes.Should().NotContain(DeletedRowStyler.DeletedClass);
    }
}
