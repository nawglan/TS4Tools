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
/// Factory for creating GeometryResource instances from various data sources.
/// Supports automatic format detection and validation for 3D geometry data.
/// </summary>
public sealed class GeometryResourceFactory : ResourceFactoryBase<GeometryResource>
{
    private readonly ILogger<GeometryResourceFactory>? _logger;

    /// <summary>
    /// Resource types that this factory can handle (string identifiers).
    /// </summary>
    public static readonly IReadOnlySet<string> SupportedResourceTypeStrings = 
        new System.Collections.ObjectModel.ReadOnlySet<string>(new HashSet<string>
        {
            "GEOM",     // Geometry Resource
            "MESH",     // Mesh Resource (alias)
            "MODEL",    // Model Resource (alias)
            "3D"        // Generic 3D Resource (alias)
        });

    /// <summary>
    /// Resource types that this factory can handle (numeric IDs).
    /// </summary>
    public new IReadOnlySet<uint> ResourceTypes { get; }

    /// <summary>
    /// Initializes a new instance of the GeometryResourceFactory.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public GeometryResourceFactory(ILogger<GeometryResourceFactory>? logger = null)
        : base(SupportedResourceTypeStrings, priority: 150) // Higher priority for 3D content
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
    /// Creates a geometry resource from the specified stream.
    /// </summary>
    /// <param name="apiVersion">The API version to use.</param>
    /// <param name="stream">The stream containing geometry data.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A new GeometryResource instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the stream contains invalid data.</exception>
    public override async Task<GeometryResource> CreateResourceAsync(int apiVersion, Stream? stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        try
        {
            _logger?.LogDebug("Creating geometry resource from stream (length: {Length} bytes)", stream.Length);
            
            // Validate stream has minimum required size
            if (stream.Length == 0)
            {
                _logger?.LogError("Failed to create geometry resource from stream: Stream is empty");
                throw new InvalidOperationException("Stream is empty");
            }
            else if (stream.Length < 24)
            {
                _logger?.LogError("Failed to create geometry resource from stream: Invalid format - stream too small");
                throw new InvalidOperationException("Invalid format - stream too small to contain valid geometry data");
            }

            // Peek at the stream to validate GEOM tag without consuming data (only for seekable streams)
            long originalPosition = 0;
            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                var buffer = new byte[4];
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                stream.Position = originalPosition;

                if (bytesRead == 4)
                {
                    var tag = BitConverter.ToUInt32(buffer, 0);
                    if (tag != 0x47454F4D) // "GEOM"
                    {
                        _logger?.LogWarning("Invalid GEOM tag found: 0x{Tag:X8}", tag);
                        _logger?.LogError("Failed to create geometry resource from stream: Invalid GEOM tag: 0x{Tag:X8}", tag);
                        throw new InvalidOperationException($"Invalid GEOM tag: 0x{tag:X8}");
                    }
                }
            }

            var resource = new GeometryResource(stream, apiVersion);
            _logger?.LogDebug("Created geometry resource with {VertexCount} vertices and {FaceCount} faces", 
                resource.VertexCount, resource.Faces.Count);
            
            // Reset stream position if seekable
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
            
            return resource;
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is InvalidOperationException))
        {
            _logger?.LogError(ex, "Failed to create geometry resource from stream");
            throw new InvalidOperationException("Failed to parse geometry data from stream", ex);
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
        
        // Primary geometry resource type (from original Sims4Tools analysis)
        resourceTypes.Add(0x015A1849); // Main GEOM resource type
        
        // Additional geometry-related resource types that might be encountered
        resourceTypes.Add(0x01661233); // Alternative geometry format
        resourceTypes.Add(0x01D0E75D); // Mesh variant
        
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
            "GEOM" => SetId(out resourceTypeId, 0x015A1849),
            "MESH" => SetId(out resourceTypeId, 0x01661233),
            "MODEL" => SetId(out resourceTypeId, 0x01D0E75D),
            "3D" => SetId(out resourceTypeId, 0x015A1849),
            _ => base.TryGetResourceTypeId(typeString, out resourceTypeId)
        };
        
        static bool SetId(out uint id, uint value)
        {
            id = value;
            return value != 0;
        }
    }

}
