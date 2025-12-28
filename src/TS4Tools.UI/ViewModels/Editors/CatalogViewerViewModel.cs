using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.Wrappers.CatalogResource;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// View model for the catalog resource viewer.
/// </summary>
public partial class CatalogViewerViewModel : ViewModelBase
{
    private AbstractCatalogResource? _resource;

    [ObservableProperty]
    private string _resourceTypeName = string.Empty;

    [ObservableProperty]
    private string _versionDisplay = string.Empty;

    [ObservableProperty]
    private string _commonVersionDisplay = string.Empty;

    [ObservableProperty]
    private string _priceDisplay = string.Empty;

    [ObservableProperty]
    private string _nameHashDisplay = string.Empty;

    [ObservableProperty]
    private string _descriptionHashDisplay = string.Empty;

    [ObservableProperty]
    private string _thumbnailHashDisplay = string.Empty;

    [ObservableProperty]
    private string _summaryInfo = string.Empty;

    [ObservableProperty]
    private string _auralInfo = string.Empty;

    [ObservableProperty]
    private string _placementInfo = string.Empty;

    /// <summary>
    /// The catalog tags.
    /// </summary>
    public ObservableCollection<CatalogTagViewModel> Tags { get; } = [];

    /// <summary>
    /// The selling points.
    /// </summary>
    public ObservableCollection<SellingPointViewModel> SellingPoints { get; } = [];

    /// <summary>
    /// The colors.
    /// </summary>
    public ObservableCollection<ColorViewModel> Colors { get; } = [];

    /// <summary>
    /// The product style TGI references.
    /// </summary>
    public ObservableCollection<TgiReferenceViewModel> ProductStyles { get; } = [];

    /// <summary>
    /// Loads a catalog resource for display.
    /// </summary>
    public void LoadResource(AbstractCatalogResource resource)
    {
        _resource = resource;

        ResourceTypeName = resource switch
        {
            CobjResource => "Catalog Object (COBJ)",
            _ => "Catalog Resource"
        };

        VersionDisplay = $"0x{resource.Version:X8}";
        CommonVersionDisplay = $"0x{resource.CommonBlock.CommonBlockVersion:X2}";
        PriceDisplay = $"${resource.CommonBlock.Price:N0}";
        NameHashDisplay = $"0x{resource.CommonBlock.NameHash:X8}";
        DescriptionHashDisplay = $"0x{resource.CommonBlock.DescriptionHash:X8}";
        ThumbnailHashDisplay = $"0x{resource.CommonBlock.ThumbnailHash:X16}";

        BuildSummaryInfo(resource);
        BuildAuralInfo(resource);
        BuildPlacementInfo(resource);
        LoadTags(resource);
        LoadSellingPoints(resource);
        LoadColors(resource);
        LoadProductStyles(resource);
    }

    private void BuildSummaryInfo(AbstractCatalogResource resource)
    {
        var sb = new StringBuilder();
        var common = resource.CommonBlock;

        sb.AppendLine(CultureInfo.InvariantCulture, $"Resource Version: {VersionDisplay}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Common Block Version: {CommonVersionDisplay}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Price: {PriceDisplay}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Name Hash: {NameHashDisplay}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Description Hash: {DescriptionHashDisplay}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Thumbnail: {ThumbnailHashDisplay}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Dev Category Flags: 0x{common.DevCategoryFlags:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Unlock By: 0x{common.UnlockByHash:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Unlocked By: 0x{common.UnlockedByHash:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Swatch Sort Priority: 0x{common.SwatchColorsSortPriority:X4}");

        if (common.CommonBlockVersion >= 10)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Pack ID: {common.PackId}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Pack Options: {common.PackOptions}");
        }

