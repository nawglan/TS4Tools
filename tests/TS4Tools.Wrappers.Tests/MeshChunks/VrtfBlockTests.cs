using FluentAssertions;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MeshChunks;

/// <summary>
/// Tests for VrtfBlock (Vertex Format) parsing and serialization.
/// </summary>
public class VrtfBlockTests
{
    /// <summary>
    /// Creates a minimal VRTF block with no layouts.
    /// </summary>
    private static byte[] CreateVrtfBlockEmpty()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'R');
        writer.Write((byte)'T');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000002u); // Version
        writer.Write(0); // Stride
        writer.Write(0); // Layout count
        writer.Write(0u); // ExtendedFormat = false

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a VRTF block with a single Position layout.
    /// Position using Float3 format = 12 bytes stride.
    /// </summary>
    private static byte[] CreateVrtfBlockWithPositionOnly()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'R');
        writer.Write((byte)'T');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000002u); // Version
        writer.Write(12); // Stride (Float3 = 12 bytes)
        writer.Write(1); // Layout count
        writer.Write(0u); // ExtendedFormat = false

        // Layout 1: Position, Float3, offset 0
        writer.Write((byte)ElementUsage.Position); // Usage
        writer.Write((byte)0); // UsageIndex
        writer.Write((byte)ElementFormat.Float3); // Format
        writer.Write((byte)0); // Offset

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a VRTF block with multiple layouts: Position, Normal, UV.
    /// Stride = 12 (Float3) + 12 (Float3) + 8 (Float2) = 32 bytes.
    /// </summary>
    private static byte[] CreateVrtfBlockWithMultipleLayouts()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'R');
        writer.Write((byte)'T');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000002u); // Version
        writer.Write(32); // Stride
        writer.Write(3); // Layout count
        writer.Write(1u); // ExtendedFormat = true

        // Layout 1: Position, Float3, offset 0
        writer.Write((byte)ElementUsage.Position);
        writer.Write((byte)0);
        writer.Write((byte)ElementFormat.Float3);
        writer.Write((byte)0);

        // Layout 2: Normal, Float3, offset 12
        writer.Write((byte)ElementUsage.Normal);
        writer.Write((byte)0);
        writer.Write((byte)ElementFormat.Float3);
        writer.Write((byte)12);

        // Layout 3: UV, Float2, offset 24
        writer.Write((byte)ElementUsage.UV);
        writer.Write((byte)0);
        writer.Write((byte)ElementFormat.Float2);
        writer.Write((byte)24);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a VRTF block with multiple UV channels.
    /// </summary>
    private static byte[] CreateVrtfBlockWithMultipleUVChannels()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Tag
        writer.Write((byte)'V');
        writer.Write((byte)'R');
        writer.Write((byte)'T');
        writer.Write((byte)'F');

        // Header
        writer.Write(0x00000002u); // Version
        writer.Write(28); // Stride = 12 (Float3) + 8 (Float2) + 8 (Float2)
        writer.Write(3); // Layout count
        writer.Write(0u); // ExtendedFormat = false

        // Layout 1: Position, Float3, offset 0
        writer.Write((byte)ElementUsage.Position);
        writer.Write((byte)0);
        writer.Write((byte)ElementFormat.Float3);
        writer.Write((byte)0);

        // Layout 2: UV0, Float2, offset 12
        writer.Write((byte)ElementUsage.UV);
        writer.Write((byte)0); // UsageIndex = 0
        writer.Write((byte)ElementFormat.Float2);
        writer.Write((byte)12);

        // Layout 3: UV1, Float2, offset 20
        writer.Write((byte)ElementUsage.UV);
        writer.Write((byte)1); // UsageIndex = 1
        writer.Write((byte)ElementFormat.Float2);
        writer.Write((byte)20);

        return ms.ToArray();
    }

    #region Parsing Tests

    [Fact]
    public void VrtfBlock_Parse_EmptyBlock_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVrtfBlockEmpty();

        // Act
        var block = new VrtfBlock(data);

        // Assert
        block.Tag.Should().Be("VRTF");
        block.ResourceType.Should().Be(VrtfBlock.TypeId);
        block.IsKnownType.Should().BeTrue();
        block.Version.Should().Be(2);
        block.Stride.Should().Be(0);
        block.ExtendedFormat.Should().BeFalse();
        block.Layouts.Should().BeEmpty();
    }

    [Fact]
    public void VrtfBlock_Parse_SingleLayout_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVrtfBlockWithPositionOnly();

        // Act
        var block = new VrtfBlock(data);

        // Assert
        block.Version.Should().Be(2);
        block.Stride.Should().Be(12);
        block.ExtendedFormat.Should().BeFalse();
        block.Layouts.Should().HaveCount(1);

        var layout = block.Layouts[0];
        layout.Usage.Should().Be(ElementUsage.Position);
        layout.UsageIndex.Should().Be(0);
        layout.Format.Should().Be(ElementFormat.Float3);
        layout.Offset.Should().Be(0);
    }

    [Fact]
    public void VrtfBlock_Parse_MultipleLayouts_ParsesCorrectly()
    {
        // Arrange
        var data = CreateVrtfBlockWithMultipleLayouts();

        // Act
        var block = new VrtfBlock(data);

        // Assert
        block.Stride.Should().Be(32);
        block.ExtendedFormat.Should().BeTrue();
        block.Layouts.Should().HaveCount(3);

        // Verify Position layout
        block.Layouts[0].Usage.Should().Be(ElementUsage.Position);
        block.Layouts[0].Format.Should().Be(ElementFormat.Float3);
        block.Layouts[0].Offset.Should().Be(0);

        // Verify Normal layout
        block.Layouts[1].Usage.Should().Be(ElementUsage.Normal);
        block.Layouts[1].Format.Should().Be(ElementFormat.Float3);
        block.Layouts[1].Offset.Should().Be(12);

        // Verify UV layout
        block.Layouts[2].Usage.Should().Be(ElementUsage.UV);
        block.Layouts[2].Format.Should().Be(ElementFormat.Float2);
        block.Layouts[2].Offset.Should().Be(24);
    }

    [Fact]
    public void VrtfBlock_Parse_InvalidTag_ThrowsException()
    {
        // Arrange - create data with wrong tag
        var data = new byte[]
        {
            (byte)'X', (byte)'R', (byte)'T', (byte)'F', // Wrong tag
            0x02, 0x00, 0x00, 0x00, // Version
            0x00, 0x00, 0x00, 0x00, // Stride
            0x00, 0x00, 0x00, 0x00, // Layout count
            0x00, 0x00, 0x00, 0x00  // ExtendedFormat
        };

        // Act & Assert
        var action = () => new VrtfBlock(data);
        action.Should().Throw<InvalidDataException>()
            .WithMessage("*Invalid VRTF tag*");
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void VrtfBlock_Serialize_EmptyBlock_RoundTrips()
    {
        // Arrange
        var originalData = CreateVrtfBlockEmpty();
        var block = new VrtfBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VrtfBlock_Serialize_SingleLayout_RoundTrips()
    {
        // Arrange
        var originalData = CreateVrtfBlockWithPositionOnly();
        var block = new VrtfBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VrtfBlock_Serialize_MultipleLayouts_RoundTrips()
    {
        // Arrange
        var originalData = CreateVrtfBlockWithMultipleLayouts();
        var block = new VrtfBlock(originalData);

        // Act
        var serialized = block.Serialize();

        // Assert
        serialized.ToArray().Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void VrtfBlock_Serialize_ParsedDataMatchesOriginal()
    {
        // Arrange
        var originalData = CreateVrtfBlockWithMultipleLayouts();
        var block = new VrtfBlock(originalData);
        var serialized = block.Serialize();

        // Act - parse the serialized data again
        var reparsed = new VrtfBlock(serialized.Span);

        // Assert
        reparsed.Version.Should().Be(block.Version);
        reparsed.Stride.Should().Be(block.Stride);
        reparsed.ExtendedFormat.Should().Be(block.ExtendedFormat);
        reparsed.Layouts.Should().HaveCount(block.Layouts.Count);

        for (int i = 0; i < block.Layouts.Count; i++)
        {
            reparsed.Layouts[i].Should().Be(block.Layouts[i]);
        }
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void VrtfBlock_CreateDefaultForSunShadow_CreatesValidBlock()
    {
        // Act
        var block = VrtfBlock.CreateDefaultForSunShadow();

        // Assert
        block.Tag.Should().Be("VRTF");
        block.Layouts.Should().HaveCount(1);
        block.Layouts[0].Usage.Should().Be(ElementUsage.Position);
        block.Layouts[0].Format.Should().Be(ElementFormat.Short4);
        block.Layouts[0].Offset.Should().Be(0);
        block.Stride.Should().Be(8); // Short4 = 8 bytes
    }

    [Fact]
    public void VrtfBlock_CreateDefaultForDropShadow_CreatesValidBlock()
    {
        // Act
        var block = VrtfBlock.CreateDefaultForDropShadow();

        // Assert
        block.Tag.Should().Be("VRTF");
        block.Layouts.Should().HaveCount(2);

        // First layout: Position with UShort4N
        block.Layouts[0].Usage.Should().Be(ElementUsage.Position);
        block.Layouts[0].Format.Should().Be(ElementFormat.UShort4N);
        block.Layouts[0].Offset.Should().Be(0);

        // Second layout: UV with Short4DropShadow
        block.Layouts[1].Usage.Should().Be(ElementUsage.UV);
        block.Layouts[1].Format.Should().Be(ElementFormat.Short4DropShadow);
        block.Layouts[1].Offset.Should().Be(8); // After UShort4N

        block.Stride.Should().Be(16); // UShort4N (8) + Short4DropShadow (8)
    }

    [Fact]
    public void VrtfBlock_CreateDefaultForSunShadow_CanSerialize()
    {
        // Arrange
        var block = VrtfBlock.CreateDefaultForSunShadow();

        // Act
        var serialized = block.Serialize();
        var reparsed = new VrtfBlock(serialized.Span);

        // Assert
        reparsed.Layouts.Should().HaveCount(1);
        reparsed.Layouts[0].Usage.Should().Be(ElementUsage.Position);
        reparsed.Stride.Should().Be(block.Stride);
    }

    [Fact]
    public void VrtfBlock_CreateDefaultForDropShadow_CanSerialize()
    {
        // Arrange
        var block = VrtfBlock.CreateDefaultForDropShadow();

        // Act
        var serialized = block.Serialize();
        var reparsed = new VrtfBlock(serialized.Span);

        // Assert
        reparsed.Layouts.Should().HaveCount(2);
        reparsed.Stride.Should().Be(block.Stride);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void VrtfBlock_FindLayout_FindsByUsage()
    {
        // Arrange
        var data = CreateVrtfBlockWithMultipleLayouts();
        var block = new VrtfBlock(data);

        // Act
        var normalLayout = block.FindLayout(ElementUsage.Normal);

        // Assert
        normalLayout.Should().NotBeNull();
        normalLayout!.Value.Usage.Should().Be(ElementUsage.Normal);
        normalLayout.Value.Format.Should().Be(ElementFormat.Float3);
        normalLayout.Value.Offset.Should().Be(12);
    }

    [Fact]
    public void VrtfBlock_FindLayout_NotFound_ReturnsNull()
    {
        // Arrange
        var data = CreateVrtfBlockWithPositionOnly();
        var block = new VrtfBlock(data);

        // Act
        var tangentLayout = block.FindLayout(ElementUsage.Tangent);

        // Assert
        tangentLayout.Should().BeNull();
    }

    [Fact]
    public void VrtfBlock_FindLayouts_ReturnsMultiple()
    {
        // Arrange
        var data = CreateVrtfBlockWithMultipleUVChannels();
        var block = new VrtfBlock(data);

        // Act
        var uvLayouts = block.FindLayouts(ElementUsage.UV).ToList();

        // Assert
        uvLayouts.Should().HaveCount(2);
        uvLayouts[0].UsageIndex.Should().Be(0);
        uvLayouts[1].UsageIndex.Should().Be(1);
    }

    [Fact]
    public void VrtfBlock_FindLayouts_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var data = CreateVrtfBlockWithPositionOnly();
        var block = new VrtfBlock(data);

        // Act
        var colorLayouts = block.FindLayouts(ElementUsage.Colour).ToList();

        // Assert
        colorLayouts.Should().BeEmpty();
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void VrtfBlock_Registry_IsRegistered()
    {
        // Assert
        RcolBlockRegistry.IsRegistered(VrtfBlock.TypeId).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("VRTF").Should().BeTrue();
    }

    [Fact]
    public void VrtfBlock_Registry_CreatesVrtfBlock()
    {
        // Arrange
        var data = CreateVrtfBlockWithPositionOnly();

        // Act
        var block = RcolBlockRegistry.CreateBlock(VrtfBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<VrtfBlock>();
        ((VrtfBlock)block!).Layouts.Should().HaveCount(1);
    }

    #endregion

    #region ElementLayout Tests

    [Fact]
    public void ElementLayout_Equals_IdenticalLayouts_ReturnsTrue()
    {
        // Arrange
        var layout1 = new ElementLayout(ElementUsage.Position, 0, ElementFormat.Float3, 0);
        var layout2 = new ElementLayout(ElementUsage.Position, 0, ElementFormat.Float3, 0);

        // Assert
        layout1.Equals(layout2).Should().BeTrue();
        (layout1 == layout2).Should().BeTrue();
    }

    [Fact]
    public void ElementLayout_Equals_DifferentLayouts_ReturnsFalse()
    {
        // Arrange
        var layout1 = new ElementLayout(ElementUsage.Position, 0, ElementFormat.Float3, 0);
        var layout2 = new ElementLayout(ElementUsage.Normal, 0, ElementFormat.Float3, 0);

        // Assert
        layout1.Equals(layout2).Should().BeFalse();
        (layout1 != layout2).Should().BeTrue();
    }

    [Fact]
    public void ElementLayout_Size_IsCorrect()
    {
        // The size should be: Usage(1) + UsageIndex(1) + Format(1) + Offset(1) = 4
        ElementLayout.Size.Should().Be(4);
    }

    [Fact]
    public void ElementLayout_ToString_ReturnsFormattedString()
    {
        // Arrange
        var layout = new ElementLayout(ElementUsage.UV, 1, ElementFormat.Float2, 24);

        // Act
        var str = layout.ToString();

        // Assert
        str.Should().Contain("UV");
        str.Should().Contain("[1]");
        str.Should().Contain("Float2");
    }

    [Fact]
    public void ElementLayout_GetHashCode_SameForIdenticalLayouts()
    {
        // Arrange
        var layout1 = new ElementLayout(ElementUsage.Normal, 0, ElementFormat.Float3, 12);
        var layout2 = new ElementLayout(ElementUsage.Normal, 0, ElementFormat.Float3, 12);

        // Assert
        layout1.GetHashCode().Should().Be(layout2.GetHashCode());
    }

    #endregion

    #region ElementFormat Extension Tests

    [Theory]
    [InlineData(ElementFormat.Float1, 4)]
    [InlineData(ElementFormat.Float2, 8)]
    [InlineData(ElementFormat.Float3, 12)]
    [InlineData(ElementFormat.Float4, 16)]
    [InlineData(ElementFormat.Short4, 8)]
    [InlineData(ElementFormat.UShort4N, 8)]
    [InlineData(ElementFormat.Short4DropShadow, 8)]
    public void ElementFormat_ByteSize_ReturnsCorrectSize(ElementFormat format, int expectedSize)
    {
        // Act
        var size = format.ByteSize();

        // Assert
        size.Should().Be(expectedSize);
    }

    [Theory]
    [InlineData(ElementFormat.Float1, 1)]
    [InlineData(ElementFormat.Float2, 2)]
    [InlineData(ElementFormat.Float3, 3)]
    [InlineData(ElementFormat.Float4, 4)]
    public void ElementFormat_FloatCount_ReturnsCorrectCount(ElementFormat format, int expectedCount)
    {
        // Act
        var count = format.FloatCount();

        // Assert
        count.Should().Be(expectedCount);
    }

    #endregion
}
