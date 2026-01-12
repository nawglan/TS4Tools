using Avalonia.Controls;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// A floating window that displays a preview control in a separate window.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2910-2936 (f_FloatControl)
/// </remarks>
public partial class FloatingPreviewWindow : Window
{
    public FloatingPreviewWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the content to display in the floating window.
    /// </summary>
    /// <param name="content">The control to display.</param>
    /// <param name="resourceName">Optional resource name to show in title.</param>
    public void SetContent(Control content, string? resourceName = null)
    {
        ContentHost.Content = content;

        if (!string.IsNullOrEmpty(resourceName))
        {
            Title = $"Preview - {resourceName}";
        }
    }
}
