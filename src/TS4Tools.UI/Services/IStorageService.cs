using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace TS4Tools.UI.Services;

/// <summary>
/// Access to the main window and its file pickers for view models, so they never reach into the
/// application lifetime themselves and can be tested without a running desktop application.
/// </summary>
/// <remarks>
/// Replaces the WinForms OpenFileDialog/SaveFileDialog/FolderBrowserDialog members owned by the main
/// form in legacy_references/Sims4Tools/s4pe/MainForm.Designer.cs lines 59-63 and 180-220.
/// Paths are local file system paths; items a picker returns that are not on the local file system
/// (e.g. a cloud or sandboxed storage provider) are skipped, so a pick that yields none behaves as
/// a cancelled pick.
/// </remarks>
public interface IStorageService
{
    /// <summary>
    /// Gets the main window, which owns dialogs and the clipboard; null when there is none
    /// (no desktop lifetime, or the window has not been created).
    /// </summary>
    Window? MainWindow { get; }

    /// <summary>Shows an open-file picker and returns the chosen local paths (empty if cancelled).</summary>
    Task<IReadOnlyList<string>> PickOpenFilesAsync(FilePickerOpenOptions options);

    /// <summary>Shows a save-file picker and returns the chosen local path, or null.</summary>
    /// <param name="options">Picker options.</param>
    /// <param name="startFolder">Local folder to start in, if any (ignored when it does not exist).</param>
    Task<string?> PickSaveFileAsync(FilePickerSaveOptions options, string? startFolder = null);

    /// <summary>Shows a folder picker and returns the chosen local folder, or null.</summary>
    /// <param name="options">Picker options.</param>
    /// <param name="startFolder">Local folder to start in, if any (ignored when it does not exist).</param>
    Task<string?> PickFolderAsync(FolderPickerOpenOptions options, string? startFolder = null);

    /// <summary>Closes the main window (which runs its closing prompts).</summary>
    void CloseMainWindow();
}

/// <summary>
/// Helpers for turning storage items into local file system paths.
/// </summary>
public static class StorageItemPaths
{
    /// <summary>
    /// Returns the local paths of <paramref name="items"/>, skipping items that are not on the
    /// local file system (<see cref="StorageProviderExtensions.TryGetLocalPath"/> returns null).
    /// </summary>
    public static List<string> LocalPaths(IEnumerable<IStorageItem>? items)
    {
        if (items == null) return [];
        var paths = new List<string>();
        foreach (var item in items)
        {
            if (item.TryGetLocalPath() is { Length: > 0 } path)
            {
                paths.Add(path);
            }
        }

        return paths;
    }
}
