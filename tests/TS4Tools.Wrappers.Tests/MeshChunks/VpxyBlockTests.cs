using System.Buffers.Binary;
using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for VpxyBlock parsing and serialization.
/// </summary>
public class VpxyBlockTests
{
    /// <summary>
    /// Creates a minimal valid VPXY block data with no entries and no TGI blocks.
    /// </summary>
    private static byte[] CreateMinimalVpxy(bool isModular = false, int ftptIndex = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: VPXY
        writer.Write((byte)'V');
        writer.Write((byte)'P');
        writer.Write((byte)'X');
        writer.Write((byte)'Y');

        // Version: 4
        writer.Write(4u);

        // TGI offset is relative to position after reading the offset field
        // Position layout:
        // 0-3: tag, 4-7: version, 8-11: tgiOffset, 12-15: tgiSize
        // 16: entry count, 17: tc02, 18-41: bounds (24), 42-45: unused (4), 46: modular, [47-50: ftptIndex]
        // Position after offset field = 12
        // TGI starts at: 16 + 1 + 1 + 24 + 4 + 1 + (isModular ? 4 : 0) = 47 (or 51 if modular)
        // tgiOffset = TGI_start - 12 = 35 (or 39 if modular)
        int contentSize = 1 + 1 + 24 + 4 + 1 + (isModular ? 4 : 0); // 31 or 35
        int tgiStartPosition = 16 + contentSize; // 47 or 51
        uint tgiOffset = (uint)(tgiStartPosition - 12);
        writer.Write(tgiOffset);
        writer.Write(0u); // TGI size = 0 (no TGI blocks)

        // Entry list: 0 entries
        writer.Write((byte)0);

        // TC02 marker
        writer.Write((byte)0x02);

        // Bounding box: 6 floats (24 bytes) - all zeros
        for (int i = 0; i < 6; i++)
            writer.Write(0f);

        // Unused: 4 bytes
        writer.Write(new byte[4]);

        // Modular flag
        writer.Write(isModular ? (byte)1 : (byte)0);

        // FTPT index (if modular)
        if (isModular)
            writer.Write(ftptIndex);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a VPXY block with entries and TGI blocks.
    /// </summary>
    private static byte[] CreateVpxyWithEntries()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: VPXY
        writer.Write((byte)'V');
        writer.Write((byte)'P');
        writer.Write((byte)'X');
        writer.Write((byte)'Y');

        // Version: 4
        writer.Write(4u);

        // TGI offset is relative to position after reading the offset field
        // Position layout:
        // 0-3: tag, 4-7: version, 8-11: tgiOffset, 12-15: tgiSize
        // 16: entry count (1 byte)
        // 17-21: Entry01 (type:1 + tgiIndex:4 = 5 bytes)
        // 22-26: Entry00 (type:1 + entryId:1 + count:1 + indices:2 = 5 bytes)
        // 27: tc02
        // 28-51: bounds (24 bytes)
        // 52-55: unused (4 bytes)
        // 56: modular
        // TGI starts at: 57
        // Position after offset field = 12
        // tgiOffset = 57 - 12 = 45
        int entriesSize = 1 + 5 + 5; // count + Entry01 + Entry00 = 11 bytes
        int otherSize = 1 + 24 + 4 + 1; // tc02 + bounds + unused + modular = 30 bytes
        int tgiStartPosition = 16 + entriesSize + otherSize;
        uint tgiOffset = (uint)(tgiStartPosition - 12);
        writer.Write(tgiOffset);
        writer.Write(32u); // TGI size = 2 TGI blocks * 16 bytes

        // Entry list: 2 entries
        writer.Write((byte)2);

        // Entry01: type=0x01, tgiIndex=0
        writer.Write((byte)0x01);
        writer.Write(0); // tgiIndex

        // Entry00: type=0x00, entryId=1, count=2, indices=[0, 1]
        writer.Write((byte)0x00);
        writer.Write((byte)1); // entryId
        writer.Write((byte)2); // count
        writer.Write((byte)0); // index 0
        writer.Write((byte)1); // index 1

        // TC02 marker
        writer.Write((byte)0x02);

        // Bounding box: min(-1,-1,-1), max(1,1,1)
        writer.Write(-1f);
        writer.Write(-1f);
        writer.Write(-1f);
        writer.Write(1f);
        writer.Write(1f);
        writer.Write(1f);

        // Unused: 4 bytes
        writer.Write(new byte[4]);

        // Modular flag: false
        writer.Write((byte)0);

        // TGI blocks (2)
        // TGI 1: Instance=0x1234, Type=0x01661233 (MODL), Group=0
        writer.Write(0x1234UL); // Instance
        writer.Write(0x01661233u); // Type
        writer.Write(0u); // Group

        // TGI 2: Instance=0x5678, Type=0x01D0E75D (MATD), Group=0
        writer.Write(0x5678UL); // Instance
        writer.Write(0x01D0E75Du); // Type
        writer.Write(0u); // Group

        return ms.ToArray();
    }

    [Fact]
    public void VpxyBlock_Parse_MinimalBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMinimalVpxy();

        // Act
        var block = new VpxyBlock(data);

        // Assert
        block.Tag.Should().Be("VPXY");
        block.ResourceType.Should().Be(VpxyBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(4);
        block.Entries.Should().BeEmpty();
        block.Tc02.Should().Be(0x02);
        block.IsModular.Should().BeFalse();
        block.FtptIndex.Should().Be(0);
        block.TgiBlocks.Should().BeEmpty();
    }

