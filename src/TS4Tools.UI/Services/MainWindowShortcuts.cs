using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.UI.ViewModels;

namespace TS4Tools.UI.Services;

/// <summary>
/// Maps the key gestures shown on the main menu items to their commands.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1282-1324 (menu items with ShortcutKeys)
/// and MenuBarWidget.cs. Avalonia's <c>MenuItem.InputGesture</c> only displays a gesture, so each one is
/// bound here. Copy, paste, duplicate, select all and copy key act on the resource browser; as in legacy
/// (where the browser list has the focus) they do not fire while a text input has the focus, so the
/// text box keeps its own copy/paste/select-all.
/// </remarks>
public static class MainWindowShortcuts
{
    private const KeyModifiers Ctrl = KeyModifiers.Control;
    private const KeyModifiers CtrlShift = KeyModifiers.Control | KeyModifiers.Shift;

    private sealed record Binding(Key Key, KeyModifiers Modifiers, bool BrowserOnly, Func<MainWindowViewModel, ICommand> Command,
        Func<MainWindowViewModel, bool>? Enabled = null);

    private static readonly Binding[] Bindings =
    [
        new(Key.N, Ctrl, false, vm => vm.NewPackageCommand),
        new(Key.O, Ctrl, false, vm => vm.OpenPackageCommand),
        new(Key.S, Ctrl, false, vm => vm.SaveCommand, vm => vm.CanSave),
        new(Key.S, CtrlShift, false, vm => vm.SaveAsCommand, vm => vm.HasOpenPackage),
        new(Key.F, Ctrl, false, vm => vm.SearchCommand, vm => vm.HasOpenPackage),
        new(Key.F5, KeyModifiers.None, false, vm => vm.RefreshCommand, vm => vm.HasOpenPackage),
        new(Key.F1, KeyModifiers.None, false, vm => vm.AboutCommand),
        new(Key.B, CtrlShift, false, vm => vm.AddCurrentToBookmarksCommand, vm => vm.HasOpenPackage),
        new(Key.M, CtrlShift, false, vm => vm.ImportResourceCommand, vm => vm.HasOpenPackage),
        new(Key.X, CtrlShift, false, vm => vm.ExportResourceCommand, vm => vm.HasSelectedResource),

        // Browser shortcuts (BrowserWidget has the focus in legacy)
        new(Key.C, Ctrl, true, vm => vm.CopyResourceCommand, vm => vm.HasSelectedResource),
        new(Key.V, Ctrl, true, vm => vm.PasteResourceCommand, vm => vm.HasOpenPackage),
        new(Key.D, Ctrl, true, vm => vm.DuplicateResourceCommand, vm => vm.HasSelectedResource),
        new(Key.A, Ctrl, true, vm => vm.SelectAllCommand, vm => vm.HasOpenPackage),
        new(Key.C, CtrlShift, true, vm => vm.CopyResourceKeyCommand, vm => vm.HasSelectedResource),
    ];

    /// <summary>
    /// Finds the command for a key press.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="modifiers">The held modifiers.</param>
    /// <param name="viewModel">The main window view model.</param>
    /// <param name="focusedElement">The control with the keyboard focus, if any.</param>
    /// <returns>The command, or null if the key is not a shortcut (or a text input should handle it).</returns>
    public static ICommand? Resolve(Key key, KeyModifiers modifiers, MainWindowViewModel viewModel, object? focusedElement)
    {
        foreach (var binding in Bindings)
        {
            if (binding.Key != key || binding.Modifiers != modifiers) continue;
            if (binding.BrowserOnly && IsTextInput(focusedElement)) return null;
            // The menu items are disabled in these states (IsEnabled bindings), so the gesture is too
            if (binding.Enabled != null && !binding.Enabled(viewModel)) return null;
            return binding.Command(viewModel);
        }
        return null;
    }

    /// <summary>
    /// Runs the command for a key press if there is one and it can execute.
    /// </summary>
    /// <returns>True if a command was run (the key press is then handled).</returns>
    public static async Task<bool> TryExecuteAsync(Key key, KeyModifiers modifiers, MainWindowViewModel viewModel, object? focusedElement)
    {
        var command = Resolve(key, modifiers, viewModel, focusedElement);
        if (command == null || !command.CanExecute(null)) return false;

        if (command is IAsyncRelayCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(null);
        }
        else
        {
            command.Execute(null);
        }
        return true;
    }

    /// <summary>Whether the focused control edits text (and so owns copy/paste/select all).</summary>
    public static bool IsTextInput(object? focusedElement) => focusedElement is TextBox;
}
