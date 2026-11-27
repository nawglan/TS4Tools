// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/GeomResource.cs lines 25-75
// (GeometryResource and GeometryResourceHandler, registered for 0x015A1849)

using TS4Tools.Resources;
using TS4Tools.Wrappers.MeshChunks;

namespace TS4Tools.Wrappers;

/// <summary>
/// Factory for standalone GEOM resources (0x015A1849).
/// </summary>
/// <remarks>
/// RCOLResources.txt marks GEOM "N" ("lies! but the stand alone is not a GenericRCOL"): a standalone GEOM
/// is an RCOL container holding exactly one chunk, which legacy <c>GeometryResource</c> re-parses as a
/// GEOM block (GeomResource.cs lines 11-27). This factory does the same on top of
/// <see cref="RcolResource"/>.
/// </remarks>
[ResourceHandler(RcolConstants.Geom)]
public sealed class GeomResourceFactory : IResourceFactory
{
    /// <inheritdoc/>
    public IResource Create(ResourceKey key, ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
            return CreateEmpty(key);

        var resource = new RcolResource(key, data);

        // Source: GeomResource.cs lines 16-17
        if (resource.ChunkCount != 1)
            throw new ResourceFormatException($"Expected one chunk, found {resource.ChunkCount}.");

        // Source: GeomResource.cs lines 19-26 - the chunk is always re-read as a GEOM
        var chunk = resource.Chunks[0];
        if (chunk.Block is not GeomBlock)
        {
            var geom = new GeomBlock(chunk.Block.Serialize().Span);
            resource.SetChunkSilently(0, chunk.TgiBlock, geom);
        }

        return resource;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Source: GeomResource.cs lines 32-47 - a new resource gets version 0, no public chunks, no external
    /// resources and a single empty GEOM chunk keyed 0x015A1849:0:0.
    /// </remarks>
    public IResource CreateEmpty(ResourceKey key)
    {
        var resource = new RcolResource(key, ReadOnlyMemory<byte>.Empty);
        resource.SetHeaderSilently(0, 0, 0);
        resource.AddChunkSilently(new RcolTgiBlock(0, RcolConstants.Geom, 0), new GeomBlock());
        return resource;
    }
}
