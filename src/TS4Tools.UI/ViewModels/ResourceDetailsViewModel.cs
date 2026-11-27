using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.Extensions;
using TS4Tools.Resources;
using TS4Tools.UI.Services;
using TS4Tools.Wrappers.Hashing;

namespace TS4Tools.UI.ViewModels;

/// <summary>
/// State and logic of the Resource Details dialog, used to add a resource, import a file or
/// pasted resource, and edit an existing resource's key, name and compression.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/ResourceDetails.cs lines 33-279 and
/// ResourceDetails.designer.cs lines 59-384.
/// </remarks>
public sealed partial class ResourceDetailsViewModel : ViewModelBase
{
    private static readonly Lazy<IReadOnlyList<string>> KnownTypeList = new(() => BuildKnownTypes());

    private bool _internalChange;

    /// <summary>Type combo text: "TAG 0xTTTTTTTT" for a listed type, or a hex/decimal number.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid), nameof(Key))]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    private string _typeText = string.Empty;

    /// <summary>Group text ("0x" prefix for hex, otherwise decimal).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid), nameof(Key))]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    private string _groupText = string.Empty;

    /// <summary>Instance text ("0x" prefix for hex, otherwise decimal).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid), nameof(Key))]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    private string _instanceText = string.Empty;

    /// <summary>Resource name (written to the package name map when Use resource name is checked).</summary>
    [ObservableProperty]
    private string _resourceName = string.Empty;

    /// <summary>Source file name, when importing a file.</summary>
    [ObservableProperty]
    private string _filename = string.Empty;

    /// <summary>
    /// Creates the dialog state.
    /// </summary>
    /// <param name="useName">Initial "Use resource name" state (ResourceDetails.cs lines 38-44).</param>
    /// <param name="displayFilename">Whether the Filename field is shown.</param>
    /// <param name="key">Existing resource key to show, if any (ResourceDetails.cs lines 45-56).</param>
    public ResourceDetailsViewModel(bool useName, bool displayFilename, ResourceKey? key = null)
    {
        Settings.UseName = useName;
        ShowFilename = displayFilename;
        Settings.PropertyChanged += OnSettingsPropertyChanged;

        if (key is { } k)
        {
            SetKey(k);
        }
    }

    /// <summary>Raised when the dialog should close; the argument is true for OK.</summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>Import settings (replace/compress/use name/rename).</summary>
    public ImportSettingsViewModel Settings { get; } = new();

    /// <summary>Whether the Filename field is shown.</summary>
    public bool ShowFilename { get; }

    /// <summary>Whether the name row is enabled (ResourceDetails.cs lines 146-149).</summary>
    public bool NameEnabled => Settings.UseName;

    /// <summary>Type drop-down entries.</summary>
    public IReadOnlyList<string> KnownTypes { get; } = KnownTypeList.Value;

    /// <summary>Whether OK can be pressed (ResourceDetails.cs line 105, btnOKCanEnable).</summary>
    public bool IsValid => TryGetKey(out _);

    /// <summary>The key entered, or null if a field does not parse.</summary>
    public ResourceKey? Key => TryGetKey(out var key) ? key : null;

    /// <summary>The key and name as a TGIN (ResourceDetails.cs line 81, implicit operator TGIN).</summary>
    public Tgin Tgin
    {
        get
        {
            var key = Key ?? default;
            return Tgin.From(key, string.IsNullOrEmpty(ResourceName) ? null : ResourceName);
        }
    }

    /// <summary>The key in legacy text form, "0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII".</summary>
    /// <remarks>Source: ResourceDetails.cs lines 214-217 (Copy RK uses AResourceKey's string form)</remarks>
    public string KeyText => FormatKey(Key ?? default);

    /// <summary>Formats a key the way legacy AResourceKey does.</summary>
    /// <remarks>Source: legacy_references/Sims4Tools/s4pi/Interfaces/AResourceKey.cs line 231 (ToString)</remarks>
    public static string FormatKey(ResourceKey key) =>
        $"0x{key.ResourceType:X8}-0x{key.ResourceGroup:X8}-0x{key.Instance:X16}";

    /// <summary>Formats a type the way the type combo does ("TAG 0xTTTTTTTT" when listed).</summary>
    /// <remarks>Source: ResourceTypeCombo.cs lines 74-82 (Value setter)</remarks>
    public static string FormatType(uint resourceType)
    {
        var type = $"0x{resourceType:X8}";
        return ExtList.IsKnown(resourceType) ? $"{ExtList.GetTag(resourceType)} {type}" : type;
    }

    /// <summary>Shows a key in the fields.</summary>
    public void SetKey(ResourceKey key)
    {
        TypeText = FormatType(key.ResourceType);
        GroupText = $"0x{key.ResourceGroup:X8}";
        InstanceText = $"0x{key.Instance:X16}";
    }

    /// <summary>Fills the key fields from pasted text, if it parses as a resource key.</summary>
    /// <remarks>Source: ResourceDetails.cs lines 225-233 (pasteResourceKeyToolStripMenuItem_Click)</remarks>
    public bool TryPasteKey(string? text)
    {
        if (!ResourceKey.TryParse(text?.Trim(), out var key)) return false;
        SetKey(key);
        return true;
    }

