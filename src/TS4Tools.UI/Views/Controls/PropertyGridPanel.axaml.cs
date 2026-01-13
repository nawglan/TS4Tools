using Avalonia.Controls;
using TS4Tools.Wrappers.Attributes;

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Property grid panel for editing resource fields.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs
/// This is a simplified Avalonia port that uses reflection to display
/// and edit properties, rather than the full ICustomTypeDescriptor system.
/// </remarks>
public partial class PropertyGridPanel : UserControl
{
    private object? _target;

    /// <summary>
    /// Gets or sets the object being edited.
    /// </summary>
    public object? Target
    {
        get => _target;
        set
        {
            _target = value;
            RefreshProperties();
        }
    }

    /// <summary>
    /// Collection of property items displayed in the grid.
    /// </summary>
    public ObservableCollection<PropertyItem> Properties { get; } = [];

    public PropertyGridPanel()
    {
        InitializeComponent();
        this.FindControl<DataGrid>("PropertiesGrid")!.ItemsSource = Properties;
    }

    private void RefreshProperties()
    {
        Properties.Clear();

        var typeLabel = this.FindControl<TextBlock>("TypeLabel")!;
        if (_target == null)
        {
            typeLabel.Text = "";
            return;
        }

        typeLabel.Text = $"({_target.GetType().Name})";

        // Get all public instance properties
        var type = _target.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !IsFilteredProperty(p))
            .OrderBy(p => GetPropertyPriority(p))
            .ThenBy(p => p.Name);

        foreach (var prop in properties)
        {
            try
            {
                var item = new PropertyItem(_target, prop);
                Properties.Add(item);
            }
            catch
            {
                // Skip properties that throw on access
            }
        }
    }

    /// <summary>
    /// Determines whether a property should be filtered from display.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 164
    /// Filters out Stream, Value, and other internal properties.
    /// </remarks>
    private static bool IsFilteredProperty(PropertyInfo prop)
    {
        // Filter out properties that don't make sense to display
        var filteredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Stream",
            "AsBytes",
            "ContentFields",
            "RecommendedApiVersion"
        };

        return filteredNames.Contains(prop.Name) ||
               typeof(Stream).IsAssignableFrom(prop.PropertyType);
    }

    /// <summary>
    /// Gets the display priority for a property.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 247
    /// Uses ElementPriorityAttribute if present, otherwise returns MaxValue.
    /// </remarks>
    private static int GetPropertyPriority(PropertyInfo prop)
    {
        // Check for ElementPriority attribute (common in s4pi resources)
        var priorityAttr = prop.GetCustomAttribute<ElementPriorityAttribute>();
        if (priorityAttr != null)
        {
            return priorityAttr.Priority;
        }

        // Put key properties first
        if (prop.Name is "Version" or "ResourceType" or "ResourceGroup" or "Instance")
        {
            return -1;
        }

        return int.MaxValue;
    }
}

/// <summary>
/// Represents a single property in the grid.
/// </summary>
public class PropertyItem : INotifyPropertyChanged
{
    private readonly object _target;
    private readonly PropertyInfo _property;
    private readonly TgiBlockIndexAttribute? _tgiBlockIndexAttr;
    private System.Collections.IList? _tgiBlockList;

    public PropertyItem(object target, PropertyInfo property)
    {
        _target = target;
        _property = property;

        // Check for TGI block index attribute
        _tgiBlockIndexAttr = property.GetCustomAttribute<TgiBlockIndexAttribute>();
        if (_tgiBlockIndexAttr != null)
        {
            // Find the TGI block list property
            var listProp = target.GetType().GetProperty(_tgiBlockIndexAttr.ListPropertyName);
            if (listProp != null)
            {
                _tgiBlockList = listProp.GetValue(target) as System.Collections.IList;
            }
        }
    }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name => _property.Name;

    /// <summary>
    /// Gets a simplified type name for display.
    /// </summary>
    public string TypeName => GetSimpleTypeName(_property.PropertyType);

    /// <summary>
    /// Gets or sets the property value as a string.
    /// </summary>
    public string Value
    {
        get => GetValueString();
        set => SetValueFromString(value);
    }

    /// <summary>
    /// Gets whether the property is read-only.
    /// </summary>
    public bool IsReadOnly => !_property.CanWrite;

    /// <summary>
    /// Gets whether this property has a TGI block selector.
    /// </summary>
    public bool HasTgiBlockSelector => _tgiBlockIndexAttr != null && _tgiBlockList != null;

    /// <summary>
    /// Gets the TGI block list for selector UI.
    /// </summary>
    public System.Collections.IList? TgiBlockList => _tgiBlockList;

    /// <summary>
    /// Gets the selected TGI block index.
    /// </summary>
    public int TgiBlockIndex
    {
        get
        {
            if (!HasTgiBlockSelector) return -1;
            var value = _property.GetValue(_target);
            return value != null ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : -1;
        }
        set
        {
            if (!_property.CanWrite) return;
            try
            {
                object? convertedValue = Convert.ChangeType(value, _property.PropertyType, CultureInfo.InvariantCulture);
                _property.SetValue(_target, convertedValue);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TgiBlockIndex)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
            catch
            {
                // Ignore conversion errors
            }
        }
    }

    /// <summary>
    /// Gets whether this property contains binary data.
    /// </summary>
    public bool IsBinaryData =>
        _property.PropertyType == typeof(byte[]) ||
        _property.PropertyType == typeof(ReadOnlyMemory<byte>) ||
        _property.PropertyType == typeof(Memory<byte>);

