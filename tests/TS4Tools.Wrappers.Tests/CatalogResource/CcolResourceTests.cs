using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CcolResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CCOLResource.cs
/// - CCOL is a catalog type for color variations, extends AbstractCatalogResource
/// - Type ID: 0x1D6DF1CF
/// - Structure: AbstractCatalogResource + Unknown02 + ReferencesIndicator + conditional refs
/// </summary>
public class CcolResourceTests
{
    private static readonly ResourceKey TestKey = new(CcolResource.TypeId, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var ccol = new CcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        ccol.Version.Should().Be(AbstractCatalogResource.DefaultVersion);
        ccol.CommonBlock.Should().NotBeNull();
        ccol.Unknown02.Should().Be(0);
        ccol.ReferencesIndicator.Should().Be(0);
        ccol.NullRefs.Should().NotBeNull();
        ccol.ModlRefs.Should().BeNull();
        ccol.FtptRefs.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_WithNullRefs_PreservesData()
    {
        var original = new CcolResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 500;
        original.CommonBlock.NameHash = 0xDEADBEEF;
        original.Unknown02 = 0x12345678;
        original.ReferencesIndicator = 0; // Use NullRefs
        original.NullRefs = new Gp4References();
        original.NullRefs.Ref01 = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.NullRefs.Ref02 = new TgiReference(0x2222222222222222, 0x22222222, 0x22222222);

        var data = original.Data.ToArray();
        var parsed = new CcolResource(TestKey, data);

        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(500);
        parsed.CommonBlock.NameHash.Should().Be(0xDEADBEEF);
        parsed.Unknown02.Should().Be(0x12345678);
        parsed.ReferencesIndicator.Should().Be(0);
        parsed.NullRefs.Should().NotBeNull();
        parsed.NullRefs!.Ref01.Instance.Should().Be(0x1111111111111111);
        parsed.NullRefs.Ref02.Instance.Should().Be(0x2222222222222222);
        parsed.ModlRefs.Should().BeNull();
        parsed.FtptRefs.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_WithModlAndFtptRefs_PreservesData()
    {
        var original = new CcolResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Unknown02 = 0xABCDEF01;
        original.ReferencesIndicator = 1; // Use ModlRefs + FtptRefs
        original.ModlRefs = new Gp9References();
        original.ModlRefs.Ref01 = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.ModlRefs.Ref09 = new TgiReference(0x9999999999999999, 0x99999999, 0x99999999);
        original.FtptRefs = new Gp9References();
        original.FtptRefs.Ref01 = new TgiReference(0xAAAAAAAAAAAAAAAA, 0xAAAAAAAA, 0xAAAAAAAA);

        var data = original.Data.ToArray();
        var parsed = new CcolResource(TestKey, data);

        parsed.ReferencesIndicator.Should().Be(1);
        parsed.NullRefs.Should().BeNull();
        parsed.ModlRefs.Should().NotBeNull();
        parsed.ModlRefs!.Ref01.Instance.Should().Be(0x1111111111111111);
        parsed.ModlRefs.Ref09.Instance.Should().Be(0x9999999999999999);
        parsed.FtptRefs.Should().NotBeNull();
        parsed.FtptRefs!.Ref01.Instance.Should().Be(0xAAAAAAAAAAAAAAAA);
    }

    [Fact]
    public void RoundTrip_FullResource_WithNullRefs_ByteForByte()
    {
        var original = new CcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 1000;
        original.CommonBlock.NameHash = 0x11223344;

        // Base AbstractCatalogResource fields
        original.AuralMaterialsVersion = 0x1;
        original.PlacementFlagsHigh = 0xFFFF;
        original.SlotDecoSize = 5;
        original.FenceHeight = 100;
        original.Colors.Add(0xFF112233);

        // Type-specific fields
        original.Unknown02 = 0xFEDCBA98;
        original.ReferencesIndicator = 0;
        original.NullRefs = new Gp4References();
        original.NullRefs.Ref01 = new TgiReference(0xABCDEF0123456789, 0x11111111, 0x22222222);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CcolResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void RoundTrip_FullResource_WithModlRefs_ByteForByte()
    {
        var original = new CcolResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Common block fields
        original.CommonBlock.Price = 2000;

        // Type-specific fields
        original.Unknown02 = 0x12345678;
        original.ReferencesIndicator = 1;
        original.ModlRefs = new Gp9References();
        original.ModlRefs.Ref01 = new TgiReference(0x1111111111111111, 0x11111111, 0x11111111);
        original.FtptRefs = new Gp9References();
        original.FtptRefs.Ref01 = new TgiReference(0x2222222222222222, 0x22222222, 0x22222222);

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CcolResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new CcolResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<CcolResource>();
        ((CcolResource)resource).Key.Should().Be(TestKey);
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new CcolResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as CcolResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(AbstractCatalogResource.DefaultVersion);
        resource.NullRefs.Should().NotBeNull();
    }
}
