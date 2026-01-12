using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TS4Tools.UI.Views.Controls;

namespace TS4Tools.UI.ViewModels.Editors;

/// <summary>
/// ViewModel for the property grid editor.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/S4PIPropertyGrid.cs
/// This provides a simplified reflection-based property editor for any resource wrapper.
/// </remarks>
public partial class PropertyGridViewModel : ViewModelBase
{
    private object? _target;

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
                Properties.Add(item);
            }
            catch
            {
                // Skip properties that throw on access
            }
        }

        StatusMessage = $"{Properties.Count} properties";
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
