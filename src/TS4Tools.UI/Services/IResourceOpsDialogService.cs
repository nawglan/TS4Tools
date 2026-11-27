using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Services;

/// <summary>
/// Answer to the "Overwrite file?" prompt of a batch export.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1835-1858
/// (buttons "No", "No to all", "Yes", "Yes to all", "Abandon").
/// </remarks>
public enum OverwritePromptResult
{
    No,
    NoToAll,
    Yes,
    YesToAll,
    Abandon
}

/// <summary>
/// Answer to a Yes/No/Cancel question.
/// </summary>
public enum YesNoCancelResult
{
    Yes,
    No,
    Cancel
}

/// <summary>
/// Answer to the "replace duplicate resources in the target package?" prompt of Export to package.
/// </summary>
/// <remarks>Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1925-1936 ("Reject", "Replace", "Abandon")</remarks>
public enum ExportDuplicatesPromptResult
{
    Reject,
    Replace,
    Abandon
}

/// <summary>
/// User interaction needed by the resource operations (add, import, export, copy/paste, details),
/// abstracted so the operations can be tested without UI.
/// </summary>
public interface IResourceOpsDialogService
{
    /// <summary>Shows the Resource Details dialog. Returns true if OK was pressed.</summary>
    Task<bool> ShowResourceDetailsAsync(ResourceDetailsViewModel details);

    /// <summary>Shows the Import Batch dialog. Returns true if OK was pressed.</summary>
    Task<bool> ShowImportBatchAsync(ImportBatchViewModel batch);

    /// <summary>Asks for files to import.</summary>
    Task<IReadOnlyList<string>> PickImportFilesAsync(string title, bool allowMultiple);

    /// <summary>Asks for packages to import from.</summary>
    Task<IReadOnlyList<string>> PickPackagesAsync(string title);

    /// <summary>Asks for one file to replace a resource with.</summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="resourceType">The resource's type, used for the file filters.</param>
    Task<string?> PickReplaceFileAsync(string title, uint resourceType);

    /// <summary>Asks where to export a single resource.</summary>
    /// <param name="suggestedFileName">TGIN file name to suggest.</param>
    /// <param name="startFolder">Folder to start in (the last export folder), if any.</param>
    Task<string?> PickExportFileAsync(string suggestedFileName, string? startFolder = null);

    /// <summary>Asks for a folder to export resources to.</summary>
    /// <param name="startFolder">Folder to start in (the last export folder), if any.</param>
    Task<string?> PickExportFolderAsync(string? startFolder = null);

    /// <summary>Asks for a package to export resources to.</summary>
    Task<string?> PickExportPackageAsync();

    /// <summary>Asks whether an existing export file should be overwritten.</summary>
    Task<OverwritePromptResult> AskOverwriteAsync(string path);

    /// <summary>Asks a Yes/No/Cancel question.</summary>
    Task<YesNoCancelResult> AskYesNoCancelAsync(string message, string title);

    /// <summary>Asks whether duplicates in an existing export target should be replaced.</summary>
    Task<ExportDuplicatesPromptResult> AskExportDuplicatesAsync();

    /// <summary>
    /// Asks the user to pick one of several options. Returns the chosen index, or
    /// <paramref name="cancelIndex"/> if the prompt was dismissed.
    /// </summary>
    Task<int> AskChoiceAsync(string message, string title, IReadOnlyList<string> options, int defaultIndex, int cancelIndex);

    /// <summary>
    /// Shows the experimental DBC warning. Returns true if the user confirmed ("Yes, proceed").
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/ExperimentalDBCWarning.cs</remarks>
    Task<bool> ConfirmDbcWarningAsync();

    /// <summary>
    /// Lets the user pick images and converts them to JFIF+ALFA thumbnails. Returns the converted
    /// files (file name, thumbnail data); empty if cancelled.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe Helpers/ThumbnailHelper/ImportThumb.cs</remarks>
    Task<IReadOnlyList<(string FileName, byte[] Data)>> ConvertThumbnailImagesAsync();

    /// <summary>Shows a message.</summary>
    Task ShowMessageAsync(string message, string title);

    /// <summary>Puts text on the system clipboard.</summary>
    Task SetClipboardTextAsync(string text);

    /// <summary>Puts copied resources (serialized) and their keys (text) on the system clipboard.</summary>
    Task SetClipboardResourcesAsync(byte[] payload, string keysText);

    /// <summary>Gets copied resources (serialized) from the system clipboard, if any.</summary>
    Task<byte[]?> GetClipboardResourcesAsync();

    /// <summary>Gets file paths from the system clipboard, if any.</summary>
    Task<IReadOnlyList<string>> GetClipboardFilesAsync();
}
