using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TS4Tools.Core.Resources;
using Xunit;

namespace TS4Tools.Resources.Utility.Tests;

public class DataEntryTests
{

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var entry = new DataEntry();

        // Assert
        entry.Name.Should().Be(string.Empty);
        entry.NameHash.Should().Be(0);
        entry.Structure.Should().BeNull();
        entry.DataType.Should().Be(FieldDataTypeFlags.Unknown);
        entry.FieldSize.Should().Be(0);
        entry.FieldCount.Should().Be(0);
        entry.FieldData.Should().BeEmpty();
        entry.IsNull.Should().BeFalse();
    }

    [Fact]
    public void SetFieldData_WithString_ShouldSetCorrectData()
    {
        // Arrange
        var entry = new DataEntry();
        var testValue = "Hello, World!";

        // Act
        entry.SetFieldData(testValue);

        // Assert
        entry.DataType.Should().Be(FieldDataTypeFlags.String);
        entry.FieldSize.Should().Be((uint)(testValue.Length + 1)); // +1 for null terminator
        entry.IsNull.Should().BeFalse();
        entry.FieldData.Should().NotBeEmpty();
    }

    [Fact]
    public void SetFieldData_WithEmptyString_ShouldSetAsNull()
    {
        // Arrange
        var entry = new DataEntry();

        // Act
        entry.SetFieldData("");

        // Assert
        entry.DataType.Should().Be(FieldDataTypeFlags.String);
        entry.FieldSize.Should().Be(0);
        entry.IsNull.Should().BeTrue();
        entry.FieldData.Should().BeEmpty();
    }

    [Fact]
    public void SetFieldData_WithInteger_ShouldSetCorrectData()
    {
        // Arrange
        var entry = new DataEntry();
        var testValue = 12345;

        // Act
        entry.SetFieldData(testValue);

        // Assert
        entry.DataType.Should().Be(FieldDataTypeFlags.Int32);
        entry.FieldSize.Should().Be(4); // 4 bytes for int32
        entry.IsNull.Should().BeFalse();
        entry.FieldData.Should().HaveCount(4);
    }

    [Fact]
    public void SetFieldData_WithFloat_ShouldSetCorrectData()
    {
        // Arrange
        var entry = new DataEntry();
        var testValue = 3.14159f;

        // Act
        entry.SetFieldData(testValue);

        // Assert
        entry.DataType.Should().Be(FieldDataTypeFlags.Single);
        entry.FieldSize.Should().Be(4); // 4 bytes for float
        entry.IsNull.Should().BeFalse();
        entry.FieldData.Should().HaveCount(4);
    }

    [Fact]
    public void SetFieldData_WithBoolean_ShouldSetCorrectData()
    {
        // Arrange
        var entry = new DataEntry();
        var testValue = true;

        // Act
        entry.SetFieldData(testValue);

        // Assert
        entry.DataType.Should().Be(FieldDataTypeFlags.Boolean);
        entry.FieldSize.Should().Be(1); // 1 byte for boolean
        entry.IsNull.Should().BeFalse();
        entry.FieldData.Should().HaveCount(1);
    }

    [Fact]
    public void GetFieldDataAs_WithValidIntegerData_ShouldReturnCorrectValue()
    {
        // Arrange
        var entry = new DataEntry();
        var testValue = 12345;
        entry.SetFieldData(testValue);

        // Act
        var result = entry.GetFieldDataAs<int>();

        // Assert
        result.Should().Be(testValue);
    }

    [Fact]
    public void GetFieldDataAs_WithValidFloatData_ShouldReturnCorrectValue()
    {
        // Arrange
        var entry = new DataEntry();
        var testValue = 3.14159f;
        entry.SetFieldData(testValue);

        // Act
        var result = entry.GetFieldDataAs<float>();

        // Assert
        result.Should().BeApproximately(testValue, 0.0001f);
    }

    [Fact]
    public void GetFieldDataAs_WithValidBooleanData_ShouldReturnCorrectValue()
    {
        // Arrange
        var entry = new DataEntry();
        var testValue = true;
        entry.SetFieldData(testValue);

        // Act
        var result = entry.GetFieldDataAs<bool>();

        // Assert
        result.Should().Be(testValue);
    }

    [Fact]
    public void GetFieldDataAs_WithEmptyData_ShouldReturnNull()
    {
        // Arrange
        var entry = new DataEntry();

        // Act
        var result = entry.GetFieldDataAs<int>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetFieldDataAsString_WithValidStringData_ShouldReturnCorrectValue()
    {
        // Arrange
        var entry = new DataEntry();
        var testValue = "Hello, World!";
        entry.SetFieldData(testValue);

        // Act
        var result = entry.GetFieldDataAsString();

        // Assert
        result.Should().Be(testValue);
    }

    [Fact]
    public void GetFieldDataAsString_WithEmptyData_ShouldReturnNull()
    {
        // Arrange
        var entry = new DataEntry();

        // Act
        var result = entry.GetFieldDataAsString();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidEntry_ShouldReturnTrue()
    {
        // Arrange
        var entry = new DataEntry
        {
            Name = "TestEntry",
            DataType = FieldDataTypeFlags.String,
            FieldSize = 5
        };
        entry.SetFieldData("test");

        // Act
        var isValid = entry.Validate(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldReturnFalse()
    {
        // Arrange
        var entry = new DataEntry
        {
            Name = "",
            DataType = FieldDataTypeFlags.String,
            FieldSize = 5
        };

        // Act
        var isValid = entry.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Data entry name is null or empty");
    }

    [Fact]
    public void Validate_WithMismatchedFieldSizeAndData_ShouldReturnFalse()
    {
        // Arrange
        var entry = new DataEntry
        {
            Name = "TestEntry",
            DataType = FieldDataTypeFlags.String,
            FieldSize = 10,
            FieldData = Array.Empty<byte>(),
            IsNull = false
        };

        // Act
        var isValid = entry.Validate(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Field size is 10 but no field data is present");
    }

    [Theory]
    [InlineData(FieldDataTypeFlags.UInt8)]
    [InlineData(FieldDataTypeFlags.Int32)]
    [InlineData(FieldDataTypeFlags.String)]
    [InlineData(FieldDataTypeFlags.Boolean)]
    [InlineData(FieldDataTypeFlags.Single)]
    public void DataType_WithVariousTypes_ShouldSetCorrectly(FieldDataTypeFlags dataType)
    {
        // Arrange
        var entry = new DataEntry();

        // Act
        entry.DataType = dataType;

        // Assert
        entry.DataType.Should().Be(dataType);
    }

    [Fact]
    public void ToString_ShouldReturnDescriptiveString()
    {
        // Arrange
        var entry = new DataEntry
        {
            Name = "TestEntry",
            DataType = FieldDataTypeFlags.String,
            FieldSize = 10,
            FieldCount = 1
        };

        // Act
        var result = entry.ToString();

        // Assert
        result.Should().Contain("TestEntry");
        result.Should().Contain("String");
        result.Should().Contain("10");
        result.Should().Contain("1");
    }

    [Fact]
    public void WriteTo_ShouldWriteCorrectData()
    {
        // Arrange
        var entry = new DataEntry
        {
            Name = "TestEntry",
            NameHash = 0x12345678,
            DataType = FieldDataTypeFlags.String,
            FieldSize = 10,
            FieldCount = 1
        };

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Act
        entry.WriteTo(writer);

        // Assert
        stream.Length.Should().Be(28); // 7 uint32 values = 28 bytes
        entry.Position.Should().Be(0);
    }

    [Fact]
    public void UpdateOffsets_ShouldUpdateCorrectly()
    {
        // Arrange
        var entry = new DataEntry
        {
            Position = 100,
            NamePosition = 200,
            StructurePosition = 300,
            FieldPosition = 400
        };

        using var stream = new MemoryStream(new byte[1000]);
        using var writer = new BinaryWriter(stream);

        // Act
        entry.UpdateOffsets(writer);

        // Assert - method should complete without throwing
        stream.Position.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(12345)]
    [InlineData(-67890)]
    [InlineData(0)]
    public void SetFieldData_WithVariousIntegers_ShouldSetCorrectly(int value)
    {
        // Arrange
        var entry = new DataEntry();

        // Act
        entry.SetFieldData(value);

        // Assert
        entry.GetFieldDataAs<int>().Should().Be(value);
        entry.DataType.Should().Be(FieldDataTypeFlags.Int32);
    }

    [Theory]
    [InlineData(3.14159f)]
    [InlineData(-2.71828f)]
    [InlineData(0.0f)]
    public void SetFieldData_WithVariousFloats_ShouldSetCorrectly(float value)
    {
        // Arrange
        var entry = new DataEntry();

        // Act
        entry.SetFieldData(value);

        // Assert
        entry.GetFieldDataAs<float>().Should().BeApproximately(value, 0.0001f);
        entry.DataType.Should().Be(FieldDataTypeFlags.Single);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetFieldData_WithBooleans_ShouldSetCorrectly(bool value)
    {
        // Arrange
        var entry = new DataEntry();

        // Act
        entry.SetFieldData(value);

        // Assert
        entry.GetFieldDataAs<bool>().Should().Be(value);
        entry.DataType.Should().Be(FieldDataTypeFlags.Boolean);
    }
}
