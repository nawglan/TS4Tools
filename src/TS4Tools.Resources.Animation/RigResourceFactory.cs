using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Factory for creating rig resources with dependency injection support.
/// </summary>
public class RigResourceFactory : ResourceFactoryBase<IRigResource>
{
    private readonly ILogger<RigResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RigResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public RigResourceFactory(ILogger<RigResourceFactory> logger)
        : base(new[] { "RIGS", "BOND", "SKEL" }, priority: 50)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<IRigResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        try
        {
            _logger.LogDebug("Creating rig resource from stream (length: {Length})", stream?.Length ?? 0);

            var resource = new RigResource();

            if (stream != null)
            {
                await resource.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully created rig resource '{RigName}' with {BoneCount} bones",
                resource.RigName, resource.BoneCount);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create rig resource from stream");
            throw;
        }
    }

    /// <summary>
    /// Validates that the provided API version is supported by this factory.
    /// </summary>
    /// <param name="apiVersion">API version to validate</param>
    /// <exception cref="ArgumentException">Thrown when API version is not supported</exception>
    protected override void ValidateApiVersion(int apiVersion)
    {
        base.ValidateApiVersion(apiVersion);

        if (apiVersion > 10) // Reasonable upper bound for API versions
        {
            throw new ArgumentException($"API version {apiVersion} is not supported by {nameof(RigResourceFactory)}", nameof(apiVersion));
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "RIGS" => 0x8EAF13DE, // Rig Resource (TS4)
            "BOND" => 0x01D0E723, // Bond Resource (Bone data)
            "SKEL" => 0x8EAF13DE, // Skeleton Resource (alternative name)
            _ => 0
        };

        return id != 0;
    }
}
