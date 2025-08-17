using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Images;

/// <summary>
/// Factory for creating RLE (Run-Length Encoded) compressed image resources.
/// Supports RLE2, RLES, and other RLE format variations used in The Sims 4.
/// </summary>
public sealed class RLEResourceFactory : ResourceFactoryBase<IRLEResource>
{
    private static readonly string[] SupportedTypes = { "RLE", "RLE2", "RLES" };
    private readonly ILogger<RLEResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RLEResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for this factory.</param>
    public RLEResourceFactory(ILogger<RLEResourceFactory> logger)
        : base(SupportedTypes, priority: 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override Task<IRLEResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating RLE resource with API version {ApiVersion}, Stream length: {StreamLength}",
                apiVersion, stream?.Length ?? 0);

            // Create resource - logger will be NullLogger by default like other resources
            var resource = new RLEResource(apiVersion, stream);

            if (stream is not null)
            {
                _logger.LogDebug("Successfully created RLE resource: {Width}x{Height}, Version: {Version}",
                    resource.Width, resource.Height, resource.Version);
            }

            return Task.FromResult<IRLEResource>(resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RLE resource");
            throw new InvalidOperationException("Failed to create RLE resource", ex);
        }
    }

    /// <summary>
    /// Override TryGetResourceTypeId to provide RLE-specific type mappings.
    /// </summary>
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "RLE" => 0x2F7D0004,  // Generic RLE resource type
            "RLE2" => 0x2F7D0005, // RLE2 specific format
            "RLES" => 0x2F7D0006, // RLES specific format
            _ => 0
        };

        return id != 0 || base.TryGetResourceTypeId(resourceType, out id);
    }
}
