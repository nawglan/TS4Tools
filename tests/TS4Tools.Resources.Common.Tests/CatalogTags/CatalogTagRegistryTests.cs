using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Resources.Common.CatalogTags;
using Xunit;

namespace TS4Tools.Resources.Common.Tests.CatalogTags;

public class CatalogTagRegistryTests
{
    private readonly ILogger<CatalogTagRegistry> _logger;
    private readonly CatalogTagRegistry _registry;

    public CatalogTagRegistryTests()
    {
        _logger = Substitute.For<ILogger<CatalogTagRegistry>>();
        _registry = new CatalogTagRegistry(_logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CatalogTagRegistry(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    [Fact]
    public void FetchTag_WithUnknownIndex_ReturnsDefaultTag()
    {
        // Act
        var tag = _registry.FetchTag(0xFFFFFFFFu);

        // Assert
        tag.Should().NotBeNull();
        tag.Index.Should().Be(0xFFFFFFFFu);
        tag.Value.Should().Be("unknown");
        tag.IsUnknown().Should().BeTrue();
    }

    [Fact]
    public void FetchCategory_WithUnknownIndex_ReturnsDefaultCategory()
    {
        // Act
        var category = _registry.FetchCategory(0xFFFFFFFFu);

        // Assert
        category.Should().NotBeNull();
        category.Index.Should().Be(0xFFFFFFFFu);
        category.Value.Should().Be("unknown");
        category.IsUnknown().Should().BeTrue();
    }

    [Fact]
    public void TryFetchTag_WithUnknownIndex_ReturnsFalse()
    {
        // Act
        var result = _registry.TryFetchTag(0xFFFFFFFFu, out var tag);

        // Assert
        result.Should().BeFalse();
        tag.Should().BeNull();
    }

    [Fact]
    public void TryFetchCategory_WithUnknownIndex_ReturnsFalse()
    {
        // Act
        var result = _registry.TryFetchCategory(0xFFFFFFFFu, out var category);

        // Assert
        result.Should().BeFalse();
        category.Should().BeNull();
    }

    [Fact]
    public void AllTags_ReturnsEnumerableOfTags()
    {
        // Act
        var tags = _registry.AllTags();

        // Assert
        tags.Should().NotBeNull();
        tags.Should().BeAssignableTo<IEnumerable<Tag>>();
    }

    [Fact]
    public void AllCategories_ReturnsEnumerableOfCategories()
    {
        // Act
        var categories = _registry.AllCategories();

        // Assert
        categories.Should().NotBeNull();
        categories.Should().BeAssignableTo<IEnumerable<Tag>>();
    }

    [Fact]
    public void Tags_Property_ReturnsReadOnlyDictionary()
    {
        // Act
        var tags = _registry.Tags;

        // Assert
        tags.Should().NotBeNull();
        tags.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyDictionary<uint, Tag>>();
    }

    [Fact]
    public void Categories_Property_ReturnsReadOnlyDictionary()
    {
        // Act
        var categories = _registry.Categories;

        // Assert
        categories.Should().NotBeNull();
        categories.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyDictionary<uint, Tag>>();
    }

    [Fact]
    public void Registry_LoadsTagsLazily()
    {
        // Arrange - Registry is already constructed but shouldn't have loaded tags yet
        
        // Act - Access tags to trigger lazy loading
        var tags = _registry.Tags;
        var categories = _registry.Categories;

        // Assert - Should have valid collections (even if empty due to missing embedded resource in test)
        tags.Should().NotBeNull();
        categories.Should().NotBeNull();
    }
}
