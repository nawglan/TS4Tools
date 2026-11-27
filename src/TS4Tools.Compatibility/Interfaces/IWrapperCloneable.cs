// Source: legacy_references/Sims4Tools/s4pi/Interfaces/IWrapperCloneable.cs lines 6-22

namespace s4pi.Interfaces;

/// <summary>
/// Interface for self-clone wrapper.
/// </summary>
/// <typeparam name="T">Wrapper type.</typeparam>
public interface IWrapperCloneable<T> where T : AResource
{
    /// <summary>
    /// Deep clone the wrapper object with built-in renumbering function.
    /// </summary>
    /// <param name="hashsalt">Hash salt to renumber the fields.</param>
    /// <param name="renumber">If true, preset fields will be renumbered.</param>
    /// <param name="isStandAlone">If true, the clone does not reference the original.</param>
    /// <returns>The cloned wrapper.</returns>
    T CloneWrapper(string hashsalt, bool renumber = true, bool isStandAlone = true);
}
