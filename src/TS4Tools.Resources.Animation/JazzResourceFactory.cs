using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Factory for creating JAZZ animation state machine resources with dependency injection support.
/// </summary>
public class JazzResourceFactory : ResourceFactoryBase<IJazzResource>
{
    private readonly ILogger<JazzResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JazzResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public JazzResourceFactory(ILogger<JazzResourceFactory> logger)
        : base(new[] { "JAZZ", "0x02D5DF13" }, priority: 60)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<IJazzResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        try
        {
            _logger.LogDebug("Creating JAZZ resource from stream (length: {Length})", stream?.Length ?? 0);

            var resource = new JazzResource();

            if (stream != null)
            {
                await resource.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully created JAZZ resource with state machine '{StateMachineName}'",
                resource.StateMachineName);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create JAZZ resource from stream");
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "JAZZ" => 0x02D5DF13, // JAZZ Animation State Machine Resource
            "0X02D5DF13" => 0x02D5DF13, // Hex format support
            _ => 0
        };

        return id != 0;
    }
}
