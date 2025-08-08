using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using Xunit;

namespace TS4Tools.Resources.Utility.Tests;

public class MetadataResourceTests
{
    private readonly ILogger<MetadataResource> _logger = NullLogger<MetadataResource>.Instance;
    private readonly ResourceKey _testKey = new(0x0166038C, 0x12345678, 0x87654321);

    private static readonly string[] ExpectedTags = { "tag1", "tag2", "tag3" };

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new MetadataResource(null!, _testKey);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        using var resource = new MetadataResource(_logger, _testKey);

        // Assert
        resource.Should().NotBeNull();
        resource.ResourceKey.Should().Be(_testKey);
        resource.MetadataCount.Should().Be(0);
        resource.IsJsonFormat.Should().BeFalse();
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public void ContentFields_ShouldReturnExpectedFields()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);

        // Act
        var fields = resource.ContentFields;

        // Assert
        fields.Should().Contain("IsJsonFormat");
        fields.Should().Contain("MetadataCount");
        fields.Should().Contain("MetadataKeys");
        fields.Should().Contain("AssetName");
        fields.Should().Contain("AssetDescription");
        fields.Should().Contain("CreatedDate");
        fields.Should().Contain("ModifiedDate");
    }

    [Fact]
    public void SetMetadata_WithValidData_ShouldStoreCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.SetMetadata("testKey", "testValue", MetadataType.String, "Test description");

        // Assert
        resource.MetadataCount.Should().Be(1);
        resource.GetMetadataValue("testKey").Should().Be("testValue");
        resource.HasMetadata("testKey").Should().BeTrue();
        var entry = resource.GetMetadata("testKey");
        entry.Should().NotBeNull();
        entry!.Type.Should().Be(MetadataType.String);
        entry.Description.Should().Be("Test description");
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void SetMetadata_WithNullOrWhitespaceKey_ShouldThrowArgumentException()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);

        // Act & Assert
        var act1 = () => resource.SetMetadata(null!, "value");
        var act2 = () => resource.SetMetadata("", "value");
        var act3 = () => resource.SetMetadata("   ", "value");

        act1.Should().Throw<ArgumentException>().WithParameterName("key");
        act2.Should().Throw<ArgumentException>().WithParameterName("key");
        act3.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void GetMetadata_WithExistingKey_ShouldReturnCorrectEntry()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.SetMetadata("testKey", "testValue", MetadataType.String, "Test description");

        // Act
        var entry = resource.GetMetadata("testKey");

        // Assert
        entry.Should().NotBeNull();
        entry!.Key.Should().Be("testKey");
        entry.Value.Should().Be("testValue");
        entry.Type.Should().Be(MetadataType.String);
        entry.Description.Should().Be("Test description");
    }

    [Fact]
    public void GetMetadataValue_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.SetMetadata("testKey", "testValue");

        // Act
        var value = resource.GetMetadataValue("testKey");

        // Assert
        value.Should().Be("testValue");
    }

    [Fact]
    public void GetMetadataValue_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);

        // Act
        var value = resource.GetMetadataValue("nonExistent");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void RemoveMetadata_WithExistingKey_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.SetMetadata("testKey", "testValue");
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        var result = resource.RemoveMetadata("testKey");

        // Assert
        result.Should().BeTrue();
        resource.HasMetadata("testKey").Should().BeFalse();
        resource.MetadataCount.Should().Be(0);
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void RemoveMetadata_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);

        // Act
        var result = resource.RemoveMetadata("nonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ClearMetadata_WithExistingData_ShouldClearAllData()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.SetMetadata("key1", "value1");
        resource.SetMetadata("key2", "value2");
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.ClearMetadata();

        // Assert
        resource.MetadataCount.Should().Be(0);
        resource.MetadataKeys.Should().BeEmpty();
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void AssetName_ShouldGetAndSetCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.AssetName = "Test Asset";

        // Assert
        resource.AssetName.Should().Be("Test Asset");
        resource.GetMetadataValue("AssetName").Should().Be("Test Asset");
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void AssetDescription_ShouldGetAndSetCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);

        // Act
        resource.AssetDescription = "This is a test asset";

        // Assert
        resource.AssetDescription.Should().Be("This is a test asset");
        resource.GetMetadataValue("Description").Should().Be("This is a test asset");
    }

    [Fact]
    public void CreatedDate_ShouldGetAndSetCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        var testDate = new DateTime(2024, 1, 15, 10, 30, 45);

        // Act
        resource.CreatedDate = testDate;

        // Assert
        resource.CreatedDate.Should().Be(testDate);
        resource.GetMetadataValue("CreatedDate").Should().Be("2024-01-15 10:30:45");
    }

    [Fact]
    public void ModifiedDate_ShouldGetAndSetCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        var testDate = new DateTime(2024, 2, 20, 14, 15, 30);

        // Act
        resource.ModifiedDate = testDate;

        // Assert
        resource.ModifiedDate.Should().Be(testDate);
        resource.GetMetadataValue("ModifiedDate").Should().Be("2024-02-20 14:15:30");
    }

    [Fact]
    public void Version_ShouldGetAndSetCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);

        // Act
        resource.Version = "1.2.3";

        // Assert
        resource.Version.Should().Be("1.2.3");
        resource.GetMetadataValue("Version").Should().Be("1.2.3");
    }

    [Fact]
    public void Author_ShouldGetAndSetCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);

        // Act
        resource.Author = "Test Author";

        // Assert
        resource.Author.Should().Be("Test Author");
        resource.GetMetadataValue("Author").Should().Be("Test Author");
    }

    [Fact]
    public void Tags_ShouldGetAndSetCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);

        // Act
        resource.Tags = ExpectedTags;

        // Assert
        resource.Tags.Should().BeEquivalentTo(ExpectedTags);
        resource.GetMetadataValue("Tags").Should().Be("tag1, tag2, tag3");
    }

    [Fact]
    public void Tags_WithCommaDelimitedString_ShouldParseCorrectly()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.SetMetadata("Tags", "tag1, tag2, tag3");

        // Act
        var tags = resource.Tags.ToArray();

        // Assert
        tags.Should().BeEquivalentTo(ExpectedTags);
    }

    [Fact]
    public void GetMetadataByType_ShouldReturnCorrectEntries()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.SetMetadata("stringKey", "stringValue", MetadataType.String);
        resource.SetMetadata("intKey", "42", MetadataType.Integer);
        resource.SetMetadata("anotherString", "anotherValue", MetadataType.String);

        // Act
        var stringEntries = resource.GetMetadataByType(MetadataType.String).ToList();
        var intEntries = resource.GetMetadataByType(MetadataType.Integer).ToList();

        // Assert
        stringEntries.Should().HaveCount(2);
        stringEntries.Should().Contain(e => e.Key == "stringKey");
        stringEntries.Should().Contain(e => e.Key == "anotherString");
        intEntries.Should().HaveCount(1);
        intEntries.Should().Contain(e => e.Key == "intKey");
    }

    [Fact]
    public void GetMetadataModifiedAfter_ShouldReturnCorrectEntries()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        var cutoffDate = DateTime.UtcNow;

        // Add some metadata before the cutoff
        resource.SetMetadata("oldKey", "oldValue");
        Thread.Sleep(10); // Ensure time difference

        // Add some metadata after the cutoff
        var afterCutoff = DateTime.UtcNow;
        resource.SetMetadata("newKey", "newValue");

        // Act
        var recentEntries = resource.GetMetadataModifiedAfter(afterCutoff).ToList();

        // Assert
        recentEntries.Should().HaveCount(1);
        recentEntries.Should().Contain(e => e.Key == "newKey");
    }

    [Fact]
    public void ParseFromStream_WithJsonFormat_ShouldParseCorrectly()
    {
        // Arrange
        var jsonContent = """
        {
            "AssetName": {
                "value": "Test Asset",
                "type": "String",
                "description": "Name of the asset"
            },
            "Version": {
                "value": "1.0.0",
                "type": "String"
            }
        }
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        using var resource = new MetadataResource(_logger, _testKey, stream);

        // Assert
        resource.IsJsonFormat.Should().BeTrue();
        resource.MetadataCount.Should().BeGreaterThan(0);
        resource.AssetName.Should().Be("Test Asset");
        resource.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void ParseFromStream_WithKeyValueFormat_ShouldParseCorrectly()
    {
        // Arrange
        var keyValueContent = """
        # Asset name
        AssetName=Test Asset
        # Asset version
        Version=1.0.0
        Description=This is a test asset
        """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(keyValueContent));

        // Act
        using var resource = new MetadataResource(_logger, _testKey, stream);

        // Assert
        resource.IsJsonFormat.Should().BeFalse();
        resource.MetadataCount.Should().Be(3);
        resource.AssetName.Should().Be("Test Asset");
        resource.Version.Should().Be("1.0.0");
        resource.AssetDescription.Should().Be("This is a test asset");
    }

    [Fact]
    public async Task SerializeAsync_WithJsonFormat_ShouldReturnJsonContent()
    {
        // Arrange
        var jsonContent = """{"AssetName": {"value": "Test", "type": "String"}}""";
        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        using var resource = new MetadataResource(_logger, _testKey, inputStream);

        // Act
        using var outputStream = await resource.SerializeAsync();
        var result = System.Text.Encoding.UTF8.GetString(((MemoryStream)outputStream).ToArray());

        // Assert
        result.Should().Contain("AssetName");
        result.Should().Contain("Test");
        result.Should().Contain("String");
    }

    [Fact]
    public async Task SerializeAsync_WithKeyValueFormat_ShouldReturnKeyValueContent()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.AssetName = "Test Asset";
        resource.Version = "1.0.0";

        // Act
        using var outputStream = await resource.SerializeAsync();
        var result = System.Text.Encoding.UTF8.GetString(((MemoryStream)outputStream).ToArray());

        // Assert
        result.Should().Contain("AssetName=Test Asset");
        result.Should().Contain("Version=1.0.0");
        result.Should().Contain("# Metadata file generated by TS4Tools");
    }

    [Fact]
    public void Clone_ShouldCreateIdenticalCopy()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.AssetName = "Test Asset";
        resource.Version = "1.0.0";
        resource.SetMetadata("customKey", "customValue", MetadataType.String, "Custom metadata");

        // Act
        var clone = resource.Clone();

        // Assert
        clone.Should().NotBeSameAs(resource);
        clone.Should().BeOfType<MetadataResource>();
        var clonedResource = (MetadataResource)clone;
        clonedResource.MetadataCount.Should().Be(resource.MetadataCount);
        clonedResource.AssetName.Should().Be("Test Asset");
        clonedResource.Version.Should().Be("1.0.0");
        clonedResource.GetMetadataValue("customKey").Should().Be("customValue");
    }

    [Fact]
    public void Validate_WithValidResource_ShouldReturnTrue()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.AssetName = "Test Asset";
        resource.SetMetadata("validKey", "validValue", MetadataType.String);

        // Act
        var isValid = resource.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ToString_ShouldReturnDescriptiveString()
    {
        // Arrange
        using var resource = new MetadataResource(_logger, _testKey);
        resource.AssetName = "Test Asset";
        resource.Version = "1.0.0";

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Contain("MetadataResource");
        result.Should().Contain("entries");
    }
}

public class MetadataEntryTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var entry = new MetadataEntry();

        // Assert
        entry.Key.Should().Be(string.Empty);
        entry.Value.Should().Be(string.Empty);
        entry.Type.Should().Be(MetadataType.String);
        entry.Description.Should().Be(string.Empty);
        entry.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var entry1 = new MetadataEntry
        {
            Key = "testKey",
            Value = "testValue",
            Type = MetadataType.String,
            Description = "Test description"
        };

        var entry2 = new MetadataEntry
        {
            Key = "testKey",
            Value = "testValue",
            Type = MetadataType.String,
            Description = "Test description"
        };

        // Act & Assert
        entry1.Equals(entry2).Should().BeTrue();
        (entry1 == entry2).Should().BeFalse(); // Reference equality
        entry1.GetHashCode().Should().Be(entry2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var entry1 = new MetadataEntry { Key = "key1", Value = "value1" };
        var entry2 = new MetadataEntry { Key = "key2", Value = "value2" };

        // Act & Assert
        entry1.Equals(entry2).Should().BeFalse();
        entry1.GetHashCode().Should().NotBe(entry2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnDescriptiveString()
    {
        // Arrange
        var entry = new MetadataEntry
        {
            Key = "testKey",
            Value = "testValue",
            Type = MetadataType.String
        };

        // Act
        var result = entry.ToString();

        // Assert
        result.Should().Contain("testKey");
        result.Should().Contain("testValue");
        result.Should().Contain("String");
    }

    [Theory]
    [InlineData(MetadataType.String)]
    [InlineData(MetadataType.Integer)]
    [InlineData(MetadataType.Float)]
    [InlineData(MetadataType.Boolean)]
    [InlineData(MetadataType.DateTime)]
    [InlineData(MetadataType.Url)]
    [InlineData(MetadataType.Json)]
    [InlineData(MetadataType.Binary)]
    public void Type_WithVariousTypes_ShouldSetCorrectly(MetadataType type)
    {
        // Arrange
        var entry = new MetadataEntry();

        // Act
        entry.Type = type;

        // Assert
        entry.Type.Should().Be(type);
    }
}
