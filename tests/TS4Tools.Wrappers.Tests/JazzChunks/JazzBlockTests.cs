using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.JazzChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.JazzChunks;

/// <summary>
/// Tests for Jazz chunk block parsing and serialization.
/// </summary>
public class JazzBlockTests
{
    #region JazzActorDefinitionBlock Tests

    [Fact]
    public void JazzActorDefinitionBlock_Parse_ReadsCorrectly()
    {
        // Arrange - minimal S_AD block
        var data = CreateActorDefinitionData(version: 0x0100, nameHash: 0x12345678, unknown1: 0x87654321);

        // Act
        var block = new JazzActorDefinitionBlock(data);

        // Assert
        block.Tag.Should().Be("S_AD");
        block.ResourceType.Should().Be(JazzConstants.ActorDefinition);
        block.Version.Should().Be(0x0100u);
        block.NameHash.Should().Be(0x12345678u);
        block.Unknown1.Should().Be(0x87654321u);
    }

    [Fact]
    public void JazzActorDefinitionBlock_Serialize_RoundTrips()
    {
        // Arrange
        var data = CreateActorDefinitionData(version: 0x0100, nameHash: 0xABCDEF01, unknown1: 0x10FEDCBA);
        var block = new JazzActorDefinitionBlock(data);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(data);
    }

    #endregion

    #region JazzParameterDefinitionBlock Tests

    [Fact]
    public void JazzParameterDefinitionBlock_Parse_ReadsCorrectly()
    {
        // Arrange - minimal S_PD block
        var data = CreateParameterDefinitionData(version: 0x0100, nameHash: 0x11111111, defaultValue: 42);

        // Act
        var block = new JazzParameterDefinitionBlock(data);

        // Assert
        block.Tag.Should().Be("S_PD");
        block.ResourceType.Should().Be(JazzConstants.ParameterDefinition);
        block.Version.Should().Be(0x0100u);
        block.NameHash.Should().Be(0x11111111u);
        block.DefaultValue.Should().Be(42u);
    }

    [Fact]
    public void JazzParameterDefinitionBlock_Serialize_RoundTrips()
    {
        // Arrange
        var data = CreateParameterDefinitionData(version: 0x0100, nameHash: 0x22222222, defaultValue: 100);
        var block = new JazzParameterDefinitionBlock(data);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(data);
    }

    #endregion

    #region JazzStateBlock Tests

    [Fact]
    public void JazzStateBlock_Parse_ReadsCorrectly()
    {
        // Arrange - minimal S_St block with no outbound states
        var data = CreateStateData(
            version: 0x0101,
            nameHash: 0x33333333,
            flags: JazzStateFlags.Public | JazzStateFlags.Entry,
            decisionGraphIndex: 5,
            outboundStateCount: 0,
            awarenessLevel: JazzAwarenessLevel.OverlayNone);

        // Act
        var block = new JazzStateBlock(data);

        // Assert
        block.Tag.Should().Be("S_St");
        block.ResourceType.Should().Be(JazzConstants.State);
        block.Version.Should().Be(0x0101u);
        block.NameHash.Should().Be(0x33333333u);
        block.Flags.Should().Be(JazzStateFlags.Public | JazzStateFlags.Entry);
        block.DecisionGraphIndex.Should().Be(5u);
        block.OutboundStateIndexes.Should().BeEmpty();
        block.AwarenessOverlayLevel.Should().Be(JazzAwarenessLevel.OverlayNone);
    }

    [Fact]
    public void JazzStateBlock_Serialize_RoundTrips()
    {
        // Arrange
        var data = CreateStateData(
            version: 0x0101,
            nameHash: 0x44444444,
            flags: JazzStateFlags.Loop,
            decisionGraphIndex: 10,
            outboundStateCount: 0,
            awarenessLevel: JazzAwarenessLevel.Unset);
        var block = new JazzStateBlock(data);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(data);
    }

    #endregion

