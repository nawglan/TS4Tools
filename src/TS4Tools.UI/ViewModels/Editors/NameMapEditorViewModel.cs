using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.Hashing;

namespace TS4Tools.UI.ViewModels.Editors;

/// <remarks>
/// Change tracking source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 700-705
/// (resource_ResourceChanged) - edits flow into the wrapper, whose Changed event is
/// forwarded as <see cref="ContentChanged"/> so the main window can enable Commit.
/// </remarks>
public partial class NameMapEditorViewModel : ViewModelBase, IResourceEditorViewModel, IDisposable
{
    private bool _disposed;
    private NameMapResource? _resource;
    private readonly PropertyChangedEventHandler _selfPropertyChangedHandler;
    private readonly EventHandler _resourceChangedHandler;

    /// <inheritdoc />
    public event EventHandler? ContentChanged;
    private readonly List<(NameMapEntryViewModel ViewModel, PropertyChangedEventHandler Handler)> _subscriptions = [];

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
        _selfPropertyChangedHandler = (_, e) =>
        {
            if (e.PropertyName == nameof(FilterText))
            {
                ApplyFilter();
            }
        };
        PropertyChanged += _selfPropertyChangedHandler;
        _resourceChangedHandler = (_, _) => ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadResource(NameMapResource resource)
    {
        if (_resource != null)
        {
            _resource.Changed -= _resourceChangedHandler;
        }

        _resource = resource;
        _resource.Changed += _resourceChangedHandler;
        RefreshEntries();
    }

    private void ClearSubscriptions()
    {
        foreach (var (viewModel, handler) in _subscriptions)
        {
            viewModel.PropertyChanged -= handler;
        }
        _subscriptions.Clear();
    }

    private void RefreshEntries()
    {
        ClearSubscriptions();
        Entries.Clear();
        FilteredEntries.Clear();

        if (_resource == null) return;

        foreach (var (hash, name) in _resource)
        {
            var vm = CreateTrackedEntry(hash, name);
            Entries.Add(vm);
            FilteredEntries.Add(vm);
        }

        OnPropertyChanged(nameof(EntryCount));
    }

    /// <summary>
    /// Creates an entry view model whose name edits are written through to the wrapper.
    /// </summary>
    private NameMapEntryViewModel CreateTrackedEntry(ulong hash, string name)
    {
        var vm = new NameMapEntryViewModel(hash, name);
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            if (_resource != null && e.PropertyName == nameof(NameMapEntryViewModel.Name) && sender is NameMapEntryViewModel changedEntry)
            {
                _resource[changedEntry.Hash] = changedEntry.Name;
            }
        };
        vm.PropertyChanged += handler;
        _subscriptions.Add((vm, handler));
        return vm;
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
        var vm = CreateTrackedEntry(hash, NewNameText);
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
        UntrackEntry(SelectedEntry);
        Entries.Remove(SelectedEntry);
        FilteredEntries.Remove(SelectedEntry);
        SelectedEntry = null;
        OnPropertyChanged(nameof(EntryCount));
    }

    private void UntrackEntry(NameMapEntryViewModel vm)
    {
        var index = _subscriptions.FindIndex(s => ReferenceEquals(s.ViewModel, vm));
        if (index < 0) return;
        vm.PropertyChanged -= _subscriptions[index].Handler;
        _subscriptions.RemoveAt(index);
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetData() => _resource?.Data ?? ReadOnlyMemory<byte>.Empty;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ClearSubscriptions();
        PropertyChanged -= _selfPropertyChangedHandler;
        if (_resource != null)
        {
            _resource.Changed -= _resourceChangedHandler;
        }
        GC.SuppressFinalize(this);
    }
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
