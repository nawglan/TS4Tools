namespace TS4Tools.Wrappers;

/// <summary>
/// Constants for RCOL resource types.
/// Source: RCOLResources.txt from legacy s4pi Wrappers/GenericRCOLResource/
/// </summary>
public static class RcolConstants
{
    /// <summary>
    /// GEOM - Body Geometry (note: standalone GEOM is not a GenericRCOL per legacy comments)
    /// </summary>
    public const uint Geom = 0x015A1849;

    /// <summary>
    /// MODL - Object Geometry
    /// </summary>
    public const uint Modl = 0x01661233;

    /// <summary>
    /// VBUF - Vertex Buffer
    /// </summary>
    public const uint Vbuf = 0x01D0E6FB;

    /// <summary>
    /// IBUF - Index Buffer
    /// </summary>
    public const uint Ibuf = 0x01D0E70F;

    /// <summary>
    /// VRTF - Vertex Format
    /// </summary>
    public const uint Vrtf = 0x01D0E723;

    /// <summary>
    /// MATD - Material Definition
    /// </summary>
    public const uint Matd = 0x01D0E75D;

    /// <summary>
    /// SKIN - Mesh Skin
    /// </summary>
    public const uint Skin = 0x01D0E76B;

    /// <summary>
    /// MLOD - Object Geometry LODs
    /// </summary>
    public const uint Mlod = 0x01D10F34;

    /// <summary>
    /// MTST - Material Set
    /// </summary>
    public const uint Mtst = 0x02019972;

    /// <summary>
    /// TREE
    /// </summary>
    public const uint Tree = 0x021D7E8C;

    /// <summary>
    /// VBUF (shadow) - Vertex Buffer for shadow meshes
    /// </summary>
    public const uint VbufShadow = 0x0229684B;

    /// <summary>
    /// IBUF (shadow) - Index Buffer for shadow meshes
    /// </summary>
    public const uint IbufShadow = 0x0229684F;

    /// <summary>
    /// TkMk
    /// </summary>
    public const uint TkMk = 0x033260E3;

    /// <summary>
    /// Slot Adjusts (no specific tag, uses "*")
    /// </summary>
    public const uint SlotAdjusts = 0x0355E0A6;

    /// <summary>
    /// LITE - Light
    /// </summary>
    public const uint Lite = 0x03B4C61D;

    /// <summary>
    /// ANIM - Animation
    /// </summary>
    public const uint Anim = 0x63A33EA7;

    /// <summary>
    /// VPXY - Model Links
    /// </summary>
    public const uint Vpxy = 0x736884F1;

    /// <summary>
    /// RSLT - Slot Definition
    /// </summary>
    public const uint Rslt = 0xD3044521;

    /// <summary>
    /// FTPT - Model Footprint
    /// </summary>
    public const uint Ftpt = 0xD382BF57;

    // Jazz State Machine chunk types (from JazzResource.cs)
    // These are RCOL blocks used in animation state machines

    /// <summary>
    /// S_SM - Jazz State Machine
    /// </summary>
    public const uint JazzStateMachine = 0x02D5DF13;

    /// <summary>
    /// S_St - Jazz State
    /// </summary>
    public const uint JazzState = 0x02EEDAFE;

    /// <summary>
    /// S_DG - Jazz Decision Graph
    /// </summary>
    public const uint JazzDecisionGraph = 0x02EEDB18;

    /// <summary>
    /// S_AD - Jazz Actor Definition
    /// </summary>
    public const uint JazzActorDefinition = 0x02EEDB2F;

    /// <summary>
    /// S_PD - Jazz Parameter Definition
    /// </summary>
    public const uint JazzParameterDefinition = 0x02EEDB46;

    /// <summary>
    /// Play - Jazz Play Animation Node
    /// </summary>
    public const uint JazzPlayAnimationNode = 0x02EEDB5F;

    /// <summary>
    /// Rand - Jazz Random Node
    /// </summary>
    public const uint JazzRandomNode = 0x02EEDB70;

    /// <summary>
    /// SoPn - Jazz Select On Parameter Node
    /// </summary>
    public const uint JazzSelectOnParameterNode = 0x02EEDB92;

    /// <summary>
    /// DG00 - Jazz Select On Destination Node
    /// </summary>
    public const uint JazzSelectOnDestinationNode = 0x02EEDBA5;

    /// <summary>
    /// SNSN - Jazz Next State Node
    /// </summary>
    public const uint JazzNextStateNode = 0x02EEEBDC;

    /// <summary>
    /// Prop - Jazz Create Prop Node
    /// </summary>
    public const uint JazzCreatePropNode = 0x02EEEBDD;

    /// <summary>
    /// AcOp - Jazz Actor Operation Node
    /// </summary>
    public const uint JazzActorOperationNode = 0x02EEEBDE;

    /// <summary>
    /// Stop - Jazz Stop Animation Node
    /// </summary>
    public const uint JazzStopAnimationNode = 0x0344D438;

    /// <summary>
    /// All standalone RCOL resource type IDs (Y=yes in RCOLResources.txt).
    /// </summary>
    public static readonly uint[] StandaloneTypes =
    [
        Modl,    // 0x01661233
        Matd,    // 0x01D0E75D
        Mlod,    // 0x01D10F34
        Mtst,    // 0x02019972
        JazzStateMachine, // 0x02D5DF13 - S_SM
        Tree,    // 0x021D7E8C
        TkMk,    // 0x033260E3
        JazzStopAnimationNode, // 0x0344D438 - Stop
        SlotAdjusts, // 0x0355E0A6
        Lite,    // 0x03B4C61D
        Anim,    // 0x63A33EA7
        Vpxy,    // 0x736884F1
        Rslt,    // 0xD3044521
        Ftpt,    // 0xD382BF57
    ];

    /// <summary>
    /// Gets a human-readable name for an RCOL resource type.
    /// </summary>
    public static string GetTypeName(uint resourceType) => resourceType switch
    {
        Geom => "GEOM",
        Modl => "MODL",
        Vbuf => "VBUF",
        Ibuf => "IBUF",
        Vrtf => "VRTF",
        Matd => "MATD",
        Skin => "SKIN",
        Mlod => "MLOD",
        Mtst => "MTST",
        Tree => "TREE",
        VbufShadow => "VBUF (shadow)",
        IbufShadow => "IBUF (shadow)",
        TkMk => "TkMk",
        SlotAdjusts => "Slot Adjusts",
        Lite => "LITE",
        Anim => "ANIM",
        Vpxy => "VPXY",
        Rslt => "RSLT",
        Ftpt => "FTPT",
        // Jazz State Machine types
        JazzStateMachine => "S_SM (Jazz State Machine)",
        JazzState => "S_St (Jazz State)",
        JazzDecisionGraph => "S_DG (Jazz Decision Graph)",
        JazzActorDefinition => "S_AD (Jazz Actor Definition)",
        JazzParameterDefinition => "S_PD (Jazz Parameter Definition)",
        JazzPlayAnimationNode => "Play (Jazz Play Animation)",
        JazzRandomNode => "Rand (Jazz Random)",
        JazzSelectOnParameterNode => "SoPn (Jazz Select On Parameter)",
        JazzSelectOnDestinationNode => "DG00 (Jazz Select On Destination)",
        JazzNextStateNode => "SNSN (Jazz Next State)",
        JazzCreatePropNode => "Prop (Jazz Create Prop)",
        JazzActorOperationNode => "AcOp (Jazz Actor Operation)",
        JazzStopAnimationNode => "Stop (Jazz Stop Animation)",
        _ => $"0x{resourceType:X8}"
    };
}
