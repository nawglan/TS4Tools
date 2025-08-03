/***************************************************************************
 *  Copyright (C) 2025 by the TS4Tools contributors                       *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by  *
 *  the Free Software Foundation, either version 3 of the License, or     *
 *  (at your option) any later version.                                   *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the          *
 *  GNU General Public License for more details.                          *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License     *
 *  along with TS4Tools. If not, see <http://www.gnu.org/licenses/>.      *
 ***************************************************************************/

using System.ComponentModel.DataAnnotations;

namespace TS4Tools.Core.Settings;

/// <summary>
/// Application settings configuration model using the modern IOptions pattern.
/// Replaces the legacy static Settings class with a strongly-typed, injectable configuration.
/// </summary>
public sealed class ApplicationSettings
{
    /// <summary>
    /// Configuration section name for binding
    /// </summary>
    public const string SectionName = "ApplicationSettings";

    /// <summary>
    /// When true, run extra validation checks as part of normal operation.
    /// Equivalent to the legacy Settings.Checking property.
    /// </summary>
    [Required]
    public bool EnableExtraChecking { get; init; } = true;

    /// <summary>
    /// When true, assume resource data is dirty regardless of change tracking.
    /// Equivalent to the legacy Settings.AsBytesWorkaround property.
    /// </summary>
    [Required]
    public bool AssumeDataDirty { get; init; } = true;

    /// <summary>
    /// Indicates whether to use TS4 format for decoding files.
    /// Equivalent to the legacy Settings.IsTS4 property.
    /// </summary>
    [Required]
    public bool UseTS4Format { get; init; } = true;

    /// <summary>
    /// Maximum number of resources to cache in memory for performance.
    /// </summary>
    [Range(100, 10000)]
    public int MaxResourceCacheSize { get; init; } = 1000;

    /// <summary>
    /// Enable detailed logging for debugging purposes.
    /// </summary>
    public bool EnableDetailedLogging { get; init; } = false;

    /// <summary>
    /// Timeout in milliseconds for asynchronous operations.
    /// </summary>
    [Range(1000, 60000)]
    public int AsyncOperationTimeoutMs { get; init; } = 30000;

    /// <summary>
    /// Enable cross-platform compatibility features.
    /// </summary>
    public bool EnableCrossPlatformFeatures { get; init; } = true;

    /// <summary>
    /// Configuration for package-related settings.
    /// </summary>
    [Required]
    public PackageSettings Package { get; init; } = new();

    /// <summary>
    /// Configuration for UI-related settings.
    /// </summary>
    [Required]
    public UserInterfaceSettings UserInterface { get; init; } = new();
}

/// <summary>
/// Package-specific configuration settings.
/// </summary>
public sealed class PackageSettings
{
    /// <summary>
    /// Enable automatic backup creation when saving packages.
    /// </summary>
    public bool CreateBackups { get; init; } = true;

    /// <summary>
    /// Maximum number of backup files to retain.
    /// </summary>
    [Range(1, 50)]
    public int MaxBackupCount { get; init; } = 10;

    /// <summary>
    /// Enable compression for package files.
    /// </summary>
    public bool EnableCompression { get; init; } = true;

    /// <summary>
    /// Compression level (0-9, where 9 is maximum compression).
    /// </summary>
    [Range(0, 9)]
    public int CompressionLevel { get; init; } = 6;

    /// <summary>
    /// Buffer size in bytes for package I/O operations.
    /// </summary>
    [Range(4096, 1048576)] // 4KB to 1MB
    public int IOBufferSize { get; init; } = 65536; // 64KB default
}

/// <summary>
/// User interface configuration settings.
/// </summary>
public sealed class UserInterfaceSettings
{
    /// <summary>
    /// Theme preference for the application UI.
    /// </summary>
    [Required]
    public ThemePreference Theme { get; init; } = ThemePreference.System;

    /// <summary>
    /// Enable UI animations and transitions.
    /// </summary>
    public bool EnableAnimations { get; init; } = true;

    /// <summary>
    /// Show advanced options in the UI.
    /// </summary>
    public bool ShowAdvancedOptions { get; init; } = false;

    /// <summary>
    /// Number of recent files to remember.
    /// </summary>
    [Range(5, 50)]
    public int RecentFilesCount { get; init; } = 10;

    /// <summary>
    /// Enable auto-save functionality.
    /// </summary>
    public bool EnableAutoSave { get; init; } = false;

    /// <summary>
    /// Auto-save interval in minutes.
    /// </summary>
    [Range(1, 60)]
    public int AutoSaveIntervalMinutes { get; init; } = 5;
}

/// <summary>
/// Theme preference options for the application.
/// </summary>
public enum ThemePreference
{
    /// <summary>
    /// Use system theme preference
    /// </summary>
    System,

    /// <summary>
    /// Always use light theme
    /// </summary>
    Light,

    /// <summary>
    /// Always use dark theme
    /// </summary>
    Dark
}
