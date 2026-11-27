using Avalonia.Controls;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// Browser list, filter, package information and control panel behaviour of the main window.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs,
/// legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs,
/// legacy_references/Sims4Tools/s4pe/PackageInfo/PackageInfoWidget.cs and
/// legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs.
/// </remarks>
public partial class MainWindowViewModel
{
    private AppSettings? _shellSettings;
    private EventHandler? _filterChangedHandler;
    private bool _shellInitialised;

    /// <summary>
    /// The settings the shell reads and updates. Tests replace it to stay independent of the
    /// user's settings file; setting it re-reads the shell options.
    /// </summary>
    internal AppSettings ShellSettings
    {
        get => _shellSettings ?? SettingsService.Instance.Settings;
        set
        {
            _shellSettings = value;
            LoadShellSettings();
        }
    }

    /// <summary>Filter fields, filter switch and QBE/Set/Revise state.</summary>
    public ResourceFilterViewModel Filter { get; } = new();

    /// <summary>Package header fields.</summary>
    public PackageInfoViewModel PackageInfo { get; } = new();

    /// <summary>Raised when the shell options were re-read from the settings (Settings dialog).</summary>
    public event EventHandler? ShellSettingsReloaded;

    /// <summary>Whether clicking a column heading sorts the list (the control panel Sort checkbox).</summary>
    [ObservableProperty]
    private bool _sortEnabled = true;

    /// <summary>Index into <see cref="BrowserColumns.All"/> of the column the list is sorted on.</summary>
    [ObservableProperty]
    private int _sortColumn;

    /// <summary>The direction of the sort.</summary>
    [ObservableProperty]
    private BrowserSortOrder _sortOrder;

    /// <summary>Hex only: resources are loaded as raw data and shown in the hex viewer.</summary>
    [ObservableProperty]
    private bool _hexOnly;

    /// <summary>Whether the Name column is shown.</summary>
    [ObservableProperty]
    private bool _useNames = true;

    /// <summary>Whether the Tag column is shown.</summary>
    [ObservableProperty]
    private bool _useTags = true;

    /// <summary>What the preview panel shows on selection: 0 off, 1 hex, 2 preview.</summary>
    [ObservableProperty]
    private int _autoChoice = 2;

    /// <summary>Zoom factor of text previews (the PreviewZoomFactor setting).</summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2837-2846 (a RichTextBox preview gets
    /// ZoomFactor = PreviewZoomFactor and writes its zoom back whenever it changes).
    /// </remarks>
    [ObservableProperty]
    private double _previewZoom = 1.0;

    /// <summary>Smallest and largest preview zoom factors (RichTextBox.ZoomFactor limits).</summary>
    public const double MinPreviewZoom = 0.015625;
    public const double MaxPreviewZoom = 64.0;

    /// <summary>The zoom applied to the current preview: text and hex previews are zoomed, others are not.</summary>
    public double EffectivePreviewZoom => IsZoomablePreview ? PreviewZoom : 1.0;

    /// <summary>Whether the current preview can be zoomed.</summary>
    public bool CanZoomPreview => IsZoomablePreview;

    private bool IsZoomablePreview => IsTextBasedPreview || EditorContent is Views.Editors.HexViewerView;

    partial void OnEditorContentChanged(Control? value)
    {
        OnPropertyChanged(nameof(EffectivePreviewZoom));
        NotifyPreviewActionsChanged();
    }

    partial void OnSelectedResourceChanged(ResourceItemViewModel? value) => NotifyPreviewActionsChanged();

    partial void OnPreviewZoomChanged(double value)
    {
        ShellSettings.PreviewZoomFactor = (float)value;
        OnPropertyChanged(nameof(EffectivePreviewZoom));
    }

    /// <summary>Multiplies the preview zoom, keeping it within limits.</summary>
    public void ZoomPreview(double factor)
        => PreviewZoom = Math.Clamp(PreviewZoom * factor, MinPreviewZoom, MaxPreviewZoom);

    /// <summary>The number of resources the browser lists after filtering.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs line 186 (lbCount)</remarks>
    public int VisibleCount => FilteredResources.Count;

    /// <summary>Whether a selected resource is shown as hex regardless of its type.</summary>
    private bool ForceHexForSelection => ShellSettings.ForceHexView || HexOnly;

    private void InitializeShell()
    {
        _filterChangedHandler = (_, _) => ApplyFilter();
        Filter.FilterChanged += _filterChangedHandler;
        LoadShellSettings();
        _shellInitialised = true;
    }

    private void DisposeShell()
    {
        if (_filterChangedHandler != null)
        {
            Filter.FilterChanged -= _filterChangedHandler;
            _filterChangedHandler = null;
        }
    }

