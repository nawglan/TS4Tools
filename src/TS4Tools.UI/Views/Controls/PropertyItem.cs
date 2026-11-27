// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 56-370 (AApiVersionedFieldsCTD,
//         TypedValuePropertyDescriptor: per-field editor selection, expandable nested fields)
// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 504-586 (AsHexCTD / AsHexConverter)
// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 588-891 (EnumChooserCTD, EnumFlagsCTD)
// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 893-952 (IResourceKeyCTD)
// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 954-1121 (collections and arrays)
// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/NewGridForm.cs lines 86-255 (list add/insert/copy/delete)

using CommunityToolkit.Mvvm.Input;
using TS4Tools.Wrappers.Attributes;
using TS4Tools.Wrappers.CatalogResource;

namespace TS4Tools.UI.Views.Controls;

/// <summary>How a property's value is edited in the grid.</summary>
public enum PropertyEditorKind
{
    /// <summary>Free text (hex for integers).</summary>
    Text,
    /// <summary>Not editable.</summary>
    ReadOnly,
    /// <summary>Check box.</summary>
    Boolean,
    /// <summary>Drop-down of enum names.</summary>
    Enum,
    /// <summary>TGI block index chooser.</summary>
    TgiBlockIndex,
    /// <summary>Binary data import/export.</summary>
    Binary,
}

/// <summary>
/// One row of the property grid: a property of an object, an element of a list or one bit of a flags enum.
/// Rows for nested objects, lists and flags have <see cref="Children"/>, built when first requested.
/// </summary>
/// <remarks>
/// <para>Legacy s4pe's grid gives every field a type descriptor (S4PIPropertyGrid.cs lines 296-339): enums get a
/// chooser (flags enums expand to one bool per flag), integers are shown and parsed as hex, IResourceKey fields expand
/// to their T/G/I parts, nested AApiVersionedFields expand, and lists/arrays expand to their elements and open a list
/// editor (NewGridForm) with Add/Insert/Copy/Delete. This class provides the same behaviour over plain reflection.</para>
/// <para>Value-type values (structs, record structs) are edited on a boxed copy that is written back to the owner after
/// every change, so editing a field of a struct element of a list updates the list.</para>
/// <para>Every successful change raises <see cref="PropertyChanged"/> for <see cref="Value"/> on this row and on each
/// ancestor, so listening to the top-level rows is enough to learn about edits anywhere in the tree.</para>
/// </remarks>
public class PropertyItem : INotifyPropertyChanged, IDisposable
{
    /// <summary>Maximum number of child rows built for one collection.</summary>
    public const int MaxChildRows = 2000;

    private static readonly HashSet<string> CollectionMetaProperties = new(StringComparer.Ordinal)
    {
        "Count", "Capacity", "IsReadOnly", "IsFixedSize", "IsSynchronized", "SyncRoot", "Keys", "Values",
    };

    private readonly Accessor _accessor;
    private readonly PropertyItem? _parent;
    private readonly int _elementIndex = -1;
    private readonly bool _isCatalogTagContext;
    private readonly TgiBlockIndexAttribute? _tgiBlockIndexAttr;
    private readonly IList? _tgiBlockList;
    private readonly List<(PropertyItem Child, PropertyChangedEventHandler Handler)> _childSubscriptions = [];
    private ObservableCollection<PropertyItem>? _children;
    private object? _childOwner;
    private bool _isExpanded;
    private bool _disposed;

    /// <summary>
    /// Creates a row for property <paramref name="property"/> of <paramref name="target"/>.
    /// </summary>
    public PropertyItem(object target, PropertyInfo property)
        : this(new PropertyAccessor(() => target, property, null), parent: null, elementIndex: -1, target)
    {
    }

