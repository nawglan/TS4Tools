using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CrtrResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CRTRResource.cs
/// - CRTR is a catalog type for roof trim
/// - Type ID: 0xB0311D0F
/// - Structure: Version + CatalogCommon + Unk01 + Unk02 + TrimRef + MaterialVariant + ModlRef + SwatchGrouping + Colors
/// </summary>
public class CrtrResourceTests
{
    private static readonly ResourceKey TestKey = new(CrtrResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var crtr = new CrtrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        crtr.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        crtr.CommonBlock.Should().NotBeNull();
        crtr.CommonBlock.Price.Should().Be(0);
        crtr.Unk01.Should().Be(0);
        crtr.Unk02.Should().Be(0);
        crtr.TrimRef.Should().Be(TgiReference.Empty);
        crtr.ModlRef.Should().Be(TgiReference.Empty);
        crtr.MaterialVariant.Should().Be(0);
        crtr.SwatchGrouping.Should().Be(0);
        crtr.Colors.Count.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        // Create resource
        var original = new CrtrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 75;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.Unk01 = 0x12;
        original.Unk02 = 0x34;
        original.MaterialVariant = 0x12345678;
        original.SwatchGrouping = 0xCAFEBABE12345678;

        // Serialize
        var data = original.Data.ToArray();

        // Parse
        var parsed = new CrtrResource(TestKey, data);

        // Verify
        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(75);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.Unk01.Should().Be(0x12);
        parsed.Unk02.Should().Be(0x34);
        parsed.MaterialVariant.Should().Be(0x12345678);
        parsed.SwatchGrouping.Should().Be(0xCAFEBABE12345678);
    }

    [Fact]
    public void RoundTrip_WithTgiReferences_PreservesReferences()
    {
        var original = new CrtrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.TrimRef = new TgiReference(0x1111111122222222, 0xAAAAAAAA, 0xBBBBBBBB);
        original.ModlRef = new TgiReference(0x3333333344444444, 0xCCCCCCCC, 0xDDDDDDDD);

        var data = original.Data.ToArray();
        var parsed = new CrtrResource(TestKey, data);

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
        var original = new CrtrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFF0000FF); // Red
        original.Colors.Add(0xFF00FF00); // Green
        original.Colors.Add(0xFFFF0000); // Blue

        var data = original.Data.ToArray();
        var parsed = new CrtrResource(TestKey, data);

        parsed.Colors.Count.Should().Be(3);
        parsed.Colors[0].Should().Be(0xFF0000FF);
        parsed.Colors[1].Should().Be(0xFF00FF00);
        parsed.Colors[2].Should().Be(0xFFFF0000);
    }

    [Fact]
    public void RoundTrip_WithTags_PreservesTags()
    {
        var original = new CrtrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Tags.Add(0x00010001);
        original.CommonBlock.Tags.Add(0x00020002);

        var data = original.Data.ToArray();
        var parsed = new CrtrResource(TestKey, data);

        parsed.CommonBlock.Tags.Count.Should().Be(2);
        parsed.CommonBlock.Tags[0].Should().Be(0x00010001);
        parsed.CommonBlock.Tags[1].Should().Be(0x00020002);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CrtrResource(TestKey, ReadOnlyMemory<byte>.Empty);

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
        original.Unk01 = 0xAB;
        original.Unk02 = 0xCD;
        original.TrimRef = new TgiReference(0x1234567890ABCDEF, 0x12345678, 0x00000001);
        original.MaterialVariant = 0xFEDCBA98;
        original.ModlRef = new TgiReference(0xFEDCBA0987654321, 0x87654321, 0x00000002);
        original.SwatchGrouping = 0xABCDEF0123456789;

        original.Colors.Add(0xFFAABBCC);
        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00000042);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CrtrResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CrtrResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CrtrResource>();
        ((CrtrResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CrtrResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CrtrResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.TrimRef.Should().Be(TgiReference.Empty);
    }
}
