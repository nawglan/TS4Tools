using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Settings dialog for application configuration.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings
/// Source: legacy_references/Sims4Tools/s4pe/Settings/ExternalProgramsDialog.cs
/// </remarks>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly List<string> _bookmarks = [];
    private readonly List<HelperSettingItem> _helperItems = [];
    private readonly List<WrapperSettingItem> _wrapperItems = [];

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Instance.Settings;

        LoadSettings();
        LoadHelpers();
        LoadWrappers();

        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
        ResetButton.Click += ResetButton_Click;
        BrowseHexEditorButton.Click += BrowseHexEditorButton_Click;
        BrowseTextEditorButton.Click += BrowseTextEditorButton_Click;
        AddBookmarkButton.Click += AddBookmarkButton_Click;
        RemoveBookmarkButton.Click += RemoveBookmarkButton_Click;
        BookmarksListBox.SelectionChanged += BookmarksListBox_SelectionChanged;
        ReloadHelpersButton.Click += ReloadHelpersButton_Click;
        LicenseButton.Click += LicenseButton_Click;

        // Show settings path
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TS4Tools", "settings.json");
        SettingsPathTextBox.Text = settingsPath;

        // Show helpers folder location
        var helpersPath = Path.Combine(AppContext.BaseDirectory, "Helpers");
        HelpersLocationTextBlock.Text = $"Helpers folder: {helpersPath}";
    }

    private void LoadSettings()
    {
        // Display options
        UseNamesCheckBox.IsChecked = _settings.UseNames;
        UseTagsCheckBox.IsChecked = _settings.UseTags;
        HexOnlyCheckBox.IsChecked = _settings.HexOnly;
        ForceHexViewCheckBox.IsChecked = _settings.ForceHexView;
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
        _settings.ForceHexView = ForceHexViewCheckBox.IsChecked == true;
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

        // Helpers
        SaveHelperSettings();

        // Wrappers
        SaveWrapperSettings();

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

    /// <summary>
    /// Opens the GPLv3 license in the default browser.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs HelpLicence() lines 2541-2561
    /// </remarks>
    private void LicenseButton_Click(object? sender, RoutedEventArgs e)
    {
        const string licenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html";
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = licenseUrl,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            // Ignore errors opening URL - user can navigate manually
        }
    }

    /// <summary>
    /// Loads helper definitions and creates UI items.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/ExternalProgramsDialog.cs lines 76-96
    /// </remarks>
    private void LoadHelpers()
    {
        _helperItems.Clear();

        foreach (var helper in HelperManager.Instance.AllHelpers)
        {
            var item = new HelperSettingItem
            {
                Id = helper.Id,
                Label = !string.IsNullOrEmpty(helper.Label) ? helper.Label : helper.Id,
                Description = helper.Description,
                Command = helper.Command,
                IsDisabled = _settings.DisabledHelpers.Contains(helper.Id)
            };
            _helperItems.Add(item);
        }

        HelpersItemsControl.ItemsSource = null;
        HelpersItemsControl.ItemsSource = _helperItems;
    }

    private void ReloadHelpersButton_Click(object? sender, RoutedEventArgs e)
    {
        HelperManager.Instance.Reload();
        LoadHelpers();
    }

    private void HelperInfoButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HelperSettingItem item })
        {
            ShowHelperInfo(item);
        }
    }

    /// <summary>
    /// Shows information about a helper program.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/ExternalProgramsDialog.cs lines 117-131
    /// </remarks>
    private async void ShowHelperInfo(HelperSettingItem item)
    {
        var info = $"ID: {item.Id}\n";
        info += $"Label: {item.Label}\n";
        info += $"Description: {item.Description}\n";
        info += $"Command: {item.Command}";

        var dialog = new Window
        {
            Title = $"Helper: {item.Label}",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new TextBlock
            {
                Text = info,
                Margin = new Avalonia.Thickness(16),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };

        await dialog.ShowDialog(this);
    }

    private void SaveHelperSettings()
    {
        _settings.DisabledHelpers.Clear();
        foreach (var item in _helperItems)
        {
            if (item.IsDisabled)
            {
                _settings.DisabledHelpers.Add(item.Id);
            }
        }
    }

    /// <summary>
    /// Loads wrapper definitions and creates UI items.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/ManageWrappersDialog.cs lines 45-75
    /// </remarks>
    private void LoadWrappers()
    {
        _wrapperItems.Clear();

        // Get all wrapper factories from the Wrappers assembly
        var wrappersAssembly = typeof(TS4Tools.Wrappers.StblResource).Assembly;
        var factoryTypes = wrappersAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Factory", StringComparison.Ordinal) && !t.IsAbstract && t.IsPublic)
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var factoryType in factoryTypes)
        {
            // Get the associated resource type count (from ResourceTypes property if available)
            var resourceTypesField = factoryType.GetField("ResourceTypes",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var typeCount = 1;
            if (resourceTypesField != null && resourceTypesField.GetValue(null) is uint[] types)
            {
                typeCount = types.Length;
            }

            var wrapperName = factoryType.Name.Replace("Factory", "");

            var item = new WrapperSettingItem
            {
                WrapperName = wrapperName,
                TypeCount = typeCount,
                IsDisabled = _settings.DisabledWrappers.Contains(wrapperName)
            };
            _wrapperItems.Add(item);
        }

        WrappersItemsControl.ItemsSource = null;
        WrappersItemsControl.ItemsSource = _wrapperItems;
        WrapperCountTextBlock.Text = $"{_wrapperItems.Count} wrappers available";
    }

    private void SaveWrapperSettings()
    {
        _settings.DisabledWrappers.Clear();
        foreach (var item in _wrapperItems)
        {
            if (item.IsDisabled)
            {
                _settings.DisabledWrappers.Add(item.WrapperName);
            }
        }
    }
}

/// <summary>
/// View model item for helper settings.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/ExternalProgramsDialog.cs lines 37-73
/// </remarks>
public partial class HelperSettingItem : ObservableObject
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string Description { get; init; } = "";
    public string Command { get; init; } = "";

    [ObservableProperty]
    private bool _isDisabled;
}

/// <summary>
/// View model item for wrapper settings.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/ManageWrappersDialog.cs
/// </remarks>
public partial class WrapperSettingItem : ObservableObject
{
    public required string WrapperName { get; init; }
    public required int TypeCount { get; init; }

    public string DisplayName => WrapperName;

    [ObservableProperty]
    private bool _isDisabled;
}
