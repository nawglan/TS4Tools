using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for RsltBlock parsing and serialization.
/// </summary>
public class RsltBlockTests
{
    /// <summary>
    /// Creates a minimal RSLT block with no parts.
    /// </summary>
    private static byte[] CreateMinimalRslt()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: RSLT
        writer.Write((byte)'R');
        writer.Write((byte)'S');
        writer.Write((byte)'L');
        writer.Write((byte)'T');

        // Version
        writer.Write(4u);

        // 5 counts (all zero)
        writer.Write(0); // routes
        writer.Write(0); // containers
        writer.Write(0); // effects
        writer.Write(0); // IK targets
        writer.Write(0); // cones

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an RSLT block with one route part.
    /// </summary>
    private static byte[] CreateRsltWithRoute()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: RSLT
        writer.Write((byte)'R');
        writer.Write((byte)'S');
        writer.Write((byte)'L');
        writer.Write((byte)'T');

        // Version
        writer.Write(4u);

        // Counts: 1 route, 0 others
        writer.Write(1); // routes
        writer.Write(0); // containers
        writer.Write(0); // effects
        writer.Write(0); // IK targets
        writer.Write(0); // cones

        // Route parts (structure-of-arrays format)
        // Slot names array
        writer.Write(0x12345678u); // slotName[0]

        // Bone names array
        writer.Write(0xDEADBEEFu); // boneName[0]

        // Transform matrices and coordinates (interleaved per part)
        // MatrixX + CoordX
        writer.Write(1f); writer.Write(0f); writer.Write(0f); // matrixX row
        writer.Write(0f); // coordX

        // MatrixY + CoordY
        writer.Write(0f); writer.Write(1f); writer.Write(0f); // matrixY row
        writer.Write(0f); // coordY

        // MatrixZ + CoordZ
        writer.Write(0f); writer.Write(0f); writer.Write(1f); // matrixZ row
        writer.Write(0f); // coordZ

        // Route offsets list (count + entries)
        writer.Write(1); // count
        writer.Write(0); // slotIndex
        writer.Write(0f); writer.Write(0f); writer.Write(0f); // position
        writer.Write(0f); writer.Write(0f); writer.Write(0f); // rotation

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an RSLT block with one container part.
    /// </summary>
    private static byte[] CreateRsltWithContainer()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: RSLT
        writer.Write((byte)'R');
        writer.Write((byte)'S');
        writer.Write((byte)'L');
        writer.Write((byte)'T');

        // Version
        writer.Write(4u);

        // Counts: 0 routes, 1 container, 0 others
        writer.Write(0); // routes
        writer.Write(1); // containers
        writer.Write(0); // effects
        writer.Write(0); // IK targets
        writer.Write(0); // cones

        // Container parts (slotted parts, structure-of-arrays format)
        // Slot names array
        writer.Write(0x11111111u); // slotName[0]

        // Bone names array
        writer.Write(0x22222222u); // boneName[0]

        // Slot sizes array (byte)
        writer.Write((byte)0x10);

        // Slot type sets array (ulong)
        writer.Write(0x0000000000000038UL); // Small | Medium | Large

        // Slot direction locked array (bool/byte)
        writer.Write(true);

        // Slot legacy hashes array
        writer.Write(0x33333333u);

        // Transform matrices and coordinates (interleaved per part)
        // MatrixX + CoordX
        writer.Write(1f); writer.Write(0f); writer.Write(0f);
        writer.Write(1f); // coordX

        // MatrixY + CoordY
        writer.Write(0f); writer.Write(1f); writer.Write(0f);
        writer.Write(2f); // coordY

        // MatrixZ + CoordZ
        writer.Write(0f); writer.Write(0f); writer.Write(1f);
        writer.Write(3f); // coordZ

        // Container offsets list
        writer.Write(0); // count = 0

