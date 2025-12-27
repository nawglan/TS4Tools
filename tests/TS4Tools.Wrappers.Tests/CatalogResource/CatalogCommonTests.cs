using FluentAssertions;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="CatalogCommon"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/CatalogCommon.cs
/// - CatalogCommon is the shared header block used by all catalog resources.
/// - Version-dependent parsing:
///   - v9 and below: Uses unused2/unused3 fields
///   - v10+: Uses packId, packFlags, reservedBytes
///   - v11+: Uses new tag list format (uint32 tags vs uint16)
/// </summary>
public class CatalogCommonTests
{
    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var common = new CatalogCommon();

        common.CommonBlockVersion.Should().Be(CatalogCommon.DefaultVersion);
        common.Price.Should().Be(0);
        common.NameHash.Should().Be(0);
        common.DescriptionHash.Should().Be(0);
        common.ThumbnailHash.Should().Be(0);
        common.Tags.Count.Should().Be(0);
        common.SellingPoints.Count.Should().Be(0);
        common.ProductStyles.Count.Should().Be(0);
    }

    [Fact]
    public void GetSerializedSize_EmptyV11_CalculatesCorrectly()
    {
        var common = new CatalogCommon
        {
            CommonBlockVersion = 0x0B // Version 11
        };

        int size = common.GetSerializedSize();

        // Fixed header (28) + ProductStyles (1) + v10 fields (12) + Tags v11 (4) +
        // SellingPoints (4) + remaining (18) = 67
        size.Should().Be(67);
    }

    [Fact]
    public void RoundTrip_EmptyV11_PreservesData()
    {
        var original = new CatalogCommon
        {
            CommonBlockVersion = 0x0B,
            NameHash = 0x12345678,
            DescriptionHash = 0xABCDEF01,
            Price = 500,
            ThumbnailHash = 0x1122334455667788,
            DevCategoryFlags = 0xFF,
            PackId = 42,
            PackOptions = PackDisplayOption.None,
            UnlockByHash = 0x11111111,
            UnlockedByHash = 0x22222222,
            SwatchColorsSortPriority = 0x1234,
            VarientThumbImageHash = 0xAABBCCDDEEFF0011
        };

        // Serialize
        var buffer = new byte[original.GetSerializedSize()];
        int written = original.WriteTo(buffer);

        // Parse
        var parsed = CatalogCommon.Parse(buffer, out int bytesRead);

        // Verify
        written.Should().Be(bytesRead);
        parsed.CommonBlockVersion.Should().Be(original.CommonBlockVersion);
        parsed.NameHash.Should().Be(original.NameHash);
        parsed.DescriptionHash.Should().Be(original.DescriptionHash);
        parsed.Price.Should().Be(original.Price);
        parsed.ThumbnailHash.Should().Be(original.ThumbnailHash);
        parsed.DevCategoryFlags.Should().Be(original.DevCategoryFlags);
        parsed.PackId.Should().Be(original.PackId);
        parsed.PackOptions.Should().Be(original.PackOptions);
        parsed.UnlockByHash.Should().Be(original.UnlockByHash);
        parsed.UnlockedByHash.Should().Be(original.UnlockedByHash);
        parsed.SwatchColorsSortPriority.Should().Be(original.SwatchColorsSortPriority);
        parsed.VarientThumbImageHash.Should().Be(original.VarientThumbImageHash);
    }

    [Fact]
    public void RoundTrip_WithTags_PreservesTags()
    {
        var original = new CatalogCommon { CommonBlockVersion = 0x0B };
        original.Tags.Add(0x00000001);
        original.Tags.Add(0x00000002);
        original.Tags.Add(0x00000003);

        // Serialize
        var buffer = new byte[original.GetSerializedSize()];
        original.WriteTo(buffer);

        // Parse
        var parsed = CatalogCommon.Parse(buffer, out _);

        parsed.Tags.Count.Should().Be(3);
        parsed.Tags[0].Should().Be(0x00000001);
        parsed.Tags[1].Should().Be(0x00000002);
        parsed.Tags[2].Should().Be(0x00000003);
    }

    [Fact]
    public void RoundTrip_WithSellingPoints_PreservesSellingPoints()
    {
        var original = new CatalogCommon { CommonBlockVersion = 0x0B };
        original.SellingPoints.Add(new SellingPoint(100, 50));
        original.SellingPoints.Add(new SellingPoint(200, -25));

        // Serialize
        var buffer = new byte[original.GetSerializedSize()];
        original.WriteTo(buffer);

        // Parse
        var parsed = CatalogCommon.Parse(buffer, out _);

        parsed.SellingPoints.Count.Should().Be(2);
        parsed.SellingPoints.Points[0].CommodityTag.Should().Be(100);
        parsed.SellingPoints.Points[0].Amount.Should().Be(50);
        parsed.SellingPoints.Points[1].CommodityTag.Should().Be(200);
        parsed.SellingPoints.Points[1].Amount.Should().Be(-25);
    }

    [Fact]
    public void RoundTrip_WithProductStyles_PreservesProductStyles()
    {
        var original = new CatalogCommon { CommonBlockVersion = 0x0B };
        original.ProductStyles.Add(new TgiReference(0x1122334455667788, 0x12345678, 0x00000001));

        // Serialize
        var buffer = new byte[original.GetSerializedSize()];
        original.WriteTo(buffer);

        // Parse
        var parsed = CatalogCommon.Parse(buffer, out _);

        parsed.ProductStyles.Count.Should().Be(1);
        parsed.ProductStyles[0].Instance.Should().Be(0x1122334455667788);
        parsed.ProductStyles[0].Type.Should().Be(0x12345678);
        parsed.ProductStyles[0].Group.Should().Be(0x00000001);
    }
}

