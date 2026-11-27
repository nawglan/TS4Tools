using Avalonia.Controls;

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
