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
/// Abstract base class for resource factories providing common functionality.
/// </summary>
/// <typeparam name="TResource">The type of resource this factory creates</typeparam>
public abstract class ResourceFactoryBase<TResource> : IResourceFactory<TResource> 
    where TResource : IResource
{
    private readonly HashSet<string> _supportedResourceTypes;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFactoryBase{TResource}"/> class.
    /// </summary>
    /// <param name="supportedResourceTypes">The resource types this factory supports</param>
    /// <param name="priority">Factory priority (higher values take precedence)</param>
    protected ResourceFactoryBase(IEnumerable<string> supportedResourceTypes, int priority = 0)
    {
        _supportedResourceTypes = new HashSet<string>(supportedResourceTypes, StringComparer.OrdinalIgnoreCase);
        Priority = priority;
    }
    
    /// <inheritdoc />
    public abstract Task<TResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public IReadOnlySet<string> SupportedResourceTypes => _supportedResourceTypes;
    
    /// <inheritdoc />
    public int Priority { get; }
    
    /// <summary>
    /// Validates that the provided API version is supported.
    /// </summary>
    /// <param name="apiVersion">API version to validate</param>
    /// <exception cref="ArgumentException">Thrown when API version is not supported</exception>
    protected virtual void ValidateApiVersion(int apiVersion)
    {
        if (apiVersion < 1)
        {
            throw new ArgumentException($"API version must be greater than 0, got {apiVersion}", nameof(apiVersion));
        }
    }
    
    /// <summary>
    /// Creates a memory stream from the provided stream, if not null.
    /// </summary>
    /// <param name="sourceStream">Source stream to copy from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A memory stream containing the data, or null if source is null</returns>
    protected static async Task<MemoryStream?> CreateMemoryStreamAsync(Stream? sourceStream, CancellationToken cancellationToken = default)
    {
        if (sourceStream == null)
            return null;
            
        var memoryStream = new MemoryStream();
        await sourceStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }
}
