using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Package;
using Xunit;

namespace TS4Tools.Resources.Utility.Tests;

/// <summary>
/// Basic integration tests for Phase 4.11 Utility Resource components
/// Full test suite will be implemented after API stabilizes
/// </summary>
public class BasicIntegrationTests
{
    private readonly ResourceKey _testKey = new(0x545AC67A, 0x12345678, 0x87654321);

    [Fact]
    public void ConfigResource_CanBeCreated()
    {
        // Arrange & Act
        using var resource = new ConfigResource(NullLogger<ConfigResource>.Instance, _testKey);

        // Assert
        resource.Should().NotBeNull();
        resource.ResourceKey.Should().Be(_testKey);
    }

    [Fact]
    public void DataResource_CanBeCreated()
    {
        // Arrange & Act
        using var resource = new DataResource(NullLogger<DataResource>.Instance, _testKey);

        // Assert
        resource.Should().NotBeNull();
        resource.ResourceKey.Should().Be(_testKey);
    }

    [Fact]
    public void MetadataResource_CanBeCreated()
    {
        // Arrange & Act
        using var resource = new MetadataResource(NullLogger<MetadataResource>.Instance, _testKey);

        // Assert
        resource.Should().NotBeNull();
        resource.ResourceKey.Should().Be(_testKey);
    }

    [Fact]
    public void DataEntry_CanBeCreated()
    {
        // Arrange & Act
        var entry = new DataEntry();

        // Assert
        entry.Should().NotBeNull();
        entry.Name.Should().Be(string.Empty);
    }

    [Fact]
    public void FieldDefinition_CanBeCreatedWithProperties()
    {
        // Arrange & Act
        var field = new FieldDefinition
        {
            Name = "TestField",
            DataType = FieldDataTypeFlags.UInt32,
            DataOffset = 100
        };

        // Assert
        field.Name.Should().Be("TestField");
        field.DataType.Should().Be(FieldDataTypeFlags.UInt32);
        field.DataOffset.Should().Be(100u);
    }

    [Fact]
    public void StructureDefinition_CanAddFields()
    {
        // Arrange
        var structure = new StructureDefinition();
        var field = new FieldDefinition
        {
            Name = "TestField",
            DataType = FieldDataTypeFlags.UInt32
        };

        // Act
        structure.Fields.Add(field);

        // Assert
        structure.Fields.Should().HaveCount(1);
        structure.Fields[0].Name.Should().Be("TestField");
        structure.Fields[0].DataType.Should().Be(FieldDataTypeFlags.UInt32);
    }

    [Fact]
    public void FieldDataTypeFlags_EnumValuesExist()
    {
        // Act & Assert - Verify key enum values exist
        FieldDataTypeFlags.UInt8.Should().BeDefined();
        FieldDataTypeFlags.UInt16.Should().BeDefined();
        FieldDataTypeFlags.UInt32.Should().BeDefined();
        FieldDataTypeFlags.UInt64.Should().BeDefined();
        FieldDataTypeFlags.Int8.Should().BeDefined();
        FieldDataTypeFlags.Int16.Should().BeDefined();
        FieldDataTypeFlags.Int32.Should().BeDefined();
        FieldDataTypeFlags.Int64.Should().BeDefined();
        FieldDataTypeFlags.Single.Should().BeDefined();
        FieldDataTypeFlags.Double.Should().BeDefined();
        FieldDataTypeFlags.Boolean.Should().BeDefined();
        FieldDataTypeFlags.String.Should().BeDefined();
    }
}
