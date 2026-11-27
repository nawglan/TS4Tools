// Source: legacy_references/Sims4Tools/s4pi/Interfaces/TGIBlockListContentFieldAttribute.cs lines 23-58

namespace s4pi.Interfaces;

/// <summary>
/// Specify the name of the content field holding the TGIBlock list into which this field is an index.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class TGIBlockListContentFieldAttribute : Attribute
{
    /// <summary>
    /// Specify the TGIBlock list content field name.
    /// </summary>
    /// <param name="value">Name of the TGIBlock list content field.</param>
    public TGIBlockListContentFieldAttribute(string value) { TGIBlockListContentField = value; }

    /// <summary>
    /// Name of the TGIBlock list content field.
    /// </summary>
    public string TGIBlockListContentField { get; set; }

    /// <summary>
    /// Return the TGIBlockListContentField value for a Content Field.
    /// </summary>
    /// <param name="t">Type on which Content Field exists.</param>
    /// <param name="index">Content Field name.</param>
    /// <returns>The value of the TGIBlockListContentFieldAttribute, if found; otherwise <c>null</c>.</returns>
    public static string? GetTGIBlockListContentField(Type t, string index)
    {
        var pi = ContentFieldReflection.FindProperty(t, index);
        if (pi != null)
            foreach (var attr in pi.GetCustomAttributes(typeof(TGIBlockListContentFieldAttribute), true))
                return ((TGIBlockListContentFieldAttribute)attr).TGIBlockListContentField;
        return null;
    }
}