    /// <summary>
    /// Gets the binary data for this property (if applicable).
    /// </summary>
    public byte[]? BinaryData
    {
        get
        {
            if (!IsBinaryData) return null;
            var value = _property.GetValue(_target);
            return value switch
            {
                byte[] arr => arr,
                ReadOnlyMemory<byte> rom => rom.ToArray(),
                Memory<byte> mem => mem.ToArray(),
                _ => null
            };
        }
        set
        {
            if (!_property.CanWrite || value == null) return;
            try
            {
                object? convertedValue = _property.PropertyType == typeof(byte[])
                    ? value
                    : _property.PropertyType == typeof(ReadOnlyMemory<byte>)
                        ? new ReadOnlyMemory<byte>(value)
                        : new Memory<byte>(value);
                _property.SetValue(_target, convertedValue);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BinaryData)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    /// <summary>
    /// Gets the target object for this property item.
    /// </summary>
    public object Target => _target;

    /// <summary>
    /// Gets the property info.
    /// </summary>
    public PropertyInfo PropertyInfo => _property;

    /// <summary>
    /// Gets the category for the property.
    /// </summary>
    /// <remarks>
    /// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 181-193
    /// </remarks>
    public string Category
    {
        get
        {
            var type = _property.PropertyType;

            if (type.IsEnum) return "Values";
            if (IsNumericType(type)) return "Values";
            if (type == typeof(string)) return "Values";
            if (type == typeof(bool)) return "Values";
            if (typeof(IEnumerable<object>).IsAssignableFrom(type)) return "Lists";
            if (type.IsClass && type != typeof(string)) return "Fields";

            return "Values";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private string GetValueString()
    {
        try
        {
            var value = _property.GetValue(_target);

            if (value == null) return "(null)";

            // Format based on type
            var type = _property.PropertyType;

            // Numeric types displayed as hex (common in Sims resources)
            // Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs AsHexCTD
            if (type == typeof(uint)) return $"0x{value:X8}";
            if (type == typeof(ulong)) return $"0x{value:X16}";
            if (type == typeof(ushort)) return $"0x{value:X4}";
            if (type == typeof(byte)) return $"0x{value:X2}";
            if (type == typeof(int)) return $"0x{value:X8}";
            if (type == typeof(long)) return $"0x{value:X16}";
            if (type == typeof(short)) return $"0x{value:X4}";
            if (type == typeof(sbyte)) return $"0x{value:X2}";

            // Enums with hex
            if (type.IsEnum)
            {
                return $"{value} (0x{Convert.ToUInt64(value, CultureInfo.InvariantCulture):X})";
            }

            // Collections show count
            if (value is System.Collections.ICollection collection)
            {
                return $"(Collection: {collection.Count} items)";
            }

            // Arrays show length
            if (value is Array array)
            {
                return $"(Array: {array.Length} items)";
            }

            // Floats
            if (type == typeof(float) || type == typeof(double))
            {
                return ((IFormattable)value).ToString("R", CultureInfo.InvariantCulture);
            }

            return value.ToString() ?? "(null)";
        }
        catch (Exception ex)
        {
            return $"(error: {ex.Message})";
        }
    }

    private void SetValueFromString(string value)
    {
        if (!_property.CanWrite) return;

        try
        {
            var type = _property.PropertyType;
            object? convertedValue = null;

            // Parse hex values for numeric types
            // Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs AsHexConverter
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var hexStr = value.Substring(2);
                var numValue = Convert.ToUInt64(hexStr, 16);

                convertedValue = type.Name switch
                {
                    nameof(UInt32) => (uint)numValue,
                    nameof(UInt64) => numValue,
                    nameof(UInt16) => (ushort)numValue,
                    nameof(Byte) => (byte)numValue,
                    nameof(Int32) => (int)numValue,
                    nameof(Int64) => (long)numValue,
                    nameof(Int16) => (short)numValue,
                    nameof(SByte) => (sbyte)numValue,
                    _ when type.IsEnum => Enum.ToObject(type, numValue),
                    _ => null
                };
            }
            // Parse enums by name
            else if (type.IsEnum)
            {
                // Handle "EnumValue (0xHEX)" format
                var enumPart = value.Split(' ')[0];
                if (Enum.TryParse(type, enumPart, out var result))
                {
                    convertedValue = result;
                }
            }
            // Standard conversion for other types
            else
            {
                convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }

            if (convertedValue != null)
            {
                _property.SetValue(_target, convertedValue);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
        catch
        {
            // Ignore conversion errors - the grid will show the original value
        }
    }

    private static string GetSimpleTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition().Name;
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetSimpleTypeName));
            var baseName = genericDef.Substring(0, genericDef.IndexOf('`'));
            return $"{baseName}<{genericArgs}>";
        }

        return type.Name switch
        {
            "UInt32" => "uint",
            "UInt64" => "ulong",
            "UInt16" => "ushort",
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Byte" => "byte",
            "SByte" => "sbyte",
            "Single" => "float",
            "Double" => "double",
            "Boolean" => "bool",
            "String" => "string",
            _ => type.Name
        };
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }
}

/// <summary>
/// Attribute to specify display priority for a property.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pi/Interfaces/AApiVersionedFields.cs ElementPriorityAttribute
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class ElementPriorityAttribute(int priority) : Attribute
{
    public int Priority { get; } = priority;
}
