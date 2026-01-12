using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CralResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CRALResource.cs
/// - CRAL is a catalog type for stair railings
/// - Type ID: 0x1C1CF1F7
/// - Structure: Version + CatalogCommon + Gp8References (8 TGIs) + MaterialVariant + SwatchGrouping + Unk02 + Colors
/// </summary>
public class CralResourceTests
{
    private static readonly ResourceKey TestKey = new(CralResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cral = new CralResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cral.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cral.CommonBlock.Should().NotBeNull();
        cral.ReferenceList.Should().NotBeNull();
        cral.ReferenceList.Ref01.Should().Be(TgiReference.Empty);
        cral.ReferenceList.Ref08.Should().Be(TgiReference.Empty);
        cral.MaterialVariant.Should().Be(0);
        cral.SwatchGrouping.Should().Be(0);
        cral.Unk02.Should().Be(0);
        cral.Colors.Count.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new CralResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 300;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.MaterialVariant = 0xABCDEF01;
        original.SwatchGrouping = 0x1234567890ABCDEF;
        original.Unk02 = 0x42424242;

        var data = original.Data.ToArray();
        var parsed = new CralResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(300);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.MaterialVariant.Should().Be(0xABCDEF01);
        parsed.SwatchGrouping.Should().Be(0x1234567890ABCDEF);
        parsed.Unk02.Should().Be(0x42424242);
    }

    [Fact]
    public void RoundTrip_WithGp8References_PreservesReferences()
    {
        var original = new CralResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.ReferenceList.Ref01 = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.ReferenceList.Ref02 = new TgiReference(0x2222222222222222, 0x22222222, 0x22222222);
        original.ReferenceList.Ref03 = new TgiReference(0x3333333333333333, 0x33333333, 0x33333333);
        original.ReferenceList.Ref04 = new TgiReference(0x4444444444444444, 0x44444444, 0x44444444);
        original.ReferenceList.Ref05 = new TgiReference(0x5555555555555555, 0x55555555, 0x55555555);
        original.ReferenceList.Ref06 = new TgiReference(0x6666666666666666, 0x66666666, 0x66666666);
        original.ReferenceList.Ref07 = new TgiReference(0x7777777777777777, 0x77777777, 0x77777777);
        original.ReferenceList.Ref08 = new TgiReference(0x8888888888888888, 0x88888888, 0x88888888);

        var data = original.Data.ToArray();
        var parsed = new CralResource(TestKey, data);

        parsed.ReferenceList.Ref01.Instance.Should().Be(0x1111111111111111);
        parsed.ReferenceList.Ref02.Instance.Should().Be(0x2222222222222222);
        parsed.ReferenceList.Ref03.Instance.Should().Be(0x3333333333333333);
        parsed.ReferenceList.Ref04.Instance.Should().Be(0x4444444444444444);
        parsed.ReferenceList.Ref05.Instance.Should().Be(0x5555555555555555);
        parsed.ReferenceList.Ref06.Instance.Should().Be(0x6666666666666666);
        parsed.ReferenceList.Ref07.Instance.Should().Be(0x7777777777777777);
        parsed.ReferenceList.Ref08.Instance.Should().Be(0x8888888888888888);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CralResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFAA5500);
        original.Colors.Add(0xFF00AA55);

        var data = original.Data.ToArray();
        var parsed = new CralResource(TestKey, data);

        parsed.Colors.Count.Should().Be(2);
        parsed.Colors[0].Should().Be(0xFFAA5500);
        parsed.Colors[1].Should().Be(0xFF00AA55);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CralResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 450;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 15;

        // Type-specific fields
        original.ReferenceList.Ref01 = new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222);
        original.ReferenceList.Ref05 = new TgiReference(0x9876543210FEDCBA, 0x33333333, 0x44444444);
        original.MaterialVariant = 0xFEDCBA98;
        original.SwatchGrouping = 0x0123456789ABCDEF;
        original.Unk02 = 0xDEADBEEF;

        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CralResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CralResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CralResource>();
        ((CralResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CralResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CralResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.ReferenceList.Ref01.Should().Be(TgiReference.Empty);
    }
}
