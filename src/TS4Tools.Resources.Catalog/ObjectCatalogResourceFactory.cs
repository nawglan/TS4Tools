using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Factory for creating object catalog resource instances from streams.
/// Handles object-specific catalog resources for Buy/Build mode functionality.
/// </summary>
public sealed class ObjectCatalogResourceFactory : ResourceFactoryBase<ObjectCatalogResource>
{
    private readonly ILogger<ObjectCatalogResourceFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectCatalogResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="loggerFactory">The logger factory for creating resource loggers.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or loggerFactory is null.</exception>
    public ObjectCatalogResourceFactory(ILogger<ObjectCatalogResourceFactory> logger, ILoggerFactory loggerFactory)
        : base(GetResourceTypes(), priority: 11) // Slightly higher priority than general catalog factory
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        _logger.LogDebug("ObjectCatalogResourceFactory initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Gets the resource type mappings supported by this factory.
    /// </summary>
    private static IEnumerable<string> GetResourceTypes()
    {
        // Return hex strings that will be parsed properly
        return new[]
        {
            "0x319E4F1D", // Object catalog resource (primary type for Buy/Build objects)
            "0x0355E0A6", // Object definition resource (alternative format)
            "0x2E75C764", // Object tuning resource (tuning-specific objects)
            "0x8BC04EDB"  // Object instance resource (placed object instances)
        };
    }

    #endregion

    #region Resource Creation

    /// <inheritdoc />
    public override async Task<ObjectCatalogResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating object catalog resource with API version {ApiVersion}", apiVersion);

            // Create logger for the specific resource instance
            var resourceLogger = _loggerFactory.CreateLogger<ObjectCatalogResource>();

            // Create the resource instance
            var resource = new ObjectCatalogResource(resourceLogger);

            // Load data from stream if provided
            if (stream is not null)
            {
                stream.Position = 0; // Ensure we start from the beginning
                await resource.LoadFromStreamAsync(stream, cancellationToken);

                _logger.LogInformation("Successfully created object catalog resource from {StreamLength} byte stream",
                    stream.Length);
            }
            else
            {
                _logger.LogInformation("Created empty object catalog resource");
            }

            return resource;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to create object catalog resource with API version {ApiVersion}", apiVersion);

            throw new InvalidDataException(
                $"Failed to create object catalog resource with API version {apiVersion}", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    private bool SupportsResourceType(uint resourceType)
    {
        var supportedTypes = new HashSet<uint>
        {
            0x319E4F1D, // Object catalog resource
            0x0355E0A6, // Object definition resource
            0x2E75C764, // Object tuning resource
            0x8BC04EDB  // Object instance resource
        };

        return supportedTypes.Contains(resourceType);
    }

    #endregion
}
