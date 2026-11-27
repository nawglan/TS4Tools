using Avalonia.Controls;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Keeps the multiple selection of the browser grid and <see cref="MainWindowViewModel.SelectedResources"/>
/// equal in both directions.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 236-285
/// (SelectedResources) and 727-760 (listView1_SelectedIndexChanged).
/// </remarks>
internal sealed class BrowserSelectionSync : IDisposable
{
    private readonly DataGrid _grid;
    private readonly MainWindowViewModel _viewModel;
    private readonly EventHandler<SelectionChangedEventArgs> _gridHandler;
    private readonly NotifyCollectionChangedEventHandler _viewModelHandler;
    private bool _updating;
    private bool _disposed;

    public BrowserSelectionSync(DataGrid grid, MainWindowViewModel viewModel)
    {
        _grid = grid;
        _viewModel = viewModel;
        _gridHandler = OnGridSelectionChanged;
        _viewModelHandler = OnViewModelSelectionChanged;
        _grid.SelectionChanged += _gridHandler;
        _viewModel.SelectedResources.CollectionChanged += _viewModelHandler;
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updating) return;

        _updating = true;
        try
        {
            foreach (var removed in e.RemovedItems.OfType<ResourceItemViewModel>())
            {
                _viewModel.SelectedResources.Remove(removed);
            }

            foreach (var added in e.AddedItems.OfType<ResourceItemViewModel>())
            {
                if (!_viewModel.SelectedResources.Contains(added))
                {
                    _viewModel.SelectedResources.Add(added);
                }
            }
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnViewModelSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_updating) return;

        _updating = true;
        try
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems?.OfType<ResourceItemViewModel>() ?? [])
                    {
                        if (!_grid.SelectedItems.Contains(item) && _viewModel.FilteredResources.Contains(item))
                        {
                            _grid.SelectedItems.Add(item);
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems?.OfType<ResourceItemViewModel>() ?? [])
                    {
                        _grid.SelectedItems.Remove(item);
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    _grid.SelectedItems.Clear();
                    break;
            }
        }
        finally
        {
            _updating = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _grid.SelectionChanged -= _gridHandler;
        _viewModel.SelectedResources.CollectionChanged -= _viewModelHandler;
    }
}
