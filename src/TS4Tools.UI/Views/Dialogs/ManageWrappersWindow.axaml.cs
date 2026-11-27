// Source: legacy_references/Sims4Tools/s4pe/Settings/ManageWrappersDialog.cs lines 33-185
// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2406-2409 (SettingsManageWrappers)

using Avalonia.Controls;
using Avalonia.Interactivity;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Manage Wrappers dialog. OK applies the disabled set to the live registry and saves the settings.
/// </summary>
public partial class ManageWrappersWindow : Window
{
    private readonly ManageWrappersViewModel _viewModel;

    public ManageWrappersWindow()
        : this(new ManageWrappersViewModel())
    {
    }

    public ManageWrappersWindow(ManageWrappersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        WrappersView.DataContext = viewModel;
    }

    private async void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.Apply(SettingsService.Instance.Settings);
        await SettingsService.Instance.SaveAsync();
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
