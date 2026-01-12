using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for VbsiBlock (Vertex Buffer Swizzle Info) parsing and serialization.
/// </summary>
public class VbsiBlockTests
{
    /// <summary>
    /// Creates a minimal VBSI block with version 1.
    /// </summary>
    private static byte[] CreateVbsiBlockVersion1()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'B');
        writer.Write((byte)'S');
        writer.Write((byte)'I');

        // Version
        writer.Write(1u);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a VBSI block with version 2 and additional data.
    /// </summary>
    private static byte[] CreateVbsiBlockVersion2()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'B');
        writer.Write((byte)'S');
        writer.Write((byte)'I');

        // Version
        writer.Write(2u);

        // Additional swizzle data placeholder
        for (int i = 0; i < 16; i++)
            writer.Write((byte)i);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a VBSI block with extended data.
    /// </summary>
    private static byte[] CreateVbsiBlockExtended()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'B');
        writer.Write((byte)'S');
        writer.Write((byte)'I');

        // Version
        writer.Write(3u);

        // Extended swizzle data
        for (int i = 0; i < 32; i++)
            writer.Write((byte)(i * 2));

        return ms.ToArray();
    }

    #region Parsing Tests

    [Fact]
    public void VbsiBlock_Parse_Version1_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVbsiBlockVersion1();

        // Act
        var block = new VbsiBlock(data);

        // Assert
        block.Tag.Should().Be("VBSI");
        block.ResourceType.Should().Be(VbsiBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(1);
    }

    [Fact]
    public void VbsiBlock_Parse_Version2_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVbsiBlockVersion2();

        // Act
        var block = new VbsiBlock(data);

        // Assert
        block.Tag.Should().Be("VBSI");
        block.Version.Should().Be(2);
    }

    [Fact]
    public void VbsiBlock_Parse_Extended_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVbsiBlockExtended();

        // Act
        var block = new VbsiBlock(data);

        // Assert
        block.Version.Should().Be(3);
    }

    [Fact]
    public void VbsiBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'B', (byte)'S', (byte)'I', // Wrong tag
            0x01, 0x00, 0x00, 0x00  // Version
        };

        // Act & Assert
        var action = () => new VbsiBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid VBSI tag*");
    }

    [Fact]
    public void VbsiBlock_TypeId_HasCorrectValue()
    {
        // Assert
        VbsiBlock.TypeId.Should().Be(0x01D10F3B);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void VbsiBlock_Serialize_Version1_RoundTrips()
    {
        // Arrange
        var originalData = CreateVbsiBlockVersion1();
        var block = new VbsiBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VbsiBlock_Serialize_Version2_RoundTrips()
    {
        // Arrange
        var originalData = CreateVbsiBlockVersion2();
        var block = new VbsiBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VbsiBlock_Serialize_Extended_RoundTrips()
    {
        // Arrange
        var originalData = CreateVbsiBlockExtended();
        var block = new VbsiBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VbsiBlock_Serialize_ParsedDataMatchesOriginal()
    {
        // Arrange
        var originalData = CreateVbsiBlockVersion2();
        var block = new VbsiBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new VbsiBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void VbsiBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(VbsiBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("VBSI").Should().BeTrue();
    }

    [Fact]
    public void VbsiBlock_Registry_CreatesVbsiBlock()
    {
        // Arrange
        var data = CreateVbsiBlockVersion1();

        // Act
        var block = RcolBlockRegistry.CreateBlock(VbsiBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<VbsiBlock>();
        ((VbsiBlock)block!).Version.Should().Be(1);
    }

    #endregion

    #region Empty Constructor Tests

    [Fact]
    public void VbsiBlock_EmptyConstructor_CreatesValidBlock()
    {
        // Act
        var block = new VbsiBlock();

        // Assert
        block.Tag.Should().Be("VBSI");
        block.ResourceType.Should().Be(VbsiBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0);
    }

    #endregion
}
