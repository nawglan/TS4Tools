using Avalonia.Controls;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.Views.Controls;

namespace TS4Tools.UI.Views;

public partial class MainWindow : Window, IAsyncDisposable
{
    private MainWindowViewModel? _viewModel;
    private bool _disposed;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        // Wire up the advanced filter panel
        FilterPanel.FilterChanged += (sender, args) =>
        {
            _viewModel.ApplyAdvancedFilter(args);
        };

        // Wire up control panel events
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs control panel event handlers
        WireControlPanelEvents();

        Closed += OnWindowClosed;
    }

    /// <summary>
    /// Wires up control panel toolbar events.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs
    /// </remarks>
    private void WireControlPanelEvents()
    {
        ControlPanel.HexClick += (_, _) => _viewModel?.ShowHexView();
        ControlPanel.ValueClick += (_, _) => _viewModel?.ShowValueView();
        ControlPanel.GridClick += (_, _) => _viewModel?.TogglePropertyGridCommand.Execute(null);
        ControlPanel.Helper1Click += async (_, _) => await ExecuteHelperAsync(0);
        ControlPanel.Helper2Click += async (_, _) => await ExecuteHelperAsync(1);
        ControlPanel.HexEditClick += async (_, _) => await _viewModel?.ExecuteHexEditorAsync()!;
        ControlPanel.CommitClick += async (_, _) => await _viewModel?.SaveCommand.ExecuteAsync(null)!;

        // Display option changes
        ControlPanel.SortChanged += (_, _) => _viewModel?.OnSortOptionChanged(ControlPanel.Sort);
        ControlPanel.HexOnlyChanged += (_, _) => _viewModel?.OnHexOnlyOptionChanged(ControlPanel.HexOnly);
        ControlPanel.UseNamesChanged += (_, _) => _viewModel?.OnUseNamesOptionChanged(ControlPanel.UseNames);
        ControlPanel.UseTagsChanged += (_, _) => _viewModel?.OnUseTagsOptionChanged(ControlPanel.UseTags);
        ControlPanel.AutoChanged += (_, _) => _viewModel?.OnAutoModeChanged(ControlPanel.AutoChoice);

        // Subscribe to ViewModel changes to update control panel helper buttons
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.AvailableHelpers))
                {
                    UpdateControlPanelHelpers();
                }
                else if (args.PropertyName == nameof(MainWindowViewModel.HasSelectedResource))
                {
                    ControlPanel.SetButtonsEnabled(_viewModel.HasSelectedResource);
                }
            };
        }
    }

    private void UpdateControlPanelHelpers()
    {
        if (_viewModel == null) return;

        var helpers = _viewModel.AvailableHelpers;
        if (helpers.Count > 0)
        {
            var h1 = helpers[0];
            ControlPanel.SetHelper1(true, h1.Label, h1.Description);
        }
        else
        {
            ControlPanel.SetHelper1(false, "Helper1", "");
        }

        if (helpers.Count > 1)
        {
            var h2 = helpers[1];
            ControlPanel.SetHelper2(true, h2.Label, h2.Description);
        }
        else
        {
            ControlPanel.SetHelper2(false, "Helper2", "");
        }
    }

    private async Task ExecuteHelperAsync(int index)
    {
        if (_viewModel?.AvailableHelpers.Count > index)
        {
            await _viewModel.AvailableHelpers[index].ExecuteCommand.ExecuteAsync(null);
        }
    }

    private async void OnWindowClosed(object? sender, EventArgs e)
    {
        await DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_viewModel != null)
        {
            await _viewModel.DisposeAsync().ConfigureAwait(false);
            _viewModel = null;
        }

        GC.SuppressFinalize(this);
    }
}
