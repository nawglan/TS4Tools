using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CstlResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CSTLResource.cs
/// - CSTL is a catalog type for stair landings
/// - Type ID: 0x9F5CFF10
/// - Structure: Version + CatalogCommon + CSTLRefs (version-dependent array) +
///              UnkList1 + Unk01-04 + Unk05 + UnkIID01
/// </summary>
public class CstlResourceTests
{
    private static readonly ResourceKey TestKey = new(CstlResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cstl = new CstlResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cstl.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cstl.CommonBlock.Should().NotBeNull();
        cstl.CstlRefs.Should().NotBeNull();
        cstl.CstlRefs.Count.Should().Be(25); // Default version 0x0E
        cstl.UnkList1.Should().NotBeNull();
        cstl.UnkList1.Count.Should().Be(0);
        cstl.Unk01.Should().Be(0);
        cstl.Unk02.Should().Be(0);
        cstl.Unk03.Should().Be(0);
        cstl.Unk04.Should().Be(0);
        cstl.Unk05.Should().Be(0);
        cstl.UnkIID01.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new CstlResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 1500;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.Unk01 = 0x11111111;
        original.Unk02 = 0x22222222;
        original.Unk03 = 0x33333333;
        original.Unk04 = 0x44444444;
        original.Unk05 = 0x55;
        original.UnkIID01 = 0x1234567890ABCDEF;

        var data = original.Data.ToArray();
        var parsed = new CstlResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(1500);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.Unk01.Should().Be(0x11111111);
        parsed.Unk02.Should().Be(0x22222222);
        parsed.Unk03.Should().Be(0x33333333);
        parsed.Unk04.Should().Be(0x44444444);
        parsed.Unk05.Should().Be(0x55);
        parsed.UnkIID01.Should().Be(0x1234567890ABCDEF);
    }

    [Fact]
    public void RoundTrip_WithRefs_PreservesRefs()
    {
        var original = new CstlResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CstlRefs[0] = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.CstlRefs[10] = new TgiReference(0xAAAAAAAAAAAAAAAA, 0xAAAAAAAA, 0xAAAAAAAA);
        original.CstlRefs[24] = new TgiReference(0x9999999999999999, 0x99999999, 0x99999999);

        var data = original.Data.ToArray();
        var parsed = new CstlResource(TestKey, data);

        parsed.CstlRefs.Count.Should().Be(25);
        parsed.CstlRefs[0].Instance.Should().Be(0x1111111111111111);
        parsed.CstlRefs[10].Instance.Should().Be(0xAAAAAAAAAAAAAAAA);
        parsed.CstlRefs[24].Instance.Should().Be(0x9999999999999999);
    }

    [Fact]
    public void RoundTrip_WithUnkList_PreservesList()
    {
        var original = new CstlResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.UnkList1.Add(0x12345678);
        original.UnkList1.Add(0xABCDEF01);
        original.UnkList1.Add(0xFEDCBA98);

        var data = original.Data.ToArray();
        var parsed = new CstlResource(TestKey, data);

        parsed.UnkList1.Count.Should().Be(3);
        parsed.UnkList1[0].Should().Be(0x12345678);
        parsed.UnkList1[1].Should().Be(0xABCDEF01);
        parsed.UnkList1[2].Should().Be(0xFEDCBA98);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CstlResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 3000;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 12;

        // Type-specific fields
        original.CstlRefs[0] = new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222);
        original.UnkList1.Add(0x87654321);
        original.Unk01 = 0xAAAAAAAA;
        original.Unk02 = 0xBBBBBBBB;
        original.Unk03 = 0xCCCCCCCC;
        original.Unk04 = 0xDDDDDDDD;
        original.Unk05 = 0xEE;
        original.UnkIID01 = 0x0123456789ABCDEF;

        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CstlResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CstlResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CstlResource>();
        ((CstlResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CstlResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CstlResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.CstlRefs.Count.Should().Be(25);
    }
}
