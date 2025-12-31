using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for MtstBlock parsing and serialization.
/// </summary>
public class MtstBlockTests
{
    /// <summary>
    /// Creates a minimal MTST block with Type200 entries (version &lt; 768).
    /// </summary>
    private static byte[] CreateMtstType200(uint version = 0x200, uint nameHash = 0x12345678,
        uint indexValue = 0, MtstEntry200[]? entries = null)
    {
        entries ??= [];

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MTST
        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'S');
        writer.Write((byte)'T');

        // Version
        writer.Write(version);

        // Name hash
        writer.Write(nameHash);

        // Chunk reference
        writer.Write(indexValue);

        // Entry count
        writer.Write(entries.Length);

        // Entries
        foreach (var entry in entries)
        {
            writer.Write(entry.MatdIndex.RawValue);
            writer.Write((uint)entry.MaterialState);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a MTST block with Type300 entries (version >= 768).
    /// </summary>
    private static byte[] CreateMtstType300(uint version = 0x300, uint nameHash = 0x12345678,
        uint indexValue = 0, MtstEntry300[]? entries = null)
    {
        entries ??= [];

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: MTST
        writer.Write((byte)'M');
        writer.Write((byte)'T');
        writer.Write((byte)'S');
        writer.Write((byte)'T');

        // Version
        writer.Write(version);

        // Name hash
        writer.Write(nameHash);

        // Chunk reference
        writer.Write(indexValue);

        // Entry count
        writer.Write(entries.Length);

        // Entries
        foreach (var entry in entries)
        {
            writer.Write(entry.MatdIndex.RawValue);
            writer.Write((uint)entry.MaterialState);
            writer.Write(entry.MaterialVariant);
        }

        return ms.ToArray();
    }

    [Fact]
    public void MtstBlock_Parse_Type200_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMtstType200(version: 0x200, nameHash: 0xABCDEF00);

        // Act
        var block = new MtstBlock(data);

        // Assert
        block.Tag.Should().Be("MTST");
        block.ResourceType.Should().Be(MtstBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(0x200);
        block.NameHash.Should().Be(0xABCDEF00);
        block.IsType300.Should().BeFalse();
        block.Type200Entries.Should().BeEmpty();
        block.Type300Entries.Should().BeEmpty();
    }

    [Fact]
    public void MtstBlock_Parse_Type300_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMtstType300(version: 0x300, nameHash: 0x12345678);

        // Act
        var block = new MtstBlock(data);

        // Assert
        block.Version.Should().Be(0x300);
        block.IsType300.Should().BeTrue();
        block.Type200Entries.Should().BeEmpty();
        block.Type300Entries.Should().BeEmpty();
    }

    [Fact]
    public void MtstBlock_Parse_Type200WithEntries_ParsesAllEntries()
    {
        // Arrange
        var entries = new[]
        {
            new MtstEntry200(new RcolChunkReference(0x10000001), MaterialState.Default),
            new MtstEntry200(new RcolChunkReference(0x10000002), MaterialState.Dirty),
        };
        var data = CreateMtstType200(entries: entries);

        // Act
        var block = new MtstBlock(data);

        // Assert
        block.Type200Entries.Should().HaveCount(2);
        block.Type200Entries[0].MatdIndex.RawValue.Should().Be(0x10000001);
        block.Type200Entries[0].MaterialState.Should().Be(MaterialState.Default);
        block.Type200Entries[1].MatdIndex.RawValue.Should().Be(0x10000002);
        block.Type200Entries[1].MaterialState.Should().Be(MaterialState.Dirty);
    }

    [Fact]
    public void MtstBlock_Parse_Type300WithEntries_ParsesAllEntries()
    {
        // Arrange
        var entries = new[]
        {
            new MtstEntry300(new RcolChunkReference(0x10000001), MaterialState.VeryDirty, 100),
            new MtstEntry300(new RcolChunkReference(0x10000002), MaterialState.Burnt, 200),
        };
        var data = CreateMtstType300(entries: entries);

        // Act
        var block = new MtstBlock(data);

        // Assert
        block.Type300Entries.Should().HaveCount(2);
        block.Type300Entries[0].MatdIndex.RawValue.Should().Be(0x10000001);
        block.Type300Entries[0].MaterialState.Should().Be(MaterialState.VeryDirty);
        block.Type300Entries[0].MaterialVariant.Should().Be(100);
        block.Type300Entries[1].MatdIndex.RawValue.Should().Be(0x10000002);
        block.Type300Entries[1].MaterialState.Should().Be(MaterialState.Burnt);
        block.Type300Entries[1].MaterialVariant.Should().Be(200);
    }

