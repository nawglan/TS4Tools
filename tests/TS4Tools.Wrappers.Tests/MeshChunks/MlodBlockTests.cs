using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for MlodBlock (Model LOD) parsing and serialization.
/// </summary>
public class MlodBlockTests
{
    /// <summary>
    /// Creates a minimal MLOD block with no meshes.
    /// </summary>
    private static byte[] CreateMlodBlockEmpty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'M');
        writer.Write((byte)'L');
        writer.Write((byte)'O');
        writer.Write((byte)'D');

        // Version (0x00000202)
        writer.Write(0x00000202u);

        // Mesh count
        writer.Write(0);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an MLOD block with multiple meshes.
    /// </summary>
    private static byte[] CreateMlodBlockWithMeshes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'M');
        writer.Write((byte)'L');
        writer.Write((byte)'O');
        writer.Write((byte)'D');

        // Version
        writer.Write(0x00000202u);

        // Mesh count
        writer.Write(3);

        // Additional mesh data would follow, but we just need enough for basic parsing
        // Add some placeholder bytes
        for (int i = 0; i < 48; i++)
            writer.Write((byte)0);

        return ms.ToArray();
    }

    #region Parsing Tests

    [Fact]
    public void MlodBlock_Parse_EmptyBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMlodBlockEmpty();

        // Act
        var block = new MlodBlock(data);

        // Assert
        block.Tag.Should().Be("MLOD");
        block.ResourceType.Should().Be(MlodBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x00000202);
        block.MeshCount.Should().Be(0);
    }

    [Fact]
    public void MlodBlock_Parse_WithMeshes_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMlodBlockWithMeshes();

        // Act
        var block = new MlodBlock(data);

        // Assert
        block.Tag.Should().Be("MLOD");
        block.Version.Should().Be(0x00000202);
        block.MeshCount.Should().Be(3);
    }

    [Fact]
    public void MlodBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'L', (byte)'O', (byte)'D', // Wrong tag
            0x02, 0x02, 0x00, 0x00, // Version
            0x00, 0x00, 0x00, 0x00  // Mesh count
        };

        // Act & Assert
        var action = () => new MlodBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid MLOD tag*");
    }

    [Fact]
    public void MlodBlock_TypeId_HasCorrectValue()
    {
        // Assert
        MlodBlock.TypeId.Should().Be(0x01D10F34);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void MlodBlock_Serialize_EmptyBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateMlodBlockEmpty();
        var block = new MlodBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MlodBlock_Serialize_WithMeshes_RoundTrips()
    {
        // Arrange
        var originalData = CreateMlodBlockWithMeshes();
        var block = new MlodBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MlodBlock_Serialize_ParsedDataMatchesOriginal()
    {
        // Arrange
        var originalData = CreateMlodBlockWithMeshes();
        var block = new MlodBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new MlodBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
        reparsed.MeshCount.Should().Be(block.MeshCount);
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void MlodBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(MlodBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("MLOD").Should().BeTrue();
    }

    [Fact]
    public void MlodBlock_Registry_CreatesMlodBlock()
    {
        // Arrange
        var data = CreateMlodBlockWithMeshes();

        // Act
        var block = RcolBlockRegistry.CreateBlock(MlodBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<MlodBlock>();
        ((MlodBlock)block!).MeshCount.Should().Be(3);
    }

    #endregion

    #region Empty Constructor Tests

    [Fact]
    public void MlodBlock_EmptyConstructor_CreatesValidBlock()
    {
        // Act
        var block = new MlodBlock();

        // Assert
        block.Tag.Should().Be("MLOD");
        block.ResourceType.Should().Be(MlodBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x00000202); // Default version
        block.MeshCount.Should().Be(0);
    }

    #endregion
}
