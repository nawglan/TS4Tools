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

using Microsoft.Extensions.DependencyInjection;

namespace TS4Tools.Resources.Characters;

/// <summary>
/// Factory for creating CAS Part resources.
/// Handles the creation of <see cref="CasPartResource"/> instances from binary data streams.
/// </summary>
/// <remarks>
/// This factory is responsible for:
/// - Creating CAS part resources from binary streams
/// - Validation of input parameters
/// - Error handling and diagnostics
/// - Integration with the dependency injection system
/// </remarks>
public sealed class CasPartResourceFactory : IResourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CasPartResourceFactory> _logger;

    /// <summary>
    /// Gets the resource type identifier for CAS Part resources (0x034AEECB).
    /// </summary>
    public static string ResourceType => "0x034AEECB";

    /// <summary>
    /// Gets the resource types supported by this factory.
    /// </summary>
    public IReadOnlySet<string> SupportedResourceTypes { get; } = new HashSet<string> { ResourceType };

    /// <summary>
    /// Gets the priority of this factory (used for conflict resolution).
    /// Higher values take precedence.
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="CasPartResourceFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider or logger is null</exception>
    public CasPartResourceFactory(IServiceProvider serviceProvider, ILogger<CasPartResourceFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new CAS Part resource from the specified stream.
    /// </summary>
    /// <param name="apiVersion">The API version for compatibility</param>
    /// <param name="stream">The stream containing the resource data (optional)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A new <see cref="CasPartResource"/> instance</returns>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid data</exception>
    public Task<IResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "Stream is required for CAS Part resource creation");

        try
        {
            _logger.LogDebug("Creating CAS Part resource with API version {ApiVersion}", apiVersion);

            // Get logger for the resource instance
            var resourceLogger = _serviceProvider.GetRequiredService<ILogger<CasPartResource>>();

            var resource = new CasPartResource(apiVersion, stream, resourceLogger);

            _logger.LogInformation("Successfully created CAS Part resource: {Name} (Version: 0x{Version:X}, Body: {BodyType})",
                resource.Name, resource.Version, resource.BodyType);

            return Task.FromResult<IResource>(resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create CAS Part resource from stream");
            throw new InvalidDataException("Unable to create CAS Part resource from the provided stream", ex);
        }
    }

    /// <summary>
    /// Gets diagnostic information about this factory.
    /// </summary>
    /// <returns>Dictionary containing factory diagnostics</returns>
    public IReadOnlyDictionary<string, object> GetDiagnosticInfo()
    {
        return new Dictionary<string, object>
        {
            ["FactoryType"] = GetType().Name,
            ["SupportedResourceTypes"] = SupportedResourceTypes.ToList(),
            ["Priority"] = Priority,
            ["CreatedAt"] = DateTime.UtcNow
        };
    }
}
