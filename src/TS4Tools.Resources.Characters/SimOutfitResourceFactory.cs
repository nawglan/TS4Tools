using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Characters;

/// <summary>
/// Factory for creating Sim outfit resources with dependency injection support.
/// </summary>
public class SimOutfitResourceFactory : ResourceFactoryBase<ISimOutfitResource>
{
    private readonly ILogger<SimOutfitResourceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimOutfitResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public SimOutfitResourceFactory(ILogger<SimOutfitResourceFactory> logger)
        : base(new[] { "SIMO", "0x025ED6F4" }, priority: 60)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<ISimOutfitResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        try
        {
            _logger.LogDebug("Creating sim outfit resource from stream (length: {Length})", stream?.Length ?? 0);

            var resource = new SimOutfitResource();

            if (stream != null)
            {
                await resource.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully created sim outfit resource with {DataReferenceCount} data references and {SliderCount} sliders",
                resource.DataReferences.Count, resource.BodySliders.Count + resource.FaceSliders.Count);

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sim outfit resource from stream");
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "SIMO" => CharacterResourceTypes.SimOutfitResourceType, // Sim Outfit Resource
            "0X025ED6F4" => CharacterResourceTypes.SimOutfitResourceType, // Hex format support
            _ => 0
        };

        return id != 0;
    }
}
