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
/// Defines a factory for creating resource instances of a specific type.
/// </summary>
/// <typeparam name="TResource">The type of resource this factory creates</typeparam>
public interface IResourceFactory<TResource> where TResource : IResource
{
    /// <summary>
    /// Creates a new resource instance.
    /// </summary>
    /// <param name="apiVersion">API version for the resource</param>
    /// <param name="stream">Optional stream containing resource data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new resource instance</returns>
    Task<TResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the resource types this factory can create.
    /// </summary>
    IReadOnlySet<string> SupportedResourceTypes { get; }
    
    /// <summary>
    /// Gets the priority of this factory (higher values have priority over lower values).
    /// </summary>
    int Priority { get; }
}
