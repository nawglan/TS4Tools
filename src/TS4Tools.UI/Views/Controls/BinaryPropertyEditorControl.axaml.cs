using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.UI.Views.Editors;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Inline editor control for binary data properties.
/// Shows Import/Export/Edit/Hex buttons.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/ReaderEditorPanel.cs
/// In legacy s4pe, ReaderEditorPanel provided inline editing for Stream/TextReader properties.
/// The Avalonia version provides the same functionality via buttons in the property grid.
/// </remarks>
public partial class BinaryPropertyEditorControl : UserControl
{
    public BinaryPropertyEditorControl()
    {
        InitializeComponent();

        ImportButton.Click += ImportButton_Click;
        ExportButton.Click += ExportButton_Click;
        EditButton.Click += EditButton_Click;
        HexButton.Click += HexButton_Click;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateSizeLabel();
        UpdateButtonStates();
    }

    private void UpdateSizeLabel()
    {
        if (DataContext is not PropertyItem item)
        {
            SizeLabel.Text = "";
            return;
        }

        var data = item.BinaryData;
        if (data == null || data.Length == 0)
        {
            SizeLabel.Text = "(empty)";
            return;
        }

        // Format size nicely
        SizeLabel.Text = data.Length switch
        {
            < 1024 => $"({data.Length} bytes)",
            < 1024 * 1024 => $"({data.Length / 1024.0:F1} KB)",
            _ => $"({data.Length / (1024.0 * 1024.0):F1} MB)"
        };
    }

    private void UpdateButtonStates()
    {
        if (DataContext is not PropertyItem item)
        {
            ExportButton.IsEnabled = false;
            EditButton.IsEnabled = false;
            HexButton.IsEnabled = false;
            return;
        }

        var hasData = item.BinaryData != null && item.BinaryData.Length > 0;
        var hasHexEditor = !string.IsNullOrWhiteSpace(SettingsService.Instance.Settings.HexEditorCommand);

        ExportButton.IsEnabled = hasData;
        EditButton.IsEnabled = hasData && hasHexEditor;
        HexButton.IsEnabled = hasData;

        // Update Edit button tooltip if no editor configured
        if (!hasHexEditor)
        {
            ToolTip.SetTip(EditButton, "No hex editor configured. Set HexEditorCommand in Settings.");
        }
        else
        {
            ToolTip.SetTip(EditButton, "Edit in external hex editor");
        }
    }

    /// <summary>
    /// Import binary data from file.
    /// Source: ReaderEditorPanel.cs lines 120-145
    /// </summary>
    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PropertyItem item) return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Import {item.Name}",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("All Files") { Patterns = ["*.*"] },
                    new FilePickerFileType("Binary Files") { Patterns = ["*.bin", "*.dat"] }
                ]
            });

            if (files.Count == 0) return;

            await using var stream = await files[0].OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            item.BinaryData = ms.ToArray();
            UpdateSizeLabel();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Import failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Export binary data to file.
    /// Source: ReaderEditorPanel.cs lines 147-170
    /// </summary>
    private async void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PropertyItem item) return;

        var data = item.BinaryData;
        if (data == null || data.Length == 0) return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Export {item.Name}",
                SuggestedFileName = $"{item.Name}.bin",
                FileTypeChoices =
                [
                    new FilePickerFileType("Binary Files") { Patterns = ["*.bin"] },
                    new FilePickerFileType("All Files") { Patterns = ["*.*"] }
                ]
            });

            if (file == null) return;

            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Edit binary data in external hex editor.
    /// Source: ReaderEditorPanel.cs lines 172-220
    /// </summary>
    private async void EditButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PropertyItem item) return;

        var data = item.BinaryData;
        if (data == null || data.Length == 0) return;

        var settings = SettingsService.Instance.Settings;
        var hexEditorCommand = settings.HexEditorCommand;

        if (string.IsNullOrWhiteSpace(hexEditorCommand))
        {
            // No hex editor configured - show message
            System.Diagnostics.Debug.WriteLine("[TS4Tools] No hex editor configured. Set HexEditorCommand in settings.");
            return;
        }

        try
        {
            // Create temp file
            var tempPath = Path.Combine(Path.GetTempPath(), $"ts4tools_{item.Name}_{Guid.NewGuid():N}.bin");
            await File.WriteAllBytesAsync(tempPath, data);

            var lastWriteTime = File.GetLastWriteTimeUtc(tempPath);

            // Build command line
            var quotedPath = settings.HexEditorWantsQuotes ? $"\"{tempPath}\"" : tempPath;
            var arguments = quotedPath;

            // Launch editor and wait
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = hexEditorCommand,
                Arguments = arguments,
                UseShellExecute = true
            };
            process.Start();
            await process.WaitForExitAsync();

            // Check if file was modified
            var newWriteTime = File.GetLastWriteTimeUtc(tempPath);
            if (!settings.HexEditorIgnoreTimestamp && newWriteTime > lastWriteTime)
            {
                // Reimport modified data
                var newData = await File.ReadAllBytesAsync(tempPath);
                item.BinaryData = newData;
                UpdateSizeLabel();
            }

            // Cleanup temp file
            try { File.Delete(tempPath); }
            catch { /* ignore cleanup errors */ }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Edit failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Show hex view in popup window.
    /// Source: ReaderEditorPanel.cs lines 222-250
    /// </summary>
    private void HexButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PropertyItem item) return;

        var data = item.BinaryData;
        if (data == null || data.Length == 0) return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel == null) return;

            // Create hex viewer window
            var hexViewModel = new HexViewerViewModel();
            hexViewModel.LoadData(data);

            var hexWindow = new Window
            {
                Title = $"Hex View - {item.Name} ({data.Length} bytes)",
                Width = 700,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new HexViewerView
                {
                    DataContext = hexViewModel
                }
            };

            hexWindow.Show(topLevel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TS4Tools] Hex view failed: {ex.Message}");
        }
    }
}
