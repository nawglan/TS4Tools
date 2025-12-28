using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CstrResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CSTRResource.cs
/// - CSTR is a catalog type for staircases
/// - Type ID: 0x9A20CD1C
/// - Structure: Version + CatalogCommon + HashIndicator + 3 hashes + CSTR_references (6 TGIs) +
///              Unk01-03 + MaterialVariant + SwatchGrouping + Colors + Unk05
/// </summary>
public class CstrResourceTests
{
    private static readonly ResourceKey TestKey = new(CstrResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cstr = new CstrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cstr.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cstr.CommonBlock.Should().NotBeNull();
        cstr.HashIndicator.Should().Be(0x1);
        cstr.Hash01.Should().Be(0x811C9DC5);
        cstr.Hash02.Should().Be(0x811C9DC5);
        cstr.Hash03.Should().Be(0x811C9DC5);
        cstr.ReferenceList.Should().NotBeNull();
        cstr.ReferenceList.Ref01.Should().Be(TgiReference.Empty);
        cstr.Unk01.Should().Be(0);
        cstr.Unk02.Should().Be(0);
        cstr.Unk03.Should().Be(0);
        cstr.MaterialVariant.Should().Be(0);
        cstr.SwatchGrouping.Should().Be(0);
        cstr.Colors.Count.Should().Be(0);
        cstr.Unk05.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new CstrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 1000;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.HashIndicator = 0x2;
        original.Hash01 = 0x11111111;
        original.Hash02 = 0x22222222;
        original.Hash03 = 0x33333333;
        original.Unk01 = 0x11;
        original.Unk02 = 0x22;
        original.Unk03 = 0x33;
        original.MaterialVariant = 0xABCDEF01;
        original.SwatchGrouping = 0x1234567890ABCDEF;
        original.Unk05 = 0x55;

        var data = original.Data.ToArray();
        var parsed = new CstrResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(1000);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.HashIndicator.Should().Be(0x2);
        parsed.Hash01.Should().Be(0x11111111);
        parsed.Hash02.Should().Be(0x22222222);
        parsed.Hash03.Should().Be(0x33333333);
        parsed.Unk01.Should().Be(0x11);
        parsed.Unk02.Should().Be(0x22);
        parsed.Unk03.Should().Be(0x33);
        parsed.MaterialVariant.Should().Be(0xABCDEF01);
        parsed.SwatchGrouping.Should().Be(0x1234567890ABCDEF);
        parsed.Unk05.Should().Be(0x55);
    }

    [Fact]
    public void RoundTrip_WithReferences_PreservesReferences()
    {
        var original = new CstrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.ReferenceList.Ref01 = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.ReferenceList.Ref02 = new TgiReference(0x2222222222222222, 0x22222222, 0x22222222);
        original.ReferenceList.Ref03 = new TgiReference(0x3333333333333333, 0x33333333, 0x33333333);
        original.ReferenceList.Ref04 = new TgiReference(0x4444444444444444, 0x44444444, 0x44444444);
        original.ReferenceList.Ref05 = new TgiReference(0x5555555555555555, 0x55555555, 0x55555555);
        original.ReferenceList.Ref06 = new TgiReference(0x6666666666666666, 0x66666666, 0x66666666);

        var data = original.Data.ToArray();
        var parsed = new CstrResource(TestKey, data);

        parsed.ReferenceList.Ref01.Instance.Should().Be(0x1111111111111111);
        parsed.ReferenceList.Ref02.Instance.Should().Be(0x2222222222222222);
        parsed.ReferenceList.Ref03.Instance.Should().Be(0x3333333333333333);
        parsed.ReferenceList.Ref04.Instance.Should().Be(0x4444444444444444);
        parsed.ReferenceList.Ref05.Instance.Should().Be(0x5555555555555555);
        parsed.ReferenceList.Ref06.Instance.Should().Be(0x6666666666666666);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CstrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFAA5500);
        original.Colors.Add(0xFF00AA55);

        var data = original.Data.ToArray();
        var parsed = new CstrResource(TestKey, data);

        parsed.Colors.Count.Should().Be(2);
        parsed.Colors[0].Should().Be(0xFFAA5500);
        parsed.Colors[1].Should().Be(0xFF00AA55);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CstrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 2500;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 10;

        // Type-specific fields
        original.HashIndicator = 0x5;
        original.Hash01 = 0xAAAAAAAA;
        original.Hash02 = 0xBBBBBBBB;
        original.Hash03 = 0xCCCCCCCC;
        original.ReferenceList.Ref01 = new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222);
        original.Unk01 = 0xAA;
        original.Unk02 = 0xBB;
        original.Unk03 = 0xCC;
        original.MaterialVariant = 0xFEDCBA98;
        original.SwatchGrouping = 0x0123456789ABCDEF;
        original.Unk05 = 0xDD;

        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CstrResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CstrResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CstrResource>();
        ((CstrResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CstrResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CstrResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.ReferenceList.Ref01.Should().Be(TgiReference.Empty);
    }
}
