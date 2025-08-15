using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Resources.Catalog;
using TS4Tools.Resources.Common;
using Xunit;

namespace TS4Tools.Resources.Catalog.Tests;

/// <summary>
/// Unit tests for ObjectCatalogResource implementation.
/// Tests the core functionality of object catalog resources for Buy/Build mode.
/// </summary>
public class ObjectCatalogResourceTests
{
    private readonly ILogger<ObjectCatalogResource> _mockLogger;

    public ObjectCatalogResourceTests()
    {
        _mockLogger = Substitute.For<ILogger<ObjectCatalogResource>>();
    }

    [Fact]
    public void Constructor_WithLogger_InitializesCorrectly()
    {
        // Act
        var resource = new ObjectCatalogResource(_mockLogger);

        // Assert
        Assert.Equal(0x319E4F1Du, resource.ResourceType);
        Assert.Equal(0u, resource.GroupId);
        Assert.Equal(0ul, resource.InstanceId);
        Assert.False(resource.IsDirty);
        Assert.True(resource.IsAvailableForPurchase);
        Assert.Equal(0.8f, resource.DepreciationValue, precision: 2);
        Assert.Equal(1, resource.RequestedApiVersion);
        Assert.Equal(1, resource.RecommendedApiVersion);
    }

    [Fact]
    public void Constructor_WithIdentifiers_InitializesCorrectly()
    {
        // Arrange
        const uint groupId = 0x12345678u;
        const ulong instanceId = 0x123456789ABCDEFul;

        // Act
        var resource = new ObjectCatalogResource(_mockLogger, groupId, instanceId);

        // Assert
        Assert.Equal(groupId, resource.GroupId);
        Assert.Equal(instanceId, resource.InstanceId);
    }

    [Fact]
    public void MarkDirty_SetsIsDirtyTrue_RaisesResourceChangedEvent()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        var eventRaised = false;
        resource.ResourceChanged += (sender, args) => eventRaised = true;

        // Act
        resource.MarkDirty();

