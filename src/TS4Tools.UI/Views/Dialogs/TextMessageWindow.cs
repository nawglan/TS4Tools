using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// A simple dialog showing selectable text with an OK button, or Yes and No buttons.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 2506-2565 (CopyableMessageBox use for
/// the Help menu: contents of the warranty, licence and command line help).
/// </remarks>
public sealed class TextMessageWindow : Window
{
    private readonly Button? _yesButton;
    private readonly Button? _noButton;
    private readonly Button? _okButton;

    public TextMessageWindow(string title, string text, bool yesNo = false)
    {
        Title = title;
        Width = 640;
        Height = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var message = new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 12, 0, 0)
        };

        if (yesNo)
        {
            _yesButton = new Button { Content = "_Yes", Padding = new Avalonia.Thickness(20, 6), IsDefault = true };
            _noButton = new Button { Content = "_No", Padding = new Avalonia.Thickness(20, 6), IsCancel = true };
            _yesButton.Click += OnYes;
            _noButton.Click += OnNo;
            buttons.Children.Add(_yesButton);
            buttons.Children.Add(_noButton);
        }
        else
        {
            _okButton = new Button { Content = "_OK", Padding = new Avalonia.Thickness(24, 6), IsDefault = true, IsCancel = true };
            _okButton.Click += OnYes;
            buttons.Children.Add(_okButton);
        }

        var root = new DockPanel { Margin = new Avalonia.Thickness(16) };
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(new ScrollViewer { Content = message });
        Content = root;

        Closed += OnClosed;
    }

    private void OnYes(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(true);

    private void OnNo(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        if (_yesButton != null) _yesButton.Click -= OnYes;
        if (_noButton != null) _noButton.Click -= OnNo;
        if (_okButton != null) _okButton.Click -= OnYes;
    }
}
