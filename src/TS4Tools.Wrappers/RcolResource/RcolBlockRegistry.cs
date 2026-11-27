using TS4Tools.Wrappers.JazzChunks;
using TS4Tools.Wrappers.MeshChunks;

namespace TS4Tools.Wrappers;

/// <summary>
/// Registry for RCOL block type factories.
/// Maps resource type IDs and tags to factory functions that create the appropriate block type.
/// Source: GenericRCOLResource.cs lines 745-870 (GenericRCOLResourceHandler: typeRegistry/tagRegistry built by
/// reflecting over every ARCOLBlock subclass, RCOLDealer, RCOLTypesForTag). Legacy discovers blocks from all
/// loaded wrapper assemblies (MeshChunks, s4piRCOLChunks, AnimationResources, JazzResource); here they are
/// registered explicitly.
/// </summary>
public static class RcolBlockRegistry
{
    private static readonly Dictionary<uint, Func<ReadOnlySpan<byte>, RcolBlock>> _factoriesByType = new();
    private static readonly Dictionary<string, Func<ReadOnlySpan<byte>, RcolBlock>> _factoriesByTag = new();
    private static readonly Dictionary<string, uint> _typesByTag = new();

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

        // Bone/slot adjustment block (note: has no tag in binary data - identified by resource type only)
        // BondBlock = SlotAdjust, uses "BOND" as a registration tag but binary has no tag
        Register(BondBlock.TypeId, "BOND", data => new BondBlock(data));

        // TrackMask (AnimationResources/TrackMask.cs)
        Register(TrackMaskBlock.TypeId, "TkMk", data => new TrackMaskBlock(data));

        // Jazz state machine chunks (JazzResource/JazzResource.cs): legacy registers these through the same
        // reflection scan as every other ARCOLBlock.
        Register(JazzStateMachineBlock.TypeId, "S_SM", data => new JazzStateMachineBlock(data));
        Register(JazzStateBlock.TypeId, "S_St", data => new JazzStateBlock(data));
        Register(JazzDecisionGraphBlock.TypeId, "S_DG", data => new JazzDecisionGraphBlock(data));
        Register(JazzActorDefinitionBlock.TypeId, "S_AD", data => new JazzActorDefinitionBlock(data));
        Register(JazzParameterDefinitionBlock.TypeId, "S_PD", data => new JazzParameterDefinitionBlock(data));
        Register(JazzPlayAnimationNodeBlock.TypeId, "Play", data => new JazzPlayAnimationNodeBlock(data));
        Register(JazzRandomNodeBlock.TypeId, "Rand", data => new JazzRandomNodeBlock(data));
        Register(JazzSelectOnParameterNodeBlock.TypeId, "SoPn", data => new JazzSelectOnParameterNodeBlock(data));
        Register(JazzSelectOnDestinationNodeBlock.TypeId, "DG00", data => new JazzSelectOnDestinationNodeBlock(data));
        Register(JazzNextStateNodeBlock.TypeId, "SNSN", data => new JazzNextStateNodeBlock(data));
        Register(JazzCreatePropNodeBlock.TypeId, "Prop", data => new JazzCreatePropNodeBlock(data));
        Register(JazzActorOperationNodeBlock.TypeId, "AcOp", data => new JazzActorOperationNodeBlock(data));
        Register(JazzStopAnimationNodeBlock.TypeId, "Stop", data => new JazzStopAnimationNodeBlock(data));
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
        // Source: GenericRCOLResource.cs line 778 (tagRegistry keeps the first type per tag)
        if (!_factoriesByTag.ContainsKey(tag))
        {
            _factoriesByTag[tag] = factory;
            _typesByTag[tag] = resourceType;
        }
    }

    /// <summary>
    /// Gets the resource type registered for a tag, or null when the tag is not registered.
    /// Source: GenericRCOLResourceHandler.RCOLTypesForTag (GenericRCOLResource.cs lines 877-885)
    /// </summary>
    public static uint? GetTypeForTag(string tag) =>
        _typesByTag.TryGetValue(tag, out uint type) ? type : null;

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
