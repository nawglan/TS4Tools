using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog for organizing bookmarks (add, remove, reorder).
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs
/// </remarks>
public partial class OrganiseBookmarksWindow : Window
{
    private readonly AppSettings _settings;
    private readonly List<BookmarkItem> _bookmarks = [];

    public OrganiseBookmarksWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Instance.Settings;

        LoadBookmarks();

        MaxBookmarksNumeric.Value = _settings.BookmarkSize;

        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
        AddButton.Click += AddButton_Click;
        DeleteButton.Click += DeleteButton_Click;
        MoveUpButton.Click += MoveUpButton_Click;
        MoveDownButton.Click += MoveDownButton_Click;
        BookmarksListBox.SelectionChanged += BookmarksListBox_SelectionChanged;
        MaxBookmarksNumeric.ValueChanged += MaxBookmarksNumeric_ValueChanged;
    }

    /// <summary>
    /// Loads bookmarks from settings.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs lines 57-67
    /// </remarks>
    private void LoadBookmarks()
    {
        _bookmarks.Clear();

        for (var i = 0; i < _settings.Bookmarks.Count; i++)
        {
            var entry = _settings.Bookmarks[i];
            _bookmarks.Add(new BookmarkItem(i + 1, entry));
        }

        RefreshList();
    }

    private void RefreshList()
    {
        for (var i = 0; i < _bookmarks.Count; i++)
        {
            _bookmarks[i].Index = i + 1;
        }

        BookmarksListBox.ItemsSource = null;
        BookmarksListBox.ItemsSource = _bookmarks;

        UpdateButtonStates();
    }

    /// <summary>
    /// Updates button enabled states based on selection.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs lines 69-74
    /// </remarks>
    private void UpdateButtonStates()
    {
        var index = BookmarksListBox.SelectedIndex;
        DeleteButton.IsEnabled = index >= 0;
        MoveUpButton.IsEnabled = index > 0;
        MoveDownButton.IsEnabled = index >= 0 && index < _bookmarks.Count - 1;
        AddButton.IsEnabled = _bookmarks.Count < (int)(MaxBookmarksNumeric.Value ?? 10);
    }

    private void BookmarksListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateButtonStates();
    }

    /// <summary>
    /// Adds a new bookmark via file picker.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs lines 98-116
    /// </remarks>
    private async void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Package File to Bookmark",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Package Files") { Patterns = ["*.package"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ]
        });

        foreach (var file in files)
        {
            if (_bookmarks.Count >= (int)(MaxBookmarksNumeric.Value ?? 10))
            {
                break;
            }

            var path = file.Path.LocalPath;
            // Add as read-write (1:) by default
            var entry = "1:" + path;
            _bookmarks.Add(new BookmarkItem(_bookmarks.Count + 1, entry));
        }

        RefreshList();

        if (_bookmarks.Count > 0)
        {
            BookmarksListBox.SelectedIndex = _bookmarks.Count - 1;
        }
    }

    /// <summary>
    /// Deletes the selected bookmark.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs lines 118-127
    /// </remarks>
    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        var index = BookmarksListBox.SelectedIndex;
        if (index < 0) return;

        _bookmarks.RemoveAt(index);
        RefreshList();

        if (_bookmarks.Count > 0)
        {
            BookmarksListBox.SelectedIndex = Math.Min(index, _bookmarks.Count - 1);
        }
    }

    /// <summary>
    /// Moves the selected bookmark up.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs lines 76-96
    /// </remarks>
    private void MoveUpButton_Click(object? sender, RoutedEventArgs e)
    {
        MoveBookmark(-1);
    }

    private void MoveDownButton_Click(object? sender, RoutedEventArgs e)
    {
        MoveBookmark(1);
    }

    private void MoveBookmark(int direction)
    {
        var index = BookmarksListBox.SelectedIndex;
        if (index < 0) return;

        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _bookmarks.Count) return;

        var item = _bookmarks[index];
        _bookmarks.RemoveAt(index);
        _bookmarks.Insert(newIndex, item);

        RefreshList();
        BookmarksListBox.SelectedIndex = newIndex;
    }

    /// <summary>
    /// Handles max bookmarks change - trims list if needed.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs lines 129-143
    /// </remarks>
    private void MaxBookmarksNumeric_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var max = (int)(MaxBookmarksNumeric.Value ?? 10);

        while (_bookmarks.Count > max)
        {
            _bookmarks.RemoveAt(_bookmarks.Count - 1);
        }

        RefreshList();
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        SaveBookmarks();
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void SaveBookmarks()
    {
        _settings.Bookmarks.Clear();
        foreach (var item in _bookmarks)
        {
            _settings.Bookmarks.Add(item.RawEntry);
        }
        _settings.BookmarkSize = (int)(MaxBookmarksNumeric.Value ?? 10);
        SettingsService.Instance.Save();
    }
}

/// <summary>
/// Represents a bookmark item in the list.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseBookmarksDialog.cs lines 57-67
/// </remarks>
public class BookmarkItem
{
    public BookmarkItem(int index, string rawEntry)
    {
        Index = index;
        RawEntry = rawEntry;

        // Parse format: "0:path" for read-only, "1:path" for read-write
        if (rawEntry.Length > 2 && rawEntry[1] == ':')
        {
            IsReadOnly = rawEntry[0] == '0';
            FilePath = rawEntry[2..];
        }
        else
        {
            IsReadOnly = false;
            FilePath = rawEntry;
        }
    }

    public int Index { get; set; }
    public string RawEntry { get; }
    public string FilePath { get; }
    public bool IsReadOnly { get; }

    public string DisplayText
    {
        get
        {
            var prefix = IsReadOnly ? "[RO] " : "[RW] ";
            return $"{Index}. {prefix}{FilePath}";
        }
    }
}
