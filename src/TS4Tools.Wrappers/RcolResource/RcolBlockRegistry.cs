using TS4Tools.Wrappers.MeshChunks;

namespace TS4Tools.Wrappers;

/// <summary>
/// Registry for RCOL block type factories.
/// Maps resource type IDs and tags to factory functions that create the appropriate block type.
/// Source: Conceptually based on GenericRCOLResourceHandler.RCOLDealer pattern from legacy s4pi.
/// </summary>
public static class RcolBlockRegistry
{
    private static readonly Dictionary<uint, Func<ReadOnlySpan<byte>, RcolBlock>> _factoriesByType = new();
    private static readonly Dictionary<string, Func<ReadOnlySpan<byte>, RcolBlock>> _factoriesByTag = new();

    static RcolBlockRegistry()
    {
        // Register existing MeshChunks blocks
        Register(VrtfBlock.TypeId, "VRTF", data => new VrtfBlock(data));
        Register(VbufBlock.TypeId, "VBUF", data => new VbufBlock(data));
        Register(Vbuf2Block.TypeId, "VBUF", data => new Vbuf2Block(data)); // Shadow variant uses same tag
        Register(IbufBlock.TypeId, "IBUF", data => new IbufBlock(data));
        Register(Ibuf2Block.TypeId, "IBUF", data => new Ibuf2Block(data)); // Shadow variant uses same tag
        Register(SkinBlock.TypeId, "SKIN", data => new SkinBlock(data));
        Register(GeomBlock.TypeId, "GEOM", data => new GeomBlock(data));
        Register(ModlBlock.TypeId, "MODL", data => new ModlBlock(data));
        Register(MlodBlock.TypeId, "MLOD", data => new MlodBlock(data));
        Register(VbsiBlock.TypeId, "VBSI", data => new VbsiBlock(data));

        // New block types
        Register(VpxyBlock.TypeId, "VPXY", data => new VpxyBlock(data));
        Register(MtstBlock.TypeId, "MTST", data => new MtstBlock(data));
        Register(LiteBlock.TypeId, "LITE", data => new LiteBlock(data));
        Register(FtptBlock.TypeId, "FTPT", data => new FtptBlock(data));
        Register(RsltBlock.TypeId, "RSLT", data => new RsltBlock(data));
        Register(MatdBlock.TypeId, "MATD", data => new MatdBlock(data));
    }

    /// <summary>
    /// Registers a block factory for a resource type and optional tag.
    /// </summary>
    /// <param name="resourceType">The resource type ID to register.</param>
    /// <param name="tag">The 4-character tag to register (optional, used for fallback).</param>
    /// <param name="factory">The factory function that creates the block.</param>
    public static void Register(uint resourceType, string tag, Func<ReadOnlySpan<byte>, RcolBlock> factory)
    {
        _factoriesByType[resourceType] = factory;
        // Only register the first factory for a tag (priority to specific resource types)
        if (!_factoriesByTag.ContainsKey(tag))
            _factoriesByTag[tag] = factory;
    }

    /// <summary>
    /// Creates an RCOL block from raw data.
    /// </summary>
    /// <param name="resourceType">The resource type ID of the block.</param>
    /// <param name="data">The raw block data.</param>
    /// <returns>A typed RcolBlock if the type is registered, otherwise an UnknownRcolBlock.</returns>
    public static RcolBlock CreateBlock(uint resourceType, ReadOnlySpan<byte> data)
    {
        // Try by resource type first (most specific)
        if (_factoriesByType.TryGetValue(resourceType, out var factory))
            return factory(data);

        // Try by tag extracted from data (fallback for unknown resource types)
        if (data.Length >= 4)
        {
            string tag = RcolBlock.ExtractTag(data);
            if (_factoriesByTag.TryGetValue(tag, out factory))
                return factory(data);
        }

        // Fallback to unknown block
        return new UnknownRcolBlock(resourceType, data);
    }

    /// <summary>
    /// Checks if a resource type is registered.
    /// </summary>
    public static bool IsRegistered(uint resourceType) => _factoriesByType.ContainsKey(resourceType);

    /// <summary>
    /// Checks if a tag is registered.
    /// </summary>
    public static bool IsTagRegistered(string tag) => _factoriesByTag.ContainsKey(tag);

    /// <summary>
    /// Gets all registered resource type IDs.
    /// </summary>
    public static IEnumerable<uint> RegisteredTypes => _factoriesByType.Keys;

    /// <summary>
    /// Gets all registered tags.
    /// </summary>
    public static IEnumerable<string> RegisteredTags => _factoriesByTag.Keys;
}