    private PropertyItem(Accessor accessor, PropertyItem? parent, int elementIndex, object? owner)
    {
        _accessor = accessor;
        _parent = parent;
        _elementIndex = elementIndex;

        _isCatalogTagContext = IsCatalogTagName(accessor.Name)
            || (parent != null && elementIndex >= 0 && (IsCatalogTagName(parent.Name) || parent.RawValue is CatalogTagList));

        if (accessor is PropertyAccessor pa && owner != null)
        {
            _tgiBlockIndexAttr = pa.Property.GetCustomAttribute<TgiBlockIndexAttribute>();
            if (_tgiBlockIndexAttr != null)
            {
                var listProp = owner.GetType().GetProperty(_tgiBlockIndexAttr.ListPropertyName);
                _tgiBlockList = listProp?.GetValue(owner) as IList;
            }
        }

        AddItemCommand = new RelayCommand(AddItem, () => CanAddItem);
        InsertItemCommand = new RelayCommand(InsertBefore, () => CanInsertBefore);
        DuplicateItemCommand = new RelayCommand(DuplicateItem, () => CanDuplicate);
        RemoveItemCommand = new RelayCommand(RemoveItem, () => CanRemove);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the property name (or <c>[i]</c> for list elements).</summary>
    public string Name => _accessor.Name;

    /// <summary>Gets a simplified type name for display.</summary>
    public string TypeName => GetSimpleTypeName(ValueType);

    /// <summary>Gets or sets the value as a string (hex for integers).</summary>
    public string Value
    {
        get => GetValueString();
        set => SetValueFromString(value);
    }

    /// <summary>Gets whether the value itself cannot be changed.</summary>
    public bool IsReadOnly => !_accessor.CanWrite || (!IsTextEditable && !IsBool && !IsEnum && !IsBinaryData && !HasTgiBlockSelector);

    /// <summary>The editor template to use.</summary>
    public PropertyEditorKind EditorKind
    {
        get
        {
            if (HasTgiBlockSelector) return PropertyEditorKind.TgiBlockIndex;
            if (IsBinaryData) return PropertyEditorKind.Binary;
            if (!_accessor.CanWrite) return PropertyEditorKind.ReadOnly;
            if (IsBool) return PropertyEditorKind.Boolean;
            if (IsEnum && !IsFlags) return PropertyEditorKind.Enum;
            return IsTextEditable ? PropertyEditorKind.Text : PropertyEditorKind.ReadOnly;
        }
    }

    /// <summary>Gets whether this property has a TGI block selector.</summary>
    public bool HasTgiBlockSelector => _tgiBlockIndexAttr != null && _tgiBlockList != null;

    /// <summary>Gets the TGI block list for selector UI.</summary>
    public IList? TgiBlockList => _tgiBlockList;

    /// <summary>Gets or sets the selected TGI block index.</summary>
    public int TgiBlockIndex
    {
        get
        {
            if (!HasTgiBlockSelector) return -1;
            var value = RawValue;
            return value != null ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : -1;
        }
        set
        {
            if (!_accessor.CanWrite) return;
            try
            {
                SetRawValue(Convert.ChangeType(value, ValueType, CultureInfo.InvariantCulture));
                Raise(nameof(TgiBlockIndex));
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException or TargetInvocationException or NotSupportedException)
            {
                // Ignore conversion errors - the grid keeps the original value
            }
        }
    }

    /// <summary>Gets whether this property contains binary data.</summary>
    public bool IsBinaryData =>
        ValueType == typeof(byte[]) || ValueType == typeof(ReadOnlyMemory<byte>) || ValueType == typeof(Memory<byte>);

    /// <summary>Gets or sets the binary data for this property (if applicable).</summary>
    public byte[]? BinaryData
    {
        get
        {
            if (!IsBinaryData) return null;
            return RawValue switch
            {
                byte[] arr => arr,
                ReadOnlyMemory<byte> rom => rom.ToArray(),
                Memory<byte> mem => mem.ToArray(),
                _ => null
            };
        }
        set
        {
            if (!_accessor.CanWrite || value == null) return;
            try
            {
                object convertedValue = ValueType == typeof(byte[])
                    ? value
                    : ValueType == typeof(ReadOnlyMemory<byte>)
                        ? new ReadOnlyMemory<byte>(value)
                        : new Memory<byte>(value);
                SetRawValue(convertedValue);
                Raise(nameof(BinaryData));
            }
            catch (Exception ex) when (ex is ArgumentException or TargetInvocationException or NotSupportedException or InvalidCastException)
            {
                // Ignore errors
            }
        }
    }

    /// <summary>Gets the object that owns this value (the list for elements).</summary>
    public object Target => _accessor.Owner ?? this;

    /// <summary>Gets the reflected property, or null for list elements and flag bits.</summary>
    public PropertyInfo? PropertyInfo => (_accessor as PropertyAccessor)?.Property;

    /// <summary>Gets the category for the property.</summary>
    /// <remarks>Source: S4PIPropertyGrid.cs lines 181-193.</remarks>
    public string Category
    {
        get
        {
            var type = ValueType;
            if (type.IsEnum) return IsFlags ? "Fields" : "Values";
            if (IsNumericType(type) || type == typeof(string) || type == typeof(bool)) return "Values";
            if (PropertyListAdapter.IsListType(type)) return "Lists";
            if (type.IsClass) return "Fields";
            return "Values";
        }
    }

    #region Value kinds

    /// <summary>The declared value type.</summary>
    public Type ValueType => Nullable.GetUnderlyingType(_accessor.ValueType) ?? _accessor.ValueType;

    /// <summary>Whether the value is a bool.</summary>
    public bool IsBool => ValueType == typeof(bool);

    /// <summary>Whether the value is an enum.</summary>
    public bool IsEnum => ValueType.IsEnum;

    /// <summary>Whether the value is a [Flags] enum (edited through one bool child per flag).</summary>
    public bool IsFlags => IsEnum && ValueType.GetCustomAttribute<FlagsAttribute>() != null;

    /// <summary>Whether the value is a collection shown as elements.</summary>
    public bool IsCollection => !IsBinaryData && PropertyListAdapter.IsListType(ValueType)
        || (RawValue is { } v && !IsBinaryData && PropertyListAdapter.IsListType(v.GetType()));

    /// <summary>Whether this row is an element of a list.</summary>
    public bool IsListElement => _elementIndex >= 0 && _parent?.IsCollection == true;

    /// <summary>Whether the value is shown as a catalog tag (named through <see cref="CatalogTagRegistry"/>).</summary>
    public bool IsCatalogTag => _isCatalogTagContext
        && (ValueType == typeof(uint) || ValueType == typeof(ushort) || ValueType == typeof(byte));

    /// <summary>Gets or sets the bool value.</summary>
    public bool BoolValue
    {
        get => RawValue is true;
        set
        {
            if (!IsBool || !_accessor.CanWrite || BoolValue == value) return;
            TrySetRawValue(value);
        }
    }

    /// <summary>Gets the enum names for the chooser.</summary>
    /// <remarks>Source: S4PIPropertyGrid.cs lines 669-727 (EnumChooserEditor lists Enum.GetNames).</remarks>
    public IReadOnlyList<string> EnumNames => IsEnum ? Enum.GetNames(ValueType) : [];

    /// <summary>Gets or sets the enum value by name.</summary>
    public string? EnumName
    {
        get => IsEnum && RawValue is { } v ? Enum.GetName(ValueType, v) ?? v.ToString() : null;
        set
        {
            if (!IsEnum || value == null || !_accessor.CanWrite) return;
            if (Enum.TryParse(ValueType, value, out var parsed) && !Equals(parsed, RawValue))
                TrySetRawValue(parsed);
        }
    }

    private bool IsTextEditable
    {
        get
        {
            var type = ValueType;
            if (type.IsEnum || type == typeof(string) || IsNumericType(type) || type == typeof(char)) return true;
            if (type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan)) return true;
            return TgiParts.TryGet(type, out _);
        }
    }

