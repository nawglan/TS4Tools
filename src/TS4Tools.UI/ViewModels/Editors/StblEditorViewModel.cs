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
public partial class StblEditorViewModel : ViewModelBase, IResourceEditorViewModel, IDisposable
{
    private bool _disposed;
    private StblResource? _resource;
    private readonly PropertyChangedEventHandler _selfPropertyChangedHandler;
    private readonly EventHandler _resourceChangedHandler;

    /// <inheritdoc />
    public event EventHandler? ContentChanged;
    private readonly List<(StblEntryViewModel ViewModel, PropertyChangedEventHandler Handler)> _subscriptions = [];

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private StblEntryViewModel? _selectedEntry;

    [ObservableProperty]
    private string _newKeyText = string.Empty;

    [ObservableProperty]
    private string _newValueText = string.Empty;

    /// <summary>Why the last Add was refused (shown under the add row), or null.</summary>
    [ObservableProperty]
    private string? _inputError;

    public ObservableCollection<StblEntryViewModel> Entries { get; } = [];
    public ObservableCollection<StblEntryViewModel> FilteredEntries { get; } = [];

    public int EntryCount => _resource?.Count ?? 0;

    public StblEditorViewModel()
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

    public void LoadResource(StblResource resource)
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

        foreach (var entry in _resource.Entries)
        {
            var vm = CreateTrackedEntry(entry.KeyHash, entry.Value);
            Entries.Add(vm);
            FilteredEntries.Add(vm);
        }

        OnPropertyChanged(nameof(EntryCount));
    }

    /// <summary>
    /// Creates an entry view model whose value edits are written through to the wrapper.
    /// </summary>
    private StblEntryViewModel CreateTrackedEntry(uint keyHash, string value)
    {
        var vm = new StblEntryViewModel(keyHash, value);
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            if (_resource != null && e.PropertyName == nameof(StblEntryViewModel.Value) && sender is StblEntryViewModel changedEntry)
            {
                // Rows mirror the wrapper's entry list in order; keys may repeat, so address the entry by position.
                int index = Entries.IndexOf(changedEntry);
                if (index >= 0)
                    _resource.SetValueAt(index, changedEntry.Value);
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
            if (!uint.TryParse(NewKeyText.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out keyHash))
            {
                InputError = $"\"{NewKeyText}\" is not a valid 32-bit hex key.";
                return;
            }
        }
        else
        {
            // Hash the key text
            keyHash = FnvHash.Fnv32Lower(NewKeyText);
        }

        InputError = null;
        _resource.Add(keyHash, NewValueText);
        var vm = CreateTrackedEntry(keyHash, NewValueText);
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

        int index = Entries.IndexOf(SelectedEntry);
        if (index >= 0)
            _resource.RemoveAt(index);
        UntrackEntry(SelectedEntry);
        Entries.Remove(SelectedEntry);
        FilteredEntries.Remove(SelectedEntry);
        SelectedEntry = null;
        OnPropertyChanged(nameof(EntryCount));
    }

    private void UntrackEntry(StblEntryViewModel vm)
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
