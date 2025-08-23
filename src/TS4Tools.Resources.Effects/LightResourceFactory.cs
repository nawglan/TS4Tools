using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Effects;

/// <summary>
/// Factory for creating light resources with dependency injection support.
/// </summary>
public class LightResourceFactory : ResourceFactoryBase<ILightResource>
{
    private readonly ILogger<LightResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LightResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public LightResourceFactory(ILogger<LightResourceFactory> logger)
        : base(new[] { "LITE", "0x03B4C61D" }, priority: 60)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<ILightResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        try
        {
            _logger.LogDebug("Creating light resource from stream (length: {Length})", stream?.Length ?? 0);

            var resource = new LightResource();

            if (stream != null)
            {
                await resource.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully created light resource with type '{LightType}' and intensity {Intensity}",
                resource.LightType, resource.Intensity);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create light resource from stream");
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "LITE" => 0x03B4C61D, // Light Resource
            "0X03B4C61D" => 0x03B4C61D, // Hex format support
            _ => 0
        };

        return id != 0;
    }
}
