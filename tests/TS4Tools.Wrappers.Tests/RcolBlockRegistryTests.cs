using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.MeshChunks;
using Xunit;

namespace TS4Tools.Wrappers.Tests;

/// <summary>
/// Tests for RcolBlockRegistry.
/// </summary>
public class RcolBlockRegistryTests
{
    [Fact]
    public void IsRegistered_VrtfType_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(VrtfBlock.TypeId).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_VbufType_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(VbufBlock.TypeId).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_IbufType_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(IbufBlock.TypeId).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_SkinType_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(SkinBlock.TypeId).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_GeomType_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(GeomBlock.TypeId).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_ModlType_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(ModlBlock.TypeId).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_MlodType_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(MlodBlock.TypeId).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_VbsiType_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(VbsiBlock.TypeId).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_UnknownType_ReturnsFalse()
    {
        RcolBlockRegistry.IsRegistered(0xDEADBEEF).Should().BeFalse();
    }

    [Fact]
    public void IsTagRegistered_VRTF_ReturnsTrue()
    {
        RcolBlockRegistry.IsTagRegistered("VRTF").Should().BeTrue();
    }

    [Fact]
    public void IsTagRegistered_VBUF_ReturnsTrue()
    {
        RcolBlockRegistry.IsTagRegistered("VBUF").Should().BeTrue();
    }

    [Fact]
    public void IsTagRegistered_Unknown_ReturnsFalse()
    {
        RcolBlockRegistry.IsTagRegistered("XXXX").Should().BeFalse();
    }

