using Avalonia.Controls;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Shows the header fields of the open package. It binds to <c>MainWindowViewModel.PackageInfo</c>.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/PackageInfo/PackageInfoWidget.cs
/// </remarks>
public partial class PackageInfoPanel : UserControl
{
    public PackageInfoPanel()
    {
        InitializeComponent();
    }
}
