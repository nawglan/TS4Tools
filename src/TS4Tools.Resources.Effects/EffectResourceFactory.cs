using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Effects;

/// <summary>
/// Factory for creating effect resources with dependency injection support.
/// </summary>
public class EffectResourceFactory : ResourceFactoryBase<IEffectResource>
{
    private readonly ILogger<EffectResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public EffectResourceFactory(ILogger<EffectResourceFactory> logger)
        : base(new[] { "RSLT", "MATD", "EFCT", "SHAD" }, priority: 50)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<IEffectResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        try
        {
            _logger.LogDebug("Creating effect resource from stream (length: {Length})", stream?.Length ?? 0);

            var resource = new EffectResource();

            if (stream != null)
            {
                await resource.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully created effect resource with type {EffectType}",
                resource.EffectType);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create effect resource from stream");
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "RSLT" => 0x033A1435, // Shader/Material Resource
            "MATD" => 0x033B2B66, // Material Definition
            "EFCT" => 0x033C3C97, // Effect Resource
            "SHAD" => 0x033D4DC8, // Shader Resource
            _ => 0
        };

        return id != 0;
    }
}
