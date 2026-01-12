using Avalonia;
using Avalonia.Controls;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Inline dropdown selector for TGI block indices.
/// Shows a ComboBox with formatted TGI block entries.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/TGIBlockSelection.cs
/// In legacy s4pe, this was a modal dialog with a ListBox showing TGI blocks.
/// The Avalonia version uses an inline ComboBox for better UX.
/// </remarks>
public partial class TgiBlockSelectorControl : UserControl
{
    /// <summary>
    /// Dependency property for the PropertyItem.
    /// </summary>
    public static readonly StyledProperty<PropertyItem?> PropertyItemProperty =
        AvaloniaProperty.Register<TgiBlockSelectorControl, PropertyItem?>(nameof(PropertyItem));

    /// <summary>
    /// Gets or sets the PropertyItem being edited.
    /// </summary>
    public PropertyItem? PropertyItem
    {
        get => GetValue(PropertyItemProperty);
        set => SetValue(PropertyItemProperty, value);
    }

    public TgiBlockSelectorControl()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateComboBoxItems();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PropertyItemProperty)
        {
            UpdateComboBoxItems();
        }
    }

    private void UpdateComboBoxItems()
    {
        TgiComboBox.ItemsSource = null;

        var item = DataContext as PropertyItem ?? PropertyItem;
        if (item?.TgiBlockList == null)
        {
            TgiComboBox.ItemsSource = new[] { "(no TGI list)" };
            TgiComboBox.IsEnabled = false;
            return;
        }

        if (item.TgiBlockList.Count == 0)
        {
            TgiComboBox.ItemsSource = new[] { "(empty TGI list)" };
            TgiComboBox.IsEnabled = false;
            return;
        }

        TgiComboBox.IsEnabled = true;
        var items = new List<string>();
        int index = 0;

        foreach (var tgiBlock in item.TgiBlockList)
        {
            var formatted = FormatTgiBlock(index, tgiBlock);
            items.Add(formatted);
            index++;
        }

        TgiComboBox.ItemsSource = items;
        TgiComboBox.SelectedIndex = item.TgiBlockIndex;
    }

    /// <summary>
    /// Formats a TGI block for display in the ComboBox.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/TGIBlockSelection.cs lines 87-98
    /// Format: [Index] TypeName (0xType:0xGroup:0xInstance)
    /// </remarks>
    private static string FormatTgiBlock(int index, object tgiBlock)
    {
        // Handle different TGI block types
        uint type = 0;
        uint group = 0;
        ulong instance = 0;

        // Use reflection to get properties - works with CaspTgiBlock, TgiReference, etc.
        var blockType = tgiBlock.GetType();

        var typeProp = blockType.GetProperty("Type") ?? blockType.GetProperty("ResourceType");
        var groupProp = blockType.GetProperty("Group") ?? blockType.GetProperty("ResourceGroup");
        var instanceProp = blockType.GetProperty("Instance");

        if (typeProp != null) type = Convert.ToUInt32(typeProp.GetValue(tgiBlock), CultureInfo.InvariantCulture);
        if (groupProp != null) group = Convert.ToUInt32(groupProp.GetValue(tgiBlock), CultureInfo.InvariantCulture);
        if (instanceProp != null) instance = Convert.ToUInt64(instanceProp.GetValue(tgiBlock), CultureInfo.InvariantCulture);

        var typeName = GetResourceTypeName(type);
        return $"[{index}] {typeName} (0x{type:X8}:0x{group:X8}:0x{instance:X16})";
    }

    /// <summary>
    /// Gets a human-readable name for a resource type.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResource.cs TypeToTag dictionary
    /// </remarks>
    private static string GetResourceTypeName(uint type)
    {
        // Common Sims 4 resource types
        // Source: Various s4pi wrapper files and Extensions.cs
        return type switch
        {
            0x00000000 => "NULL",
            0x00AE6C67 => "BONE",
            0x015A1849 => "GEOM",
            0x0166038C => "_XML",
            0x01661233 => "MODL",
            0x01D0E75D => "MTST",
            0x01D10F34 => "THUM",
            0x01EEF63A => "ICON",
            0x02019972 => "AUDT",
            0x025C90A6 => "BGEO",
            0x025ED6F4 => "SIMO",
            0x0333406C => "_IMG",
            0x033A1435 => "LOOT",
            0x034AEECB => "CASP",
            0x03B33DDF => "TXTC",
            0x0418FE2A => "CFEN",
            0x049CA4CD => "OBJK",
            0x04ED4BB2 => "CTPT",
            0x067CAA11 => "_KEY",
            0x0A36F07A => "SMAP",
            0x0B3683CB => "COBJ",
            0x0C772E27 => "BUFF",
            0x0E56A030 => "CLIP",
            0x220557DA => "STBL",
            0x25796DCA => "JAZZ",
            0x29A4EB7A => "LOCO",
            0x2F7D0004 => "CWAL",
            0x316C78F2 => "CFND",
            0x319E4F1D => "CSTR",
            0x3453CF95 => "RLES",
            0x36F47D56 => "DTXT",
            0x545AC67A => "TXTF",
            0x6B20C4F3 => "RSLT",
            0x736884F1 => "LITE",
            0x7FB6AD8A => "DMAP",
            0x8070223D => "BCON",
            0x91EDBD3E => "CRST",
            0xA8F7B517 => "A8F7",
            0xAC16FBEC => "GMLS",
            0xB52F5055 => "FTPT",
            0xB61DE6B4 => "SKIN",
            0xC0DB5AE7 => "OBJD",
            0xD3044521 => "MRST",
            0xD382BF57 => "SLOT",
            0xDEA2951C => "SPNX",
            0x71BDB8A2 => "STYL",
            _ => $"0x{type:X8}"
        };
    }
}