    partial void OnFilenameChanged(string value)
    {
        if (_internalChange) return;

        // Source: ResourceDetails.cs lines 84-98 (FillPanel parses the TGIN out of the file name)
        var tgin = Tgin.Parse(value);
        _internalChange = true;
        try
        {
            SetKey(tgin.Key);
            ResourceName = tgin.ResourceName ?? string.Empty;
        }
        finally
        {
            _internalChange = false;
        }
    }

    /// <summary>Sets the instance to the FNV64 hash of the name.</summary>
    /// <remarks>Source: ResourceDetails.cs lines 157-160</remarks>
    [RelayCommand]
    private void Fnv64() => InstanceText = $"0x{FNV64.GetHash(ResourceName ?? string.Empty):X16}";

    /// <summary>Sets the instance to the CLIP IID hash of the name.</summary>
    /// <remarks>Source: ResourceDetails.cs lines 162-165; ResourceDetails.designer.cs line 296 ("CLIP &amp;IID")</remarks>
    [RelayCommand]
    private void ClipIid() => InstanceText = $"0x{FNV64CLIP.GetHash(ResourceName ?? string.Empty):X16}";

    /// <summary>Sets the instance to the FNV32 hash of the name.</summary>
    /// <remarks>Source: ResourceDetails.cs lines 167-170 (formatted as 16 hex digits)</remarks>
    [RelayCommand]
    private void Fnv32() => InstanceText = $"0x{(ulong)FNV32.GetHash(ResourceName ?? string.Empty):X16}";

    /// <summary>Sets the top bit of the group and instance.</summary>
    /// <remarks>Source: ResourceDetails.cs lines 240-278 (btnHighBit_Click)</remarks>
    [RelayCommand]
    private void HighBit()
    {
        const uint uintMask = 1u << 31;
        const ulong ulongMask = 1ul << 63;

        if (TryParseNumber(GroupText, out var group) && group <= uint.MaxValue)
        {
            GroupText = $"0x{(uint)group | uintMask:X8}";
        }

        if (TryParseNumber(InstanceText, out var instance))
        {
            InstanceText = $"0x{instance | ulongMask:X16}";
        }
    }

    /// <summary>Closes the dialog with OK.</summary>
    [RelayCommand(CanExecute = nameof(IsValid))]
    private void Ok() => CloseRequested?.Invoke(this, true);

    /// <summary>Closes the dialog with Cancel.</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    /// <summary>Releases the settings subscription.</summary>
    public void Detach() => Settings.PropertyChanged -= OnSettingsPropertyChanged;

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImportSettingsViewModel.UseName))
        {
            OnPropertyChanged(nameof(NameEnabled));
        }
    }

    private bool TryGetKey(out ResourceKey key)
    {
        key = default;
        if (!TryParseType(TypeText, out var type)) return false;
        if (!TryParseNumber(GroupText, out var group) || group > uint.MaxValue) return false;
        if (!TryParseNumber(InstanceText, out var instance)) return false;
        key = new ResourceKey(type, (uint)group, instance);
        return true;
    }

    /// <summary>
    /// Parses the type combo text: a listed "TAG 0xTTTTTTTT" entry, else "0x" hex or decimal.
    /// </summary>
    /// <remarks>Source: ResourceTypeCombo.cs lines 57-72 (Value getter) and 88-106 (validity)</remarks>
    internal static bool TryParseType(string? text, out uint type)
    {
        type = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = text.Trim();
        var space = s.LastIndexOf(' ');
        if (space >= 0)
        {
            s = s[(space + 1)..];
        }

        if (!TryParseNumber(s, out var value) || value > uint.MaxValue) return false;
        type = (uint)value;
        return true;
    }

    /// <summary>
    /// Parses "0x" hex or decimal, as legacy Convert.ToUInt64(text, text.StartsWith("0x") ? 16 : 10).
    /// </summary>
    /// <remarks>Source: ResourceDetails.cs lines 62-70</remarks>
    internal static bool TryParseNumber(string? text, out ulong value)
    {
        value = 0;
        if (string.IsNullOrEmpty(text)) return false;
        var s = text.Trim();
        return s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? s.Length > 2 && ulong.TryParse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
            : ulong.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    // Source: ResourceTypeCombo.cs lines 33-43 (ExtList "TAG 0xTTTTTTTT" entries); types that only
    // have a registered wrapper (WrapperDealer TypeMap) are appended so they can be chosen too.
    private static List<string> BuildKnownTypes()
    {
        var list = new List<string>();
        var seen = new HashSet<uint>();
        foreach (var (type, tag) in ExtList.KnownTypes)
        {
            if (seen.Add(type))
            {
                list.Add($"{tag} 0x{type:X8}");
            }
        }

        try
        {
            foreach (var (type, _) in ResourceHandlerRegistry.Shared.TypeMap.OrderBy(kvp => kvp.Key))
            {
                if (seen.Add(type))
                {
                    list.Add($"0x{type:X8}");
                }
            }
        }
        catch (InvalidOperationException)
        {
            // No wrappers available: the ExtList entries are enough.
        }

        return list;
    }
}
