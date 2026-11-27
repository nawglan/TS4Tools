// Source: legacy_references/Sims4Tools/s4pi/Interfaces/VersionAttribute.cs lines 23-67

namespace s4pi.Interfaces;

/// <summary>
/// Base class for versioning support.
/// </summary>
public class VersionAttribute : Attribute
{
    /// <summary>
    /// Initialise a new instance.
    /// </summary>
    /// <param name="Version">The version.</param>
    public VersionAttribute(int Version) { this.Version = Version; }

    /// <summary>
    /// The version.
    /// </summary>
    public int Version { get; set; }
}

/// <summary>
/// Specify the minimum version from which a field or method is supported.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public class MinimumVersionAttribute : VersionAttribute
{
    /// <summary>
    /// Specify the minimum version from which a field or method is supported.
    /// </summary>
    /// <param name="Version">The version from which the field or method is supported.</param>
    public MinimumVersionAttribute(int Version) : base(Version) { }
}

/// <summary>
/// Specify the maximum version up to which a field or method is supported.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public class MaximumVersionAttribute : VersionAttribute
{
    /// <summary>
    /// Specify the maximum version up to which a field or method is supported.
    /// </summary>
    /// <param name="Version">The version up to which the field or method is supported.</param>
    public MaximumVersionAttribute(int Version) : base(Version) { }
}
