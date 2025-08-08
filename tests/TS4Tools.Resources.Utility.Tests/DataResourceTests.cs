using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;
using Xunit;

namespace TS4Tools.Resources.Utility.Tests;

public class DataResourceTests
{
    private readonly ILogger<DataResource> _logger = NullLogger<DataResource>.Instance;
    private readonly ResourceKey _testKey = new ResourceKey(0x545AC67A, 0x12345678, 0x87654321);

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new DataResource(null!, _testKey);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        using var resource = new DataResource(_logger, _testKey);

        // Assert
        resource.Should().NotBeNull();
        resource.ResourceKey.Should().Be(_testKey);
        resource.DataCount.Should().Be(0);
        resource.StructureCount.Should().Be(0);
        resource.IsXmlFormat.Should().BeFalse();
        resource.RequestedApiVersion.Should().Be(1);
        resource.RecommendedApiVersion.Should().Be(1);
    }

    [Fact]
    public void ContentFields_ShouldReturnExpectedFields()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);

        // Act
        var fields = resource.ContentFields;

        // Assert
        fields.Should().Contain("Version");
        fields.Should().Contain("DataTablePosition");
        fields.Should().Contain("StructureTablePosition");
        fields.Should().Contain("DataCount");
        fields.Should().Contain("StructureCount");
        fields.Should().Contain("IsXmlFormat");
    }

    [Fact]
    public void Version_WhenChanged_ShouldTriggerResourceChanged()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.Version = 0x200;

        // Assert
        resource.Version.Should().Be(0x200u);
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void DataTablePosition_WhenChanged_ShouldTriggerResourceChanged()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.DataTablePosition = 0x1000;

        // Assert
        resource.DataTablePosition.Should().Be(0x1000u);
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void StructureTablePosition_WhenChanged_ShouldTriggerResourceChanged()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.StructureTablePosition = 0x2000;

        // Assert
        resource.StructureTablePosition.Should().Be(0x2000u);
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void ParseFromStream_WithEmptyStream_ShouldCreateEmptyResource()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        using var resource = new DataResource(_logger, _testKey, stream);

        // Assert
        resource.DataCount.Should().Be(0);
        resource.StructureCount.Should().Be(0);
        resource.IsXmlFormat.Should().BeFalse();
    }

    [Fact]
    public void ParseFromStream_WithSimpleXml_ShouldParseAsXml()
    {
        // Arrange
        var xmlContent = "<?xml version=\"1.0\"?><root><item>test</item></root>";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act
        using var resource = new DataResource(_logger, _testKey, stream);

        // Assert
        resource.IsXmlFormat.Should().BeTrue();
        resource.XmlContent.Should().NotBeNull();
        resource.XmlContent.Should().Contain("<root>");
    }

    [Fact]
    public void ParseFromStream_WithInvalidData_ShouldStoreAsRawData()
    {
        // Arrange
        var binaryData = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA };
        using var stream = new MemoryStream(binaryData);

        // Act
        using var resource = new DataResource(_logger, _testKey, stream);

        // Assert
        resource.IsXmlFormat.Should().BeFalse();
        resource.DataCount.Should().Be(0);
        resource.StructureCount.Should().Be(0);
    }

    [Fact]
    public async Task SerializeAsync_WithEmptyResource_ShouldReturnEmptyStream()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);

        // Act
        using var stream = await resource.SerializeAsync();

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0); // Should contain at least the DATA header
    }

    [Fact]
    public async Task SerializeAsync_WithXmlResource_ShouldReturnXmlContent()
    {
        // Arrange
        var xmlContent = "<?xml version=\"1.0\"?><root><item>test</item></root>";
        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        using var resource = new DataResource(_logger, _testKey, inputStream);

        // Act
        using var outputStream = await resource.SerializeAsync();
        var result = System.Text.Encoding.UTF8.GetString(((MemoryStream)outputStream).ToArray());

        // Assert
        result.Should().Contain("<root>");
        result.Should().Contain("<item>test</item>");
    }

    [Fact]
    public void AddDataEntry_WithValidEntry_ShouldAddToCollection()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var entry = new DataEntry
        {
            Name = "TestEntry",
            DataType = FieldDataTypeFlags.String,
            FieldSize = 10
        };
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.AddDataEntry(entry);

        // Assert
        resource.DataCount.Should().Be(1);
        resource.DataEntries.Should().Contain(entry);
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void AddDataEntry_WithNullEntry_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);

        // Act & Assert
        var act = () => resource.AddDataEntry(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveDataEntry_WithExistingEntry_ShouldRemoveFromCollection()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var entry = new DataEntry { Name = "TestEntry" };
        resource.AddDataEntry(entry);
        var changedCalled = false;

        // Reset the event handler to test removal
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        var result = resource.RemoveDataEntry(entry);

        // Assert
        result.Should().BeTrue();
        resource.DataCount.Should().Be(0);
        resource.DataEntries.Should().NotContain(entry);
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void RemoveDataEntry_WithNonExistentEntry_ShouldReturnFalse()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var entry = new DataEntry { Name = "TestEntry" };

        // Act
        var result = resource.RemoveDataEntry(entry);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AddStructureDefinition_WithValidStructure_ShouldAddToCollection()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var structure = new StructureDefinition
        {
            Name = "TestStructure",
            Size = 32
        };
        var changedCalled = false;
        resource.ResourceChanged += (_, _) => changedCalled = true;

        // Act
        resource.AddStructureDefinition(structure);

        // Assert
        resource.StructureCount.Should().Be(1);
        resource.StructureDefinitions.Should().Contain(structure);
        changedCalled.Should().BeTrue();
    }

    [Fact]
    public void AddStructureDefinition_WithNullStructure_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);

        // Act & Assert
        var act = () => resource.AddStructureDefinition(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FindStructureByName_WithExistingStructure_ShouldReturnStructure()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var structure = new StructureDefinition { Name = "TestStructure" };
        resource.AddStructureDefinition(structure);

        // Act
        var result = resource.FindStructureByName("TestStructure");

        // Assert
        result.Should().Be(structure);
    }

    [Fact]
    public void FindStructureByName_WithNonExistentStructure_ShouldReturnNull()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);

        // Act
        var result = resource.FindStructureByName("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindDataEntriesByName_WithExistingEntries_ShouldReturnMatchingEntries()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var entry1 = new DataEntry { Name = "TestEntry" };
        var entry2 = new DataEntry { Name = "TestEntry" };
        var entry3 = new DataEntry { Name = "OtherEntry" };
        resource.AddDataEntry(entry1);
        resource.AddDataEntry(entry2);
        resource.AddDataEntry(entry3);

        // Act
        var results = resource.FindDataEntriesByName("TestEntry").ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(entry1);
        results.Should().Contain(entry2);
        results.Should().NotContain(entry3);
    }

    [Fact]
    public void Clone_ShouldCreateIdenticalCopy()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        resource.Version = 0x200;
        resource.AddDataEntry(new DataEntry { Name = "TestEntry", DataType = FieldDataTypeFlags.String });

        // Act
        var clone = resource.Clone();

        // Assert
        clone.Should().NotBeSameAs(resource);
        clone.Should().BeOfType<DataResource>();
        var clonedResource = (DataResource)clone;
        clonedResource.Version.Should().Be(resource.Version);
        clonedResource.DataCount.Should().Be(resource.DataCount);
    }

    [Fact]
    public void Validate_WithValidResource_ShouldReturnTrueWithNoErrors()
    {
        // Arrange
        using var resource = new DataResource(_logger, _testKey);
        var entry = new DataEntry
        {
            Name = "TestEntry",
            DataType = FieldDataTypeFlags.String,
            FieldSize = 5
        };
        entry.SetFieldData("test");
        resource.AddDataEntry(entry);

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
        using var resource = new DataResource(_logger, _testKey);

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("DataResource");
    }
}