    [Fact]
    public void MtstBlock_Parse_ChunkReference_ParsesCorrectly()
    {
        // Arrange - create reference with Public type and index 5
        // Format: top 4 bits = type, lower 28 bits = index + 1
        uint refValue = (0u << 28) | 6; // Public[5] = 0x00000006
        var data = CreateMtstType200(indexValue: refValue);

        // Act
        var block = new MtstBlock(data);

        // Assert
        block.Index.RefType.Should().Be(RcolReferenceType.Public);
        block.Index.Index.Should().Be(5);
    }

    [Fact]
    public void MtstBlock_Serialize_Type200_RoundTrips()
    {
        // Arrange
        var entries = new[]
        {
            new MtstEntry200(new RcolChunkReference(0x10000001), MaterialState.Default),
        };
        var originalData = CreateMtstType200(version: 0x200, nameHash: 0xDEADBEEF, entries: entries);
        var block = new MtstBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MtstBlock_Serialize_Type300_RoundTrips()
    {
        // Arrange
        var entries = new[]
        {
            new MtstEntry300(new RcolChunkReference(0x10000001), MaterialState.Clogged, 42),
        };
        var originalData = CreateMtstType300(version: 0x300, nameHash: 0xCAFEBABE, entries: entries);
        var block = new MtstBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MtstBlock_Serialize_EmptyBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateMtstType200();
        var block = new MtstBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void MtstBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = CreateMtstType200();
        data[0] = (byte)'X'; // Corrupt the tag

        // Act & Assert
        var action = () => new MtstBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid MTST tag*");
    }

    [Fact]
    public void MtstBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(MtstBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("MTST").Should().BeTrue();
    }

    [Fact]
    public void MtstBlock_Registry_CreatesMtstBlock()
    {
        // Arrange
        var data = CreateMtstType200();

        // Act
        var block = RcolBlockRegistry.CreateBlock(MtstBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<MtstBlock>();
    }

    [Fact]
    public void MaterialState_HasCorrectValues()
    {
        // Assert - verify the hash values match legacy
        ((uint)MaterialState.Default).Should().Be(0x2EA8FB98);
        ((uint)MaterialState.Dirty).Should().Be(0xEEAB4327);
        ((uint)MaterialState.VeryDirty).Should().Be(0x2E5DF9BB);
        ((uint)MaterialState.Burnt).Should().Be(0xC3867C32);
        ((uint)MaterialState.Clogged).Should().Be(0x257FB026);
        ((uint)MaterialState.CarLightsOff).Should().Be(0xE4AF52C1);
    }

    [Fact]
    public void MtstEntry200_Equality_WorksCorrectly()
    {
        // Arrange
        var entry1 = new MtstEntry200(new RcolChunkReference(1), MaterialState.Default);
        var entry2 = new MtstEntry200(new RcolChunkReference(1), MaterialState.Default);
        var entry3 = new MtstEntry200(new RcolChunkReference(2), MaterialState.Default);

        // Assert
        entry1.Should().Be(entry2);
        entry1.Should().NotBe(entry3);
        (entry1 == entry2).Should().BeTrue();
        (entry1 != entry3).Should().BeTrue();
    }

    [Fact]
    public void MtstEntry300_Equality_WorksCorrectly()
    {
        // Arrange
        var entry1 = new MtstEntry300(new RcolChunkReference(1), MaterialState.Default, 10);
        var entry2 = new MtstEntry300(new RcolChunkReference(1), MaterialState.Default, 10);
        var entry3 = new MtstEntry300(new RcolChunkReference(1), MaterialState.Default, 20);

        // Assert
        entry1.Should().Be(entry2);
        entry1.Should().NotBe(entry3);
        (entry1 == entry2).Should().BeTrue();
        (entry1 != entry3).Should().BeTrue();
    }
}