    #endregion

    #region Children

    /// <summary>Whether the row can be expanded.</summary>
    public bool HasChildren => IsFlags || IsCollection || IsComplex(ValueType, RawValue);

    /// <summary>Gets or sets whether the row is expanded.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            Raise(nameof(IsExpanded));
        }
    }

    /// <summary>Child rows: nested properties, list elements or flag bits.</summary>
    public ObservableCollection<PropertyItem> Children
    {
        get
        {
            if (_children == null)
            {
                _children = [];
                BuildChildren();
            }
            return _children;
        }
    }

    /// <summary>Rebuilds the child rows from the current value.</summary>
    public void RefreshChildren()
    {
        if (_children == null) return;
        BuildChildren();
    }

    private void BuildChildren()
    {
        ClearChildren();
        if (_children == null) return;

        object? value;
        try
        {
            value = _accessor.GetValue();
        }
        catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or NotSupportedException or ArgumentException or IndexOutOfRangeException)
        {
            return;
        }

        if (value == null) return;

        if (IsFlags)
        {
            // Source: S4PIPropertyGrid.cs lines 729-891 (EnumFlagsCTD: one bool per flag value)
            foreach (var name in Enum.GetNames(ValueType))
            {
                var flag = Convert.ToUInt64(Enum.Parse(ValueType, name), CultureInfo.InvariantCulture);
                if (flag == 0 || (flag & (flag - 1)) != 0) continue; // single bits only
                AddChild(new PropertyItem(new FlagAccessor(this, name, flag), this, -1, null));
            }
            return;
        }

        var list = PropertyListAdapter.TryCreate(value, _accessor.ValueType, _accessor.CanWrite ? SetRawValue : null);
        if (list != null)
        {
            _childOwner = list;
            int count = Math.Min(list.Count, MaxChildRows);
            for (int i = 0; i < count; i++)
            {
                AddChild(new PropertyItem(new ElementAccessor(list, i), this, i, list.List));
            }
            return;
        }

        if (!IsComplex(ValueType, value)) return;

        // Nested object: one row per readable public property. Value types are edited on this box and written back.
        _childOwner = value;
        var owner = value;
        Action<object>? writeBack = value.GetType().IsValueType ? box => SetRawValue(box) : null;
        foreach (var prop in GetDisplayProperties(value.GetType()))
        {
            AddChild(new PropertyItem(new PropertyAccessor(() => owner, prop, writeBack), this, -1, owner));
        }
    }

    private void AddChild(PropertyItem child)
    {
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (e.PropertyName == nameof(Value))
                OnChildEdited();
        };
        child.PropertyChanged += handler;
        _childSubscriptions.Add((child, handler));
        _children!.Add(child);
    }

    private void ClearChildren()
    {
        foreach (var (child, handler) in _childSubscriptions)
        {
            child.PropertyChanged -= handler;
            child.Dispose();
        }
        _childSubscriptions.Clear();
        _children?.Clear();
        _childOwner = null;
    }

    private void OnChildEdited()
    {
        // A child changed this value in place (or wrote a new box back); show the new summary and bubble up.
        RaiseValueChanged();
    }

    /// <summary>Gets the readable public instance properties shown for a nested object.</summary>
    internal static IEnumerable<PropertyInfo> GetDisplayProperties(Type type)
    {
        bool isList = PropertyListAdapter.IsListType(type);
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Where(p => !typeof(Stream).IsAssignableFrom(p.PropertyType))
            .Where(p => !(isList && CollectionMetaProperties.Contains(p.Name)))
            .OrderBy(p => p.MetadataToken);
    }

    /// <summary>Whether a value of <paramref name="type"/> is shown as an expandable object.</summary>
    internal static bool IsComplex(Type type, object? value)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)) return false;
        if (type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan)) return false;
        if (type == typeof(byte[]) || type == typeof(ReadOnlyMemory<byte>) || type == typeof(Memory<byte>)) return false;
        if (typeof(Type).IsAssignableFrom(type) || typeof(Delegate).IsAssignableFrom(type)) return false;
        var runtime = value?.GetType() ?? type;
        return runtime.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.CanRead && p.GetIndexParameters().Length == 0);
    }

    #endregion

    #region List element commands

    /// <summary>Adds a default element (collection rows).</summary>
    public IRelayCommand AddItemCommand { get; }

    /// <summary>Inserts a default element before this one (element rows).</summary>
    public IRelayCommand InsertItemCommand { get; }

    /// <summary>Appends a copy of this element (element rows). Source: NewGridForm.cs lines 154-169 (btnCopy_Click).</summary>
    public IRelayCommand DuplicateItemCommand { get; }

    /// <summary>Removes this element (element rows).</summary>
    public IRelayCommand RemoveItemCommand { get; }

    /// <summary>Whether a default element can be appended.</summary>
    public bool CanAddItem => IsCollection && GetListAdapter() is { CanAdd: true };

    /// <summary>Whether this element can be removed.</summary>
    public bool CanRemove => IsListElement && _parent!.GetListAdapter() is { CanRemove: true };

    /// <summary>Whether a default element can be inserted before this one.</summary>
    public bool CanInsertBefore => IsListElement && _parent!.GetListAdapter() is { CanRemove: true, CanAdd: true };

    /// <summary>Whether this element can be copied (value types and strings only; reference elements have no clone).</summary>
    public bool CanDuplicate => IsListElement && _parent!.GetListAdapter() is { CanAdd: true }
        && (ValueType.IsValueType || ValueType == typeof(string) || RawValue is ICloneable);

    /// <summary>Whether the row shows any list buttons.</summary>
    public bool HasListCommands => CanAddItem || CanRemove;

    private PropertyListAdapter? GetListAdapter()
    {
        if (_childOwner is PropertyListAdapter cached) return cached;
        try
        {
            return PropertyListAdapter.TryCreate(_accessor.GetValue(), _accessor.ValueType, _accessor.CanWrite ? SetRawValue : null);
        }
        catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Appends a default element.</summary>
    public void AddItem()
    {
        var list = GetListAdapter();
        if (list is not { CanAdd: true }) return;
        if (TryListEdit(() => list.AddDefault()))
            AfterListEdit();
    }

    /// <summary>Removes this element from its list.</summary>
    public void RemoveItem()
    {
        if (!CanRemove) return;
        var parent = _parent!;
        var list = parent.GetListAdapter()!;
        if (TryListEdit(() => list.RemoveAt(_elementIndex)))
            parent.AfterListEdit();
    }

    /// <summary>Inserts a default element before this one.</summary>
    public void InsertBefore()
    {
        if (!CanInsertBefore) return;
        var parent = _parent!;
        var list = parent.GetListAdapter()!;
        if (TryListEdit(() => list.Insert(_elementIndex, PropertyListAdapter.CreateDefault(list.ElementType))))
            parent.AfterListEdit();
    }

    /// <summary>Appends a copy of this element.</summary>
    public void DuplicateItem()
    {
        if (!CanDuplicate) return;
        var parent = _parent!;
        var list = parent.GetListAdapter()!;
        var value = RawValue is ICloneable c && !ValueType.IsValueType ? c.Clone() : RawValue;
        if (TryListEdit(() => list.Insert(list.Count, value)))
            parent.AfterListEdit();
    }

    private static bool TryListEdit(Action edit)
    {
        try
        {
            edit();
            return true;
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentException or InvalidCastException or TargetInvocationException or InvalidOperationException)
        {
            return false;
        }
    }

    private void AfterListEdit()
    {
        _childOwner = null;
        if (_children != null) BuildChildren();
        IsExpanded = true;
        RaiseValueChanged();
    }

    #endregion

    #region Get / set

    /// <summary>The current raw value.</summary>
    public object? RawValue
    {
        get
        {
            try
            {
                return _accessor.GetValue();
            }
            catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or NotSupportedException or ArgumentException or IndexOutOfRangeException)
            {
                return null;
            }
        }
    }

    private bool TrySetRawValue(object? value)
    {
        try
        {
            SetRawValue(value);
            return true;
        }
        catch (Exception ex) when (ex is TargetInvocationException or ArgumentException or InvalidCastException or NotSupportedException or InvalidOperationException or FormatException or OverflowException)
        {
            return false;
        }
    }

    private void SetRawValue(object? value)
    {
        _accessor.SetValue(value);
        if (_children != null && value is not null && !ReferenceEquals(value, _childOwner))
        {
            BuildChildren();
        }
        RaiseValueChanged();
    }

    private void RaiseValueChanged()
    {
        Raise(nameof(BoolValue));
        Raise(nameof(EnumName));
        Raise(nameof(Value));
    }

    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private string GetValueString()
    {
        try
        {
            var value = _accessor.GetValue();
            if (value == null) return "(null)";

            var type = ValueType;

            // Numeric types displayed as hex (common in Sims resources)
            // Source: S4PIPropertyGrid.cs lines 539-586 (AsHexConverter)
            string? hex = FormatHex(value);
            if (hex != null)
            {
                if (IsCatalogTag && CatalogTagRegistry.GetTagName(Convert.ToUInt32(value, CultureInfo.InvariantCulture)) is { } tagName)
                    return $"{hex} ({tagName})";
                return hex;
            }

            if (type.IsEnum)
                return $"{value} (0x{Convert.ToUInt64(value, CultureInfo.InvariantCulture):X})";

            if (type == typeof(float) || type == typeof(double))
                return ((IFormattable)value).ToString("R", CultureInfo.InvariantCulture);

            if (TgiParts.TryGet(value.GetType(), out var parts))
                return parts.Format(value);

            if (IsBinaryData)
                return $"({BinaryData?.Length ?? 0} bytes)";

            if (PropertyListAdapter.IsListType(value.GetType()))
            {
                var list = PropertyListAdapter.TryCreate(value, _accessor.ValueType, null);
                return $"({GetSimpleTypeName(value.GetType())}: {list?.Count ?? 0} items)";
            }

            var text = value.ToString() ?? "(null)";
            if (text == value.GetType().FullName) text = $"({GetSimpleTypeName(value.GetType())})";
            return text.Length > 200 ? text[..200] + "..." : text;
        }
        catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or NotSupportedException or ArgumentException or IndexOutOfRangeException or FormatException or InvalidCastException or OverflowException)
        {
            return $"(error: {(ex.InnerException ?? ex).Message})";
        }
    }

    private void SetValueFromString(string value)
    {
        if (!_accessor.CanWrite || value == null) return;
        if (TryParse(value, ValueType, out var converted))
            TrySetRawValue(converted);
    }

    /// <summary>
    /// Parses grid text into a value of <paramref name="type"/>: hex (<c>0x</c>) or decimal integers, enum names,
    /// floats (invariant culture), bools, strings and TGI triples.
    /// </summary>
    /// <remarks>Source: S4PIPropertyGrid.cs lines 539-586 (AsHexConverter.ConvertFrom) and 893-952 (IResourceKeyConverter).</remarks>
    internal static bool TryParse(string text, Type type, out object? result)
    {
        result = null;
        type = Nullable.GetUnderlyingType(type) ?? type;
        try
        {
            if (type == typeof(string))
            {
                result = text;
                return true;
            }

            var trimmed = text.Trim();

            if (TgiParts.TryGet(type, out var parts))
                return parts.TryParse(trimmed, out result);

            if (type.IsEnum)
            {
                // Display is "Name (0xHEX)" or "A, B (0xHEX)" for flags.
                var token = trimmed;
                var suffix = token.LastIndexOf(" (0x", StringComparison.OrdinalIgnoreCase);
                if (suffix > 0) token = token[..suffix];
                if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    result = Enum.ToObject(type, Convert.ToUInt64(token[2..], 16));
                    return true;
                }
                if (Enum.TryParse(type, token.Replace("|", ",", StringComparison.Ordinal), ignoreCase: true, out var e))
                {
                    result = e;
                    return true;
                }
                return false;
            }

            if (type == typeof(bool))
            {
                if (bool.TryParse(trimmed, out var b)) { result = b; return true; }
                return false;
            }

            if (IsIntegral(type))
            {
                // Display may append a name: "0x00000123 (Name)".
                var token = trimmed.Split(' ', 2)[0];
                if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    var raw = Convert.ToUInt64(token[2..], 16);
                    result = type.Name switch
                    {
                        nameof(UInt32) => (uint)raw,
                        nameof(UInt64) => raw,
                        nameof(UInt16) => (ushort)raw,
                        nameof(Byte) => (byte)raw,
                        nameof(Int32) => unchecked((int)raw),
                        nameof(Int64) => unchecked((long)raw),
                        nameof(Int16) => unchecked((short)raw),
                        nameof(SByte) => unchecked((sbyte)raw),
                        _ => null
                    };
                    return result != null;
                }
                result = Convert.ChangeType(token, type, CultureInfo.InvariantCulture);
                return true;
            }

            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(char)
                || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid))
            {
                result = type == typeof(Guid) ? Guid.Parse(trimmed)
                    : type == typeof(TimeSpan) ? TimeSpan.Parse(trimmed, CultureInfo.InvariantCulture)
                    : Convert.ChangeType(trimmed, type, CultureInfo.InvariantCulture);
                return true;
            }
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException or ArgumentException)
        {
            result = null;
        }
        return false;
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClearChildren();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} = {Value}";

    #region Helpers

    private static bool IsCatalogTagName(string name) =>
        name.Contains("Tag", StringComparison.Ordinal)
        && !name.Contains("Tgi", StringComparison.OrdinalIgnoreCase)
        && !name.EndsWith("Count", StringComparison.Ordinal)
        && !name.EndsWith("Version", StringComparison.Ordinal);

    private static string? FormatHex(object value) => value switch
    {
        uint v => $"0x{v:X8}",
        ulong v => $"0x{v:X16}",
        ushort v => $"0x{v:X4}",
        byte v => $"0x{v:X2}",
        int v => $"0x{v:X8}",
        long v => $"0x{v:X16}",
        short v => $"0x{v:X4}",
        sbyte v => $"0x{v:X2}",
        _ => null
    };

    private static bool IsIntegral(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);

    private static bool IsNumericType(Type type) =>
        IsIntegral(type) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    internal static string GetSimpleTypeName(Type type)
    {
        if (type.IsArray) return GetSimpleTypeName(type.GetElementType()!) + "[]";
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition().Name;
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetSimpleTypeName));
            var tick = genericDef.IndexOf('`', StringComparison.Ordinal);
            var baseName = tick >= 0 ? genericDef[..tick] : genericDef;
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

    #endregion

    #region Accessors

    private abstract class Accessor
    {
        public abstract string Name { get; }
        public abstract Type ValueType { get; }
        public abstract bool CanWrite { get; }
        public virtual object? Owner => null;
        public abstract object? GetValue();
        public abstract void SetValue(object? value);
    }

    private sealed class PropertyAccessor(Func<object> owner, PropertyInfo property, Action<object>? writeBackOwner) : Accessor
    {
        public PropertyInfo Property => property;
        public override string Name => property.Name;
        public override Type ValueType => property.PropertyType;
        public override bool CanWrite => property.CanWrite && property.SetMethod is { IsPublic: true };
        public override object? Owner => owner();
        public override object? GetValue() => property.GetValue(owner());

        public override void SetValue(object? value)
        {
            var target = owner();
            property.SetValue(target, value);
            // Boxed struct owners must be written back to their own owner.
            writeBackOwner?.Invoke(target);
        }
    }

    private sealed class ElementAccessor(PropertyListAdapter list, int index) : Accessor
    {
        public override string Name => $"[{index:X}]";
        public override Type ValueType => list[index]?.GetType() is { } t && list.ElementType == typeof(object) ? t : list.ElementType;
        public override bool CanWrite => list.CanSetItem;
        public override object? Owner => list.List;
        public override object? GetValue() => list[index];

        public override void SetValue(object? value) => list.SetItem(index, value);
    }

    private sealed class FlagAccessor(PropertyItem parent, string name, ulong flag) : Accessor
    {
        public override string Name => name;
        public override Type ValueType => typeof(bool);
        public override bool CanWrite => parent._accessor.CanWrite;

        public override object? GetValue() =>
            parent.RawValue is { } v && (Convert.ToUInt64(v, CultureInfo.InvariantCulture) & flag) != 0;

        public override void SetValue(object? value)
        {
            var current = parent.RawValue is { } v ? Convert.ToUInt64(v, CultureInfo.InvariantCulture) : 0UL;
            var updated = value is true ? current | flag : current & ~flag;
            parent._accessor.SetValue(Enum.ToObject(parent.ValueType, updated));
        }
    }

    #endregion
}

