using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SkiaSharp;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog for importing PNG files as thumbnail resources (JFIF+ALFA format).
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe Helpers/ThumbnailHelper/ImportThumb.cs
/// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ThumbnailResource.cs
///
/// JFIF+ALFA format:
///   - Standard JFIF/JPEG data (no alpha)
///   - After first 12 bytes, insert APP0 extension with ALFA marker
///   - Format: FFE0 + length(BE) + "ALFA" + alpha_png_length(BE) + alpha_png_data
///   - Rest of JPEG data follows
/// </remarks>
public partial class ThumbnailHelperWindow : Window
{
    private readonly List<string> _files = [];

    /// <summary>
    /// Gets the list of files to import and their converted data.
    /// </summary>
    public List<(string FileName, byte[] Data)> ConvertedFiles { get; } = [];

    public ThumbnailHelperWindow()
    {
        InitializeComponent();

        ImportButton.Click += ImportButton_Click;
        CancelButton.Click += CancelButton_Click;
        AddFilesButton.Click += AddFilesButton_Click;
        RemoveButton.Click += RemoveButton_Click;
        ClearButton.Click += ClearButton_Click;
        FilesListBox.SelectionChanged += FilesListBox_SelectionChanged;
    }

    private void UpdateFileList()
    {
        FilesListBox.ItemsSource = null;
        FilesListBox.ItemsSource = _files.Select(Path.GetFileName).ToList();
        FileCountLabel.Text = $"{_files.Count} file{(_files.Count != 1 ? "s" : "")} selected";
        ImportButton.IsEnabled = _files.Count > 0;
    }

    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_files.Count == 0)
        {
            return;
        }

        try
        {
            ConvertedFiles.Clear();

            foreach (var file in _files)
            {
                var convertedData = ConvertPngToJfifAlfa(file);
                if (convertedData != null)
                {
                    ConvertedFiles.Add((Path.GetFileName(file), convertedData));
                }
            }

            Close(true);
        }
        catch (Exception ex)
        {
            // Show error
            await MessageBox.ShowAsync(this, $"Error converting files: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// Converts a PNG file to JFIF+ALFA format.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pi Wrappers/ImageResource/ThumbnailResource.cs lines 69-100, 143-193
    ///
    /// The JFIF+ALFA format embeds an alpha PNG within a JPEG APP0 extension segment:
    /// 1. Read PNG with alpha channel
    /// 2. Create JPEG from RGB channels (no alpha)
    /// 3. Create grayscale PNG from alpha channel (R=G=B=Alpha)
    /// 4. Embed alpha PNG in JFIF APP0 extension after the initial APP0 marker
    /// </remarks>
    private static byte[]? ConvertPngToJfifAlfa(string pngPath)
    {
        using var pngData = File.OpenRead(pngPath);
        using var bitmap = SKBitmap.Decode(pngData);

        if (bitmap == null)
            return null;

        int width = bitmap.Width;
        int height = bitmap.Height;

        // Create RGB image for JPEG (no alpha)
        using var rgbBitmap = new SKBitmap(width, height, SKColorType.Rgb888x, SKAlphaType.Opaque);
        // Create grayscale image for alpha channel
        using var alphaBitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Opaque);

        // Extract RGB and Alpha channels
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);

                // RGB to color image
                rgbBitmap.SetPixel(x, y, new SKColor(pixel.Red, pixel.Green, pixel.Blue));

                // Alpha to grayscale (store alpha as gray level)
                alphaBitmap.SetPixel(x, y, new SKColor(pixel.Alpha, pixel.Alpha, pixel.Alpha));
            }
        }

        // Encode JPEG
        using var jpegStream = new MemoryStream();
        rgbBitmap.Encode(jpegStream, SKEncodedImageFormat.Jpeg, 90);
        var jpegData = jpegStream.ToArray();

        // Encode alpha as PNG
        using var alphaStream = new MemoryStream();
        alphaBitmap.Encode(alphaStream, SKEncodedImageFormat.Png, 100);
        var alphaPngData = alphaStream.ToArray();

        // Build JFIF+ALFA format
        // Source: ThumbnailResource.cs lines 69-100 (ToJFIF)
        // Format: First 12 bytes of JPEG, then ALFA segment, then rest of JPEG
        using var outputStream = new MemoryStream();
        using var writer = new BinaryWriter(outputStream);

        // Write first 12 bytes of JPEG (SOI + APP0 header)
        writer.Write(jpegData, 0, 12);

        // Write ALFA APP0 extension
        // APP0 marker: FF E0
        writer.Write((byte)0xFF);
        writer.Write((byte)0xE0);

        // Length of APP0 segment (big-endian): "ALFA" (4) + length field (4) + alpha PNG data
        int app0Length = 4 + 4 + alphaPngData.Length + 2; // +2 for length field itself
        writer.Write((byte)((app0Length >> 8) & 0xFF));
        writer.Write((byte)(app0Length & 0xFF));

        // "ALFA" marker
        writer.Write((byte)'A');
        writer.Write((byte)'L');
        writer.Write((byte)'F');
        writer.Write((byte)'A');

        // Alpha PNG length (big-endian)
        int alphaLength = alphaPngData.Length;
        writer.Write((byte)((alphaLength >> 24) & 0xFF));
        writer.Write((byte)((alphaLength >> 16) & 0xFF));
        writer.Write((byte)((alphaLength >> 8) & 0xFF));
        writer.Write((byte)(alphaLength & 0xFF));

        // Alpha PNG data
        writer.Write(alphaPngData);

        // Rest of JPEG data (skip first 12 bytes already written)
        writer.Write(jpegData, 12, jpegData.Length - 12);

        return outputStream.ToArray();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private async void AddFilesButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select PNG Files",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("PNG Images") { Patterns = ["*.png"] },
            ]
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (!_files.Contains(path))
            {
                _files.Add(path);
            }
        }
        UpdateFileList();
    }

    private void RemoveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (FilesListBox.SelectedItems == null) return;

        var selectedNames = FilesListBox.SelectedItems.OfType<string>().ToList();
        var indicesToRemove = selectedNames
            .Select(name => _files.FindIndex(f => Path.GetFileName(f) == name))
            .Where(i => i >= 0)
            .OrderByDescending(i => i)
            .ToList();

        foreach (var index in indicesToRemove)
        {
            _files.RemoveAt(index);
        }
        UpdateFileList();
    }

    private void ClearButton_Click(object? sender, RoutedEventArgs e)
    {
        _files.Clear();
        UpdateFileList();
    }

    private void FilesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RemoveButton.IsEnabled = FilesListBox.SelectedItems?.Count > 0;
    }
}

/// <summary>
/// Simple message box helper.
/// </summary>
internal static class MessageBox
{
    public static async Task ShowAsync(Window owner, string message, string title)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Avalonia.Thickness(0, 16, 0, 0) }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is Button btn)
        {
            btn.Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(owner);
    }
}
