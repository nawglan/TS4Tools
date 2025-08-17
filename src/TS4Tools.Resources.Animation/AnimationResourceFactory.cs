using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Factory for creating animation resources with dependency injection support.
/// </summary>
public class AnimationResourceFactory : ResourceFactoryBase<IAnimationResource>
{
    private readonly ILogger<AnimationResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public AnimationResourceFactory(ILogger<AnimationResourceFactory> logger)
        : base(new[] { "CLIP", "ANIM", "S3CL", "JAZZ", "IKPD", "IKTM" }, priority: 50)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<IAnimationResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        try
        {
            _logger.LogDebug("Creating animation resource from stream (length: {Length})", stream?.Length ?? 0);

            var resource = new AnimationResource();

            if (stream != null)
            {
                await resource.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully created animation resource with type {AnimationType}",
                resource.AnimationType);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create animation resource from stream");
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "CLIP" => 0x6B20C4F3, // Animation Clip Resource (TS4)
            "ANIM" => 0x6B20C4F3, // Generic Animation Resource
            "S3CL" => 0x0166038C, // Sims 3 Clip Resource (legacy support)
            "JAZZ" => 0x02D5DF13, // Jazz Animation Resource
            "IKPD" => 0x0354796A, // IK Pose Data
            "IKTM" => 0x0354796B, // IK Track Mask
            _ => 0
        };

        return id != 0;
    }
}
