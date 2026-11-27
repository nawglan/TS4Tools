using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using TS4Tools.Extensions;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.Views.Dialogs;

namespace TS4Tools.UI.Services;

/// <summary>
/// Avalonia implementation of <see cref="IResourceOpsDialogService"/>: modal dialogs and pickers owned
/// by the main window, and the system clipboard.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.Designer.cs lines 191-220 (file dialog titles
/// and filters) and legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 42-43 (the s4pe
/// clipboard formats, here a cross-platform application format).
/// </remarks>
public sealed class ResourceOpsDialogService : IResourceOpsDialogService
{
    private const string AppName = "TS4Tools";

    /// <summary>
    /// Clipboard format for copied resources, shared between TS4Tools instances.
    /// </summary>
    // Source: legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 42-43 ("x-application/s3pe.batch")
    private static readonly DataFormat<byte[]> ResourcesFormat =
        DataFormat.CreateBytesApplicationFormat("ts4tools.resources");

    // Source: MainForm.Designer.cs lines 191, 200 ("Exported files (S4_*.*)|S4_*.*|All files (*.*)|*.*")
    private static readonly FilePickerFileType ExportedFilesType = new("Exported files (S4_*.*)") { Patterns = ["S4_*.*"] };
    private static readonly FilePickerFileType AllFilesType = new("All files (*.*)") { Patterns = ["*"] };
    // Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings DBPFFilesAndAll
    private static readonly FilePickerFileType PackageFileType = new("Sims 4 Package") { Patterns = ["*.package", "*.dbc", "*.world", "*.nhd"] };

    /// <summary>Gets the shared instance.</summary>
    public static ResourceOpsDialogService Instance { get; } = new();

    private ResourceOpsDialogService()
    {
    }

    /// <inheritdoc />
    public async Task<bool> ShowResourceDetailsAsync(ResourceDetailsViewModel details)
    {
        var owner = GetMainWindow();
        if (owner == null) return false;
        var dialog = new ResourceDetailsWindow { DataContext = details };
        return await dialog.ShowDialog<bool>(owner);
    }

