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
/// Non-generic base interface for all resource factories.
/// </summary>
public interface IResourceFactory
{
    /// <summary>
    /// Creates a new resource instance asynchronously.
    /// </summary>
    /// <param name="apiVersion">API version for the resource</param>
    /// <param name="stream">Optional stream containing resource data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new resource instance</returns>
    Task<IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the resource types this factory can create.
    /// </summary>
    IReadOnlySet<string> SupportedResourceTypes { get; }

    /// <summary>
    /// Gets the priority of this factory (higher values have priority over lower values).
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Defines a factory for creating resource instances of a specific type.
/// </summary>
/// <typeparam name="TResource">The type of resource this factory creates</typeparam>
public interface IResourceFactory<TResource> : IResourceFactory where TResource : IResource
{
    /// <summary>
    /// Creates a new resource instance asynchronously.
    /// </summary>
    /// <param name="apiVersion">API version for the resource</param>
    /// <param name="stream">Optional stream containing resource data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new resource instance</returns>
    new Task<TResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new resource instance synchronously.
    /// </summary>
    /// <param name="stream">Stream containing resource data</param>
    /// <param name="resourceType">Resource type identifier</param>
    /// <returns>A new resource instance</returns>
    TResource CreateResource(Stream stream, uint resourceType);

    /// <summary>
    /// Creates an empty resource instance.
    /// </summary>
    /// <param name="resourceType">Resource type identifier</param>
    /// <returns>A new empty resource instance</returns>
    TResource CreateEmptyResource(uint resourceType);

    /// <summary>
    /// Determines if this factory can create resources of the specified type.
    /// </summary>
    /// <param name="resourceType">Resource type identifier</param>
    /// <returns>True if the factory can create this resource type</returns>
    bool CanCreateResource(uint resourceType);

    /// <summary>
    /// Gets the resource types this factory can create.
    /// </summary>
    new IReadOnlySet<string> SupportedResourceTypes { get; }

    /// <summary>
    /// Gets the resource types this factory can create (legacy compatibility).
    /// </summary>
    IReadOnlySet<uint> ResourceTypes { get; }

    /// <summary>
    /// Gets the priority of this factory (higher values have priority over lower values).
    /// </summary>
    new int Priority { get; }
}
