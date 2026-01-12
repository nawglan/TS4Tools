using Avalonia.Controls;
using Avalonia.Interactivity;
using TS4Tools;

namespace TS4Tools.UI.Views.Dialogs;

/// <summary>
/// Dialog for viewing and editing resource details (TGI, compression).
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/Import/ResourceDetails.cs
/// </remarks>
public partial class ResourceDetailsWindow : Window
{
    // Using same known types as ResourceItemViewModel
    // Source: legacy_references/Sims4Tools/s4pi Extras/Extensions/Extensions.cs (ExtList class)
    private static readonly Dictionary<uint, string> KnownTypes = new()
    {
        { 0x220557DA, "String Table (STBL)" },
        { 0x0166038C, "Name Map" },
        { 0x545AC67A, "SimData (DATA)" },
        { 0x01D10F34, "Rig (BSRF)" },
        { 0x015A1849, "Geometry (GEOM)" },
        { 0x00B2D882, "Image (DDS/DST)" },
        { 0x00B00000, "Image (PNG)" },
        { 0x034AEECB, "CAS Part" },
        { 0x319E4F1D, "Catalog Object (COBJ)" },
        { 0x0418FE2A, "Fence (CFEN)" },
        { 0x03B33DDF, "Tuning (XML)" },
        { 0x6017E896, "Tuning Instance (XML)" },
        { 0x73E93EEB, "Tuning Instance" }
    };

    private ResourceKey _originalKey;
    private bool _originalCompressed;
    private bool _internalChange;

    /// <summary>
    /// Gets the modified resource key after editing.
    /// </summary>
    public ResourceKey ResourceKey { get; private set; }

    /// <summary>
    /// Gets or sets the resource name.
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// Gets whether the resource should be compressed.
    /// </summary>
    public bool Compress { get; private set; }

    /// <summary>
    /// Gets whether the key was modified.
    /// </summary>
    public bool WasModified { get; private set; }

    public ResourceDetailsWindow()
    {
        InitializeComponent();

        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
        CopyKeyButton.Click += CopyKeyButton_Click;
        PasteKeyButton.Click += PasteKeyButton_Click;
        FNV64Button.Click += FNV64Button_Click;
        FNV32Button.Click += FNV32Button_Click;
        HighBitButton.Click += HighBitButton_Click;

        TypeTextBox.TextChanged += TextBox_TextChanged;
        GroupTextBox.TextChanged += TextBox_TextChanged;
        InstanceTextBox.TextChanged += TextBox_TextChanged;
    }

    /// <summary>
    /// Initializes the dialog with resource information.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/MainForm.cs lines 1584-1617 (ResourceDetails method)
    /// </remarks>
    public void Initialize(ResourceKey key, string? name, bool isCompressed, uint fileSize, uint memSize)
    {
        _internalChange = true;
        try
        {
            _originalKey = key;
            _originalCompressed = isCompressed;
            ResourceKey = key;
            ResourceName = name;
            Compress = isCompressed;

            // Type
            TypeTextBox.Text = key.ResourceType.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            UpdateTypeName(key.ResourceType);

            // Group
            GroupTextBox.Text = key.ResourceGroup.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);

            // Instance
            InstanceTextBox.Text = key.Instance.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);

            // Name
            NameTextBox.Text = name ?? "";

            // Compression
            CompressedCheckBox.IsChecked = isCompressed;

