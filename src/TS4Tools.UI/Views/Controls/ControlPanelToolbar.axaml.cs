using Avalonia.Controls;
using Avalonia.Interactivity;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Control panel toolbar with view buttons, helper buttons, and display options.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs
/// </remarks>
public partial class ControlPanelToolbar : UserControl
{
    private readonly AppSettings _settings;

    public ControlPanelToolbar()
    {
        InitializeComponent();
        _settings = SettingsService.Instance.Settings;

        LoadSettings();

        // Wire up events
        SortCheckBox.IsCheckedChanged += SortCheckBox_Changed;
        HexOnlyCheckBox.IsCheckedChanged += HexOnlyCheckBox_Changed;
        UseNamesCheckBox.IsCheckedChanged += UseNamesCheckBox_Changed;
        UseTagsCheckBox.IsCheckedChanged += UseTagsCheckBox_Changed;

        AutoOffRadio.IsCheckedChanged += AutoRadio_Changed;
        AutoHexRadio.IsCheckedChanged += AutoRadio_Changed;
        AutoPreviewRadio.IsCheckedChanged += AutoRadio_Changed;

        HexButton.Click += HexButton_Click;
        ValueButton.Click += ValueButton_Click;
        GridButton.Click += GridButton_Click;
        Helper1Button.Click += Helper1Button_Click;
        Helper2Button.Click += Helper2Button_Click;
        HexEditButton.Click += HexEditButton_Click;
        CommitButton.Click += CommitButton_Click;
    }

    /// <summary>
    /// Loads settings from the settings service.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs lines 35-53
    /// </remarks>
    private void LoadSettings()
    {
        SortCheckBox.IsChecked = _settings.Sort;
        HexOnlyCheckBox.IsChecked = _settings.HexOnly;
        UseNamesCheckBox.IsChecked = _settings.UseNames;
        UseTagsCheckBox.IsChecked = _settings.UseTags;

        // Auto mode: 0=Off, 1=Hex, 2=Preview
        switch (_settings.AutoUpdateChoice)
        {
            case 0: AutoOffRadio.IsChecked = true; break;
            case 1: AutoHexRadio.IsChecked = true; break;
            case 2: AutoPreviewRadio.IsChecked = true; break;
            default: AutoOffRadio.IsChecked = true; break;
        }

        // Initial button states
        SetButtonsEnabled(false);
    }

    /// <summary>
    /// Saves settings to the settings service.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs lines 55-62
    /// </remarks>
    public void SaveSettings()
    {
        _settings.Sort = SortCheckBox.IsChecked == true;
        _settings.HexOnly = HexOnlyCheckBox.IsChecked == true;
        _settings.UseNames = UseNamesCheckBox.IsChecked == true;
        _settings.UseTags = UseTagsCheckBox.IsChecked == true;
        _settings.AutoUpdateChoice = AutoHexRadio.IsChecked == true ? 1 : AutoPreviewRadio.IsChecked == true ? 2 : 0;

        SettingsService.Instance.Save();
    }

    /// <summary>
    /// Sets the enabled state for action buttons.
    /// </summary>
    public void SetButtonsEnabled(bool enabled)
    {
        HexButton.IsEnabled = enabled;
        ValueButton.IsEnabled = enabled;
        GridButton.IsEnabled = enabled;
        HexEditButton.IsEnabled = enabled;
        CommitButton.IsEnabled = enabled;
    }

    /// <summary>
    /// Sets the enabled state and labels for helper buttons.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/ControlPanel/ControlPanel.cs lines 204-256
    /// </remarks>
    public void SetHelper1(bool enabled, string label, string tooltip)
    {
        Helper1Button.IsEnabled = enabled;
        Helper1Button.Content = label;
        ToolTip.SetTip(Helper1Button, tooltip);
    }

    public void SetHelper2(bool enabled, string label, string tooltip)
    {
        Helper2Button.IsEnabled = enabled;
        Helper2Button.Content = label;
        ToolTip.SetTip(Helper2Button, tooltip);
    }

    #region Events

    public event EventHandler? SortChanged;
    public event EventHandler? HexOnlyChanged;
    public event EventHandler? UseNamesChanged;
    public event EventHandler? UseTagsChanged;
    public event EventHandler? AutoChanged;
    public event EventHandler? HexClick;
    public event EventHandler? ValueClick;
    public event EventHandler? GridClick;
    public event EventHandler? Helper1Click;
    public event EventHandler? Helper2Click;
    public event EventHandler? HexEditClick;
    public event EventHandler? CommitClick;

    private void SortCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        SaveSettings();
        SortChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HexOnlyCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        SaveSettings();
        HexOnlyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UseNamesCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        SaveSettings();
        UseNamesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UseTagsCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        SaveSettings();
        UseTagsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AutoRadio_Changed(object? sender, RoutedEventArgs e)
    {
        SaveSettings();
        AutoChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HexButton_Click(object? sender, RoutedEventArgs e)
    {
        HexClick?.Invoke(this, EventArgs.Empty);
    }

    private void ValueButton_Click(object? sender, RoutedEventArgs e)
    {
        ValueClick?.Invoke(this, EventArgs.Empty);
    }

    private void GridButton_Click(object? sender, RoutedEventArgs e)
    {
        GridClick?.Invoke(this, EventArgs.Empty);
    }

    private void Helper1Button_Click(object? sender, RoutedEventArgs e)
    {
        Helper1Click?.Invoke(this, EventArgs.Empty);
    }

    private void Helper2Button_Click(object? sender, RoutedEventArgs e)
    {
        Helper2Click?.Invoke(this, EventArgs.Empty);
    }

    private void HexEditButton_Click(object? sender, RoutedEventArgs e)
    {
        HexEditClick?.Invoke(this, EventArgs.Empty);
    }

    private void CommitButton_Click(object? sender, RoutedEventArgs e)
    {
        CommitClick?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Properties

    public bool Sort
    {
        get => SortCheckBox.IsChecked == true;
        set => SortCheckBox.IsChecked = value;
    }

    public bool HexOnly
    {
        get => HexOnlyCheckBox.IsChecked == true;
        set => HexOnlyCheckBox.IsChecked = value;
    }

    public bool UseNames
    {
        get => UseNamesCheckBox.IsChecked == true;
        set => UseNamesCheckBox.IsChecked = value;
    }

    public bool UseTags
    {
        get => UseTagsCheckBox.IsChecked == true;
        set => UseTagsCheckBox.IsChecked = value;
    }

    public int AutoChoice
    {
        get => AutoHexRadio.IsChecked == true ? 1 : AutoPreviewRadio.IsChecked == true ? 2 : 0;
        set
        {
            switch (value)
            {
                case 1: AutoHexRadio.IsChecked = true; break;
                case 2: AutoPreviewRadio.IsChecked = true; break;
                default: AutoOffRadio.IsChecked = true; break;
            }
        }
    }

    #endregion
}
