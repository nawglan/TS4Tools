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
/// Provides modern dependency injection-based resource loading and factory management.
/// Replaces the legacy s4pi.WrapperDealer reflection-based system.
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Creates a new resource instance of the specified type.
    /// </summary>
    /// <param name="resourceType">Resource type identifier (e.g., "0xDEADBEEF")</param>
    /// <param name="apiVersion">API version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new resource instance</returns>
    Task<IResource> CreateResourceAsync(string resourceType, int apiVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a resource from a package.
    /// </summary>
    /// <param name="package">Package containing the resource</param>
    /// <param name="resourceIndexEntry">Resource index entry</param>
    /// <param name="apiVersion">API version</param>
    /// <param name="forceDefaultWrapper">If true, forces use of default resource wrapper</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded resource</returns>
    Task<IResource> LoadResourceAsync(IPackage package, IResourceIndexEntry resourceIndexEntry, int apiVersion, bool forceDefaultWrapper = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered resource factories and their supported types.
    /// </summary>
    IReadOnlyDictionary<string, Type> GetResourceTypeMap();

    /// <summary>
    /// Registers a resource factory for dependency injection.
    /// </summary>
    /// <typeparam name="TResource">Resource type</typeparam>
    /// <typeparam name="TFactory">Factory type</typeparam>
    void RegisterFactory<TResource, TFactory>()
        where TResource : IResource
        where TFactory : class, IResourceFactory<TResource>;

    /// <summary>
    /// Gets statistics about resource loading performance and cache usage.
    /// </summary>
    ResourceManagerStatistics GetStatistics();
}
