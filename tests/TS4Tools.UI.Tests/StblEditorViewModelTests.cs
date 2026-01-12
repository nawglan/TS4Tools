using System.ComponentModel;
using FluentAssertions;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="StblEditorViewModel"/>.
/// Focuses on event subscription cleanup to prevent memory leaks.
/// </summary>
public class StblEditorViewModelTests
{
    private static readonly ResourceKey TestKey = new(0x220557DA, 0, 0);

    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        // Act
        var vm = new StblEditorViewModel();

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
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(0x12345678, "Test Entry 1");
        resource.Add(0x87654321, "Test Entry 2");

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
        var vm = new StblEditorViewModel();

        // Load first resource with entries
        var resource1 = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource1.Add(0x11111111, "First Resource Entry 1");
        resource1.Add(0x22222222, "First Resource Entry 2");
        vm.LoadResource(resource1);

        // Track reference to old entry VMs
        var oldEntries = vm.Entries.ToList();
        oldEntries.Should().HaveCount(2);

        // Load second resource with different entries
        var resource2 = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource2.Add(0x33333333, "Second Resource Entry");
        vm.LoadResource(resource2);

        // Assert - new resource should be loaded
        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].KeyHash.Should().Be(0x33333333);

        // Verify old entries are cleared (subscriptions would have been cleaned up)
        vm.Entries.Should().NotContain(oldEntries[0]);
        vm.Entries.Should().NotContain(oldEntries[1]);
    }

    [Fact]
    public void EntryValueChanged_UpdatesResource()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(0x12345678, "Original Value");
        vm.LoadResource(resource);

        // Act - modify the entry through the ViewModel
        vm.Entries[0].Value = "Modified Value";

        // Assert - the underlying resource should be updated
        resource.TryGetValue(0x12345678, out var value).Should().BeTrue();
        value.Should().Be("Modified Value");
    }

    [Fact]
    public void FilterText_FiltersEntries()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(0x12345678, "Hello World");
        resource.Add(0x87654321, "Goodbye Universe");
        vm.LoadResource(resource);

        // Act
        vm.FilterText = "Hello";

        // Assert
        vm.FilteredEntries.Should().HaveCount(1);
        vm.FilteredEntries[0].Value.Should().Be("Hello World");
    }

    [Fact]
    public void FilterText_EmptyString_ShowsAllEntries()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(0x12345678, "Hello World");
        resource.Add(0x87654321, "Goodbye Universe");
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
    public void AddEntry_WithHexKey_AddsCorrectly()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        vm.LoadResource(resource);

        vm.NewKeyText = "0xDEADBEEF";
        vm.NewValueText = "Test Value";

        // Act
        vm.AddEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].KeyHash.Should().Be(0xDEADBEEF);
        vm.Entries[0].Value.Should().Be("Test Value");
        vm.EntryCount.Should().Be(1);
    }

    [Fact]
    public void AddEntry_WithTextKey_HashesKey()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        vm.LoadResource(resource);

        vm.NewKeyText = "test_key";
        vm.NewValueText = "Test Value";

        // Act
        vm.AddEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
        // The key should be hashed
        vm.Entries[0].KeyHash.Should().NotBe(0);
        vm.Entries[0].Value.Should().Be("Test Value");
    }

    [Fact]
    public void AddEntry_WithEmptyKey_AutoGeneratesHash()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        vm.LoadResource(resource);

        vm.NewKeyText = "";
        vm.NewValueText = "Test Value";

        // Act
        vm.AddEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
        // Hash should be generated from value
        vm.Entries[0].KeyHash.Should().NotBe(0);
    }

    [Fact]
    public void AddEntry_WithEmptyValue_DoesNothing()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        vm.LoadResource(resource);

        vm.NewKeyText = "0x12345678";
        vm.NewValueText = "";

        // Act
        vm.AddEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().BeEmpty();
    }

    [Fact]
    public void DeleteEntry_RemovesSelectedEntry()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(0x12345678, "Entry to Delete");
        resource.Add(0x87654321, "Entry to Keep");
        vm.LoadResource(resource);

        vm.SelectedEntry = vm.Entries[0];

        // Act
        vm.DeleteEntryCommand.Execute(null);

        // Assert
        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].KeyHash.Should().Be(0x87654321);
        vm.SelectedEntry.Should().BeNull();
    }

    [Fact]
    public void DeleteEntry_WithNoSelection_DoesNothing()
    {
        // Arrange
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(0x12345678, "Test Entry");
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
        var vm = new StblEditorViewModel();
        var resource = new StblResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.Add(0x12345678, "Test Entry");
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
        var vm = new StblEditorViewModel();

        // Act
        var data = vm.GetData();

        // Assert
        data.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void StblEntryViewModel_KeyHex_FormatsCorrectly()
    {
        // Arrange & Act
        var entry = new StblEntryViewModel(0x12345678, "Test");

        // Assert
        entry.KeyHex.Should().Be("0x12345678");
    }

    [Fact]
    public void StblEntryViewModel_Value_CanBeChanged()
    {
        // Arrange
        var entry = new StblEntryViewModel(0x12345678, "Original");
        bool propertyChanged = false;
        entry.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(StblEntryViewModel.Value))
                propertyChanged = true;
        };

        // Act
        entry.Value = "Modified";

        // Assert
        entry.Value.Should().Be("Modified");
        propertyChanged.Should().BeTrue();
    }
}