/// <summary>
/// Reads, formats and parses TGI-style values (any type with type, group and instance members), mirroring legacy
/// IResourceKeyCTD which shows and accepts "0xTTTTTTTT-0xGGGGGGGG-0xIIIIIIIIIIIIIIII".
/// </summary>
/// <remarks>Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 893-952.</remarks>
internal sealed class TgiParts
{
    private static readonly Dictionary<Type, TgiParts?> Cache = [];
    private static readonly Lock CacheLock = new();

    private readonly PropertyInfo _type;
    private readonly PropertyInfo _group;
    private readonly PropertyInfo _instance;
    private readonly ConstructorInfo? _ctor;
    private readonly string[] _ctorOrder = [];

    private TgiParts(PropertyInfo type, PropertyInfo group, PropertyInfo instance, ConstructorInfo? ctor, string[] ctorOrder)
    {
        _type = type;
        _group = group;
        _instance = instance;
        _ctor = ctor;
        _ctorOrder = ctorOrder;
    }

    public static bool TryGet(Type type, [NotNullWhen(true)] out TgiParts? parts)
    {
        lock (CacheLock)
        {
            if (!Cache.TryGetValue(type, out parts))
            {
                parts = Create(type);
                Cache[type] = parts;
            }
        }
        return parts != null;
    }

