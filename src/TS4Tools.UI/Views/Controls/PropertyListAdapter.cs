// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/NewGridForm.cs lines 86-255 (list editor: add/insert/copy/delete)
// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs lines 319-334 (arrays and collections)

namespace TS4Tools.UI.Views.Controls;

/// <summary>
/// Uniform read/write access to the collection kinds used by the wrappers: arrays, <see cref="IList"/> (e.g.
/// <c>List&lt;T&gt;</c>) and the wrappers' own list classes (an <c>int</c> indexer and <c>Count</c>, usually with
/// <c>Add(T)</c> and <c>Clear()</c>).
/// </summary>
/// <remarks>
/// <para>Legacy lists are all <c>DependentList&lt;T&gt;</c>/<c>IGenericAdd</c>, which NewGridForm drives through
/// <c>Add()</c>, <c>Insert</c>, <c>Remove</c> and <c>Clone</c>. The modern wrappers expose several shapes, so this
/// adapter finds the operations each one supports; where only <c>Add</c> and <c>Clear</c> exist, insert and delete
/// are done by rebuilding the list (clear, then add every remaining item).</para>
/// <para>A collection exposed through a read-only declared type (<c>IReadOnlyList&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>)
/// is never resized even if the runtime object is a <c>List&lt;T&gt;</c>: the wrapper keeps such lists consistent
/// through its own methods. Arrays are resized only when the owning property can be set.</para>
/// </remarks>
internal sealed class PropertyListAdapter
{
    private readonly object _list;
    private readonly IList? _ilist;
    private readonly Array? _array;
    private readonly PropertyInfo? _indexer;
    private readonly PropertyInfo? _countProperty;
    private readonly MethodInfo? _add;
    private readonly MethodInfo? _clear;
    private readonly MethodInfo? _removeAt;
    private readonly MethodInfo? _insert;
    private readonly IList? _snapshot;
    private readonly Action<object>? _replace;
    private readonly bool _resizable;

