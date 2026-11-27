// Source: legacy_references/Sims4Tools/s4pi/Interfaces/ConstructorParametersAttribute.cs lines 23-42

// Legacy declares this attribute in the s3pi namespace (not s4pi); kept for source compatibility.
namespace s3pi.Interfaces;

/// <summary>
/// Used to specify the parameters to pass to a list element constructor (deprecated).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[Obsolete("Use DependentList<T>.Add(T instance) or DependendList<T>.Add(Type concrete-of-T).")]
public class ConstructorParametersAttribute : Attribute
{
    /// <summary>
    /// Parameters to pass to the constructor.
    /// </summary>
    public readonly object[] parameters;

    /// <summary>
    /// Specify the parameters to pass to a list element constructor.
    /// </summary>
    /// <param name="parameters">Parameters to pass to the constructor.</param>
    public ConstructorParametersAttribute(object[] parameters) { this.parameters = parameters; }
}
