using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels;
using TS4Tools.UI.Views.Controls;

namespace TS4Tools.UI.Views;

/// <summary>
/// Window placement, browser columns and start up of the main window.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 146-226 (window and splitter settings,
/// MainForm_Shown) and legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 46-89, 584-621.
/// </remarks>
public partial class MainWindow
{
    private BrowserSelectionSync? _selectionSync;
    private DeletedRowStyler? _deletedRowStyler;
    private DataGridColumn[] _browserColumns = [];
    private double[] _columnWidths = [];
    private PropertyChangedEventHandler? _shellChangedHandler;
    private EventHandler? _shellSettingsReloadedHandler;
    private bool _startupRun;

    private ColumnDefinition BrowserColumnDefinition => MainLayoutGrid.ColumnDefinitions[0];

    private RowDefinition PackageInfoRowDefinition => OuterLayoutGrid.RowDefinitions[2];

    private void InitializeShell()
    {
        if (_viewModel == null) return;

        RestoreWindowPlacement();
        SetupBrowserColumns();

        _shellChangedHandler = OnShellPropertyChanged;
        _viewModel.PropertyChanged += _shellChangedHandler;
        _shellSettingsReloadedHandler = (_, _) => SyncControlPanelFromViewModel();
        _viewModel.ShellSettingsReloaded += _shellSettingsReloadedHandler;

        ResourceGrid.Sorting += OnGridSorting;
        _deletedRowStyler = new DeletedRowStyler(ResourceGrid);
        Opened += OnWindowOpened;
        PreviewZoomHost.PointerWheelChanged += OnPreviewWheel;
        ApplyPreviewZoom();
    }

    private void DisposeShell()
    {
        ResourceGrid.Sorting -= OnGridSorting;
        Opened -= OnWindowOpened;
        PreviewZoomHost.PointerWheelChanged -= OnPreviewWheel;
        _deletedRowStyler?.Dispose();
        _deletedRowStyler = null;
        _selectionSync?.Dispose();
        _selectionSync = null;

        if (_viewModel == null) return;
        if (_shellChangedHandler != null) _viewModel.PropertyChanged -= _shellChangedHandler;
        if (_shellSettingsReloadedHandler != null) _viewModel.ShellSettingsReloaded -= _shellSettingsReloadedHandler;
        _shellChangedHandler = null;
        _shellSettingsReloadedHandler = null;
    }

    /// <summary>Applies the saved window size, position, state and splitter positions.</summary>
    /// <remarks>Source: MainForm.cs lines 146-189 (MainForm_LoadFormSettings)</remarks>
    /// <summary>Current main-window layout version (2: legacy browser|preview over filter/package info strip).</summary>
    internal const int CurrentLayoutVersion = 2;

    private void RestoreWindowPlacement()
    {
        var settings = SettingsService.Instance.Settings;
        if (settings.LayoutVersion < CurrentLayoutVersion)
        {
            settings.Splitter1Position = -1;
            settings.Splitter2Position = -1;
            settings.ColumnWidths = "";
            settings.LayoutVersion = CurrentLayoutVersion;
        }
        var screen = Screens.Primary?.WorkingArea;
        var scaling = Screens.Primary?.Scaling ?? 1.0;
        var screenWidth = screen is { } s ? s.Width / scaling : 1500;
        var screenHeight = screen is { } h ? h.Height / scaling : 1000;

        var placement = WindowPlacement.Resolve(settings, screenWidth, screenHeight);
        Width = placement.Width;
        Height = placement.Height;

        if (placement.X is { } x && placement.Y is { } y && IsOnScreen(new PixelPoint(x, y)))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint(x, y);
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (placement.Maximized) WindowState = WindowState.Maximized;
        if (placement.BrowserWidth is { } bw) BrowserColumnDefinition.Width = new GridLength(bw);
        if (placement.PackageInfoHeight is { } ph) PackageInfoRowDefinition.Height = new GridLength(ph);
    }

    private bool IsOnScreen(PixelPoint point)
        => Screens.All.Any(screen => screen.WorkingArea.Contains(point));

