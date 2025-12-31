using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for IbufBlock (Index Buffer) parsing and serialization.
/// </summary>
public class IbufBlockTests
{
    /// <summary>
    /// Creates a minimal IBUF block with no indices.
    /// </summary>
    private static byte[] CreateIbufBlockEmpty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'I');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000101u); // Version
        writer.Write((uint)IbufFormat.None); // Flags
        writer.Write(0u); // DisplayListUsage

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an IBUF block with 16-bit indices (default format).
    /// </summary>
    private static byte[] CreateIbufBlockWith16BitIndices()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'I');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000101u); // Version
        writer.Write((uint)IbufFormat.None); // Flags - 16-bit
        writer.Write(0u); // DisplayListUsage

        // Triangle indices: 0, 1, 2, 0, 2, 3 (two triangles)
        writer.Write((short)0);
        writer.Write((short)1);
        writer.Write((short)2);
        writer.Write((short)0);
        writer.Write((short)2);
        writer.Write((short)3);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an IBUF block with 32-bit indices.
    /// </summary>
    private static byte[] CreateIbufBlockWith32BitIndices()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'I');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000101u); // Version
        writer.Write((uint)IbufFormat.Uses32BitIndices); // Flags - 32-bit
        writer.Write(0u); // DisplayListUsage

        // Triangle indices using large values (>65535)
        writer.Write(0);
        writer.Write(70000);
        writer.Write(140000);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an IBUF block with differenced (delta-encoded) indices.
    /// Indices: 0, 1, 2 stored as deltas: 0, 1, 1
    /// </summary>
    private static byte[] CreateIbufBlockWithDifferencedIndices()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'I');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000101u); // Version
        writer.Write((uint)IbufFormat.DifferencedIndices); // Delta encoding
        writer.Write(0u); // DisplayListUsage

        // Delta-encoded: actual values 0, 5, 10 stored as 0, 5, 5
        writer.Write((short)0);
        writer.Write((short)5);
        writer.Write((short)5);

        return ms.ToArray();
    }

    #region Parsing Tests

    [Fact]
    public void IbufBlock_Parse_EmptyBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateIbufBlockEmpty();

        // Act
        var block = new IbufBlock(data);

        // Assert
        block.Tag.Should().Be("IBUF");
        block.ResourceType.Should().Be(IbufBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x00000101u);
        block.Flags.Should().Be(IbufFormat.None);
        block.DisplayListUsage.Should().Be(0u);
        block.Buffer.Should().BeEmpty();
    }

    [Fact]
    public void IbufBlock_Parse_16BitIndices_ParsesCorrectly()
    {
        // Arrange
        var data = CreateIbufBlockWith16BitIndices();

        // Act
        var block = new IbufBlock(data);

        // Assert
        block.Version.Should().Be(0x00000101u);
        block.Flags.Should().Be(IbufFormat.None);
        block.Buffer.Should().HaveCount(6);
        block.Buffer.Should().BeEquivalentTo([0, 1, 2, 0, 2, 3]);
    }

    [Fact]
    public void IbufBlock_Parse_32BitIndices_ParsesCorrectly()
    {
        // Arrange
        var data = CreateIbufBlockWith32BitIndices();

        // Act
        var block = new IbufBlock(data);

        // Assert
        block.Flags.Should().HaveFlag(IbufFormat.Uses32BitIndices);
        block.Buffer.Should().HaveCount(3);
        block.Buffer.Should().BeEquivalentTo([0, 70000, 140000]);
    }

    [Fact]
    public void IbufBlock_Parse_DifferencedIndices_DecodesCorrectly()
    {
        // Arrange
        var data = CreateIbufBlockWithDifferencedIndices();

        // Act
        var block = new IbufBlock(data);

        // Assert
        block.Flags.Should().HaveFlag(IbufFormat.DifferencedIndices);
        // Delta decoding: 0, 0+5=5, 5+5=10
        block.Buffer.Should().BeEquivalentTo([0, 5, 10]);
    }

    [Fact]
    public void IbufBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'B', (byte)'U', (byte)'F', // Wrong tag
            0x01, 0x01, 0x00, 0x00, // Version
            0x00, 0x00, 0x00, 0x00, // Flags
            0x00, 0x00, 0x00, 0x00  // DisplayListUsage
        };

        // Act & Assert
        var action = () => new IbufBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid IBUF tag*");
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void IbufBlock_Serialize_EmptyBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateIbufBlockEmpty();
        var block = new IbufBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void IbufBlock_Serialize_16BitIndices_RoundTrips()
    {
        // Arrange
        var originalData = CreateIbufBlockWith16BitIndices();
        var block = new IbufBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void IbufBlock_Serialize_32BitIndices_RoundTrips()
    {
        // Arrange
        var originalData = CreateIbufBlockWith32BitIndices();
        var block = new IbufBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void IbufBlock_Serialize_DifferencedIndices_RoundTrips()
    {
        // Arrange
        var originalData = CreateIbufBlockWithDifferencedIndices();
        var block = new IbufBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void IbufBlock_Serialize_ParsedDataMatchesOriginal()
    {
        // Arrange
        var originalData = CreateIbufBlockWith16BitIndices();
        var block = new IbufBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new IbufBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
        reparsed.Flags.Should().Be(block.Flags);
        reparsed.DisplayListUsage.Should().Be(block.DisplayListUsage);
        reparsed.Buffer.Should().BeEquivalentTo(block.Buffer);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void IbufBlock_GetIndices_ReturnsCorrectRange()
    {
        // Arrange
        var data = CreateIbufBlockWith16BitIndices();
        var block = new IbufBlock(data);

        // Act - get first triangle (3 indices starting at 0)
        var triangle1 = block.GetIndices(ModelPrimitiveType.TriangleList, 0, 1);

        // Assert
        triangle1.Should().BeEquivalentTo([0, 1, 2]);
    }

    [Fact]
    public void IbufBlock_GetIndices_ReturnsSecondTriangle()
    {
        // Arrange
        var data = CreateIbufBlockWith16BitIndices();
        var block = new IbufBlock(data);

        // Act - get second triangle (3 indices starting at 3)
        var triangle2 = block.GetIndices(ModelPrimitiveType.TriangleList, 3, 1);

        // Assert
        triangle2.Should().BeEquivalentTo([0, 2, 3]);
    }

    [Fact]
    public void IbufBlock_GetIndices_MultipleTriangles()
    {
        // Arrange
        var data = CreateIbufBlockWith16BitIndices();
        var block = new IbufBlock(data);

        // Act - get both triangles
        var allIndices = block.GetIndices(ModelPrimitiveType.TriangleList, 0, 2);

        // Assert
        allIndices.Should().BeEquivalentTo([0, 1, 2, 0, 2, 3]);
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void IbufBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(IbufBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("IBUF").Should().BeTrue();
    }

    [Fact]
    public void IbufBlock_Registry_CreatesIbufBlock()
    {
        // Arrange
        var data = CreateIbufBlockWith16BitIndices();

        // Act
        var block = RcolBlockRegistry.CreateBlock(IbufBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<IbufBlock>();
        ((IbufBlock)block!).Buffer.Should().HaveCount(6);
    }

    #endregion

    #region Ibuf2Block Tests

    [Fact]
    public void Ibuf2Block_HasCorrectTypeId()
    {
        // Assert
        Ibuf2Block.TypeId.Should().Be(0x0229684Fu);
    }

    [Fact]
    public void Ibuf2Block_Parse_WorksCorrectly()
    {
        // Arrange - create IBUF2 data (same format as IBUF)
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)'I');
        writer.Write((byte)'B');
        writer.Write((byte)'U');
        writer.Write((byte)'F');
        writer.Write(0x00000101u);
        writer.Write((uint)IbufFormat.None);
        writer.Write(0u);
        writer.Write((short)0);
        writer.Write((short)1);
        writer.Write((short)2);
        var data = ms.ToArray();

        // Act
        var block = new Ibuf2Block(data);

        // Assert
        block.ResourceType.Should().Be(Ibuf2Block.TypeId);
        block.Tag.Should().Be("IBUF");
        block.Buffer.Should().BeEquivalentTo([0, 1, 2]);
    }

    #endregion
}