    [Fact]
    public void VpxyBlock_Parse_ModularBlock_ParsesFtptIndex()
    {
        // Arrange
        var data = CreateMinimalVpxy(isModular: true, ftptIndex: 42);

        // Act
        var block = new VpxyBlock(data);

        // Assert
        block.IsModular.Should().BeTrue();
        block.FtptIndex.Should().Be(42);
    }

    [Fact]
    public void VpxyBlock_Parse_WithEntries_ParsesAllEntries()
    {
        // Arrange
        var data = CreateVpxyWithEntries();

        // Act
        var block = new VpxyBlock(data);

        // Assert
        block.Entries.Should().HaveCount(2);

        // First entry is Entry01
        block.Entries[0].Should().BeOfType<VpxyEntry01>();
        var entry01 = (VpxyEntry01)block.Entries[0];
        entry01.TgiIndex.Should().Be(0);

        // Second entry is Entry00
        block.Entries[1].Should().BeOfType<VpxyEntry00>();
        var entry00 = (VpxyEntry00)block.Entries[1];
        entry00.EntryId.Should().Be(1);
        entry00.TgiIndices.Should().HaveCount(2);
        entry00.TgiIndices[0].Should().Be(0);
        entry00.TgiIndices[1].Should().Be(1);
    }

    [Fact]
    public void VpxyBlock_Parse_WithEntries_ParsesTgiBlocks()
    {
        // Arrange
        var data = CreateVpxyWithEntries();

        // Act
        var block = new VpxyBlock(data);

        // Assert
        block.TgiBlocks.Should().HaveCount(2);
        block.TgiBlocks[0].Instance.Should().Be(0x1234UL);
        block.TgiBlocks[0].ResourceType.Should().Be(0x01661233u);
        block.TgiBlocks[1].Instance.Should().Be(0x5678UL);
        block.TgiBlocks[1].ResourceType.Should().Be(0x01D0E75Du);
    }

    [Fact]
    public void VpxyBlock_Parse_WithBoundingBox_ParsesBounds()
    {
        // Arrange
        var data = CreateVpxyWithEntries();

        // Act
        var block = new VpxyBlock(data);

        // Assert
        block.Bounds.Min.X.Should().Be(-1f);
        block.Bounds.Min.Y.Should().Be(-1f);
        block.Bounds.Min.Z.Should().Be(-1f);
        block.Bounds.Max.X.Should().Be(1f);
        block.Bounds.Max.Y.Should().Be(1f);
        block.Bounds.Max.Z.Should().Be(1f);
    }

    [Fact]
    public void VpxyBlock_Serialize_MinimalBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateMinimalVpxy();
        var block = new VpxyBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VpxyBlock_Serialize_ModularBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateMinimalVpxy(isModular: true, ftptIndex: 42);
        var block = new VpxyBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VpxyBlock_Serialize_WithEntries_RoundTrips()
    {
        // Arrange
        var originalData = CreateVpxyWithEntries();
        var block = new VpxyBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VpxyBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = CreateMinimalVpxy();
        data[0] = (byte)'X'; // Corrupt the tag

        // Act & Assert
        var action = () => new VpxyBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid VPXY tag*");
    }

    [Fact]
    public void VpxyBlock_Parse_InvalidVersion_ThrowsException()
    {
        // Arrange
        var data = CreateMinimalVpxy();
        // Version is at offset 4, change it to 5
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 5u);

        // Act & Assert
        var action = () => new VpxyBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid VPXY version*");
    }

    [Fact]
    public void VpxyBlock_Parse_InvalidTc02_ThrowsException()
    {
        // Arrange
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Valid header
        writer.Write((byte)'V');
        writer.Write((byte)'P');
        writer.Write((byte)'X');
        writer.Write((byte)'Y');
        writer.Write(4u); // Version
        writer.Write(31u); // TGI offset
        writer.Write(0u); // TGI size
        writer.Write((byte)0); // Entry count

        // Invalid TC02
        writer.Write((byte)0x03); // Should be 0x02

        // Padding to fill expected size
        writer.Write(new byte[28]); // bounds(24) + unused(4)

        var data = ms.ToArray();

        // Act & Assert
        var action = () => new VpxyBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid VPXY TC02*");
    }

    [Fact]
    public void VpxyBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(VpxyBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("VPXY").Should().BeTrue();
    }

    [Fact]
    public void VpxyBlock_Registry_CreatesVpxyBlock()
    {
        // Arrange
        var data = CreateMinimalVpxy();

        // Act
        var block = RcolBlockRegistry.CreateBlock(VpxyBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<VpxyBlock>();
    }

    [Fact]
    public void VpxyEntry00_Properties_AreCorrect()
    {
        // Arrange
        var indices = new List<int> { 0, 1, 2 };
        var entry = new VpxyEntry00(5, indices);

        // Assert
        entry.EntryType.Should().Be(0x00);
        entry.EntryId.Should().Be(5);
        entry.TgiIndices.Should().BeEquivalentTo(indices);
    }

    [Fact]
    public void VpxyEntry01_Properties_AreCorrect()
    {
        // Arrange
        var entry = new VpxyEntry01(42);

        // Assert
        entry.EntryType.Should().Be(0x01);
        entry.TgiIndex.Should().Be(42);
    }
}
