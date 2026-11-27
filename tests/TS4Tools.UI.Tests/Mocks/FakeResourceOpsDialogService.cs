using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Tests.Mocks;

/// <summary>
/// Scripted <see cref="IResourceOpsDialogService"/> for resource operation tests: dialogs run a callback
/// (which may edit the dialog state) and pickers/prompts return queued answers. The clipboard is in memory.
/// </summary>
public sealed class FakeResourceOpsDialogService : IResourceOpsDialogService
{
    /// <summary>Called for each Resource Details dialog; returns whether OK was pressed. Default: OK.</summary>
    public Func<ResourceDetailsViewModel, bool> OnResourceDetails { get; set; } = _ => true;

    /// <summary>Called for each Import Batch dialog; returns whether OK was pressed. Default: OK.</summary>
    public Func<ImportBatchViewModel, bool> OnImportBatch { get; set; } = _ => true;

    public List<ResourceDetailsViewModel> DetailsShown { get; } = [];

    public List<ImportBatchViewModel> BatchesShown { get; } = [];

    public Queue<IReadOnlyList<string>> ImportFiles { get; } = new();

    public Queue<IReadOnlyList<string>> Packages { get; } = new();

    public Queue<string?> ReplaceFiles { get; } = new();

    public Queue<string?> ExportFiles { get; } = new();

    public Queue<string?> ExportFolders { get; } = new();

    public Queue<string?> ExportPackages { get; } = new();

    public Queue<OverwritePromptResult> OverwriteAnswers { get; } = new();

    public YesNoCancelResult YesNoCancelAnswer { get; set; } = YesNoCancelResult.No;

    public ExportDuplicatesPromptResult ExportDuplicatesAnswer { get; set; } = ExportDuplicatesPromptResult.Reject;

    public int ChoiceAnswer { get; set; }

    public bool DbcWarningAnswer { get; set; } = true;

    public int DbcWarningsShown { get; private set; }

    public IReadOnlyList<(string FileName, byte[] Data)> ThumbnailFiles { get; set; } = [];

    public List<string> Messages { get; } = [];

    public List<string> SuggestedExportNames { get; } = [];

    public string? ClipboardText { get; set; }

    public byte[]? ClipboardPayload { get; set; }

    public IReadOnlyList<string> ClipboardFiles { get; set; } = [];

    public Task<bool> ShowResourceDetailsAsync(ResourceDetailsViewModel details)
    {
        DetailsShown.Add(details);
        return Task.FromResult(OnResourceDetails(details));
    }

    public Task<bool> ShowImportBatchAsync(ImportBatchViewModel batch)
    {
        BatchesShown.Add(batch);
        return Task.FromResult(OnImportBatch(batch));
    }

    public Task<IReadOnlyList<string>> PickImportFilesAsync(string title, bool allowMultiple) =>
        Task.FromResult(ImportFiles.Count > 0 ? ImportFiles.Dequeue() : (IReadOnlyList<string>)[]);

    public Task<IReadOnlyList<string>> PickPackagesAsync(string title) =>
        Task.FromResult(Packages.Count > 0 ? Packages.Dequeue() : (IReadOnlyList<string>)[]);

    public Task<string?> PickReplaceFileAsync(string title, uint resourceType) =>
        Task.FromResult(ReplaceFiles.Count > 0 ? ReplaceFiles.Dequeue() : null);

    public Task<string?> PickExportFileAsync(string suggestedFileName, string? startFolder = null)
    {
        SuggestedExportNames.Add(suggestedFileName);
        ExportStartFolders.Add(startFolder);
        return Task.FromResult(ExportFiles.Count > 0 ? ExportFiles.Dequeue() : null);
    }

    public Task<string?> PickExportFolderAsync(string? startFolder = null)
    {
        ExportStartFolders.Add(startFolder);
        return Task.FromResult(ExportFolders.Count > 0 ? ExportFolders.Dequeue() : null);
    }

    /// <summary>The start folder passed to each export file/folder picker, in order.</summary>
    public List<string?> ExportStartFolders { get; } = [];

    public Task<string?> PickExportPackageAsync() =>
        Task.FromResult(ExportPackages.Count > 0 ? ExportPackages.Dequeue() : null);

    public Task<OverwritePromptResult> AskOverwriteAsync(string path) =>
        Task.FromResult(OverwriteAnswers.Count > 0 ? OverwriteAnswers.Dequeue() : OverwritePromptResult.No);

    public Task<YesNoCancelResult> AskYesNoCancelAsync(string message, string title) =>
        Task.FromResult(YesNoCancelAnswer);

    public Task<ExportDuplicatesPromptResult> AskExportDuplicatesAsync() =>
        Task.FromResult(ExportDuplicatesAnswer);

    public Task<int> AskChoiceAsync(string message, string title, IReadOnlyList<string> options, int defaultIndex, int cancelIndex)
    {
        Messages.Add(message);
        return Task.FromResult(ChoiceAnswer);
    }

    public Task<bool> ConfirmDbcWarningAsync()
    {
        DbcWarningsShown++;
        return Task.FromResult(DbcWarningAnswer);
    }

    public Task<IReadOnlyList<(string FileName, byte[] Data)>> ConvertThumbnailImagesAsync() =>
        Task.FromResult(ThumbnailFiles);

    public Task ShowMessageAsync(string message, string title)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task SetClipboardTextAsync(string text)
    {
        ClipboardText = text;
        ClipboardPayload = null;
        return Task.CompletedTask;
    }

    public Task SetClipboardResourcesAsync(byte[] payload, string keysText)
    {
        ClipboardPayload = payload;
        ClipboardText = keysText;
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetClipboardResourcesAsync() => Task.FromResult(ClipboardPayload);

    public Task<IReadOnlyList<string>> GetClipboardFilesAsync() => Task.FromResult(ClipboardFiles);
}
