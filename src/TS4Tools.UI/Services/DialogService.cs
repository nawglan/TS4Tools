using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace TS4Tools.UI.Services;

/// <summary>
/// Avalonia implementation of <see cref="IDialogService"/> using modal windows owned by the main window.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 643-672 (save prompt) and
/// lines 2617-2641 (commit prompt), which use CopyableMessageBox with YesNoCancel buttons.
/// </remarks>
public sealed class DialogService : IDialogService
{
    // Source: legacy_references/Sims4Tools/s4pe/MainForm.cs line 73 (myName - the application name)
    private const string AppName = "TS4Tools";

    // Source: legacy_references/Sims4Tools/s4pe/Properties/Settings.settings DBPFFilesAndAll
    private static readonly FilePickerFileType PackageFileType = new("Sims 4 Package") { Patterns = ["*.package", "*.dbc", "*.world", "*.nhd"] };

    /// <summary>
    /// Gets the shared dialog service instance.
    /// </summary>
    public static DialogService Instance { get; } = new();

    private DialogService()
    {
    }

    /// <inheritdoc />
    public async Task<CommitPromptResult> AskCommitChangesAsync(string resourceName)
    {
        // Source: MainForm.cs lines 2619-2628 - default button is "No" (index 1)
        var choice = await ShowYesNoCancelAsync($"Commit changes to {resourceName}?", defaultIndex: 1);
        return choice switch
        {
            0 => CommitPromptResult.Commit,
            1 => CommitPromptResult.Discard,
            _ => CommitPromptResult.Cancel
        };
    }

    /// <inheritdoc />
    public async Task<SavePromptResult> AskSaveChangesAsync()
    {
        // Source: MainForm.cs lines 645-649 - default button is "Cancel" (index 2)
        var choice = await ShowYesNoCancelAsync("Current package has unsaved changes.\nSave now?", defaultIndex: 2);
        return choice switch
        {
            0 => SavePromptResult.Save,
            1 => SavePromptResult.DontSave,
            _ => SavePromptResult.Cancel
        };
    }

    /// <inheritdoc />
    public Task<string?> PickSavePackagePathAsync(string title, string? suggestedFileName = null) =>
        AvaloniaStorageService.Instance.PickSaveFileAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = "package",
            FileTypeChoices = [PackageFileType],
            SuggestedFileName = suggestedFileName
        });

    private static Window? GetMainWindow() => AvaloniaStorageService.Instance.MainWindow;

    /// <summary>
    /// Shows a modal Yes/No/Cancel prompt. Returns 0 (Yes), 1 (No) or 2 (Cancel / closed).
    /// </summary>
    private static async Task<int> ShowYesNoCancelAsync(string message, int defaultIndex)
    {
        var owner = GetMainWindow();
        if (owner == null) return 2;

        var result = 2;
        var dialog = new Window
        {
            Title = AppName,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 320,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 16, 0, 0)
        };

        string[] labels = ["_Yes", "_No", "Cancel"];
        for (int i = 0; i < labels.Length; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = labels[i],
                MinWidth = 72,
                IsDefault = i == defaultIndex,
                IsCancel = i == 2
            };
            button.Click += (_, _) =>
            {
                result = index;
                dialog.Close();
            };
            buttons.Children.Add(button);
        }

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 480 },
                buttons
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
