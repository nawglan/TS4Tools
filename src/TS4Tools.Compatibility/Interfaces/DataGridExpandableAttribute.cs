// Source: legacy_references/Sims4Tools/s4pi/Interfaces/DataGridExpandableAttribute.cs lines 23-65

// Legacy declares this attribute in the s3pi namespace (not s4pi); kept for source compatibility.
namespace s3pi.Interfaces;

/// <summary>
/// Specify whether the element should be expandable in a data grid (deprecated).
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
[Obsolete]
public class DataGridExpandableAttribute : Attribute
{
    /// <summary>
    /// Specify the element is expandable.
    /// </summary>
    public DataGridExpandableAttribute() { DataGridExpandable = true; }

    /// <summary>
    /// Specify whether the element is expandable.
    /// </summary>
    /// <param name="value">True if expandable.</param>
    public DataGridExpandableAttribute(bool value) { DataGridExpandable = value; }

    /// <summary>
    /// True if the element is expandable.
    /// </summary>
    public bool DataGridExpandable { get; set; }

    /// <summary>
    /// Return the DataGridExpandable value for a Content Field.
    /// </summary>
    /// <param name="t">Type on which Content Field exists.</param>
    /// <param name="index">Content Field name.</param>
    /// <returns>The value of the attribute, if found; otherwise <c>false</c>.</returns>
    public static bool GetDataGridExpandable(Type t, string index)
    {
        var pi = s4pi.Interfaces.ContentFieldReflection.FindProperty(t, index);
        if (pi != null)
            foreach (var attr in pi.GetCustomAttributes(typeof(DataGridExpandableAttribute), true))
                return ((DataGridExpandableAttribute)attr).DataGridExpandable;

        return false;
    }
}
