using FluentAssertions;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for <see cref="CatalogViewerViewModel"/>.
/// </summary>
public class CatalogViewerViewModelTests
{
    private static readonly ResourceKey CobjKey = new(0x319E4F1D, 0, 0);

    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        // Act
        var vm = new CatalogViewerViewModel();

        // Assert
        vm.ResourceTypeName.Should().BeEmpty();
        vm.VersionDisplay.Should().BeEmpty();
        vm.CommonVersionDisplay.Should().BeEmpty();
        vm.PriceDisplay.Should().BeEmpty();
        vm.NameHashDisplay.Should().BeEmpty();
        vm.DescriptionHashDisplay.Should().BeEmpty();
        vm.ThumbnailHashDisplay.Should().BeEmpty();
        vm.SummaryInfo.Should().BeEmpty();
        vm.Tags.Should().BeEmpty();
        vm.SellingPoints.Should().BeEmpty();
        vm.Colors.Should().BeEmpty();
        vm.ProductStyles.Should().BeEmpty();
    }

    [Fact]
    public void LoadResource_SetsResourceTypeName_ForCobj()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.ResourceTypeName.Should().Contain("COBJ");
    }

    [Fact]
    public void LoadResource_SetsVersionDisplay()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.VersionDisplay.Should().StartWith("0x");
    }

    [Fact]
    public void LoadResource_SetsCommonVersionDisplay()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.CommonVersionDisplay.Should().StartWith("0x");
    }

    [Fact]
    public void LoadResource_SetsPriceDisplay()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.PriceDisplay.Should().StartWith("$");
    }

    [Fact]
    public void LoadResource_SetsHashDisplays()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.NameHashDisplay.Should().StartWith("0x");
        vm.DescriptionHashDisplay.Should().StartWith("0x");
        vm.ThumbnailHashDisplay.Should().StartWith("0x");
    }

    [Fact]
    public void LoadResource_BuildsSummaryInfo()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.SummaryInfo.Should().NotBeEmpty();
        vm.SummaryInfo.Should().Contain("Resource Version");
        vm.SummaryInfo.Should().Contain("Price");
    }

    [Fact]
    public void LoadResource_BuildsAuralInfo()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.AuralInfo.Should().NotBeEmpty();
        vm.AuralInfo.Should().Contain("Materials Version");
    }

    [Fact]
    public void LoadResource_BuildsPlacementInfo()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act
        vm.LoadResource(resource);

        // Assert
        vm.PlacementInfo.Should().NotBeEmpty();
        vm.PlacementInfo.Should().Contain("Placement Flags");
    }

    [Fact]
    public void LoadResource_ClearsCollectionsOnReload()
    {
        // Arrange
        var vm = new CatalogViewerViewModel();
        var resource1 = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);
        var resource2 = new CobjResource(CobjKey, ReadOnlyMemory<byte>.Empty);

        // Act - load twice
        vm.LoadResource(resource1);
        int tagsCountAfterFirst = vm.Tags.Count;
        vm.LoadResource(resource2);

        // Assert - collections should be refreshed
        vm.Tags.Count.Should().Be(tagsCountAfterFirst);
    }

    #region CatalogTagViewModel Tests

    [Fact]
    public void CatalogTagViewModel_FormatsTagCorrectly()
    {
        // Act
        var tag = new CatalogTagViewModel(0, 0xDEADBEEF);

        // Assert
        tag.Index.Should().Be(0);
        tag.TagValue.Should().Be(0xDEADBEEF);
        tag.TagHex.Should().Be("0xDEADBEEF");
    }

    #endregion

    #region SellingPointViewModel Tests

    [Fact]
    public void SellingPointViewModel_FormatsCorrectly()
    {
        // Act
        var point = new SellingPointViewModel(1, 0x1234, 100);

        // Assert
        point.Index.Should().Be(1);
        point.Commodity.Should().Be(0x1234);
        point.CommodityHex.Should().Be("0x1234");
        point.Amount.Should().Be(100);
    }

    #endregion

    #region ColorViewModel Tests

    [Fact]
    public void ColorViewModel_FormatsArgbCorrectly()
    {
        // Act
        var color = new ColorViewModel(0, 0xFFFF0000); // Red with full alpha

        // Assert
        color.Index.Should().Be(0);
        color.Argb.Should().Be(0xFFFF0000);
        color.ArgbHex.Should().Be("#FFFF0000");
    }

    #endregion

    #region TgiReferenceViewModel Tests

    [Fact]
    public void TgiReferenceViewModel_FormatsCorrectly()
    {
        // Arrange - TgiReference(Instance: ulong, Type: uint, Group: uint)
        var tgi = new TgiReference(0x123456789ABCDEF0, 0x12345678, 0x87654321);

        // Act
        var vm = new TgiReferenceViewModel(0, tgi);

        // Assert
        vm.Index.Should().Be(0);
        vm.TypeHex.Should().Be("0x12345678");
        vm.GroupHex.Should().Be("0x87654321");
        vm.InstanceHex.Should().Be("0x123456789ABCDEF0");
    }

    #endregion
}
