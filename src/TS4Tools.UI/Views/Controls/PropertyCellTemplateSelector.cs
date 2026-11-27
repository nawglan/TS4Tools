using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Selects the appropriate cell template based on PropertyItem metadata.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs
/// In legacy s4pe, UITypeEditor subclasses handled custom property editing.
/// This selector provides equivalent functionality via DataTemplates.
/// </remarks>
public sealed class PropertyCellTemplateSelector : IDataTemplate
{
    /// <summary>
    /// Default text editing template.
    /// </summary>
    [Content]
    public IDataTemplate? DefaultTemplate { get; set; }

    /// <summary>
    /// Template for TGI block index selection.
    /// </summary>
    public IDataTemplate? TgiBlockSelectorTemplate { get; set; }

    /// <summary>
    /// Template for binary data editing.
    /// </summary>
    public IDataTemplate? BinaryEditorTemplate { get; set; }

    /// <summary>
    /// Template for read-only properties.
    /// </summary>
    public IDataTemplate? ReadOnlyTemplate { get; set; }

    /// <summary>
    /// Template for bool properties (check box).
    /// </summary>
    public IDataTemplate? BooleanTemplate { get; set; }

    /// <summary>
    /// Template for non-flags enum properties (drop-down).
    /// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 588-727 (EnumChooserCTD)
    /// </summary>
    public IDataTemplate? EnumTemplate { get; set; }

    public Control? Build(object? param)
    {
        if (param is not PropertyItem item)
            return DefaultTemplate?.Build(param);

        // Select template based on property metadata
        if (item.HasTgiBlockSelector && TgiBlockSelectorTemplate != null)
            return TgiBlockSelectorTemplate.Build(param);

        if (item.IsBinaryData && BinaryEditorTemplate != null)
            return BinaryEditorTemplate.Build(param);

        if (item.EditorKind == PropertyEditorKind.Boolean && BooleanTemplate != null)
            return BooleanTemplate.Build(param);

        if (item.EditorKind == PropertyEditorKind.Enum && EnumTemplate != null)
            return EnumTemplate.Build(param);

        if (item.IsReadOnly && ReadOnlyTemplate != null)
            return ReadOnlyTemplate.Build(param);

        return DefaultTemplate?.Build(param);
    }

    public bool Match(object? data) => data is PropertyItem;
}
