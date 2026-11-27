namespace TS4Tools.UI.Services;

/// <summary>
/// Answer to the "Commit changes to ...?" prompt.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2609-2642
/// (YesNoCancel: Yes = commit, No = discard, Cancel = keep current selection).
/// </remarks>
public enum CommitPromptResult
{
    Commit,
    Discard,
    Cancel
}

/// <summary>
/// Answer to the "Current package has unsaved changes. Save now?" prompt.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 643-672
/// (YesNoCancel: Yes = save, No = close without saving, Cancel = abort).
/// </remarks>
public enum SavePromptResult
{
    Save,
    DontSave,
    Cancel
}

/// <summary>
/// User prompts needed by the main window view model, abstracted so view model
/// logic (commit/save prompts, save-as path selection) can be tested without UI.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Asks whether uncommitted edits to a resource should be committed.
    /// </summary>
    /// <param name="resourceName">Display name of the resource (name or TGI).</param>
    Task<CommitPromptResult> AskCommitChangesAsync(string resourceName);

    /// <summary>
    /// Asks whether the current package's unsaved changes should be saved.
    /// </summary>
    Task<SavePromptResult> AskSaveChangesAsync();

    /// <summary>
    /// Asks the user for a path to save a package to.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="suggestedFileName">Suggested file name, if any.</param>
    /// <returns>The chosen local path, or null if cancelled.</returns>
    Task<string?> PickSavePackagePathAsync(string title, string? suggestedFileName = null);
}
