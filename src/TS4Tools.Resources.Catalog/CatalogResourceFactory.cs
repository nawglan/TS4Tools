using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Factory for creating catalog resource instances from streams.
/// Supports multiple catalog resource types for different object categories.
/// </summary>
public sealed class CatalogResourceFactory : ResourceFactoryBase<CatalogResource>
{
    private readonly ILogger<CatalogResourceFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="loggerFactory">The logger factory for creating resource loggers</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or loggerFactory is null.</exception>
    public CatalogResourceFactory(ILogger<CatalogResourceFactory> logger, ILoggerFactory loggerFactory)
        : base(GetResourceTypes(), priority: 10)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Gets the resource type mappings supported by this factory.
    /// Matches the original Sims4Tools catalog resource types.
    /// </summary>
    private static IEnumerable<string> GetResourceTypes()
    {
        // Resource types from original Sims4Tools catalog handlers:
        // COBJResource, _48C28979CatalogResource, A8F7B517CatalogResource
        return new[]
        {
            "0x319E4F1D", // COBJResource (Object catalog) - PRIMARY TYPE from original
            "0x48C28979", // _48C28979CatalogResource (Standard catalog resource)
            "0xA8F7B517"  // A8F7B517CatalogResource (Alternative catalog format)
        };
    }

    #endregion

    #region Resource Creation

    /// <inheritdoc />
    public override Task<CatalogResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        // Check for cancellation before doing any work
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Creating catalog resource with API version {ApiVersion}", apiVersion);

        try
        {
            // Create a properly typed logger for the resource
            var resourceLogger = _loggerFactory.CreateLogger<CatalogResource>();

            var resource = stream != null
                ? new CatalogResource(resourceLogger, stream, cancellationToken)
                : new CatalogResource(resourceLogger);

            _logger.LogInformation("Successfully created catalog resource with API version {ApiVersion}", apiVersion);
            return Task.FromResult(resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create catalog resource with API version {ApiVersion}", apiVersion);
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = 0;

        // Handle hex string format
        if (resourceType.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(resourceType[2..], System.Globalization.NumberStyles.HexNumber, null, out id);
        }

        // Handle our named types
        id = resourceType.ToUpperInvariant() switch
        {
            "CTLG" => 0x48C28979,   // Standard catalog resource
            "CTLG2" => 0xA8F7B517,  // Alternative catalog resource format
            "CTLG5" => 0x1CC03E4C,  // Room catalog resource
            _ => 0
        };

        return id != 0;
    }

    #endregion
}
