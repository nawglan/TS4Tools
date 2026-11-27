using Avalonia.Controls;
using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// After a helper modifies the resource, the user is asked to commit the changes.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3147-3223
/// (do_HelperClick and AfterEdit).
/// </remarks>
public sealed class HelperCommitPromptTests
{
    private static readonly ResourceKey Key = new(0x00AA0001, 0, 0x42);

    private sealed class ReturningClipboard(byte[]? result) : IHelperClipboard
    {
        public Task SetResourceAsync(byte[] data) => Task.CompletedTask;

        public Task<byte[]?> TryGetResourceAsync() => Task.FromResult(result);
    }

    private static HelperInstance ClipboardHelper() => new(
        new HelperDefinition { Id = "test", Label = "Test helper", Command = "sh", Arguments = "-c \"exit 0\"" },
        Key.ResourceType, 0, 1);

    private static async Task<(MainWindowViewModel Vm, DbpfPackage Package, FakeDialogService Dialogs)> CreateAsync(byte[]? helperResult)
    {
        var dialogs = new FakeDialogService();
        var package = DbpfPackage.CreateNew();
        var entry = package.AddResource(Key, new byte[] { 1, 2, 3 })!;
        package.SetCompression(entry, 0x5A42);
        var vm = new MainWindowViewModel(new MockFileSystemService(), dialogs, model => new Border { DataContext = model })
        {
            HelperClipboard = new ReturningClipboard(helperResult)
        };
        await vm.AttachPackageAsync(package, "");
        vm.SelectedResource = vm.Resources[0];
        await vm.PendingSelectionTask;
        return (vm, package, dialogs);
    }

    [Fact]
    public async Task ModifiedByHelper_Commit_ReplacesTheResourceLikeLegacy()
    {
        var (vm, package, dialogs) = await CreateAsync([9, 9]);
        dialogs.CommitAnswers.Enqueue(CommitPromptResult.Commit);

        await vm.ExecuteHelperAsync(ClipboardHelper());

        dialogs.CommitPrompts.Should().ContainSingle();
        // Legacy NewResource(Replace): the old entry is flagged deleted and a new, compressed one added and selected
        package.Resources.Should().HaveCount(2);
        package.Resources[0].IsDeleted.Should().BeTrue();
        var live = package.FindAll(e => e.Key == Key).Single();
        (await package.GetResourceDataAsync(live)).ToArray().Should().Equal(9, 9);
        live.CompressionType.Should().Be(0x5A42);
        vm.SelectedResource!.Entry.Should().BeSameAs(live);
        vm.StatusMessage.Should().Contain("Test helper");
    }

    [Fact]
    public async Task ModifiedByHelper_Declined_ChangesNothing()
    {
        var (vm, package, dialogs) = await CreateAsync([9, 9]);
        dialogs.CommitAnswers.Enqueue(CommitPromptResult.Discard);

        await vm.ExecuteHelperAsync(ClipboardHelper());

        dialogs.CommitPrompts.Should().ContainSingle();
        package.Resources.Should().ContainSingle();
        (await package.GetResourceDataAsync(package.Resources[0])).ToArray().Should().Equal(1, 2, 3);
        vm.StatusMessage.Should().Contain("not committed");
    }

    [Fact]
    public async Task HelperLeavingNothing_DoesNotAsk()
    {
        var (vm, package, dialogs) = await CreateAsync(null);

        await vm.ExecuteHelperAsync(ClipboardHelper());

        dialogs.CommitPrompts.Should().BeEmpty();
        package.Resources.Should().ContainSingle();
    }
}
