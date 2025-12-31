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
    /// Creates a minimal MTNF block with MTNF tag.
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

        // Shader data length
        byte[] shaderData = [0x01, 0x02, 0x03, 0x04, 0x05];
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
    /// Creates an MTNF block with shader data.
    /// </summary>
    private static byte[] CreateMtnfWithShaderData()
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

        // Shader data (simulated)
        byte[] shaderData = new byte[32];
        for (int i = 0; i < shaderData.Length; i++)
            shaderData[i] = (byte)i;

        writer.Write((uint)shaderData.Length);
        writer.Write(shaderData);

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
        mtnf.ShaderData.Should().HaveCount(5);
        mtnf.ShaderData.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
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
    public void MtnfData_Parse_WithShaderData_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMtnfWithShaderData();

        // Act
        var mtnf = new MtnfData(data);

        // Assert
        mtnf.ShaderData.Should().HaveCount(32);
        mtnf.ShaderData[0].Should().Be(0);
        mtnf.ShaderData[31].Should().Be(31);
    }

    [Fact]
    public void MtnfData_Serialize_MtnfTag_RoundTrips()
    {
        // Arrange
        var originalData = CreateMtnfBlock();
        var mtnf = new MtnfData(originalData);

        // Act
        var serialized = mtnf.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MtnfData_Serialize_MtrlTag_RoundTrips()
    {
        // Arrange
        var originalData = CreateMtrlBlock();
        var mtnf = new MtnfData(originalData);

        // Act
        var serialized = mtnf.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MtnfData_Serialize_WithShaderData_RoundTrips()
    {
        // Arrange
        var originalData = CreateMtnfWithShaderData();
        var mtnf = new MtnfData(originalData);

        // Act
        var serialized = mtnf.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
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
    public void MtnfData_Parse_TooShort_ThrowsException()
    {
        // Arrange
        var data = new byte[] { (byte)'M', (byte)'T', (byte)'N', (byte)'F' };

        // Act & Assert
        var action = () => new MtnfData(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*too short*");
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
        // Arrange
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

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
    public void MtnfData_Equals_IdenticalData_ReturnsTrue()
    {
        // Arrange
        var data = CreateMtnfBlock();
        var mtnf1 = new MtnfData(data);
        var mtnf2 = new MtnfData(data);

        // Assert
        mtnf1.Equals(mtnf2).Should().BeTrue();
    }

    [Fact]
    public void MtnfData_Equals_DifferentUnknown1_ReturnsFalse()
    {
        // Arrange
        var mtnf1 = new MtnfData(CreateMtnfBlock());
        var mtnf2 = new MtnfData(CreateMtrlBlock());

        // Assert
        mtnf1.Equals(mtnf2).Should().BeFalse();
    }
}
