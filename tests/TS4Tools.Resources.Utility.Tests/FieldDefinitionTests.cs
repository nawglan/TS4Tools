using FluentAssertions;
using Xunit;

namespace TS4Tools.Resources.Utility.Tests;

/// <summary>
/// Basic tests for FieldDefinition class - comprehensive tests will be added after API is stable
/// </summary>
public class FieldDefinitionTests
{
    [Fact]
    public void FieldDefinition_CanBeCreated()
    {
        // Arrange & Act
        var field = new FieldDefinition();

        // Assert
        field.Should().NotBeNull();
        field.Name.Should().Be(string.Empty);
    }

    [Fact]
    public void FieldDefinition_PropertiesCanBeSet()
    {
        // Arrange
        var field = new FieldDefinition();

        // Act
        field.Name = "TestField";
        field.DataType = FieldDataTypeFlags.UInt32;
        field.DataOffset = 100;

        // Assert
        field.Name.Should().Be("TestField");
        field.DataType.Should().Be(FieldDataTypeFlags.UInt32);
        field.DataOffset.Should().Be(100u);
    }

    [Fact]
    public void StructureDefinition_CanBeCreated()
    {
        // Arrange & Act
        var structure = new StructureDefinition();

        // Assert
        structure.Should().NotBeNull();
        structure.Name.Should().Be(string.Empty);
        structure.Fields.Should().NotBeNull();
        structure.Fields.Should().BeEmpty();
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
    }
}
