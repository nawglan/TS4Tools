using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CwalResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CWALResource.cs
/// - Type ID: 0xD5F0F921
/// - Wall pattern catalog type with MATD entries and image groups
/// </summary>
public class CwalResourceTests
{
    private static readonly ResourceKey TestKey = new(CwalResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        CwalResource.TypeId.Should().Be(0xD5F0F921);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new CwalResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.CommonBlock.Should().NotBeNull();
        resource.MaterialList.Count.Should().Be(0);
        resource.CornerTextures.Count.Should().Be(0);
        resource.CorneringFactor.Should().Be(0);
        resource.Colors.Count.Should().Be(0);
        resource.SwatchGrouping.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_BasicFields_PreservesData()
    {
        var original = new CwalResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CorneringFactor = 0x12345678;
        original.SwatchGrouping = 0xCAFEBABE12345678;

        var data = original.Data.ToArray();
        var parsed = new CwalResource(TestKey, data);

        parsed.CorneringFactor.Should().Be(0x12345678);
        parsed.SwatchGrouping.Should().Be(0xCAFEBABE12345678);
    }

    [Fact]
    public void RoundTrip_MaterialList_PreservesData()
    {
        var original = new CwalResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.MaterialList.Add(MainWallHeight.ShortWall, new TgiReference(0x1111111111111111, 0x11111111, 0x11111111));
        original.MaterialList.Add(MainWallHeight.MediumWall, new TgiReference(0x2222222222222222, 0x22222222, 0x22222222));
        original.MaterialList.Add(MainWallHeight.TallWall, new TgiReference(0x3333333333333333, 0x33333333, 0x33333333));

        var data = original.Data.ToArray();
        var parsed = new CwalResource(TestKey, data);

        parsed.MaterialList.Count.Should().Be(3);
        parsed.MaterialList[0].Height.Should().Be(MainWallHeight.ShortWall);
        parsed.MaterialList[0].MatdRef.Instance.Should().Be(0x1111111111111111);
        parsed.MaterialList[1].Height.Should().Be(MainWallHeight.MediumWall);
        parsed.MaterialList[2].Height.Should().Be(MainWallHeight.TallWall);
    }

    [Fact]
    public void RoundTrip_CornerTextures_PreservesData()
    {
        var original = new CwalResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CornerTextures.Add(
            CornerWallHeight.ShortWall,
            new TgiReference(0xAAAAAAAAAAAAAAAA, 0xAAAAAAAA, 0xAAAAAAAA),
            new TgiReference(0xBBBBBBBBBBBBBBBB, 0xBBBBBBBB, 0xBBBBBBBB),
            new TgiReference(0xCCCCCCCCCCCCCCCC, 0xCCCCCCCC, 0xCCCCCCCC));

        var data = original.Data.ToArray();
        var parsed = new CwalResource(TestKey, data);

        parsed.CornerTextures.Count.Should().Be(1);
        parsed.CornerTextures[0].Height.Should().Be(CornerWallHeight.ShortWall);
        parsed.CornerTextures[0].DiffuseMap.Instance.Should().Be(0xAAAAAAAAAAAAAAAA);
        parsed.CornerTextures[0].BumpMap.Instance.Should().Be(0xBBBBBBBBBBBBBBBB);
        parsed.CornerTextures[0].SpecularMap.Instance.Should().Be(0xCCCCCCCCCCCCCCCC);
    }

    [Fact]
    public void RoundTrip_Colors_PreservesData()
    {
        var original = new CwalResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFF0000FF);
        original.Colors.Add(0xFF00FF00);
        original.Colors.Add(0xFFFF0000);

        var data = original.Data.ToArray();
        var parsed = new CwalResource(TestKey, data);

        parsed.Colors.Count.Should().Be(3);
        parsed.Colors[0].Should().Be(0xFF0000FF);
        parsed.Colors[1].Should().Be(0xFF00FF00);
        parsed.Colors[2].Should().Be(0xFFFF0000);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CwalResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block
        original.CommonBlock.Price = 999;
        original.CommonBlock.NameHash = 0xDEADBEEF;

        // Type-specific fields
        original.MaterialList.Add(MainWallHeight.TallWall, new TgiReference(0x1234567890ABCDEF, 0x12345678, 0x00000001));
        original.CornerTextures.Add(
            CornerWallHeight.MediumWall,
            new TgiReference(0xFEDCBA9876543210, 0x87654321, 0x00000002),
            new TgiReference(0xABCDEF0123456789, 0x11111111, 0x00000003),
            new TgiReference(0x9876543210FEDCBA, 0x22222222, 0x00000004));
        original.CorneringFactor = 0xABCDEF01;
        original.Colors.Add(0xFFAABBCC);
        original.Colors.Add(0xFF112233);
        original.SwatchGrouping = 0x0123456789ABCDEF;

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new CwalResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void MainWallHeight_Values_AreCorrect()
    {
        ((byte)MainWallHeight.ShortWall).Should().Be(0x03);
        ((byte)MainWallHeight.MediumWall).Should().Be(0x04);
        ((byte)MainWallHeight.TallWall).Should().Be(0x05);
    }

    [Fact]
    public void CornerWallHeight_Values_AreCorrect()
    {
        ((byte)CornerWallHeight.ShortWall).Should().Be(0xC3);
        ((byte)CornerWallHeight.MediumWall).Should().Be(0xC4);
        ((byte)CornerWallHeight.TallWall).Should().Be(0xC5);
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CwalResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CwalResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CwalResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CwalResource;

        resource.Should().NotBeNull();
        resource!.MaterialList.Count.Should().Be(0);
        resource.CornerTextures.Count.Should().Be(0);
    }
}