    #region JazzNextStateNodeBlock Tests

    [Fact]
    public void JazzNextStateNodeBlock_Parse_ReadsCorrectly()
    {
        // Arrange - minimal SNSN block
        var data = CreateNextStateNodeData(version: 0x0101, stateIndex: 7);

        // Act
        var block = new JazzNextStateNodeBlock(data);

        // Assert
        block.Tag.Should().Be("SNSN");
        block.ResourceType.Should().Be(JazzConstants.NextStateNode);
        block.Version.Should().Be(0x0101u);
        block.StateIndex.Should().Be(7u);
    }

    [Fact]
    public void JazzNextStateNodeBlock_Serialize_RoundTrips()
    {
        // Arrange
        var data = CreateNextStateNodeData(version: 0x0101, stateIndex: 15);
        var block = new JazzNextStateNodeBlock(data);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(data);
    }

    #endregion

    #region Constants Tests

    [Fact]
    public void JazzConstants_AllTypes_ContainsAll13Types()
    {
        // Assert
        JazzConstants.AllTypes.Should().HaveCount(13);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.StateMachine);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.State);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.DecisionGraph);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.ActorDefinition);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.ParameterDefinition);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.PlayAnimationNode);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.RandomNode);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.SelectOnParameterNode);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.SelectOnDestinationNode);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.NextStateNode);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.CreatePropNode);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.ActorOperationNode);
        JazzConstants.AllTypes.Should().Contain(JazzConstants.StopAnimationNode);
    }

    [Fact]
    public void JazzConstants_DeadBeef_HasCorrectValue()
    {
        JazzConstants.DeadBeef.Should().Be(0xDEADBEEFu);
    }

    [Fact]
    public void JazzConstants_CloseDgn_HasCorrectValue()
    {
        // "/DGN" in little-endian = 0x4E47442F
        JazzConstants.CloseDgn.Should().Be(0x4E47442Fu);
    }

    #endregion

    #region Helper Methods

    private static byte[] CreateActorDefinitionData(uint version, uint nameHash, uint unknown1)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag "S_AD"
        writer.Write((byte)'S');
        writer.Write((byte)'_');
        writer.Write((byte)'A');
        writer.Write((byte)'D');

        writer.Write(version);
        writer.Write(nameHash);
        writer.Write(unknown1);

        return ms.ToArray();
    }

    private static byte[] CreateParameterDefinitionData(uint version, uint nameHash, uint defaultValue)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag "S_PD"
        writer.Write((byte)'S');
        writer.Write((byte)'_');
        writer.Write((byte)'P');
        writer.Write((byte)'D');

        writer.Write(version);
        writer.Write(nameHash);
        writer.Write(defaultValue);

        return ms.ToArray();
    }

    private static byte[] CreateStateData(
        uint version,
        uint nameHash,
        JazzStateFlags flags,
        uint decisionGraphIndex,
        int outboundStateCount,
        JazzAwarenessLevel awarenessLevel)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag "S_St"
        writer.Write((byte)'S');
        writer.Write((byte)'_');
        writer.Write((byte)'S');
        writer.Write((byte)'t');

        writer.Write(version);
        writer.Write(nameHash);
        writer.Write((uint)flags);
        writer.Write(decisionGraphIndex); // ChunkReference is just a uint
        writer.Write(outboundStateCount); // Count of outbound states
        // No outbound state indexes if count is 0
        writer.Write((uint)awarenessLevel);

        return ms.ToArray();
    }

    private static byte[] CreateNextStateNodeData(uint version, uint stateIndex)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag "SNSN"
        writer.Write((byte)'S');
        writer.Write((byte)'N');
        writer.Write((byte)'S');
        writer.Write((byte)'N');

        writer.Write(version);
        writer.Write(stateIndex);
        writer.Write(JazzConstants.CloseDgn); // /DGN marker

        return ms.ToArray();
    }

    #endregion
}