    /// <summary>
    /// Reads the control panel and sort options from the settings.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs lines 35-53</remarks>
    internal void LoadShellSettings()
    {
        var settings = ShellSettings;
        SortEnabled = settings.Sort;
        HexOnly = settings.HexOnly;
        UseNames = settings.UseNames;
        UseTags = settings.UseTags;
        AutoChoice = Math.Clamp(settings.AutoDisplayMode, 0, 2);
        PreviewZoom = settings.PreviewZoomFactor > 0
            ? Math.Clamp(settings.PreviewZoomFactor, MinPreviewZoom, MaxPreviewZoom)
            : 1.0;
        SortColumn = Math.Clamp(settings.ColumnToSort, 0, BrowserColumns.All.Count - 1);
        SortOrder = Enum.IsDefined((BrowserSortOrder)settings.SortOrder)
            ? (BrowserSortOrder)settings.SortOrder
            : BrowserSortOrder.None;

        if (_shellInitialised) ApplyFilter();
        OnPropertyChanged(nameof(HasTextEditor));
        NotifyPreviewActionsChanged();
        ShellSettingsReloaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Writes the sort state to the settings (saved when the window closes).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/SortingListviews.cs lines 55-59</remarks>
    internal void StoreSortSettings()
    {
        var settings = ShellSettings;
        settings.Sort = SortEnabled;
        settings.ColumnToSort = SortColumn;
        settings.SortOrder = (int)SortOrder;
    }

    /// <summary>
    /// Handles a click on a column heading: clicking the sorted column flips its direction,
    /// clicking another column sorts it ascending.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 899-935 (listView1_ColumnClick)</remarks>
    public void SortByColumn(int column)
    {
        if (column < 0 || column >= BrowserColumns.All.Count) return;

        if (column == SortColumn)
        {
            SortOrder = SortOrder == BrowserSortOrder.Ascending ? BrowserSortOrder.Descending : BrowserSortOrder.Ascending;
        }
        else
        {
            SortColumn = column;
            SortOrder = BrowserSortOrder.Ascending;
        }

        StoreSortSettings();
        ApplyFilter();
        StatusMessage = $"Sorted by {BrowserColumns.All[column]} ({SortOrder})";
    }

    /// <summary>The Sort checkbox: turns sorting of the list on or off.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2776-2789 (controlPanel1_SortChanged)</remarks>
    public void OnSortOptionChanged(bool sort)
    {
        if (SortEnabled == sort) return;
        SortEnabled = sort;
        StoreSortSettings();
        ApplyFilter();
        StatusMessage = sort ? "Sorting enabled" : "Sorting disabled";
    }

    /// <summary>The HexOnly checkbox: reloads the selected resource as raw data (or as its wrapper).</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2791-2800 (controlPanel1_HexOnlyChanged)</remarks>
    public void OnHexOnlyOptionChanged(bool hexOnly)
    {
        if (HexOnly == hexOnly) return;
        HexOnly = hexOnly;
        ShellSettings.HexOnly = hexOnly;
        StatusMessage = hexOnly ? "Hex only: resources are shown as raw data" : "Resources are shown with their viewers";
        _ = RunGuardedAsync(ReselectSelectedResourceAsync, "Error reloading resource");
    }

    /// <summary>The Names checkbox: shows or hides the Name column.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 300-331 (DisplayResourceNames)</remarks>
    public void OnUseNamesOptionChanged(bool useNames)
    {
        UseNames = useNames;
        ShellSettings.UseNames = useNames;
        StatusMessage = useNames ? "Showing resource names" : "Hiding resource names";
    }

    /// <summary>The Tags checkbox: shows or hides the Tag column.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 334-352 (DisplayResourceTags)</remarks>
    public void OnUseTagsOptionChanged(bool useTags)
    {
        UseTags = useTags;
        ShellSettings.UseTags = useTags;
        StatusMessage = useTags ? "Showing resource tags" : "Hiding resource tags";
    }

    /// <summary>The Auto radio buttons: what the preview panel shows for the selected resource.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2802-2850 (controlPanel1_AutoChanged)</remarks>
    public void OnAutoModeChanged(int autoChoice)
    {
        AutoChoice = Math.Clamp(autoChoice, 0, 2);
        ShellSettings.AutoDisplayMode = AutoChoice;
        StatusMessage = $"Auto mode: {AutoChoice switch { 1 => "Hex", 2 => "Preview", _ => "Off" }}";
        _ = RunGuardedAsync(RefreshAutoDisplayAsync, "Error updating preview");
    }

    /// <summary>
    /// Chooses what the preview panel shows for a loaded resource: nothing, its hex dump or the
    /// viewer built for it.
    /// </summary>
    private Control? ApplyAutoChoice(Control? preview, ReadOnlyMemory<byte> data) => AutoChoice switch
    {
        0 => null,
        1 => _viewFactory(CreateHexViewer(data)),
        _ => preview
    };

    private async Task RefreshAutoDisplayAsync()
    {
        if (_package == null || SelectedResource == null || _currentEditorView == null) return;
        if (!ReferenceEquals(_loadedResourceItem, SelectedResource)) return;

        var entry = ResolveEntry(SelectedResource);
        if (entry == null) return;

        var data = await _package.GetResourceDataAsync(entry);
        EditorContent = ApplyAutoChoice(_currentEditorView, data);
    }

    private async Task ReselectSelectedResourceAsync()
    {
        var item = SelectedResource;
        if (item == null) return;

        SelectedResource = null;
        await PendingSelectionTask;

        // A cancelled commit prompt puts the selection back; otherwise load the resource again.
        if (SelectedResource == null)
        {
            SelectedResource = item;
            await PendingSelectionTask;
        }
    }

    /// <summary>
    /// Fills <see cref="FilteredResources"/> with the resources that pass the filter, in sort order.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 622-695 (UpdateList),
    /// 978-1010 (FilteredList) and SortingListviews.cs lines 61-95 (Compare: case-insensitive text
    /// comparison of the sorted column, reversed for a descending sort).
    /// </remarks>
    internal void RebuildBrowserList()
    {
        RefreshEntryColumns();
        PackageInfo.Update(_package);

        IEnumerable<ResourceItemViewModel> items = Resources;
        if (Filter.IsEnabled)
        {
            items = items.Where(Filter.Matches);
        }

        if (SortEnabled && SortOrder != BrowserSortOrder.None)
        {
            var column = BrowserColumns.All[SortColumn];
            items = SortOrder == BrowserSortOrder.Ascending
                ? items.OrderBy(r => r.GetColumnText(column), StringComparer.OrdinalIgnoreCase)
                : items.OrderByDescending(r => r.GetColumnText(column), StringComparer.OrdinalIgnoreCase);
        }

        var result = items.ToList();
        var selected = SelectedResources.Where(result.Contains).ToList();
        var primary = SelectedResource;
        var primaryRemains = primary != null && result.Contains(primary);

        // Rebuilding empties the list control, which would clear the selection; keep it away from
        // the selection handling and put it back afterwards (legacy re-selects the selected resource).
        var wasSuppressed = _suppressSelectionHandling;
        _suppressSelectionHandling = true;
        try
        {
            FilteredResources.Clear();
            foreach (var item in result)
            {
                FilteredResources.Add(item);
            }

            if (primary != null && !primaryRemains)
            {
                SelectedResource = null;
            }
            else if (primary != null)
            {
                SelectedResource = primary;
            }

            SelectedResources.Clear();
            foreach (var item in selected)
            {
                SelectedResources.Add(item);
            }
        }
        finally
        {
            _suppressSelectionHandling = wasSuppressed;
        }

        if (primary != null && !primaryRemains && !wasSuppressed)
        {
            _pendingSelectionTask = RunGuardedAsync(HandleSelectedResourceChangedAsync, "Error loading resource");
        }

        OnPropertyChanged(nameof(VisibleCount));
    }

    /// <summary>Copies the index entry fields shown in the columns into the list items.</summary>
    private void RefreshEntryColumns()
    {
        if (_package == null) return;

        // Each row carries its own index entry, so duplicate keys show their own columns.
        foreach (var item in Resources)
        {
            if (ResolveEntry(item) is { } entry)
            {
                item.ApplyEntryData(entry);
            }
        }
    }

    /// <summary>Applies the entered filter values.</summary>
    [RelayCommand]
    private void FilterSet() => Filter.Set();

    /// <summary>Copies the applied filter back into the entry boxes.</summary>
    [RelayCommand]
    private void FilterRevise() => Filter.Revise();

    /// <summary>Fills the filter from the selected resource (query by example).</summary>
    [RelayCommand]
    private void FilterQbe()
    {
        if (SelectedResource != null) Filter.QueryByExample(SelectedResource);
    }

    /// <summary>Fills the filter's Type, Group and Instance from a resource key on the clipboard.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2593-2601 (FilterPaste)</remarks>
    [RelayCommand]
    private async Task FilterPasteKeyAsync()
    {
        var clipboard = GetTopLevel()?.Clipboard;
        if (clipboard == null) return;

        var text = await clipboard.TryGetTextAsync();
        if (ResourceKey.TryParse(text, out var key))
        {
            Filter.PasteResourceKey(key);
        }
    }
}
