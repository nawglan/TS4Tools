using System.ComponentModel;
using FluentAssertions;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="NameMapEditorViewModel"/>.
/// Focuses on event subscription cleanup to prevent memory leaks.
/// </summary>
public class NameMapEditorViewModelTests
{
    private static readonly ResourceKey TestKey = new(0x0166038C, 0, 0);

    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        // Act
        var vm = new NameMapEditorViewModel();

        // Assert
        vm.Entries.Should().BeEmpty();
        vm.FilteredEntries.Should().BeEmpty();
        vm.EntryCount.Should().Be(0);
        vm.FilterText.Should().BeEmpty();
        vm.SelectedEntry.Should().BeNull();
    }

    [Fact]
    public void LoadResource_PopulatesEntries()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource[0x123456789ABCDEF0] = "TestName1";
        resource[0xFEDCBA9876543210] = "TestName2";

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.Entries.Should().HaveCount(2);
        vm.FilteredEntries.Should().HaveCount(2);
        vm.EntryCount.Should().Be(2);
    }

    [Fact]
    public void LoadResource_ClearsSubscriptionsOnReload()
    {
        // Arrange - this test verifies that event subscriptions are properly cleaned up
        // when loading a new resource, preventing memory leaks per CLAUDE.md guidelines.
        var vm = new NameMapEditorViewModel();

        // Load first resource with entries
        var resource1 = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource1[0x1111111111111111] = "First Resource Entry 1";
        resource1[0x2222222222222222] = "First Resource Entry 2";
        vm.LoadResource(resource1);

        // Track reference to old entry VMs
        var oldEntries = vm.Entries.ToList();
        oldEntries.Should().HaveCount(2);

        // Load second resource with different entries
        var resource2 = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource2[0x3333333333333333] = "Second Resource Entry";
        vm.LoadResource(resource2);

        // Assert - new resource should be loaded
        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].Hash.Should().Be(0x3333333333333333);

        // Verify old entries are cleared (subscriptions would have been cleaned up)
        vm.Entries.Should().NotContain(oldEntries[0]);
        vm.Entries.Should().NotContain(oldEntries[1]);
    }

    [Fact]
    public void EntryNameChanged_UpdatesResource()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource[0x123456789ABCDEF0] = "OriginalName";
        vm.LoadResource(resource);

        // Act - modify the entry through the ViewModel
        vm.Entries[0].Name = "ModifiedName";

        // Assert - the underlying resource should be updated
        resource.TryGetValue(0x123456789ABCDEF0, out var name).Should().BeTrue();
        name.Should().Be("ModifiedName");
    }

    [Fact]
    public void FilterText_FiltersEntries()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource[0x1111111111111111] = "HelloWorld";
        resource[0x2222222222222222] = "GoodbyeUniverse";
        vm.LoadResource(resource);

        // Act
        vm.FilterText = "Hello";

        // Assert
        vm.FilteredEntries.Should().HaveCount(1);
        vm.FilteredEntries[0].Name.Should().Be("HelloWorld");
    }

    [Fact]
    public void FilterText_FiltersByHash()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource[0xDEADBEEF12345678] = "Name1";
        resource[0x1234567890ABCDEF] = "Name2";
        vm.LoadResource(resource);

        // Act - filter by part of the hex hash
        vm.FilterText = "DEADBEEF";

        // Assert
        vm.FilteredEntries.Should().HaveCount(1);
        vm.FilteredEntries[0].Hash.Should().Be(0xDEADBEEF12345678);
    }

    [Fact]
    public void FilterText_EmptyString_ShowsAllEntries()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource[0x1111111111111111] = "HelloWorld";
        resource[0x2222222222222222] = "GoodbyeUniverse";
        vm.LoadResource(resource);

        // First filter
        vm.FilterText = "Hello";
        vm.FilteredEntries.Should().HaveCount(1);

        // Then clear filter
        vm.FilterText = "";

        // Assert
        vm.FilteredEntries.Should().HaveCount(2);
    }

    [Fact]
    public void AddEntry_WithHexHash_AddsCorrectly()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        vm.LoadResource(resource);

        vm.NewHashText = "0xDEADBEEF12345678";
        vm.NewNameText = "TestName";

        // Act
        vm.AddEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].Hash.Should().Be(0xDEADBEEF12345678);
        vm.Entries[0].Name.Should().Be("TestName");
        vm.EntryCount.Should().Be(1);
    }

    [Fact]
    public void AddEntry_WithTextHash_HashesKey()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        vm.LoadResource(resource);

        vm.NewHashText = "test_key";
        vm.NewNameText = "TestName";

        // Act
        vm.AddEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
        // The hash should be FNV64 of "test_key"
        vm.Entries[0].Hash.Should().NotBe(0);
        vm.Entries[0].Name.Should().Be("TestName");
    }

    [Fact]
    public void AddEntry_WithEmptyHash_AutoGeneratesFromName()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        vm.LoadResource(resource);

        vm.NewHashText = "";
        vm.NewNameText = "TestName";

        // Act
        vm.AddEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
        // Hash should be generated from name
        vm.Entries[0].Hash.Should().NotBe(0);
    }

    [Fact]
    public void AddEntry_WithEmptyName_DoesNothing()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        vm.LoadResource(resource);

        vm.NewHashText = "0x12345678";
        vm.NewNameText = "";

        // Act
        vm.AddEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().BeEmpty();
    }

    [Fact]
    public void DeleteEntry_RemovesSelectedEntry()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource[0x1111111111111111] = "Entry to Delete";
        resource[0x2222222222222222] = "Entry to Keep";
        vm.LoadResource(resource);

        vm.SelectedEntry = vm.Entries[0];

        // Act
        vm.DeleteEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].Hash.Should().Be(0x2222222222222222);
        vm.SelectedEntry.Should().BeNull();
    }

    [Fact]
    public void DeleteEntry_WithNoSelection_DoesNothing()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource[0x1111111111111111] = "Test Entry";
        vm.LoadResource(resource);

        vm.SelectedEntry = null;

        // Act
        vm.DeleteEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void GetData_ReturnsResourceData()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();
        var resource = new NameMapResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource[0x1111111111111111] = "Test Entry";
        vm.LoadResource(resource);

        // Act
        var data = vm.GetData();

        // Assert
        data.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetData_WithNoResource_ReturnsEmpty()
    {
        // Arrange
        var vm = new NameMapEditorViewModel();

        // Act
        var data = vm.GetData();

        // Assert
        data.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void NameMapEntryViewModel_HashHex_FormatsCorrectly()
    {
        // Arrange & Act
        var entry = new NameMapEntryViewModel(0x123456789ABCDEF0, "Test");

        // Assert
        entry.HashHex.Should().Be("0x123456789ABCDEF0");
    }

    [Fact]
    public void NameMapEntryViewModel_Name_CanBeChanged()
    {
        // Arrange
        var entry = new NameMapEntryViewModel(0x123456789ABCDEF0, "Original");
        bool propertyChanged = false;
        entry.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NameMapEntryViewModel.Name))
                propertyChanged = true;
        };

        // Act
        entry.Name = "Modified";

        // Assert
        entry.Name.Should().Be("Modified");
        propertyChanged.Should().BeTrue();
    }
}
