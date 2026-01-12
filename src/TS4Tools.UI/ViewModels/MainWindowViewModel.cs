using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools;
using TS4Tools.Package;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels.Editors;
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

    public bool HasSelectedResource => SelectedResource != null;

    [ObservableProperty]
    private string _selectedResourceTypeName = string.Empty;

    [ObservableProperty]
    private string _selectedResourceKey = string.Empty;

    [ObservableProperty]
    private string _selectedResourceInfo = string.Empty;

    [ObservableProperty]
    private Control? _editorContent;

    public bool HasOpenPackage => _package != null;

    public int ResourceCount => _package?.ResourceCount ?? 0;

    public bool HasClipboardResource => _clipboardData != null;

    // Internal clipboard for resource copy/paste
    // Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 42-43, 96-100
    private ClipboardResourceData? _clipboardData;

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

        var filter = FilterText.Trim().ToLowerInvariant();
        var filtered = string.IsNullOrEmpty(filter)
            ? Resources.AsEnumerable()
            : Resources.Where(r =>
                r.TypeName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                r.DisplayKey.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (r.InstanceName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));

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

    private async Task UpdateSelectedResourceDetailsAsync()
    {
        if (SelectedResource == null || _package == null)
        {
            SelectedResourceTypeName = string.Empty;
            SelectedResourceKey = string.Empty;
            SelectedResourceInfo = string.Empty;
            EditorContent = null;
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

            // Create appropriate editor based on resource type
            EditorContent = key.ResourceType switch
            {
                0x220557DA => CreateStblEditor(key, data), // STBL
                0x0166038C => CreateNameMapEditor(key, data), // NameMap
                0x03B33DDF or 0x6017E896 => CreateTextEditor(key, data), // Tuning XML
                0x00B00000 or 0x00B2D882 => CreateImageViewer(key, data), // PNG, DDS
                0x545AC67A => CreateSimDataViewer(key, data), // SimData
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
                    => CreateRcolViewer(key, data),
                0x319E4F1D => CreateCatalogViewer(key, data), // COBJ (Catalog Object)
                _ => CreateHexViewer(data) // Default hex viewer
            };
        }
    }

    private static HexViewerView CreateHexViewer(ReadOnlyMemory<byte> data)
    {
        var vm = new HexViewerViewModel();
        vm.LoadData(data);
        return new HexViewerView { DataContext = vm };
    }

    private static StblEditorView CreateStblEditor(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new StblResource(key, data);
        var vm = new StblEditorViewModel();
        vm.LoadResource(resource);
        return new StblEditorView { DataContext = vm };
    }

    private static NameMapEditorView CreateNameMapEditor(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new NameMapResource(key, data);
        var vm = new NameMapEditorViewModel();
        vm.LoadResource(resource);
        return new NameMapEditorView { DataContext = vm };
    }

    private static TextEditorView CreateTextEditor(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new TextResource(key, data);
        var vm = new TextEditorViewModel();
        vm.LoadResource(resource);
        return new TextEditorView { DataContext = vm };
    }

    private static ImageViewerView CreateImageViewer(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new ImageResource(key, data);
        var vm = new ImageViewerViewModel();
        vm.LoadResource(resource);
        return new ImageViewerView { DataContext = vm };
    }

    private static SimDataViewerView CreateSimDataViewer(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new SimDataResource(key, data);
        var vm = new SimDataViewerViewModel();
        vm.LoadResource(resource);
        return new SimDataViewerView { DataContext = vm };
    }

    private static RcolViewerView CreateRcolViewer(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new RcolResource(key, data);
        var vm = new RcolViewerViewModel();
        vm.LoadResource(resource);
        return new RcolViewerView { DataContext = vm };
    }

    private static CatalogViewerView CreateCatalogViewer(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        var resource = new CobjResource(key, data);
        var vm = new CatalogViewerViewModel();
        vm.LoadResource(resource);
        return new CatalogViewerView { DataContext = vm };
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
