using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for MtnfData parsing and serialization.
/// </summary>
public class MtnfDataTests
{
    // Layouts follow legacy s4piRCOLChunks/MTNF.cs lines 63-88 (MTNF: tag, unknown1, data length, list),
    // MATD.cs lines 205-228 (MTRL: tag, unknown1, uint16, uint16, list) and ShaderData.cs lines 262-270,
    // 944-964 (list: count, 16-byte headers, data; offsets relative to the tag; data length = data bytes only).

    /// <summary>Creates a minimal MTNF block: no shader data, data length 0.</summary>
    private static byte[] CreateMtnfBlock()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write("MTNF"u8);
        writer.Write(0x12345678u); // Unknown1
        writer.Write(0u);          // data length
        writer.Write(0);           // shader data count
        return ms.ToArray();
    }

    /// <summary>Creates a minimal MTRL block (as embedded in MATD): two uint16 unknowns, no data length.</summary>
    private static byte[] CreateMtrlBlock()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write("MTRL"u8);
        writer.Write(0xAABBCCDDu);   // Unknown1
        writer.Write((ushort)0x0102); // Unknown2
        writer.Write((ushort)0x0304); // Unknown3
        writer.Write(0);             // shader data count
        return ms.ToArray();
    }

    /// <summary>Creates an MTNF block with one Float element.</summary>
    private static byte[] CreateMtnfWithFloatElement()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write("MTNF"u8);
        writer.Write(0u);  // Unknown1
        writer.Write(4u);  // data length: one float (headers are not counted)
        writer.Write(1);   // count
        writer.Write((uint)ShaderFieldType.Shininess);
        writer.Write((uint)ShaderDataType.Float);
        writer.Write(1);   // component count
        writer.Write(16u + 16u); // offset from the tag: 16-byte header + 16-byte element header
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
        mtnf.Unknown2.Should().Be(0x0102);
        mtnf.Unknown3.Should().Be(0x0304);
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
    public void MtnfData_Parse_TooShort_Throws()
    {
        // Legacy reads past the end and throws; the GEOM/MATD callers then keep the raw bytes.
        var data = new byte[] { (byte)'M', (byte)'T', (byte)'N', (byte)'F' };

        var action = () => new MtnfData(data);

        action.Should().Throw<InvalidDataException>();
        MtnfData.TryParse(data, out _).Should().BeFalse();
    }

    /// <summary>
    /// Regression (real data): every real GEOM MTNF and MATD MTRL block must re-serialize byte-identically.
    /// The old model treated MTRL's two uint16 unknowns as a data length, counted headers in the MTNF data
    /// length and resolved offsets from after the length field instead of from the tag.
    /// </summary>
    [Fact]
    public void MtnfData_Serialize_WithFloatElement_IsByteIdentical()
    {
        var data = CreateMtnfWithFloatElement();

        new MtnfData(data).Serialize().Should().Equal(data);
    }

    [Fact]
    public void MtnfData_Serialize_Mtrl_IsByteIdentical()
    {
        var data = CreateMtrlBlock();

        new MtnfData(data).Serialize().Should().Equal(data);
    }

    [Fact]
    public void MtnfData_Parse_DataLengthMismatch_Throws()
    {
        // Source: ShaderData.cs lines 953-954 - the MTNF data length must equal the element data size
        var data = CreateMtnfWithFloatElement();
        data[8] = 8;

        var action = () => new MtnfData(data);

        action.Should().Throw<InvalidDataException>();
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
