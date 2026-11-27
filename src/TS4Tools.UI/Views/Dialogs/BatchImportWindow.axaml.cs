using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Import Batch dialog: the list of files (or pasted resources, or packages) to import and the import settings.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/ImportBatch.cs. Files can be dropped on the dialog
/// in file mode (ImportBatch.cs lines 48, 83-94).
/// </remarks>
public partial class BatchImportWindow : Window
{
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
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, Window_DragOver);
        AddHandler(DragDrop.DropEvent, Window_Drop);
        Closed += OnClosed;
    }

    private ImportBatchViewModel? ViewModel => DataContext as ImportBatchViewModel;

    private void OnClosed(object? sender, EventArgs e)
    {
        ImportButton.Click -= ImportButton_Click;
        CancelButton.Click -= CancelButton_Click;
        AddFilesButton.Click -= AddFilesButton_Click;
        AddFolderButton.Click -= AddFolderButton_Click;
        RemoveButton.Click -= RemoveButton_Click;
        ClearButton.Click -= ClearButton_Click;
        FilesListBox.SelectionChanged -= FilesListBox_SelectionChanged;
        RemoveHandler(DragDrop.DragOverEvent, Window_DragOver);
        RemoveHandler(DragDrop.DropEvent, Window_Drop);
        Closed -= OnClosed;
    }

    private void ImportButton_Click(object? sender, RoutedEventArgs e) => Close(ViewModel?.Items.Count > 0);

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(false);

    // Source: ImportBatch.cs lines 83-87 (DragOver: copy when files are dragged, file mode only)
    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = ViewModel?.AllowAddFiles == true && e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    // Source: ImportBatch.cs lines 89-94 (DragDrop replaces the list)
    private void Window_Drop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        if (ViewModel is not { AllowAddFiles: true } vm) return;
        var files = e.DataTransfer.TryGetFiles();
        if (files == null) return;
        var paths = StorageItemPaths.LocalPaths(files);
        if (paths.Count > 0)
        {
            vm.SetItems(paths);
        }
    }

    private async void AddFilesButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Files to Import",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Exported files (S4_*.*)") { Patterns = ["S4_*.*"] },
                new FilePickerFileType("All files (*.*)") { Patterns = ["*"] },
            ]
        });

        foreach (var path in StorageItemPaths.LocalPaths(files))
        {
            if (!vm.Items.Contains(path))
            {
                vm.Items.Add(path);
            }
        }
    }

    private async void AddFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder to Import",
            AllowMultiple = false
        });

        if (folders.Count != 1 || folders[0].TryGetLocalPath() is not { } folder) return;

        // Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 639-652 (folders are imported recursively).
        // The tree is walked off the UI thread so a large folder does not freeze the dialog.
        List<string> found;
        try
        {
            found = await Task.Run(() => Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToList());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var file in found)
        {
            if (!vm.Items.Contains(file))
            {
                vm.Items.Add(file);
            }
        }
    }

    private void RemoveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm || FilesListBox.SelectedItems == null) return;

        foreach (var item in FilesListBox.SelectedItems.OfType<string>().ToList())
        {
            vm.Items.Remove(item);
        }
    }

    private void ClearButton_Click(object? sender, RoutedEventArgs e) => ViewModel?.Items.Clear();

    private void FilesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RemoveButton.IsEnabled = FilesListBox.SelectedItems?.Count > 0;
    }
}
