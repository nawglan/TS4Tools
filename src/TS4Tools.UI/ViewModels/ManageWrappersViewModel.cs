// Source: legacy_references/Sims4Tools/s4pe/Settings/ManageWrappersDialog.cs lines 33-185 (Manage Wrappers dialog)

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Compatibility;
using TS4Tools.Extensions;
using TS4Tools.Resources;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// View model for the Manage Wrappers dialog: lists every (resource type, wrapper) claim in the registry's
/// <see cref="ResourceHandlerRegistry.TypeMap"/> and lets the user disable or re-enable them.
/// </summary>
/// <remarks>
/// <para>Legacy shows two list views, "Wrappers" (enabled, from <c>WrapperDealer.TypeMap</c>) and "Disabled", with
/// Enable/Disable buttons that move the selected rows between them (lines 45-48, 96-157). OK replaces
/// <c>WrapperDealer.Disabled</c> (lines 140-145); Cancel discards. Columns are order, tag, type, wrapper, file and the
/// assembly title/description/company/product (lines 50-75).</para>
/// <para>Here one list carries an IsDisabled flag per row (filterable by text and by disabled state); the
/// Disable/Enable commands act on the selected row like legacy's buttons, and <see cref="Apply"/> is OK.</para>
/// </remarks>
public partial class ManageWrappersViewModel : ViewModelBase
{
    private readonly WrapperRegistryService _service;
    private readonly List<WrapperEntryViewModel> _all = [];

    /// <summary>All rows matching the current filter.</summary>
    public ObservableCollection<WrapperEntryViewModel> Wrappers { get; } = [];

    /// <summary>Plugin load problems, one line each.</summary>
    public ObservableCollection<string> LoadErrors { get; } = [];

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _showDisabledOnly;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DisableSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(EnableSelectedCommand))]
    private WrapperEntryViewModel? _selectedWrapper;

    [ObservableProperty]
    private string _summary = string.Empty;

    public bool HasLoadErrors => LoadErrors.Count > 0;

    public ManageWrappersViewModel()
        : this(WrapperRegistryService.Instance)
    {
    }

    public ManageWrappersViewModel(WrapperRegistryService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Load();
    }

    /// <summary>Every row, ignoring the filter.</summary>
    public IReadOnlyList<WrapperEntryViewModel> AllWrappers => _all;

    /// <summary>
    /// Rebuilds the rows from the registry.
    /// </summary>
    /// <remarks>Source: ManageWrappersDialog.cs lines 45-75 (PopulateWrappers / PopulateDisable / PopulateListView).</remarks>
    private void Load()
    {
        _all.Clear();
        var registry = _service.Registry;
        var disabled = registry.Disabled.ToHashSet();
        var map = registry.TypeMap;

        // Legacy numbers rows in TypeMap order; the first enabled claim per type is the one used.
        var activeByType = new Dictionary<uint, Type>();
        int order = 0;
        foreach (var kvp in map)
        {
            order++;
            bool isDisabled = disabled.Contains(kvp);
            if (!isDisabled && !activeByType.ContainsKey(kvp.Key))
                activeByType[kvp.Key] = kvp.Value;

            // A bridged legacy wrapper is listed under its own type and assembly, not the adapter factory's.
            var displayType = kvp.Value == typeof(LegacyResourceFactory)
                && _service.LegacyWrappers.TryGetValue(kvp.Key, out var legacy) ? legacy : null;
            _all.Add(new WrapperEntryViewModel(order, kvp.Key, kvp.Value, isDisabled, displayType));
        }

        foreach (var entry in _all)
        {
            entry.IsActive = !entry.IsDisabled
                && activeByType.TryGetValue(entry.ResourceType, out var t) && t == entry.FactoryType;
        }

        LoadErrors.Clear();
        foreach (var error in _service.LoadErrors)
        {
            LoadErrors.Add(error.TypeName is null
                ? $"{Path.GetFileName(error.Path)}: {error.Message}"
                : $"{Path.GetFileName(error.Path)} ({error.TypeName}): {error.Message}");
        }
        foreach (var entry in _service.UnmatchedDisabledEntries)
        {
            LoadErrors.Add($"Disabled entry matches no wrapper: {entry}");
        }
        OnPropertyChanged(nameof(HasLoadErrors));

        ApplyFilter();
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    partial void OnShowDisabledOnlyChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        Wrappers.Clear();
        var filter = FilterText.Trim();
        foreach (var entry in _all)
        {
            if (ShowDisabledOnly && !entry.IsDisabled) continue;
            if (filter.Length > 0
                && !entry.TypeDisplay.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !entry.Tag.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !entry.WrapperName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !entry.FileName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            Wrappers.Add(entry);
        }

        Summary = $"{_all.Count} wrapper claims, {_all.Count(e => e.IsDisabled)} disabled";
    }

    private bool CanDisableSelected() => SelectedWrapper is { IsDisabled: false };

    private bool CanEnableSelected() => SelectedWrapper is { IsDisabled: true };

    /// <summary>Source: ManageWrappersDialog.cs lines 156-159 (btnDisable_Click).</summary>
    [RelayCommand(CanExecute = nameof(CanDisableSelected))]
    private void DisableSelected()
    {
        if (SelectedWrapper == null) return;
        SelectedWrapper.IsDisabled = true;
        RefreshCommandState();
    }

    /// <summary>Source: ManageWrappersDialog.cs lines 151-154 (btnEnable_Click).</summary>
    [RelayCommand(CanExecute = nameof(CanEnableSelected))]
    private void EnableSelected()
    {
        if (SelectedWrapper == null) return;
        SelectedWrapper.IsDisabled = false;
        RefreshCommandState();
    }

    /// <summary>Re-enables every wrapper.</summary>
    [RelayCommand]
    private void EnableAll()
    {
        foreach (var entry in _all)
            entry.IsDisabled = false;
        RefreshCommandState();
    }

    private void RefreshCommandState()
    {
        DisableSelectedCommand.NotifyCanExecuteChanged();
        EnableSelectedCommand.NotifyCanExecuteChanged();
        Summary = $"{_all.Count} wrapper claims, {_all.Count(e => e.IsDisabled)} disabled";
    }

    /// <summary>
    /// Applies the disabled set to the live registry and settings, then rebuilds the rows.
    /// </summary>
    /// <param name="settings">Settings whose DisabledWrappers receives the new list (the caller saves).</param>
    /// <remarks>Source: ManageWrappersDialog.cs lines 140-145 (btnOK_Click).</remarks>
    public void Apply(AppSettings settings)
    {
        _service.SetDisabled(
            _all.Where(e => e.IsDisabled).Select(e => new KeyValuePair<uint, Type>(e.ResourceType, e.FactoryType)),
            settings);
        Load();
    }
}

