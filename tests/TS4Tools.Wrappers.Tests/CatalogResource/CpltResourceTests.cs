using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CpltResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CPLTResource.cs
/// - CPLT is a simple catalog type for pool trim
/// - Type ID: 0xA5DFFCF3
/// - Structure: Version + CatalogCommon + TrimRef + MaterialVariant + ModlRef + SwatchGrouping + Colors + Unk02
/// </summary>
public class CpltResourceTests
{
    private static readonly ResourceKey TestKey = new(CpltResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cplt = new CpltResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cplt.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cplt.CommonBlock.Should().NotBeNull();
        cplt.CommonBlock.Price.Should().Be(0);
        cplt.TrimRef.Should().Be(TgiReference.Empty);
        cplt.ModlRef.Should().Be(TgiReference.Empty);
        cplt.MaterialVariant.Should().Be(0);
        cplt.SwatchGrouping.Should().Be(0);
        cplt.Colors.Count.Should().Be(0);
        cplt.Unk02.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        // Create resource
        var original = new CpltResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 150;
        original.CommonBlock.NameHash = 0xBEEFCAFE;
        original.MaterialVariant = 0x87654321;
        original.SwatchGrouping = 0xFEDCBA9876543210;
        original.Unk02 = 0x98765432;

        // Serialize
        var data = original.Data.ToArray();

        // Parse
        var parsed = new CpltResource(TestKey, data);

        // Verify
        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(150);
        parsed.CommonBlock.NameHash.Should().Be(0xBEEFCAFE);
        parsed.MaterialVariant.Should().Be(0x87654321);
        parsed.SwatchGrouping.Should().Be(0xFEDCBA9876543210);
        parsed.Unk02.Should().Be(0x98765432);
    }

    [Fact]
    public void RoundTrip_WithTgiReferences_PreservesReferences()
    {
        var original = new CpltResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.TrimRef = new TgiReference(0xAAAABBBBCCCCDDDD, 0x11112222, 0x33334444);
        original.ModlRef = new TgiReference(0xEEEEFFFF00001111, 0x55556666, 0x77778888);

        var data = original.Data.ToArray();
        var parsed = new CpltResource(TestKey, data);

        parsed.TrimRef.Instance.Should().Be(0xAAAABBBBCCCCDDDD);
        parsed.TrimRef.Type.Should().Be(0x11112222);
        parsed.TrimRef.Group.Should().Be(0x33334444);

        parsed.ModlRef.Instance.Should().Be(0xEEEEFFFF00001111);
        parsed.ModlRef.Type.Should().Be(0x55556666);
        parsed.ModlRef.Group.Should().Be(0x77778888);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CpltResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFF00AAFF);
        original.Colors.Add(0xFFAA00FF);
        original.Colors.Add(0xFFFFAA00);

        var data = original.Data.ToArray();
        var parsed = new CpltResource(TestKey, data);

        parsed.Colors.Count.Should().Be(3);
        parsed.Colors[0].Should().Be(0xFF00AAFF);
        parsed.Colors[1].Should().Be(0xFFAA00FF);
        parsed.Colors[2].Should().Be(0xFFFFAA00);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CpltResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 500;
        original.CommonBlock.NameHash = 0xDEADC0DE;
        original.CommonBlock.DescriptionHash = 0xCAFED00D;
        original.CommonBlock.ThumbnailHash = 0x1234567890ABCDEF;
        original.CommonBlock.DevCategoryFlags = 0xFF;
        original.CommonBlock.PackId = 20;

        // Type-specific fields
        original.TrimRef = new TgiReference(0x0011223344556677, 0xAABBCCDD, 0xEEFF0011);
        original.MaterialVariant = 0x12345678;
        original.ModlRef = new TgiReference(0x8899AABBCCDDEEFF, 0x22334455, 0x66778899);
        original.SwatchGrouping = 0xDEADBEEFCAFEBABE;
        original.Unk02 = 0xCAFECAFE;

        original.Colors.Add(0xFFDDEEFF);
        original.Colors.Add(0xFF001122);
        original.CommonBlock.Tags.Add(0x00005678);
        original.CommonBlock.Tags.Add(0x0000ABCD);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CpltResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CpltResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CpltResource>();
        ((CpltResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CpltResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CpltResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.TrimRef.Should().Be(TgiReference.Empty);
    }
}
