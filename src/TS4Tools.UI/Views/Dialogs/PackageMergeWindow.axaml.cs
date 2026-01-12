using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TS4Tools.Package;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog for merging multiple packages into the current package.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 350-500
/// </remarks>
public partial class PackageMergeWindow : Window
{
    private readonly List<PackageInfo> _packages = [];
    private static readonly FilePickerFileType PackageFileType = new("Sims 4 Package") { Patterns = ["*.package"] };

    /// <summary>
    /// Gets the duplicate handling mode.
    /// </summary>
    public DuplicateHandling DuplicateMode => (DuplicateHandling)(DuplicateHandlingCombo.SelectedIndex);

    /// <summary>
    /// Gets whether to compress resources.
    /// </summary>
    public bool Compress => CompressCheckBox.IsChecked == true;

    /// <summary>
    /// Gets whether to merge NameMap entries.
    /// </summary>
    public bool MergeNameMaps => MergeNameMapsCheckBox.IsChecked == true;

    /// <summary>
    /// Gets whether to skip NameMap resources during merge.
    /// </summary>
    public bool SkipNameMaps => SkipNameMapsCheckBox.IsChecked == true;

    /// <summary>
    /// Gets the list of packages to merge.
    /// </summary>
    public IReadOnlyList<PackageInfo> Packages => _packages;

    /// <summary>
    /// Gets the current progress (0-100).
    /// </summary>
    public double Progress
    {
        get => MergeProgress.Value;
        set => MergeProgress.Value = value;
    }

    /// <summary>
    /// Gets or sets the progress label text.
    /// </summary>
    public string ProgressText
    {
        get => ProgressLabel.Text ?? "";
        set => ProgressLabel.Text = value;
    }

    public PackageMergeWindow()
    {
        InitializeComponent();

        MergeButton.Click += MergeButton_Click;
        CancelButton.Click += CancelButton_Click;
        AddPackagesButton.Click += AddPackagesButton_Click;
        RemovePackageButton.Click += RemovePackageButton_Click;
        ClearPackagesButton.Click += ClearPackagesButton_Click;
        PackageListBox.SelectionChanged += PackageListBox_SelectionChanged;
    }

    private void UpdateSummary()
    {
        PackageListBox.ItemsSource = null;
        PackageListBox.ItemsSource = _packages;

        var totalResources = _packages.Sum(p => p.ResourceCount);
        SummaryLabel.Text = _packages.Count == 0
            ? "No packages selected"
            : $"{_packages.Count} package(s), {totalResources:N0} total resources";

        MergeButton.IsEnabled = _packages.Count > 0;
    }

    private void MergeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_packages.Count == 0) return;
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        // Dispose any loaded packages
        foreach (var pkg in _packages)
        {
            pkg.Package?.Dispose();
        }
        _packages.Clear();
        Close(false);
    }

    private async void AddPackagesButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Packages to Merge",
            AllowMultiple = true,
            FileTypeFilter = [PackageFileType]
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;

            // Skip if already added
            if (_packages.Any(p => p.Path == path)) continue;

            try
            {
                var package = await DbpfPackage.OpenAsync(path);
                var info = new PackageInfo
                {
                    Path = path,
                    FileName = Path.GetFileName(path),
                    ResourceCount = package.ResourceCount,
                    Package = package
                };
                _packages.Add(info);
            }
            catch (Exception ex)
            {
                // Show error but continue with other packages
                System.Diagnostics.Debug.WriteLine($"[TS4Tools] Failed to open package {path}: {ex.Message}");
            }
        }

        UpdateSummary();
    }

    private void RemovePackageButton_Click(object? sender, RoutedEventArgs e)
    {
        if (PackageListBox.SelectedItems == null) return;

        var toRemove = PackageListBox.SelectedItems
            .OfType<PackageInfo>()
            .ToList();

        foreach (var pkg in toRemove)
        {
            pkg.Package?.Dispose();
            _packages.Remove(pkg);
        }

        UpdateSummary();
    }

    private void ClearPackagesButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var pkg in _packages)
        {
            pkg.Package?.Dispose();
        }
        _packages.Clear();
        UpdateSummary();
    }

    private void PackageListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RemovePackageButton.IsEnabled = PackageListBox.SelectedItems?.Count > 0;
    }

    /// <summary>
    /// Cleans up packages after merge completes.
    /// </summary>
    public void DisposePackages()
    {
        foreach (var pkg in _packages)
        {
            pkg.Package?.Dispose();
        }
    }
}

/// <summary>
/// Information about a package to merge.
/// </summary>
public sealed class PackageInfo
{
    public required string Path { get; init; }
    public required string FileName { get; init; }
    public int ResourceCount { get; init; }
    public DbpfPackage? Package { get; init; }
}

/// <summary>
/// How to handle duplicate resources during merge.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs DuplicateHandling enum
/// </remarks>
public enum DuplicateHandling
{
    /// <summary>Replace existing resources with the same key.</summary>
    Replace = 0,
    /// <summary>Skip resources that already exist.</summary>
    Skip = 1,
    /// <summary>Allow duplicate keys.</summary>
    Allow = 2
}
