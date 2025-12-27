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

    /// <summary>
    /// All standalone RCOL resource type IDs (Y=yes in RCOLResources.txt).
    /// </summary>
    public static readonly uint[] StandaloneTypes =
    [
        Modl,    // 0x01661233
        Matd,    // 0x01D0E75D
        Mlod,    // 0x01D10F34
        Mtst,    // 0x02019972
        Tree,    // 0x021D7E8C
        TkMk,    // 0x033260E3
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
        _ => $"0x{resourceType:X8}"
    };
}