        SummaryInfo = sb.ToString();
    }

    private void BuildAuralInfo(AbstractCatalogResource resource)
    {
        var sb = new StringBuilder();

        sb.AppendLine(CultureInfo.InvariantCulture, $"Materials Version: 0x{resource.AuralMaterialsVersion:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Material 1: 0x{resource.AuralMaterials1:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Material 2: 0x{resource.AuralMaterials2:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Material 3: 0x{resource.AuralMaterials3:X8}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Properties Version: 0x{resource.AuralPropertiesVersion:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Quality: 0x{resource.AuralQuality:X8}");

        if (resource.AuralPropertiesVersion > 1)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Ambient Object: 0x{resource.AuralAmbientObject:X8}");
        }

        if (resource.AuralPropertiesVersion == 3)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Ambience File: 0x{resource.AmbienceFileInstanceId:X16}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Override Ambience: {resource.IsOverrideAmbience}");
        }

        AuralInfo = sb.ToString();
    }

    private void BuildPlacementInfo(AbstractCatalogResource resource)
    {
        var sb = new StringBuilder();

        sb.AppendLine(CultureInfo.InvariantCulture, $"Placement Flags High: 0x{resource.PlacementFlagsHigh:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Placement Flags Low: 0x{resource.PlacementFlagsLow:X8}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Slot Type Set: 0x{resource.SlotTypeSet:X16}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Slot Deco Size: {resource.SlotDecoSize}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Catalog Group: 0x{resource.CatalogGroup:X16}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"State Usage: 0x{resource.StateUsage:X2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Fence Height: {resource.FenceHeight}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Is Stackable: {resource.IsStackable}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Can Depreciate: {resource.CanItemDepreciate}");

        if (resource.Version >= 0x19)
        {
            var fallback = resource.FallbackObjectKey;
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Fallback Object: {fallback}");
        }

        PlacementInfo = sb.ToString();
    }

    private void LoadTags(AbstractCatalogResource resource)
    {
        Tags.Clear();
        for (int i = 0; i < resource.CommonBlock.Tags.Count; i++)
        {
            Tags.Add(new CatalogTagViewModel(i, resource.CommonBlock.Tags[i]));
        }
    }

    private void LoadSellingPoints(AbstractCatalogResource resource)
    {
        SellingPoints.Clear();
        for (int i = 0; i < resource.CommonBlock.SellingPoints.Count; i++)
        {
            var point = resource.CommonBlock.SellingPoints.Points[i];
            SellingPoints.Add(new SellingPointViewModel(i, point.CommodityTag, point.Amount));
        }
    }

    private void LoadColors(AbstractCatalogResource resource)
    {
        Colors.Clear();
        for (int i = 0; i < resource.Colors.Count; i++)
        {
            Colors.Add(new ColorViewModel(i, resource.Colors[i]));
        }
    }

    private void LoadProductStyles(AbstractCatalogResource resource)
    {
        ProductStyles.Clear();
        for (int i = 0; i < resource.CommonBlock.ProductStyles.Count; i++)
        {
            var tgi = resource.CommonBlock.ProductStyles[i];
            ProductStyles.Add(new TgiReferenceViewModel(i, tgi));
        }
    }
}

/// <summary>
/// View model for a catalog tag.
/// </summary>
public partial class CatalogTagViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _tagHex = string.Empty;

    [ObservableProperty]
    private uint _tagValue;

    public CatalogTagViewModel(int index, uint tag)
    {
        Index = index;
        TagValue = tag;
        TagHex = $"0x{tag:X8}";
    }
}

/// <summary>
/// View model for a selling point.
/// </summary>
public partial class SellingPointViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _commodityHex = string.Empty;

    [ObservableProperty]
    private ushort _commodity;

    [ObservableProperty]
    private int _amount;

    public SellingPointViewModel(int index, ushort commodity, int amount)
    {
        Index = index;
        Commodity = commodity;
        CommodityHex = $"0x{commodity:X4}";
        Amount = amount;
    }
}

/// <summary>
/// View model for a color.
/// </summary>
public partial class ColorViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _argbHex = string.Empty;

    [ObservableProperty]
    private uint _argb;

    public ColorViewModel(int index, uint argb)
    {
        Index = index;
        Argb = argb;
        ArgbHex = $"#{argb:X8}";
    }
}

/// <summary>
/// View model for a TGI reference.
/// </summary>
public partial class TgiReferenceViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string _typeHex = string.Empty;

    [ObservableProperty]
    private string _groupHex = string.Empty;

    [ObservableProperty]
    private string _instanceHex = string.Empty;

    public TgiReferenceViewModel(int index, TgiReference tgi)
    {
        Index = index;
        TypeHex = $"0x{tgi.Type:X8}";
        GroupHex = $"0x{tgi.Group:X8}";
        InstanceHex = $"0x{tgi.Instance:X16}";
    }
}
