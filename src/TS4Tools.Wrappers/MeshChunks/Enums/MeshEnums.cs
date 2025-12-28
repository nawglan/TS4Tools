namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Flags for vertex buffer format.
/// Source: s4pi Wrappers/MeshChunks/VBUF.cs lines 77-84
/// </summary>
[Flags]
public enum VbufFormat : uint
{
    /// <summary>No special format flags.</summary>
    None = 0x0,

    /// <summary>Buffer is dynamic (can be modified at runtime).</summary>
    Dynamic = 0x1,

    /// <summary>Vertex data uses delta encoding.</summary>
    DifferencedVertices = 0x2,

    /// <summary>Vertex data is collapsed/compressed.</summary>
    Collapsed = 0x4
}

/// <summary>
/// Flags for index buffer format.
/// Source: s4pi Wrappers/MeshChunks/IBUF.cs lines 134-140
/// </summary>
[Flags]
public enum IbufFormat : uint
{
    /// <summary>No special format flags.</summary>
    None = 0x0,

    /// <summary>Index data uses delta encoding.</summary>
    DifferencedIndices = 0x1,

    /// <summary>Uses 32-bit indices instead of 16-bit.</summary>
    Uses32BitIndices = 0x2,

    /// <summary>Buffer is a display list.</summary>
    IsDisplayList = 0x4
}

/// <summary>
/// Level of detail identifier for model LOD entries.
/// Source: s4pi Wrappers/MeshChunks/MODL.cs lines 36-44
/// </summary>
public enum LodId : uint
{
    /// <summary>Highest detail level.</summary>
    HighDetail = 0x00000000,

    /// <summary>Medium detail level.</summary>
    MediumDetail = 0x00000001,

    /// <summary>Lowest detail level.</summary>
    LowDetail = 0x00000002,

    /// <summary>High detail shadow caster.</summary>
    HighDetailShadow = 0x00010000,

    /// <summary>Medium detail shadow caster.</summary>
    MediumDetailShadow = 0x00010001,

    /// <summary>Low detail shadow caster.</summary>
    LowDetailShadow = 0x00010002
}

/// <summary>
/// Flags for LOD entry properties.
/// Source: s4pi Wrappers/MeshChunks/MODL.cs lines 30-35
/// </summary>
[Flags]
public enum LodInfo : uint
{
    /// <summary>No special flags.</summary>
    None = 0x0,

    /// <summary>LOD is a portal.</summary>
    Portal = 0x00000001,

    /// <summary>LOD is a door.</summary>
    Door = 0x00000002
}

/// <summary>
/// Primitive type for mesh rendering.
/// Source: s4pi Wrappers/MeshChunks/MLOD.cs lines 696-707
/// </summary>
public enum ModelPrimitiveType : uint
{
    /// <summary>Vertices rendered as individual points.</summary>
    PointList = 0,

    /// <summary>Vertices rendered as line pairs.</summary>
    LineList = 1,

    /// <summary>Vertices rendered as connected line strip.</summary>
    LineStrip = 2,

    /// <summary>Vertices rendered as triangles (3 vertices per triangle).</summary>
    TriangleList = 3,

    /// <summary>Vertices rendered as triangle fan.</summary>
    TriangleFan = 4,

    /// <summary>Vertices rendered as triangle strip.</summary>
    TriangleStrip = 5,

    /// <summary>Vertices rendered as rectangles.</summary>
    RectList = 6,

    /// <summary>Vertices rendered as quads (4 vertices per quad).</summary>
    QuadList = 7,

    /// <summary>Display list rendering.</summary>
    DisplayList = 8
}

/// <summary>
/// Flags for mesh properties.
/// Source: s4pi Wrappers/MeshChunks/MLOD.cs lines 685-695
/// </summary>
[Flags]
public enum MeshOption : uint
{
    /// <summary>No special flags.</summary>
    None = 0x0,

    /// <summary>Mesh is a basin interior.</summary>
    BasinInterior = 0x00000001,

    /// <summary>HD exterior lit mesh.</summary>
    HdExteriorLit = 0x00000002,

    /// <summary>Portal side mesh.</summary>
    PortalSide = 0x00000004,

    /// <summary>Drop shadow mesh.</summary>
    DropShadow = 0x00000008,

    /// <summary>Shadow caster mesh.</summary>
    ShadowCaster = 0x00000010,

    /// <summary>Foundation mesh.</summary>
    Foundation = 0x00000020,

    /// <summary>Pickable mesh.</summary>
    Pickable = 0x00000040
}

/// <summary>
/// Utility methods for primitive types.
/// Source: s4pi Wrappers/MeshChunks/IBUF.cs lines 252-261
/// </summary>
public static class ModelPrimitiveTypeExtensions
{
    /// <summary>
    /// Returns the number of indices per primitive for this type.
    /// </summary>
    public static int IndexCountPerPrimitive(this ModelPrimitiveType type) => type switch
    {
        ModelPrimitiveType.TriangleList => 3,
        ModelPrimitiveType.QuadList => 4,
        ModelPrimitiveType.LineList => 2,
        ModelPrimitiveType.PointList => 1,
        _ => throw new NotSupportedException($"Unsupported primitive type: {type}")
    };
}
