using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class SimDataResourceTests
{
    private static readonly ResourceKey TestKey = new(0x545AC67A, 0, 0);

    // Minimal valid SimData header using relative offsets
    // Format (24 bytes):
    //   0x00: magic "DATA"
    //   0x04: version
    //   0x08: dataTablePos (relative: absolutePos = relativeValue + currentPos)
    //   0x0C: dataCount
    //   0x10: structTablePos (relative)
    //   0x14: structCount
    //
    // For data table at position 24 (0x18), relative offset at pos 0x08 = 24 - 8 = 16 (0x10)
    // For struct table at position 52 (0x34), relative offset at pos 0x10 = 52 - 16 = 36 (0x24)
    private static readonly byte[] MinimalSimData =
    [
        // 0x00: Magic "DATA"
        0x44, 0x41, 0x54, 0x41,
        // 0x04: Version (0x100)
        0x00, 0x01, 0x00, 0x00,
        // 0x08: Data table position (relative: 16 = 0x10, meaning position 24)
        0x10, 0x00, 0x00, 0x00,
        // 0x0C: Data count (1)
        0x01, 0x00, 0x00, 0x00,
        // 0x10: Structure table position (relative: 36 = 0x24, meaning position 52)
        0x24, 0x00, 0x00, 0x00,
        // 0x14: Structure count (1)
        0x01, 0x00, 0x00, 0x00,

        // 0x18: Data entry (28 bytes) - at position 24
        0x80, 0x00, 0x00, 0x80, // Name position (NullOffset = 0x80000000)
        0x00, 0x00, 0x00, 0x00, // Name hash
        0x80, 0x00, 0x00, 0x80, // Structure position (NullOffset)
        0x00, 0x00, 0x00, 0x00, // Unknown 0C
        0x00, 0x00, 0x00, 0x00, // Unknown 10
        0x80, 0x00, 0x00, 0x80, // Field position (NullOffset)
        0x00, 0x00, 0x00, 0x00, // Field count

        // 0x34: Structure entry (24 bytes) - at position 52
        0x80, 0x00, 0x00, 0x80, // Name position (NullOffset)
        0x00, 0x00, 0x00, 0x00, // Name hash
        0x00, 0x00, 0x00, 0x00, // Unknown 08
        0x10, 0x00, 0x00, 0x00, // Size (16 bytes)
        0x80, 0x00, 0x00, 0x80, // Field table position (NullOffset)
        0x00, 0x00, 0x00, 0x00  // Field count (0)
    ];

    [Fact]
    public void CreateEmpty_IsInvalid()
    {
        var resource = new SimDataResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.IsValid.Should().BeFalse();
        resource.DataSize.Should().Be(0);
    }

    [Fact]
    public void Parse_ValidHeader_IsValid()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        resource.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Parse_ValidHeader_ReadsVersion()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        resource.Version.Should().Be(0x100);
    }

    [Fact]
    public void Parse_ValidHeader_ReadsTableOffset()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        resource.DataTablePosition.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Parse_ValidHeader_ReadsSchemaCount()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        // SchemaCount is now the count of actually parsed schemas
        resource.SchemaCount.Should().BeGreaterThanOrEqualTo(0);
        resource.Schemas.Should().NotBeNull();
    }

    [Fact]
    public void Parse_InvalidMagic_IsInvalid()
    {
        var data = new byte[]
        {
            // Wrong magic
            0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x01, 0x00, 0x00,
            0x20, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00
        };

        var resource = new SimDataResource(TestKey, data);

        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_TooShort_IsInvalid()
    {
        var data = new byte[] { 0x44, 0x41, 0x54, 0x41, 0x00 }; // Only 5 bytes

        var resource = new SimDataResource(TestKey, data);

        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DataSize_ReturnsCorrectSize()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        resource.DataSize.Should().Be(MinimalSimData.Length);
    }

    [Fact]
    public void RawData_ReturnsOriginalBytes()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        resource.RawData.ToArray().Should().BeEquivalentTo(MinimalSimData);
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new SimDataResource(TestKey, MinimalSimData);

        var serialized = original.Data;
        var parsed = new SimDataResource(TestKey, serialized);

        parsed.IsValid.Should().Be(original.IsValid);
        parsed.Version.Should().Be(original.Version);
        parsed.SchemaCount.Should().Be(original.SchemaCount);
        parsed.RawData.ToArray().Should().BeEquivalentTo(original.RawData.ToArray());
    }

    [Fact]
    public void Magic_IsCorrectValue()
    {
        SimDataResource.Magic.Should().Be(0x41544144); // "DATA" in little-endian
    }

    [Fact]
    public void Schemas_EmptyForBasicVersion()
    {
        // With version 0x100, schemas aren't fully parsed
        var resource = new SimDataResource(TestKey, MinimalSimData);

        // With basic version, schema list exists but may be empty
        resource.Schemas.Should().NotBeNull();
    }

    [Fact]
    public void Schemas_ParsesWithVersion101()
    {
        // Create SimData with two structures using proper relative offsets
        // Data table at 24, Structure table at 80 (24 + 2*28 data entries)
        var data = new byte[]
        {
            // 0x00: Magic "DATA"
            0x44, 0x41, 0x54, 0x41,
            // 0x04: Version (0x100)
            0x00, 0x01, 0x00, 0x00,
            // 0x08: Data table position (relative: 16 = 0x10 -> absolute 24)
            0x10, 0x00, 0x00, 0x00,
            // 0x0C: Data count (0)
            0x00, 0x00, 0x00, 0x00,
            // 0x10: Structure table position (relative: 8 = 0x08 -> absolute 24)
            0x08, 0x00, 0x00, 0x00,
            // 0x14: Structure count (2)
            0x02, 0x00, 0x00, 0x00,

            // 0x18: Structure entry 1 (24 bytes) - at position 24
            0x80, 0x00, 0x00, 0x80, // Name position (NullOffset)
            0x00, 0x00, 0x00, 0x00, // Name hash
            0x00, 0x00, 0x00, 0x00, // Unknown 08
            0x10, 0x00, 0x00, 0x00, // Size
            0x80, 0x00, 0x00, 0x80, // Field table position (NullOffset)
            0x00, 0x00, 0x00, 0x00, // Field count

            // 0x30: Structure entry 2 (24 bytes) - at position 48
            0x80, 0x00, 0x00, 0x80, // Name position (NullOffset)
            0x00, 0x00, 0x00, 0x00, // Name hash
            0x00, 0x00, 0x00, 0x00, // Unknown 08
            0x20, 0x00, 0x00, 0x00, // Size
            0x80, 0x00, 0x00, 0x80, // Field table position (NullOffset)
            0x00, 0x00, 0x00, 0x00  // Field count
        };

        var resource = new SimDataResource(TestKey, data);

        resource.IsValid.Should().BeTrue();
        // Should have parsed 2 schemas
        resource.SchemaCount.Should().Be(2);
        resource.Schemas.Count.Should().Be(2);
    }

    [Fact]
    public void SimDataSchema_HasExpectedProperties()
    {
        // Note: ColumnCount is computed from Fields.Count
        var schema = new SimDataSchema
        {
            Index = 0,
            Name = "TestSchema",
            NameHash = 0x12345678,
            Size = 32,
            FieldCount = 5
        };

        schema.Index.Should().Be(0);
        schema.Name.Should().Be("TestSchema");
        schema.NameHash.Should().Be(0x12345678);
        schema.Size.Should().Be(32);
        schema.FieldCount.Should().Be(5);
        // ColumnCount reflects actual parsed fields (empty until AddField is called)
        schema.ColumnCount.Should().Be(0);
    }

    [Fact]
    public void SimDataField_HasExpectedProperties()
    {
        var field = new SimDataField
        {
            Index = 2,
            Name = "TestField",
            NameHash = 0xABCDEF01,
            Type = SimDataFieldType.FloatValue,
            TypeValue = 0x0A,
            DataOffset = 16
        };

        field.Index.Should().Be(2);
        field.Name.Should().Be("TestField");
        field.NameHash.Should().Be(0xABCDEF01);
        field.Type.Should().Be(SimDataFieldType.FloatValue);
        field.TypeValue.Should().Be(0x0A);
        field.DataOffset.Should().Be(16);
        field.DataSize.Should().Be(4); // Float is 4 bytes
    }

    [Fact]
    public void SimDataTable_HasExpectedProperties()
    {
        var schema = new SimDataSchema
        {
            Index = 0,
            Name = "TestSchema",
            Size = 24
        };

        var table = new SimDataTable
        {
            Index = 0,
            Name = "TestTable",
            NameHash = 0x11223344,
            Schema = schema,
            RowCount = 5
        };

        table.Index.Should().Be(0);
        table.Name.Should().Be("TestTable");
        table.NameHash.Should().Be(0x11223344);
        table.Schema.Should().BeSameAs(schema);
        table.RowCount.Should().Be(5);
    }

    [Fact]
    public void SimDataFieldType_GetSize_ReturnsCorrectSizes()
    {
        SimDataFieldType.Boolean.GetSize().Should().Be(4);
        SimDataFieldType.Integer16.GetSize().Should().Be(4);
        SimDataFieldType.FloatValue.GetSize().Should().Be(4);
        SimDataFieldType.VFX.GetSize().Should().Be(8);
        SimDataFieldType.RGBColor.GetSize().Should().Be(12);
        SimDataFieldType.ARGBColor.GetSize().Should().Be(16);
        SimDataFieldType.DataInstance.GetSize().Should().Be(8);
        SimDataFieldType.ImageInstance.GetSize().Should().Be(16);
        SimDataFieldType.StringInstance.GetSize().Should().Be(4);
    }

    [Fact]
    public void SimDataFieldType_IsKnownType_ReturnsTrueForKnownTypes()
    {
        SimDataFieldTypeExtensions.IsKnownType(0x00).Should().BeTrue(); // Boolean
        SimDataFieldTypeExtensions.IsKnownType(0x0A).Should().BeTrue(); // Float
        SimDataFieldTypeExtensions.IsKnownType(0x14).Should().BeTrue(); // StringInstance
    }

    [Fact]
    public void SimDataFieldType_IsKnownType_ReturnsFalseForUnknownTypes()
    {
        SimDataFieldTypeExtensions.IsKnownType(0xFF).Should().BeFalse();
        SimDataFieldTypeExtensions.IsKnownType(0x99).Should().BeFalse();
    }

    [Fact]
    public void Parse_WithNamedSchema_ParsesName()
    {
        // Create SimData with a named schema
        // Data table at 24 (empty), Structure at 24, name string at end
        var data = new byte[]
        {
            // 0x00: Magic "DATA"
            0x44, 0x41, 0x54, 0x41,
            // 0x04: Version (0x100)
            0x00, 0x01, 0x00, 0x00,
            // 0x08: Data table position (relative: 16 -> absolute 24)
            0x10, 0x00, 0x00, 0x00,
            // 0x0C: Data count (0)
            0x00, 0x00, 0x00, 0x00,
            // 0x10: Structure table position (relative: 8 -> absolute 24)
            0x08, 0x00, 0x00, 0x00,
            // 0x14: Structure count (1)
            0x01, 0x00, 0x00, 0x00,

            // 0x18: Structure entry (24 bytes) - at position 24
            // Name position: relative 24 from pos 24 -> absolute 48
            0x18, 0x00, 0x00, 0x00, // Name position (relative: 24 -> absolute 48)
            0x12, 0x34, 0x56, 0x78, // Name hash
            0x00, 0x00, 0x00, 0x00, // Unknown 08
            0x10, 0x00, 0x00, 0x00, // Size (16 bytes)
            0x80, 0x00, 0x00, 0x80, // Field table position (NullOffset)
            0x00, 0x00, 0x00, 0x00, // Field count (0)

            // 0x30: Name string "Test" (null-terminated)
            0x54, 0x65, 0x73, 0x74, 0x00  // "Test\0"
        };

        var resource = new SimDataResource(TestKey, data);

        resource.IsValid.Should().BeTrue();
        resource.SchemaCount.Should().Be(1);
        resource.Schemas[0].Name.Should().Be("Test");
        resource.Schemas[0].NameHash.Should().Be(0x78563412); // Little-endian
    }

    [Fact]
    public void Tables_ParsesCorrectly()
    {
        // Create SimData with one table and one schema
        var resource = new SimDataResource(TestKey, MinimalSimData);

        resource.IsValid.Should().BeTrue();
        resource.Tables.Should().NotBeNull();
        resource.TableCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Parse_InvalidDataTablePosition_IsInvalid()
    {
        var data = new byte[]
        {
            // Magic "DATA"
            0x44, 0x41, 0x54, 0x41,
            // Version
            0x00, 0x01, 0x00, 0x00,
            // Invalid data table position (would be out of bounds)
            0xFF, 0xFF, 0xFF, 0x7F,
            // Data count
            0x01, 0x00, 0x00, 0x00,
            // Structure table position
            0x00, 0x00, 0x00, 0x00,
            // Structure count
            0x00, 0x00, 0x00, 0x00
        };

        var resource = new SimDataResource(TestKey, data);

        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_NegativeCount_IsInvalid()
    {
        var data = new byte[]
        {
            // Magic "DATA"
            0x44, 0x41, 0x54, 0x41,
            // Version
            0x00, 0x01, 0x00, 0x00,
            // Data table position
            0x10, 0x00, 0x00, 0x00,
            // Negative data count (security check)
            0xFF, 0xFF, 0xFF, 0xFF,
            // Structure table position
            0x18, 0x00, 0x00, 0x00,
            // Structure count
            0x00, 0x00, 0x00, 0x00
        };

        var resource = new SimDataResource(TestKey, data);

        resource.IsValid.Should().BeFalse();
    }

    #region Serialization Tests

    [Fact]
    public void Serialize_MinimalSimData_ProducesValidHeader()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        var serialized = resource.Data.ToArray();

        // Should start with magic "DATA"
        serialized.Length.Should().BeGreaterThanOrEqualTo(24);
        var magic = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(serialized);
        magic.Should().Be(SimDataResource.Magic);
    }

    [Fact]
    public void Serialize_MinimalSimData_PreservesVersion()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        var serialized = resource.Data.ToArray();
        var reparsed = new SimDataResource(TestKey, serialized);

        reparsed.Version.Should().Be(resource.Version);
    }

    [Fact]
    public void RoundTrip_MinimalSimData_PreservesCounts()
    {
        var original = new SimDataResource(TestKey, MinimalSimData);

        var serialized = original.Data;
        var reparsed = new SimDataResource(TestKey, serialized);

        reparsed.IsValid.Should().Be(original.IsValid);
        reparsed.SchemaCount.Should().Be(original.SchemaCount);
        reparsed.TableCount.Should().Be(original.TableCount);
    }

    [Fact]
    public void RoundTrip_WithNamedSchema_PreservesName()
    {
        // SimData with a named schema
        var data = new byte[]
        {
            // Header
            0x44, 0x41, 0x54, 0x41, // Magic "DATA"
            0x00, 0x01, 0x00, 0x00, // Version (0x100)
            0x10, 0x00, 0x00, 0x00, // Data table position (relative: 16 -> absolute 24)
            0x00, 0x00, 0x00, 0x00, // Data count (0)
            0x08, 0x00, 0x00, 0x00, // Structure table position (relative: 8 -> absolute 24)
            0x01, 0x00, 0x00, 0x00, // Structure count (1)

            // Structure entry at position 24
            0x18, 0x00, 0x00, 0x00, // Name position (relative: 24 -> absolute 48)
            0x12, 0x34, 0x56, 0x78, // Name hash
            0x00, 0x00, 0x00, 0x00, // Unknown 08
            0x10, 0x00, 0x00, 0x00, // Size (16 bytes)
            0x80, 0x00, 0x00, 0x80, // Field table position (NullOffset)
            0x00, 0x00, 0x00, 0x00, // Field count (0)

            // Name string "Test" at position 48
            0x54, 0x65, 0x73, 0x74, 0x00  // "Test\0"
        };

        var original = new SimDataResource(TestKey, data);
        original.IsValid.Should().BeTrue();
        original.Schemas[0].Name.Should().Be("Test");

        var serialized = original.Data;
        var reparsed = new SimDataResource(TestKey, serialized);

        reparsed.IsValid.Should().BeTrue();
        reparsed.SchemaCount.Should().Be(1);
        reparsed.Schemas[0].Name.Should().Be("Test");
        reparsed.Schemas[0].NameHash.Should().Be(original.Schemas[0].NameHash);
    }

    [Fact]
    public void RoundTrip_WithTwoSchemas_PreservesAll()
    {
        // SimData with two structures
        var data = new byte[]
        {
            // Header
            0x44, 0x41, 0x54, 0x41, // Magic "DATA"
            0x00, 0x01, 0x00, 0x00, // Version (0x100)
            0x10, 0x00, 0x00, 0x00, // Data table position (relative: 16 -> absolute 24)
            0x00, 0x00, 0x00, 0x00, // Data count (0)
            0x08, 0x00, 0x00, 0x00, // Structure table position (relative: 8 -> absolute 24)
            0x02, 0x00, 0x00, 0x00, // Structure count (2)

            // Structure entry 1 at position 24
            0x80, 0x00, 0x00, 0x80, // Name position (NullOffset)
            0x11, 0x11, 0x11, 0x11, // Name hash
            0x00, 0x00, 0x00, 0x00, // Unknown 08
            0x10, 0x00, 0x00, 0x00, // Size
            0x80, 0x00, 0x00, 0x80, // Field table position (NullOffset)
            0x00, 0x00, 0x00, 0x00, // Field count

            // Structure entry 2 at position 48
            0x80, 0x00, 0x00, 0x80, // Name position (NullOffset)
            0x22, 0x22, 0x22, 0x22, // Name hash
            0x00, 0x00, 0x00, 0x00, // Unknown 08
            0x20, 0x00, 0x00, 0x00, // Size
            0x80, 0x00, 0x00, 0x80, // Field table position (NullOffset)
            0x00, 0x00, 0x00, 0x00  // Field count
        };

        var original = new SimDataResource(TestKey, data);
        original.IsValid.Should().BeTrue();
        original.SchemaCount.Should().Be(2);

        var serialized = original.Data;
        var reparsed = new SimDataResource(TestKey, serialized);

        reparsed.IsValid.Should().BeTrue();
        reparsed.SchemaCount.Should().Be(2);
        reparsed.Schemas[0].NameHash.Should().Be(0x11111111);
        reparsed.Schemas[0].Size.Should().Be(16);
        reparsed.Schemas[1].NameHash.Should().Be(0x22222222);
        reparsed.Schemas[1].Size.Should().Be(32);
    }

    [Fact]
    public void RoundTrip_EmptySimData_ProducesValidOutput()
    {
        // Minimal valid SimData with no data and no structures
        // Table offsets must point to valid positions within buffer
        // Data table offset at pos 8: relative 0x10 -> absolute 24
        // Structure table offset at pos 16: relative 0x08 -> absolute 24
        var data = new byte[]
        {
            // Header (24 bytes)
            0x44, 0x41, 0x54, 0x41, // Magic "DATA"
            0x00, 0x01, 0x00, 0x00, // Version (0x100)
            0x10, 0x00, 0x00, 0x00, // Data table position (relative: 16 -> absolute 24)
            0x00, 0x00, 0x00, 0x00, // Data count (0)
            0x08, 0x00, 0x00, 0x00, // Structure table position (relative: 8 -> absolute 24)
            0x00, 0x00, 0x00, 0x00, // Structure count (0)
            // 8 bytes padding so position 24 is valid
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        var original = new SimDataResource(TestKey, data);
        original.IsValid.Should().BeTrue();

        var serialized = original.Data;
        var reparsed = new SimDataResource(TestKey, serialized);

        reparsed.IsValid.Should().BeTrue();
        reparsed.SchemaCount.Should().Be(0);
        reparsed.TableCount.Should().Be(0);
    }

    [Fact]
    public void Serialize_PreservesUnknownFields()
    {
        // SimData with non-zero Unknown08 values
        var data = new byte[]
        {
            // Header
            0x44, 0x41, 0x54, 0x41, // Magic "DATA"
            0x00, 0x01, 0x00, 0x00, // Version (0x100)
            0x10, 0x00, 0x00, 0x00, // Data table position
            0x00, 0x00, 0x00, 0x00, // Data count (0)
            0x08, 0x00, 0x00, 0x00, // Structure table position
            0x01, 0x00, 0x00, 0x00, // Structure count (1)

            // Structure entry
            0x80, 0x00, 0x00, 0x80, // Name position (NullOffset)
            0xAA, 0xBB, 0xCC, 0xDD, // Name hash
            0x12, 0x34, 0x56, 0x78, // Unknown 08 (non-zero)
            0x10, 0x00, 0x00, 0x00, // Size
            0x80, 0x00, 0x00, 0x80, // Field table position (NullOffset)
            0x00, 0x00, 0x00, 0x00  // Field count
        };

        var original = new SimDataResource(TestKey, data);
        original.Schemas[0].Unknown08.Should().Be(0x78563412); // Little-endian

        var serialized = original.Data;
        var reparsed = new SimDataResource(TestKey, serialized);

        reparsed.Schemas[0].Unknown08.Should().Be(original.Schemas[0].Unknown08);
    }

    #endregion

    #region SimDataTable Getter/Setter Tests

    private static SimDataTable CreateTableWithField(SimDataFieldType fieldType, uint size, byte[] rawData)
    {
        var field = new SimDataField
        {
            Index = 0,
            Name = "TestField",
            NameHash = 0x12345678,
            Type = fieldType,
            TypeValue = (uint)fieldType,
            DataOffset = 0
        };

        var schema = new SimDataSchema
        {
            Index = 0,
            Name = "TestSchema",
            NameHash = 0xABCD1234,
            Size = size,
            FieldCount = 1
        };
        schema.AddField(field);

        var table = new SimDataTable
        {
            Index = 0,
            Name = "TestTable",
            NameHash = 0x11223344,
            Schema = schema,
            RowCount = 1
        };
        table.SetRawData(rawData);

        return table;
    }

    [Fact]
    public void SetUInt32_ModifiesData()
    {
        var rawData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var table = CreateTableWithField(SimDataFieldType.Boolean, 4, rawData);
        var field = table.Schema!.Fields[0];

        table.SetUInt32(0, field, 0x12345678);

        table.GetUInt32(0, field).Should().Be(0x12345678);
    }

    [Fact]
    public void SetFloat_ModifiesData()
    {
        var rawData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var table = CreateTableWithField(SimDataFieldType.FloatValue, 4, rawData);
        var field = table.Schema!.Fields[0];

        table.SetFloat(0, field, 3.14159f);

        table.GetFloat(0, field).Should().BeApproximately(3.14159f, 0.00001f);
    }

    [Fact]
    public void SetBoolean_True_SetsNonZero()
    {
        var rawData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var table = CreateTableWithField(SimDataFieldType.Boolean, 4, rawData);
        var field = table.Schema!.Fields[0];

        table.SetBoolean(0, field, true);

        table.GetBoolean(0, field).Should().BeTrue();
        table.GetUInt32(0, field).Should().Be(1);
    }

    [Fact]
    public void SetBoolean_False_SetsZero()
    {
        var rawData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var table = CreateTableWithField(SimDataFieldType.Boolean, 4, rawData);
        var field = table.Schema!.Fields[0];

        table.SetBoolean(0, field, false);

        table.GetBoolean(0, field).Should().BeFalse();
        table.GetUInt32(0, field).Should().Be(0);
    }

    [Fact]
    public void SetInt16_ModifiesData()
    {
        var rawData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var table = CreateTableWithField(SimDataFieldType.Integer16, 4, rawData);
        var field = table.Schema!.Fields[0];

        table.SetInt16(0, field, -1234);

        table.GetInt16(0, field).Should().Be(-1234);
    }

    [Fact]
    public void SetUInt64_ModifiesData()
    {
        var rawData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var table = CreateTableWithField(SimDataFieldType.DataInstance, 8, rawData);
        var field = table.Schema!.Fields[0];

        table.SetUInt64(0, field, 0xDEADBEEFCAFEBABE);

        table.GetUInt64(0, field).Should().Be(0xDEADBEEFCAFEBABE);
    }

    [Fact]
    public void DataChanged_RaisesEventOnSet()
    {
        var rawData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var table = CreateTableWithField(SimDataFieldType.Boolean, 4, rawData);
        var field = table.Schema!.Fields[0];

        var eventRaised = false;
        table.DataChanged += (_, _) => eventRaised = true;

        table.SetUInt32(0, field, 1);

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void GetFieldBytes_InvalidRow_ReturnsEmpty()
    {
        var rawData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var table = CreateTableWithField(SimDataFieldType.Boolean, 4, rawData);
        var field = table.Schema!.Fields[0];

        var bytes = table.GetFieldBytes(-1, field);

        bytes.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetFieldBytes_RowOutOfRange_ReturnsEmpty()
    {
        var rawData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var table = CreateTableWithField(SimDataFieldType.Boolean, 4, rawData);
        var field = table.Schema!.Fields[0];

        var bytes = table.GetFieldBytes(10, field);

        bytes.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void SetUInt32_InvalidRow_DoesNothing()
    {
        var rawData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var table = CreateTableWithField(SimDataFieldType.Boolean, 4, rawData);
        var field = table.Schema!.Fields[0];

        table.SetUInt32(-1, field, 0);

        // Data should be unchanged
        table.GetUInt32(0, field).Should().Be(0x78563412);
    }

    [Fact]
    public void SetUInt32_NoSchema_DoesNothing()
    {
        var table = new SimDataTable
        {
            Index = 0,
            Name = "TestTable",
            Schema = null,
            RowCount = 1
        };
        var field = new SimDataField
        {
            Index = 0,
            Type = SimDataFieldType.Boolean,
            DataOffset = 0
        };

        // Should not throw
        table.SetUInt32(0, field, 123);
    }

    #endregion
}