/// <summary>
/// One (resource type, wrapper) claim in the Manage Wrappers list.
/// </summary>
/// <remarks>Source: ManageWrappersDialog.cs lines 58-70 (list view columns).</remarks>
public partial class WrapperEntryViewModel : ObservableObject
{
    /// <param name="order">Row number.</param>
    /// <param name="resourceType">The claimed resource type.</param>
    /// <param name="factoryType">The registry factory type (what is disabled and persisted).</param>
    /// <param name="isDisabled">Whether the claim is disabled.</param>
    /// <param name="displayType">The wrapper type to show, when it differs from the factory (bridged legacy wrappers).</param>
    public WrapperEntryViewModel(int order, uint resourceType, Type factoryType, bool isDisabled, Type? displayType = null)
    {
        ArgumentNullException.ThrowIfNull(factoryType);
        Order = order;
        ResourceType = resourceType;
        FactoryType = factoryType;
        DisplayType = displayType ?? factoryType;
        _isDisabled = isDisabled;

        Tag = ExtList.IsKnown(resourceType) ? ExtList.GetTag(resourceType) : string.Empty;
        var assembly = DisplayType.Assembly;
        FileName = string.IsNullOrEmpty(assembly.Location) ? assembly.GetName().Name ?? "?" : Path.GetFileName(assembly.Location);
        Title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "(not set)";
        Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "(not set)";
        Company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "(not set)";
        Product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "(not set)";
    }

    public int Order { get; }

    public uint ResourceType { get; }

    public Type FactoryType { get; }

    /// <summary>The wrapper type shown (the legacy wrapper for a bridged legacy plugin, else the factory).</summary>
    public Type DisplayType { get; }

    public string TypeDisplay => $"0x{ResourceType:X8}";

    public string Tag { get; }

    public string WrapperName => DisplayType.Name;

    public string WrapperFullName => DisplayType.FullName ?? DisplayType.Name;

    public string FileName { get; }

    public string Title { get; }

    public string Description { get; }

    public string Company { get; }

    public string Product { get; }

    /// <summary>The persisted form of this entry.</summary>
    public string PersistedKey => ResourceHandlerRegistry.FormatDisabledWrapper(ResourceType, FactoryType);

    [ObservableProperty]
    private bool _isDisabled;

    /// <summary>Whether this claim currently wins for its resource type.</summary>
    [ObservableProperty]
    private bool _isActive;
}
