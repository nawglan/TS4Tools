using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.UI.Views.Controls;
using TS4Tools.UI.Views.Dialogs;
using TS4Tools.UI.Views.Editors;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CatalogResource;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// Sort modes for the resource list.
/// </summary>
public enum ResourceSortMode
{
    TypeName,
    Instance,
    Size
}

public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private bool _disposed;
    private readonly HashNameService _hashNameService = new();
    private readonly PropertyChangedEventHandler _propertyChangedHandler;
    private static readonly FilePickerFileType PackageFileType = new("Sims 4 Package") { Patterns = ["*.package"] };
    private static readonly FilePickerFileType BinaryFileType = new("Binary File") { Patterns = ["*.bin", "*.dat"] };
    private static readonly FilePickerFileType AllFilesType = new("All Files") { Patterns = ["*"] };

    private static readonly string RecentFilesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TS4Tools", "recent.json");

    private const int MaxRecentFiles = 10;

    private DbpfPackage? _package;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOpenPackage))]
    [NotifyPropertyChangedFor(nameof(ResourceCount))]
    private string _packagePath = string.Empty;

    [ObservableProperty]
    private string _title = "TS4Tools";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedResource))]
    private ResourceItemViewModel? _selectedResource;

    [ObservableProperty]
    private bool _showAdvancedFilter;

    // Current advanced filter criteria
    private FilterChangedEventArgs? _advancedFilterArgs;

    public bool HasSelectedResource => SelectedResource != null;

    [ObservableProperty]
    private string _selectedResourceTypeName = string.Empty;

    [ObservableProperty]
    private string _selectedResourceKey = string.Empty;

    [ObservableProperty]
    private string _selectedResourceInfo = string.Empty;

    [ObservableProperty]
    private Control? _editorContent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PropertyGridButtonText))]
    private bool _showPropertyGrid;

    [ObservableProperty]
    private Control? _propertyGridContent;

    /// <summary>
    /// The current resource wrapper for property grid display.
    /// </summary>
    private object? _currentResourceWrapper;

    public string PropertyGridButtonText => ShowPropertyGrid ? "Hide Properties" : "Show Properties";

    public bool HasOpenPackage => _package != null;

    public int ResourceCount => _package?.ResourceCount ?? 0;

    public bool HasClipboardResource => _clipboardData != null;

    // Internal clipboard for resource copy/paste
    // Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 42-43, 96-100
    private ClipboardResourceData? _clipboardData;

    /// <summary>
    /// Available helpers for the currently selected resource.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<HelperMenuItemViewModel> _availableHelpers = [];

    public bool HasHelpers => AvailableHelpers.Count > 0;

    /// <summary>
    /// Bookmarked package files for quick access.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs
    /// Format: "0:path" for read-only, "1:path" for read-write
    /// </remarks>
    public ObservableCollection<BookmarkItemViewModel> Bookmarks { get; } = [];

    public bool HasBookmarks => Bookmarks.Count > 0;

    public ObservableCollection<ResourceItemViewModel> Resources { get; } = [];

    public ObservableCollection<ResourceItemViewModel> FilteredResources { get; } = [];

    public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = [];

    [ObservableProperty]
    private ResourceSortMode _sortMode = ResourceSortMode.TypeName;

    public MainWindowViewModel()
    {
        _propertyChangedHandler = (_, e) =>
        {
            if (e.PropertyName == nameof(FilterText) || e.PropertyName == nameof(SortMode))
            {
                ApplyFilter();
            }
            else if (e.PropertyName == nameof(SelectedResource))
            {
                ExecuteAsync(UpdateSelectedResourceDetailsAsync, "Error loading resource");
            }
        };
        PropertyChanged += _propertyChangedHandler;

        LoadRecentFiles();
        LoadBookmarks();
    }

    /// <summary>
    /// Creates a new empty package.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 787-792 (FileNew method)
    /// </remarks>
    [RelayCommand]
    private async Task NewPackageAsync()
    {
        await CloseCurrentPackageAsync();

        _package = DbpfPackage.CreateNew();
        PackagePath = "";
        Title = "TS4Tools - [New Package]";

        _hashNameService.Clear();
        PopulateResourceList();

        StatusMessage = "New package created";
        OnPropertyChanged(nameof(HasOpenPackage));
        OnPropertyChanged(nameof(ResourceCount));
    }

    [RelayCommand]
    private async Task OpenPackageAsync()
    {
        var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null);

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Package",
            AllowMultiple = false,
            FileTypeFilter = [PackageFileType, AllFilesType]
        });

        if (files.Count == 1)
        {
            var path = files[0].Path.LocalPath;
            await LoadPackageAsync(path);
        }
    }

    private async Task LoadPackageAsync(string path)
    {
        try
        {
            StatusMessage = $"Loading {System.IO.Path.GetFileName(path)}...";

            await CloseCurrentPackageAsync();

            _package = await DbpfPackage.OpenAsync(path);
            PackagePath = path;
            Title = $"TS4Tools - {System.IO.Path.GetFileName(path)}";

            // Load hash-to-name mappings from NameMap resources
            await _hashNameService.LoadFromPackageAsync(_package);

            PopulateResourceList();
            AddToRecentFiles(path);

            var nameCount = _hashNameService.Count;
            StatusMessage = nameCount > 0
                ? $"Loaded {ResourceCount} resources ({nameCount} named)"
                : $"Loaded {ResourceCount} resources";
            OnPropertyChanged(nameof(HasOpenPackage));
            OnPropertyChanged(nameof(ResourceCount));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void PopulateResourceList()
    {
        Resources.Clear();
        FilteredResources.Clear();

        if (_package == null) return;

        foreach (var entry in _package.Resources)
        {
            var instanceName = _hashNameService.TryGetName(entry.Key.Instance);
            var item = new ResourceItemViewModel(entry.Key, entry.FileSize, instanceName);
            Resources.Add(item);
            FilteredResources.Add(item);
        }
    }

    private void ApplyFilter()
    {
        FilteredResources.Clear();

        IEnumerable<ResourceItemViewModel> filtered;

        // Use advanced filter if active, otherwise use simple filter
        if (ShowAdvancedFilter && _advancedFilterArgs != null)
        {
            filtered = Resources.Where(r => _advancedFilterArgs.Matches(r));
        }
        else
        {
            var filter = FilterText.Trim().ToLowerInvariant();
            filtered = string.IsNullOrEmpty(filter)
                ? Resources.AsEnumerable()
                : Resources.Where(r =>
                    r.TypeName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    r.DisplayKey.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (r.InstanceName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Apply sorting
        filtered = SortMode switch
        {
            ResourceSortMode.TypeName => filtered.OrderBy(r => r.TypeName).ThenBy(r => r.Key.Instance),
            ResourceSortMode.Instance => filtered.OrderBy(r => r.Key.Instance),
            ResourceSortMode.Size => filtered.OrderByDescending(r => r.FileSize),
            _ => filtered
        };

        foreach (var item in filtered)
        {
            FilteredResources.Add(item);
        }
    }

    /// <summary>
    /// Applies advanced filter criteria from the filter panel.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs
    /// </remarks>
    public void ApplyAdvancedFilter(FilterChangedEventArgs args)
    {
        _advancedFilterArgs = args;
        ApplyFilter();
    }

    private async Task UpdateSelectedResourceDetailsAsync()
    {
        if (SelectedResource == null || _package == null)
        {
            SelectedResourceTypeName = string.Empty;
            SelectedResourceKey = string.Empty;
            SelectedResourceInfo = string.Empty;
            EditorContent = null;
            _currentResourceWrapper = null;
            PropertyGridContent = null;
            return;
        }

        var key = SelectedResource.Key;
        SelectedResourceTypeName = SelectedResource.TypeName;
        SelectedResourceKey = SelectedResource.DisplayKey;

        var entry = _package.Find(key);
        if (entry != null)
        {
            var data = await _package.GetResourceDataAsync(entry);
            var info = $"Type: 0x{key.ResourceType:X8}\n";
            info += $"Group: 0x{key.ResourceGroup:X8}\n";
            info += $"Instance: 0x{key.Instance:X16}\n";
            info += $"\nData Size: {data.Length:N0} bytes\n";
            info += $"Compressed: {entry.IsCompressed}";

            SelectedResourceInfo = info;

            // Create appropriate editor based on resource type and track wrapper for property grid
            (EditorContent, _currentResourceWrapper) = key.ResourceType switch
            {
                0x220557DA => CreateStblEditorWithWrapper(key, data), // STBL
                0x0166038C => CreateNameMapEditorWithWrapper(key, data), // NameMap
                0x03B33DDF or 0x6017E896 => CreateTextEditorWithWrapper(key, data), // Tuning XML
                0x00B00000 or 0x00B2D882 => CreateImageViewerWithWrapper(key, data), // PNG, DDS
                0x545AC67A => CreateSimDataViewerWithWrapper(key, data), // SimData
                // RCOL resource types (standalone)
                RcolConstants.Modl or        // 0x01661233 - MODL
                RcolConstants.Matd or        // 0x01D0E75D - MATD
                RcolConstants.Mlod or        // 0x01D10F34 - MLOD
                RcolConstants.Mtst or        // 0x02019972 - MTST
                RcolConstants.Tree or        // 0x021D7E8C - TREE
                RcolConstants.TkMk or        // 0x033260E3 - TkMk
                RcolConstants.SlotAdjusts or // 0x0355E0A6 - Slot Adjusts
                RcolConstants.Lite or        // 0x03B4C61D - LITE
                RcolConstants.Anim or        // 0x63A33EA7 - ANIM
                RcolConstants.Vpxy or        // 0x736884F1 - VPXY
                RcolConstants.Rslt or        // 0xD3044521 - RSLT
                RcolConstants.Ftpt           // 0xD382BF57 - FTPT
                    => CreateRcolViewerWithWrapper(key, data),
                0x319E4F1D => CreateCatalogViewerWithWrapper(key, data), // COBJ (Catalog Object)
                _ => (CreateHexViewer(data), null) // Default hex viewer, no wrapper
            };

            // Update property grid if visible
            if (ShowPropertyGrid)
            {
                UpdatePropertyGrid(_currentResourceWrapper);
            }

            // Update available helpers
            UpdateAvailableHelpers(key);
        }
    }

    /// <summary>
    /// Updates the list of available helpers for the given resource key.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3095-3134 (SetHelpers)
    /// </remarks>
    private void UpdateAvailableHelpers(ResourceKey key)
    {
        AvailableHelpers.Clear();

        var wrapperName = _currentResourceWrapper?.GetType().Name;
        var helpers = HelperManager.Instance.GetHelpersForResource(
            key.ResourceType,
            key.ResourceGroup,
            key.Instance,
            wrapperName);

        // Filter out disabled helpers
        var settings = SettingsService.Instance.Settings;
        var enabledHelpers = helpers.Where(h => !settings.DisabledHelpers.Contains(h.Id));

        foreach (var helper in enabledHelpers)
        {
            AvailableHelpers.Add(new HelperMenuItemViewModel(helper, ExecuteHelperAsync));
        }

        OnPropertyChanged(nameof(HasHelpers));
    }

    /// <summary>
    /// Executes a helper program for the currently selected resource.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3147-3200 (do_HelperClick)
    /// </remarks>
    private async Task ExecuteHelperAsync(HelperInstance helper)
    {
        if (_package == null || SelectedResource == null) return;

        var key = SelectedResource.Key;
        var entry = _package.Find(key);
        if (entry == null)
        {
            StatusMessage = "Resource not found";
            return;
        }

        StatusMessage = $"Running {helper.Label}...";

        try
        {
            var data = await _package.GetResourceDataAsync(entry);
            var result = await helper.ExecuteAsync(data, SelectedResource.InstanceName);

            if (!result.Success)
            {
                StatusMessage = $"Helper error: {result.ErrorMessage}";
                return;
            }

            if (result.ModifiedData != null)
            {
                // Update the resource with modified data
                _package.ReplaceResource(entry, result.ModifiedData);
                await UpdateSelectedResourceDetailsAsync();
                StatusMessage = $"Resource updated by {helper.Label}";
            }
            else
            {
                StatusMessage = $"{helper.Label} completed (no changes)";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Helper error: {ex.Message}";
        }
    }

    private static HexViewerView CreateHexViewer(ReadOnlyMemory<byte> data)
    {
        var vm = new HexViewerViewModel();
        vm.LoadData(data);
        return new HexViewerView { DataContext = vm };
    }

    private static (Control View, object? Wrapper) CreateStblEditorWithWrapper(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new StblResource(key, data);
        var vm = new StblEditorViewModel();
        vm.LoadResource(resource);
        return (new StblEditorView { DataContext = vm }, resource);
    }

    private static (Control View, object? Wrapper) CreateNameMapEditorWithWrapper(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new NameMapResource(key, data);
        var vm = new NameMapEditorViewModel();
        vm.LoadResource(resource);
        return (new NameMapEditorView { DataContext = vm }, resource);
    }

    private static (Control View, object? Wrapper) CreateTextEditorWithWrapper(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new TextResource(key, data);
        var vm = new TextEditorViewModel();
        vm.LoadResource(resource);
        return (new TextEditorView { DataContext = vm }, resource);
    }

    private static (Control View, object? Wrapper) CreateImageViewerWithWrapper(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new ImageResource(key, data);
        var vm = new ImageViewerViewModel();
        vm.LoadResource(resource);
        return (new ImageViewerView { DataContext = vm }, resource);
    }

    private static (Control View, object? Wrapper) CreateSimDataViewerWithWrapper(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new SimDataResource(key, data);
        var vm = new SimDataViewerViewModel();
        vm.LoadResource(resource);
        return (new SimDataViewerView { DataContext = vm }, resource);
    }

    private static (Control View, object? Wrapper) CreateRcolViewerWithWrapper(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new RcolResource(key, data);
        var vm = new RcolViewerViewModel();
        vm.LoadResource(resource);
        return (new RcolViewerView { DataContext = vm }, resource);
    }

    private static (Control View, object? Wrapper) CreateCatalogViewerWithWrapper(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new CobjResource(key, data);
        var vm = new CatalogViewerViewModel();
        vm.LoadResource(resource);
        return (new CatalogViewerView { DataContext = vm }, resource);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_package == null || string.IsNullOrEmpty(PackagePath)) return;

        try
        {
            await _package.SaveAsync();
            StatusMessage = "Package saved";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (_package == null) return;

        var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null);

        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Package As",
            DefaultExtension = "package",
            FileTypeChoices = [PackageFileType]
        });

        if (file != null)
        {
            try
            {
                var path = file.Path.LocalPath;
                await _package.SaveAsAsync(path);
                PackagePath = path;
                Title = $"TS4Tools - {System.IO.Path.GetFileName(path)}";
                StatusMessage = "Package saved";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ClosePackageAsync()
    {
        await CloseCurrentPackageAsync();
        StatusMessage = "Ready";
    }

    private async ValueTask CloseCurrentPackageAsync()
    {
        if (_package != null)
        {
            await _package.DisposeAsync();
            _package = null;
        }
        _hashNameService.Clear();
        PackagePath = string.Empty;
        Title = "TS4Tools";
        Resources.Clear();
        FilteredResources.Clear();
        SelectedResource = null;
        OnPropertyChanged(nameof(HasOpenPackage));
        OnPropertyChanged(nameof(ResourceCount));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(PackagePath))
        {
            await LoadPackageAsync(PackagePath);
        }
    }

    [RelayCommand]
    private static void Exit()
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private static async Task AboutAsync()
    {
        var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null);

        if (topLevel is Window window)
        {
            var dialog = new Window
            {
                Title = "About TS4Tools",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(24),
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "TS4Tools", FontSize = 24, FontWeight = Avalonia.Media.FontWeight.Bold },
                        new TextBlock { Text = "Cross-platform Sims 4 Package Editor" },
                        new TextBlock { Text = "Version 0.1.0", Margin = new Avalonia.Thickness(0, 8, 0, 0) },
                        new TextBlock { Text = "A modern rewrite of s4pe/s4pi", Opacity = 0.7 },
                        new TextBlock { Text = "Licensed under GPLv3", Margin = new Avalonia.Thickness(0, 16, 0, 0), Opacity = 0.7 }
                    }
                }
            };

            await dialog.ShowDialog(window);
        }
    }

    /// <summary>
    /// Opens the Settings dialog.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings
    /// </remarks>
    [RelayCommand]
    private async Task SettingsAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new SettingsWindow();
        await dialog.ShowDialog(window);
        StatusMessage = "Settings updated";
    }

    [RelayCommand]
    private async Task ExportResourceAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var key = SelectedResource.Key;
        var defaultName = $"S4_{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}.bin";

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Resource",
            SuggestedFileName = defaultName,
            FileTypeChoices = [BinaryFileType, AllFilesType]
        });

        if (file != null)
        {
            try
            {
                var entry = _package.Find(key);
                if (entry == null)
                {
                    StatusMessage = "Resource not found";
                    return;
                }

                var data = await _package.GetResourceDataAsync(entry);
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(data);
                StatusMessage = $"Exported {data.Length:N0} bytes";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ImportResourceAsync()
    {
        if (_package == null) return;

        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Resource",
            AllowMultiple = false,
            FileTypeFilter = [BinaryFileType, AllFilesType]
        });

        if (files.Count != 1) return;

        try
        {
            var filePath = files[0].Path.LocalPath;
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // Try to parse S4_Type_Group_Instance format
            ResourceKey key;
            if (TryParseS4FileName(fileName, out var parsedKey))
            {
                key = parsedKey;
            }
            else
            {
                // Use a default key - user should edit later
                key = new ResourceKey(0x00000000, 0x00000000, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }

            var data = await File.ReadAllBytesAsync(filePath);
            var entry = _package.AddResource(key, data, rejectDuplicates: false);

            if (entry != null)
            {
                var instanceName = _hashNameService.TryGetName(key.Instance);
                var item = new ResourceItemViewModel(key, entry.FileSize, instanceName);
                Resources.Add(item);
                ApplyFilter();
                SelectedResource = item;
                StatusMessage = $"Imported {data.Length:N0} bytes";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeleteResource()
    {
        if (_package == null || SelectedResource == null) return;

        var key = SelectedResource.Key;
        var entry = _package.Find(key);

        if (entry != null)
        {
            _package.DeleteResource(entry);
            var item = SelectedResource;
            Resources.Remove(item);
            FilteredResources.Remove(item);
            SelectedResource = null;
            OnPropertyChanged(nameof(ResourceCount));
            StatusMessage = "Resource deleted";
        }
    }

    /// <summary>
    /// Opens the Batch Import dialog for importing multiple files.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Import/ImportBatch.cs
    /// </remarks>
    [RelayCommand]
    private async Task BatchImportAsync()
    {
        if (_package == null) return;

        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new BatchImportWindow();
        var result = await dialog.ShowDialog<bool>(window);

        if (result && dialog.Files.Count > 0)
        {
            var imported = 0;
            var errors = 0;

            foreach (var filePath in dialog.Files)
            {
                try
                {
                    if (!File.Exists(filePath)) continue;

                    var fileName = Path.GetFileName(filePath);
                    ResourceKey key;

                    if (TryParseS4FileName(fileName, out var parsedKey))
                    {
                        key = parsedKey;
                    }
                    else
                    {
                        // Generate a unique key
                        key = new ResourceKey(0x00000000, 0x00000000, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (ulong)imported);
                    }

                    // Check for existing resource
                    var existing = _package.Find(key);
                    if (existing != null && !dialog.Replace)
                    {
                        // Skip duplicate
                        continue;
                    }

                    if (existing != null)
                    {
                        // Remove existing for replacement
                        _package.DeleteResource(existing);
                        var existingItem = Resources.FirstOrDefault(r => r.Key == key);
                        if (existingItem != null)
                        {
                            Resources.Remove(existingItem);
                        }
                    }

                    var data = await File.ReadAllBytesAsync(filePath);
                    var entry = _package.AddResource(key, data, rejectDuplicates: false);

                    if (entry != null)
                    {
                        var instanceName = dialog.UseNames ? Path.GetFileNameWithoutExtension(fileName) : _hashNameService.TryGetName(key.Instance);
                        var item = new ResourceItemViewModel(key, entry.FileSize, instanceName);
                        Resources.Add(item);
                        imported++;
                    }
                }
                catch
                {
                    errors++;
                }
            }

            ApplyFilter();
            OnPropertyChanged(nameof(ResourceCount));
            StatusMessage = $"Imported {imported} file(s)" + (errors > 0 ? $", {errors} error(s)" : "");
        }
    }

    [RelayCommand]
    private async Task ReplaceResourceAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var topLevel = GetTopLevel();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Replace Resource",
            AllowMultiple = false,
            FileTypeFilter = [BinaryFileType, AllFilesType]
        });

        if (files.Count != 1) return;

        try
        {
            var key = SelectedResource.Key;
            var entry = _package.Find(key);

            if (entry == null)
            {
                StatusMessage = "Resource not found";
                return;
            }

            var data = await File.ReadAllBytesAsync(files[0].Path.LocalPath);
            _package.ReplaceResource(entry, data);
            await UpdateSelectedResourceDetailsAsync();
            StatusMessage = $"Replaced with {data.Length:N0} bytes";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Replace error: {ex.Message}";
        }
    }

    private static TopLevel? GetTopLevel()
    {
        return TopLevel.GetTopLevel(
            App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);
    }

    private static bool TryParseS4FileName(string fileName, out ResourceKey key)
    {
        key = default;

        // Expected format: S4_XXXXXXXX_XXXXXXXX_XXXXXXXXXXXXXXXX
        if (!fileName.StartsWith("S4_", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = fileName[3..].Split('_');
        if (parts.Length != 3)
            return false;

        try
        {
            var type = Convert.ToUInt32(parts[0], 16);
            var group = Convert.ToUInt32(parts[1], 16);
            var instance = Convert.ToUInt64(parts[2], 16);
            key = new ResourceKey(type, group, instance);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand]
    private async Task HashCalculatorAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new HashCalculatorWindow();
        await dialog.ShowDialog(window);
        StatusMessage = "Hash calculator closed";
    }

    [RelayCommand]
    private async Task PackageStatsAsync()
    {
        if (_package == null) return;

        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new PackageStatsWindow(_package);
        await dialog.ShowDialog(window);
    }

    /// <summary>
    /// Opens the Merge Packages dialog for importing resources from other packages.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 350-500
    /// </remarks>
    [RelayCommand]
    private async Task MergePackagesAsync()
    {
        if (_package == null) return;

        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new PackageMergeWindow();
        var result = await dialog.ShowDialog<bool>(window);

        if (result && dialog.Packages.Count > 0)
        {
            try
            {
                var totalMerged = 0;
                var totalSkipped = 0;
                var packageCount = dialog.Packages.Count;

                foreach (var pkgInfo in dialog.Packages)
                {
                    if (pkgInfo.Package == null) continue;

                    dialog.ProgressText = $"Merging {pkgInfo.FileName}...";
                    var entries = pkgInfo.Package.Resources.ToList();
                    var processed = 0;

                    foreach (var entry in entries)
                    {
                        var key = entry.Key;

                        // Skip NameMap if requested
                        if (key.ResourceType == 0x0166038C && dialog.SkipNameMaps)
                        {
                            continue;
                        }

                        // Check for duplicates
                        var existing = _package.Find(key);

                        if (existing != null)
                        {
                            switch (dialog.DuplicateMode)
                            {
                                case DuplicateHandling.Skip:
                                    totalSkipped++;
                                    continue;
                                case DuplicateHandling.Replace:
                                    _package.DeleteResource(existing);
                                    break;
                                case DuplicateHandling.Allow:
                                    // Allow duplicate keys
                                    break;
                            }
                        }

                        // Get data and add to target package
                        var data = await pkgInfo.Package.GetResourceDataAsync(entry);
                        var newEntry = _package.AddResource(key, data.ToArray(), rejectDuplicates: false);

                        if (newEntry != null)
                        {
                            var instanceName = _hashNameService.TryGetName(key.Instance);
                            var item = new ResourceItemViewModel(key, newEntry.FileSize, instanceName);
                            Resources.Add(item);
                            totalMerged++;
                        }

                        processed++;
                        dialog.Progress = (processed * 100.0) / entries.Count;
                    }
                }

                dialog.DisposePackages();
                ApplyFilter();
                OnPropertyChanged(nameof(ResourceCount));
                StatusMessage = $"Merged {totalMerged} resources from {packageCount} package(s)" +
                    (totalSkipped > 0 ? $", {totalSkipped} skipped" : "");
            }
            catch (Exception ex)
            {
                dialog.DisposePackages();
                StatusMessage = $"Merge error: {ex.Message}";
            }
        }
        else
        {
            dialog.DisposePackages();
        }
    }

    /// <summary>
    /// Opens the Search dialog to find resources.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Tools/SearchForm.cs
    /// </remarks>
    [RelayCommand]
    private async Task SearchAsync()
    {
        if (_package == null) return;

        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new SearchWindow(_package, FilteredResources);
        var result = await dialog.ShowDialog<bool>(window);

        if (result && dialog.SelectedResult != null)
        {
            // Navigate to the selected resource
            SelectedResource = dialog.SelectedResult;
            StatusMessage = $"Found: {dialog.SelectedResult.TypeName}";
        }
    }

    [RelayCommand]
    private async Task CopyResourceKeyAsync()
    {
        if (SelectedResource == null) return;

        var key = SelectedResource.Key;
        var keyString = $"S4_{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}";

        var topLevel = GetTopLevel();
        if (topLevel?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(keyString);
            StatusMessage = $"Copied: {keyString}";
        }
    }

    /// <summary>
    /// Copies the selected resource to the internal clipboard.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1495-1529
    /// </remarks>
    [RelayCommand]
    private async Task CopyResourceAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var key = SelectedResource.Key;
        var entry = _package.Find(key);
        if (entry == null)
        {
            StatusMessage = "Resource not found";
            return;
        }

        try
        {
            var data = await _package.GetResourceDataAsync(entry);
            _clipboardData = new ClipboardResourceData
            {
                Key = key,
                Data = data.ToArray(),
                ResourceName = SelectedResource.InstanceName
            };

            // Also copy the key to system clipboard for cross-application use
            var keyString = $"S4_{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}";
            var topLevel = GetTopLevel();
            if (topLevel?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(keyString);
            }

            OnPropertyChanged(nameof(HasClipboardResource));
            StatusMessage = $"Copied resource: {SelectedResource.TypeName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Copy error: {ex.Message}";
        }
    }

    /// <summary>
    /// Pastes the resource from the internal clipboard into the current package.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 513-560 (ResourcePaste)
    /// </remarks>
    [RelayCommand]
    private void PasteResource()
    {
        if (_package == null || _clipboardData == null) return;

        try
        {
            var key = _clipboardData.Key;
            var entry = _package.AddResource(key, _clipboardData.Data, rejectDuplicates: false);

            if (entry != null)
            {
                var item = new ResourceItemViewModel(key, entry.FileSize, _clipboardData.ResourceName);
                Resources.Add(item);
                ApplyFilter();
                SelectedResource = item;
                OnPropertyChanged(nameof(ResourceCount));
                StatusMessage = $"Pasted resource: {item.TypeName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Paste error: {ex.Message}";
        }
    }

    /// <summary>
    /// Duplicates the selected resource within the current package.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1540-1558 (ResourceDuplicate)
    /// </remarks>
    [RelayCommand]
    private async Task DuplicateResourceAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var key = SelectedResource.Key;
        var entry = _package.Find(key);
        if (entry == null)
        {
            StatusMessage = "Resource not found";
            return;
        }

        try
        {
            var data = await _package.GetResourceDataAsync(entry);
            var newEntry = _package.AddResource(key, data.ToArray(), rejectDuplicates: false);

            if (newEntry != null)
            {
                var item = new ResourceItemViewModel(key, newEntry.FileSize, SelectedResource.InstanceName);
                Resources.Add(item);
                ApplyFilter();
                SelectedResource = item;
                OnPropertyChanged(nameof(ResourceCount));
                StatusMessage = $"Duplicated resource: {item.TypeName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Duplicate error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CycleSortMode()
    {
        SortMode = SortMode switch
        {
            ResourceSortMode.TypeName => ResourceSortMode.Instance,
            ResourceSortMode.Instance => ResourceSortMode.Size,
            ResourceSortMode.Size => ResourceSortMode.TypeName,
            _ => ResourceSortMode.TypeName
        };
        StatusMessage = $"Sorting by: {SortMode}";
    }

    #region Control Panel Support
    // Source: legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs

    /// <summary>
    /// Shows the hex view for the selected resource.
    /// </summary>
    public void ShowHexView()
    {
        if (SelectedResource == null || _package == null) return;

        var key = SelectedResource.Key;
        var entry = _package.Find(key);
        if (entry == null) return;

        try
        {
            var dataTask = _package.GetResourceDataAsync(entry);
            dataTask.AsTask().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        EditorContent = CreateHexViewer(t.Result);
                        _currentResourceWrapper = null;
                        StatusMessage = "Hex view";
                    });
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Shows the value/preview view for the selected resource.
    /// </summary>
    public async void ShowValueView()
    {
        if (SelectedResource == null) return;
        await UpdateSelectedResourceDetailsAsync();
        StatusMessage = "Preview view";
    }

    /// <summary>
    /// Executes the configured hex editor for the selected resource.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3176-3200 (EditOte hex editor)
    /// </remarks>
    public async Task ExecuteHexEditorAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var settings = SettingsService.Instance.Settings;
        if (string.IsNullOrEmpty(settings.HexEditorCommand))
        {
            StatusMessage = "No hex editor configured. Set it in Settings > External Programs.";
            return;
        }

        var key = SelectedResource.Key;
        var entry = _package.Find(key);
        if (entry == null)
        {
            StatusMessage = "Resource not found";
            return;
        }

        StatusMessage = "Opening hex editor...";

        try
        {
            var data = await _package.GetResourceDataAsync(entry);
            var tempFile = Path.Combine(Path.GetTempPath(), $"S4_{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}.bin");

            // Write to temp file
            await File.WriteAllBytesAsync(tempFile, data.ToArray());
            var lastWriteTime = File.GetLastWriteTimeUtc(tempFile);

            // Build command line
            var quote = settings.HexEditorWantsQuotes ? "\"" : "";
            var args = quote + tempFile + quote;

            // Execute editor
            using var process = new Process();
            process.StartInfo.FileName = settings.HexEditorCommand;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            await process.WaitForExitAsync();

            // Check for modifications
            var newWriteTime = File.GetLastWriteTimeUtc(tempFile);
            if (settings.HexEditorIgnoreTimestamp || newWriteTime != lastWriteTime)
            {
                var modifiedData = await File.ReadAllBytesAsync(tempFile);
                _package.ReplaceResource(entry, modifiedData);
                await UpdateSelectedResourceDetailsAsync();
                StatusMessage = "Resource updated from hex editor";
            }
            else
            {
                StatusMessage = "Hex editor closed (no changes)";
            }

            // Cleanup
            try { File.Delete(tempFile); } catch { }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hex editor error: {ex.Message}";
        }
    }

    /// <summary>
    /// Called when the Sort option changes in the control panel.
    /// </summary>
    public void OnSortOptionChanged(bool sort)
    {
        // The Sort option enables/disables sorting - we can use TypeName sort as default
        if (sort && SortMode == ResourceSortMode.TypeName)
        {
            ApplyFilter();
        }
        StatusMessage = sort ? "Sorting enabled" : "Sorting disabled";
    }

    /// <summary>
    /// Called when the HexOnly option changes in the control panel.
    /// </summary>
    public void OnHexOnlyOptionChanged(bool hexOnly)
    {
        // This would affect how type names are displayed - hex IDs vs friendly names
        // For now, just update status
        StatusMessage = hexOnly ? "Showing hex type IDs only" : "Showing type names";
        // Would need to refresh the resource list display here
    }

    /// <summary>
    /// Called when the UseNames option changes in the control panel.
    /// </summary>
    public void OnUseNamesOptionChanged(bool useNames)
    {
        StatusMessage = useNames ? "Using resource names from NameMap" : "Not using resource names";
        // Would need to refresh the resource list display here
    }

    /// <summary>
    /// Called when the UseTags option changes in the control panel.
    /// </summary>
    public void OnUseTagsOptionChanged(bool useTags)
    {
        StatusMessage = useTags ? "Showing type tags" : "Not showing type tags";
        // Would need to refresh the resource list display here
    }

    /// <summary>
    /// Called when the Auto mode changes in the control panel.
    /// </summary>
    public void OnAutoModeChanged(int autoChoice)
    {
        var mode = autoChoice switch
        {
            1 => "Hex",
            2 => "Preview",
            _ => "Off"
        };
        StatusMessage = $"Auto mode: {mode}";
    }

    #endregion

    /// <summary>
    /// Toggles the advanced filter panel visibility.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Filter/FilterField.cs
    /// </remarks>
    [RelayCommand]
    private void ToggleAdvancedFilter()
    {
        ShowAdvancedFilter = !ShowAdvancedFilter;
    }

    /// <summary>
    /// Toggles the property grid panel visibility.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs
    /// </remarks>
    [RelayCommand]
    private void TogglePropertyGrid()
    {
        ShowPropertyGrid = !ShowPropertyGrid;

        if (ShowPropertyGrid && _currentResourceWrapper != null)
        {
            UpdatePropertyGrid(_currentResourceWrapper);
        }
    }

    private void UpdatePropertyGrid(object? resource)
    {
        if (resource == null)
        {
            PropertyGridContent = null;
            return;
        }

        var vm = new PropertyGridViewModel();
        vm.LoadResource(resource);
        PropertyGridContent = new PropertyGridEditorView { DataContext = vm };
    }

    /// <summary>
    /// Opens the Resource Details dialog for viewing/editing resource TGI.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1584-1617 (ResourceDetails method)
    /// </remarks>
    [RelayCommand]
    private async Task ResourceDetailsAsync()
    {
        if (_package == null || SelectedResource == null) return;

        var key = SelectedResource.Key;
        var entry = _package.Find(key);
        if (entry == null)
        {
            StatusMessage = "Resource not found";
            return;
        }

        var topLevel = GetTopLevel();
        if (topLevel is not Window window) return;

        var dialog = new ResourceDetailsWindow();
        dialog.Initialize(
            key,
            SelectedResource.InstanceName,
            entry.IsCompressed,
            entry.FileSize,
            entry.MemorySize);

        var result = await dialog.ShowDialog<bool>(window);

        if (result && dialog.WasModified)
        {
            try
            {
                // Update the resource key if changed
                var newKey = dialog.ResourceKey;
                if (newKey != key)
                {
                    // Get data, delete old, add new
                    var data = await _package.GetResourceDataAsync(entry);
                    _package.DeleteResource(entry);

                    var newEntry = _package.AddResource(newKey, data.ToArray(), rejectDuplicates: false);
                    if (newEntry != null)
                    {
                        // Note: compression setting requires re-adding with new data
                        // This is a simplified implementation - full implementation would
                        // need to actually compress/decompress the data

                        // Update the list
                        Resources.Remove(SelectedResource);
                        FilteredResources.Remove(SelectedResource);

                        var newItem = new ResourceItemViewModel(newKey, newEntry.FileSize, dialog.ResourceName);
                        Resources.Add(newItem);
                        ApplyFilter();
                        SelectedResource = newItem;
                    }
                }

                StatusMessage = "Resource details updated";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task OpenRecentFileAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (File.Exists(path))
        {
            await LoadPackageAsync(path);
        }
        else
        {
            StatusMessage = $"File not found: {path}";
            // Remove from recent files
            var item = RecentFiles.FirstOrDefault(r => r.Path == path);
            if (item != null)
            {
                RecentFiles.Remove(item);
                SaveRecentFiles();
            }
        }
    }

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();

        try
        {
            if (File.Exists(RecentFilesPath))
            {
                var json = File.ReadAllText(RecentFilesPath);
                var paths = JsonSerializer.Deserialize<string[]>(json) ?? [];

                foreach (var path in paths.Take(MaxRecentFiles))
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        RecentFiles.Add(new RecentFileViewModel(path));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to load recent files: {ex.Message}");
        }
    }

    private void SaveRecentFiles()
    {
        try
        {
            var dir = Path.GetDirectoryName(RecentFilesPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var paths = RecentFiles.Select(r => r.Path).ToArray();
            var json = JsonSerializer.Serialize(paths);
            File.WriteAllText(RecentFilesPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to save recent files: {ex.Message}");
        }
    }

    private void AddToRecentFiles(string path)
    {
        // Remove if already exists
        var existing = RecentFiles.FirstOrDefault(r => r.Path == path);
        if (existing != null)
        {
            RecentFiles.Remove(existing);
        }

        // Add to front
        RecentFiles.Insert(0, new RecentFileViewModel(path));

        // Trim to max
        while (RecentFiles.Count > MaxRecentFiles)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        SaveRecentFiles();
    }

    #region Bookmarks
    // Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs

    /// <summary>
    /// Loads bookmarks from settings.
    /// </summary>
    private void LoadBookmarks()
    {
        Bookmarks.Clear();
        var settings = SettingsService.Instance.Settings;

        foreach (var bookmark in settings.Bookmarks)
        {
            if (!string.IsNullOrEmpty(bookmark))
            {
                Bookmarks.Add(new BookmarkItemViewModel(bookmark));
            }
        }

        OnPropertyChanged(nameof(HasBookmarks));
    }

    /// <summary>
    /// Opens a bookmarked package.
    /// </summary>
    [RelayCommand]
    private async Task OpenBookmarkAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        // Parse bookmark format: "0:path" for read-only, "1:path" for read-write
        var isReadOnly = false;
        var actualPath = path;
        if (path.Length > 2 && path[1] == ':' && (path[0] == '0' || path[0] == '1'))
        {
            isReadOnly = path[0] == '0';
            actualPath = path[2..];
        }

        if (!File.Exists(actualPath))
        {
            StatusMessage = $"Bookmark not found: {actualPath}";
            return;
        }

        await LoadPackageAsync(actualPath);
        if (isReadOnly)
        {
            StatusMessage = $"Opened (read-only): {Path.GetFileName(actualPath)}";
        }
    }

    /// <summary>
    /// Adds the current package to bookmarks.
    /// </summary>
    [RelayCommand]
    private void AddCurrentToBookmarks()
    {
        if (string.IsNullOrEmpty(PackagePath)) return;

        var settings = SettingsService.Instance.Settings;
        if (settings.Bookmarks.Count >= settings.BookmarkSize)
        {
            StatusMessage = $"Cannot add bookmark: maximum of {settings.BookmarkSize} bookmarks reached";
            return;
        }

        var bookmarkEntry = "1:" + PackagePath; // 1 = read-write
        if (settings.Bookmarks.Contains(bookmarkEntry) || settings.Bookmarks.Contains("0:" + PackagePath))
        {
            StatusMessage = "Package is already bookmarked";
            return;
        }

        settings.Bookmarks.Add(bookmarkEntry);
        SettingsService.Instance.Save();
        LoadBookmarks();
        StatusMessage = $"Added bookmark: {Path.GetFileName(PackagePath)}";
    }

    #endregion

    /// <summary>
    /// Executes an async task with error handling, updating StatusMessage on failure.
    /// </summary>
    private async void ExecuteAsync(Func<Task> asyncAction, string errorPrefix = "Error")
    {
        try
        {
            await asyncAction().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{errorPrefix}: {ex}");
            StatusMessage = $"{errorPrefix}: {ex.Message}";
        }
    }

    /// <summary>
    /// Disposes the package when the window closes without explicit ClosePackage.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        PropertyChanged -= _propertyChangedHandler;
        await CloseCurrentPackageAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// View model for a recent file entry.
/// </summary>
public sealed class RecentFileViewModel
{
    public string Path { get; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? "";

    public RecentFileViewModel(string path)
    {
        Path = path;
    }
}

/// <summary>
/// Internal clipboard data for resource copy/paste operations.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 96-100 (MyDataFormat struct)
/// </remarks>
public sealed class ClipboardResourceData
{
    public required ResourceKey Key { get; init; }
    public required byte[] Data { get; init; }
    public string? ResourceName { get; init; }
}

/// <summary>
/// View model for a helper menu item.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3095-3134 (helper buttons)
/// </remarks>
public sealed class HelperMenuItemViewModel
{
    private readonly HelperInstance _helper;
    private readonly Func<HelperInstance, Task> _executeAction;

    public HelperMenuItemViewModel(HelperInstance helper, Func<HelperInstance, Task> executeAction)
    {
        _helper = helper;
        _executeAction = executeAction;
    }

    public string Label => _helper.Label;
    public string Description => _helper.Description;
    public string Id => _helper.Id;

    public IAsyncRelayCommand ExecuteCommand => new AsyncRelayCommand(async () => await _executeAction(_helper));
}

/// <summary>
/// View model for a bookmark menu item.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs
/// Format: "0:path" for read-only, "1:path" for read-write
/// </remarks>
public sealed class BookmarkItemViewModel
{
    public BookmarkItemViewModel(string bookmarkEntry)
    {
        RawEntry = bookmarkEntry;

        // Parse format: "0:path" for read-only, "1:path" for read-write
        if (bookmarkEntry.Length > 2 && bookmarkEntry[1] == ':')
        {
            IsReadOnly = bookmarkEntry[0] == '0';
            FilePath = bookmarkEntry[2..];
        }
        else
        {
            IsReadOnly = false;
            FilePath = bookmarkEntry;
        }

        FileName = Path.GetFileName(FilePath);
    }

    public string RawEntry { get; }
    public string FilePath { get; }
    public string FileName { get; }
    public bool IsReadOnly { get; }

    public string DisplayName => IsReadOnly ? $"[RO] {FileName}" : FileName;
    public string ToolTip => FilePath;
}
