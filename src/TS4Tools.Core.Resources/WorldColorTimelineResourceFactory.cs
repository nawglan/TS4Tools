using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Factory for creating WorldColorTimelineResource instances.
/// </summary>
/// <remarks>
/// Handles creation of world color timeline resources that manage environmental lighting data
/// including day/night cycles, weather effects, and point-of-interest specific lighting.
/// Supports legacy format versions 13 and 14 with byte-perfect compatibility.
/// </remarks>
internal sealed class WorldColorTimelineResourceFactory : ResourceFactoryBase<IWorldColorTimelineResource>
{
    /// <summary>
    /// Initializes a new instance of the WorldColorTimelineResourceFactory class.
    /// </summary>
    public WorldColorTimelineResourceFactory() : base(new[] { "0x19301120" }, priority: 100)
    {
    }

    /// <inheritdoc />
    public override async Task<IWorldColorTimelineResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        // For async consistency, but no actual async work needed for this resource
        await Task.CompletedTask;

        return stream != null ? new WorldColorTimelineResource(stream) : new WorldColorTimelineResource();
    }

    /// <inheritdoc />
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        // Handle WorldColorTimeline specific mappings
        id = resourceType.ToUpperInvariant() switch
        {
            "WORLDCOLORTIMELINE" => 0x19301120,
            "0X19301120" => 0x19301120,
            _ => 0
        };

        if (id != 0)
            return true;

        // Fall back to base implementation
        return base.TryGetResourceTypeId(resourceType, out id);
    }
}
