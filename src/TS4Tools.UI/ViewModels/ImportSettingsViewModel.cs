using CommunityToolkit.Mvvm.ComponentModel;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// How to handle a resource whose key already exists in the package.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1689-1708 (DuplicateHandling enum)
/// </remarks>
public enum ImportDuplicateHandling
{
    /// <summary>Refuse to create the requested resource.</summary>
    Reject,

    /// <summary>Delete (mark deleted) any conflicting resource.</summary>
    Replace,

    /// <summary>Ignore any conflicting resource and add a duplicate.</summary>
    Allow
}

/// <summary>
/// The import options panel shared by the Resource Details and Import Batch dialogs.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/ImportSettings.cs lines 29-97 and
/// ImportSettings.Designer.cs lines 64-135 (defaults: Replace duplicates, Compress and
/// Use resource name checked; Rename if present unchecked and only enabled with Use name).
/// </remarks>
public sealed partial class ImportSettingsViewModel : ViewModelBase
{
    /// <summary>"Replace duplicates" (true) or "Reject duplicates" (false).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Reject))]
    private bool _replace = true;

    /// <summary>"Compress".</summary>
    [ObservableProperty]
    private bool _compress = true;

    /// <summary>"Use resource name".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAllowRename))]
    private bool _useName = true;

    /// <summary>"Rename if present".</summary>
    [ObservableProperty]
    private bool _allowRename;

    /// <summary>Whether the Replace/Reject choice can be changed.</summary>
    [ObservableProperty]
    private bool _replaceEnabled = true;

    /// <summary>Whether the Compress option can be changed.</summary>
    [ObservableProperty]
    private bool _compressEnabled = true;

    /// <summary>Whether the Use resource name option can be changed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAllowRename))]
    private bool _useNameEnabled = true;

    /// <summary>Whether the Rename if present option can be changed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAllowRename))]
    private bool _allowRenameEnabled = true;

    /// <summary>"Reject duplicates" radio button (the inverse of <see cref="Replace"/>).</summary>
    public bool Reject
    {
        get => !Replace;
        set => Replace = !value;
    }

    /// <summary>
    /// Rename is only available when names are used (ImportSettings.cs line 33, 94).
    /// </summary>
    public bool CanAllowRename => AllowRenameEnabled && UseName;

    /// <summary>The duplicate handling selected by the Replace/Reject choice.</summary>
    public ImportDuplicateHandling DuplicateHandling =>
        Replace ? ImportDuplicateHandling.Replace : ImportDuplicateHandling.Reject;
}
