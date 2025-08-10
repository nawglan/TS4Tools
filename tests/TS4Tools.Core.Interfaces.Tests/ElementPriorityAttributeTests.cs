using FluentAssertions;
using TS4Tools.Core.Interfaces;
using Xunit;

namespace TS4Tools.Core.Interfaces.Tests;

/// <summary>
/// Tests for the ElementPriorityAttribute
/// </summary>
public class ElementPriorityAttributeTests
{
    [Fact]
    public void Constructor_ShouldSetPriority()
    {
        // Arrange & Act
        var attribute = new ElementPriorityAttribute(5);

        // Assert
        attribute.Priority.Should().Be(5);
    }

    [Fact]
    public void GetPriority_WithValidProperty_ShouldReturnPriority()
    {
        // Arrange
        var priority = ElementPriorityAttribute.GetPriority(typeof(TestClass), nameof(TestClass.TestProperty));

        // Assert
        priority.Should().Be(10);
    }

    [Fact]
    public void GetPriority_WithValidField_ShouldReturnPriority()
    {
        // Arrange
        var priority = ElementPriorityAttribute.GetPriority(typeof(TestClass), nameof(TestClass.TestField));

        // Assert
        priority.Should().Be(20);
    }

    [Fact]
    public void GetPriority_WithNonExistentMember_ShouldReturnMaxValue()
    {
        // Arrange
        var priority = ElementPriorityAttribute.GetPriority(typeof(TestClass), "NonExistent");

        // Assert
        priority.Should().Be(int.MaxValue);
    }

    [Fact]
    public void GetPriority_WithMemberWithoutAttribute_ShouldReturnMaxValue()
    {
        // Arrange
        var priority = ElementPriorityAttribute.GetPriority(typeof(TestClass), nameof(TestClass.PropertyWithoutAttribute));

        // Assert
        priority.Should().Be(int.MaxValue);
    }

    [Fact]
    public void GetPriority_WithNullType_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => ElementPriorityAttribute.GetPriority(null!, "test");
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetPriority_WithNullOrEmptyFieldName_ShouldThrowArgumentException(string? fieldName)
    {
        // Act & Assert
        Action act = () => ElementPriorityAttribute.GetPriority(typeof(TestClass), fieldName!);
        act.Should().Throw<ArgumentException>();
    }

    private sealed class TestClass
    {
        [ElementPriority(10)]
        public string TestProperty { get; set; } = string.Empty;

        [ElementPriority(20)]
        public string TestField = string.Empty;

        public string PropertyWithoutAttribute { get; set; } = string.Empty;
    }
}
