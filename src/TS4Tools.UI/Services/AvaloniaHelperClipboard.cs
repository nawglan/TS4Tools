using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace TS4Tools.UI.Services;

/// <summary>
/// <see cref="IHelperClipboard"/> on the clipboard of the main window, using a binary
/// application format.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 176-205
/// </remarks>
public sealed class AvaloniaHelperClipboard : IHelperClipboard
{
    private static readonly DataFormat<byte[]> ResourceFormat = DataFormat.CreateBytesPlatformFormat(HelperClipboardFormat.Name);

    private static IClipboard? Clipboard => AvaloniaStorageService.Instance.MainWindow?.Clipboard;

    public async Task SetResourceAsync(byte[] data)
    {
        if (Clipboard is not { } clipboard) throw new InvalidOperationException("The clipboard is not available");

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(ResourceFormat, data));
        await clipboard.SetDataAsync(transfer);
    }

    public async Task<byte[]?> TryGetResourceAsync()
    {
        if (Clipboard is not { } clipboard) return null;

        using var data = await clipboard.TryGetDataAsync();
        return data == null ? null : await data.TryGetValueAsync(ResourceFormat);
    }
}
