using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Factory for creating character resources with dependency injection support.
/// </summary>
public class CharacterResourceFactory : ResourceFactoryBase<ICharacterResource>
{
    private readonly ILogger<CharacterResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public CharacterResourceFactory(ILogger<CharacterResourceFactory> logger)
        : base(new[] { "CASP", "SIMO", "CPRE", "BONE", "DMAP", "TONE", "ACOS", "OUTF", "BOND", "SKIN" }, priority: 50)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<ICharacterResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        try
        {
            _logger.LogDebug("Creating character resource from stream (length: {Length})", stream?.Length ?? 0);

            var resource = new CharacterResource();

            if (stream != null)
            {
                await resource.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully created character resource with type {CharacterType}",
                resource.CharacterType);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create character resource from stream");
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
            throw new ArgumentException($"API version {apiVersion} is not supported by {nameof(CharacterResourceFactory)}", nameof(apiVersion));
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "CASP" => 0x034AEECB, // CAS Part Resource
            "SIMO" => 0x04A1C2CA, // Sim Outfit Resource
            "CPRE" => 0x04ED4BB2, // CAS Preset Resource
            "BONE" => 0x01D0E723, // Bone Resource
            "DMAP" => 0x025C95B6, // Deformer Map Resource
            "TONE" => 0x0354796C, // Skin Tone Resource
            "ACOS" => 0x0354796D, // Animal Coat Resource
            _ => 0
        };

        return id != 0;
    }
}
