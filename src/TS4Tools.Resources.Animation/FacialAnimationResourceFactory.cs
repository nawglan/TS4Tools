using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Factory for creating facial animation resources with dependency injection support.
/// Handles The Sims 4 facial animation resource type 0x0C772E27.
/// </summary>
public class FacialAnimationResourceFactory : ResourceFactoryBase<IFacialAnimationResource>
{
    private readonly ILogger<FacialAnimationResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FacialAnimationResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public FacialAnimationResourceFactory(ILogger<FacialAnimationResourceFactory> logger)
        : base(new[] { "FACE", "FACS", "EXPR", "EMOT", "LIPS" }, priority: 55)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<IFacialAnimationResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        _logger.LogDebug("Creating facial animation resource with API version {ApiVersion}", apiVersion);

        var resource = new FacialAnimationResource();

        if (stream != null)
        {
            try
            {
                var data = new byte[stream.Length];
                await stream.ReadExactlyAsync(data, cancellationToken).ConfigureAwait(false);
                await resource.DeserializeAsync(data, cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Successfully deserialized facial animation resource from stream ({Length} bytes)", data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize facial animation resource from stream");
                throw;
            }
        }
        else
        {
            _logger.LogDebug("Created empty facial animation resource");
        }

        return resource;
    }

    /// <inheritdoc />
    public override bool CanCreateResource(uint resourceType)
    {
        var canCreate = resourceType == 0x0C772E27;

        if (canCreate)
        {
            _logger.LogDebug("Can create facial animation resource for type 0x{ResourceType:X8}", resourceType);
        }

        return canCreate;
    }

    /// <inheritdoc />
    protected override void ValidateApiVersion(int apiVersion)
    {
        if (apiVersion < 1 || apiVersion > 2)
        {
            throw new NotSupportedException($"API version {apiVersion} is not supported for facial animation resources. Supported versions: 1-2");
        }
    }
}
