using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog for batch importing multiple files into a package.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/ImportBatch.cs
/// </remarks>
public partial class BatchImportWindow : Window
{
    private readonly List<string> _files = [];

    /// <summary>
    /// Gets the list of files to import.
    /// </summary>
    public IReadOnlyList<string> Files => _files;

    /// <summary>
    /// Gets whether to replace existing resources with the same key.
    /// </summary>
    public bool Replace => ReplaceCheckBox.IsChecked == true;

    /// <summary>
    /// Gets whether to compress resources on import.
    /// </summary>
    public bool Compress => CompressCheckBox.IsChecked == true;

    /// <summary>
    /// Gets whether to use resource names from filenames.
    /// </summary>
    public bool UseNames => UseNamesCheckBox.IsChecked == true;

    public BatchImportWindow()
    {
        InitializeComponent();

        ImportButton.Click += ImportButton_Click;
        CancelButton.Click += CancelButton_Click;
        AddFilesButton.Click += AddFilesButton_Click;
        AddFolderButton.Click += AddFolderButton_Click;
        RemoveButton.Click += RemoveButton_Click;
        ClearButton.Click += ClearButton_Click;
        FilesListBox.SelectionChanged += FilesListBox_SelectionChanged;
    }

    /// <summary>
    /// Pre-populates the file list.
    /// </summary>
    public void AddFiles(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            if (!_files.Contains(file))
            {
                _files.Add(file);
            }
        }
        UpdateFileList();
    }

    private void UpdateFileList()
    {
        FilesListBox.ItemsSource = null;
        FilesListBox.ItemsSource = _files.Select(f => Path.GetFileName(f)).ToList();
        FileCountLabel.Text = $"{_files.Count} file{(_files.Count != 1 ? "s" : "")}";
        ImportButton.IsEnabled = _files.Count > 0;
    }

    private void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_files.Count == 0)
        {
            return;
        }
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    // Source: legacy_references/Sims4Tools/s4pe/Import/ImportBatch.cs lines 82-87
    private async void AddFilesButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Files to Import",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("All Files") { Patterns = ["*"] },
                new FilePickerFileType("Binary Files") { Patterns = ["*.bin"] },
                new FilePickerFileType("S4 Resource Files") { Patterns = ["S4_*"] },
            ]
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (!_files.Contains(path))
            {
                _files.Add(path);
            }
        }
        UpdateFileList();
    }

    private async void AddFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder to Import",
            AllowMultiple = false
        });

        if (folders.Count == 1)
        {
            var folderPath = folders[0].Path.LocalPath;
            var files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                if (!_files.Contains(file))
                {
                    _files.Add(file);
                }
            }
            UpdateFileList();
        }
    }

    private void RemoveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (FilesListBox.SelectedItems == null) return;

        var indicesToRemove = FilesListBox.SelectedItems
            .OfType<string>()
            .Select(name => _files.FindIndex(f => Path.GetFileName(f) == name))
            .Where(i => i >= 0)
            .OrderByDescending(i => i)
            .ToList();

        foreach (var index in indicesToRemove)
        {
            _files.RemoveAt(index);
        }
        UpdateFileList();
    }

    private void ClearButton_Click(object? sender, RoutedEventArgs e)
    {
        _files.Clear();
        UpdateFileList();
    }

    private void FilesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RemoveButton.IsEnabled = FilesListBox.SelectedItems?.Count > 0;
    }
}
