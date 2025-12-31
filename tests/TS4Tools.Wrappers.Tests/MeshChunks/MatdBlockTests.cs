using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for MatdBlock parsing and serialization.
/// </summary>
public class MatdBlockTests
{
    /// <summary>
    /// Creates a minimal MATD block with new format (version >= 0x103).
    /// </summary>
    private static byte[] CreateMatdNewFormat()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MATD
        writer.Write((byte)'M');
        writer.Write((byte)'A');
        writer.Write((byte)'T');
        writer.Write((byte)'D');

        // Header
        writer.Write(0x103u); // Version (new format)
        writer.Write(0x12345678u); // MaterialNameHash
        writer.Write((uint)MatdShaderType.Phong); // Shader

        // Create a minimal MTRL block
        var mtrlData = CreateMinimalMtrl();
        writer.Write((uint)mtrlData.Length); // MTRL length

        // New format flags
        writer.Write(1); // isVideoSurface (true)
        writer.Write(0); // isPaintingSurface (false)

        // MTRL data
        writer.Write(mtrlData);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a minimal MATD block with old format (version < 0x103).
    /// </summary>
    private static byte[] CreateMatdOldFormat()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MATD
        writer.Write((byte)'M');
        writer.Write((byte)'A');
        writer.Write((byte)'T');
        writer.Write((byte)'D');

        // Header
        writer.Write(0x102u); // Version (old format)
        writer.Write(0xAABBCCDDu); // MaterialNameHash
        writer.Write((uint)MatdShaderType.SimSkin); // Shader

        // Create a minimal MTRL block
        var mtrlData = CreateMinimalMtrl();
        writer.Write((uint)mtrlData.Length); // MTRL length

        // Old format has no video/painting flags
        // MTRL data immediately follows
        writer.Write(mtrlData);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a minimal MTRL block.
    /// </summary>
    private static byte[] CreateMinimalMtrl()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MTRL
        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'R');
        writer.Write((byte)'L');

        // Header
        writer.Write(0u); // Unknown1
        writer.Write((ushort)0); // Unknown2
        writer.Write((ushort)0); // Unknown3

        // ShaderData count = 0
        writer.Write(0); // No shader data entries

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a MATD block with a more complex MTRL containing shader data.
    /// </summary>
    private static byte[] CreateMatdWithShaderData()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MATD
        writer.Write((byte)'M');
        writer.Write((byte)'A');
        writer.Write((byte)'T');
        writer.Write((byte)'D');

        // Header
        writer.Write(0x103u); // Version
        writer.Write(0x11111111u); // MaterialNameHash
        writer.Write((uint)MatdShaderType.GlassForObjects); // Shader

        // Create MTRL with some shader data
        var mtrlData = CreateMtrlWithOneFloatEntry();
        writer.Write((uint)mtrlData.Length); // MTRL length

        // New format flags
        writer.Write(0); // isVideoSurface
        writer.Write(1); // isPaintingSurface

        // MTRL data
        writer.Write(mtrlData);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an MTRL block with one float shader data entry.
    /// </summary>
    private static byte[] CreateMtrlWithOneFloatEntry()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        long mtrlStart = ms.Position;

        // Tag: MTRL
        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'R');
        writer.Write((byte)'L');

        // Header (12 bytes)
        writer.Write(0u); // Unknown1
        writer.Write((ushort)0); // Unknown2
        writer.Write((ushort)0); // Unknown3

        // ShaderData count = 1
        writer.Write(1);

        // ShaderData header: field(4) + dataType(4) + count(4) + offset(4) = 16 bytes
        // Transparency field (0x05D22FD3), dtFloat (1), count 1, offset = header size
        writer.Write(0x05D22FD3u); // FieldType.Transparency
        writer.Write(1u); // DataType.dtFloat
        writer.Write(1); // count = 1
        writer.Write(32u); // offset from MTRL start (after header + entry header)

        // Float data at offset 32
        writer.Write(0.5f); // transparency value

        return ms.ToArray();
    }

    [Fact]
    public void MatdBlock_Parse_NewFormat_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMatdNewFormat();

        // Act
        var block = new MatdBlock(data);

        // Assert
        block.Tag.Should().Be("MATD");
        block.ResourceType.Should().Be(MatdBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x103);
        block.IsNewFormat.Should().BeTrue();
        block.MaterialNameHash.Should().Be(0x12345678u);
        block.Shader.Should().Be(MatdShaderType.Phong);
        block.IsVideoSurface.Should().BeTrue();
        block.IsPaintingSurface.Should().BeFalse();
        block.MtrlData.Should().NotBeEmpty();
    }

    [Fact]
    public void MatdBlock_Parse_OldFormat_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMatdOldFormat();

        // Act
        var block = new MatdBlock(data);

        // Assert
        block.Version.Should().Be(0x102);
        block.IsNewFormat.Should().BeFalse();
        block.MaterialNameHash.Should().Be(0xAABBCCDDu);
        block.Shader.Should().Be(MatdShaderType.SimSkin);
        block.IsVideoSurface.Should().BeFalse();
        block.IsPaintingSurface.Should().BeFalse();
        block.MtrlData.Should().NotBeEmpty();
    }

    [Fact]
    public void MatdBlock_Parse_WithShaderData_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMatdWithShaderData();

        // Act
        var block = new MatdBlock(data);

        // Assert
        block.Shader.Should().Be(MatdShaderType.GlassForObjects);
        block.IsVideoSurface.Should().BeFalse();
        block.IsPaintingSurface.Should().BeTrue();
        block.MtrlData.Should().NotBeEmpty();
    }

    [Fact]
    public void MatdBlock_Serialize_NewFormat_RoundTrips()
    {
        // Arrange
        var originalData = CreateMatdNewFormat();
        var block = new MatdBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MatdBlock_Serialize_OldFormat_RoundTrips()
    {
        // Arrange
        var originalData = CreateMatdOldFormat();
        var block = new MatdBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MatdBlock_Serialize_WithShaderData_RoundTrips()
    {
        // Arrange
        var originalData = CreateMatdWithShaderData();
        var block = new MatdBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MatdBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = CreateMatdNewFormat();
        data[0] = (byte)'X'; // Corrupt the tag

        // Act & Assert
        var action = () => new MatdBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid MATD tag*");
    }

    [Fact]
    public void MatdBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(MatdBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("MATD").Should().BeTrue();
    }

    [Fact]
    public void MatdBlock_Registry_CreatesMatdBlock()
    {
        // Arrange
        var data = CreateMatdNewFormat();

        // Act
        var block = RcolBlockRegistry.CreateBlock(MatdBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<MatdBlock>();
    }

    [Fact]
    public void MatdShaderType_HasCorrectValues()
    {
        // Assert - verify some key values match legacy
        ((uint)MatdShaderType.None).Should().Be(0x00000000);
        ((uint)MatdShaderType.Phong).Should().Be(0xB9105A6D);
        ((uint)MatdShaderType.SimSkin).Should().Be(0x548394B9);
        ((uint)MatdShaderType.SimHair).Should().Be(0x84FD7152);
        ((uint)MatdShaderType.GlassForObjects).Should().Be(0x492ECA7C);
    }
}
