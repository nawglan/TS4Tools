using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.UI.Services;
using TS4Tools.UI.ViewModels.Editors;
using TS4Tools.UI.Views.Controls;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// Edit menu actions on the preview: copy, save as image and open in the text editor.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 990-1253 (Edit menu: EditCopy, EditSavePreview,
/// EditFloat, EditOte) and lines 3095-3172 (TextEdit).
/// </remarks>
public partial class MainWindowViewModel
{
    private IPreviewClipboard? _previewClipboard;
    private IProcessLauncher? _processLauncher;

    /// <summary>The clipboard preview text and images are copied to (replaceable for tests).</summary>
    internal IPreviewClipboard PreviewClipboard
    {
        get => _previewClipboard ??= new AvaloniaPreviewClipboard();
        set => _previewClipboard = value;
    }

    private IHelperClipboard? _helperClipboard;

    /// <summary>The clipboard used to exchange resources with helpers that take no file (replaceable for tests).</summary>
    internal IHelperClipboard HelperClipboard
    {
        get => _helperClipboard ??= new AvaloniaHelperClipboard();
        set => _helperClipboard = value;
    }

    /// <summary>Starts external editors (replaceable for tests).</summary>
    internal IProcessLauncher ProcessLauncher
    {
        get => _processLauncher ?? Services.ProcessLauncher.Instance;
        set => _processLauncher = value;
    }

    /// <summary>Finds the text selected in a preview control (replaceable for tests).</summary>
    internal Func<Control, string?> SelectedTextFinder { get; set; } = PreviewTextSelection.Find;

    /// <summary>The last external text editor session started by Open in Text Editor, for tests.</summary>
    internal Task LastTextEditorSession { get; private set; } = Task.CompletedTask;

    /// <summary>The view model behind the control shown in the preview panel.</summary>
    private object? PreviewModel => (EditorContent as Control)?.DataContext;

    /// <summary>Whether the preview shows text that can be saved, copied or opened in the text editor.</summary>
    private bool IsTextBasedPreview => PreviewModel is StblEditorViewModel or NameMapEditorViewModel
        or TextEditorViewModel or SimDataViewerViewModel or HexViewerViewModel or ValueViewerViewModel;

    /// <summary>Whether the preview shows an image.</summary>
    private bool HasImagePreview => PreviewModel is ImageViewerViewModel { Image: not null };

    /// <summary>Whether a text editor program is set in the settings.</summary>
    /// <remarks>Source: MainForm.cs lines 2315-2318 (HasTextEditor)</remarks>
    public bool HasTextEditor => !string.IsNullOrEmpty(ShellSettings.TextEditorCommand);

    /// <summary>Whether Edit > Copy Preview has something to copy.</summary>
    /// <remarks>Source: MainForm.cs lines 1030-1050 (CanCopy)</remarks>
    public bool CanCopyPreview => HasSelectedResource && (IsTextBasedPreview || HasImagePreview);

    /// <summary>Whether the preview can be opened in the text editor.</summary>
    /// <remarks>Source: MainForm.cs lines 1165-1180 (CanOte)</remarks>
    public bool CanOpenInTextEditor => HasSelectedResource && HasTextEditor && IsTextBasedPreview;

    /// <summary>Whether the selected resource can be edited with the text editor.</summary>
    /// <remarks>Source: MainForm.cs lines 1304 (MBR_textEditor enabled when a resource is selected and HasTextEditor)</remarks>
    public bool CanEditInTextEditor => HasSelectedResource && HasTextEditor;

    private void NotifyPreviewActionsChanged()
    {
        OnPropertyChanged(nameof(CanCopyPreview));
        OnPropertyChanged(nameof(CanOpenInTextEditor));
        OnPropertyChanged(nameof(CanEditInTextEditor));
        OnPropertyChanged(nameof(CanSavePreview));
        OnPropertyChanged(nameof(CanFloat));
    }

