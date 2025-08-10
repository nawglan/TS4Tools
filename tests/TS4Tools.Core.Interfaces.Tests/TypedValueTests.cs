using FluentAssertions;
using TS4Tools.Core.Interfaces;
using Xunit;

namespace TS4Tools.Core.Interfaces.Tests;

/// <summary>
/// Tests for the TypedValue record struct
/// </summary>
public class TypedValueTests
{
    [Fact]
    public void Create_ShouldCreateTypedValueWithCorrectType()
    {
        // Arrange & Act
        var typedValue = TypedValue.Create(42);

        // Assert
        typedValue.Type.Should().Be<int>();
        typedValue.Value.Should().Be(42);
        typedValue.Format.Should().Be("");
    }

    [Fact]
    public void Create_WithFormat_ShouldCreateTypedValueWithFormat()
    {
        // Arrange & Act
        var typedValue = TypedValue.Create(255, "X");

        // Assert
        typedValue.Type.Should().Be<int>();
        typedValue.Value.Should().Be(255);
        typedValue.Format.Should().Be("X");
    }

    [Fact]
    public void GetValue_ShouldReturnCorrectlyTypedValue()
    {
        // Arrange
        var typedValue = TypedValue.Create("test");

        // Act
        var result = typedValue.GetValue<string>();

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void GetValue_WithWrongType_ShouldReturnDefault()
    {
        // Arrange
        var typedValue = TypedValue.Create("test");

        // Act
        var result = typedValue.GetValue<int>();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void ToString_WithHexFormat_ShouldFormatCorrectly()
    {
        // Arrange
        var typedValue = TypedValue.Create(255, "X");

        // Act
        var result = typedValue.ToString();

        // Assert
        result.Should().Be("0x000000FF");
    }

    [Theory]
    [InlineData(255L, "X", "0x00000000000000FF")]
    [InlineData(255, "X", "0x000000FF")]
    [InlineData((short)255, "X", "0x00FF")]
    [InlineData((byte)255, "X", "0xFF")]
    public void ToString_WithHexFormat_ShouldFormatDifferentTypesCorrectly(object value, string format, string expected)
    {
        // Arrange
        var typedValue = new TypedValue(value.GetType(), value, format);

        // Act
        var result = typedValue.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ImplicitStringConversion_ShouldWork()
    {
        // Arrange
        var typedValue = TypedValue.Create(42);

        // Act
        string result = typedValue;

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void CompareTo_ShouldCompareCorrectly()
    {
        // Arrange
        var value1 = TypedValue.Create(10);
        var value2 = TypedValue.Create(20);
        var value3 = TypedValue.Create(10);

        // Act & Assert
        value1.CompareTo(value2).Should().BeLessThan(0);
        value2.CompareTo(value1).Should().BeGreaterThan(0);
        value1.CompareTo(value3).Should().Be(0);
    }
}