    private static TgiParts? Create(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return null;
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo? Find(params string[] names) =>
            props.FirstOrDefault(p => names.Contains(p.Name, StringComparer.OrdinalIgnoreCase) && p.GetIndexParameters().Length == 0
                && (p.PropertyType == typeof(uint) || p.PropertyType == typeof(ulong)));

        var t = Find("ResourceType", "Type");
        var g = Find("ResourceGroup", "Group");
        var i = Find("Instance");
        if (t == null || g == null || i == null || t.PropertyType != typeof(uint) || g.PropertyType != typeof(uint) || i.PropertyType != typeof(ulong))
            return null;

        foreach (var ctor in type.GetConstructors())
        {
            var ps = ctor.GetParameters();
            if (ps.Length != 3) continue;
            var order = new string[3];
            bool ok = true;
            for (int k = 0; k < 3; k++)
            {
                var n = ps[k].Name ?? string.Empty;
                order[k] = n.Contains("instance", StringComparison.OrdinalIgnoreCase) ? "I"
                    : n.Contains("group", StringComparison.OrdinalIgnoreCase) ? "G"
                    : n.Contains("type", StringComparison.OrdinalIgnoreCase) ? "T" : "?";
                if (order[k] == "?") ok = false;
            }
            if (ok && order.Distinct().Count() == 3) return new TgiParts(t, g, i, ctor, order);
        }

        return new TgiParts(t, g, i, null, []);
    }

