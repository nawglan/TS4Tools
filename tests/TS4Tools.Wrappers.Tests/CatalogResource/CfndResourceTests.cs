using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CfndResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFNDResource.cs
/// - CFND is a catalog type for building foundations
/// - Type ID: 0x2FAE983E
/// - Structure: Version + CatalogCommon + Unk01 + Unk02 + ModlRef1 + MaterialVariant + SwatchGrouping + Float1 + Float2 + TrimRef + ModlRef2 + Colors
/// </summary>
public class CfndResourceTests
{
    private static readonly ResourceKey TestKey = new(CfndResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cfnd = new CfndResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cfnd.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cfnd.CommonBlock.Should().NotBeNull();
        cfnd.Unk01.Should().Be(0);
        cfnd.Unk02.Should().Be(0);
        cfnd.ModlRef1.Should().Be(TgiReference.Empty);
        cfnd.MaterialVariant.Should().Be(0);
        cfnd.SwatchGrouping.Should().Be(0);
        cfnd.Float1.Should().Be(0);
        cfnd.Float2.Should().Be(0);
        cfnd.TrimRef.Should().Be(TgiReference.Empty);
        cfnd.ModlRef2.Should().Be(TgiReference.Empty);
        cfnd.Colors.Count.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new CfndResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 500;
        original.CommonBlock.NameHash = 0xBEEFCAFE;
        original.Unk01 = 0xAA;
        original.Unk02 = 0xBB;
        original.MaterialVariant = 0x12345678;
        original.SwatchGrouping = 0xFEDCBA0987654321;
        original.Float1 = 1.25f;
        original.Float2 = 3.5f;

        var data = original.Data.ToArray();
        var parsed = new CfndResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(500);
        parsed.CommonBlock.NameHash.Should().Be(0xBEEFCAFE);
        parsed.Unk01.Should().Be(0xAA);
        parsed.Unk02.Should().Be(0xBB);
        parsed.MaterialVariant.Should().Be(0x12345678);
        parsed.SwatchGrouping.Should().Be(0xFEDCBA0987654321);
        parsed.Float1.Should().Be(1.25f);
        parsed.Float2.Should().Be(3.5f);
    }

    [Fact]
    public void RoundTrip_WithTgiReferences_PreservesReferences()
    {
        var original = new CfndResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.ModlRef1 = new TgiReference(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB);
        original.TrimRef = new TgiReference(0x3333333344444444, 0xCCCCCCCC, 0xDDDDDDDD);
        original.ModlRef2 = new TgiReference(0x5555555566666666, 0xEEEEEEEE, 0xFFFFFFFF);

        var data = original.Data.ToArray();
        var parsed = new CfndResource(TestKey, data);

        parsed.ModlRef1.Instance.Should().Be(0x1111111122222222);
        parsed.ModlRef1.Type.Should().Be(0xAAAAAAAA);
        parsed.ModlRef1.Group.Should().Be(0xBBBBBBBB);

        parsed.TrimRef.Instance.Should().Be(0x3333333344444444);
        parsed.TrimRef.Type.Should().Be(0xCCCCCCCC);
        parsed.TrimRef.Group.Should().Be(0xDDDDDDDD);

        parsed.ModlRef2.Instance.Should().Be(0x5555555566666666);
        parsed.ModlRef2.Type.Should().Be(0xEEEEEEEE);
        parsed.ModlRef2.Group.Should().Be(0xFFFFFFFF);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CfndResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFAA5500);
        original.Colors.Add(0xFF00AA55);
        original.Colors.Add(0xFF5500AA);
        original.Colors.Add(0xFFAA00FF);

        var data = original.Data.ToArray();
        var parsed = new CfndResource(TestKey, data);

        parsed.Colors.Count.Should().Be(4);
        parsed.Colors[0].Should().Be(0xFFAA5500);
        parsed.Colors[1].Should().Be(0xFF00AA55);
        parsed.Colors[2].Should().Be(0xFF5500AA);
        parsed.Colors[3].Should().Be(0xFFAA00FF);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CfndResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 1000;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 12;

        // Type-specific fields
        original.Unk01 = 0x12;
        original.Unk02 = 0x34;
        original.ModlRef1 = new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222);
        original.MaterialVariant = 0xFEDCBA98;
        original.SwatchGrouping = 0x0123456789ABCDEF;
        original.Float1 = 3.14159f;
        original.Float2 = 2.71828f;
        original.TrimRef = new TgiReference(0x9876543210FEDCBA, 0x33333333, 0x44444444);
        original.ModlRef2 = new TgiReference(0xFEDCBA9876543210, 0x55555555, 0x66666666);

        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CfndResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CfndResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CfndResource>();
        ((CfndResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CfndResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CfndResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.ModlRef1.Should().Be(TgiReference.Empty);
    }
}