        return ms.ToArray();
    }

    /// <summary>
    /// Creates an RSLT block with two routes to verify multiple parts.
    /// </summary>
    private static byte[] CreateRsltWithTwoRoutes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: RSLT
        writer.Write((byte)'R');
        writer.Write((byte)'S');
        writer.Write((byte)'L');
        writer.Write((byte)'T');

        // Version
        writer.Write(4u);

        // Counts: 2 routes
        writer.Write(2); // routes
        writer.Write(0); // containers
        writer.Write(0); // effects
        writer.Write(0); // IK targets
        writer.Write(0); // cones

        // Route parts - structure of arrays
        // All slot names first
        writer.Write(0xAAAAAAAAu); // slotName[0]
        writer.Write(0xBBBBBBBBu); // slotName[1]

        // All bone names
        writer.Write(0xCCCCCCCCu); // boneName[0]
        writer.Write(0xDDDDDDDDu); // boneName[1]

        // Transform for part 0
        writer.Write(1f); writer.Write(0f); writer.Write(0f);
        writer.Write(10f); // coordX
        writer.Write(0f); writer.Write(1f); writer.Write(0f);
        writer.Write(20f); // coordY
        writer.Write(0f); writer.Write(0f); writer.Write(1f);
        writer.Write(30f); // coordZ

        // Transform for part 1
        writer.Write(0.5f); writer.Write(0.5f); writer.Write(0f);
        writer.Write(100f);
        writer.Write(0.5f); writer.Write(0.5f); writer.Write(0f);
        writer.Write(200f);
        writer.Write(0f); writer.Write(0f); writer.Write(1f);
        writer.Write(300f);

        // Route offsets
        writer.Write(2); // count
        // Offset 0
        writer.Write(0); // slotIndex
        writer.Write(1f); writer.Write(2f); writer.Write(3f); // position
        writer.Write(0f); writer.Write(0f); writer.Write(0f); // rotation
        // Offset 1
        writer.Write(1); // slotIndex
        writer.Write(4f); writer.Write(5f); writer.Write(6f);
        writer.Write(0.1f); writer.Write(0.2f); writer.Write(0.3f);

        return ms.ToArray();
    }

    [Fact]
    public void RsltBlock_Parse_MinimalBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMinimalRslt();

        // Act
        var block = new RsltBlock(data);

        // Assert
        block.Tag.Should().Be("RSLT");
        block.ResourceType.Should().Be(RsltBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(4);
        block.Routes.Should().BeEmpty();
        block.RouteOffsets.Should().BeEmpty();
        block.Containers.Should().BeEmpty();
        block.ContainerOffsets.Should().BeEmpty();
        block.Effects.Should().BeEmpty();
        block.EffectOffsets.Should().BeEmpty();
        block.InverseKineticsTargets.Should().BeEmpty();
        block.InverseKineticsTargetOffsets.Should().BeEmpty();
        block.Cones.Should().BeEmpty();
        block.ConeOffsets.Should().BeEmpty();
    }

    [Fact]
    public void RsltBlock_Parse_WithRoute_ParsesCorrectly()
    {
        // Arrange
        var data = CreateRsltWithRoute();

        // Act
        var block = new RsltBlock(data);

        // Assert
        block.Routes.Should().HaveCount(1);
        var route = block.Routes[0];
        route.SlotNameHash.Should().Be(0x12345678u);
        route.BoneNameHash.Should().Be(0xDEADBEEFu);
        route.MatrixX.R1.Should().Be(1f);
        route.MatrixX.R2.Should().Be(0f);
        route.MatrixX.R3.Should().Be(0f);
        route.Coordinates.X.Should().Be(0f);
        route.Coordinates.Y.Should().Be(0f);
        route.Coordinates.Z.Should().Be(0f);

        block.RouteOffsets.Should().HaveCount(1);
        block.RouteOffsets[0].SlotIndex.Should().Be(0);
    }

    [Fact]
    public void RsltBlock_Parse_WithContainer_ParsesCorrectly()
    {
        // Arrange
        var data = CreateRsltWithContainer();

        // Act
        var block = new RsltBlock(data);

        // Assert
        block.Containers.Should().HaveCount(1);
        var container = block.Containers[0];
        container.SlotNameHash.Should().Be(0x11111111u);
        container.BoneNameHash.Should().Be(0x22222222u);
        container.SlotSize.Should().Be(0x10);
        container.SlotTypeSet.Should().Be(0x38UL);
        container.SlotDirectionLocked.Should().BeTrue();
        container.SlotLegacyHash.Should().Be(0x33333333u);
        container.Coordinates.X.Should().Be(1f);
        container.Coordinates.Y.Should().Be(2f);
        container.Coordinates.Z.Should().Be(3f);

        block.ContainerOffsets.Should().BeEmpty();
    }

    [Fact]
    public void RsltBlock_Parse_WithTwoRoutes_ParsesCorrectly()
    {
        // Arrange
        var data = CreateRsltWithTwoRoutes();

        // Act
        var block = new RsltBlock(data);

        // Assert
        block.Routes.Should().HaveCount(2);

        // First route
        block.Routes[0].SlotNameHash.Should().Be(0xAAAAAAAAu);
        block.Routes[0].BoneNameHash.Should().Be(0xCCCCCCCCu);
        block.Routes[0].Coordinates.X.Should().Be(10f);
        block.Routes[0].Coordinates.Y.Should().Be(20f);
        block.Routes[0].Coordinates.Z.Should().Be(30f);

        // Second route
        block.Routes[1].SlotNameHash.Should().Be(0xBBBBBBBBu);
        block.Routes[1].BoneNameHash.Should().Be(0xDDDDDDDDu);
        block.Routes[1].Coordinates.X.Should().Be(100f);
        block.Routes[1].Coordinates.Y.Should().Be(200f);
        block.Routes[1].Coordinates.Z.Should().Be(300f);

        // Route offsets
        block.RouteOffsets.Should().HaveCount(2);
        block.RouteOffsets[0].SlotIndex.Should().Be(0);
        block.RouteOffsets[0].Position.X.Should().Be(1f);
        block.RouteOffsets[1].SlotIndex.Should().Be(1);
        block.RouteOffsets[1].Rotation.Z.Should().Be(0.3f);
    }

    [Fact]
    public void RsltBlock_Serialize_MinimalBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateMinimalRslt();
        var block = new RsltBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void RsltBlock_Serialize_WithRoute_RoundTrips()
    {
        // Arrange
        var originalData = CreateRsltWithRoute();
        var block = new RsltBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void RsltBlock_Serialize_WithContainer_RoundTrips()
    {
        // Arrange
        var originalData = CreateRsltWithContainer();
        var block = new RsltBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void RsltBlock_Serialize_WithTwoRoutes_RoundTrips()
    {
        // Arrange
        var originalData = CreateRsltWithTwoRoutes();
        var block = new RsltBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void RsltBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = CreateMinimalRslt();
        data[0] = (byte)'X'; // Corrupt the tag

        // Act & Assert
        var action = () => new RsltBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid RSLT tag*");
    }

    [Fact]
    public void RsltBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(RsltBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("RSLT").Should().BeTrue();
    }

    [Fact]
    public void RsltBlock_Registry_CreatesRsltBlock()
    {
        // Arrange
        var data = CreateMinimalRslt();

        // Act
        var block = RcolBlockRegistry.CreateBlock(RsltBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<RsltBlock>();
    }
}
