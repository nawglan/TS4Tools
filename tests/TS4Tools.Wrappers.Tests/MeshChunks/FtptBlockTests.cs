using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for FtptBlock parsing and serialization.
/// </summary>
public class FtptBlockTests
{
    /// <summary>
    /// Creates a minimal FTPT block with Type != 0 (uses height overrides).
    /// </summary>
    private static byte[] CreateFtptWithHeightOverrides()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: FTPT
        writer.Write((byte)'F');
        writer.Write((byte)'T');
        writer.Write((byte)'P');
        writer.Write((byte)'T');

        // Header
        writer.Write(12u); // Version
        writer.Write(0x123456789ABCDEF0UL); // Instance
        writer.Write(1u); // Type (non-zero)
        writer.Write(0u); // Group

        // Min height overrides: 1 entry
        writer.Write(1); // count
        writer.Write(0xDEADBEEFu); // nameHash
        writer.Write(1.5f); // height

        // Max height overrides: 0 entries
        writer.Write(0); // count

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a FTPT block with Type == 0 (uses areas).
    /// </summary>
    private static byte[] CreateFtptWithAreas()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: FTPT
        writer.Write((byte)'F');
        writer.Write((byte)'T');
        writer.Write((byte)'P');
        writer.Write((byte)'T');

        // Header
        writer.Write(12u); // Version
        writer.Write(0x123456789ABCDEF0UL); // Instance
        writer.Write(0u); // Type (zero = use areas)
        writer.Write(0u); // Group

        // Footprint areas: 1 area
        writer.Write(1); // count
        writer.Write(0x12345678u); // nameHash
        writer.Write((byte)1); // priority
        writer.Write(0u); // areaType

        // Points: 3 points (triangle)
        writer.Write(3); // point count
        writer.Write(0f); writer.Write(0f);  // point 0
        writer.Write(1f); writer.Write(0f);  // point 1
        writer.Write(0.5f); writer.Write(1f); // point 2

        // Surface types: 0 entries
        writer.Write(0);

        // Bounding box
        writer.Write(0f);   // minX
        writer.Write(0f);   // minZ
        writer.Write(1f);   // maxX
        writer.Write(1f);   // maxZ

        // Slot areas: 0 entries
        writer.Write(0);

        // Heights
        writer.Write(10f); // maxHeight
        writer.Write(0f);  // minHeight

        return ms.ToArray();
    }

    [Fact]
    public void FtptBlock_Parse_WithHeightOverrides_ParsesCorrectly()
    {
        // Arrange
        var data = CreateFtptWithHeightOverrides();

        // Act
        var block = new FtptBlock(data);

        // Assert
        block.Tag.Should().Be("FTPT");
        block.ResourceType.Should().Be(FtptBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(12);
        block.Instance.Should().Be(0x123456789ABCDEF0UL);
        block.Type.Should().Be(1);
        block.Group.Should().Be(0);
        block.HasHeightOverrides.Should().BeTrue();

        block.MinHeightOverrides.Should().HaveCount(1);
        block.MinHeightOverrides[0].NameHash.Should().Be(0xDEADBEEFu);
        block.MinHeightOverrides[0].Height.Should().Be(1.5f);

        block.MaxHeightOverrides.Should().BeEmpty();
        block.FootprintAreas.Should().BeEmpty();
        block.SlotAreas.Should().BeEmpty();
    }

    [Fact]
    public void FtptBlock_Parse_WithAreas_ParsesCorrectly()
    {
        // Arrange
        var data = CreateFtptWithAreas();

        // Act
        var block = new FtptBlock(data);

        // Assert
        block.Type.Should().Be(0);
        block.HasHeightOverrides.Should().BeFalse();
        block.MaxHeight.Should().Be(10f);
        block.MinHeight.Should().Be(0f);

        block.FootprintAreas.Should().HaveCount(1);
        var area = block.FootprintAreas[0];
        area.NameHash.Should().Be(0x12345678u);
        area.Priority.Should().Be(1);
        area.AreaType.Should().Be(0);
        area.Points.Should().HaveCount(3);
        area.Points[0].X.Should().Be(0f);
        area.Points[0].Z.Should().Be(0f);
        area.Points[2].X.Should().Be(0.5f);
        area.Points[2].Z.Should().Be(1f);
        area.MinX.Should().Be(0f);
        area.MinZ.Should().Be(0f);
        area.MaxX.Should().Be(1f);
        area.MaxZ.Should().Be(1f);

        block.SlotAreas.Should().BeEmpty();
    }

    [Fact]
    public void FtptBlock_Serialize_WithHeightOverrides_RoundTrips()
    {
        // Arrange
        var originalData = CreateFtptWithHeightOverrides();
        var block = new FtptBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void FtptBlock_Serialize_WithAreas_RoundTrips()
    {
        // Arrange
        var originalData = CreateFtptWithAreas();
        var block = new FtptBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void FtptBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = CreateFtptWithHeightOverrides();
        data[0] = (byte)'X'; // Corrupt the tag

        // Act & Assert
        var action = () => new FtptBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid FTPT tag*");
    }

    [Fact]
    public void FtptBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(FtptBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("FTPT").Should().BeTrue();
    }

    [Fact]
    public void FtptBlock_Registry_CreatesFtptBlock()
    {
        // Arrange
        var data = CreateFtptWithHeightOverrides();

        // Act
        var block = RcolBlockRegistry.CreateBlock(FtptBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<FtptBlock>();
    }
}