    /// <summary>Stores the window placement, splitters and column widths.</summary>
    /// <remarks>Source: MainForm.cs lines 191-198 and BrowserWidget.cs lines 73-89 (SaveSettings)</remarks>
    private void StoreShellSettings()
    {
        if (_viewModel == null) return;
        var settings = SettingsService.Instance.Settings;

        var state = WindowState switch
        {
            WindowState.Maximized => WindowPlacement.Maximized,
            WindowState.Minimized => WindowPlacement.Minimized,
            _ => WindowPlacement.Normal
        };

        // A maximized window keeps the size and position it had before, as legacy does by not
        // reading them from a maximized form.
        var normal = WindowState == WindowState.Normal;
        WindowPlacement.Capture(
            settings,
            normal ? Width : settings.WindowWidth,
            normal ? Height : settings.WindowHeight,
            normal ? Position.X : settings.WindowX,
            normal ? Position.Y : settings.WindowY,
            state,
            BrowserColumnDefinition.ActualWidth > 0 ? BrowserColumnDefinition.ActualWidth : settings.Splitter1Position,
            PackageInfoRowDefinition.ActualHeight > 0 ? PackageInfoRowDefinition.ActualHeight : settings.Splitter2Position);

        for (var i = 0; i < _browserColumns.Length && i < _columnWidths.Length; i++)
        {
            if (_browserColumns[i].IsVisible && _browserColumns[i].ActualWidth > 0)
            {
                _columnWidths[i] = _browserColumns[i].ActualWidth;
            }
        }

        settings.ColumnWidths = BrowserColumns.FormatWidths(_columnWidths);
        _viewModel.StoreSortSettings();
    }

    private void SetupBrowserColumns()
    {
        _browserColumns = [.. ResourceGrid.Columns];
        var saved = BrowserColumns.ParseWidths(SettingsService.Instance.Settings.ColumnWidths);
        _columnWidths = new double[_browserColumns.Length];
        for (var i = 0; i < _browserColumns.Length; i++)
        {
            _columnWidths[i] = BrowserColumns.WidthFor(saved, i);
            _browserColumns[i].Width = new DataGridLength(_columnWidths[i]);
        }

        UpdateColumnVisibility();
        UpdateSortHeaders();
    }

    private void UpdateColumnVisibility()
    {
        if (_viewModel == null || _browserColumns.Length == 0) return;

        // Source: BrowserWidget.cs lines 300-352 (name and tag columns are shown or hidden)
        _browserColumns[0].IsVisible = _viewModel.UseNames;
        _browserColumns[1].IsVisible = _viewModel.UseTags;
    }

    private void UpdateSortHeaders()
    {
        if (_viewModel == null) return;

        for (var i = 0; i < _browserColumns.Length; i++)
        {
            var glyph = i == _viewModel.SortColumn && _viewModel.SortEnabled
                ? _viewModel.SortOrder switch
                {
                    BrowserSortOrder.Ascending => " ▲",
                    BrowserSortOrder.Descending => " ▼",
                    _ => ""
                }
                : "";
            _browserColumns[i].Header = BrowserColumns.All[i] + glyph;
        }
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.UseNames):
            case nameof(MainWindowViewModel.UseTags):
                UpdateColumnVisibility();
                break;
            case nameof(MainWindowViewModel.EffectivePreviewZoom):
                ApplyPreviewZoom();
                break;
            case nameof(MainWindowViewModel.SortColumn):
            case nameof(MainWindowViewModel.SortOrder):
            case nameof(MainWindowViewModel.SortEnabled):
                UpdateSortHeaders();
                break;
        }
    }

    private void ApplyPreviewZoom()
    {
        var zoom = _viewModel?.EffectivePreviewZoom ?? 1.0;
        PreviewZoomHost.LayoutTransform = new ScaleTransform(zoom, zoom);
    }

    /// <summary>Ctrl+wheel over a text preview zooms it, as in a RichTextBox.</summary>
    private void OnPreviewWheel(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel is not { CanZoomPreview: true } || !e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        _viewModel.ZoomPreview(e.Delta.Y > 0 ? 1.1 : 1 / 1.1);
        e.Handled = true;
    }

    private void OnGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        var index = Array.IndexOf(_browserColumns, e.Column);
        if (index >= 0) _viewModel?.SortByColumn(index);
        e.Handled = true;
    }

    private void SyncControlPanelFromViewModel()
    {
        if (_viewModel == null) return;
        ControlPanel.Sort = _viewModel.SortEnabled;
        ControlPanel.HexOnly = _viewModel.HexOnly;
        ControlPanel.UseNames = _viewModel.UseNames;
        ControlPanel.UseTags = _viewModel.UseTags;
        ControlPanel.AutoChoice = _viewModel.AutoChoice;
    }

    /// <summary>Opens the package named on the command line and checks for updates.</summary>
    /// <remarks>Source: MainForm.cs lines 201-226 (MainForm_Shown)</remarks>
    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        if (_startupRun || _viewModel == null) return;
        _startupRun = true;

        var args = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Args ?? [];
        try
        {
            await _viewModel.RunStartupAsync(args);
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Start up error: {ex.Message}";
        }
    }
}
