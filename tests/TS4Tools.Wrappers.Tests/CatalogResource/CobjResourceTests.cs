using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CobjResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/COBJResource.cs
/// - COBJ is the simplest catalog type - extends AbstractCatalogResource with no additional fields
/// - Type ID: 0x319E4F1D
/// - Base class handles all parsing/serialization
/// </summary>
public class CobjResourceTests
{
    private static readonly ResourceKey TestKey = new(0x319E4F1D, 0, 0);

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var cobj = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cobj.Version.Should().Be(AbstractCatalogResource.DefaultVersion);
        cobj.CommonBlock.Should().NotBeNull();
        cobj.CommonBlock.Price.Should().Be(0);
        cobj.Colors.Count.Should().Be(0);
        cobj.AuralMaterialsVersion.Should().Be(0x1);
        cobj.AuralPropertiesVersion.Should().Be(0x2);
    }

    [Fact]
    public void CreateEmpty_AuralMaterials_HaveFnvBasis()
    {
        var cobj = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        cobj.AuralMaterials1.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
        cobj.AuralMaterials2.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
        cobj.AuralMaterials3.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
        cobj.AuralQuality.Should().Be(AbstractCatalogResource.FnvOffsetBasis);
    }

    [Fact]
    public void RoundTrip_EmptyResource_PreservesData()
    {
        // Create resource
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 100;
        original.CommonBlock.NameHash = 0x12345678;
        original.FenceHeight = 5;

        // Serialize
        var data = original.Data.ToArray();

        // Parse
        var parsed = new CobjResource(TestKey, data);

        // Verify
        parsed.Version.Should().Be(original.Version);
        parsed.CommonBlock.Price.Should().Be(100);
        parsed.CommonBlock.NameHash.Should().Be(0x12345678);
        parsed.FenceHeight.Should().Be(5);
    }

    [Fact]
    public void RoundTrip_WithColors_PreservesColors()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Colors.Add(0xFF0000FF);
        original.Colors.Add(0xFF00FF00);

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.Colors.Count.Should().Be(2);
        parsed.Colors[0].Should().Be(0xFF0000FF);
        parsed.Colors[1].Should().Be(0xFF00FF00);
    }

    [Fact]
    public void RoundTrip_WithPlacement_PreservesPlacement()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.PlacementFlagsHigh = 0x11111111;
        original.PlacementFlagsLow = 0x22222222;
        original.SlotTypeSet = 0x3333333344444444;
        original.SlotDecoSize = 5;
        original.CatalogGroup = 0x5555555566666666;
        original.StateUsage = 0x77;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.PlacementFlagsHigh.Should().Be(0x11111111);
        parsed.PlacementFlagsLow.Should().Be(0x22222222);
        parsed.SlotTypeSet.Should().Be(0x3333333344444444);
        parsed.SlotDecoSize.Should().Be(5);
        parsed.CatalogGroup.Should().Be(0x5555555566666666);
        parsed.StateUsage.Should().Be(0x77);
    }

    [Fact]
    public void RoundTrip_WithAuralV3_PreservesAmbienceFields()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.AuralPropertiesVersion = 3;
        original.AmbienceFileInstanceId = 0xAABBCCDDEEFF0011;
        original.IsOverrideAmbience = 1;

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.AuralPropertiesVersion.Should().Be(3);
        parsed.AmbienceFileInstanceId.Should().Be(0xAABBCCDDEEFF0011);
        parsed.IsOverrideAmbience.Should().Be(1);
    }

    [Fact]
    public void RoundTrip_WithFallbackKey_PreservesFallbackKey()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x19; // Requires version 0x19+
        original.FallbackObjectKey = new TgiReference(0x1122334455667788, 0x12345678, 0x00000001);

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.FallbackObjectKey.Instance.Should().Be(0x1122334455667788);
        parsed.FallbackObjectKey.Type.Should().Be(0x12345678);
        parsed.FallbackObjectKey.Group.Should().Be(0x00000001);
    }

    [Fact]
    public void RoundTrip_WithTags_PreservesTags()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Tags.Add(0x00010001);
        original.CommonBlock.Tags.Add(0x00020002);
        original.CommonBlock.Tags.Add(0x00030003);

        var data = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data);

        parsed.CommonBlock.Tags.Count.Should().Be(3);
        parsed.CommonBlock.Tags[0].Should().Be(0x00010001);
        parsed.CommonBlock.Tags[1].Should().Be(0x00020002);
        parsed.CommonBlock.Tags[2].Should().Be(0x00030003);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new CobjResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Set various fields
        original.CommonBlock.Price = 1234;
        original.CommonBlock.NameHash = 0xAABBCCDD;
        original.CommonBlock.DescriptionHash = 0x11223344;
        original.CommonBlock.ThumbnailHash = 0xDEADBEEFCAFEBABE;
        original.CommonBlock.DevCategoryFlags = 0x12;
        original.CommonBlock.PackId = 5;
        original.CommonBlock.UnlockByHash = 0x55667788;
        original.CommonBlock.UnlockedByHash = 0x99AABBCC;

        original.AuralMaterials1 = 0x01010101;
        original.AuralMaterials2 = 0x02020202;
        original.AuralMaterials3 = 0x03030303;
        original.AuralQuality = 0x04040404;
        original.AuralAmbientObject = 0x05050505;

        original.PlacementFlagsHigh = 0x10101010;
        original.PlacementFlagsLow = 0x20202020;
        original.IsStackable = 1;
        original.CanItemDepreciate = 1;

        original.Colors.Add(0xFFAABBCC);
        original.CommonBlock.Tags.Add(0x00000042);
        original.CommonBlock.SellingPoints.Add(new SellingPoint(1, 100));

        // Serialize twice and compare
        var data1 = original.Data.ToArray();
        var parsed = new CobjResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }
}
