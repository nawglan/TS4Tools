// Source: legacy_references/Sims4Tools/s4pi/Interfaces/IGenericAdd.cs lines 23-57

namespace s4pi.Interfaces;

/// <summary>
/// Generic list "add" support.
/// </summary>
public interface IGenericAdd : IList
{
    /// <summary>
    /// Adds a default element to a list of the implementing type.
    /// </summary>
    void Add();

    /// <summary>
    /// Adds a default element of the given concrete type to a list of the implementing type.
    /// </summary>
    /// <param name="instanceType">The concrete type of element to add.</param>
    void Add(Type instanceType);
}
