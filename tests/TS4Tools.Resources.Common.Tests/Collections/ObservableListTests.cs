using FluentAssertions;
using TS4Tools.Resources.Common.Collections;
using Xunit;

namespace TS4Tools.Resources.Common.Tests.Collections;

public class ObservableListTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptyList()
    {
        // Act
        var list = new ObservableList<string>();

        // Assert
        list.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithItems_CreatesListWithItems()
    {
        // Arrange
        var items = new[] { "Item1", "Item2", "Item3" };

        // Act
        var list = new ObservableList<string>(items);

        // Assert
        list.Should().HaveCount(3);
        list.Should().ContainInOrder(items);
    }

    [Fact]
    public void AddRange_AddsMultipleItems()
    {
        // Arrange
        var list = new ObservableList<string>();
        var items = new[] { "Item1", "Item2", "Item3" };
        var collectionChangedRaised = false;

        list.CollectionChanged += (sender, e) => collectionChangedRaised = true;

        // Act
        list.AddRange(items);

        // Assert
        list.Should().HaveCount(3);
        list.Should().ContainInOrder(items);
        collectionChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void AddRange_WithNullItems_ThrowsArgumentNullException()
    {
        // Arrange
        var list = new ObservableList<string>();

        // Act & Assert
        var act = () => list.AddRange(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveRange_RemovesMultipleItems_ReturnsCorrectCount()
    {
        // Arrange
        var list = new ObservableList<string> { "Item1", "Item2", "Item3", "Item4" };
        var itemsToRemove = new[] { "Item2", "Item4", "NonExistent" };
        var collectionChangedRaised = false;

        list.CollectionChanged += (sender, e) => collectionChangedRaised = true;

        // Act
        var removedCount = list.RemoveRange(itemsToRemove);

        // Assert
        removedCount.Should().Be(2);
        list.Should().HaveCount(2);
        list.Should().ContainInOrder("Item1", "Item3");
        collectionChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void RemoveRange_WithNoItemsRemoved_DoesNotRaiseCollectionChanged()
    {
        // Arrange
        var list = new ObservableList<string> { "Item1", "Item2" };
        var itemsToRemove = new[] { "NonExistent1", "NonExistent2" };
        var collectionChangedRaised = false;

        list.CollectionChanged += (sender, e) => collectionChangedRaised = true;

        // Act
        var removedCount = list.RemoveRange(itemsToRemove);

        // Assert
        removedCount.Should().Be(0);
        collectionChangedRaised.Should().BeFalse();
    }

    [Fact]
    public void ReplaceAll_ReplacesAllItems()
    {
        // Arrange
        var list = new ObservableList<string> { "OldItem1", "OldItem2" };
        var newItems = new[] { "NewItem1", "NewItem2", "NewItem3" };
        var collectionChangedRaised = false;

        list.CollectionChanged += (sender, e) => collectionChangedRaised = true;

        // Act
        list.ReplaceAll(newItems);

        // Assert
        list.Should().HaveCount(3);
        list.Should().ContainInOrder(newItems);
        collectionChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void SuppressNotifications_SuppressesEvents()
    {
        // Arrange
        var list = new ObservableList<string>();
        var collectionChangedCount = 0;
        var propertyChangedCount = 0;

        list.CollectionChanged += (sender, e) => collectionChangedCount++;
        ((System.ComponentModel.INotifyPropertyChanged)list).PropertyChanged += (sender, e) => propertyChangedCount++;

        // Act
        using (list.SuppressNotifications())
        {
            list.Add("Item1");
            list.Add("Item2");
            list.Remove("Item1");
        }

        // Events should be suppressed during the using block
        collectionChangedCount.Should().Be(0);
        propertyChangedCount.Should().Be(0);

        // Add item after suppression is lifted
        list.Add("Item3");

        // Assert
        list.Should().HaveCount(2);
        list.Should().ContainInOrder("Item2", "Item3");
        collectionChangedCount.Should().Be(1); // Only the last Add should raise event
        propertyChangedCount.Should().BeGreaterThan(0); // Count and indexer properties changed
    }

    [Fact]
    public void SuppressNotifications_IsReentrant()
    {
        // Arrange
        var list = new ObservableList<string>();
        var eventCount = 0;

        list.CollectionChanged += (sender, e) => eventCount++;

        // Act
        using (list.SuppressNotifications())
        {
            using (list.SuppressNotifications()) // Nested suppression
            {
                list.Add("Item1");
            }
            list.Add("Item2");
        }

        // Assert
        eventCount.Should().Be(0);
        list.Should().HaveCount(2);
    }

    [Fact]
    public void BulkOperations_WithSuppression_OnlyRaiseResetEvent()
    {
        // Arrange
        var list = new ObservableList<string> { "Existing" };
        var resetEventCount = 0;
        var otherEventCount = 0;

        // Use static readonly array instead of constant array
        var itemsToAdd = new[] { "Item1", "Item2", "Item3" };

        list.CollectionChanged += (sender, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                resetEventCount++;
            else
                otherEventCount++;
        };

        // Act
        list.AddRange(itemsToAdd);

        // Assert
        resetEventCount.Should().Be(1);
        otherEventCount.Should().Be(0);
        list.Should().HaveCount(4);
    }
}
