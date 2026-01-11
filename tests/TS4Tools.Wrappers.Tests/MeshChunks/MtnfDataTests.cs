using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for MtnfData parsing and serialization.
/// </summary>
public class MtnfDataTests
{
    /// <summary>
    /// Creates a minimal MTNF block with MTNF tag (just header, no structured shader data).
    /// </summary>
    private static byte[] CreateMtnfBlock()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MTNF
        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'N');
        writer.Write((byte)'F');

        // Unknown1
        writer.Write(0x12345678u);

        // Shader data length (contains entry count + raw data)
        byte[] shaderData = [0x00, 0x00, 0x00, 0x00, 0x05]; // entry count = 0, then 1 byte raw
        writer.Write((uint)shaderData.Length);

        // Shader data
        writer.Write(shaderData);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a minimal MTRL block (alternative tag).
    /// </summary>
    private static byte[] CreateMtrlBlock()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MTRL
        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'R');
        writer.Write((byte)'L');

        // Unknown1
        writer.Write(0xAABBCCDDu);

        // Shader data length = 0
        writer.Write(0u);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an MTNF block with a valid Float shader element.
    /// </summary>
    private static byte[] CreateMtnfWithFloatElement()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MTNF
        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'N');
        writer.Write((byte)'F');

        // Unknown1
        writer.Write(0u);

        // We'll write data section, then calculate total length
        // Data section format:
        // - entry count (4 bytes)
        // - for each entry: field (4), type (4), count (4), offset (4) = 16 bytes
        // - then data values

        // Entry count = 1
        // Entry: field=Shininess (0xF755F7FF), type=Float (1), count=1, offset=20 (after header)
        // Data: 1 float = 4 bytes

        int headerSize = 4 + 16; // count + 1 entry header
        int dataSize = 4; // 1 float
        int totalSize = headerSize + dataSize;

        writer.Write((uint)totalSize); // Shader data length

        // Entry count
        writer.Write(1);

        // Entry header: field, type, count, offset
        writer.Write((uint)ShaderFieldType.Shininess); // 0xF755F7FF
        writer.Write((uint)ShaderDataType.Float);      // 1
        writer.Write(1);                                // count
        writer.Write((uint)headerSize);                 // offset to data

        // Data: float value
        writer.Write(0.75f);

        return ms.ToArray();
    }

    [Fact]
    public void MtnfData_Parse_MtnfTag_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMtnfBlock();

        // Act
        var mtnf = new MtnfData(data);

        // Assert
        mtnf.Tag.Should().Be("MTNF");
        mtnf.Unknown1.Should().Be(0x12345678u);
        // No structured elements parsed (the raw data doesn't follow element format)
        mtnf.ShaderData.Should().BeEmpty();
    }

    [Fact]
    public void MtnfData_Parse_MtrlTag_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMtrlBlock();

        // Act
        var mtnf = new MtnfData(data);

        // Assert
        mtnf.Tag.Should().Be("MTRL");
        mtnf.Unknown1.Should().Be(0xAABBCCDDu);
        mtnf.ShaderData.Should().BeEmpty();
    }

    [Fact]
    public void MtnfData_Parse_WithFloatElement_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMtnfWithFloatElement();

        // Act
        var mtnf = new MtnfData(data);

        // Assert
        mtnf.ShaderData.Should().HaveCount(1);
        mtnf.ShaderData[0].Should().BeOfType<ShaderFloat>();
        var shaderFloat = (ShaderFloat)mtnf.ShaderData[0];
        shaderFloat.Field.Should().Be(ShaderFieldType.Shininess);
        shaderFloat.Value.Should().BeApproximately(0.75f, 0.001f);
    }

    [Fact]
    public void MtnfData_Serialize_MtnfTag_RoundTrips()
    {
        // Arrange - MTRL with no data (simple round-trip case)
        var originalData = CreateMtrlBlock();
        var mtnf = new MtnfData(originalData);

        // Act
        var serialized = mtnf.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MtnfData_Serialize_WithFloatElement_RoundTrips()
    {
        // Arrange
        var originalData = CreateMtnfWithFloatElement();
        var mtnf = new MtnfData(originalData);

        // Act
        var serialized = mtnf.Serialize();

        // Assert - verify we can re-parse and get same values
        var reparsed = new MtnfData(serialized);
        reparsed.Tag.Should().Be(mtnf.Tag);
        reparsed.Unknown1.Should().Be(mtnf.Unknown1);
        reparsed.ShaderData.Should().HaveCount(1);
        var original = (ShaderFloat)mtnf.ShaderData[0];
        var result = (ShaderFloat)reparsed.ShaderData[0];
        result.Field.Should().Be(original.Field);
        result.Value.Should().BeApproximately(original.Value, 0.001f);
    }

    [Fact]
    public void MtnfData_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = new byte[] { (byte)'X', (byte)'X', (byte)'X', (byte)'X', 0, 0, 0, 0, 0, 0, 0, 0 };

        // Act & Assert
        var action = () => new MtnfData(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid MTNF tag*");
    }

    [Fact]
    public void MtnfData_Parse_TooShort_HandledGracefully()
    {
        // Arrange - data too short for full header
        var data = new byte[] { (byte)'M', (byte)'T', (byte)'N', (byte)'F' };

        // Act - should not throw, just preserve raw data
        var mtnf = new MtnfData(data);

        // Assert
        mtnf.ShaderData.Should().BeEmpty();
    }

    [Fact]
    public void MtnfData_TryParse_ValidData_ReturnsTrue()
    {
        // Arrange
        var data = CreateMtnfBlock();

        // Act
        var result = MtnfData.TryParse(data, out var mtnf);

        // Assert
        result.Should().BeTrue();
        mtnf.Should().NotBeNull();
        mtnf!.Tag.Should().Be("MTNF");
    }

    [Fact]
    public void MtnfData_TryParse_InvalidData_ReturnsFalse()
    {
        // Arrange - invalid tag
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0, 0, 0, 0, 0, 0, 0, 0 };

        // Act
        var result = MtnfData.TryParse(data, out var mtnf);

        // Assert
        result.Should().BeFalse();
        mtnf.Should().BeNull();
    }

    [Fact]
    public void MtnfData_IsValidTag_ValidMtnf_ReturnsTrue()
    {
        // Arrange
        var data = CreateMtnfBlock();

        // Act & Assert
        MtnfData.IsValidTag(data).Should().BeTrue();
    }

    [Fact]
    public void MtnfData_IsValidTag_ValidMtrl_ReturnsTrue()
    {
        // Arrange
        var data = CreateMtrlBlock();

        // Act & Assert
        MtnfData.IsValidTag(data).Should().BeTrue();
    }

    [Fact]
    public void MtnfData_IsValidTag_InvalidTag_ReturnsFalse()
    {
        // Arrange
        var data = new byte[] { (byte)'X', (byte)'X', (byte)'X', (byte)'X' };

        // Act & Assert
        MtnfData.IsValidTag(data).Should().BeFalse();
    }

    [Fact]
    public void MtnfData_IsValidTag_TooShort_ReturnsFalse()
    {
        // Arrange
        var data = new byte[] { (byte)'M', (byte)'T' };

        // Act & Assert
        MtnfData.IsValidTag(data).Should().BeFalse();
    }

    [Fact]
    public void MtnfData_GetFloat_ReturnsValue()
    {
        // Arrange
        var data = CreateMtnfWithFloatElement();
        var mtnf = new MtnfData(data);

        // Act
        var value = mtnf.GetFloat(ShaderFieldType.Shininess);

        // Assert
        value.Should().NotBeNull();
        value!.Value.Should().BeApproximately(0.75f, 0.001f);
    }

    [Fact]
    public void MtnfData_GetFloat_MissingField_ReturnsNull()
    {
        // Arrange
        var data = CreateMtnfWithFloatElement();
        var mtnf = new MtnfData(data);

        // Act
        var value = mtnf.GetFloat(ShaderFieldType.Transparency); // Not in data

        // Assert
        value.Should().BeNull();
    }
}
