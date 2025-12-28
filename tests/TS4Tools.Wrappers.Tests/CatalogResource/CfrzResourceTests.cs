using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CfrzResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFRZResource.cs
/// - CFRZ is a catalog type for frieze/crown molding
/// - Type ID: 0xA057811C
/// - Structure: Version + CatalogCommon + TrimRef + ModlRef1 + MaterialVariant + SwatchGrouping + Float01 + Float02 + ModlRef2 + Colors
/// </summary>
public class CfrzResourceTests
{
    private static readonly ResourceKey TestKey = new(CfrzResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cfrz = new CfrzResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cfrz.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cfrz.CommonBlock.Should().NotBeNull();
        cfrz.TrimRef.Should().Be(TgiReference.Empty);
        cfrz.ModlRef1.Should().Be(TgiReference.Empty);
        cfrz.MaterialVariant.Should().Be(0);
        cfrz.SwatchGrouping.Should().Be(0);
        cfrz.Float01.Should().Be(0);
        cfrz.Float02.Should().Be(0);
        cfrz.ModlRef2.Should().Be(TgiReference.Empty);
        cfrz.Colors.Count.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new CfrzResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 200;
        original.CommonBlock.NameHash = 0xCAFEBABE;
        original.MaterialVariant = 0xABCDEF01;
        original.SwatchGrouping = 0x1234567890ABCDEF;
        original.Float01 = 1.5f;
        original.Float02 = 2.75f;

        var data = original.Data.ToArray();
        var parsed = new CfrzResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(200);
        parsed.CommonBlock.NameHash.Should().Be(0xCAFEBABE);
        parsed.MaterialVariant.Should().Be(0xABCDEF01);
        parsed.SwatchGrouping.Should().Be(0x1234567890ABCDEF);
        parsed.Float01.Should().Be(1.5f);
        parsed.Float02.Should().Be(2.75f);
    }

    [Fact]
    public void RoundTrip_WithTgiReferences_PreservesReferences()
    {
        var original = new CfrzResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.TrimRef = new TgiReference(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB);
        original.ModlRef1 = new TgiReference(0x3333333344444444, 0xCCCCCCCC, 0xDDDDDDDD);
        original.ModlRef2 = new TgiReference(0x5555555566666666, 0xEEEEEEEE, 0xFFFFFFFF);

        var data = original.Data.ToArray();
        var parsed = new CfrzResource(TestKey, data);

        parsed.TrimRef.Instance.Should().Be(0x1111111122222222);
        parsed.TrimRef.Type.Should().Be(0xAAAAAAAA);
        parsed.TrimRef.Group.Should().Be(0xBBBBBBBB);

        parsed.ModlRef1.Instance.Should().Be(0x3333333344444444);
        parsed.ModlRef1.Type.Should().Be(0xCCCCCCCC);
        parsed.ModlRef1.Group.Should().Be(0xDDDDDDDD);

        parsed.ModlRef2.Instance.Should().Be(0x5555555566666666);
        parsed.ModlRef2.Type.Should().Be(0xEEEEEEEE);
        parsed.ModlRef2.Group.Should().Be(0xFFFFFFFF);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CfrzResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFAA5500);
        original.Colors.Add(0xFF00AA55);

        var data = original.Data.ToArray();
        var parsed = new CfrzResource(TestKey, data);

        parsed.Colors.Count.Should().Be(2);
        parsed.Colors[0].Should().Be(0xFFAA5500);
        parsed.Colors[1].Should().Be(0xFF00AA55);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CfrzResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 350;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 8;

        // Type-specific fields
        original.TrimRef = new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222);
        original.ModlRef1 = new TgiReference(0x9876543210FEDCBA, 0x33333333, 0x44444444);
        original.MaterialVariant = 0xFEDCBA98;
        original.SwatchGrouping = 0x0123456789ABCDEF;
        original.Float01 = 3.14159f;
        original.Float02 = 2.71828f;
        original.ModlRef2 = new TgiReference(0xFEDCBA9876543210, 0x55555555, 0x66666666);

        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CfrzResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CfrzResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CfrzResource>();
        ((CfrzResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CfrzResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CfrzResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.TrimRef.Should().Be(TgiReference.Empty);
    }
}
