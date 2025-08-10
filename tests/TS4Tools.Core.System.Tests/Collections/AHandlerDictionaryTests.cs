using FluentAssertions;
using TS4Tools.Core.System.Collections;
using Xunit;

namespace TS4Tools.Core.System.Tests.Collections;

public class AHandlerDictionaryTests
{
    private sealed class TestDictionary : AHandlerDictionary<string, string>
    {
        public TestDictionary(EventHandler? handler = null) : base(handler) { }
        public TestDictionary(EventHandler? handler, IDictionary<string, string> dictionary) : base(handler, dictionary) { }
        public TestDictionary(EventHandler? handler, IEqualityComparer<string>? comparer) : base(handler, comparer) { }
        public TestDictionary(EventHandler? handler, int capacity) : base(handler, capacity) { }
    }

    [Fact]
    public void Constructor_WithHandler_ShouldInitializeEmpty()
    {
        // Arrange
        var handlerCalled = false;
        EventHandler handler = (sender, args) => handlerCalled = true;

        // Act
        var dictionary = new TestDictionary(handler);

        // Assert
        dictionary.Should().BeEmpty();
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public void Add_ShouldTriggerHandler()
    {
        // Arrange
        var handlerCalled = false;
        object? handlerSender = null;
        EventHandler handler = (sender, args) =>
        {
            handlerCalled = true;
            handlerSender = sender;
        };
        var dictionary = new TestDictionary(handler);

        // Act
        dictionary.Add("key1", "value1");

        // Assert
        handlerCalled.Should().BeTrue();
        handlerSender.Should().Be(dictionary);
        dictionary.Should().ContainKey("key1");
        dictionary["key1"].Should().Be("value1");
    }

    [Fact]
    public void Indexer_SetNewValue_ShouldTriggerHandler()
    {
        // Arrange
        var handlerCallCount = 0;
        EventHandler handler = (sender, args) => handlerCallCount++;
        var dictionary = new TestDictionary(handler);

        // Act
        dictionary["key1"] = "value1";

        // Assert
        handlerCallCount.Should().Be(1);
        dictionary["key1"].Should().Be("value1");
    }

    [Fact]
    public void Indexer_SetSameValue_ShouldNotTriggerHandler()
    {
        // Arrange
        var handlerCallCount = 0;
        EventHandler handler = (sender, args) => handlerCallCount++;
        var dictionary = new TestDictionary(handler);
        dictionary["key1"] = "value1";
        handlerCallCount = 0; // Reset counter

        // Act
        dictionary["key1"] = "value1"; // Same value

        // Assert
        handlerCallCount.Should().Be(0);
    }

    [Fact]
    public void Indexer_SetDifferentValue_ShouldTriggerHandler()
    {
        // Arrange
        var handlerCallCount = 0;
        EventHandler handler = (sender, args) => handlerCallCount++;
        var dictionary = new TestDictionary(handler);
        dictionary["key1"] = "value1";
        handlerCallCount = 0; // Reset counter

        // Act
        dictionary["key1"] = "value2"; // Different value

        // Assert
        handlerCallCount.Should().Be(1);
        dictionary["key1"].Should().Be("value2");
    }

    [Fact]
    public void Clear_WithItems_ShouldTriggerHandler()
    {
        // Arrange
        var handlerCallCount = 0;
        EventHandler handler = (sender, args) => handlerCallCount++;
        var dictionary = new TestDictionary(handler);
        dictionary.Add("key1", "value1");
        handlerCallCount = 0; // Reset counter

        // Act
        dictionary.Clear();

        // Assert
        handlerCallCount.Should().Be(1);
        dictionary.Should().BeEmpty();
    }

    [Fact]
    public void Clear_WhenEmpty_ShouldNotTriggerHandler()
    {
        // Arrange
        var handlerCallCount = 0;
        EventHandler handler = (sender, args) => handlerCallCount++;
        var dictionary = new TestDictionary(handler);

        // Act
        dictionary.Clear();

        // Assert
        handlerCallCount.Should().Be(0);
    }

    [Fact]
    public void Remove_ExistingKey_ShouldTriggerHandlerAndReturnTrue()
    {
        // Arrange
        var handlerCallCount = 0;
        EventHandler handler = (sender, args) => handlerCallCount++;
        var dictionary = new TestDictionary(handler);
        dictionary.Add("key1", "value1");
        handlerCallCount = 0; // Reset counter

        // Act
        var result = dictionary.Remove("key1");

        // Assert
        result.Should().BeTrue();
        handlerCallCount.Should().Be(1);
        dictionary.Should().NotContainKey("key1");
    }

    [Fact]
    public void Remove_NonExistingKey_ShouldNotTriggerHandlerAndReturnFalse()
    {
        // Arrange
        var handlerCallCount = 0;
        EventHandler handler = (sender, args) => handlerCallCount++;
        var dictionary = new TestDictionary(handler);

        // Act
        var result = dictionary.Remove("nonexistent");

        // Assert
        result.Should().BeFalse();
        handlerCallCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullHandler_ShouldNotThrow()
    {
        // Act & Assert
        var dictionary = new TestDictionary(null);
        dictionary.Add("key", "value"); // Should not throw
        dictionary.Should().ContainKey("key");
    }

    [Fact]
    public void Constructor_WithDictionary_ShouldCopyItems()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var dictionary = new TestDictionary(null, source);

        // Assert
        dictionary.Should().HaveCount(2);
        dictionary.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        dictionary.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void Constructor_WithCapacity_ShouldInitializeWithCapacity()
    {
        // Act
        var dictionary = new TestDictionary(null, 100);

        // Assert
        dictionary.Should().BeEmpty();
        // Capacity is internal, but we can verify it works by adding items
        for (int i = 0; i < 100; i++)
        {
            dictionary.Add($"key{i}", $"value{i}");
        }
        dictionary.Should().HaveCount(100);
    }

    [Fact]
    public void Operations_WithStringComparer_ShouldRespectComparer()
    {
        // Arrange
        var dictionary = new TestDictionary(null, StringComparer.OrdinalIgnoreCase);

        // Act
        dictionary.Add("KEY", "value1");
        var hasLowerCase = dictionary.ContainsKey("key");

        // Assert
        hasLowerCase.Should().BeTrue();
        dictionary["key"].Should().Be("value1");
    }
}
