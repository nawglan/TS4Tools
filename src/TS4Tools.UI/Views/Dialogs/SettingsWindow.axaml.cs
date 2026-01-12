using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Settings dialog for application configuration.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings
/// </remarks>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly List<string> _bookmarks = [];

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Instance.Settings;

        LoadSettings();

        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
        ResetButton.Click += ResetButton_Click;
        BrowseHexEditorButton.Click += BrowseHexEditorButton_Click;
        BrowseTextEditorButton.Click += BrowseTextEditorButton_Click;
        AddBookmarkButton.Click += AddBookmarkButton_Click;
        RemoveBookmarkButton.Click += RemoveBookmarkButton_Click;
        BookmarksListBox.SelectionChanged += BookmarksListBox_SelectionChanged;

        // Show settings path
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TS4Tools", "settings.json");
        SettingsPathTextBox.Text = settingsPath;
    }

    private void LoadSettings()
    {
        // Display options
        UseNamesCheckBox.IsChecked = _settings.UseNames;
        UseTagsCheckBox.IsChecked = _settings.UseTags;
        HexOnlyCheckBox.IsChecked = _settings.HexOnly;
        EnableDDSPreviewCheckBox.IsChecked = _settings.EnableDDSPreview;
        EnableTextPreviewCheckBox.IsChecked = _settings.EnableFallbackTextPreview;
        EnableHexPreviewCheckBox.IsChecked = _settings.EnableFallbackHexPreview;
        HexRowSizeNumeric.Value = _settings.HexRowSize;
        PreviewZoomNumeric.Value = (decimal)_settings.PreviewZoomFactor;

        // External editors
        HexEditorCommandTextBox.Text = _settings.HexEditorCommand ?? "";
        HexEditorIgnoreTSCheckBox.IsChecked = _settings.HexEditorIgnoreTimestamp;
        HexEditorQuotesCheckBox.IsChecked = _settings.HexEditorWantsQuotes;
        TextEditorCommandTextBox.Text = _settings.TextEditorCommand ?? "";
        TextEditorIgnoreTSCheckBox.IsChecked = _settings.TextEditorIgnoreTimestamp;
        TextEditorQuotesCheckBox.IsChecked = _settings.TextEditorWantsQuotes;

        // Bookmarks
        _bookmarks.Clear();
        _bookmarks.AddRange(_settings.Bookmarks);
        BookmarksListBox.ItemsSource = null;
        BookmarksListBox.ItemsSource = _bookmarks;
    }

    private void SaveSettings()
    {
        // Display options
        _settings.UseNames = UseNamesCheckBox.IsChecked == true;
        _settings.UseTags = UseTagsCheckBox.IsChecked == true;
        _settings.HexOnly = HexOnlyCheckBox.IsChecked == true;
        _settings.EnableDDSPreview = EnableDDSPreviewCheckBox.IsChecked == true;
        _settings.EnableFallbackTextPreview = EnableTextPreviewCheckBox.IsChecked == true;
        _settings.EnableFallbackHexPreview = EnableHexPreviewCheckBox.IsChecked == true;
        _settings.HexRowSize = (int)(HexRowSizeNumeric.Value ?? 16);
        _settings.PreviewZoomFactor = (float)(PreviewZoomNumeric.Value ?? 1.0m);

        // External editors
        _settings.HexEditorCommand = string.IsNullOrWhiteSpace(HexEditorCommandTextBox.Text) ? null : HexEditorCommandTextBox.Text;
        _settings.HexEditorIgnoreTimestamp = HexEditorIgnoreTSCheckBox.IsChecked == true;
        _settings.HexEditorWantsQuotes = HexEditorQuotesCheckBox.IsChecked == true;
        _settings.TextEditorCommand = string.IsNullOrWhiteSpace(TextEditorCommandTextBox.Text) ? null : TextEditorCommandTextBox.Text;
        _settings.TextEditorIgnoreTimestamp = TextEditorIgnoreTSCheckBox.IsChecked == true;
        _settings.TextEditorWantsQuotes = TextEditorQuotesCheckBox.IsChecked == true;

        // Bookmarks
        _settings.Bookmarks = [.._bookmarks];

        SettingsService.Instance.Save();
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        SaveSettings();
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ResetButton_Click(object? sender, RoutedEventArgs e)
    {
        SettingsService.Instance.Reset();
        LoadSettings();
    }

    private async void BrowseHexEditorButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = await BrowseForExecutableAsync("Select Hex Editor");
        if (!string.IsNullOrEmpty(path))
        {
            HexEditorCommandTextBox.Text = path;
        }
    }

    private async void BrowseTextEditorButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = await BrowseForExecutableAsync("Select Text Editor");
        if (!string.IsNullOrEmpty(path))
        {
            TextEditorCommandTextBox.Text = path;
        }
    }

    private async Task<string?> BrowseForExecutableAsync(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Executable") { Patterns = ["*.exe", "*"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ]
        });

        return files.Count == 1 ? files[0].Path.LocalPath : null;
    }

    private async void AddBookmarkButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Add Bookmark Folder",
            AllowMultiple = false
        });

        if (folders.Count == 1)
        {
            var path = folders[0].Path.LocalPath;
            if (!_bookmarks.Contains(path))
            {
                _bookmarks.Add(path);
                BookmarksListBox.ItemsSource = null;
                BookmarksListBox.ItemsSource = _bookmarks;
            }
        }
    }

    private void RemoveBookmarkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (BookmarksListBox.SelectedItem is string selectedPath)
        {
            _bookmarks.Remove(selectedPath);
            BookmarksListBox.ItemsSource = null;
            BookmarksListBox.ItemsSource = _bookmarks;
        }
    }

    private void BookmarksListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RemoveBookmarkButton.IsEnabled = BookmarksListBox.SelectedItem != null;
    }
}
