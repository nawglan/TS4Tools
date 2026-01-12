namespace TS4Tools.Wrappers.Attributes;

/// <summary>
/// Marks a byte property as an index into a TGI block list.
/// The property grid will render a dropdown selector for this property.
/// </summary>
/// <remarks>
/// Source: legacy_references/Sims4Tools/s4pe/s4pePropertyGrid/TGIBlockSelection.cs
/// In legacy s4pe, this was done via TGIBlockListIndex and DependentList&lt;TGIBlock&gt;.
/// The modern approach uses an attribute to identify the list property name.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TgiBlockIndexAttribute : Attribute
{
    /// <summary>
    /// The name of the property containing the TGI block list.
    /// </summary>
    public string ListPropertyName { get; }

    /// <summary>
    /// Creates a new TgiBlockIndexAttribute.
    /// </summary>
    /// <param name="listPropertyName">Name of the property containing the TGI block list (e.g., "TgiBlocks").</param>
    public TgiBlockIndexAttribute(string listPropertyName)
    {
        ListPropertyName = listPropertyName;
    }
}
