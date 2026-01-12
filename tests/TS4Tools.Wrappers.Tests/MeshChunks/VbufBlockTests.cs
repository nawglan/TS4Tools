using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for VbufBlock (Vertex Buffer) parsing and serialization.
/// </summary>
public class VbufBlockTests
{
    /// <summary>
    /// Creates a minimal VBUF block with no vertex data.
    /// </summary>
    private static byte[] CreateVbufBlockEmpty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000101u); // Version
        writer.Write((uint)VbufFormat.None); // Flags
        writer.Write(0u); // SwizzleInfoIndex

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a VBUF block with vertex position data.
    /// 3 vertices with Float3 positions (12 bytes each = 36 bytes total).
    /// </summary>
    private static byte[] CreateVbufBlockWithPositions()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000101u); // Version
        writer.Write((uint)VbufFormat.None); // Flags
        writer.Write(0u); // SwizzleInfoIndex

        // Vertex 0: (0.0, 0.0, 0.0)
        writer.Write(0.0f);
        writer.Write(0.0f);
        writer.Write(0.0f);

        // Vertex 1: (1.0, 0.0, 0.0)
        writer.Write(1.0f);
        writer.Write(0.0f);
        writer.Write(0.0f);

        // Vertex 2: (0.0, 1.0, 0.0)
        writer.Write(0.0f);
        writer.Write(1.0f);
        writer.Write(0.0f);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a VBUF block with dynamic format flag.
    /// </summary>
    private static byte[] CreateVbufBlockDynamic()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000101u); // Version
        writer.Write((uint)VbufFormat.Dynamic); // Dynamic flag
        writer.Write(5u); // SwizzleInfoIndex

        // Single vertex
        writer.Write(1.5f);
        writer.Write(2.5f);
        writer.Write(3.5f);

        return ms.ToArray();
    }

    #region Parsing Tests

    [Fact]
    public void VbufBlock_Parse_EmptyBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVbufBlockEmpty();

        // Act
        var block = new VbufBlock(data);

        // Assert
        block.Tag.Should().Be("VBUF");
        block.ResourceType.Should().Be(VbufBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x00000101u);
        block.Flags.Should().Be(VbufFormat.None);
        block.SwizzleInfoIndex.Should().Be(0u);
        block.Buffer.Should().BeEmpty();
    }

    [Fact]
    public void VbufBlock_Parse_WithPositions_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVbufBlockWithPositions();

        // Act
        var block = new VbufBlock(data);

        // Assert
        block.Version.Should().Be(0x00000101u);
        block.Flags.Should().Be(VbufFormat.None);
        block.Buffer.Should().HaveCount(36); // 3 vertices * 12 bytes
    }

    [Fact]
    public void VbufBlock_Parse_DynamicFlag_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVbufBlockDynamic();

        // Act
        var block = new VbufBlock(data);

        // Assert
        block.Flags.Should().HaveFlag(VbufFormat.Dynamic);
        block.SwizzleInfoIndex.Should().Be(5u);
        block.Buffer.Should().HaveCount(12); // 1 vertex * 12 bytes
    }

    [Fact]
    public void VbufBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'B', (byte)'U', (byte)'F', // Wrong tag
            0x01, 0x01, 0x00, 0x00, // Version
            0x00, 0x00, 0x00, 0x00, // Flags
            0x00, 0x00, 0x00, 0x00  // SwizzleInfoIndex
        };

        // Act & Assert
        var action = () => new VbufBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid VBUF tag*");
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void VbufBlock_Serialize_EmptyBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateVbufBlockEmpty();
        var block = new VbufBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VbufBlock_Serialize_WithPositions_RoundTrips()
    {
        // Arrange
        var originalData = CreateVbufBlockWithPositions();
        var block = new VbufBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VbufBlock_Serialize_DynamicFlag_RoundTrips()
    {
        // Arrange
        var originalData = CreateVbufBlockDynamic();
        var block = new VbufBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VbufBlock_Serialize_ParsedDataMatchesOriginal()
    {
        // Arrange
        var originalData = CreateVbufBlockWithPositions();
        var block = new VbufBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new VbufBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
        reparsed.Flags.Should().Be(block.Flags);
        reparsed.SwizzleInfoIndex.Should().Be(block.SwizzleInfoIndex);
        reparsed.Buffer.Should().BeEquivalentTo(block.Buffer);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void VbufBlock_GetVertexCount_ReturnsCorrectCount()
    {
        // Arrange
        var data = CreateVbufBlockWithPositions();
        var block = new VbufBlock(data);

        // Act
        int count = block.GetVertexCount(12); // Float3 stride = 12 bytes

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public void VbufBlock_GetVertexCount_ZeroStride_ReturnsZero()
    {
        // Arrange
        var data = CreateVbufBlockWithPositions();
        var block = new VbufBlock(data);

        // Act
        int count = block.GetVertexCount(0);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void VbufBlock_GetVertexData_ReturnsCorrectData()
    {
        // Arrange
        var data = CreateVbufBlockWithPositions();
        var block = new VbufBlock(data);

        // Act - get vertex 1 data
        var vertexData = block.GetVertexData(1, 12);

        // Assert
        vertexData.Length.Should().Be(12);
        // First float should be 1.0f
        BitConverter.ToSingle(vertexData).Should().Be(1.0f);
    }

    [Fact]
    public void VbufBlock_ReadFloat_ReturnsCorrectValue()
    {
        // Arrange
        var data = CreateVbufBlockWithPositions();
        var block = new VbufBlock(data);

        // Act - read X of vertex 1 (at offset 0)
        float x = block.ReadFloat(1, 12, 0);
        // Read Y of vertex 1 (at offset 4)
        float y = block.ReadFloat(1, 12, 4);

        // Assert
        x.Should().Be(1.0f);
        y.Should().Be(0.0f);
    }

    [Fact]
    public void VbufBlock_ReadVector3_ReturnsCorrectVector()
    {
        // Arrange
        var data = CreateVbufBlockWithPositions();
        var block = new VbufBlock(data);

        // Act - read position of vertex 2
        var pos = block.ReadVector3(2, 12, 0);

        // Assert
        pos.X.Should().Be(0.0f);
        pos.Y.Should().Be(1.0f);
        pos.Z.Should().Be(0.0f);
    }

    [Fact]
    public void VbufBlock_ReadVector3_FirstVertex()
    {
        // Arrange
        var data = CreateVbufBlockWithPositions();
        var block = new VbufBlock(data);

        // Act
        var pos = block.ReadVector3(0, 12, 0);

        // Assert
        pos.Should().Be(MeshVector3.Zero);
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void VbufBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(VbufBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("VBUF").Should().BeTrue();
    }

    [Fact]
    public void VbufBlock_Registry_CreatesVbufBlock()
    {
        // Arrange
        var data = CreateVbufBlockWithPositions();

        // Act
        var block = RcolBlockRegistry.CreateBlock(VbufBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<VbufBlock>();
        ((VbufBlock)block!).Buffer.Should().HaveCount(36);
    }

    #endregion

    #region Vbuf2Block Tests

    [Fact]
    public void Vbuf2Block_HasCorrectTypeId()
    {
        // Assert
        Vbuf2Block.TypeId.Should().Be(0x0229684Bu);
    }

    [Fact]
    public void Vbuf2Block_Parse_WorksCorrectly()
    {
        // Arrange - create VBUF2 data (same format as VBUF)
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)'V');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');
        writer.Write(0x00000101u);
        writer.Write((uint)VbufFormat.None);
        writer.Write(0u);
        writer.Write(1.0f);
        writer.Write(2.0f);
        writer.Write(3.0f);
        var data = ms.ToArray();

        // Act
        var block = new Vbuf2Block(data);

        // Assert
        block.ResourceType.Should().Be(Vbuf2Block.TypeId);
        block.Tag.Should().Be("VBUF");
        block.Buffer.Should().HaveCount(12);
    }

    [Fact]
    public void Vbuf2Block_ReadVector3_ReturnsCorrectValue()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)'V');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');
        writer.Write(0x00000101u);
        writer.Write((uint)VbufFormat.None);
        writer.Write(0u);
        writer.Write(1.0f);
        writer.Write(2.0f);
        writer.Write(3.0f);
        var data = ms.ToArray();

        var block = new Vbuf2Block(data);

        // Act
        var pos = block.ReadVector3(0, 12, 0);

        // Assert
        pos.X.Should().Be(1.0f);
        pos.Y.Should().Be(2.0f);
        pos.Z.Should().Be(3.0f);
    }

    #endregion

    #region VbufFormat Tests

    [Theory]
    [InlineData(VbufFormat.None, 0x0u)]
    [InlineData(VbufFormat.Dynamic, 0x1u)]
    [InlineData(VbufFormat.DifferencedVertices, 0x2u)]
    [InlineData(VbufFormat.Collapsed, 0x4u)]
    public void VbufFormat_HasCorrectValues(VbufFormat format, uint expectedValue)
    {
        // Assert
        ((uint)format).Should().Be(expectedValue);
    }

    [Fact]
    public void VbufFormat_CombineFlags_WorksCorrectly()
    {
        // Arrange
        var combined = VbufFormat.Dynamic | VbufFormat.Collapsed;

        // Assert
        combined.HasFlag(VbufFormat.Dynamic).Should().BeTrue();
        combined.HasFlag(VbufFormat.Collapsed).Should().BeTrue();
        combined.HasFlag(VbufFormat.DifferencedVertices).Should().BeFalse();
    }

    #endregion
}
