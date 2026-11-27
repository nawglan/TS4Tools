using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.UI.Views.Controls;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// ViewModel for the property grid editor.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs
/// This provides a reflection-based property editor for any resource wrapper. Rows expand to nested objects,
/// list elements (with add/insert/copy/delete, legacy NewGridForm) and flag bits; edits anywhere in the tree
/// bubble up as a Value change of the top-level row (see <see cref="PropertyItem"/>).
/// Edits are made directly on the wrapper; <see cref="PropertyValueChanged"/> lets the main
/// window mark the resource as having uncommitted edits, mirroring how legacy s4pe's grid
/// edits raise ResourceChanged and are written back by Commit
/// (legacy_references/Sims4Tools/s4pe/MainForm.cs lines 3008-3029 controlPanel1_GridClick,
/// lines 3060-3073 controlPanel1_CommitClick).
/// </remarks>
public partial class PropertyGridViewModel : ViewModelBase, IDisposable
{
    private bool _disposed;
    private object? _target;
    private readonly List<(PropertyItem Item, PropertyChangedEventHandler Handler)> _subscriptions = [];

    /// <summary>
    /// Raised after a property value on the target has been changed through the grid.
    /// </summary>
    public event EventHandler? PropertyValueChanged;

    [ObservableProperty]
    private string _typeName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<PropertyItem> Properties { get; } = [];

    /// <summary>
    /// Loads a resource wrapper for editing.
    /// </summary>
    public void LoadResource(object resource)
    {
        _target = resource;
        TypeName = resource.GetType().Name;
        RefreshProperties();
    }

    /// <summary>
    /// Refreshes the property list from the target object.
    /// </summary>
    [RelayCommand]
    private void RefreshProperties()
    {
        ClearSubscriptions();
        Properties.Clear();

        if (_target == null)
        {
            TypeName = string.Empty;
            return;
        }

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
                PropertyChangedEventHandler handler = (_, e) =>
                {
                    // PropertyItem raises Value only after a successful set on the target.
                    if (e.PropertyName == nameof(PropertyItem.Value))
                    {
                        PropertyValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                };
                item.PropertyChanged += handler;
                _subscriptions.Add((item, handler));
                Properties.Add(item);
            }
            catch
            {
                // Skip properties that throw on access
            }
        }

        StatusMessage = $"{Properties.Count} properties";
    }

    private void ClearSubscriptions()
    {
        foreach (var (item, handler) in _subscriptions)
        {
            item.PropertyChanged -= handler;
            // Releases the nested rows' subscriptions to their own children.
            item.Dispose();
        }
        _subscriptions.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ClearSubscriptions();
        GC.SuppressFinalize(this);
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
            "RecommendedApiVersion",
            "Key",  // Already shown in header
            "Data", // Raw byte data
            "ResourceKey"
        };

        return filteredNames.Contains(prop.Name) ||
               typeof(Stream).IsAssignableFrom(prop.PropertyType) ||
               typeof(ReadOnlyMemory<byte>).IsAssignableFrom(prop.PropertyType);
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
