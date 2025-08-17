using FluentAssertions;
using TS4Tools.Resources.Common.CatalogTags;
using Xunit;

namespace TS4Tools.Resources.Common.Tests.CatalogTags;

public class TagTests
{
    [Fact]
    public void Constructor_Default_CreatesTagWithDefaultValues()
    {
        // Act
        var tag = new Tag();

        // Assert
        tag.Index.Should().Be(0u);
        tag.Value.Should().Be("unknown");
        tag.IsUnknown().Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithIndexAndValue_CreatesTagWithSpecifiedValues()
    {
        // Arrange
        const uint expectedIndex = 0x12345678u;
        const string expectedValue = "TestTag";

        // Act
        var tag = new Tag(expectedIndex, expectedValue);

        // Assert
        tag.Index.Should().Be(expectedIndex);
        tag.Value.Should().Be(expectedValue);
        tag.IsUnknown().Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullValue_UsesUnknownAsDefault()
    {
        // Act
        var tag = new Tag(123u, null!);

        // Assert
        tag.Value.Should().Be("unknown");
        tag.IsUnknown().Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var tag = new Tag(0x12345678u, "TestTag");

        // Act
        var result = tag.ToString();

        // Assert
        result.Should().Be("TestTag (0x12345678)");
    }

    [Fact]
    public void IsUnknown_WithUnknownValue_ReturnsTrue()
    {
        // Arrange
        var tag = new Tag(123u, "unknown");

        // Act & Assert
        tag.IsUnknown().Should().BeTrue();
    }

    [Fact]
    public void IsUnknown_WithKnownValue_ReturnsFalse()
    {
        // Arrange
        var tag = new Tag(123u, "KnownTag");

        // Act & Assert
        tag.IsUnknown().Should().BeFalse();
    }

    [Fact]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var tag1 = new Tag(123u, "TestTag");
        var tag2 = new Tag(123u, "TestTag");
        var tag3 = new Tag(456u, "TestTag");
        var tag4 = new Tag(123u, "DifferentTag");

        // Act & Assert
        tag1.Should().Be(tag2);
        tag1.Should().NotBe(tag3);
        tag1.Should().NotBe(tag4);
        tag1.GetHashCode().Should().Be(tag2.GetHashCode());
    }
}
