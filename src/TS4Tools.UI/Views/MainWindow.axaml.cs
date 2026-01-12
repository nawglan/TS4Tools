using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Input;
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

        // Wire up selection sync for Select All functionality
        // Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs
        WireSelectionSync();

        // Wire up quick access keyboard shortcuts
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs keyboard shortcuts
        WireQuickAccessShortcuts();

        Closed += OnWindowClosed;
    }

    /// <summary>
    /// Wires up Ctrl+1-9 for recent files and Ctrl+Shift+1-9 for bookmarks.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs MRU/Bookmark handling
    /// </remarks>
    private void WireQuickAccessShortcuts()
    {
        KeyDown += async (sender, e) =>
        {
            if (_viewModel == null) return;

            // Check for digit key (D1-D9)
            if (e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                var index = e.Key - Key.D1; // 0-8

                if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
                {
                    // Ctrl+Shift+1-9: Open bookmark
                    if (index < _viewModel.Bookmarks.Count)
                    {
                        await _viewModel.OpenBookmarkCommand.ExecuteAsync(_viewModel.Bookmarks[index].RawEntry);
                        e.Handled = true;
                    }
                }
                else if (e.KeyModifiers == KeyModifiers.Control)
                {
                    // Ctrl+1-9: Open recent file
                    if (index < _viewModel.RecentFiles.Count)
                    {
                        await _viewModel.OpenRecentFileCommand.ExecuteAsync(_viewModel.RecentFiles[index].Path);
                        e.Handled = true;
                    }
                }
            }
        };
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

    /// <summary>
    /// Wires up selection synchronization between ListBox and ViewModel.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 236-285
    /// </remarks>
    private void WireSelectionSync()
    {
        if (_viewModel == null) return;

        // Subscribe to SelectedResources changes from ViewModel (for SelectAll command)
        _viewModel.SelectedResources.CollectionChanged += (_, args) =>
        {
            if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                // When items are added programmatically, select them in the ListBox
                foreach (var item in args.NewItems ?? Array.Empty<object>())
                {
                    if (item is ResourceItemViewModel resource &&
                        !ResourceListBox.Selection.SelectedItems.Contains(resource))
                    {
                        var index = ResourceListBox.ItemsSource?.Cast<ResourceItemViewModel>()
                            .ToList().IndexOf(resource) ?? -1;
                        if (index >= 0)
                        {
                            ResourceListBox.Selection.Select(index);
                        }
                    }
                }
            }
            else if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                // Clear was called - don't clear UI selection as new items will be added
            }
        };

        // Subscribe to ListBox selection changes
        ResourceListBox.SelectionChanged += (_, args) =>
        {
            // Update ViewModel's SelectedResources from ListBox selection
            // (but avoid recursion when ViewModel is updating ListBox)
            var selectedItems = ResourceListBox.Selection.SelectedItems
                .OfType<ResourceItemViewModel>()
                .ToList();

            // Only sync if different from current SelectedResources
            if (!selectedItems.SequenceEqual(_viewModel.SelectedResources))
            {
                _viewModel.SelectedResources.Clear();
                foreach (var item in selectedItems)
                {
                    _viewModel.SelectedResources.Add(item);
                }
            }
        };
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
