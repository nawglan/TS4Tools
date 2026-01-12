using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="SimpleCatalogResource"/> base class.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CFTRResource.cs (pattern)
/// - Abstract base class for simple catalog resources (CFTR, CFLT, CFLR, CWAL, etc.)
/// - Contains only Version + CatalogCommon + type-specific fields
/// - Tests use CftrResource as the concrete implementation
/// </summary>
public class SimpleCatalogResourceTests
{
    private static readonly ResourceKey TestKey = new(CftrResource.TypeId, 0, 0);

    #region Constants Tests

    [Fact]
    public void DefaultVersion_IsExpected()
    {
        SimpleCatalogResource.DefaultVersion.Should().Be(0x07);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void CreateEmpty_Version_IsDefault()
    {
        var resource = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
    }

    [Fact]
    public void CreateEmpty_CommonBlock_IsInitialized()
    {
        var resource = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.CommonBlock.Should().NotBeNull();
    }

    [Fact]
    public void CreateEmpty_CommonBlock_HasDefaultValues()
    {
        var resource = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.CommonBlock.Price.Should().Be(0);
        resource.CommonBlock.NameHash.Should().Be(0);
        resource.CommonBlock.DescriptionHash.Should().Be(0);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_Version_PreservesValue()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 0x08;

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.Version.Should().Be(0x08);
    }

    [Fact]
    public void RoundTrip_CommonBlock_PreservesAllFields()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.NameHash = 0x12345678;
        original.CommonBlock.DescriptionHash = 0x87654321;
        original.CommonBlock.Price = 999;
        original.CommonBlock.ThumbnailHash = 0xAABBCCDDEEFF0011;
        original.CommonBlock.DevCategoryFlags = 0x11223344;
        original.CommonBlock.VarientThumbImageHash = 0xFFEEDDCCBBAA9988;

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.CommonBlock.NameHash.Should().Be(0x12345678);
        parsed.CommonBlock.DescriptionHash.Should().Be(0x87654321);
        parsed.CommonBlock.Price.Should().Be(999);
        parsed.CommonBlock.ThumbnailHash.Should().Be(0xAABBCCDDEEFF0011);
        parsed.CommonBlock.DevCategoryFlags.Should().Be(0x11223344);
        parsed.CommonBlock.VarientThumbImageHash.Should().Be(0xFFEEDDCCBBAA9988);
    }

    [Fact]
    public void RoundTrip_CommonBlockTags_PreservesTags()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Tags.Add(0x1111);
        original.CommonBlock.Tags.Add(0x2222);
        original.CommonBlock.Tags.Add(0x3333);

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.CommonBlock.Tags.Count.Should().Be(3);
        parsed.CommonBlock.Tags[0].Should().Be(0x1111);
        parsed.CommonBlock.Tags[1].Should().Be(0x2222);
        parsed.CommonBlock.Tags[2].Should().Be(0x3333);
    }

    [Fact]
    public void RoundTrip_CommonBlockSellingPoints_PreservesSellingPoints()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.SellingPoints.Add(new SellingPoint(0xAAAA, 100));
        original.CommonBlock.SellingPoints.Add(new SellingPoint(0xBBBB, 200));

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.CommonBlock.SellingPoints.Count.Should().Be(2);
        parsed.CommonBlock.SellingPoints.Points[0].CommodityTag.Should().Be(0xAAAA);
        parsed.CommonBlock.SellingPoints.Points[0].Amount.Should().Be(100);
        parsed.CommonBlock.SellingPoints.Points[1].CommodityTag.Should().Be(0xBBBB);
        parsed.CommonBlock.SellingPoints.Points[1].Amount.Should().Be(200);
    }

    #endregion

    #region Multiple Derived Types Tests

    [Fact]
    public void CfltResource_UsesSimpleCatalogBase()
    {
        var cflt = new CfltResource(new ResourceKey(CfltResource.TypeId, 0, 0), ReadOnlyMemory<byte>.Empty);

        cflt.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cflt.CommonBlock.Should().NotBeNull();
    }

    [Fact]
    public void CflrResource_UsesSimpleCatalogBase()
    {
        var cflr = new CflrResource(new ResourceKey(CflrResource.TypeId, 0, 0), ReadOnlyMemory<byte>.Empty);

        cflr.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cflr.CommonBlock.Should().NotBeNull();
    }

    [Fact]
    public void CralResource_UsesSimpleCatalogBase()
    {
        var cral = new CralResource(new ResourceKey(CralResource.TypeId, 0, 0), ReadOnlyMemory<byte>.Empty);

        cral.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cral.CommonBlock.Should().NotBeNull();
    }

    [Fact]
    public void CspnResource_UsesSimpleCatalogBase()
    {
        var cspn = new CspnResource(new ResourceKey(CspnResource.TypeId, 0, 0), ReadOnlyMemory<byte>.Empty);

        cspn.Version.Should().Be(SimpleCatalogResource.DefaultVersion);
        cspn.CommonBlock.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void CreateEmpty_EmptyTagList_SerializesCorrectly()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        // Tags list is empty by default

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.CommonBlock.Tags.Count.Should().Be(0);
    }

    [Fact]
    public void CreateEmpty_EmptySellingPoints_SerializesCorrectly()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        // SellingPoints list is empty by default

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.CommonBlock.SellingPoints.Count.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_ZeroPrice_PreservesValue()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = 0;

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.CommonBlock.Price.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_MaxPrice_PreservesValue()
    {
        var original = new CftrResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.CommonBlock.Price = uint.MaxValue;

        var data = original.Data.ToArray();
        var parsed = new CftrResource(TestKey, data);

        parsed.CommonBlock.Price.Should().Be(uint.MaxValue);
    }

    #endregion
}
