using System;
using System.IO;

namespace TS4Tools.Core.System.Platform;

/// <summary>
/// Provides platform-specific functionality and information.
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// Gets the current platform type.
    /// </summary>
    PlatformType CurrentPlatform { get; }

    /// <summary>
    /// Gets the platform-appropriate directory for application configuration files.
    /// </summary>
    string GetConfigurationDirectory();

    /// <summary>
    /// Gets the platform-appropriate directory for application data files.
    /// </summary>
    string GetApplicationDataDirectory();

    /// <summary>
    /// Validates whether a filename is valid for the current platform.
    /// </summary>
    /// <param name="fileName">The filename to validate.</param>
    /// <returns>True if the filename is valid for the current platform; otherwise, false.</returns>
    bool IsValidFileName(string fileName);

    /// <summary>
    /// Sanitizes a filename to be valid for the current platform.
    /// </summary>
    /// <param name="fileName">The filename to sanitize.</param>
    /// <returns>A sanitized filename that is valid for the current platform.</returns>
    string SanitizeFileName(string fileName);

    /// <summary>
    /// Gets the default line ending for the current platform.
    /// </summary>
    string GetLineEnding();

    /// <summary>
    /// Determines if the current platform supports the application manifest.
    /// </summary>
    bool SupportsApplicationManifest { get; }
}
