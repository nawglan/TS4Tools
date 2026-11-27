using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.Views.Controls;

namespace TS4Tools.UI.Views;

public partial class MainWindow : Window, IAsyncDisposable
{
    private MainWindowViewModel? _viewModel;
    private bool _disposed;
    private bool _closeConfirmed;
    private bool _closePromptActive;
    private PropertyChangedEventHandler? _viewModelPropertyChangedHandler;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        // Wire up control panel events
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs control panel event handlers
        WireControlPanelEvents();

        // Wire up selection sync for Select All functionality
        // Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs
        WireSelectionSync();

        // Wire up quick access keyboard shortcuts
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs keyboard shortcuts
        WireQuickAccessShortcuts();

        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2729-2752 (DragOver/DragDrop, DeletePressed)
        WireResourceOpsEvents();

        // Window placement, browser columns, command line and update check
        InitializeShell();

        Closing += OnWindowClosing;
        Closed += OnWindowClosed;
    }

    /// <summary>
    /// Defers closing to the view model so uncommitted resource edits and unsaved package
    /// changes can be committed/saved, discarded, or the close cancelled, and so the settings
    /// are saved asynchronously while the window is still open.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 231-247 (MainForm_FormClosing:
    /// closing the package prompts; if the package is still open the close is cancelled; otherwise
    /// the settings are saved)
    /// </remarks>
    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || _viewModel == null)
        {
            return;
        }

        // The prompts and the settings save are asynchronous, so always cancel this close and
        // re-issue it once they have finished.
        e.Cancel = true;
        if (_closePromptActive) return;

        _closePromptActive = true;
        try
        {
            if (await _viewModel.PrepareWindowCloseAsync(StoreShellSettings))
            {
                _closeConfirmed = true;
                Close();
            }
        }
        finally
        {
            _closePromptActive = false;
        }
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

            // Menu gestures (Ctrl+N/O/S/F, F5, Ctrl+C/V/D/A, ...); the text inputs keep copy/paste/select all.
            // Events already handled by a focused control are not seen here.
            if (await MainWindowShortcuts.TryExecuteAsync(e.Key, e.KeyModifiers, _viewModel,
                    TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement()))
            {
                e.Handled = true;
                return;
            }

            // Ctrl+Shift+V: Paste ResourceKey to filter
            // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2593-2601
            if (e.Key == Key.V && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                await _viewModel.FilterPasteKeyCommand.ExecuteAsync(null);
                e.Handled = true;
                return;
            }

            // Insert key: Import resource (alternative to Ctrl+Shift+M)
            // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs keyboard shortcuts
            if (e.Key == Key.Insert && e.KeyModifiers == KeyModifiers.None)
            {
                if (_viewModel.ImportResourceCommand.CanExecute(null))
                {
                    await _viewModel.ImportResourceCommand.ExecuteAsync(null);
                    e.Handled = true;
                    return;
                }
            }

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
        // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3060-3073 (controlPanel1_CommitClick
        // replaces the resource in the package; it does not save the package)
        ControlPanel.CommitClick += (_, _) =>
        {
            if (_viewModel?.CommitCommand.CanExecute(null) == true)
            {
                _viewModel.CommitCommand.Execute(null);
            }
        };

        // Display option changes
        ControlPanel.SortChanged += (_, _) => _viewModel?.OnSortOptionChanged(ControlPanel.Sort);
        ControlPanel.HexOnlyChanged += (_, _) => _viewModel?.OnHexOnlyOptionChanged(ControlPanel.HexOnly);
        ControlPanel.UseNamesChanged += (_, _) => _viewModel?.OnUseNamesOptionChanged(ControlPanel.UseNames);
        ControlPanel.UseTagsChanged += (_, _) => _viewModel?.OnUseTagsOptionChanged(ControlPanel.UseTags);
        ControlPanel.AutoChanged += (_, _) => _viewModel?.OnAutoModeChanged(ControlPanel.AutoChoice);

        // Subscribe to ViewModel changes to update control panel helper buttons
        if (_viewModel != null)
        {
            _viewModelPropertyChangedHandler = (_, args) =>
            {
                if (_viewModel == null) return;

                if (args.PropertyName == nameof(MainWindowViewModel.AvailableHelpers))
                {
                    UpdateControlPanelHelpers();
                }
                else if (args.PropertyName == nameof(MainWindowViewModel.HasSelectedResource))
                {
                    ControlPanel.SetButtonsEnabled(_viewModel.HasSelectedResource);
                }
                else if (args.PropertyName == nameof(MainWindowViewModel.IsResourceDirty))
                {
                    // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs line 704 (CommitEnabled = resourceIsDirty)
                    ControlPanel.CommitEnabled = _viewModel.IsResourceDirty;
                }
            };
            _viewModel.PropertyChanged += _viewModelPropertyChangedHandler;
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

        _selectionSync = new BrowserSelectionSync(ResourceGrid, _viewModel);
    }

    /// <summary>
    /// Wires drag-and-drop import onto the window and the Delete key (toggle deleted) on the resource list.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2729-2752 (browserWidget1_DragOver,
    /// browserWidget1_DeletePressed) and legacy_references/Sims4Tools/s4pe/Import/Import.cs lines 561-590
    /// (browserWidget1_DragDrop).
    /// </remarks>
    private void WireResourceOpsEvents()
    {
        AddHandler(DragDrop.DragOverEvent, OnResourceDragOver);
        AddHandler(DragDrop.DropEvent, OnResourceDrop);
        ResourceGrid.KeyDown += OnResourceListKeyDown;
    }

    private void UnwireResourceOpsEvents()
    {
        RemoveHandler(DragDrop.DragOverEvent, OnResourceDragOver);
        RemoveHandler(DragDrop.DropEvent, OnResourceDrop);
        ResourceGrid.KeyDown -= OnResourceListKeyDown;
    }

    private void OnResourceDragOver(object? sender, DragEventArgs e)
    {
        if (e.Handled) return;
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnResourceDrop(object? sender, DragEventArgs e)
    {
        if (e.Handled || _viewModel == null) return;
        e.Handled = true;

        var paths = StorageItemPaths.LocalPaths(e.DataTransfer.TryGetFiles());
        if (paths.Count == 0) return;

        try
        {
            await _viewModel.ImportDroppedFilesAsync(paths);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _viewModel.StatusMessage = $"Import error: {ex.Message}";
        }
    }

    private void OnResourceListKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel == null || e.Key != Key.Delete || e.KeyModifiers != KeyModifiers.None) return;
        if (_viewModel.ToggleDeletedCommand.CanExecute(null))
        {
            _viewModel.ToggleDeletedCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void OnWindowClosed(object? sender, EventArgs e)
    {
        // Settings were stored and saved in the closing flow (OnWindowClosing).
        await DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        UnwireResourceOpsEvents();

        DisposeShell();

        if (_viewModel != null)
        {
            if (_viewModelPropertyChangedHandler != null)
            {
                _viewModel.PropertyChanged -= _viewModelPropertyChangedHandler;
                _viewModelPropertyChangedHandler = null;
            }
            await _viewModel.DisposeAsync().ConfigureAwait(false);
            _viewModel = null;
        }

        GC.SuppressFinalize(this);
    }
}
