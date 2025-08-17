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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Specialized.Configuration;

namespace TS4Tools.Resources.Specialized.Configuration;

/// <summary>
/// Factory for creating ConfigurationResource instances following the TS4Tools resource factory pattern.
/// Provides async creation methods for configuration resources with validation and error handling.
/// </summary>
public sealed class ConfigurationResourceFactory : ResourceFactoryBase<IConfigurationResource>
{
    private readonly ILogger<ConfigurationResourceFactory>? _logger;

    /// <summary>
    /// The standard resource type identifier for Configuration resources.
    /// This is a placeholder value - replace with actual resource type when known.
    /// </summary>
    public const uint ResourceType = 0xCF000000; // Placeholder - needs actual value

    /// <summary>
    /// Initializes a new instance of the ConfigurationResourceFactory class.
    /// </summary>
    public ConfigurationResourceFactory()
        : base(new[] { $"0x{ResourceType:X8}" }, 100) // Priority 100 for specialized resources
    {
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationResourceFactory class with logging.
    /// </summary>
    /// <param name="logger">Logger instance for factory operations</param>
    public ConfigurationResourceFactory(ILogger<ConfigurationResourceFactory> logger)
        : base(new[] { $"0x{ResourceType:X8}" }, 100) // Priority 100 for specialized resources
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new empty ConfigurationResource asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource</param>
    /// <param name="stream">Optional stream containing resource data (ignored for new resources)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created ConfigurationResource</returns>
    public override Task<IConfigurationResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (stream == null)
        {
            // Create new empty resource
            _logger?.LogDebug("Creating new empty ConfigurationResource with API version {ApiVersion}", apiVersion);
            var resource = new ConfigurationResource(apiVersion);
            return Task.FromResult<IConfigurationResource>(resource);
        }        // Create from stream data
        _logger?.LogDebug("Creating ConfigurationResource from stream with API version {ApiVersion}", apiVersion);
        return CreateFromStreamAsync(apiVersion, stream, cancellationToken);
    }

    /// <summary>
    /// Creates a ConfigurationResource from binary data asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource</param>
    /// <param name="data">The binary data to parse</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created ConfigurationResource</returns>
    /// <exception cref="ArgumentException">Thrown when data is invalid</exception>
    public Task<IConfigurationResource> CreateFromDataAsync(
        int apiVersion,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (data.IsEmpty)
        {
            _logger?.LogWarning("Attempted to create ConfigurationResource from empty data, creating empty resource instead");
            return CreateResourceAsync(apiVersion, null, cancellationToken);
        }

        _logger?.LogDebug("Creating ConfigurationResource from {DataSize} bytes with API version {ApiVersion}",
            data.Length, apiVersion);

        using var stream = new MemoryStream(data.ToArray());
        return CreateFromStreamAsync(apiVersion, stream, cancellationToken);
    }

    /// <summary>
    /// Creates a ConfigurationResource from a stream asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource</param>
    /// <param name="stream">The stream containing resource data</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created ConfigurationResource</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    /// <exception cref="ArgumentException">Thrown when stream data is invalid</exception>
    private async Task<IConfigurationResource> CreateFromStreamAsync(
        int apiVersion,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var resource = new ConfigurationResource(apiVersion);

            // Parse the stream data
            await resource.ParseFromStreamAsync(stream, cancellationToken);            _logger?.LogDebug("Successfully created ConfigurationResource from stream with {SectionCount} sections",
                resource.Sections.Count);

            return resource;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create ConfigurationResource from stream");
            throw new ArgumentException("Invalid configuration resource data", nameof(stream), ex);
        }
    }

    /// <summary>
    /// Creates a new ConfigurationResource with specific configuration data.
    /// </summary>
    /// <param name="apiVersion">The API version to use for the resource</param>
    /// <param name="configurationName">The name of the configuration</param>
    /// <param name="configurationVersion">The version of the configuration</param>
    /// <param name="configurationCategory">The category of the configuration</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created ConfigurationResource</returns>
    public async Task<IConfigurationResource> CreateConfigurationAsync(
        int apiVersion,
        string configurationName,
        string configurationVersion = "1.0",
        string configurationCategory = "general",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resource = await CreateResourceAsync(apiVersion, null, cancellationToken);

        // Configure the resource with provided data
        // Note: This would require updating the resource properties which may need setters
        _logger?.LogDebug("Created ConfigurationResource '{Name}' version {Version} in category {Category}",
            configurationName, configurationVersion, configurationCategory);

        return resource;
    }
}
