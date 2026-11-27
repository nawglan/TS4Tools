using Avalonia.Controls;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// The resource filter: a row per filterable column plus the Filter, Revise, Set, QBE and Paste RK
/// controls and the count of listed resources. It binds to <c>MainWindowViewModel</c>.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Filter/ResourceFilterWidget.cs and FilterField.cs
/// </remarks>
public partial class ResourceFilterPanel : UserControl
{
    public ResourceFilterPanel()
    {
        InitializeComponent();
    }
}
