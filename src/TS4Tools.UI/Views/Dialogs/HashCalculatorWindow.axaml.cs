using Avalonia.Controls;
using Avalonia.Interactivity;
using TS4Tools.Wrappers.Hashing;

namespace TS4Tools.UI.Views.Dialogs;

public partial class HashCalculatorWindow : Window
{
    private readonly EventHandler<TextChangedEventArgs> _textChangedHandler;
    private readonly EventHandler<RoutedEventArgs> _isCheckedChangedHandler;

    public HashCalculatorWindow()
    {
        InitializeComponent();

        _textChangedHandler = (_, _) => Calculate();
        _isCheckedChangedHandler = (_, _) => Calculate();

        InputTextBox.TextChanged += _textChangedHandler;
        LowercaseCheckBox.IsCheckedChanged += _isCheckedChangedHandler;
    }

    protected override void OnClosed(EventArgs e)
    {
        InputTextBox.TextChanged -= _textChangedHandler;
        LowercaseCheckBox.IsCheckedChanged -= _isCheckedChangedHandler;
        base.OnClosed(e);
    }

    private void Calculate()
    {
        var input = InputTextBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            ClearResults();
            return;
        }

        var useLowercase = LowercaseCheckBox.IsChecked == true;

        var fnv32 = useLowercase ? FnvHash.Fnv32Lower(input) : FnvHash.Fnv32(input);
        var fnv64 = useLowercase ? FnvHash.Fnv64Lower(input) : FnvHash.Fnv64(input);
        var fnv24 = FnvHash.Fnv24(input);

        Fnv32Result.Text = fnv32.ToString(CultureInfo.InvariantCulture);
        Fnv32HexResult.Text = $"0x{fnv32:X8}";
        Fnv64Result.Text = fnv64.ToString(CultureInfo.InvariantCulture);
        Fnv64HexResult.Text = $"0x{fnv64:X16}";
        Fnv24Result.Text = fnv24.ToString(CultureInfo.InvariantCulture);
        Fnv24HexResult.Text = $"0x{fnv24:X6}";
    }

    private void ClearResults()
    {
        Fnv32Result.Text = string.Empty;
        Fnv32HexResult.Text = string.Empty;
        Fnv64Result.Text = string.Empty;
        Fnv64HexResult.Text = string.Empty;
        Fnv24Result.Text = string.Empty;
        Fnv24HexResult.Text = string.Empty;
    }

    private void CalculateButton_Click(object? sender, RoutedEventArgs e)
    {
        Calculate();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
