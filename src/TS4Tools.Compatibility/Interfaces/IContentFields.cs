// Source: legacy_references/Sims4Tools/s4pi/Interfaces/IContentFields.cs lines 23-59

namespace s4pi.Interfaces;

/// <summary>
/// Standardised interface to API objects (hiding the reflection).
/// </summary>
public interface IContentFields
{
    /// <summary>
    /// The list of available field names on this API object.
    /// </summary>
    List<string> ContentFields { get; }

    /// <summary>
    /// A typed value on this object.
    /// </summary>
    /// <param name="index">The name of the field (i.e. one of the values from <see cref="ContentFields"/>).</param>
    /// <returns>The typed value of the named field.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown index name is requested.</exception>
    TypedValue this[string index] { get; set; }
}
