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

namespace TS4Tools.Core.Resources;

/// <summary>
/// Statistics about resource manager performance and usage.
/// </summary>
public sealed record ResourceManagerStatistics
{
    /// <summary>
    /// Total number of resources created since manager initialization.
    /// </summary>
    public long TotalResourcesCreated { get; init; }
    
    /// <summary>
    /// Total number of resources loaded from packages.
    /// </summary>
    public long TotalResourcesLoaded { get; init; }
    
    /// <summary>
    /// Number of registered resource factories.
    /// </summary>
    public int RegisteredFactories { get; init; }
    
    /// <summary>
    /// Cache hit ratio (0.0 to 1.0).
    /// </summary>
    public double CacheHitRatio { get; init; }
    
    /// <summary>
    /// Number of items currently in cache.
    /// </summary>
    public int CacheSize { get; init; }
    
    /// <summary>
    /// Total memory used by cached resources (in bytes).
    /// </summary>
    public long CacheMemoryUsage { get; init; }
    
    /// <summary>
    /// Average time to create a resource (in milliseconds).
    /// </summary>
    public double AverageCreationTimeMs { get; init; }
    
    /// <summary>
    /// Average time to load a resource from package (in milliseconds).
    /// </summary>
    public double AverageLoadTimeMs { get; init; }
}
