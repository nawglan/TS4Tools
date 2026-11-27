using Avalonia.Controls;
using Avalonia.VisualTree;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Finds the text a user has selected in a preview.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1030-1045 (CanCopy: a RichTextBox
/// with a selection) and 1052-1092 (EditCopy copies the selected text).
/// </remarks>
internal static class PreviewTextSelection
{
    /// <summary>Gets the selected text of the first text control in <paramref name="root"/> that has a selection.</summary>
    public static string? Find(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);

        foreach (var control in root.GetSelfAndVisualDescendants())
        {
            var selected = control switch
            {
                TextBox textBox => textBox.SelectedText,
                SelectableTextBlock block => block.SelectedText,
                _ => null
            };

            if (!string.IsNullOrEmpty(selected)) return selected;
        }

        return null;
    }
}