            // Sizes
            DataSizeLabel.Text = $"{memSize:N0} bytes";
            FileSizeLabel.Text = $"{fileSize:N0} bytes";
        }
        finally
        {
            _internalChange = false;
        }
    }

    private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_internalChange) return;

        // Update type name when type changes
        if (sender == TypeTextBox)
        {
            if (uint.TryParse(TypeTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out var type))
            {
                UpdateTypeName(type);
            }
            else
            {
                TypeNameLabel.Text = "";
            }
        }
    }

    private void UpdateTypeName(uint type)
    {
        TypeNameLabel.Text = KnownTypes.TryGetValue(type, out var name) ? name : "";
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        // Parse values
        if (!uint.TryParse(TypeTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out var type))
        {
            StatusChanged("Invalid Type value");
            return;
        }

        if (!uint.TryParse(GroupTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out var group))
        {
            StatusChanged("Invalid Group value");
            return;
        }

        if (!ulong.TryParse(InstanceTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out var instance))
        {
            StatusChanged("Invalid Instance value");
            return;
        }

        ResourceKey = new ResourceKey(type, group, instance);
        ResourceName = string.IsNullOrWhiteSpace(NameTextBox.Text) ? null : NameTextBox.Text;
        Compress = CompressedCheckBox.IsChecked == true;
        WasModified = ResourceKey != _originalKey || Compress != _originalCompressed;

        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    // Source: legacy_references/Sims4Tools/s4pe/Import/ResourceDetails.cs lines 216-219
    private async void CopyKeyButton_Click(object? sender, RoutedEventArgs e)
    {
        var key = $"S4_{TypeTextBox.Text}_{GroupTextBox.Text}_{InstanceTextBox.Text}";
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(key);
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/Import/ResourceDetails.cs lines 223-231
    private async void PasteKeyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            var text = await clipboard.GetTextAsync();
            if (TryParseResourceKey(text, out var type, out var group, out var instance))
            {
                _internalChange = true;
                try
                {
                    TypeTextBox.Text = type.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
                    GroupTextBox.Text = group.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
                    InstanceTextBox.Text = instance.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
                    UpdateTypeName(type);
                }
                finally
                {
                    _internalChange = false;
                }
            }
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/Import/ResourceDetails.cs lines 159-162
    private void FNV64Button_Click(object? sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text;
        if (!string.IsNullOrEmpty(name))
        {
            var hash = ComputeFnv64(name);
            InstanceTextBox.Text = hash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/Import/ResourceDetails.cs lines 169-172
    private void FNV32Button_Click(object? sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text;
        if (!string.IsNullOrEmpty(name))
        {
            var hash = ComputeFnv32(name);
            InstanceTextBox.Text = hash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    // Source: legacy_references/Sims4Tools/s4pe/Import/ResourceDetails.cs lines 238-277
    private void HighBitButton_Click(object? sender, RoutedEventArgs e)
    {
        // Set high bit on group
        if (uint.TryParse(GroupTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out var group))
        {
            group |= 0x80000000;
            GroupTextBox.Text = group.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
        }

        // Set high bit on instance
        if (ulong.TryParse(InstanceTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out var instance))
        {
            instance |= 0x8000000000000000;
            InstanceTextBox.Text = instance.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private static bool TryParseResourceKey(string? text, out uint type, out uint group, out ulong instance)
    {
        type = 0;
        group = 0;
        instance = 0;

        if (string.IsNullOrWhiteSpace(text)) return false;

        // Try S4_Type_Group_Instance format
        if (text.StartsWith("S4_", StringComparison.OrdinalIgnoreCase))
        {
            var parts = text[3..].Split('_');
            if (parts.Length >= 3)
            {
                return uint.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out type) &&
                       uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out group) &&
                       ulong.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out instance);
            }
        }

        // Try 0xType-0xGroup-0xInstance format
        var dashParts = text.Split('-');
        if (dashParts.Length == 3)
        {
            static bool TryParseHex(string s, out uint val)
            {
                s = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s;
                return uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out val);
            }

            static bool TryParseHex64(string s, out ulong val)
            {
                s = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s;
                return ulong.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out val);
            }

            return TryParseHex(dashParts[0], out type) &&
                   TryParseHex(dashParts[1], out group) &&
                   TryParseHex64(dashParts[2], out instance);
        }

        return false;
    }

    // FNV-1a 64-bit hash
    private static ulong ComputeFnv64(string text)
    {
        const ulong fnvPrime = 0x00000100000001B3;
        const ulong fnvOffset = 0xCBF29CE484222325;

        var hash = fnvOffset;
        foreach (var c in text.ToLowerInvariant())
        {
            hash ^= c;
            hash *= fnvPrime;
        }
        return hash;
    }

    // FNV-1a 32-bit hash
    private static uint ComputeFnv32(string text)
    {
        const uint fnvPrime = 0x01000193;
        const uint fnvOffset = 0x811C9DC5;

        var hash = fnvOffset;
        foreach (var c in text.ToLowerInvariant())
        {
            hash ^= c;
            hash *= fnvPrime;
        }
        return hash;
    }

    private void StatusChanged(string message)
    {
        // Simple error handling - could use a status label
        Title = $"Resource Details - {message}";
    }
}
