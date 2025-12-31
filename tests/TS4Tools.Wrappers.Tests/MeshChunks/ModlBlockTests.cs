using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for ModlBlock (Model) parsing and serialization.
/// </summary>
public class ModlBlockTests
{
    /// <summary>
    /// Creates a minimal MODL block with no LOD entries.
    /// </summary>
    private static byte[] CreateModlBlockEmpty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'M');
        writer.Write((byte)'O');
        writer.Write((byte)'D');
        writer.Write((byte)'L');

        // Version (256 = 0x100)
        writer.Write(256u);

        // LOD entry count
        writer.Write(0);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a MODL block with multiple LOD entries.
    /// </summary>
    private static byte[] CreateModlBlockWithLods()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'M');
        writer.Write((byte)'O');
        writer.Write((byte)'D');
        writer.Write((byte)'L');

        // Version
        writer.Write(256u);

        // LOD entry count
        writer.Write(4);

        // Additional LOD data would follow, but we just need enough for basic parsing
        // Add some placeholder bytes
        for (int i = 0; i < 64; i++)
            writer.Write((byte)0);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a MODL block with a different version.
    /// </summary>
    private static byte[] CreateModlBlockDifferentVersion()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'M');
        writer.Write((byte)'O');
        writer.Write((byte)'D');
        writer.Write((byte)'L');

        // Version (different version)
        writer.Write(512u);

        // LOD entry count
        writer.Write(2);

        return ms.ToArray();
    }

    #region Parsing Tests

    [Fact]
    public void ModlBlock_Parse_EmptyBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateModlBlockEmpty();

        // Act
        var block = new ModlBlock(data);

        // Assert
        block.Tag.Should().Be("MODL");
        block.ResourceType.Should().Be(ModlBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(256);
        block.LodEntryCount.Should().Be(0);
    }

    [Fact]
    public void ModlBlock_Parse_WithLods_ParsesCorrectly()
    {
        // Arrange
        var data = CreateModlBlockWithLods();

        // Act
        var block = new ModlBlock(data);

        // Assert
        block.Tag.Should().Be("MODL");
        block.Version.Should().Be(256);
        block.LodEntryCount.Should().Be(4);
    }

    [Fact]
    public void ModlBlock_Parse_DifferentVersion_ParsesCorrectly()
    {
        // Arrange
        var data = CreateModlBlockDifferentVersion();

        // Act
        var block = new ModlBlock(data);

        // Assert
        block.Version.Should().Be(512);
        block.LodEntryCount.Should().Be(2);
    }

    [Fact]
    public void ModlBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'O', (byte)'D', (byte)'L', // Wrong tag
            0x00, 0x01, 0x00, 0x00, // Version
            0x00, 0x00, 0x00, 0x00  // LOD count
        };

        // Act & Assert
        var action = () => new ModlBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid MODL tag*");
    }

    [Fact]
    public void ModlBlock_TypeId_HasCorrectValue()
    {
        // Assert
        ModlBlock.TypeId.Should().Be(0x01661233);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void ModlBlock_Serialize_EmptyBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateModlBlockEmpty();
        var block = new ModlBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void ModlBlock_Serialize_WithLods_RoundTrips()
    {
        // Arrange
        var originalData = CreateModlBlockWithLods();
        var block = new ModlBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void ModlBlock_Serialize_ParsedDataMatchesOriginal()
    {
        // Arrange
        var originalData = CreateModlBlockWithLods();
        var block = new ModlBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new ModlBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
        reparsed.LodEntryCount.Should().Be(block.LodEntryCount);
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void ModlBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(ModlBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("MODL").Should().BeTrue();
    }

    [Fact]
    public void ModlBlock_Registry_CreatesModlBlock()
    {
        // Arrange
        var data = CreateModlBlockWithLods();

        // Act
        var block = RcolBlockRegistry.CreateBlock(ModlBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<ModlBlock>();
        ((ModlBlock)block!).LodEntryCount.Should().Be(4);
    }

    #endregion

    #region Empty Constructor Tests

    [Fact]
    public void ModlBlock_EmptyConstructor_CreatesValidBlock()
    {
        // Act
        var block = new ModlBlock();

        // Assert
        block.Tag.Should().Be("MODL");
        block.ResourceType.Should().Be(ModlBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(256); // Default version
        block.LodEntryCount.Should().Be(0);
    }

    #endregion
}
