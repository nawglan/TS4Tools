using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for LiteBlock parsing and serialization.
/// </summary>
public class LiteBlockTests
{
    /// <summary>
    /// Creates a minimal LITE block with no light sources or occluders.
    /// </summary>
    private static byte[] CreateMinimalLite(uint version = 4, uint unknown1 = 0x84, ushort unknown2 = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: LITE
        writer.Write((byte)'L');
        writer.Write((byte)'I');
        writer.Write((byte)'T');
        writer.Write((byte)'E');

        // Version
        writer.Write(version);

        // Unknown1
        writer.Write(unknown1);

        // Light source count, occluder count
        writer.Write((byte)0);
        writer.Write((byte)0);

        // Unknown2
        writer.Write(unknown2);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a LITE block with one light source.
    /// </summary>
    private static byte[] CreateLiteWithLightSource()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: LITE
        writer.Write((byte)'L');
        writer.Write((byte)'I');
        writer.Write((byte)'T');
        writer.Write((byte)'E');

        // Header
        writer.Write(4u); // Version
        writer.Write(0x84u); // Unknown1
        writer.Write((byte)1); // Light source count
        writer.Write((byte)0); // Occluder count
        writer.Write((ushort)0); // Unknown2

        // Light source (128 bytes)
        writer.Write((uint)LightSourceType.Point); // type
        writer.Write(1f); writer.Write(2f); writer.Write(3f); // transform
        writer.Write(1f); writer.Write(0.5f); writer.Write(0f); // color (RGB)
        writer.Write(100f); // intensity
        // 24 floats of light data
        for (int i = 0; i < 24; i++)
            writer.Write((float)i);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a LITE block with one occluder.
    /// </summary>
    private static byte[] CreateLiteWithOccluder()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag: LITE
        writer.Write((byte)'L');
        writer.Write((byte)'I');
        writer.Write((byte)'T');
        writer.Write((byte)'E');

        // Header
        writer.Write(4u); // Version
        writer.Write(0x84u); // Unknown1
        writer.Write((byte)0); // Light source count
        writer.Write((byte)1); // Occluder count
        writer.Write((ushort)0); // Unknown2

        // Occluder (56 bytes)
        writer.Write((uint)OccluderType.Disc); // type
        writer.Write(0f); writer.Write(0f); writer.Write(0f); // origin
        writer.Write(0f); writer.Write(1f); writer.Write(0f); // normal
        writer.Write(1f); writer.Write(0f); writer.Write(0f); // xAxis
        writer.Write(0f); writer.Write(0f); writer.Write(1f); // yAxis
        writer.Write(0.5f); // pairOffset

        return ms.ToArray();
    }

    [Fact]
    public void LiteBlock_Parse_MinimalBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateMinimalLite();

        // Act
        var block = new LiteBlock(data);

        // Assert
        block.Tag.Should().Be("LITE");
        block.ResourceType.Should().Be(LiteBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(4);
        block.Unknown1.Should().Be(0x84);
        block.Unknown2.Should().Be(0);
        block.LightSources.Should().BeEmpty();
        block.Occluders.Should().BeEmpty();
    }

    [Fact]
    public void LiteBlock_Parse_WithLightSource_ParsesCorrectly()
    {
        // Arrange
        var data = CreateLiteWithLightSource();

        // Act
        var block = new LiteBlock(data);

        // Assert
        block.LightSources.Should().HaveCount(1);
        var ls = block.LightSources[0];
        ls.LightType.Should().Be(LightSourceType.Point);
        ls.Transform.X.Should().Be(1f);
        ls.Transform.Y.Should().Be(2f);
        ls.Transform.Z.Should().Be(3f);
        ls.Color.X.Should().Be(1f); // R
        ls.Color.Y.Should().Be(0.5f); // G
        ls.Color.Z.Should().Be(0f); // B
        ls.Intensity.Should().Be(100f);
        ls.LightData.Should().HaveCount(24);
        ls.LightData[0].Should().Be(0f);
        ls.LightData[23].Should().Be(23f);
    }

    [Fact]
    public void LiteBlock_Parse_WithOccluder_ParsesCorrectly()
    {
        // Arrange
        var data = CreateLiteWithOccluder();

        // Act
        var block = new LiteBlock(data);

        // Assert
        block.Occluders.Should().HaveCount(1);
        var occ = block.Occluders[0];
        occ.OccluderType.Should().Be(OccluderType.Disc);
        occ.Origin.X.Should().Be(0f);
        occ.Origin.Y.Should().Be(0f);
        occ.Origin.Z.Should().Be(0f);
        occ.Normal.Y.Should().Be(1f);
        occ.XAxis.X.Should().Be(1f);
        occ.YAxis.Z.Should().Be(1f);
        occ.PairOffset.Should().Be(0.5f);
    }

    [Fact]
    public void LiteBlock_Serialize_MinimalBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateMinimalLite();
        var block = new LiteBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void LiteBlock_Serialize_WithLightSource_RoundTrips()
    {
        // Arrange
        var originalData = CreateLiteWithLightSource();
        var block = new LiteBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void LiteBlock_Serialize_WithOccluder_RoundTrips()
    {
        // Arrange
        var originalData = CreateLiteWithOccluder();
        var block = new LiteBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void LiteBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange
        var data = CreateMinimalLite();
        data[0] = (byte)'X'; // Corrupt the tag

        // Act & Assert
        var action = () => new LiteBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid LITE tag*");
    }

    [Fact]
    public void LiteBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(LiteBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("LITE").Should().BeTrue();
    }

    [Fact]
    public void LiteBlock_Registry_CreatesLiteBlock()
    {
        // Arrange
        var data = CreateMinimalLite();

        // Act
        var block = RcolBlockRegistry.CreateBlock(LiteBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<LiteBlock>();
    }

    [Fact]
    public void LightSourceType_HasCorrectValues()
    {
        // Assert - verify the values match legacy
        ((uint)LightSourceType.Unknown).Should().Be(0x00);
        ((uint)LightSourceType.Point).Should().Be(0x03);
        ((uint)LightSourceType.Spot).Should().Be(0x04);
        ((uint)LightSourceType.LampShade).Should().Be(0x05);
        ((uint)LightSourceType.WorldLight).Should().Be(0x0B);
    }

    [Fact]
    public void OccluderType_HasCorrectValues()
    {
        // Assert
        ((uint)OccluderType.Disc).Should().Be(0x00);
        ((uint)OccluderType.Rectangle).Should().Be(0x01);
    }

    [Fact]
    public void LiteLightSource_InvalidLightDataLength_ThrowsException()
    {
        // Arrange
        var invalidData = new float[10]; // Should be 24

        // Act & Assert
        var action = () => new LiteLightSource(
            LightSourceType.Point,
            new MeshVector3(0, 0, 0),
            new MeshVector3(1, 1, 1),
            1f,
            invalidData);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*24 elements*");
    }
}
