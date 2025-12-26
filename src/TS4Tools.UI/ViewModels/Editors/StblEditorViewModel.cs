using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.Hashing;

namespace TS4Tools.UI.ViewModels.Editors;

public partial class StblEditorViewModel : ViewModelBase
{
    private StblResource? _resource;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private StblEntryViewModel? _selectedEntry;

    [ObservableProperty]
    private string _newKeyText = string.Empty;

    [ObservableProperty]
    private string _newValueText = string.Empty;

    public ObservableCollection<StblEntryViewModel> Entries { get; } = [];
    public ObservableCollection<StblEntryViewModel> FilteredEntries { get; } = [];

    public int EntryCount => _resource?.Count ?? 0;

    public StblEditorViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilterText))
            {
                ApplyFilter();
            }
        };
    }

    public void LoadResource(StblResource resource)
    {
        _resource = resource;
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        Entries.Clear();
        FilteredEntries.Clear();

        if (_resource == null) return;

        foreach (var entry in _resource.Entries)
        {
            var vm = new StblEntryViewModel(entry.KeyHash, entry.Value);
            vm.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(StblEntryViewModel.Value) && sender is StblEntryViewModel changedEntry)
                {
                    _resource[changedEntry.KeyHash] = changedEntry.Value;
                }
            };
            Entries.Add(vm);
            FilteredEntries.Add(vm);
        }

        OnPropertyChanged(nameof(EntryCount));
    }

    private void ApplyFilter()
    {
        FilteredEntries.Clear();

        var filter = FilterText.Trim().ToLowerInvariant();
        var filtered = string.IsNullOrEmpty(filter)
            ? Entries
            : Entries.Where(e =>
                e.KeyHex.Contains(filter, System.StringComparison.OrdinalIgnoreCase) ||
                e.Value.Contains(filter, System.StringComparison.OrdinalIgnoreCase));

        foreach (var entry in filtered)
        {
            FilteredEntries.Add(entry);
        }
    }

    [RelayCommand]
    private void AddEntry()
    {
        if (_resource == null || string.IsNullOrWhiteSpace(NewValueText)) return;

        uint keyHash;
        if (string.IsNullOrWhiteSpace(NewKeyText))
        {
            // Auto-generate hash from value
            keyHash = FnvHash.Fnv32Lower(NewValueText);
        }
        else if (NewKeyText.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
        {
            keyHash = uint.Parse(NewKeyText[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
        else
        {
            // Hash the key text
            keyHash = FnvHash.Fnv32Lower(NewKeyText);
        }

        _resource.Add(keyHash, NewValueText);
        var vm = new StblEntryViewModel(keyHash, NewValueText);
        Entries.Add(vm);
        ApplyFilter();

        NewKeyText = string.Empty;
        NewValueText = string.Empty;
        OnPropertyChanged(nameof(EntryCount));
    }

    [RelayCommand]
    private void DeleteEntry()
    {
        if (_resource == null || SelectedEntry == null) return;

        _resource.Remove(SelectedEntry.KeyHash);
        Entries.Remove(SelectedEntry);
        FilteredEntries.Remove(SelectedEntry);
        SelectedEntry = null;
        OnPropertyChanged(nameof(EntryCount));
    }

    public ReadOnlyMemory<byte> GetData() => _resource?.Data ?? ReadOnlyMemory<byte>.Empty;
}

public partial class StblEntryViewModel : ViewModelBase
{
    public uint KeyHash { get; }

    public string KeyHex => $"0x{KeyHash:X8}";

    [ObservableProperty]
    private string _value;

    public StblEntryViewModel(uint keyHash, string value)
    {
        KeyHash = keyHash;
        _value = value;
    }
}
