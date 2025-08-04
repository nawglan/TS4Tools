using TS4Tools.Extensions.ResourceIdentification;
using TS4Tools.Extensions.ResourceTypes;

namespace TS4Tools.Extensions.Utilities;

/// <summary>
/// Provides services for converting resource identifiers to appropriate filenames.
/// This replaces the legacy FileNameConverter with a modern, service-based approach.
/// </summary>
public interface IFileNameService
{
    /// <summary>
    /// Generates a filename for the specified resource key.
    /// </summary>
    /// <param name="resourceKey">The resource key.</param>
    /// <param name="baseName">Optional base name to use instead of the default pattern.</param>
    /// <returns>A filename appropriate for the resource type.</returns>
    string GetFileName(IResourceKey resourceKey, string? baseName = null);

    /// <summary>
    /// Generates a filename for the specified resource identifier.
    /// </summary>
    /// <param name="identifier">The resource identifier.</param>
    /// <param name="baseName">Optional base name to use instead of the default pattern.</param>
    /// <returns>A filename appropriate for the resource type.</returns>
    string GetFileName(ResourceIdentifier identifier, string? baseName = null);

    /// <summary>
    /// Sanitizes a filename to ensure it's valid for the current file system.
    /// </summary>
    /// <param name="fileName">The filename to sanitize.</param>
    /// <returns>A sanitized filename safe for use on the current platform.</returns>
    string SanitizeFileName(string fileName);

    /// <summary>
    /// Gets a unique filename by appending a number if the file already exists.
    /// </summary>
    /// <param name="baseFileName">The base filename.</param>
    /// <param name="directory">The target directory.</param>
    /// <returns>A unique filename that doesn't exist in the specified directory.</returns>
    string GetUniqueFileName(string baseFileName, string directory);
}
