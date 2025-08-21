namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Represents a reference to another resource using TGI (Type-Group-Instance) format.
/// This is commonly used in Sims 4 resources to reference other assets.
/// </summary>
public interface IResourceReference
{
    /// <summary>
    /// Gets the resource type identifier.
    /// </summary>
    uint Type { get; }

    /// <summary>
    /// Gets the resource group identifier.
    /// </summary>
    uint Group { get; }

    /// <summary>
    /// Gets the resource instance identifier.
    /// </summary>
    ulong Instance { get; }

    /// <summary>
    /// Gets a value indicating whether this reference is valid (non-zero).
    /// </summary>
    bool IsValid { get; }
}
