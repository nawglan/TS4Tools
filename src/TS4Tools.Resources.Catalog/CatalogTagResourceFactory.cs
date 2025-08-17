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

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Factory for creating CatalogTagResource instances.
/// Provides support for catalog tagging and categorization resources.
/// </summary>
public sealed class CatalogTagResourceFactory : ResourceFactoryBase<CatalogTagResource>
{
    private readonly ILogger<CatalogTagResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTagResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <param name="loggerFactory">The logger factory for creating child loggers.</param>
    public CatalogTagResourceFactory(ILogger<CatalogTagResourceFactory> logger, ILoggerFactory loggerFactory)
        : base(GetResourceTypes(), priority: 200) // Higher priority than base catalog resource factory
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("CatalogTagResourceFactory initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Gets the resource type mappings supported by this factory.
    /// </summary>
    private static IEnumerable<string> GetResourceTypes()
    {
        return new[]
        {
            "0xCAAAD4B0", // Catalog tag resource (hypothetical resource type for tagging)
            "0xDEFACED0"  // Secondary catalog tag format
        };
    }

    /// <inheritdoc />
    public override async Task<CatalogTagResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating catalog tag resource with API version {ApiVersion}", apiVersion);

            var resource = new CatalogTagResource();

            if (stream is not null)
            {
                stream.Position = 0;
                await resource.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Successfully created catalog tag resource from {StreamLength} byte stream: TagId={TagId}, Name='{TagName}', Category={Category}",
                    stream.Length,
                    resource.TagId,
                    resource.TagName,
                    resource.Category);
            }
            else
            {
                _logger.LogInformation("Created empty catalog tag resource");
            }

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create catalog tag resource with API version {ApiVersion}", apiVersion);
            throw;
        }
    }
}
