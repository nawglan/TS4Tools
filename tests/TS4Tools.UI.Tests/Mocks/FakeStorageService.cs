using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.Tests.Mocks;

/// <summary>
/// Scripted <see cref="IStorageService"/>: picks are dequeued in order and every request is recorded.
/// There is no main window, as in a test run without a desktop lifetime.
/// </summary>
public sealed class FakeStorageService : IStorageService
{
    public Queue<IReadOnlyList<string>> OpenFiles { get; } = new();

    public Queue<string?> SaveFiles { get; } = new();

    public Queue<string?> Folders { get; } = new();

    public List<string?> RequestTitles { get; } = [];

    public int CloseRequests { get; private set; }

    public Window? MainWindow => null;

    public Task<IReadOnlyList<string>> PickOpenFilesAsync(FilePickerOpenOptions options)
    {
        RequestTitles.Add(options.Title);
        return Task.FromResult(OpenFiles.Count > 0 ? OpenFiles.Dequeue() : (IReadOnlyList<string>)[]);
    }

    public Task<string?> PickSaveFileAsync(FilePickerSaveOptions options, string? startFolder = null)
    {
        RequestTitles.Add(options.Title);
        return Task.FromResult(SaveFiles.Count > 0 ? SaveFiles.Dequeue() : null);
    }

    public Task<string?> PickFolderAsync(FolderPickerOpenOptions options, string? startFolder = null)
    {
        RequestTitles.Add(options.Title);
        return Task.FromResult(Folders.Count > 0 ? Folders.Dequeue() : null);
    }

    public void CloseMainWindow() => CloseRequests++;
}