    public string Format(object value) =>
        $"0x{(uint)_type.GetValue(value)!:X8}-0x{(uint)_group.GetValue(value)!:X8}-0x{(ulong)_instance.GetValue(value)!:X16}";

    public bool TryParse(string text, out object? result)
    {
        result = null;
        var pieces = text.Split(['-', ':', ' '], StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length != 3) return false;

        static string Hex(string s) => s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s;
        if (!uint.TryParse(Hex(pieces[0]), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var t)) return false;
        if (!uint.TryParse(Hex(pieces[1]), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)) return false;
        if (!ulong.TryParse(Hex(pieces[2]), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var i)) return false;

        if (_ctor != null)
        {
            var args = _ctorOrder.Select(o => o switch { "T" => (object)t, "G" => g, _ => i }).ToArray();
            result = _ctor.Invoke(args);
            return true;
        }

        var declaring = _type.DeclaringType!;
        if (declaring.GetConstructor(Type.EmptyTypes) == null && !declaring.IsValueType) return false;
        if (!_type.CanWrite || !_group.CanWrite || !_instance.CanWrite) return false;
        var obj = Activator.CreateInstance(declaring)!;
        _type.SetValue(obj, t);
        _group.SetValue(obj, g);
        _instance.SetValue(obj, i);
        result = obj;
        return true;
    }
}