/// <summary>
/// Tests for helper types used by CatalogCommon.
/// </summary>
public class CatalogHelperTypesTests
{
    [Fact]
    public void ColorList_RoundTrip_PreservesColors()
    {
        var original = new ColorList();
        original.Add(0xFF0000FF); // Blue
        original.Add(0xFF00FF00); // Green
        original.Add(0xFFFF0000); // Red

        var buffer = new byte[original.GetSerializedSize()];
        original.WriteTo(buffer);

        var parsed = ColorList.Parse(buffer, out int bytesRead);

        bytesRead.Should().Be(buffer.Length);
        parsed.Count.Should().Be(3);
        parsed[0].Should().Be(0xFF0000FF);
        parsed[1].Should().Be(0xFF00FF00);
        parsed[2].Should().Be(0xFFFF0000);
    }

    [Fact]
    public void SellingPointList_RoundTrip_PreservesPoints()
    {
        var original = new SellingPointList();
        original.Add(new SellingPoint(1, 100));
        original.Add(new SellingPoint(2, -50));

        var buffer = new byte[original.GetSerializedSize()];
        original.WriteTo(buffer);

        var parsed = SellingPointList.Parse(buffer, out int bytesRead);

        bytesRead.Should().Be(buffer.Length);
        parsed.Count.Should().Be(2);
        parsed.Points[0].Amount.Should().Be(100);
        parsed.Points[1].Amount.Should().Be(-50);
    }

    [Fact]
    public void TgiReferenceList_RoundTrip_PreservesReferences()
    {
        var original = new TgiReferenceList();
        original.Add(new TgiReference(0x0011223344556677, 0x12345678, 0x00000001));
        original.Add(new TgiReference(0xAABBCCDDEEFF0011, 0x87654321, 0x00000002));

        var buffer = new byte[original.GetSerializedSize()];
        original.WriteTo(buffer);

        var parsed = TgiReferenceList.Parse(buffer, out int bytesRead);

        bytesRead.Should().Be(buffer.Length);
        parsed.Count.Should().Be(2);
        parsed[0].Instance.Should().Be(0x0011223344556677);
        parsed[0].Type.Should().Be(0x12345678);
        parsed[1].Instance.Should().Be(0xAABBCCDDEEFF0011);
    }

    [Fact]
    public void CatalogTagList_V11_RoundTrip()
    {
        var original = new CatalogTagList();
        original.Add(0x00010001);
        original.Add(0x00020002);

        var buffer = new byte[original.GetSerializedSizeV11()];
        original.WriteToV11(buffer);

        var parsed = CatalogTagList.ParseV11(buffer, out int bytesRead);

        bytesRead.Should().Be(buffer.Length);
        parsed.Count.Should().Be(2);
        parsed[0].Should().Be(0x00010001);
        parsed[1].Should().Be(0x00020002);
    }

    [Fact]
    public void CatalogTagList_Legacy_RoundTrip()
    {
        var original = new CatalogTagList();
        original.Add(0x0001);
        original.Add(0x0002);

        var buffer = new byte[original.GetSerializedSizeLegacy()];
        original.WriteToLegacy(buffer);

        var parsed = CatalogTagList.ParseLegacy(buffer, out int bytesRead);

        bytesRead.Should().Be(buffer.Length);
        parsed.Count.Should().Be(2);
        parsed[0].Should().Be(0x0001);
        parsed[1].Should().Be(0x0002);
    }
}
