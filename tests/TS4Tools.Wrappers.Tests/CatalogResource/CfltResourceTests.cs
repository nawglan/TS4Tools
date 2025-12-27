using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CfltResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFLTResource.cs
/// - CFLT is a simple catalog type for floor trim
/// - Type ID: 0x84C23219
/// - Structure: Version + CatalogCommon + TrimRef + MaterialVariant + ModlRef + SwatchGrouping + Colors + Unk02
/// </summary>
public class CfltResourceTests
{
    private static readonly ResourceKey TestKey = new(CfltResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cflt = new CfltResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cflt.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cflt.CommonBlock.Should().NotBeNull();
        cflt.CommonBlock.Price.Should().Be(0);
        cflt.TrimRef.Should().Be(TgiReference.Empty);
        cflt.ModlRef.Should().Be(TgiReference.Empty);
        cflt.MaterialVariant.Should().Be(0);
        cflt.SwatchGrouping.Should().Be(0);
        cflt.Colors.Count.Should().Be(0);
        cflt.Unk02.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        // Create resource
        var original = new CfltResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 75;
        original.CommonBlock.NameHash = 0xCAFEBABE;
        original.MaterialVariant = 0xABCDEF01;
        original.SwatchGrouping = 0x1234567890ABCDEF;
        original.Unk02 = 0x12345678;

        // Serialize
        var data = original.Data.ToArray();

        // Parse
        var parsed = new CfltResource(TestKey, data);

        // Verify
        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(75);
        parsed.CommonBlock.NameHash.Should().Be(0xCAFEBABE);
        parsed.MaterialVariant.Should().Be(0xABCDEF01);
        parsed.SwatchGrouping.Should().Be(0x1234567890ABCDEF);
        parsed.Unk02.Should().Be(0x12345678);
    }

    [Fact]
    public void RoundTrip_WithTgiReferences_PreservesReferences()
    {
        var original = new CfltResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.TrimRef = new TgiReference(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB);
        original.ModlRef = new TgiReference(0x3333333344444444, 0xCCCCCCCC, 0xDDDDDDDD);

        var data = original.Data.ToArray();
        var parsed = new CfltResource(TestKey, data);

        parsed.TrimRef.Instance.Should().Be(0x1111111122222222);
        parsed.TrimRef.Type.Should().Be(0xAAAAAAAA);
        parsed.TrimRef.Group.Should().Be(0xBBBBBBBB);

        parsed.ModlRef.Instance.Should().Be(0x3333333344444444);
        parsed.ModlRef.Type.Should().Be(0xCCCCCCCC);
        parsed.ModlRef.Group.Should().Be(0xDDDDDDDD);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CfltResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFAA5500);
        original.Colors.Add(0xFF00AA55);

        var data = original.Data.ToArray();
        var parsed = new CfltResource(TestKey, data);

        parsed.Colors.Count.Should().Be(2);
        parsed.Colors[0].Should().Be(0xFFAA5500);
        parsed.Colors[1].Should().Be(0xFF00AA55);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CfltResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 999;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 10;

        // Type-specific fields
        original.TrimRef = new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222);
        original.MaterialVariant = 0xFEDCBA98;
        original.ModlRef = new TgiReference(0x9876543210FEDCBA, 0x33333333, 0x44444444);
        original.SwatchGrouping = 0x0123456789ABCDEF;
        original.Unk02 = 0xDEADBEEF;

        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CfltResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CfltResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CfltResource>();
        ((CfltResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CfltResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CfltResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.TrimRef.Should().Be(TgiReference.Empty);
    }
}
