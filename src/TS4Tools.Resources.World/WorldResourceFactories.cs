using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.World;

/// <summary>
/// Factory for creating world resources that handle world and scene definitions.
/// </summary>
public sealed class WorldResourceFactory : ResourceFactoryBase<WorldResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldResourceFactory"/> class.
    /// </summary>
    public WorldResourceFactory() : base(new[] { "WORLD", "0x810A102D" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<WorldResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        var key = new ResourceKey(0x810A102D, 0x00000000, 0x0000000000000000);
        var resource = new WorldResource(key, 1);

        if (stream != null)
        {
            await resource.LoadFromStreamAsync(stream);
        }

        return resource;
    }
}

/// <summary>
/// Factory for creating terrain resources that handle terrain data and heightmaps.
/// </summary>
public sealed class TerrainResourceFactory : ResourceFactoryBase<TerrainResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerrainResourceFactory"/> class.
    /// </summary>
    public TerrainResourceFactory() : base(new[] { "TERRAIN", "0xAE39399F" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<TerrainResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        var key = new ResourceKey(0xAE39399F, 0x00000000, 0x0000000000000000);
        var resource = new TerrainResource(key, 1);

        if (stream != null)
        {
            await resource.LoadFromStreamAsync(stream);
        }

        return resource;
    }
}

/// <summary>
/// Factory for creating lot resources that handle lot placement and configuration.
/// </summary>
public sealed class LotResourceFactory : ResourceFactoryBase<LotResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LotResourceFactory"/> class.
    /// </summary>
    public LotResourceFactory() : base(new[] { "LOT", "0x01942E2C" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<LotResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        var key = new ResourceKey(0x01942E2C, 0x00000000, 0x0000000000000000);
        var resource = new LotResource(key, 9);

        if (stream != null)
        {
            await resource.LoadFromStreamAsync(stream);
        }

        return resource;
    }
}

/// <summary>
/// Factory for creating neighborhood resources that handle neighborhood and world metadata.
/// </summary>
public sealed class NeighborhoodResourceFactory : ResourceFactoryBase<NeighborhoodResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NeighborhoodResourceFactory"/> class.
    /// </summary>
    public NeighborhoodResourceFactory() : base(new[] { "NEIGHBORHOOD", "REGION", "0xD65DAFF9", "0xA680EA4B" }, priority: 50)
    {
    }

    /// <inheritdoc/>
    public override async Task<NeighborhoodResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        // Use the world description resource type by default
        var key = new ResourceKey(0xA680EA4B, 0x00000000, 0x0000000000000000);
        var resource = new NeighborhoodResource(key, 1);

        if (stream != null)
        {
            await resource.LoadFromStreamAsync(stream);
        }

        return resource;
    }
}
