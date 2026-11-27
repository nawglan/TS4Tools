using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace TS4Tools.UI.Services;

/// <summary>
/// Avalonia implementation of <see cref="IStorageService"/> backed by the desktop main window's
/// <see cref="TopLevel.StorageProvider"/>.
/// </summary>
/// <remarks>
/// Replaces the main form's file dialogs in legacy_references/Sims4Tools/s4pe/MainForm.Designer.cs
/// lines 59-63 and 180-220. This is the only place the UI services look up the application
/// lifetime's main window.
/// </remarks>
public sealed class AvaloniaStorageService : IStorageService
{
    /// <summary>Gets the shared instance.</summary>
    public static AvaloniaStorageService Instance { get; } = new();

    private AvaloniaStorageService()
    {
    }

    /// <inheritdoc />
    public Window? MainWindow =>
        Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> PickOpenFilesAsync(FilePickerOpenOptions options)
    {
        if (MainWindow is not { } owner) return [];
        var files = await owner.StorageProvider.OpenFilePickerAsync(options);
        return StorageItemPaths.LocalPaths(files);
    }

    /// <inheritdoc />
    public async Task<string?> PickSaveFileAsync(FilePickerSaveOptions options, string? startFolder = null)
    {
        if (MainWindow is not { } owner) return null;
        options.SuggestedStartLocation ??= await TryGetFolderAsync(owner.StorageProvider, startFolder);
        var file = await owner.StorageProvider.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(FolderPickerOpenOptions options, string? startFolder = null)
    {
        if (MainWindow is not { } owner) return null;
        options.SuggestedStartLocation ??= await TryGetFolderAsync(owner.StorageProvider, startFolder);
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(options);
        return StorageItemPaths.LocalPaths(folders).FirstOrDefault();
    }

    /// <inheritdoc />
    public void CloseMainWindow() => MainWindow?.Close();

    private static async Task<IStorageFolder?> TryGetFolderAsync(IStorageProvider provider, string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return null;
        try
        {
            return await provider.TryGetFolderFromPathAsync(path);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }
}