    /// <summary>
    /// Builds the text of a text based preview.
    /// </summary>
    internal static string? GetPreviewTextFor(object? model)
    {
        var sb = new StringBuilder();
        switch (model)
        {
            case StblEditorViewModel stbl:
                foreach (var entry in stbl.Entries)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"0x{entry.KeyHash:X16}: {entry.Value}");
                }

                return sb.ToString();
            case NameMapEditorViewModel nameMap:
                foreach (var entry in nameMap.Entries)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"0x{entry.Hash:X16}: {entry.Name}");
                }

                return sb.ToString();
            case TextEditorViewModel text:
                return text.Text;
            case SimDataViewerViewModel simData:
                // SimData viewer has HeaderInfo and HexPreview - combine them
                return $"{simData.HeaderInfo}\n\n{simData.HexPreview}";
            case HexViewerViewModel hex:
                return hex.HexContent;
            case ValueViewerViewModel value:
                // Generic Value / fallback text / error view
                return value.Text;
            default:
                return null;
        }
    }

    /// <summary>
    /// Copies the selected preview text, or an image preview, to the clipboard. With no text selected
    /// the whole preview text is copied.
    /// </summary>
    /// <remarks>Source: MainForm.cs lines 1052-1092 (EditCopy)</remarks>
    [RelayCommand]
    private async Task CopyPreviewAsync()
    {
        if (PreviewModel is ImageViewerViewModel { Image: { } bitmap })
        {
            await PreviewClipboard.SetBitmapAsync(bitmap);
            StatusMessage = "Image copied to the clipboard";
            return;
        }

        var text = EditorContent is Control view ? SelectedTextFinder(view) : null;
        if (string.IsNullOrEmpty(text)) text = GetPreviewText();
        if (string.IsNullOrEmpty(text))
        {
            StatusMessage = "Nothing to copy";
            return;
        }

        await PreviewClipboard.SetTextAsync(NormaliseLines(text));
        StatusMessage = "Text copied to the clipboard";
    }

    /// <summary>Saves an image preview as a PNG file.</summary>
    private async Task SavePreviewImageAsync()
    {
        if (SelectedResource == null || PreviewModel is not ImageViewerViewModel { Image: { } bitmap }) return;

        var baseName = string.IsNullOrEmpty(SelectedResource.InstanceName)
            ? $"S4_{SelectedResource.Key.ResourceType:X8}_{SelectedResource.Key.ResourceGroup:X8}_{SelectedResource.Key.Instance:X16}"
            : SelectedResource.InstanceName;

        var path = await Storage.PickSaveFileAsync(new FilePickerSaveOptions
        {
            Title = "Save Preview Content",
            SuggestedFileName = baseName + ".png",
            FileTypeChoices = [new FilePickerFileType("PNG Image") { Patterns = ["*.png"] }, new FilePickerFileType("All Files") { Patterns = ["*"] }],
            DefaultExtension = ".png"
        });

        if (path == null) return;

        await SaveBitmapAsPngAsync(bitmap, path);
    }

    /// <summary>Writes a bitmap to a PNG file.</summary>
    internal async Task SaveBitmapAsPngAsync(Bitmap bitmap, string path)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, new PngBitmapEncoderOptions());
        await _fileSystem.WriteAllBytesAsync(path, stream.ToArray());
        StatusMessage = $"Preview saved to {_fileSystem.GetFileName(path)}";
    }

    /// <summary>
    /// Opens the preview text in the configured text editor: the text is written to a temporary file that is
    /// removed when the editor exits.
    /// </summary>
    /// <remarks>Source: MainForm.cs lines 1182-1250 (EditOte, p_Exited)</remarks>
    [RelayCommand]
    private async Task OpenInTextEditorAsync()
    {
        if (!CanOpenInTextEditor) return;

        var text = GetPreviewText();
        if (string.IsNullOrEmpty(text)) return;

        var settings = ShellSettings;
        var command = settings.TextEditorCommand!;
        var file = _fileSystem.CombinePath(_fileSystem.GetTempPath(), $"ts4tools-{Guid.NewGuid():N}.txt");
        await _fileSystem.WriteAllTextAsync(file, NormaliseLines(text));

        var quote = settings.TextEditorWantsQuotes ? "\"" : "";
        StatusMessage = "Opened preview in the text editor";
        LastTextEditorSession = RunEditorAndDeleteAsync(command, quote + file + quote, file);
    }

    private async Task RunEditorAndDeleteAsync(string command, string arguments, string file)
    {
        try
        {
            await ProcessLauncher.RunAsync(command, arguments);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Application failed to start: {command} ({ex.Message})";
        }
        finally
        {
            try
            {
                _fileSystem.DeleteFile(file);
            }
            catch (IOException)
            {
                // The editor may still hold the file; nothing more can be done.
            }
        }
    }

    /// <summary>
    /// Edits the selected resource with the configured text editor and imports the changes.
    /// </summary>
    /// <remarks>Source: MainForm.cs lines 3095-3172 (TextEdit via HelperManager.Edit)</remarks>
    [RelayCommand]
    private async Task EditInTextEditorAsync()
    {
        if (_package == null || SelectedResource == null || !HasTextEditor) return;

        var settings = ShellSettings;
        var key = SelectedResource.Key;
        var entry = ResolveEntry(SelectedResource);
        if (entry == null)
        {
            StatusMessage = "Resource not found";
            return;
        }

        var data = await _package.GetResourceDataAsync(entry);
        var file = _fileSystem.CombinePath(_fileSystem.GetTempPath(), $"S4_{key.ResourceType:X8}_{key.ResourceGroup:X8}_{key.Instance:X16}.txt");
        await _fileSystem.WriteAllBytesAsync(file, data.ToArray());
        var written = _fileSystem.GetLastWriteTimeUtc(file);

        try
        {
            var quote = settings.TextEditorWantsQuotes ? "\"" : "";
            StatusMessage = "Opening text editor...";
            await ProcessLauncher.RunAsync(settings.TextEditorCommand!, quote + file + quote);

            if (settings.TextEditorIgnoreTimestamp || _fileSystem.GetLastWriteTimeUtc(file) != written)
            {
                var modified = await _fileSystem.ReadAllBytesAsync(file);
                StatusMessage = await AfterEditAsync(SelectedResource, entry, modified)
                    ? "Resource updated from text editor"
                    : "Text editor changes not committed";
            }
            else
            {
                StatusMessage = "Text editor closed (no changes)";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Text editor error: {ex.Message}";
        }
        finally
        {
            _fileSystem.DeleteFile(file);
        }
    }

    /// <summary>
    /// Asks whether the data an external helper or editor produced should be committed and, if so,
    /// replaces the resource with it.
    /// </summary>
    /// <returns>True if the resource was replaced.</returns>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3201-3223 (AfterEdit: "Resource has been
    /// updated.  Commit changes?", then NewResource(selected, data, Replace, Compressed != 0) and add to the
    /// browser, which selects the new entry; the replaced entry stays listed, deleted).
    /// </remarks>
    private async Task<bool> AfterEditAsync(ResourceItemViewModel item, IResourceIndexEntry entry, byte[] data)
    {
        if (_package == null) return false;

        if (await _dialogs.AskCommitChangesAsync(GetResourceDisplayName(item)) != CommitPromptResult.Commit)
        {
            return false;
        }

        var newEntry = NewResource(entry.Key, data, ImportDuplicateHandling.Replace, entry.CompressionType != 0);
        if (newEntry == null) return false;

        AddResourceItem(newEntry, select: true);
        return true;
    }

    private static string NormaliseLines(string text)
    {
        // Source: MainForm.cs lines 1081-1090 (each line is re-appended with the platform newline)
        var sb = new StringBuilder();
        using var reader = new StringReader(text);
        for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
