using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for GeomBlock (Geometry) parsing and serialization.
/// </summary>
public class GeomBlockTests
{
    /// <summary>
    /// Creates a minimal GEOM block with version 5 and no shader data.
    /// </summary>
    private static byte[] CreateGeomBlockMinimal()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'G');
        writer.Write((byte)'E');
        writer.Write((byte)'O');
        writer.Write((byte)'M');

        // Version
        writer.Write(0x00000005u);

        // TGI offset (placeholder)
        writer.Write(0u);

        // TGI size
        writer.Write(0u);

        // Shader type (0 = no shader)
        writer.Write(0u);

        // MergeGroup
        writer.Write(0u);

        // SortOrder
        writer.Write(0u);

        // Vertex count
        writer.Write(42);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a GEOM block with version 12 (0x0C) and shader data.
    /// The parser reads shader type from offset 12, then MTNF size from offset 16.
    /// </summary>
    private static byte[] CreateGeomBlockWithShader()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag (bytes 0-3)
        writer.Write((byte)'G');
        writer.Write((byte)'E');
        writer.Write((byte)'O');
        writer.Write((byte)'M');

        // Version (bytes 4-7)
        writer.Write(0x0000000Cu);

        // TGI offset (bytes 8-11)
        writer.Write(0u);

        // Shader type at offset 12 (the parser reads shaderType from here)
        writer.Write(1u);

        // MTNF size at offset 16
        writer.Write(8u);

        // MTNF data (8 bytes placeholder)
        writer.Write(0L);

        // After MTNF data, position is 20 + 8 = 28
        // MergeGroup (bytes 28-31)
        writer.Write(0u);

        // SortOrder (bytes 32-35)
        writer.Write(0u);

        // Vertex count at pos + 8 = 28 + 8 = 36 (bytes 36-39)
        writer.Write(100);

        return ms.ToArray();
    }

    #region Parsing Tests

    [Fact]
    public void GeomBlock_Parse_MinimalBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateGeomBlockMinimal();

        // Act
        var block = new GeomBlock(data);

        // Assert
        block.Tag.Should().Be("GEOM");
        block.ResourceType.Should().Be(GeomBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(5);
        block.VertexCount.Should().Be(42);
    }

    [Fact]
    public void GeomBlock_Parse_WithShaderData_ParsesCorrectly()
    {
        // Arrange
        var data = CreateGeomBlockWithShader();

        // Act
        var block = new GeomBlock(data);

        // Assert
        block.Tag.Should().Be("GEOM");
        block.Version.Should().Be(0x0C);
        block.VertexCount.Should().Be(100);
    }

    [Fact]
    public void GeomBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'E', (byte)'O', (byte)'M', // Wrong tag
            0x05, 0x00, 0x00, 0x00, // Version
            0x00, 0x00, 0x00, 0x00, // TGI offset
            0x00, 0x00, 0x00, 0x00, // TGI size
            0x00, 0x00, 0x00, 0x00, // Shader type
            0x00, 0x00, 0x00, 0x00, // MergeGroup
            0x00, 0x00, 0x00, 0x00, // SortOrder
            0x00, 0x00, 0x00, 0x00  // Vertex count
        };

        // Act & Assert
        var action = () => new GeomBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid GEOM tag*");
    }

    [Fact]
    public void GeomBlock_TypeId_HasCorrectValue()
    {
        // Assert
        GeomBlock.TypeId.Should().Be(0x015A1849);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void GeomBlock_Serialize_MinimalBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateGeomBlockMinimal();
        var block = new GeomBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void GeomBlock_Serialize_WithShaderData_RoundTrips()
    {
        // Arrange
        var originalData = CreateGeomBlockWithShader();
        var block = new GeomBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void GeomBlock_Serialize_ParsedDataMatchesOriginal()
    {
        // Arrange
        var originalData = CreateGeomBlockMinimal();
        var block = new GeomBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new GeomBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
        reparsed.VertexCount.Should().Be(block.VertexCount);
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void GeomBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(GeomBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("GEOM").Should().BeTrue();
    }

    [Fact]
    public void GeomBlock_Registry_CreatesGeomBlock()
    {
        // Arrange
        var data = CreateGeomBlockMinimal();

        // Act
        var block = RcolBlockRegistry.CreateBlock(GeomBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<GeomBlock>();
        ((GeomBlock)block!).VertexCount.Should().Be(42);
    }

    #endregion

    #region Empty Constructor Tests

    [Fact]
    public void GeomBlock_EmptyConstructor_CreatesValidBlock()
    {
        // Act
        var block = new GeomBlock();

        // Assert
        block.Tag.Should().Be("GEOM");
        block.ResourceType.Should().Be(GeomBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0);
        block.VertexCount.Should().Be(0);
    }

    #endregion
}
