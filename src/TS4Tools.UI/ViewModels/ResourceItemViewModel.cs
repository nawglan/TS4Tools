using TS4Tools;
using TS4Tools.Extensions;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// One row of the resource browser. Each row wraps one package index entry, so resources that
/// share a key (duplicates) are still distinct rows.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs lines 710-730 (CreateItem:
/// each ListViewItem's Tag is its IResourceIndexEntry; deleted entries stay listed, struck out).
/// </remarks>
public sealed partial class ResourceItemViewModel : ViewModelBase
{
    private static readonly Dictionary<uint, string> KnownTypes = new()
    {
        { 0x220557DA, "String Table (STBL)" },
        { 0x0166038C, "Name Map" },
        { 0x545AC67A, "SimData (DATA)" },
        { 0x01D10F34, "Model LOD (MLOD)" },
        { 0x015A1849, "Geometry (GEOM)" },
        { 0x00B2D882, "Image (DDS/DST)" },
        { 0x034AEECB, "CAS Part" },
        { 0x319E4F1D, "Catalog Object (COBJ)" },
        { 0x0418FE2A, "Fence (CFEN)" },
        { 0x03B33DDF, "Tuning (XML)" },
        { 0x6017E896, "Tuning Instance (XML)" },
        { 0x73E93EEB, "Tuning Instance" }
    };

    private static readonly string[] EntryDependentProperties =
    [
        nameof(Key), nameof(Tag), nameof(TypeName), nameof(InstanceDisplay), nameof(DisplayKey),
        nameof(IsCompressed), nameof(IsDeleted), nameof(TypeText), nameof(GroupText), nameof(InstanceText)
    ];

    private IResourceIndexEntry? _entry;
    private ResourceKey _key;
    private uint _fileSize;
    private string? _instanceName;

    /// <summary>
    /// Creates a row for a package index entry.
    /// </summary>
    public ResourceItemViewModel(IResourceIndexEntry entry, string? instanceName = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;
        _key = entry.Key;
        _fileSize = entry.FileSize;
        _instanceName = instanceName;
        ApplyEntryData(entry);
    }

    /// <summary>
    /// Creates a row that is not backed by an index entry (e.g. a listing of another package).
    /// </summary>
    public ResourceItemViewModel(ResourceKey key, uint fileSize, string? instanceName = null)
    {
        _key = key;
        _fileSize = fileSize;
        _instanceName = instanceName;
    }

    /// <summary>
    /// The package index entry this row represents, or null for rows not backed by an entry.
    /// </summary>
    public IResourceIndexEntry? Entry => _entry;

    /// <summary>
    /// The resource key (read live from the entry, so in-place key edits show up).
    /// </summary>
    public ResourceKey Key => _entry?.Key ?? _key;

    /// <summary>
    /// Size of the resource. Updated after a commit or save so the list reflects the new data.
    /// </summary>
    public uint FileSize
    {
        get => _fileSize;
        set => SetProperty(ref _fileSize, value);
    }

    /// <summary>
    /// Whether the entry is flagged compressed (legacy <c>Compressed != 0</c>, MainForm.cs line 1347).
    /// </summary>
    public bool IsCompressed => CompressionType != 0;

    /// <summary>
    /// Whether the entry is marked deleted. Deleted entries stay listed until the package is saved.
    /// </summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pe/BrowserWidget/BrowserWidget.cs line 713 (strikeout)</remarks>
    public bool IsDeleted => _entry?.IsDeleted ?? false;

    /// <summary>
    /// Human-readable name for the instance, if available from NameMap.
    /// </summary>
    public string? InstanceName
    {
        get => _instanceName;
        set
        {
            if (SetProperty(ref _instanceName, value))
            {
                OnPropertyChanged(nameof(HasInstanceName));
                OnPropertyChanged(nameof(NameText));
                OnPropertyChanged(nameof(InstanceDisplay));
                OnPropertyChanged(nameof(DisplayKey));
            }
        }
    }

    /// <summary>
    /// Whether this resource has a known instance name.
    /// </summary>
    public bool HasInstanceName => InstanceName != null;

    /// <summary>
    /// Short legacy tag for the resource type (e.g. "STBL"), or "UNKN".
    /// </summary>
    // Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/ExtList.cs (s4pe Tag column)
    public string Tag => ExtList.GetTag(Key.ResourceType);

    /// <summary>
    /// Human-readable type name: a friendly name when available, otherwise the legacy
    /// Extensions.txt tag, otherwise "Unknown".
    /// </summary>
    public string TypeName => KnownTypes.TryGetValue(Key.ResourceType, out var name)
        ? name
        : ExtList.IsKnown(Key.ResourceType)
            ? $"{ExtList.GetTag(Key.ResourceType)} (0x{Key.ResourceType:X8})"
            : $"Unknown (0x{Key.ResourceType:X8})";

    /// <summary>
    /// Display string for the instance ID, showing name if available.
    /// </summary>
    public string InstanceDisplay => HasInstanceName
        ? $"{InstanceName}"
        : $"0x{Key.Instance:X16}";

    /// <summary>
    /// Full display key with type, group, and instance.
    /// </summary>
    public string DisplayKey => HasInstanceName
        ? $"G: 0x{Key.ResourceGroup:X8} | {InstanceName} (0x{Key.Instance:X16})"
        : $"G: 0x{Key.ResourceGroup:X8} | I: 0x{Key.Instance:X16}";

    /// <summary>
    /// Re-reads the entry's state (key, sizes, compression, deleted flag) and raises change
    /// notifications for everything derived from it.
    /// </summary>
    public void Refresh()
    {
        if (_entry != null)
        {
            _key = _entry.Key;
            ApplyEntryData(_entry);
        }

        foreach (var property in EntryDependentProperties)
        {
            OnPropertyChanged(property);
        }
    }

    /// <summary>
    /// Points this row at a new index entry for the same resource (the package re-reads its
    /// index after a save, replacing every entry object).
    /// </summary>
    internal void Rebind(IResourceIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;
        Refresh();
    }
}
