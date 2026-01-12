using Avalonia.Controls;
using Avalonia.Interactivity;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog warning about experimental DBC import functionality.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/ExperimentalDBCWarning.cs
/// Source: legacy_references/Sims4Tools/s4pe/Import/ExperimentalDBCWarning.Designer.cs
/// </remarks>
public partial class DbcWarningWindow : Window
{
    /// <summary>
    /// Gets whether the user confirmed understanding the risks.
    /// </summary>
    public bool Confirmed { get; private set; }

    /// <summary>
    /// Gets whether the prompt autosave option is checked.
    /// </summary>
    public static bool PromptAutosave
    {
        get => SettingsService.Instance.Settings.PromptDbcAutosave;
        set => SettingsService.Instance.Settings.PromptDbcAutosave = value;
    }

    public DbcWarningWindow()
    {
        InitializeComponent();
        DataContext = this;

        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
        UnderstandComboBox.SelectionChanged += UnderstandComboBox_SelectionChanged;

        // Load current settings
        PromptAutosaveCheckBox.IsChecked = PromptAutosave;
    }

    private void UnderstandComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Only enable OK button when "Yes, proceed" is selected (index 2)
        // Source: ExperimentalDBCWarning.cs lines 52-55
        OkButton.IsEnabled = UnderstandComboBox.SelectedIndex == 2;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;

        // Save the autosave setting
        PromptAutosave = PromptAutosaveCheckBox.IsChecked == true;
        SettingsService.Instance.Save();

        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
