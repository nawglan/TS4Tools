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
    private readonly HashSet<uint> _supportedResourceTypeIds;
    private readonly IReadOnlySet<string> _readOnlySupportedResourceTypes;
    private readonly IReadOnlySet<uint> _readOnlyResourceTypeIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFactoryBase{TResource}"/> class.
    /// </summary>
    /// <param name="supportedResourceTypes">The resource types this factory supports</param>
    /// <param name="priority">Factory priority (higher values take precedence)</param>
    protected ResourceFactoryBase(IEnumerable<string> supportedResourceTypes, int priority = 0)
    {
        _supportedResourceTypes = new HashSet<string>(supportedResourceTypes, StringComparer.OrdinalIgnoreCase);
        _readOnlySupportedResourceTypes = new global::System.Collections.ObjectModel.ReadOnlySet<string>(_supportedResourceTypes);

        // Convert string types to numeric IDs for legacy compatibility
        _supportedResourceTypeIds = new HashSet<uint>();
        var supportedTypesList = supportedResourceTypes.ToList(); // Avoid multiple enumeration
        foreach (var type in supportedTypesList)
        {
            if (TryGetResourceTypeIdSafe(type, out var id))
            {
                _supportedResourceTypeIds.Add(id);
            }
        }
        _readOnlyResourceTypeIds = new global::System.Collections.ObjectModel.ReadOnlySet<uint>(_supportedResourceTypeIds);

        Priority = priority;
    }

    /// <inheritdoc />
    public abstract Task<TResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    async Task<IResource> IResourceFactory.CreateResourceAsync(int apiVersion, Stream? stream, CancellationToken cancellationToken)
    {
        return await CreateResourceAsync(apiVersion, stream, cancellationToken);
    }

    /// <inheritdoc />
    public virtual TResource CreateResource(Stream stream, uint resourceType)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!CanCreateResource(resourceType))
        {
            throw new ArgumentException($"Resource type 0x{resourceType:X8} is not supported by this factory", nameof(resourceType));
        }

        // Use async method synchronously for compatibility - deadlock-safe pattern
        return Task.Run(async () => await CreateResourceAsync(1, stream).ConfigureAwait(false)).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public virtual TResource CreateEmptyResource(uint resourceType)
    {
        if (!CanCreateResource(resourceType))
        {
            throw new ArgumentException($"Resource type 0x{resourceType:X8} is not supported by this factory", nameof(resourceType));
        }

        // Use async method synchronously for compatibility - deadlock-safe pattern
        return Task.Run(async () => await CreateResourceAsync(1, null).ConfigureAwait(false)).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public virtual bool CanCreateResource(uint resourceType)
    {
        return _supportedResourceTypeIds.Contains(resourceType);
    }

    /// <inheritdoc />
    public IReadOnlySet<string> SupportedResourceTypes => _readOnlySupportedResourceTypes;

    /// <inheritdoc />
    public IReadOnlySet<uint> ResourceTypes => _readOnlyResourceTypeIds;

    /// <inheritdoc />
    public int Priority { get; }

    /// <summary>
    /// Attempts to convert a resource type string to its numeric ID.
    /// Override this method to provide custom type mappings.
    /// </summary>
    /// <param name="resourceType">Resource type string</param>
    /// <param name="id">Resulting numeric ID</param>
    /// <returns>True if conversion was successful</returns>
    protected virtual bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = 0;

        // Handle hex string format first
        if (resourceType.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(resourceType[2..], global::System.Globalization.NumberStyles.HexNumber, null, out id);
        }

        // Default mappings for common image types
        id = resourceType.ToUpperInvariant() switch
        {
            "DDS" => 0x00B2D882,   // DDS Resource Type
            "PNG" => 0x2E75C765,   // PNG Resource Type  
            "TGA" => 0x2E75C764,   // TGA Resource Type
            "JPEG" => 0x2E75C766,  // JPEG Resource Type
            "BMP" => 0x2E75C767,   // BMP Resource Type
            "IMG" => 0x2E75C768,   // Generic Image Resource Type
            "TEX" => 0x2E75C769,   // Texture Resource Type
            _ => 0
        };

        return id != 0;
    }

    /// <summary>
    /// Non-virtual version for use in constructor to avoid CA2214 warning.
    /// </summary>
    /// <param name="resourceType">Resource type string</param>
    /// <param name="id">Resulting numeric ID</param>
    /// <returns>True if conversion was successful</returns>
    private bool TryGetResourceTypeIdSafe(string resourceType, out uint id)
    {
        id = 0;

        // Handle hex string format first
        if (resourceType.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(resourceType[2..], global::System.Globalization.NumberStyles.HexNumber, null, out id);
        }

        // Use base implementation in constructor to avoid virtual call
        id = resourceType.ToUpperInvariant() switch
        {
            "DDS" => 0x00B2D882,   // DDS Resource Type
            "PNG" => 0x2E75C765,   // PNG Resource Type  
            "TGA" => 0x2E75C764,   // TGA Resource Type
            "JPEG" => 0x2E75C766,  // JPEG Resource Type
            "BMP" => 0x2E75C767,   // BMP Resource Type
            "IMG" => 0x2E75C768,   // Generic Image Resource Type
            "TEX" => 0x2E75C769,   // Texture Resource Type
            _ => 0
        };

        return id != 0;
    }

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
