using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog for getting a numeric value from the user.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/GetNumberDialog.cs
/// </remarks>
public partial class GetNumberWindow : Window
{
    public GetNumberWindow()
    {
        InitializeComponent();
    }

    public GetNumberWindow(string message, string title, int min, int max, int value)
        : this()
    {
        Title = title;
        MessageText.Text = message;
        NumberInput.Minimum = min;
        NumberInput.Maximum = max;
        NumberInput.Value = value;
    }

    /// <summary>
    /// Gets the selected value.
    /// </summary>
    public int Value => (int)(NumberInput.Value ?? 0);

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
