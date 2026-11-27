using Avalonia;
using Avalonia.Controls;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Marks browser rows of deleted resources with the <see cref="DeletedClass"/> style class, which
/// MainWindow.axaml draws struck out and dimmed.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs line 713
/// (<c>if (ie.IsDeleted) lvi.Font = new Font(lvi.Font, lvi.Font.Style | FontStyle.Strikeout)</c>) and
/// lines 940-960 (the item font follows the entry when it is refreshed).
/// The class is toggled from code because a DataGrid style cannot bind a row's classes to its item.
/// </remarks>
internal sealed class DeletedRowStyler : IDisposable
{
    /// <summary>The style class set on rows whose resource is deleted.</summary>
    public const string DeletedClass = "deleted";

    private readonly DataGrid _grid;
    private readonly Dictionary<DataGridRow, (ResourceItemViewModel Item, PropertyChangedEventHandler Handler)> _rows = [];
    private bool _disposed;

    public DeletedRowStyler(DataGrid grid)
    {
        _grid = grid;
        _grid.LoadingRow += OnLoadingRow;
        _grid.UnloadingRow += OnUnloadingRow;
    }

    /// <summary>Sets or clears the deleted class on a row.</summary>
    public static void Apply(StyledElement row, bool deleted) => row.Classes.Set(DeletedClass, deleted);

    private void OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        Release(e.Row);
        if (e.Row.DataContext is not ResourceItemViewModel item) return;

        Apply(e.Row, item.IsDeleted);
        PropertyChangedEventHandler handler = (_, args) =>
        {
            if (args.PropertyName is nameof(ResourceItemViewModel.IsDeleted) or null or "")
            {
                Apply(e.Row, item.IsDeleted);
            }
        };
        item.PropertyChanged += handler;
        _rows[e.Row] = (item, handler);
    }

    private void OnUnloadingRow(object? sender, DataGridRowEventArgs e) => Release(e.Row);

    private void Release(DataGridRow row)
    {
        if (!_rows.Remove(row, out var tracked)) return;
        tracked.Item.PropertyChanged -= tracked.Handler;
        Apply(row, false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _grid.LoadingRow -= OnLoadingRow;
        _grid.UnloadingRow -= OnUnloadingRow;
        foreach (var (item, handler) in _rows.Values)
        {
            item.PropertyChanged -= handler;
        }
        _rows.Clear();
    }
}
