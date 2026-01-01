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
    /// Creates a complete GEOM block with version 5, no shader, 2 vertices, and 1 face.
    /// </summary>
    private static byte[] CreateGeomBlockV5()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'G');
        writer.Write((byte)'E');
        writer.Write((byte)'O');
        writer.Write((byte)'M');

        // Version 5
        writer.Write(0x00000005u);

        // TGI offset (will be patched)
        long tgiOffsetPos = ms.Position;
        writer.Write(0u);

        // TGI size
        writer.Write(0u);

        // Shader type (0 = no shader)
        writer.Write(0u);

        // MergeGroup
        writer.Write(1u);

        // SortOrder
        writer.Write(2u);

        // Vertex count
        writer.Write(2);

        // Vertex format count
        writer.Write(2);

        // Format 1: Position (usage=1, dataType=1, size=12)
        writer.Write(1u); // usage
        writer.Write(1u); // dataType
        writer.Write((byte)12); // size

        // Format 2: Normal (usage=2, dataType=1, size=12)
        writer.Write(2u); // usage
        writer.Write(1u); // dataType
        writer.Write((byte)12); // size

        // Vertex 1: Position (0,0,0) + Normal (0,1,0)
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f);
        writer.Write(0.0f); writer.Write(1.0f); writer.Write(0.0f);

        // Vertex 2: Position (1,0,0) + Normal (0,1,0)
        writer.Write(1.0f); writer.Write(0.0f); writer.Write(0.0f);
        writer.Write(0.0f); writer.Write(1.0f); writer.Write(0.0f);

        // numFacePointSizes (must be 1)
        writer.Write(1);

        // facePointSize (must be 2)
        writer.Write((byte)2);

        // Face count (indices count = faces * 3)
        writer.Write(3); // 1 face * 3 indices

        // Face: indices 0, 1, 1
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)1);

        // SkinIndex (v5 only)
        writer.Write(0);

        // BoneHashes count
        writer.Write(0);

        // Record TGI position and patch offset
        long tgiStartPos = ms.Position;
        uint tgiOffset = (uint)(tgiStartPos - (tgiOffsetPos + 4));
        ms.Position = tgiOffsetPos;
        writer.Write(tgiOffset);
        writer.Write(0u); // tgiSize = 0

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a complete GEOM block with version 12 (0x0C), no shader, 3 vertices, 1 face.
    /// </summary>
    private static byte[] CreateGeomBlockV12()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'G');
        writer.Write((byte)'E');
        writer.Write((byte)'O');
        writer.Write((byte)'M');

        // Version 12
        writer.Write(0x0000000Cu);

        // TGI offset (will be patched)
        long tgiOffsetPos = ms.Position;
        writer.Write(0u);

        // TGI size
        writer.Write(0u);

        // Shader type (0 = no shader)
        writer.Write(0u);

        // MergeGroup
        writer.Write(1u);

        // SortOrder
        writer.Write(2u);

        // Vertex count
        writer.Write(3);

        // Vertex format count
        writer.Write(1);

        // Format 1: Position (usage=1, dataType=1, size=12)
        writer.Write(1u); // usage
        writer.Write(1u); // dataType
        writer.Write((byte)12); // size

        // Vertex 1: Position (0,0,0)
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f);

        // Vertex 2: Position (1,0,0)
        writer.Write(1.0f); writer.Write(0.0f); writer.Write(0.0f);

        // Vertex 3: Position (0,1,0)
        writer.Write(0.0f); writer.Write(1.0f); writer.Write(0.0f);

        // numFacePointSizes (must be 1)
        writer.Write(1);

        // facePointSize (must be 2)
        writer.Write((byte)2);

        // Face count (indices count = faces * 3)
        writer.Write(3); // 1 face * 3 indices

        // Face: triangle 0, 1, 2
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)2);

        // UnknownThings count (v12)
        writer.Write(0);

        // UnknownThings2 count (v12)
        writer.Write(0);

        // BoneHashes count
        writer.Write(0);

        // Record TGI position and patch offset
        long tgiStartPos = ms.Position;
        uint tgiOffset = (uint)(tgiStartPos - (tgiOffsetPos + 4));
        ms.Position = tgiOffsetPos;
        writer.Write(tgiOffset);
        writer.Write(0u); // tgiSize = 0

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a GEOM block with version 12 and shader data.
    /// </summary>
    private static byte[] CreateGeomBlockWithShader()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'G');
        writer.Write((byte)'E');
        writer.Write((byte)'O');
        writer.Write((byte)'M');

        // Version 12
        writer.Write(0x0000000Cu);

        // TGI offset (will be patched)
        long tgiOffsetPos = ms.Position;
        writer.Write(0u);

        // TGI size
        writer.Write(0u);

        // Shader type (non-zero = has shader)
        writer.Write(1u);

        // MTNF size
        byte[] mtnfData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        writer.Write(mtnfData.Length);
        writer.Write(mtnfData);

        // MergeGroup
        writer.Write(1u);

        // SortOrder
        writer.Write(2u);

        // Vertex count
        writer.Write(1);

        // Vertex format count
        writer.Write(1);

        // Format 1: Position (usage=1, dataType=1, size=12)
        writer.Write(1u); // usage
        writer.Write(1u); // dataType
        writer.Write((byte)12); // size

        // Vertex 1: Position (0,0,0)
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f);

        // numFacePointSizes (must be 1)
        writer.Write(1);

        // facePointSize (must be 2)
        writer.Write((byte)2);

        // Face count (no faces)
        writer.Write(0);

        // UnknownThings count (v12)
        writer.Write(0);

        // UnknownThings2 count (v12)
        writer.Write(0);

        // BoneHashes count
        writer.Write(0);

        // Record TGI position and patch offset
        long tgiStartPos = ms.Position;
        uint tgiOffset = (uint)(tgiStartPos - (tgiOffsetPos + 4));
        ms.Position = tgiOffsetPos;
        writer.Write(tgiOffset);
        writer.Write(0u); // tgiSize = 0

        return ms.ToArray();
    }

    #region Parsing Tests

    [Fact]
    public void GeomBlock_Parse_V5Block_ParsesCorrectly()
    {
        // Arrange
        var data = CreateGeomBlockV5();

        // Act
        var block = new GeomBlock(data);

        // Assert
        block.Tag.Should().Be("GEOM");
        block.ResourceType.Should().Be(GeomBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(5);
        block.VertexCount.Should().Be(2);
        block.FaceCount.Should().Be(1);
        block.MergeGroup.Should().Be(1);
        block.SortOrder.Should().Be(2);
        block.Shader.Should().Be(0);
        block.MtnfData.Should().BeNull();

        // Check vertex formats
        block.VertexFormats.Should().NotBeNull();
        block.VertexFormats!.Count.Should().Be(2);
        block.VertexFormats[0].Usage.Should().Be(GeomUsageType.Position);
        block.VertexFormats[1].Usage.Should().Be(GeomUsageType.Normal);

        // Check vertices
        block.Vertices.Should().NotBeNull();
        block.Vertices!.Count.Should().Be(2);

        var vertex0 = block.Vertices[0];
        vertex0.Position.Should().NotBeNull();
        vertex0.Position!.X.Should().Be(0);
        vertex0.Position.Y.Should().Be(0);
        vertex0.Position.Z.Should().Be(0);

        var vertex1 = block.Vertices[1];
        vertex1.Position.Should().NotBeNull();
        vertex1.Position!.X.Should().Be(1);
        vertex1.Position.Y.Should().Be(0);
        vertex1.Position.Z.Should().Be(0);

        // Check faces
        block.Faces.Should().NotBeNull();
        block.Faces!.Count.Should().Be(1);
        block.Faces[0].VertexIndex0.Should().Be(0);
        block.Faces[0].VertexIndex1.Should().Be(1);
        block.Faces[0].VertexIndex2.Should().Be(1);

        // V5-specific
        block.SkinIndex.Should().Be(0);
        block.UnknownThings.Should().BeNull();
        block.UnknownThings2.Should().BeNull();
    }

    [Fact]
    public void GeomBlock_Parse_V12Block_ParsesCorrectly()
    {
        // Arrange
        var data = CreateGeomBlockV12();

        // Act
        var block = new GeomBlock(data);

        // Assert
        block.Version.Should().Be(0x0C);
        block.VertexCount.Should().Be(3);
        block.FaceCount.Should().Be(1);

        // V12-specific
        block.SkinIndex.Should().Be(0);
        block.UnknownThings.Should().NotBeNull();
        block.UnknownThings!.Count.Should().Be(0);
        block.UnknownThings2.Should().NotBeNull();
        block.UnknownThings2!.Count.Should().Be(0);
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
        block.Shader.Should().Be(1);
        block.MtnfData.Should().NotBeNull();
        block.MtnfData!.Length.Should().Be(8);
        block.VertexCount.Should().Be(1);
    }

    [Fact]
    public void GeomBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'E', (byte)'O', (byte)'M', // Wrong tag
            0x0C, 0x00, 0x00, 0x00, // Version
        };

        // Act & Assert
        var action = () => new GeomBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid GEOM tag*");
    }

    [Fact]
    public void GeomBlock_Parse_InvalidVersion_ThrowsException()
    {
        // Arrange - create data with invalid version
        var data = new byte[]
        {
            (byte)'G', (byte)'E', (byte)'O', (byte)'M',
            0x99, 0x00, 0x00, 0x00, // Invalid version
        };

        // Act & Assert
        var action = () => new GeomBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid GEOM version*");
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
    public void GeomBlock_Serialize_V5Block_RoundTrips()
    {
        // Arrange
        var originalData = CreateGeomBlockV5();
        var block = new GeomBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void GeomBlock_Serialize_V12Block_RoundTrips()
    {
        // Arrange
        var originalData = CreateGeomBlockV12();
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
        var originalData = CreateGeomBlockV12();
        var block = new GeomBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new GeomBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
        reparsed.VertexCount.Should().Be(block.VertexCount);
        reparsed.FaceCount.Should().Be(block.FaceCount);
        reparsed.MergeGroup.Should().Be(block.MergeGroup);
        reparsed.SortOrder.Should().Be(block.SortOrder);
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
        var data = CreateGeomBlockV12();

        // Act
        var block = RcolBlockRegistry.CreateBlock(GeomBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<GeomBlock>();
        ((GeomBlock)block!).VertexCount.Should().Be(3);
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
        block.Version.Should().Be(GeomBlock.Version12); // Default version
        block.VertexCount.Should().Be(0);
        block.FaceCount.Should().Be(0);
    }

    #endregion

    #region Vertex Element Tests

    [Fact]
    public void GeomBlock_Parse_WithWeightsV5_ParsesFloatWeights()
    {
        // Arrange - Create V5 block with Weights
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag + Version
        writer.Write((byte)'G'); writer.Write((byte)'E'); writer.Write((byte)'O'); writer.Write((byte)'M');
        writer.Write(0x00000005u);

        // TGI offset/size
        long tgiOffsetPos = ms.Position;
        writer.Write(0u);
        writer.Write(0u);

        // Shader = 0
        writer.Write(0u);

        // MergeGroup, SortOrder
        writer.Write(0u);
        writer.Write(0u);

        // Vertex count
        writer.Write(1);

        // Format count = 2 (Position + Weights)
        writer.Write(2);

        // Position format
        writer.Write(1u); writer.Write(1u); writer.Write((byte)12);

        // Weights format (v5: usage=5, dataType=2, size=16 for floats)
        // Note: Legacy uses same dataType table for both v5 and v12
        writer.Write(5u); writer.Write(2u); writer.Write((byte)16);

        // Vertex data: Position (0,0,0) + Weights (0.5, 0.3, 0.2, 0.0)
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f);
        writer.Write(0.5f); writer.Write(0.3f); writer.Write(0.2f); writer.Write(0.0f);

        // Face data
        writer.Write(1); writer.Write((byte)2); writer.Write(0);

        // SkinIndex
        writer.Write(0);

        // BoneHashes
        writer.Write(0);

        // Patch TGI
        long tgiStartPos = ms.Position;
        uint tgiOffset = (uint)(tgiStartPos - (tgiOffsetPos + 4));
        ms.Position = tgiOffsetPos;
        writer.Write(tgiOffset);
        writer.Write(0u);

        var data = ms.ToArray();

        // Act
        var block = new GeomBlock(data);

        // Assert
        block.VertexFormats!.Count.Should().Be(2);
        block.VertexFormats[1].Usage.Should().Be(GeomUsageType.Weights);

        var vertex = block.Vertices![0];
        var weightsElement = vertex.Elements.FirstOrDefault(e => e.Usage == GeomUsageType.Weights);
        weightsElement.Should().NotBeNull();
        weightsElement.Should().BeOfType<WeightsElement>();

        var weights = (WeightsElement)weightsElement!;
        weights.W1.Should().Be(0.5f);
        weights.W2.Should().Be(0.3f);
        weights.W3.Should().Be(0.2f);
        weights.W4.Should().Be(0.0f);
    }

    [Fact]
    public void GeomBlock_Parse_WithWeightsV12_ParsesByteWeights()
    {
        // Arrange - Create V12 block with Weights
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag + Version
        writer.Write((byte)'G'); writer.Write((byte)'E'); writer.Write((byte)'O'); writer.Write((byte)'M');
        writer.Write(0x0000000Cu);

        // TGI offset/size
        long tgiOffsetPos = ms.Position;
        writer.Write(0u);
        writer.Write(0u);

        // Shader = 0
        writer.Write(0u);

        // MergeGroup, SortOrder
        writer.Write(0u);
        writer.Write(0u);

        // Vertex count
        writer.Write(1);

        // Format count = 2 (Position + Weights)
        writer.Write(2);

        // Position format
        writer.Write(1u); writer.Write(1u); writer.Write((byte)12);

        // Weights format (v12: usage=5, dataType=2, size=4 for bytes)
        writer.Write(5u); writer.Write(2u); writer.Write((byte)4);

        // Vertex data: Position (0,0,0) + Weights (128, 64, 32, 31)
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f);
        writer.Write((byte)128); writer.Write((byte)64); writer.Write((byte)32); writer.Write((byte)31);

        // Face data
        writer.Write(1); writer.Write((byte)2); writer.Write(0);

        // UnknownThings, UnknownThings2
        writer.Write(0); writer.Write(0);

        // BoneHashes
        writer.Write(0);

        // Patch TGI
        long tgiStartPos = ms.Position;
        uint tgiOffset = (uint)(tgiStartPos - (tgiOffsetPos + 4));
        ms.Position = tgiOffsetPos;
        writer.Write(tgiOffset);
        writer.Write(0u);

        var data = ms.ToArray();

        // Act
        var block = new GeomBlock(data);

        // Assert
        block.VertexFormats!.Count.Should().Be(2);
        block.VertexFormats[1].Usage.Should().Be(GeomUsageType.Weights);

        var vertex = block.Vertices![0];
        var weightsElement = vertex.Elements.FirstOrDefault(e => e.Usage == GeomUsageType.Weights);
        weightsElement.Should().NotBeNull();
        weightsElement.Should().BeOfType<WeightBytesElement>();

        var weights = (WeightBytesElement)weightsElement!;
        weights.W1.Should().Be(128);
        weights.W2.Should().Be(64);
        weights.W3.Should().Be(32);
        weights.W4.Should().Be(31);
    }

    #endregion
}
