using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CftrResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFTRResource.cs
/// - CFTR is a simple catalog type for fountain trim
/// - Type ID: 0xE7ADA79D
/// - Structure: Version + CatalogCommon + TrimRef + MaterialVariant + ModlRef + SwatchGrouping + Colors + Unk01
/// </summary>
public class CftrResourceTests
{
    private static readonly ResourceKey TestKey = new(CftrResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cftr = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cftr.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cftr.CommonBlock.Should().NotBeNull();
        cftr.CommonBlock.Price.Should().Be(0);
        cftr.TrimRef.Should().Be(TgiReference.Empty);
        cftr.ModlRef.Should().Be(TgiReference.Empty);
        cftr.MaterialVariant.Should().Be(0);
        cftr.SwatchGrouping.Should().Be(0);
        cftr.Colors.Count.Should().Be(0);
        cftr.Unk01.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        // Create resource
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 50;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.MaterialVariant = 0x12345678;
        original.SwatchGrouping = 0xCAFEBABE12345678;
        original.Unk01 = 1;

        // Serialize
        var data = original.Data.ToArray();

        // Parse
        var parsed = new CftrResource(TestKey, data);

        // Verify
        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(50);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.MaterialVariant.Should().Be(0x12345678);
        parsed.SwatchGrouping.Should().Be(0xCAFEBABE12345678);
        parsed.Unk01.Should().Be(1);
    }

    [Fact]
    public void RoundTrip_WithTgiReferences_PreservesReferences()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.TrimRef = new TgiReference(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB);
        original.ModlRef = new TgiReference(0x3333333344444444, 0xCCCCCCCC, 0xDDDDDDDD);

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

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
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFF0000FF); // Red
        original.Colors.Add(0xFF00FF00); // Green
        original.Colors.Add(0xFFFF0000); // Blue

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.Colors.Count.Should().Be(3);
        parsed.Colors[0].Should().Be(0xFF0000FF);
        parsed.Colors[1].Should().Be(0xFF00FF00);
        parsed.Colors[2].Should().Be(0xFFFF0000);
    }

    [Fact]
    public void RoundTrip_WithTags_PreservesTags()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Tags.Add(0x00010001);
        original.CommonBlock.Tags.Add(0x00020002);

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.CommonBlock.Tags.Count.Should().Be(2);
        parsed.CommonBlock.Tags[0].Should().Be(0x00010001);
        parsed.CommonBlock.Tags[1].Should().Be(0x00020002);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 1234;
        original.CommonBlock.NameHash = 0xAABBCCDD;
        original.CommonBlock.DescriptionHash = 0x11223344;
        original.CommonBlock.ThumbnailHash = 0xDEADBEEFCAFEBABE;
        original.CommonBlock.DevCategoryFlags = 0x12;
        original.CommonBlock.PackId = 5;
        original.CommonBlock.UnlockByHash = 0x55667788;
        original.CommonBlock.UnlockedByHash = 0x99AABBCC;

        // Type-specific fields
        original.TrimRef = new TgiReference(0x1234567890ABCDEF, 0x12345678, 0x00000001);
        original.MaterialVariant = 0xFEDCBA98;
        original.ModlRef = new TgiReference(0xFEDCBA0987654321, 0x87654321, 0x00000002);
        original.SwatchGrouping = 0xABCDEF0123456789;
        original.Unk01 = 0xFF;

        original.Colors.Add(0xFFAABBCC);
        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00000042);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CftrResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CftrResource>();
        ((CftrResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CftrResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CftrResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.TrimRef.Should().Be(TgiReference.Empty);
    }
}