    private PropertyListAdapter(object list, Type elementType, Action<object>? replace, bool declaredMutable)
    {
        _list = list;
        ElementType = elementType;
        _replace = replace;

        switch (list)
        {
            case Array array:
                _array = array;
                _ilist = array;
                _resizable = replace != null && declaredMutable;
                CanSetItem = true;
                return;
            case IList ilist:
                _ilist = ilist;
                _resizable = declaredMutable && !ilist.IsReadOnly && !ilist.IsFixedSize;
                CanSetItem = !ilist.IsReadOnly;
                return;
        }

        var type = list.GetType();
        _indexer = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.GetIndexParameters() is [{ ParameterType: var t }] && t == typeof(int));
        _countProperty = type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);

        if (_indexer == null || _countProperty == null || _countProperty.PropertyType != typeof(int))
        {
            // Read-only enumerable (e.g. IReadOnlyList<T>, dictionaries): take a snapshot for display.
            _snapshot = list is IEnumerable enumerable ? enumerable.Cast<object?>().ToList() : [];
            _indexer = null;
            _countProperty = null;
            CanSetItem = false;
            return;
        }

        CanSetItem = _indexer.CanWrite;
        _add = FindMethod(type, "Add", elementType);
        _clear = type.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
        _removeAt = type.GetMethod("RemoveAt", BindingFlags.Public | BindingFlags.Instance, [typeof(int)]);
        _insert = type.GetMethod("Insert", BindingFlags.Public | BindingFlags.Instance, [typeof(int), elementType]);
        _resizable = declaredMutable && _add != null;
    }

    /// <summary>The element type.</summary>
    public Type ElementType { get; }

    /// <summary>The underlying list object.</summary>
    public object List => _list;

    /// <summary>Whether elements can be replaced in place.</summary>
    public bool CanSetItem { get; }

    /// <summary>Whether elements can be added.</summary>
    public bool CanAdd => _resizable && CanCreateDefault(ElementType);

    /// <summary>Whether elements can be removed or inserted.</summary>
    public bool CanRemove => _resizable && (_array != null || _ilist != null || _removeAt != null || _clear != null);

    /// <summary>Number of elements.</summary>
    public int Count => _snapshot?.Count ?? _ilist?.Count ?? (int)_countProperty!.GetValue(_list)!;

    /// <summary>Gets an element.</summary>
    public object? this[int index]
    {
        get
        {
            if (_snapshot != null) return _snapshot[index];
            if (_ilist != null) return _ilist[index];
            return _indexer!.GetValue(_list, [index]);
        }
    }

    /// <summary>
    /// Creates an adapter if <paramref name="value"/> is a collection the grid should show as elements.
    /// </summary>
    /// <param name="value">The property value.</param>
    /// <param name="declaredType">The property's declared type (decides whether the collection may be resized).</param>
    /// <param name="replace">Sets a new collection instance on the owner (needed to resize arrays), or null.</param>
    public static PropertyListAdapter? TryCreate(object? value, Type declaredType, Action<object>? replace)
    {
        if (value is null or string or byte[]) return null;

        var type = value.GetType();
        if (!IsListType(type)) return null;

        var elementType = GetElementType(type) ?? typeof(object);
        bool declaredMutable = IsMutableDeclaration(declaredType);
        return new PropertyListAdapter(value, elementType, replace, declaredMutable);
    }

    /// <summary>Whether a value of <paramref name="type"/> is shown as a list of elements.</summary>
    public static bool IsListType(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[])) return false;
        if (typeof(IEnumerable).IsAssignableFrom(type)) return true;

        // Wrapper list classes that are not IEnumerable but expose "this[int]" and "Count" (e.g. CatalogTagList).
        return HasIndexer(type) && type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance)?.PropertyType == typeof(int);
    }

    private static bool HasIndexer(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.GetIndexParameters() is [{ ParameterType: var t }] && t == typeof(int));

    /// <summary>Replaces an element.</summary>
    public void SetItem(int index, object? value)
    {
        if (_ilist != null)
        {
            _ilist[index] = value;
            return;
        }

        if (_indexer is { CanWrite: true })
        {
            _indexer.SetValue(_list, value, [index]);
            return;
        }

        // Rebuild when there is no setter: legacy lists always allow element replacement.
        if (_add != null && _clear != null)
        {
            var items = Snapshot();
            items[index] = value;
            Rebuild(items);
            return;
        }

        throw new NotSupportedException("This list does not allow entries to be changed.");
    }

    /// <summary>Adds a default element at the end. Source: NewGridForm.cs lines 208-226 (simpleAddInsert).</summary>
    public void AddDefault() => Insert(Count, CreateDefault(ElementType));

    /// <summary>Inserts an element. Source: NewGridForm.cs lines 230-236 (doInsert).</summary>
    public void Insert(int index, object? value)
    {
        if (!_resizable) throw new NotSupportedException("This list does not allow entries to be added this way.");
        index = Math.Clamp(index, 0, Count);

        if (_array != null)
        {
            var items = Snapshot();
            items.Insert(index, value);
            Rebuild(items);
            return;
        }

        if (_ilist != null)
        {
            _ilist.Insert(index, value);
            return;
        }

        if (index == Count)
        {
            _add!.Invoke(_list, [value]);
            return;
        }

        if (_insert != null)
        {
            _insert.Invoke(_list, [index, value]);
            return;
        }

        var rebuilt = Snapshot();
        rebuilt.Insert(index, value);
        Rebuild(rebuilt);
    }

    /// <summary>Removes an element. Source: NewGridForm.cs lines 238-255 (btnDelete_Click).</summary>
    public void RemoveAt(int index)
    {
        if (!CanRemove) throw new NotSupportedException("This list does not allow entries to be deleted this way.");

        if (_array != null)
        {
            var items = Snapshot();
            items.RemoveAt(index);
            Rebuild(items);
            return;
        }

        if (_ilist != null)
        {
            _ilist.RemoveAt(index);
            return;
        }

        if (_removeAt != null)
        {
            _removeAt.Invoke(_list, [index]);
            return;
        }

        var rebuilt = Snapshot();
        rebuilt.RemoveAt(index);
        Rebuild(rebuilt);
    }

    private List<object?> Snapshot()
    {
        var items = new List<object?>(Count);
        for (int i = 0; i < Count; i++) items.Add(this[i]);
        return items;
    }

    private void Rebuild(List<object?> items)
    {
        if (_array != null)
        {
            var array = Array.CreateInstance(ElementType, items.Count);
            for (int i = 0; i < items.Count; i++) array.SetValue(items[i], i);
            _replace!(array);
            return;
        }

        if (_clear == null || _add == null)
            throw new NotSupportedException("This list does not allow entries to be deleted this way.");

        _clear.Invoke(_list, null);
        foreach (var item in items) _add.Invoke(_list, [item]);
    }

    /// <summary>Whether a default element can be created for <paramref name="type"/>.</summary>
    public static bool CanCreateDefault(Type type) =>
        type == typeof(string) || type.IsValueType ||
        (!type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null);

    /// <summary>Creates the value a new element starts with.</summary>
    public static object? CreateDefault(Type type)
    {
        if (type == typeof(string)) return string.Empty;
        if (type.IsValueType) return Activator.CreateInstance(type);
        return type.GetConstructor(Type.EmptyTypes) != null ? Activator.CreateInstance(type) : null;
    }

    private static MethodInfo? FindMethod(Type type, string name, Type elementType) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == name && m.GetParameters() is [{ ParameterType: var p }] && p.IsAssignableFrom(elementType));

    private static Type? GetElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();

        foreach (var candidate in new[] { typeof(IList<>), typeof(IReadOnlyList<>), typeof(IEnumerable<>) })
        {
            var iface = type.IsGenericType && type.GetGenericTypeDefinition() == candidate
                ? type
                : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == candidate);
            if (iface != null) return iface.GetGenericArguments()[0];
        }

        var indexer = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.GetIndexParameters() is [{ ParameterType: var t }] && t == typeof(int));
        return indexer?.PropertyType;
    }

    private static bool IsMutableDeclaration(Type declaredType)
    {
        if (declaredType.IsArray) return true;
        if (!declaredType.IsInterface) return true; // concrete list classes (List<T>, CatalogTagList, ...)
        if (declaredType.IsGenericType)
        {
            var def = declaredType.GetGenericTypeDefinition();
            return def == typeof(IList<>) || def == typeof(ICollection<>);
        }
        return declaredType == typeof(IList);
    }
}