        // Assert
        Assert.True(resource.IsDirty);
        Assert.True(eventRaised);
    }

    [Fact]
    public void MarkClean_SetsIsDirtyFalse()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        resource.MarkDirty();
        Assert.True(resource.IsDirty);

        // Act
        resource.MarkClean();

        // Assert
        Assert.False(resource.IsDirty);
    }

    [Fact]
    public void AddCategory_AddsCategoryToList()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        const uint categoryId = 0x12345678u;

        // Act
        resource.AddCategory(categoryId);

        // Assert
        Assert.Contains(categoryId, resource.Categories);
        Assert.True(resource.IsDirty);
    }

    [Fact]
    public void AddCategory_DoesNotAddDuplicateCategory()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        const uint categoryId = 0x12345678u;

        // Act
        resource.AddCategory(categoryId);
        resource.MarkClean();
        resource.AddCategory(categoryId); // Add same category again

        // Assert
        Assert.Single(resource.Categories);
        Assert.False(resource.IsDirty); // Should not mark dirty if duplicate
    }

    [Fact]
    public void RemoveCategory_RemovesCategoryFromList()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        const uint categoryId = 0x12345678u;
        resource.AddCategory(categoryId);
        resource.MarkClean();

        // Act
        var removed = resource.RemoveCategory(categoryId);

        // Assert
        Assert.True(removed);
        Assert.Empty(resource.Categories);
        Assert.True(resource.IsDirty);
    }

    [Fact]
    public void RemoveCategory_ReturnsFalseIfCategoryNotFound()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);

        // Act
        var removed = resource.RemoveCategory(0x12345678u);

        // Assert
        Assert.False(removed);
        Assert.False(resource.IsDirty);
    }

    [Fact]
    public void SetEnvironmentScore_SetsScoreCorrectly()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);

        // Act
        resource.SetEnvironmentScore(EnvironmentType.Fun, 8.5f);

        // Assert
        Assert.Equal(8.5f, resource.EnvironmentScores[EnvironmentType.Fun], precision: 2);
        Assert.True(resource.IsDirty);
    }

    [Fact]
    public void SetEnvironmentScore_ClampsScoreToValidRange()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);

        // Act
        resource.SetEnvironmentScore(EnvironmentType.Comfort, 15.0f); // Above maximum
        resource.SetEnvironmentScore(EnvironmentType.Decor, -15.0f); // Below minimum

        // Assert
        Assert.Equal(10.0f, resource.EnvironmentScores[EnvironmentType.Comfort], precision: 2);
        Assert.Equal(-10.0f, resource.EnvironmentScores[EnvironmentType.Decor], precision: 2);
    }

    [Fact]
    public void AddIcon_AddsIconToList()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        var iconRef = new TgiReference(0x12345678u, 0x87654321u, 0x123456789ABCDEFul);

        // Act
        resource.AddIcon(iconRef);

        // Assert
        Assert.Contains(iconRef, resource.Icons);
        Assert.True(resource.IsDirty);
    }

    [Fact]
    public void AddTag_AddsTagToList()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        const uint tagId = 0x12345678u;

        // Act
        resource.AddTag(tagId);

        // Assert
        Assert.Contains(tagId, resource.Tags);
        Assert.True(resource.IsDirty);
    }

    [Fact]
    public void Validate_WithValidData_ReturnsNoErrors()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);

        // Create a minimal valid catalog common block
        var commonBlock = new CatalogCommonBlock
        {
            FormatVersion = 1,
            CompatibilityFlags = CatalogCompatibilityFlags.BaseGame,
            CategoryId = 0x12345678u,
            MetadataFlags = 0
        };

        // Use reflection to set private properties for testing
        var commonBlockProperty = typeof(ObjectCatalogResource).GetProperty("CommonBlock");
        commonBlockProperty?.SetValue(resource, commonBlock);

        var priceProperty = typeof(ObjectCatalogResource).GetProperty("Price");
        priceProperty?.SetValue(resource, 100u);

        resource.AddCategory(0x12345678u);

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithMissingCommonBlock_ReturnsError()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Contains("Catalog common block is required", errors);
    }

    [Fact]
    public void Validate_WithZeroPrice_ReturnsError()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        resource.AddCategory(0x12345678u);

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Contains("Object price must be greater than zero", errors);
    }

    [Fact]
    public void Validate_WithNoCategories_ReturnsError()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);

        var priceProperty = typeof(ObjectCatalogResource).GetProperty("Price");
        priceProperty?.SetValue(resource, 100u);

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Contains("At least one category is required", errors);
    }

    [Fact]
    public void ContentFields_Indexer_ReturnsCorrectValues()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);

        // Act & Assert
        var result = resource[0];
        Assert.Equal(typeof(string), result.Type);
        Assert.Equal(string.Empty, result.Value);

        var resultByName = resource["nonexistent"];
        Assert.Equal(typeof(string), resultByName.Type);
        Assert.Equal(string.Empty, resultByName.Value);
    }

    [Fact]
    public void AsBytes_WithMemoryStream_ReturnsCorrectByteArray()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Access the private Stream property through reflection
        var streamProperty = typeof(ObjectCatalogResource).GetProperty("Stream");
        var memoryStream = new MemoryStream(testData);
        streamProperty?.SetValue(resource, memoryStream);

        // Act
        var result = resource.AsBytes;

        // Assert
        Assert.Equal(testData, result);
    }

    [Fact]
    public async Task DisposeAsync_ClearsResourcesCorrectly()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        resource.AddCategory(0x12345678u);
        resource.AddTag(0x87654321u);

        // Act
        await resource.DisposeAsync();

        // Assert
        Assert.Empty(resource.Categories);
        Assert.Empty(resource.Tags);
        Assert.Empty(resource.Properties);
    }

    [Fact]
    public void Dispose_ClearsResourcesCorrectly()
    {
        // Arrange
        var resource = new ObjectCatalogResource(_mockLogger);
        resource.AddCategory(0x12345678u);
        resource.AddTag(0x87654321u);

        // Act
        resource.Dispose();

        // Assert
        Assert.Empty(resource.Categories);
        Assert.Empty(resource.Tags);
        Assert.Empty(resource.Properties);
    }
}
