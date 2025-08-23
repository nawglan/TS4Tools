using Microsoft.Extensions.Logging;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Animation;

namespace TS4Tools.Resources.Animation;

/// <summary>
/// Factory for creating clip header resources (BC4A5044 - CLHD) with dependency injection support.
/// </summary>
public class ClipHeaderResourceFactory : ResourceFactoryBase<IClipHeaderResource>
{
    private readonly ILogger<ClipHeaderResourceFactory> _logger;
    private readonly ILogger<ClipHeaderResource>? _resourceLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipHeaderResourceFactory"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <param name="resourceLogger">Optional logger specifically for ClipHeaderResource instances</param>
    public ClipHeaderResourceFactory(
        ILogger<ClipHeaderResourceFactory> logger,
        ILogger<ClipHeaderResource>? resourceLogger = null)
        : base(new[] { "CLHD", "0xBC4A5044" }, priority: 50)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceLogger = resourceLogger;
    }

    /// <inheritdoc />
    public override async Task<IClipHeaderResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        try
        {
            _logger.LogDebug("Creating clip header resource from stream (length: {Length})", stream?.Length ?? 0);

            var resource = stream != null 
                ? new ClipHeaderResource(stream) 
                : new ClipHeaderResource();

            _logger.LogInformation("Successfully created clip header resource with version {Version}",
                resource.Version);

            return await Task.FromResult(resource).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create clip header resource from stream");
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        id = resourceType.ToUpperInvariant() switch
        {
            "CLHD" => 0xBC4A5044, // Clip Header Resource (TS4)
            "0XBC4A5044" => 0xBC4A5044, // Direct hex format
            _ => 0
        };

        return id != 0;
    }
}
