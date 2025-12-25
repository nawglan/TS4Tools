using FluentAssertions;
using TS4Tools.Wrappers;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

public class SimDataResourceTests
{
    private static readonly ResourceKey TestKey = new(0x545AC67A, 0, 0);

    // Minimal valid SimData header
    private static readonly byte[] MinimalSimData =
    [
        // Magic "DATA"
        0x44, 0x41, 0x54, 0x41,
        // Version (0x100)
        0x00, 0x01, 0x00, 0x00,
        // Table offset
        0x20, 0x00, 0x00, 0x00,
        // Schema count (1)
        0x01, 0x00, 0x00, 0x00,
        // Additional header bytes (padding to 32 bytes)
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00
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

        resource.TableOffset.Should().Be(0x20);
    }

    [Fact]
    public void Parse_ValidHeader_ReadsSchemaCount()
    {
        var resource = new SimDataResource(TestKey, MinimalSimData);

        resource.SchemaCount.Should().Be(1);
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
        // Create SimData with version 0x101 which has schema parsing support
        // Need enough data for schema offset to be valid (>= offset value)
        var data = new byte[]
        {
            // Magic "DATA"
            0x44, 0x41, 0x54, 0x41,
            // Version (0x101)
            0x01, 0x01, 0x00, 0x00,
            // Table offset
            0x20, 0x00, 0x00, 0x00,
            // Schema count (2)
            0x02, 0x00, 0x00, 0x00,
            // Schema offset (pointing to byte 32, need data to extend past this)
            0x20, 0x00, 0x00, 0x00,
            // Table count
            0x01, 0x00, 0x00, 0x00,
            // Padding to reach offset 32
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            // Additional data so schemaOffset (32) < data.Length (40)
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        var resource = new SimDataResource(TestKey, data);

        // Schema count should be read from header
        resource.SchemaCount.Should().Be(2);
        // Schema list will have placeholder entries
        resource.Schemas.Count.Should().Be(2);
    }

    [Fact]
    public void SimDataSchema_HasExpectedProperties()
    {
        var schema = new SimDataSchema
        {
            Index = 0,
            Name = "TestSchema",
            NameHash = 0x12345678,
            ColumnCount = 5
        };

        schema.Index.Should().Be(0);
        schema.Name.Should().Be("TestSchema");
        schema.NameHash.Should().Be(0x12345678);
        schema.ColumnCount.Should().Be(5);
    }
}
