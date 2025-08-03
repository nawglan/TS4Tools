/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using System.ComponentModel.DataAnnotations;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Configuration settings for the resource management system.
/// </summary>
public sealed class ResourceManagerOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ResourceManager";
    
    /// <summary>
    /// Whether to enable resource caching (default: true).
    /// </summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>
    /// Maximum number of resources to keep in cache (default: 1000).
    /// </summary>
    [Range(1, 100000)]
    public int MaxCacheSize { get; set; } = 1000;
    
    /// <summary>
    /// Maximum memory usage for cache in MB (default: 100MB).
    /// </summary>
    [Range(1, 10000)]
    public int MaxCacheMemoryMB { get; set; } = 100;
    
    /// <summary>
    /// Cache expiration time in minutes (default: 30 minutes).
    /// </summary>
    [Range(1, 1440)]
    public int CacheExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// Whether to enable strict validation of resource types (default: true).
    /// </summary>
    public bool EnableStrictValidation { get; set; } = true;
    
    /// <summary>
    /// Whether to throw exceptions on missing resource handlers (default: true).
    /// When false, returns default resource wrapper.
    /// </summary>
    public bool ThrowOnMissingHandler { get; set; } = true;
    
    /// <summary>
    /// Timeout for resource loading operations in seconds (default: 30 seconds).
    /// </summary>
    [Range(1, 3600)]
    public int LoadTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Whether to enable performance metrics collection (default: true).
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
}
