using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Advanced filter panel for filtering resources by multiple criteria.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs
/// </remarks>
public partial class AdvancedFilterPanel : UserControl
{
    /// <summary>
    /// Event raised when the filter criteria change.
    /// </summary>
    public event EventHandler<FilterChangedEventArgs>? FilterChanged;

    public AdvancedFilterPanel()
    {
        InitializeComponent();

        // Wire up events
        ClearButton.Click += ClearButton_Click;
        PasteButton.Click += PasteButton_Click;

        TypeCheckBox.IsCheckedChanged += OnFilterChanged;
        GroupCheckBox.IsCheckedChanged += OnFilterChanged;
        InstanceCheckBox.IsCheckedChanged += OnFilterChanged;
        NameCheckBox.IsCheckedChanged += OnFilterChanged;
        RegexToggle.IsCheckedChanged += OnFilterChanged;

        TypeTextBox.TextChanged += OnFilterTextChanged;
        GroupTextBox.TextChanged += OnFilterTextChanged;
        InstanceTextBox.TextChanged += OnFilterTextChanged;
        NameTextBox.TextChanged += OnFilterTextChanged;
        QuickFilterTextBox.TextChanged += OnFilterTextChanged;
    }

    private void ClearButton_Click(object? sender, RoutedEventArgs e)
    {
        TypeCheckBox.IsChecked = false;
        GroupCheckBox.IsChecked = false;
        InstanceCheckBox.IsChecked = false;
        NameCheckBox.IsChecked = false;
        TypeTextBox.Text = "";
        GroupTextBox.Text = "";
        InstanceTextBox.Text = "";
        NameTextBox.Text = "";
        QuickFilterTextBox.Text = "";

        RaiseFilterChanged();
    }

    /// <summary>
    /// Handles paste button click to parse ResourceKey from clipboard.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2593-2601
    /// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 182-212
    /// Supports two formats:
    /// - 0x{X8}-0x{X8}-0x{X16} (e.g., "0x220557DA-0x00000000-0x1234567890ABCDEF")
    /// - {X8}:{X8}:{X16} or key:{X8}:{X8}:{X16}
    /// </remarks>
    private async void PasteButton_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var text = await clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text)) return;

        if (TryParseResourceKey(text, out var type, out var group, out var instance))
        {
            // Populate filter fields and enable checkboxes
            TypeTextBox.Text = type.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            TypeCheckBox.IsChecked = true;

            GroupTextBox.Text = group.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            GroupCheckBox.IsChecked = true;

            InstanceTextBox.Text = instance.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
            InstanceCheckBox.IsChecked = true;

            RaiseFilterChanged();
        }
    }

    /// <summary>
    /// Parses a ResourceKey string in TGI format.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs lines 182-212
    /// </remarks>
    private static bool TryParseResourceKey(string value, out uint type, out uint group, out ulong instance)
    {
        type = 0;
        group = 0;
        instance = 0;

        if (string.IsNullOrWhiteSpace(value)) return false;

        value = value.Trim();

        // Format 1: 0x{X8}-0x{X8}-0x{X16}
        if (value.Contains('-'))
        {
            var tgi = value.ToLowerInvariant().Split('-');
            if (tgi.Length != 3) return false;

            foreach (var x in tgi)
                if (!x.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return false;

            if (!uint.TryParse(tgi[0][2..], System.Globalization.NumberStyles.HexNumber, null, out type)) return false;
            if (!uint.TryParse(tgi[1][2..], System.Globalization.NumberStyles.HexNumber, null, out group)) return false;
            if (!ulong.TryParse(tgi[2][2..], System.Globalization.NumberStyles.HexNumber, null, out instance)) return false;

            return true;
        }

        // Format 2: {X8}:{X8}:{X16} or key:{X8}:{X8}:{X16}
        var parts = value.ToLowerInvariant().Split(':');
        if (parts.Length == 4 && parts[0] == "key")
        {
            // Shift array to remove "key:" prefix
            parts = [parts[1], parts[2], parts[3]];
        }
        else if (parts.Length != 3)
        {
            return false;
        }

        if (!uint.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out type)) return false;
        if (!uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out group)) return false;
        if (!ulong.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out instance)) return false;

        return true;
    }

    private void OnFilterChanged(object? sender, RoutedEventArgs e)
    {
        RaiseFilterChanged();
    }

    private void OnFilterTextChanged(object? sender, TextChangedEventArgs e)
    {
        RaiseFilterChanged();
    }

    private void RaiseFilterChanged()
    {
        var args = new FilterChangedEventArgs
        {
            UseRegex = RegexToggle.IsChecked == true,
            QuickFilter = QuickFilterTextBox.Text?.Trim() ?? "",
            FilterByType = TypeCheckBox.IsChecked == true,
            TypeFilter = TypeTextBox.Text?.Trim() ?? "",
            FilterByGroup = GroupCheckBox.IsChecked == true,
            GroupFilter = GroupTextBox.Text?.Trim() ?? "",
            FilterByInstance = InstanceCheckBox.IsChecked == true,
            InstanceFilter = InstanceTextBox.Text?.Trim() ?? "",
            FilterByName = NameCheckBox.IsChecked == true,
            NameFilter = NameTextBox.Text?.Trim() ?? ""
        };

        FilterChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Sets the quick filter text programmatically.
    /// </summary>
    public void SetQuickFilter(string filter)
    {
        QuickFilterTextBox.Text = filter;
    }

    /// <summary>
    /// Triggers paste from clipboard programmatically.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2593-2601
    /// Called by Ctrl+Shift+V keyboard shortcut.
    /// </remarks>
    public async Task PasteResourceKeyAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var text = await clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text)) return;

        if (TryParseResourceKey(text, out var type, out var group, out var instance))
        {
            // Populate filter fields and enable checkboxes
            TypeTextBox.Text = type.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            TypeCheckBox.IsChecked = true;

            GroupTextBox.Text = group.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            GroupCheckBox.IsChecked = true;

            InstanceTextBox.Text = instance.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
            InstanceCheckBox.IsChecked = true;

            RaiseFilterChanged();
        }
    }

    /// <summary>
    /// Gets the current quick filter text.
    /// </summary>
    public string QuickFilter => QuickFilterTextBox.Text ?? "";

    /// <summary>
    /// Applies the filter criteria to a resource item.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs lines 108-116
    /// </remarks>
    public bool MatchesFilter(ResourceItemViewModel resource)
    {
        var args = new FilterChangedEventArgs
        {
            UseRegex = RegexToggle.IsChecked == true,
            QuickFilter = QuickFilterTextBox.Text?.Trim() ?? "",
            FilterByType = TypeCheckBox.IsChecked == true,
            TypeFilter = TypeTextBox.Text?.Trim() ?? "",
            FilterByGroup = GroupCheckBox.IsChecked == true,
            GroupFilter = GroupTextBox.Text?.Trim() ?? "",
            FilterByInstance = InstanceCheckBox.IsChecked == true,
            InstanceFilter = InstanceTextBox.Text?.Trim() ?? "",
            FilterByName = NameCheckBox.IsChecked == true,
            NameFilter = NameTextBox.Text?.Trim() ?? ""
        };

        return args.Matches(resource);
    }
}

