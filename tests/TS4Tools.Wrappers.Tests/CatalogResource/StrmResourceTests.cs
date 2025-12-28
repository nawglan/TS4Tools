using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="StrmResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/STRMResource.cs
/// - STRM is a catalog type for styled room presets
/// - Type ID: 0x74050B1F
/// - Structure: Version + CatalogCommon + Unk01-05 + SwatchGrouping + Colors + UnkRef1 + Unk06 + Unk07
/// </summary>
public class StrmResourceTests
{
    private static readonly ResourceKey TestKey = new(StrmResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var strm = new StrmResource(TestKey, ReadOnlyMemory<byte>.Empty);

        strm.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        strm.CommonBlock.Should().NotBeNull();
        strm.Unk01.Should().Be(0);
        strm.Unk02.Should().Be(0);
        strm.Unk03.Should().Be(0);
        strm.Unk04.Should().Be(0);
        strm.Unk05.Should().Be(0);
        strm.SwatchGrouping.Should().Be(0);
        strm.Colors.Count.Should().Be(0);
        strm.UnkRef1.Should().Be(TgiReference.Empty);
        strm.Unk06.Should().Be(0);
        strm.Unk07.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new StrmResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 750;
        original.CommonBlock.NameHash = 0xCAFEBABE;
        original.Unk01 = 0x11111111;
        original.Unk02 = 0x22222222;
        original.Unk03 = 0x33333333;
        original.Unk04 = 0x44444444;
        original.Unk05 = 0x55555555;
        original.SwatchGrouping = 0x1234567890ABCDEF;
        original.Unk06 = 0x66666666;
        original.Unk07 = 0x77777777;

        var data = original.Data.ToArray();
        var parsed = new StrmResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(750);
        parsed.CommonBlock.NameHash.Should().Be(0xCAFEBABE);
        parsed.Unk01.Should().Be(0x11111111);
        parsed.Unk02.Should().Be(0x22222222);
        parsed.Unk03.Should().Be(0x33333333);
        parsed.Unk04.Should().Be(0x44444444);
        parsed.Unk05.Should().Be(0x55555555);
        parsed.SwatchGrouping.Should().Be(0x1234567890ABCDEF);
        parsed.Unk06.Should().Be(0x66666666);
        parsed.Unk07.Should().Be(0x77777777);
    }

    [Fact]
    public void RoundTrip_WithTgiReference_PreservesReference()
    {
        var original = new StrmResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.UnkRef1 = new TgiReference(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB);

        var data = original.Data.ToArray();
        var parsed = new StrmResource(TestKey, data);

        parsed.UnkRef1.Instance.Should().Be(0x1111111122222222);
        parsed.UnkRef1.Type.Should().Be(0xAAAAAAAA);
        parsed.UnkRef1.Group.Should().Be(0xBBBBBBBB);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new StrmResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFAA5500);
        original.Colors.Add(0xFF00AA55);
        original.Colors.Add(0xFF5500AA);

        var data = original.Data.ToArray();
        var parsed = new StrmResource(TestKey, data);

        parsed.Colors.Count.Should().Be(3);
        parsed.Colors[0].Should().Be(0xFFAA5500);
        parsed.Colors[1].Should().Be(0xFF00AA55);
        parsed.Colors[2].Should().Be(0xFF5500AA);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new StrmResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 2000;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 20;

        // Type-specific fields
        original.Unk01 = 0xABCDEF01;
        original.Unk02 = 0x12345678;
        original.Unk03 = 0x87654321;
        original.Unk04 = 0xFEDCBA98;
        original.Unk05 = 0x11223344;
        original.SwatchGrouping = 0x0123456789ABCDEF;
        original.UnkRef1 = new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222);
        original.Unk06 = 0x55667788;
        original.Unk07 = 0x99AABBCC;

        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new StrmResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new StrmResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<StrmResource>();
        ((StrmResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new StrmResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as StrmResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.UnkRef1.Should().Be(TgiReference.Empty);
    }
}
