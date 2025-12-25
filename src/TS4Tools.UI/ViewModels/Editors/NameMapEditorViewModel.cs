using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.Hashing;

namespace TS4Tools.UI.ViewModels.Editors;

public partial class NameMapEditorViewModel : ViewModelBase
{
    private NameMapResource? _resource;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private NameMapEntryViewModel? _selectedEntry;

    [ObservableProperty]
    private string _newHashText = string.Empty;

    [ObservableProperty]
    private string _newNameText = string.Empty;

    public ObservableCollection<NameMapEntryViewModel> Entries { get; } = [];
    public ObservableCollection<NameMapEntryViewModel> FilteredEntries { get; } = [];

    public int EntryCount => _resource?.Count ?? 0;

    public NameMapEditorViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilterText))
            {
                ApplyFilter();
            }
        };
    }

    public void LoadResource(NameMapResource resource)
    {
        _resource = resource;
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        Entries.Clear();
        FilteredEntries.Clear();

        if (_resource == null) return;

        foreach (var (hash, name) in _resource)
        {
            var vm = new NameMapEntryViewModel(hash, name);
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
                e.HashHex.Contains(filter, System.StringComparison.OrdinalIgnoreCase) ||
                e.Name.Contains(filter, System.StringComparison.OrdinalIgnoreCase));

        foreach (var entry in filtered)
        {
            FilteredEntries.Add(entry);
        }
    }

    [RelayCommand]
    private void AddEntry()
    {
        if (_resource == null || string.IsNullOrWhiteSpace(NewNameText)) return;

        ulong hash;
        if (string.IsNullOrWhiteSpace(NewHashText))
        {
            // Auto-generate hash from name
            hash = FnvHash.Fnv64Lower(NewNameText);
        }
        else if (NewHashText.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
        {
            hash = ulong.Parse(NewHashText[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
        else
        {
            // Hash the text
            hash = FnvHash.Fnv64Lower(NewHashText);
        }

        _resource[hash] = NewNameText;
        var vm = new NameMapEntryViewModel(hash, NewNameText);
        Entries.Add(vm);
        ApplyFilter();

        NewHashText = string.Empty;
        NewNameText = string.Empty;
        OnPropertyChanged(nameof(EntryCount));
    }

    [RelayCommand]
    private void DeleteEntry()
    {
        if (_resource == null || SelectedEntry == null) return;

        _resource.Remove(SelectedEntry.Hash);
        Entries.Remove(SelectedEntry);
        FilteredEntries.Remove(SelectedEntry);
        SelectedEntry = null;
        OnPropertyChanged(nameof(EntryCount));
    }

    public ReadOnlyMemory<byte> GetData() => _resource?.Data ?? ReadOnlyMemory<byte>.Empty;
}

public partial class NameMapEntryViewModel : ViewModelBase
{
    public ulong Hash { get; }

    public string HashHex => $"0x{Hash:X16}";

    [ObservableProperty]
    private string _name;

    public NameMapEntryViewModel(ulong hash, string name)
    {
        Hash = hash;
        _name = name;
    }
}
