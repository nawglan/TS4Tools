using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.Wrappers;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// View model for an RCOL external resource reference.
/// </summary>
public partial class RcolExternalViewModel : ViewModelBase
{
    private readonly RcolTgiBlock _tgiBlock;

    /// <summary>
    /// The index of this external resource in the list.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The resource type in hex format.
    /// </summary>
    public string TypeHex => $"0x{_tgiBlock.ResourceType:X8}";

    /// <summary>
    /// Human-readable type name.
    /// </summary>
    public string TypeName => RcolConstants.GetTypeName(_tgiBlock.ResourceType);

    /// <summary>
    /// The resource group in hex format.
    /// </summary>
    public string GroupHex => $"0x{_tgiBlock.ResourceGroup:X8}";

    /// <summary>
    /// The instance in hex format.
    /// </summary>
    public string InstanceHex => $"0x{_tgiBlock.Instance:X16}";

    /// <summary>
    /// The TGI as a single string.
    /// </summary>
    public string TgiString => _tgiBlock.ToString();

    /// <summary>
    /// Creates a new external resource view model.
    /// </summary>
    public RcolExternalViewModel(RcolTgiBlock tgiBlock, int index)
    {
        _tgiBlock = tgiBlock;
        Index = index;
    }
}
