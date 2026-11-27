// Source: legacy_references/Sims4Tools/s4pi/Interfaces/ElementPriorityAttribute.cs lines 23-60

namespace s4pi.Interfaces;

/// <summary>
/// Specify the Priority of a data element when displayed (lower values sort first).
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public class ElementPriorityAttribute : Attribute
{
    /// <summary>
    /// Specify the Priority of a data element when displayed.
    /// </summary>
    /// <param name="priority">Priority of the data element.</param>
    public ElementPriorityAttribute(int priority) { Priority = priority; }

    /// <summary>
    /// Priority of the data element.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Return the ElementPriority value for a Content Field.
    /// </summary>
    /// <param name="t">Type on which Content Field exists.</param>
    /// <param name="index">Content Field name.</param>
    /// <returns>The value of the ElementPriorityAttribute Priority field, if found; otherwise <see cref="int.MaxValue"/>.</returns>
    /// <remarks>
    /// Like legacy, attributes are read from the property found by name (<c>PropertyInfo.GetCustomAttributes(Type, bool)</c>,
    /// which does not walk overridden declarations).
    /// </remarks>
    public static int GetPriority(Type t, string index)
    {
        var pi = ContentFieldReflection.FindProperty(t, index);
        if (pi != null)
            foreach (var attr in pi.GetCustomAttributes(typeof(ElementPriorityAttribute), true))
                return ((ElementPriorityAttribute)attr).Priority;

        return int.MaxValue;
    }
}
