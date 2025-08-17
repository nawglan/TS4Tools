using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Visual;

/// <summary>
/// Factory for creating and managing IconResource instances.
/// Handles resource type 0x73E93EEC.
/// </summary>
public sealed class IconResourceFactory : ResourceFactoryBase<IconResource>
{
    /// <summary>
    /// The resource type ID for Icon resources
    /// </summary>
    public const string ResourceTypeId = "0x73E93EEC";

    private readonly ILogger<IconResourceFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="IconResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating resource loggers.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or loggerFactory is null.</exception>
    public IconResourceFactory(ILogger<IconResourceFactory> logger, ILoggerFactory loggerFactory)
        : base(new[] { ResourceTypeId }, priority: 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Creates an IconResource from a stream.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="stream">The stream containing icon data, or null for empty resource</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created IconResource</returns>
    /// <exception cref="ArgumentException">Thrown when the stream contains invalid icon data</exception>
    /// <exception cref="NotSupportedException">Thrown when the API version is unsupported</exception>
    public override async Task<IconResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Creating IconResource with API version {ApiVersion}, stream: {HasStream}",
            apiVersion, stream != null);

        try
        {
            var resourceLogger = _loggerFactory.CreateLogger<IconResource>();

            // Create a placeholder resource key for factory creation
            var resourceKey = new ResourceKey(0x73E93EEC, 0, 0);
            var iconResource = new IconResource(resourceLogger, resourceKey);

            if (stream != null)
            {
                await iconResource.LoadFromStreamAsync(stream, cancellationToken);
            }

            _logger.LogDebug("Successfully created IconResource");
            return iconResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create IconResource from stream");
            throw;
        }
    }

    /// <summary>
    /// Creates an IconResource with the specified resource key.
    /// </summary>
    /// <param name="apiVersion">The API version to use</param>
    /// <param name="resourceKey">The resource key for the icon</param>
    /// <param name="stream">The stream containing icon data, or null for empty resource</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task containing the created IconResource</returns>
    public async Task<IconResource> CreateResourceAsync(
        int apiVersion,
        ResourceKey resourceKey,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resourceKey);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Creating IconResource for key {ResourceKey} with API version {ApiVersion}",
            resourceKey, apiVersion);

        try
        {
            var resourceLogger = _loggerFactory.CreateLogger<IconResource>();
            var iconResource = new IconResource(resourceLogger, resourceKey);

            if (stream != null)
            {
                await iconResource.LoadFromStreamAsync(stream, cancellationToken);
            }

            return iconResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create IconResource for key {ResourceKey}", resourceKey);
            throw;
        }
    }
}
