using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TS4Tools.Package;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog for importing resources from another package file.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 133-162 (ResourceImportPackages)
/// Source: legacy_references/Sims4Tools/s4pe/Import/ImportBatch.cs
/// </remarks>
public partial class ImportFromPackageWindow : Window, IAsyncDisposable
{
    private static readonly FilePickerFileType PackageFileType = new("Sims 4 Package") { Patterns = ["*.package"] };
    private static readonly FilePickerFileType AllFilesType = new("All Files") { Patterns = ["*"] };

    private DbpfPackage? _sourcePackage;
    private readonly List<ResourceItemViewModel> _resources = [];
    private bool _disposed;

    /// <summary>
    /// Resources selected for import.
    /// </summary>
    public IReadOnlyList<ResourceItemViewModel> SelectedResources { get; private set; } = [];

    /// <summary>
    /// How to handle duplicate resources.
    /// </summary>
    public DuplicateHandling DuplicateMode => (DuplicateHandling)DuplicateModeCombo.SelectedIndex;

    /// <summary>
    /// Whether to import NameMap entries.
    /// </summary>
    public bool ImportNames => ImportNamesCheckBox.IsChecked == true;

    /// <summary>
    /// The source package (for reading resource data).
    /// </summary>
    public DbpfPackage? SourcePackage => _sourcePackage;

    public ImportFromPackageWindow()
    {
        InitializeComponent();
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Package to Import From",
            AllowMultiple = false,
            FileTypeFilter = [PackageFileType, AllFilesType]
        });

        if (files.Count == 1)
        {
            await LoadSourcePackageAsync(files[0].Path.LocalPath);
        }
    }

    private async Task LoadSourcePackageAsync(string path)
    {
        try
        {
            StatusText.Text = "Loading package...";

            // Close any existing package
            if (_sourcePackage != null)
            {
                await _sourcePackage.DisposeAsync();
                _sourcePackage = null;
            }

            _sourcePackage = await DbpfPackage.OpenAsync(path);
            SourcePathTextBox.Text = path;

            // Populate resource list
            _resources.Clear();
            foreach (var entry in _sourcePackage.Resources)
            {
                var item = new ResourceItemViewModel(entry.Key, entry.FileSize, null);
                _resources.Add(item);
            }

            ResourceListBox.ItemsSource = _resources;
            ResourcesHeader.Text = $"Resources to import ({_resources.Count}):";

            // Select all if checkbox is checked
            if (SelectAllCheckBox.IsChecked == true)
            {
                ResourceListBox.SelectAll();
            }

            ImportButton.IsEnabled = _resources.Count > 0;
            StatusText.Text = $"Loaded {_resources.Count} resources";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            ImportButton.IsEnabled = false;
        }
    }

    private void SelectAllCheckBox_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectAllCheckBox.IsChecked == true)
        {
            ResourceListBox.SelectAll();
        }
        else
        {
            ResourceListBox.UnselectAll();
        }
    }

    private void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        // Collect selected resources
        SelectedResources = ResourceListBox.SelectedItems?
            .OfType<ResourceItemViewModel>()
            .ToList() ?? [];

        if (SelectedResources.Count == 0)
        {
            StatusText.Text = "No resources selected";
            return;
        }

        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    protected override void OnClosed(EventArgs e)
    {
        // Don't dispose here - caller needs to read the package
        base.OnClosed(e);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_sourcePackage != null)
        {
            await _sourcePackage.DisposeAsync();
            _sourcePackage = null;
        }

        GC.SuppressFinalize(this);
    }
}
