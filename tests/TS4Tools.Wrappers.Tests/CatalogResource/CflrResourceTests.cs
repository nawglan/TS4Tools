using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CflrResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFLRResource.cs
/// - CFLR is a catalog type for floor patterns/textures
/// - Type ID: 0xB4F762C9
/// - Structure: Version + CatalogCommon + HashIndicator + 3 Hashes + MaterialList + Colors + Unk02 + SwatchGrouping
/// </summary>
public class CflrResourceTests
{
    private static readonly ResourceKey TestKey = new(CflrResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cflr = new CflrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cflr.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cflr.CommonBlock.Should().NotBeNull();
        cflr.CommonBlock.Price.Should().Be(0);
        cflr.HashIndicator.Should().Be(0x1);
        cflr.Hash01.Should().Be(0x811C9DC5);
        cflr.Hash02.Should().Be(0x811C9DC5);
        cflr.Hash03.Should().Be(0x811C9DC5);
        cflr.MaterialList.Count.Should().Be(0);
        cflr.Colors.Count.Should().Be(0);
        cflr.Unk02.Should().Be(0);
        cflr.SwatchGrouping.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        // Create resource
        var original = new CflrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 150;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.HashIndicator = 0x2;
        original.Hash01 = 0x12345678;
        original.Hash02 = 0x87654321;
        original.Hash03 = 0xABCDEF00;
        original.Unk02 = 0x99887766;
        original.SwatchGrouping = 0xFEDCBA9876543210;

        // Serialize
        var data = original.Data.ToArray();

        // Parse
        var parsed = new CflrResource(TestKey, data);

        // Verify
        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(150);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.HashIndicator.Should().Be(0x2);
        parsed.Hash01.Should().Be(0x12345678);
        parsed.Hash02.Should().Be(0x87654321);
        parsed.Hash03.Should().Be(0xABCDEF00);
        parsed.Unk02.Should().Be(0x99887766);
        parsed.SwatchGrouping.Should().Be(0xFEDCBA9876543210);
    }

    [Fact]
    public void RoundTrip_WithMaterialList_PreservesReferences()
    {
        var original = new CflrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.MaterialList.Add(new TgiReference(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB));
        original.MaterialList.Add(new TgiReference(0x3333333344444444, 0xCCCCCCCC, 0xDDDDDDDD));

        var data = original.Data.ToArray();
        var parsed = new CflrResource(TestKey, data);

        parsed.MaterialList.Count.Should().Be(2);
        parsed.MaterialList[0].Instance.Should().Be(0x1111111122222222);
        parsed.MaterialList[0].Type.Should().Be(0xAAAAAAAA);
        parsed.MaterialList[0].Group.Should().Be(0xBBBBBBBB);
        parsed.MaterialList[1].Instance.Should().Be(0x3333333344444444);
        parsed.MaterialList[1].Type.Should().Be(0xCCCCCCCC);
        parsed.MaterialList[1].Group.Should().Be(0xDDDDDDDD);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CflrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFAA5500);
        original.Colors.Add(0xFF00AA55);
        original.Colors.Add(0xFF5500AA);

        var data = original.Data.ToArray();
        var parsed = new CflrResource(TestKey, data);

        parsed.Colors.Count.Should().Be(3);
        parsed.Colors[0].Should().Be(0xFFAA5500);
        parsed.Colors[1].Should().Be(0xFF00AA55);
        parsed.Colors[2].Should().Be(0xFF5500AA);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CflrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 250;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 5;

        // Type-specific fields
        original.HashIndicator = 0x3;
        original.Hash01 = 0xFEDCBA98;
        original.Hash02 = 0x12345678;
        original.Hash03 = 0x9ABCDEF0;
        original.Unk02 = 0xDEADBEEF;
        original.SwatchGrouping = 0x0123456789ABCDEF;

        original.MaterialList.Add(new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222));
        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CflrResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CflrResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CflrResource>();
        ((CflrResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CflrResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CflrResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.HashIndicator.Should().Be(0x1);
    }
}
