using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools;
using TS4Tools.Package;

namespace TS4Tools.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly FilePickerFileType PackageFileType = new("Sims 4 Package") { Patterns = ["*.package"] };
    private static readonly FilePickerFileType AllFilesType = new("All Files") { Patterns = ["*"] };

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
    private ResourceItemViewModel? _selectedResource;

    [ObservableProperty]
    private string _selectedResourceTypeName = string.Empty;

    [ObservableProperty]
    private string _selectedResourceKey = string.Empty;

    [ObservableProperty]
    private string _selectedResourceInfo = string.Empty;

    public bool HasOpenPackage => _package != null;

    public int ResourceCount => _package?.ResourceCount ?? 0;

    public ObservableCollection<ResourceItemViewModel> Resources { get; } = [];

    public ObservableCollection<ResourceItemViewModel> FilteredResources { get; } = [];

    public MainWindowViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilterText))
            {
                ApplyFilter();
            }
            else if (e.PropertyName == nameof(SelectedResource))
            {
                _ = UpdateSelectedResourceDetailsAsync();
            }
        };
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
            ? Resources
            : Resources.Where(r =>
                r.TypeName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                r.DisplayKey.Contains(filter, StringComparison.OrdinalIgnoreCase));

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
        }
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
}
