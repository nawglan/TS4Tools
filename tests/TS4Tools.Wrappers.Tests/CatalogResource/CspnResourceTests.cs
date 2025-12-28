using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CspnResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CSPNResource.cs
/// - CSPN is a catalog type for spindles/spandrels (ceiling rails)
/// - Type ID: 0x3F0C529A
/// - Structure: Version + CatalogCommon + 4x SpnFenMODLEntryList + Gp7references +
///              MaterialVariant + SwatchGrouping + Colors
/// </summary>
public class CspnResourceTests
{
    private static readonly ResourceKey TestKey = new(CspnResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cspn = new CspnResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cspn.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cspn.CommonBlock.Should().NotBeNull();
        cspn.ModlEntryList01.Should().NotBeNull();
        cspn.ModlEntryList01.Count.Should().Be(0);
        cspn.ModlEntryList02.Should().NotBeNull();
        cspn.ModlEntryList03.Should().NotBeNull();
        cspn.ModlEntryList04.Should().NotBeNull();
        cspn.ReferenceList.Should().NotBeNull();
        cspn.ReferenceList.Ref01.Should().Be(TgiReference.Empty);
        cspn.MaterialVariant.Should().Be(0);
        cspn.SwatchGrouping.Should().Be(0);
        cspn.Colors.Count.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        var original = new CspnResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 800;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.MaterialVariant = 0xABCDEF01;
        original.SwatchGrouping = 0x1234567890ABCDEF;

        var data = original.Data.ToArray();
        var parsed = new CspnResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(800);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.MaterialVariant.Should().Be(0xABCDEF01);
        parsed.SwatchGrouping.Should().Be(0x1234567890ABCDEF);
    }

    [Fact]
    public void RoundTrip_WithModlEntries_PreservesEntries()
    {
        var original = new CspnResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.ModlEntryList01.Add(0x1234, new TgiReference(0x1111111111111111, 0x11111111, 0x11111111));
        original.ModlEntryList02.Add(0x5678, new TgiReference(0x2222222222222222, 0x22222222, 0x22222222));
        original.ModlEntryList03.Add(0x9ABC, new TgiReference(0x3333333333333333, 0x33333333, 0x33333333));
        original.ModlEntryList04.Add(0xDEF0, new TgiReference(0x4444444444444444, 0x44444444, 0x44444444));

        var data = original.Data.ToArray();
        var parsed = new CspnResource(TestKey, data);

        parsed.ModlEntryList01.Count.Should().Be(1);
        parsed.ModlEntryList01[0].Label.Should().Be(0x1234);
        parsed.ModlEntryList01[0].Reference.Instance.Should().Be(0x1111111111111111);

        parsed.ModlEntryList02.Count.Should().Be(1);
        parsed.ModlEntryList02[0].Label.Should().Be(0x5678);

        parsed.ModlEntryList03.Count.Should().Be(1);
        parsed.ModlEntryList03[0].Label.Should().Be(0x9ABC);

        parsed.ModlEntryList04.Count.Should().Be(1);
        parsed.ModlEntryList04[0].Label.Should().Be(0xDEF0);
    }

    [Fact]
    public void RoundTrip_WithGp7References_PreservesReferences()
    {
        var original = new CspnResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.ReferenceList.Ref01 = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.ReferenceList.Ref04 = new TgiReference(0x4444444444444444, 0x44444444, 0x44444444);
        original.ReferenceList.Ref07 = new TgiReference(0x7777777777777777, 0x77777777, 0x77777777);

        var data = original.Data.ToArray();
        var parsed = new CspnResource(TestKey, data);

        parsed.ReferenceList.Ref01.Instance.Should().Be(0x1111111111111111);
        parsed.ReferenceList.Ref04.Instance.Should().Be(0x4444444444444444);
        parsed.ReferenceList.Ref07.Instance.Should().Be(0x7777777777777777);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CspnResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFFAA5500);
        original.Colors.Add(0xFF00AA55);
        original.Colors.Add(0xFF5500AA);

        var data = original.Data.ToArray();
        var parsed = new CspnResource(TestKey, data);

        parsed.Colors.Count.Should().Be(3);
        parsed.Colors[0].Should().Be(0xFFAA5500);
        parsed.Colors[1].Should().Be(0xFF00AA55);
        parsed.Colors[2].Should().Be(0xFF5500AA);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CspnResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 1200;
        original.CommonBlock.NameHash = 0x11223344;
        original.CommonBlock.DescriptionHash = 0x55667788;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x42;
        original.CommonBlock.PackId = 8;

        // Type-specific fields
        original.ModlEntryList01.Add(0xAAAA, new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222));
        original.ReferenceList.Ref01 = new TgiReference(0x9876543210FEDCBA, 0x33333333, 0x44444444);
        original.MaterialVariant = 0xFEDCBA98;
        original.SwatchGrouping = 0x0123456789ABCDEF;

        original.Colors.Add(0xFF112233);
        original.CommonBlock.Tags.Add(0x00001234);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CspnResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CspnResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CspnResource>();
        ((CspnResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CspnResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CspnResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        resource.ReferenceList.Ref01.Should().Be(TgiReference.Empty);
    }
}