    /// <inheritdoc />
    public async Task<bool> ShowImportBatchAsync(ImportBatchViewModel batch)
    {
        var owner = GetMainWindow();
        if (owner == null) return false;
        var dialog = new BatchImportWindow { DataContext = batch };
        return await dialog.ShowDialog<bool>(owner);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> PickImportFilesAsync(string title, bool allowMultiple)
    {
        var owner = GetMainWindow();
        if (owner == null) return [];
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = [ExportedFilesType, AllFilesType]
        });
        return LocalPaths(files);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> PickPackagesAsync(string title)
    {
        var owner = GetMainWindow();
        if (owner == null) return [];
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = [PackageFileType, AllFilesType]
        });
        return LocalPaths(files);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1640-1660 (filters "TAG by type|S4_TTTTTTTT*.*",
    /// "TAG by ext|*.ext", "All files|*.*")
    /// </remarks>
    public async Task<string?> PickReplaceFileAsync(string title, uint resourceType)
    {
        var owner = GetMainWindow();
        if (owner == null) return null;
        var ext = ExtList.Get(resourceType);
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType($"{ext[0]} by type") { Patterns = [$"S4_{resourceType:X8}*.*"] },
                new FilePickerFileType($"{ext[0]} by ext") { Patterns = [$"*{ext[^1]}"] },
                AllFilesType
            ]
        });
        return LocalPaths(files).FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<string?> PickExportFileAsync(string suggestedFileName, string? startFolder = null) =>
        AvaloniaStorageService.Instance.PickSaveFileAsync(new FilePickerSaveOptions
        {
            Title = "Export File",
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = [ExportedFilesType, AllFilesType]
        }, startFolder);

    /// <inheritdoc />
    public Task<string?> PickExportFolderAsync(string? startFolder = null) =>
        AvaloniaStorageService.Instance.PickFolderAsync(new FolderPickerOpenOptions
        {
            Title = "Export to folder",
            AllowMultiple = false
        }, startFolder);

    /// <inheritdoc />
    public Task<string?> PickExportPackageAsync() =>
        AvaloniaStorageService.Instance.PickSaveFileAsync(new FilePickerSaveOptions
        {
            Title = "Export to package",
            DefaultExtension = "package",
            FileTypeChoices = [PackageFileType, AllFilesType],
            ShowOverwritePrompt = false
        });

    /// <inheritdoc />
    public async Task<OverwritePromptResult> AskOverwriteAsync(string path)
    {
        // Source: MainForm.cs lines 1835-1841 (default "No", cancel "Abandon")
        var choice = await ShowButtonsAsync($"Overwrite file?\n{path}", AppName,
            ["_No", "N_o to all", "_Yes", "Y_es to all", "_Abandon"], defaultIndex: 0, cancelIndex: 4);
        return (OverwritePromptResult)choice;
    }

    /// <inheritdoc />
    public async Task<YesNoCancelResult> AskYesNoCancelAsync(string message, string title)
    {
        var choice = await ShowButtonsAsync(message, title, ["_Yes", "_No", "Cancel"], defaultIndex: 0, cancelIndex: 2);
        return (YesNoCancelResult)choice;
    }

    /// <inheritdoc />
    public async Task<ExportDuplicatesPromptResult> AskExportDuplicatesAsync()
    {
        // Source: MainForm.cs lines 1925-1931 (default "Reject", cancel "Abandon")
        var choice = await ShowButtonsAsync(
            "Do you want to replace any duplicate resources in the target package discovered during export?",
            "Export to package", ["Re_ject", "Re_place", "_Abandon"], defaultIndex: 0, cancelIndex: 2);
        return (ExportDuplicatesPromptResult)choice;
    }

    /// <inheritdoc />
    public Task<int> AskChoiceAsync(string message, string title, IReadOnlyList<string> options, int defaultIndex, int cancelIndex)
        => ShowButtonsAsync(message, title, [.. options], defaultIndex, cancelIndex);

    /// <inheritdoc />
    public async Task<bool> ConfirmDbcWarningAsync()
    {
        var owner = GetMainWindow();
        if (owner == null) return false;
        var dialog = new DbcWarningWindow();
        var result = await dialog.ShowDialog<bool?>(owner);
        return result == true && dialog.Confirmed;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(string FileName, byte[] Data)>> ConvertThumbnailImagesAsync()
    {
        var owner = GetMainWindow();
        if (owner == null) return [];
        var dialog = new ThumbnailHelperWindow();
        var result = await dialog.ShowDialog<bool?>(owner);
        return result == true ? dialog.ConvertedFiles : [];
    }

    /// <inheritdoc />
    public async Task ShowMessageAsync(string message, string title)
    {
        await ShowButtonsAsync(message, title, ["OK"], defaultIndex: 0, cancelIndex: 0);
    }

    /// <inheritdoc />
    public async Task SetClipboardTextAsync(string text)
    {
        if (GetMainWindow()?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    /// <inheritdoc />
    public async Task SetClipboardResourcesAsync(byte[] payload, string keysText)
    {
        if (GetMainWindow()?.Clipboard is not { } clipboard) return;

        var item = new DataTransferItem();
        item.Set(ResourcesFormat, payload);
        item.SetText(keysText);
        var data = new DataTransfer();
        data.Add(item);
        await clipboard.SetDataAsync(data);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetClipboardResourcesAsync()
    {
        if (GetMainWindow()?.Clipboard is not { } clipboard) return null;
        try
        {
            return await clipboard.TryGetValueAsync(ResourcesFormat);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or COMException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetClipboardFilesAsync()
    {
        if (GetMainWindow()?.Clipboard is not { } clipboard) return [];
        try
        {
            var files = await clipboard.TryGetFilesAsync();
            return files == null ? [] : LocalPaths(files);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or COMException)
        {
            return [];
        }
    }

    private static List<string> LocalPaths(IEnumerable<IStorageItem> items) => StorageItemPaths.LocalPaths(items);

    private static Window? GetMainWindow() => AvaloniaStorageService.Instance.MainWindow;

    /// <summary>
    /// Shows a modal message with the given buttons. Returns the index of the button pressed
    /// (or <paramref name="cancelIndex"/> if the window was closed).
    /// </summary>
    private static async Task<int> ShowButtonsAsync(string message, string title, string[] labels, int defaultIndex, int cancelIndex)
    {
        var owner = GetMainWindow();
        if (owner == null) return cancelIndex;

        var result = cancelIndex;
        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 320,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 16, 0, 0)
        };

        for (var i = 0; i < labels.Length; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = labels[i],
                MinWidth = 72,
                IsDefault = i == defaultIndex,
                IsCancel = i == cancelIndex
            };
            button.Click += (_, _) =>
            {
                result = index;
                dialog.Close();
            };
            buttons.Children.Add(button);
        }

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 480 },
                buttons
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
