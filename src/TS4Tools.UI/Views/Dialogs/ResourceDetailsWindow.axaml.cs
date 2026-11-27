using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Resource Details dialog: key, name and import settings of a new, imported or existing resource.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/ResourceDetails.cs. Logic lives in
/// <see cref="ResourceDetailsViewModel"/>; this view handles the clipboard and file drops.
/// </remarks>
public partial class ResourceDetailsWindow : Window
{
    private ResourceDetailsViewModel? _viewModel;

    public ResourceDetailsWindow()
    {
        InitializeComponent();

        CopyKeyButton.Click += CopyKeyButton_Click;
        PasteKeyButton.Click += PasteKeyButton_Click;
        FilenameTextBox.AddHandler(DragDrop.DragOverEvent, Filename_DragOver);
        FilenameTextBox.AddHandler(DragDrop.DropEvent, Filename_Drop);
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as ResourceDetailsViewModel;
        if (_viewModel != null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, bool ok) => Close(ok);

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
            _viewModel = null;
        }

        CopyKeyButton.Click -= CopyKeyButton_Click;
        PasteKeyButton.Click -= PasteKeyButton_Click;
        FilenameTextBox.RemoveHandler(DragDrop.DragOverEvent, Filename_DragOver);
        FilenameTextBox.RemoveHandler(DragDrop.DropEvent, Filename_Drop);
        DataContextChanged -= OnDataContextChanged;
        Closed -= OnClosed;
    }

    // Source: ResourceDetails.cs lines 214-219 (Copy RK)
    private async void CopyKeyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null && Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(_viewModel.KeyText);
        }
    }

    // Source: ResourceDetails.cs lines 225-238 (Paste RK)
    private async void PasteKeyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null && Clipboard is { } clipboard)
        {
            _viewModel.TryPasteKey(await clipboard.TryGetTextAsync());
        }
    }

    // Source: ResourceDetails.cs lines 172-183 (tbFilename_DragOver / DragDrop)
    private void Filename_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Filename_Drop(object? sender, DragEventArgs e)
    {
        var path = StorageItemPaths.LocalPaths(e.DataTransfer.TryGetFiles()).FirstOrDefault();
        if (path != null && _viewModel != null)
        {
            _viewModel.Filename = path;
        }
        e.Handled = true;
    }
}
