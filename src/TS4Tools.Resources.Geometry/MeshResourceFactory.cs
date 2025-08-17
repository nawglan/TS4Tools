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

using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Geometry;

/// <summary>
/// Factory for creating MeshResource instances from various data sources.
/// Supports simplified mesh data format for basic 3D geometry.
/// </summary>
public sealed class MeshResourceFactory : ResourceFactoryBase<MeshResource>
{
    private readonly ILogger<MeshResourceFactory>? _logger;

    /// <summary>
    /// Resource types that this factory can handle (string identifiers).
    /// </summary>
    public static readonly IReadOnlySet<string> SupportedResourceTypeStrings =
        new System.Collections.ObjectModel.ReadOnlySet<string>(new HashSet<string>
        {
            "MLOD",     // Model Level of Detail
            "VBUF",     // Vertex Buffer
            "IBUF",     // Index Buffer
            "VRTF",     // Vertex Format
            "MESH"      // Generic Mesh
        });

    /// <summary>
    /// Resource types that this factory can handle (numeric IDs).
    /// </summary>
    public new IReadOnlySet<uint> ResourceTypes { get; }

    /// <summary>
    /// Initializes a new instance of the MeshResourceFactory.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public MeshResourceFactory(ILogger<MeshResourceFactory>? logger = null)
        : base(SupportedResourceTypeStrings, priority: 140) // Slightly lower than GeometryResourceFactory
    {
        _logger = logger;

        // Build the correct resource types collection using our override method
        var resourceTypeIds = new HashSet<uint>();
        foreach (var typeString in SupportedResourceTypeStrings)
        {
            if (TryGetResourceTypeId(typeString, out var id))
            {
                resourceTypeIds.Add(id);
            }
        }
        ResourceTypes = resourceTypeIds;
    }

    /// <summary>
    /// Creates a mesh resource from the specified stream.
    /// </summary>
    /// <param name="apiVersion">The API version to use.</param>
    /// <param name="stream">The stream containing mesh data.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A new MeshResource instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the stream contains invalid data.</exception>
    public override async Task<MeshResource> CreateResourceAsync(int apiVersion, Stream? stream, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make this truly async

        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var originalPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            _logger?.LogDebug("Creating mesh resource from stream (length: {Length} bytes)", stream.Length);

            // Validate stream has minimum required size
            if (stream.Length == 0)
            {
                _logger?.LogError("Failed to create mesh resource from stream: Stream is empty");
                throw new InvalidOperationException("Stream is empty");
            }
            else if (stream.Length < 16)
            {
                _logger?.LogError("Failed to create mesh resource from stream: Invalid format - stream too small");
                throw new InvalidOperationException("Invalid format - stream too small to contain valid mesh data");
            }

            // For mesh resources, we'll try to parse the simple format first
            // If that fails, we might try to extract mesh data from other formats
            var resource = new MeshResource(stream, apiVersion);
            _logger?.LogDebug("Created mesh resource with {VertexCount} vertices and {TriangleCount} triangles",
                resource.VertexCount, resource.TriangleCount);

            // Reset stream position if seekable
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }

            return resource;
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is InvalidOperationException))
        {
            _logger?.LogError(ex, "Failed to create mesh resource from stream");
            throw new InvalidOperationException("Failed to parse mesh data from stream", ex);
        }
    }

    /// <summary>
    /// Determines if this factory can handle the specified resource type.
    /// </summary>
    /// <param name="resourceType">The resource type to check.</param>
    /// <returns>True if this factory can handle the resource type.</returns>
    public bool CanHandle(uint resourceType)
    {
        var result = ResourceTypes.Contains(resourceType);
        _logger?.LogTrace("CanHandle check for resource type 0x{ResourceType:X8}: {Result}", resourceType, result);
        return result;
    }

    /// <summary>
    /// Builds the set of supported resource types.
    /// </summary>
    /// <returns>A set of supported resource type IDs.</returns>
    private static IReadOnlySet<uint> BuildSupportedResourceTypes()
    {
        var resourceTypes = new HashSet<uint>();

        // Mesh-related resource types (from original Sims4Tools analysis)
        resourceTypes.Add(0x021CDB6C); // MLOD - Model Level of Detail
        resourceTypes.Add(0x0229684B); // VBUF - Vertex Buffer  
        resourceTypes.Add(0x0229684A); // IBUF - Index Buffer
        resourceTypes.Add(0x01D0E723); // VRTF - Vertex Format
        resourceTypes.Add(0x01661233); // Additional mesh format

        return resourceTypes;
    }

    /// <summary>
    /// Attempts to get a resource type ID from a string identifier.
    /// </summary>
    /// <param name="typeString">The string identifier.</param>
    /// <param name="resourceTypeId">The resulting resource type ID.</param>
    /// <returns>True if the conversion was successful.</returns>
    protected override bool TryGetResourceTypeId(string typeString, out uint resourceTypeId)
    {
        return typeString.ToUpperInvariant() switch
        {
            "MLOD" => SetId(out resourceTypeId, 0x021CDB6C),
            "VBUF" => SetId(out resourceTypeId, 0x0229684B),
            "IBUF" => SetId(out resourceTypeId, 0x0229684A),
            "VRTF" => SetId(out resourceTypeId, 0x01D0E723),
            "MESH" => SetId(out resourceTypeId, 0x01661233),
            _ => base.TryGetResourceTypeId(typeString, out resourceTypeId)
        };

        static bool SetId(out uint id, uint value)
        {
            id = value;
            return value != 0;
        }
    }
}
