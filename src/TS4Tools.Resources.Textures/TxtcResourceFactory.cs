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
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Textures;

/// <summary>
/// Factory for creating <see cref="TxtcResource"/> instances.
/// Supports TXTC (Texture Compositor) resource type 0x00B2D882.
/// </summary>
public class TxtcResourceFactory : IResourceFactory
{
    #region Constants

    /// <summary>The resource types supported by this factory.</summary>
    public static readonly IReadOnlySet<string> SupportedTypes = new HashSet<string> { TxtcResource.ResourceType };

    #endregion

    #region Fields

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TxtcResourceFactory> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TxtcResourceFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider or logger is null.</exception>
    public TxtcResourceFactory(IServiceProvider serviceProvider, ILogger<TxtcResourceFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("TxtcResourceFactory initialized with priority {Priority}", Priority);
    }

    #endregion

    #region IResourceFactory Implementation

    /// <inheritdoc />
    public IReadOnlySet<string> SupportedResourceTypes => new HashSet<string> { TxtcResource.ResourceType };

    /// <inheritdoc />
    public int Priority => 100;

    /// <inheritdoc />
    public async Task<IResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating TxtcResource from stream with API version {ApiVersion}", apiVersion);

        var resourceLogger = _serviceProvider.GetRequiredService<ILogger<TxtcResource>>();
        var resource = new TxtcResource(resourceLogger, apiVersion, stream);

        if (stream != null && stream.Length > 0)
        {
            try
            {
                await resource.DeserializeAsync();
                _logger.LogInformation("Successfully created TxtcResource with {LayerCount} layers", resource.Layers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create TxtcResource from stream");
                resource.Dispose();
                throw;
            }
        }

        return resource;
    }

    /// <inheritdoc />
    public Dictionary<string, object> GetDiagnosticInfo()
    {
        return new Dictionary<string, object>
        {
            ["FactoryType"] = nameof(TxtcResourceFactory),
            ["SupportedResourceTypes"] = SupportedResourceTypes.ToArray(),
            ["Priority"] = Priority,
            ["CreatedAt"] = DateTimeOffset.UtcNow,
            ["SupportedApiVersions"] = 1,
            ["ResourceTypeDescription"] = "Texture Compositor (TXTC) resources for texture composition and material systems"
        };
    }

    #endregion
}
