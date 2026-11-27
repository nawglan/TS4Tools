namespace TS4Tools.UI.ViewModels;

/// <summary>
/// What an Import Batch dialog is importing.
/// </summary>
/// <remarks>Source: legacy_references/Sims4Tools/s4pe/Import/ImportBatch.cs lines 31-37 (Mode)</remarks>
public enum ImportBatchMode
{
    /// <summary>Resource files (and packages) from disk.</summary>
    File,

    /// <summary>Whole packages.</summary>
    Package,

    /// <summary>Resources from the clipboard.</summary>
    Paste,

    /// <summary>Replace the selected resources from packages.</summary>
    ReplaceFrom
}

/// <summary>
/// State of the Import Batch dialog: the list of items to import and the import settings.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/ImportBatch.cs lines 29-111
/// </remarks>
public sealed class ImportBatchViewModel : ViewModelBase
{
    /// <summary>
    /// Creates the dialog state for a list of items.
    /// </summary>
    /// <remarks>
    /// Source: ImportBatch.cs lines 46-58 (replaceFrom forces Replace and disables names/rename;
    /// drag-and-drop is only allowed in file mode).
    /// </remarks>
    public ImportBatchViewModel(IEnumerable<string> items, ImportBatchMode mode, string title)
    {
        Mode = mode;
        Title = title;
        foreach (var item in items)
        {
            Items.Add(item);
        }

        if (mode == ImportBatchMode.ReplaceFrom)
        {
            Settings.ReplaceEnabled = false;
            Settings.Replace = true;
            Settings.UseNameEnabled = false;
            Settings.UseName = false;
            Settings.AllowRenameEnabled = false;
            Settings.AllowRename = false;
        }
    }

    /// <summary>The dialog mode.</summary>
    public ImportBatchMode Mode { get; }

    /// <summary>The dialog title.</summary>
    public string Title { get; }

    /// <summary>The items to import: file paths, or TGIN names for pasted resources.</summary>
    public ObservableCollection<string> Items { get; } = [];

    /// <summary>The import settings.</summary>
    public ImportSettingsViewModel Settings { get; } = new();

    /// <summary>Whether files can be added (dropped or picked) in this mode.</summary>
    public bool AllowAddFiles => Mode == ImportBatchMode.File;

    /// <summary>
    /// Replaces the item list (legacy addDrop, ImportBatch.cs lines 76-81).
    /// </summary>
    public void SetItems(IEnumerable<string> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}
