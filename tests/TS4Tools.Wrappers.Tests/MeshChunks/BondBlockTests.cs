using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for BondBlock (SlotAdjust) parsing and serialization.
/// </summary>
public class BondBlockTests
{
    /// <summary>
    /// Creates a minimal BOND block with no adjustments.
    /// Note: BOND has NO tag in the binary data (unlike other RCOL blocks).
    /// </summary>
    private static byte[] CreateBondBlockEmpty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // NO tag - BOND starts directly with version
        writer.Write(4u); // Version
        writer.Write(0); // Adjustment count

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a BOND block with one adjustment.
    /// </summary>
    private static byte[] CreateBondBlockWithOneAdjustment()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // NO tag
        writer.Write(4u); // Version
        writer.Write(1); // Adjustment count

        // Adjustment 1
        writer.Write(0x12345678u); // SlotNameHash
        writer.Write(1.0f); // Offset.X
        writer.Write(2.0f); // Offset.Y
        writer.Write(3.0f); // Offset.Z
        writer.Write(1.0f); // Scale.X
        writer.Write(1.0f); // Scale.Y
        writer.Write(1.0f); // Scale.Z
        writer.Write(0.0f); // Quaternion.X
        writer.Write(0.0f); // Quaternion.Y
        writer.Write(0.0f); // Quaternion.Z
        writer.Write(1.0f); // Quaternion.W

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a BOND block with multiple adjustments.
    /// </summary>
    private static byte[] CreateBondBlockWithMultipleAdjustments()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // NO tag
        writer.Write(4u); // Version
        writer.Write(2); // Adjustment count

        // Adjustment 1
        writer.Write(0xAAAAAAAAu); // SlotNameHash
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f); // Offset
        writer.Write(1.0f); writer.Write(1.0f); writer.Write(1.0f); // Scale
        writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.0f); writer.Write(1.0f); // Quaternion

        // Adjustment 2
        writer.Write(0xBBBBBBBBu); // SlotNameHash
        writer.Write(10.0f); writer.Write(20.0f); writer.Write(30.0f); // Offset
        writer.Write(2.0f); writer.Write(2.0f); writer.Write(2.0f); // Scale
        writer.Write(0.707f); writer.Write(0.0f); writer.Write(0.0f); writer.Write(0.707f); // Quaternion (45deg X rotation)

        return ms.ToArray();
    }

    [Fact]
    public void BondBlock_Parse_EmptyBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateBondBlockEmpty();

        // Act
        var block = new BondBlock(data);

        // Assert
        block.Tag.Should().Be("BOND");
        block.ResourceType.Should().Be(BondBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(4);
        block.Adjustments.Should().BeEmpty();
    }

    [Fact]
    public void BondBlock_Parse_OneAdjustment_ParsesCorrectly()
    {
        // Arrange
        var data = CreateBondBlockWithOneAdjustment();

        // Act
        var block = new BondBlock(data);

        // Assert
        block.Adjustments.Should().HaveCount(1);
        block.Adjustments[0].SlotNameHash.Should().Be(0x12345678u);
        block.Adjustments[0].Offset.X.Should().Be(1.0f);
        block.Adjustments[0].Offset.Y.Should().Be(2.0f);
        block.Adjustments[0].Offset.Z.Should().Be(3.0f);
        block.Adjustments[0].Scale.X.Should().Be(1.0f);
        block.Adjustments[0].Scale.Y.Should().Be(1.0f);
        block.Adjustments[0].Scale.Z.Should().Be(1.0f);
        block.Adjustments[0].Quaternion.W.Should().Be(1.0f);
    }

    [Fact]
    public void BondBlock_Parse_MultipleAdjustments_ParsesCorrectly()
    {
        // Arrange
        var data = CreateBondBlockWithMultipleAdjustments();

        // Act
        var block = new BondBlock(data);

        // Assert
        block.Adjustments.Should().HaveCount(2);
        block.Adjustments[0].SlotNameHash.Should().Be(0xAAAAAAAAu);
        block.Adjustments[1].SlotNameHash.Should().Be(0xBBBBBBBBu);
        block.Adjustments[1].Offset.X.Should().Be(10.0f);
        block.Adjustments[1].Scale.X.Should().Be(2.0f);
    }

    [Fact]
    public void BondBlock_Serialize_EmptyBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateBondBlockEmpty();
        var block = new BondBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void BondBlock_Serialize_OneAdjustment_RoundTrips()
    {
        // Arrange
        var originalData = CreateBondBlockWithOneAdjustment();
        var block = new BondBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void BondBlock_Serialize_MultipleAdjustments_RoundTrips()
    {
        // Arrange
        var originalData = CreateBondBlockWithMultipleAdjustments();
        var block = new BondBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void BondBlock_FindAdjustment_FindsByHash()
    {
        // Arrange
        var data = CreateBondBlockWithMultipleAdjustments();
        var block = new BondBlock(data);

        // Act
        var found = block.FindAdjustment(0xBBBBBBBBu);

        // Assert
        found.Should().NotBeNull();
        found!.SlotNameHash.Should().Be(0xBBBBBBBBu);
    }

    [Fact]
    public void BondBlock_FindAdjustment_NotFound_ReturnsNull()
    {
        // Arrange
        var data = CreateBondBlockWithOneAdjustment();
        var block = new BondBlock(data);

        // Act
        var found = block.FindAdjustment(0xDEADBEEFu);

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public void BondBlock_AddAdjustment_AddsToList()
    {
        // Arrange
        var block = new BondBlock();
        var adjustment = new SlotAdjustment
        {
            SlotNameHash = 0x11223344u,
            Offset = new MeshVector3(1, 2, 3)
        };

        // Act
        block.AddAdjustment(adjustment);

        // Assert
        block.Adjustments.Should().HaveCount(1);
        block.Adjustments[0].SlotNameHash.Should().Be(0x11223344u);
    }

    [Fact]
    public void BondBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(BondBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("BOND").Should().BeTrue();
    }

    [Fact]
    public void BondBlock_Registry_CreatesBondBlock()
    {
        // Arrange
        var data = CreateBondBlockEmpty();

        // Act
        var block = RcolBlockRegistry.CreateBlock(BondBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<BondBlock>();
    }

    [Fact]
    public void SlotAdjustment_Equals_IdenticalAdjustments_ReturnsTrue()
    {
        // Arrange
        var adj1 = new SlotAdjustment
        {
            SlotNameHash = 0x1234,
            Offset = new MeshVector3(1, 2, 3),
            Scale = MeshVector3.One,
            Quaternion = new MeshVector4(0, 0, 0, 1)
        };
        var adj2 = new SlotAdjustment
        {
            SlotNameHash = 0x1234,
            Offset = new MeshVector3(1, 2, 3),
            Scale = MeshVector3.One,
            Quaternion = new MeshVector4(0, 0, 0, 1)
        };

        // Assert
        adj1.Equals(adj2).Should().BeTrue();
    }

    [Fact]
    public void SlotAdjustment_Size_IsCorrect()
    {
        // The size should be: slotNameHash(4) + offset(12) + scale(12) + quaternion(16) = 44
        SlotAdjustment.Size.Should().Be(44);
    }
}
