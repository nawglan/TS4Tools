using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools;
using TS4Tools.Package;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.UI.Views.Dialogs;
using TS4Tools.UI.Views.Editors;
using TS4Tools.Wrappers;

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

public partial class MainWindowViewModel : ViewModelBase
{
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

    public ObservableCollection<ResourceItemViewModel> Resources { get; } = [];

    public ObservableCollection<ResourceItemViewModel> FilteredResources { get; } = [];

    public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = [];

    [ObservableProperty]
    private ResourceSortMode _sortMode = ResourceSortMode.TypeName;

    public MainWindowViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilterText) || e.PropertyName == nameof(SortMode))
            {
                ApplyFilter();
            }
            else if (e.PropertyName == nameof(SelectedResource))
            {
                _ = UpdateSelectedResourceDetailsAsync();
            }
        };

        LoadRecentFiles();
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

            PopulateResourceList();
            AddToRecentFiles(path);

            StatusMessage = $"Loaded {ResourceCount} resources";
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
            var item = new ResourceItemViewModel(entry.Key);
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
                r.DisplayKey.Contains(filter, StringComparison.OrdinalIgnoreCase));

        // Apply sorting
        filtered = SortMode switch
        {
            ResourceSortMode.TypeName => filtered.OrderBy(r => r.TypeName).ThenBy(r => r.Key.Instance),
            ResourceSortMode.Instance => filtered.OrderBy(r => r.Key.Instance),
            ResourceSortMode.Size => filtered, // Size would require entry lookup, keep as-is for now
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
        PackagePath = string.Empty;
        Title = "TS4Tools";
        Resources.Clear();
        FilteredResources.Clear();
        SelectedResource = null;
        OnPropertyChanged(nameof(HasOpenPackage));
        OnPropertyChanged(nameof(ResourceCount));
    }

    [RelayCommand]
    private void Refresh()
    {
        if (!string.IsNullOrEmpty(PackagePath))
        {
            _ = LoadPackageAsync(PackagePath);
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
                var item = new ResourceItemViewModel(key);
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

    [RelayCommand]
    private void CycleSortMode()
    {
        SortMode = SortMode switch
        {
            ResourceSortMode.TypeName => ResourceSortMode.Instance,
            ResourceSortMode.Instance => ResourceSortMode.TypeName,
            _ => ResourceSortMode.TypeName
        };
        StatusMessage = $"Sorting by: {SortMode}";
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
        catch
        {
            // Ignore errors loading recent files
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
        catch
        {
            // Ignore errors saving recent files
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
