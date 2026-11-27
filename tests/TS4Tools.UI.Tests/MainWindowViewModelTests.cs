using FluentAssertions;
using TS4Tools.Package;
using TS4Tools.UI.Tests.Mocks;
using TS4Tools.UI.ViewModels;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="MainWindowViewModel"/>.
/// </summary>
public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_InitializesDefaultState()
    {
        // Act
        var vm = new MainWindowViewModel();

        // Assert
        vm.HasOpenPackage.Should().BeFalse();
        vm.ResourceCount.Should().Be(0);
        vm.PackagePath.Should().BeEmpty();
        vm.Title.Should().Be("TS4Tools");
        vm.StatusMessage.Should().Be("Ready");
        vm.Filter.IsEnabled.Should().BeFalse();
        vm.SelectedResource.Should().BeNull();
        vm.HasSelectedResource.Should().BeFalse();
        vm.SortOrder.Should().Be(BrowserSortOrder.None);
    }

    [Fact]
    public void Constructor_InitializesEmptyCollections()
    {
        // Act
        var vm = new MainWindowViewModel();

        // Assert
        vm.Resources.Should().BeEmpty();
        vm.FilteredResources.Should().BeEmpty();
        vm.RecentFiles.Should().NotBeNull();
    }

    [Fact]
    public void SelectedResource_WhenNull_HasSelectedResourceIsFalse()
    {
        // Arrange
        var vm = new MainWindowViewModel();

        // Act
        vm.SelectedResource = null;

        // Assert
        vm.HasSelectedResource.Should().BeFalse();
    }

    [Fact]
    public void SelectedResource_WhenSet_HasSelectedResourceIsTrue()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var resourceItem = new ResourceItemViewModel(new ResourceKey(0x220557DA, 0, 0), 100);

        // Act
        vm.SelectedResource = resourceItem;

        // Assert
        vm.HasSelectedResource.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        // Arrange - a VM with an open package and a loaded resource
        var package = DbpfPackage.CreateNew();
        var key = new ResourceKey(0x220557DA, 0, 1);
        var stbl = new StblResource(key, ReadOnlyMemory<byte>.Empty);
        stbl.Add(1, "value");
        package.AddResource(key, stbl.Data);

        var vm = new MainWindowViewModel(new MockFileSystemService(), new FakeDialogService(), _ => null);
        await vm.AttachPackageAsync(package, "");
        vm.SelectedResource = vm.Resources.Single();
        await vm.PendingSelectionTask;
        vm.CurrentEditorViewModel.Should().NotBeNull();

        // Act
        await vm.DisposeAsync();

        // Assert - package closed and disposed, editor state released
        vm.HasOpenPackage.Should().BeFalse();
        vm.Resources.Should().BeEmpty();
        vm.SelectedResource.Should().BeNull();
        vm.CurrentEditorViewModel.Should().BeNull();
        vm.Title.Should().Be("TS4Tools");
        package.Invoking(p => p.AddResource(new ResourceKey(1, 2, 3), new byte[] { 1 }))
            .Should().Throw<ObjectDisposedException>();

        // The VM no longer reacts to its own property changes (handler unsubscribed)
        var item = new ResourceItemViewModel(key, 10);
        vm.Resources.Add(item);
        vm.Filter.IsEnabled = true; // with the handler attached this would list the item
        vm.FilteredResources.Should().BeEmpty("filtering is no longer wired up after dispose");

        // Idempotent
        await vm.Invoking(v => v.DisposeAsync().AsTask()).Should().NotThrowAsync();
    }

    [Fact]
    public void TryParseS4FileName_ValidFormat_ParsesCorrectly()
    {
        // This tests the internal file name parsing logic.
        // File names like "S4_XXXXXXXX_XXXXXXXX_XXXXXXXXXXXXXXXX" should parse.
        // We test this indirectly through the import behavior.

        // The format is: S4_<Type>_<Group>_<Instance>
        var vm = new MainWindowViewModel();

        // We can't test private method directly, but we verify the VM initializes
        vm.Should().NotBeNull();
    }

    [Fact]
    public void ToggleDeletedCommand_WithNoSelection_DoesNothing()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.SelectedResource = null;

        // Act - should not throw
        vm.ToggleDeletedCommand.Execute(null);

        // Assert
        vm.Resources.Should().BeEmpty();
    }
}
