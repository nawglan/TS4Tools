using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;

namespace TS4Tools.UI.Services;

/// <summary>
/// Puts the text or image of the preview on the system clipboard.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1052-1092 (EditCopy:
/// Clipboard.SetImage for a picture, Clipboard.SetText for text).
/// </remarks>
public interface IPreviewClipboard
{
    /// <summary>Places text on the clipboard.</summary>
    Task SetTextAsync(string text);

    /// <summary>Places an image on the clipboard.</summary>
    Task SetBitmapAsync(Bitmap bitmap);
}

/// <summary>The clipboard of the main window.</summary>
public sealed class AvaloniaPreviewClipboard : IPreviewClipboard
{
    private static IClipboard? Clipboard => AvaloniaStorageService.Instance.MainWindow?.Clipboard;

    public async Task SetTextAsync(string text)
    {
        if (Clipboard is { } clipboard) await clipboard.SetTextAsync(text);
    }

    public async Task SetBitmapAsync(Bitmap bitmap)
    {
        if (Clipboard is { } clipboard) await clipboard.SetBitmapAsync(bitmap);
    }
}