/// <summary>
/// Event args for filter changes.
/// </summary>
public class FilterChangedEventArgs : EventArgs
{
    public bool UseRegex { get; init; }
    public string QuickFilter { get; init; } = "";

    public bool FilterByType { get; init; }
    public string TypeFilter { get; init; } = "";

    public bool FilterByGroup { get; init; }
    public string GroupFilter { get; init; } = "";

    public bool FilterByInstance { get; init; }
    public string InstanceFilter { get; init; } = "";

    public bool FilterByName { get; init; }
    public string NameFilter { get; init; } = "";

    /// <summary>
    /// Tests if a resource matches all filter criteria.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs lines 104, 113
    /// </remarks>
    public bool Matches(ResourceItemViewModel resource)
    {
        // Quick filter - applies to type name, display key, or instance name
        if (!string.IsNullOrEmpty(QuickFilter))
        {
            if (UseRegex)
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(QuickFilter, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (!regex.IsMatch(resource.TypeName) &&
                        !regex.IsMatch(resource.DisplayKey) &&
                        !(resource.InstanceName != null && regex.IsMatch(resource.InstanceName)))
                        return false;
                }
                catch
                {
                    // Invalid regex - treat as literal
                    if (!MatchesText(resource.TypeName, QuickFilter) &&
                        !MatchesText(resource.DisplayKey, QuickFilter) &&
                        !(resource.InstanceName != null && MatchesText(resource.InstanceName, QuickFilter)))
                        return false;
                }
            }
            else
            {
                if (!MatchesText(resource.TypeName, QuickFilter) &&
                    !MatchesText(resource.DisplayKey, QuickFilter) &&
                    !(resource.InstanceName != null && MatchesText(resource.InstanceName, QuickFilter)))
                    return false;
            }
        }

        // Type filter
        if (FilterByType && !string.IsNullOrEmpty(TypeFilter))
        {
            var typeHex = resource.Key.ResourceType.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            if (!MatchesField(typeHex, TypeFilter))
                return false;
        }

        // Group filter
        if (FilterByGroup && !string.IsNullOrEmpty(GroupFilter))
        {
            var groupHex = resource.Key.ResourceGroup.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            if (!MatchesField(groupHex, GroupFilter))
                return false;
        }

        // Instance filter
        if (FilterByInstance && !string.IsNullOrEmpty(InstanceFilter))
        {
            var instanceHex = resource.Key.Instance.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
            if (!MatchesField(instanceHex, InstanceFilter))
                return false;
        }

        // Name filter
        if (FilterByName && !string.IsNullOrEmpty(NameFilter))
        {
            if (resource.InstanceName == null)
                return false;

            if (UseRegex)
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(NameFilter, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (!regex.IsMatch(resource.InstanceName))
                        return false;
                }
                catch
                {
                    if (!MatchesText(resource.InstanceName, NameFilter))
                        return false;
                }
            }
            else
            {
                if (!MatchesText(resource.InstanceName, NameFilter))
                    return false;
            }
        }

        return true;
    }

    private bool MatchesField(string value, string pattern)
    {
        if (UseRegex)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return regex.IsMatch(value);
            }
            catch
            {
                return MatchesText(value, pattern);
            }
        }
        return MatchesText(value, pattern);
    }

    private static bool MatchesText(string value, string pattern)
    {
        return value.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
