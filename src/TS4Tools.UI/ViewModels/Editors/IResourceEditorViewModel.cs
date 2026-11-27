namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// An editor view model whose edits can be committed back to the package.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 700-705 (resource_ResourceChanged)
/// and lines 3060-3073 (controlPanel1_CommitClick).
/// Legacy s4pe listens to IResource.ResourceChanged to enable Commit, then writes the
/// (edited) resource back with IPackage.ReplaceResource. Editors that keep pending state
/// of their own (e.g. the text editor buffer) raise <see cref="ContentChanged"/> and
/// flush that state in <see cref="GetData"/>.
/// </remarks>
public interface IResourceEditorViewModel
{
    /// <summary>
    /// Raised when the user changes the resource content in the editor.
    /// </summary>
    event EventHandler? ContentChanged;

    /// <summary>
    /// Gets the serialized resource data including all edits made in the editor.
    /// </summary>
    ReadOnlyMemory<byte> GetData();
}
