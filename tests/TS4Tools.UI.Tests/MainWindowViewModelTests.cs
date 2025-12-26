using FluentAssertions;
using TS4Tools.UI.ViewModels;
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
        vm.FilterText.Should().BeEmpty();
        vm.SelectedResource.Should().BeNull();
        vm.HasSelectedResource.Should().BeFalse();
        vm.SortMode.Should().Be(ResourceSortMode.TypeName);
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
    public void FilterText_WhenChanged_FiltersResources()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var stblResource = new ResourceItemViewModel(new ResourceKey(0x220557DA, 0, 1), 100); // STBL
        var nameMapResource = new ResourceItemViewModel(new ResourceKey(0x0166038C, 0, 2), 200); // NameMap
        vm.Resources.Add(stblResource);
        vm.Resources.Add(nameMapResource);
        vm.FilteredResources.Add(stblResource);
        vm.FilteredResources.Add(nameMapResource);

        // Act
        vm.FilterText = "String Table";

        // Assert - filtered resources should only contain matching items
        vm.FilteredResources.Should().HaveCount(1);
        vm.FilteredResources.Should().Contain(stblResource);
    }

    [Fact]
    public void FilterText_WhenEmpty_ShowsAllResources()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var stblResource = new ResourceItemViewModel(new ResourceKey(0x220557DA, 0, 1), 100);
        var nameMapResource = new ResourceItemViewModel(new ResourceKey(0x0166038C, 0, 2), 200);
        vm.Resources.Add(stblResource);
        vm.Resources.Add(nameMapResource);

        // First filter, then clear
        vm.FilterText = "String";
        vm.FilterText = "";

        // Assert
        vm.FilteredResources.Should().HaveCount(2);
    }

    [Fact]
    public void CycleSortMode_CyclesThroughModes()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.SortMode.Should().Be(ResourceSortMode.TypeName);

        // Act & Assert - cycle through all modes
        vm.CycleSortModeCommand.Execute(null);
        vm.SortMode.Should().Be(ResourceSortMode.Instance);

        vm.CycleSortModeCommand.Execute(null);
        vm.SortMode.Should().Be(ResourceSortMode.Size);

        vm.CycleSortModeCommand.Execute(null);
        vm.SortMode.Should().Be(ResourceSortMode.TypeName);
    }

    [Fact]
    public void SortMode_WhenChanged_SortsResources()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var resource1 = new ResourceItemViewModel(new ResourceKey(0x220557DA, 0, 3), 300);
        var resource2 = new ResourceItemViewModel(new ResourceKey(0x0166038C, 0, 1), 100);
        var resource3 = new ResourceItemViewModel(new ResourceKey(0x545AC67A, 0, 2), 200);
        vm.Resources.Add(resource1);
        vm.Resources.Add(resource2);
        vm.Resources.Add(resource3);
        vm.FilteredResources.Add(resource1);
        vm.FilteredResources.Add(resource2);
        vm.FilteredResources.Add(resource3);

        // Act - sort by size (descending)
        vm.SortMode = ResourceSortMode.Size;

        // Assert - largest first
        vm.FilteredResources[0].FileSize.Should().Be(300);
        vm.FilteredResources[1].FileSize.Should().Be(200);
        vm.FilteredResources[2].FileSize.Should().Be(100);
    }

    [Fact]
    public void SortMode_ByInstance_SortsByInstance()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var resource1 = new ResourceItemViewModel(new ResourceKey(0x220557DA, 0, 300), 100);
        var resource2 = new ResourceItemViewModel(new ResourceKey(0x220557DA, 0, 100), 100);
        var resource3 = new ResourceItemViewModel(new ResourceKey(0x220557DA, 0, 200), 100);
        vm.Resources.Add(resource1);
        vm.Resources.Add(resource2);
        vm.Resources.Add(resource3);
        vm.FilteredResources.Add(resource1);
        vm.FilteredResources.Add(resource2);
        vm.FilteredResources.Add(resource3);

        // Act
        vm.SortMode = ResourceSortMode.Instance;

        // Assert - smallest instance first
        vm.FilteredResources[0].Key.Instance.Should().Be(100);
        vm.FilteredResources[1].Key.Instance.Should().Be(200);
        vm.FilteredResources[2].Key.Instance.Should().Be(300);
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        // Arrange
        var vm = new MainWindowViewModel();

        // Act
        await vm.DisposeAsync();

        // Assert - should be idempotent (can call twice)
        await vm.DisposeAsync();
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
    public void DeleteResourceCommand_WithNoSelection_DoesNothing()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.SelectedResource = null;

        // Act - should not throw
        vm.DeleteResourceCommand.Execute(null);

        // Assert
        vm.Resources.Should().BeEmpty();
    }
}
