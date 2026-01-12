using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TS4Tools.UI.Services;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog for organizing custom places (folder shortcuts in file dialogs).
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseCustomPlacesDialog.cs
/// </remarks>
public partial class OrganiseCustomPlacesWindow : Window
{
    private readonly AppSettings _settings;
    private readonly List<string> _places = [];

    public OrganiseCustomPlacesWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Instance.Settings;

        LoadPlaces();

        MaxPlacesNumeric.Value = _settings.CustomPlacesCount;

        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
        AddButton.Click += AddButton_Click;
        DeleteButton.Click += DeleteButton_Click;
        MoveUpButton.Click += MoveUpButton_Click;
        MoveDownButton.Click += MoveDownButton_Click;
        PlacesListBox.SelectionChanged += PlacesListBox_SelectionChanged;
        MaxPlacesNumeric.ValueChanged += MaxPlacesNumeric_ValueChanged;
    }

    /// <summary>
    /// Loads custom places from settings.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseCustomPlacesDialog.cs lines 33-41
    /// </remarks>
    private void LoadPlaces()
    {
        _places.Clear();
        _places.AddRange(_settings.CustomPlaces);
        RefreshList();
    }

    private void RefreshList()
    {
        var items = new List<string>();
        for (var i = 0; i < _places.Count; i++)
        {
            items.Add($"{i + 1}. {_places[i]}");
        }

        PlacesListBox.ItemsSource = null;
        PlacesListBox.ItemsSource = items;

        UpdateButtonStates();
    }

    /// <summary>
    /// Updates button enabled states based on selection.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseCustomPlacesDialog.cs lines 43-48
    /// </remarks>
    private void UpdateButtonStates()
    {
        var index = PlacesListBox.SelectedIndex;
        DeleteButton.IsEnabled = index >= 0;
        MoveUpButton.IsEnabled = index > 0;
        MoveDownButton.IsEnabled = index >= 0 && index < _places.Count - 1;
        AddButton.IsEnabled = _places.Count < (int)(MaxPlacesNumeric.Value ?? 5);
    }

    private void PlacesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateButtonStates();
    }

    /// <summary>
    /// Adds a new custom place via folder picker.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseCustomPlacesDialog.cs lines 72-87
    /// </remarks>
    private async void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder for Quick Access",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            if (_places.Count < (int)(MaxPlacesNumeric.Value ?? 5))
            {
                _places.Add(path);
                RefreshList();
                PlacesListBox.SelectedIndex = _places.Count - 1;
            }
        }
    }

    /// <summary>
    /// Deletes the selected custom place.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseCustomPlacesDialog.cs lines 89-98
    /// </remarks>
    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        var index = PlacesListBox.SelectedIndex;
        if (index < 0) return;

        _places.RemoveAt(index);
        RefreshList();

        if (_places.Count > 0)
        {
            PlacesListBox.SelectedIndex = Math.Min(index, _places.Count - 1);
        }
    }

    /// <summary>
    /// Moves the selected place up.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseCustomPlacesDialog.cs lines 50-70
    /// </remarks>
    private void MoveUpButton_Click(object? sender, RoutedEventArgs e)
    {
        MovePlace(-1);
    }

    private void MoveDownButton_Click(object? sender, RoutedEventArgs e)
    {
        MovePlace(1);
    }

    private void MovePlace(int direction)
    {
        var index = PlacesListBox.SelectedIndex;
        if (index < 0) return;

        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _places.Count) return;

        var item = _places[index];
        _places.RemoveAt(index);
        _places.Insert(newIndex, item);

        RefreshList();
        PlacesListBox.SelectedIndex = newIndex;
    }

    /// <summary>
    /// Handles max places change - trims list if needed.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/Settings/OrganiseCustomPlacesDialog.cs lines 100-118
    /// </remarks>
    private void MaxPlacesNumeric_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var max = (int)(MaxPlacesNumeric.Value ?? 5);

        while (_places.Count > max)
        {
            _places.RemoveAt(_places.Count - 1);
        }

        RefreshList();
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        SavePlaces();
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void SavePlaces()
    {
        _settings.CustomPlaces.Clear();
        _settings.CustomPlaces.AddRange(_places);
        _settings.CustomPlacesCount = (int)(MaxPlacesNumeric.Value ?? 5);
        SettingsService.Instance.Save();
    }
}
