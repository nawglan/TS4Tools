using Avalonia.Controls;
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
