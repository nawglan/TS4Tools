using FluentAssertions;
using TS4Tools.Resources.Common.CatalogTags;
using Xunit;

namespace TS4Tools.Resources.Common.Tests.CatalogTags;

public class CompoundTagTests
{
    [Fact]
    public void Constructor_CreatesEmptyCompoundTag()
    {
        // Act
        var compoundTag = new CompoundTag();

        // Assert
        compoundTag.Tags.Should().BeEmpty();
        compoundTag.Name.Should().Be(string.Empty);
        compoundTag.Count.Should().Be(0);
    }

    [Fact]
    public void AddTag_AddsTagToCollection()
    {
        // Arrange
        var compoundTag = new CompoundTag();
        var tag = new Tag(123u, "TestTag");

        // Act
        compoundTag.AddTag(tag);

        // Assert
        compoundTag.Tags.Should().ContainSingle()
            .Which.Should().Be(tag);
        compoundTag.Count.Should().Be(1);
    }

    [Fact]
    public void AddTag_WithNullTag_ThrowsArgumentNullException()
    {
        // Arrange
        var compoundTag = new CompoundTag();

        // Act & Assert
        var act = () => compoundTag.AddTag(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveTag_RemovesExistingTag_ReturnsTrue()
    {
        // Arrange
        var compoundTag = new CompoundTag();
        var tag = new Tag(123u, "TestTag");
        compoundTag.AddTag(tag);

        // Act
        var result = compoundTag.RemoveTag(tag);

        // Assert
        result.Should().BeTrue();
        compoundTag.Tags.Should().BeEmpty();
        compoundTag.Count.Should().Be(0);
    }

    [Fact]
    public void RemoveTag_WithNonExistentTag_ReturnsFalse()
    {
        // Arrange
        var compoundTag = new CompoundTag();
        var tag = new Tag(123u, "TestTag");

        // Act
        var result = compoundTag.RemoveTag(tag);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveTagByIndex_RemovesTagWithMatchingIndex_ReturnsTrue()
    {
        // Arrange
        var compoundTag = new CompoundTag();
        var tag = new Tag(123u, "TestTag");
        compoundTag.AddTag(tag);

        // Act
        var result = compoundTag.RemoveTagByIndex(123u);

        // Assert
        result.Should().BeTrue();
        compoundTag.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTagByIndex_WithNonExistentIndex_ReturnsFalse()
    {
        // Arrange
        var compoundTag = new CompoundTag();

        // Act
        var result = compoundTag.RemoveTagByIndex(123u);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsTag_WithExistingIndex_ReturnsTrue()
    {
        // Arrange
        var compoundTag = new CompoundTag();
        var tag = new Tag(123u, "TestTag");
        compoundTag.AddTag(tag);

        // Act
        var result = compoundTag.ContainsTag(123u);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsTag_WithNonExistentIndex_ReturnsFalse()
    {
        // Arrange
        var compoundTag = new CompoundTag();

        // Act
        var result = compoundTag.ContainsTag(123u);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetTag_WithExistingIndex_ReturnsTag()
    {
        // Arrange
        var compoundTag = new CompoundTag();
        var tag = new Tag(123u, "TestTag");
        compoundTag.AddTag(tag);

        // Act
        var result = compoundTag.GetTag(123u);

        // Assert
        result.Should().Be(tag);
    }

    [Fact]
    public void GetTag_WithNonExistentIndex_ReturnsNull()
    {
        // Arrange
        var compoundTag = new CompoundTag();

        // Act
        var result = compoundTag.GetTag(123u);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Clear_RemovesAllTags()
    {
        // Arrange
        var compoundTag = new CompoundTag();
        compoundTag.AddTag(new Tag(1u, "Tag1"));
        compoundTag.AddTag(new Tag(2u, "Tag2"));

        // Act
        compoundTag.Clear();

        // Assert
        compoundTag.Tags.Should().BeEmpty();
        compoundTag.Count.Should().Be(0);
    }

    [Fact]
    public void Equals_WithSameTagsAndName_ReturnsTrue()
    {
        // Arrange
        var compoundTag1 = new CompoundTag { Name = "TestCompound" };
        var compoundTag2 = new CompoundTag { Name = "TestCompound" };
        var tag = new Tag(123u, "TestTag");

        compoundTag1.AddTag(tag);
        compoundTag2.AddTag(tag);

        // Act & Assert
        compoundTag1.Should().Be(compoundTag2);
        compoundTag1.GetHashCode().Should().Be(compoundTag2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentNames_ReturnsFalse()
    {
        // Arrange
        var compoundTag1 = new CompoundTag { Name = "TestCompound1" };
        var compoundTag2 = new CompoundTag { Name = "TestCompound2" };

        // Act & Assert
        compoundTag1.Should().NotBe(compoundTag2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var compoundTag = new CompoundTag { Name = "TestCompound" };
        compoundTag.AddTag(new Tag(1u, "Tag1"));
        compoundTag.AddTag(new Tag(2u, "Tag2"));

        // Act
        var result = compoundTag.ToString();

        // Assert
        result.Should().Be("TestCompound (2 tags)");
    }
}
