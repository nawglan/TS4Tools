namespace TS4Tools.Extensions.ResourceTypes;

/// <summary>
/// Provides a modern lookup service from resource types to resource "tags" and file extensions.
/// This replaces the legacy static ExtList with a service-based approach that supports
/// dependency injection and configuration.
/// </summary>
public interface IResourceTypeRegistry
{
    /// <summary>
    /// Gets the file extension for the specified resource type.
    /// </summary>
    /// <param name="resourceType">The resource type identifier.</param>
    /// <returns>The file extension (including the dot) or null if not found.</returns>
    string? GetExtension(uint resourceType);

    /// <summary>
    /// Gets the resource tag/name for the specified resource type.
    /// </summary>
    /// <param name="resourceType">The resource type identifier.</param>
    /// <returns>The resource tag/name or null if not found.</returns>
    string? GetTag(uint resourceType);

    /// <summary>
    /// Gets all supported resource types.
    /// </summary>
    /// <returns>A collection of all supported resource type identifiers.</returns>
    IEnumerable<uint> GetSupportedTypes();

    /// <summary>
    /// Checks if the specified resource type is supported.
    /// </summary>
    /// <param name="resourceType">The resource type identifier.</param>
    /// <returns>True if the resource type is supported; otherwise, false.</returns>
    bool IsSupported(uint resourceType);

    /// <summary>
    /// Registers a new resource type with its associated tag and extension.
    /// </summary>
    /// <param name="resourceType">The resource type identifier.</param>
    /// <param name="tag">The resource tag/name.</param>
    /// <param name="extension">The file extension (should include the dot).</param>
    void RegisterType(uint resourceType, string tag, string extension);
}
