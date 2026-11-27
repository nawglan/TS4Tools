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

    [Fact]
    public void VbsiType_UsesLegacyResourceType()
    {
        VbsiBlock.TypeId.Should().Be(0xFFFFFFFFu);
        RcolBlockRegistry.IsRegistered(0xFFFFFFFF).Should().BeTrue();
    }

    [Fact]
    public void IsRegistered_TrackMask_ReturnsTrue()
    {
        RcolBlockRegistry.IsRegistered(0x033260E3).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered("TkMk").Should().BeTrue();
    }

    public static TheoryData<uint, string> JazzChunkTypes => new()
    {
        { 0x02D5DF13, "S_SM" },
        { 0x02EEDAFE, "S_St" },
        { 0x02EEDB18, "S_DG" },
        { 0x02EEDB2F, "S_AD" },
        { 0x02EEDB46, "S_PD" },
        { 0x02EEDB5F, "Play" },
        { 0x02EEDB70, "Rand" },
        { 0x02EEDB92, "SoPn" },
        { 0x02EEDBA5, "DG00" },
        { 0x02EEEBDC, "SNSN" },
        { 0x02EEEBDD, "Prop" },
        { 0x02EEEBDE, "AcOp" },
        { 0x0344D438, "Stop" },
    };

    [Theory]
    [MemberData(nameof(JazzChunkTypes))]
    public void JazzChunks_AreRegisteredByTypeAndTag(uint type, string tag)
    {
        RcolBlockRegistry.IsRegistered(type).Should().BeTrue();
        RcolBlockRegistry.IsTagRegistered(tag).Should().BeTrue();
        RcolBlockRegistry.GetTypeForTag(tag).Should().Be(type);
    }

    [Fact]
    public void JazzChunks_AllThirteenCreateTheirOwnBlockType()
    {
        var blocks = new Dictionary<uint, Func<RcolBlock>>
        {
            [0x02D5DF13] = () => new TS4Tools.Wrappers.JazzChunks.JazzStateMachineBlock(),
            [0x02EEDAFE] = () => new TS4Tools.Wrappers.JazzChunks.JazzStateBlock(),
            [0x02EEDB18] = () => new TS4Tools.Wrappers.JazzChunks.JazzDecisionGraphBlock(),
            [0x02EEDB2F] = () => new TS4Tools.Wrappers.JazzChunks.JazzActorDefinitionBlock(),
            [0x02EEDB46] = () => new TS4Tools.Wrappers.JazzChunks.JazzParameterDefinitionBlock(),
            [0x02EEDB5F] = () => new TS4Tools.Wrappers.JazzChunks.JazzPlayAnimationNodeBlock(),
            [0x02EEDB70] = () => new TS4Tools.Wrappers.JazzChunks.JazzRandomNodeBlock(),
            [0x02EEDB92] = () => new TS4Tools.Wrappers.JazzChunks.JazzSelectOnParameterNodeBlock(),
            [0x02EEDBA5] = () => new TS4Tools.Wrappers.JazzChunks.JazzSelectOnDestinationNodeBlock(),
            [0x02EEEBDC] = () => new TS4Tools.Wrappers.JazzChunks.JazzNextStateNodeBlock(),
            [0x02EEEBDD] = () => new TS4Tools.Wrappers.JazzChunks.JazzCreatePropNodeBlock(),
            [0x02EEEBDE] = () => new TS4Tools.Wrappers.JazzChunks.JazzActorOperationNodeBlock(),
            [0x0344D438] = () => new TS4Tools.Wrappers.JazzChunks.JazzStopAnimationNodeBlock(),
        };

        foreach (var (type, make) in blocks)
        {
            var original = make();
            var bytes = original.Serialize();

            var created = RcolBlockRegistry.CreateBlock(type, bytes.Span);

            created.GetType().Should().Be(original.GetType(), $"type 0x{type:X8}");
            created.ResourceType.Should().Be(type);
            created.Serialize().ToArray().Should().Equal(bytes.ToArray(), $"type 0x{type:X8} round trips");
        }
    }

    [Fact]
    public void GetTypeForTag_CoversBondVbsiAndJazz()
    {
        RcolBlockRegistry.GetTypeForTag("BOND").Should().Be(RcolConstants.SlotAdjusts);
        RcolBlockRegistry.GetTypeForTag("VBSI").Should().Be(0xFFFFFFFFu);
        RcolBlockRegistry.GetTypeForTag("S_SM").Should().Be(0x02D5DF13u);
        RcolBlockRegistry.GetTypeForTag("VBUF").Should().Be(VbufBlock.TypeId, "first registration wins");
        RcolBlockRegistry.GetTypeForTag("IBUF").Should().Be(IbufBlock.TypeId);
        RcolBlockRegistry.GetTypeForTag("ZZZZ").Should().BeNull();
    }

    [Fact]
    public void GeomType_IsRegisteredAsBlock()
    {
        RcolBlockRegistry.IsRegistered(0x015A1849).Should().BeTrue();
        RcolBlockRegistry.CreateBlock(0x015A1849, new GeomBlock().Serialize().Span).Should().BeOfType<GeomBlock>();
    }

    [Fact]
    public void CreateBlock_EmptyData_FallsBackToUnknownBlock()
    {
        var block = RcolBlockRegistry.CreateBlock(0xDEADBEEF, ReadOnlySpan<byte>.Empty);

        block.Should().BeOfType<UnknownRcolBlock>();
        block.Serialize().Length.Should().Be(0);
        block.Tag.Should().Be("????");
    }

    [Fact]
    public void CreateBlock_ShortDataUnknownType_KeepsBytesAndDoesNotThrow()
    {
        byte[] data = [1, 2, 3];

        var block = RcolBlockRegistry.CreateBlock(0xDEADBEEF, data);

        block.Should().BeOfType<UnknownRcolBlock>();
        block.Serialize().ToArray().Should().Equal(data);
    }

    [Fact]
    public void CreateBlock_KnownTypeWithGarbageData_Throws()
    {
        byte[] garbage = [0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0];

        foreach (uint type in new[] { MlodBlock.TypeId, ModlBlock.TypeId, FtptBlock.TypeId, RcolConstants.TkMk })
        {
            var action = () => RcolBlockRegistry.CreateBlock(type, garbage);
            action.Should().Throw<InvalidDataException>($"type 0x{type:X8} validates its tag");
        }
    }

    [Fact]
    public void CreateBlock_UnknownTypeUnknownTag_PreservesDataByteIdentical()
    {
        byte[] data = [(byte)'Q', (byte)'Q', (byte)'Q', (byte)'Q', 9, 8, 7];

        var block = RcolBlockRegistry.CreateBlock(0x12345678, data);

        block.Tag.Should().Be("QQQQ");
        block.ResourceType.Should().Be(0x12345678u);
        block.Serialize().ToArray().Should().Equal(data);
    }

    [Fact]
    public void Register_CustomType_IsResolvedAndFirstTagWins()
    {
        const uint customType = 0x7E57AB1E;
        RcolBlockRegistry.Register(customType, "VBUF", data => new UnknownRcolBlock(customType, "CUST", data));

        // The registry has no unregister; the type id above is private to this test.
        RcolBlockRegistry.IsRegistered(customType).Should().BeTrue();
        RcolBlockRegistry.CreateBlock(customType, [1, 2, 3, 4]).Tag.Should().Be("CUST");
        RcolBlockRegistry.GetTypeForTag("VBUF").Should().Be(VbufBlock.TypeId, "an existing tag keeps its first registration");
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