    [Fact]
    public void CreateBlock_VrtfType_ReturnsVrtfBlock()
    {
        // Arrange - minimal valid VRTF data
        var data = CreateVrtfData();

        // Act
        var block = RcolBlockRegistry.CreateBlock(VrtfBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<VrtfBlock>();
        block.IsKnownType.Should().BeTrue();
        block.Tag.Should().Be("VRTF");
    }

    [Fact]
    public void CreateBlock_VbufType_ReturnsVbufBlock()
    {
        // Arrange - minimal valid VBUF data
        var data = CreateVbufData();

        // Act
        var block = RcolBlockRegistry.CreateBlock(VbufBlock.TypeId, data);

        // Assert
        block.Should().BeOfType<VbufBlock>();
        block.IsKnownType.Should().BeTrue();
        block.Tag.Should().Be("VBUF");
    }

    [Fact]
    public void CreateBlock_UnknownType_ReturnsUnknownRcolBlock()
    {
        // Arrange
        var data = new byte[] { (byte)'T', (byte)'E', (byte)'S', (byte)'T', 0, 0, 0, 0 };

        // Act
        var block = RcolBlockRegistry.CreateBlock(0xDEADBEEF, data);

        // Assert
        block.Should().BeOfType<UnknownRcolBlock>();
        block.IsKnownType.Should().BeFalse();
    }

    [Fact]
    public void CreateBlock_UnknownTypeWithKnownTag_FallsBackToTagLookup()
    {
        // Arrange - data with VRTF tag but unknown resource type
        var data = CreateVrtfData();

        // Act - use unknown resource type, but data has VRTF tag
        var block = RcolBlockRegistry.CreateBlock(0, data);

        // Assert - should fall back to tag lookup and return VrtfBlock
        block.Should().BeOfType<VrtfBlock>();
    }

    [Fact]
    public void RegisteredTypes_ContainsAllExpectedTypes()
    {
        var types = RcolBlockRegistry.RegisteredTypes.ToList();

        types.Should().Contain(VrtfBlock.TypeId);
        types.Should().Contain(VbufBlock.TypeId);
        types.Should().Contain(Vbuf2Block.TypeId);
        types.Should().Contain(IbufBlock.TypeId);
        types.Should().Contain(Ibuf2Block.TypeId);
        types.Should().Contain(SkinBlock.TypeId);
        types.Should().Contain(GeomBlock.TypeId);
        types.Should().Contain(ModlBlock.TypeId);
        types.Should().Contain(MlodBlock.TypeId);
        types.Should().Contain(VbsiBlock.TypeId);
    }

    [Fact]
    public void RegisteredTags_ContainsAllExpectedTags()
    {
        var tags = RcolBlockRegistry.RegisteredTags.ToList();

        tags.Should().Contain("VRTF");
        tags.Should().Contain("VBUF");
        tags.Should().Contain("IBUF");
        tags.Should().Contain("SKIN");
        tags.Should().Contain("GEOM");
        tags.Should().Contain("MODL");
        tags.Should().Contain("MLOD");
        tags.Should().Contain("VBSI");
    }

    /// <summary>
    /// Creates minimal valid VRTF block data.
    /// </summary>
    private static byte[] CreateVrtfData()
    {
        var data = new byte[20]; // Tag (4) + Version (4) + Stride (4) + Count (4) + Extended (4)
        data[0] = (byte)'V';
        data[1] = (byte)'R';
        data[2] = (byte)'T';
        data[3] = (byte)'F';
        // Version = 2
        data[4] = 0x02;
        data[5] = 0x00;
        data[6] = 0x00;
        data[7] = 0x00;
        // Stride = 0
        // Count = 0
        // Extended = 0
        return data;
    }

    /// <summary>
    /// Creates minimal valid VBUF block data.
    /// </summary>
    private static byte[] CreateVbufData()
    {
        var data = new byte[16]; // Tag (4) + Version (4) + Flags (4) + SwizzleIndex (4)
        data[0] = (byte)'V';
        data[1] = (byte)'B';
        data[2] = (byte)'U';
        data[3] = (byte)'F';
        // Version = 0x101
        data[4] = 0x01;
        data[5] = 0x01;
        data[6] = 0x00;
        data[7] = 0x00;
        // Flags = 0
        // SwizzleIndex = 0
        return data;
    }

    [Fact]
    public void CreateBlock_EmptyData_ReturnsUnknownBlock()
    {
        // Arrange
        var data = Array.Empty<byte>();

        // Act
        var block = RcolBlockRegistry.CreateBlock(0xDEADBEEF, data);

        // Assert
        block.Should().BeOfType<UnknownRcolBlock>();
    }

    [Fact]
    public void CreateBlock_ZeroTypeId_WithNoTag_ReturnsUnknownBlock()
    {
        // Arrange - data with unrecognized tag
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0, 0, 0, 0 };

        // Act
        var block = RcolBlockRegistry.CreateBlock(0, data);

        // Assert
        block.Should().BeOfType<UnknownRcolBlock>();
    }

    [Fact]
    public void IsTagRegistered_EmptyString_ReturnsFalse()
    {
        RcolBlockRegistry.IsTagRegistered(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_ZeroTypeId_ReturnsFalse()
    {
        RcolBlockRegistry.IsRegistered(0).Should().BeFalse();
    }

    [Fact]
    public void RegisteredTypes_IsNotEmpty()
    {
        RcolBlockRegistry.RegisteredTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void RegisteredTags_IsNotEmpty()
    {
        RcolBlockRegistry.RegisteredTags.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateBlock_DataShorterThanTag_ReturnsUnknownBlock()
    {
        // Arrange - data too short to have a valid tag
        var data = new byte[] { 0x01, 0x02 };

        // Act
        var block = RcolBlockRegistry.CreateBlock(0xDEADBEEF, data);

        // Assert
        block.Should().BeOfType<UnknownRcolBlock>();
    }

    [Fact]
    public void CreateBlock_NullBytesInTag_ReturnsUnknownBlock()
    {
        // Arrange - data with null bytes where tag should be
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Act
        var block = RcolBlockRegistry.CreateBlock(0, data);

        // Assert
        block.Should().BeOfType<UnknownRcolBlock>();
    }
}
