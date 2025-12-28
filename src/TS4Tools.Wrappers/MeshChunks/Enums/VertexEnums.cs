namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Defines the semantic usage of a vertex element.
/// Source: s4pi Wrappers/MeshChunks/VRTF.cs lines 66-75
/// </summary>
public enum ElementUsage : byte
{
    /// <summary>Vertex position in 3D space.</summary>
    Position = 0,

    /// <summary>Surface normal vector.</summary>
    Normal = 1,

    /// <summary>Texture coordinate (UV mapping).</summary>
    UV = 2,

    /// <summary>Bone indices for skeletal animation.</summary>
    BlendIndex = 3,

    /// <summary>Bone weights for skeletal animation blending.</summary>
    BlendWeight = 4,

    /// <summary>Tangent vector for normal mapping.</summary>
    Tangent = 5,

    /// <summary>Vertex color.</summary>
    Colour = 6
}

/// <summary>
/// Defines the binary format of a vertex element.
/// Source: s4pi Wrappers/MeshChunks/VRTF.cs lines 76-96
/// </summary>
public enum ElementFormat : byte
{
    /// <summary>1 float (4 bytes)</summary>
    Float1 = 0,

    /// <summary>2 floats (8 bytes)</summary>
    Float2 = 1,

    /// <summary>3 floats (12 bytes)</summary>
    Float3 = 2,

    /// <summary>4 floats (16 bytes)</summary>
    Float4 = 3,

    /// <summary>4 unsigned bytes (4 bytes)</summary>
    UByte4 = 4,

    /// <summary>4 unsigned bytes representing BGRA color (4 bytes)</summary>
    ColorUByte4 = 5,

    /// <summary>2 signed shorts (4 bytes)</summary>
    Short2 = 6,

    /// <summary>4 signed shorts (8 bytes)</summary>
    Short4 = 7,

    /// <summary>4 unsigned bytes normalized to [0,1] (4 bytes)</summary>
    UByte4N = 8,

    /// <summary>2 signed shorts normalized to [-1,1] (4 bytes)</summary>
    Short2N = 9,

    /// <summary>4 signed shorts normalized to [-1,1] (8 bytes)</summary>
    Short4N = 10,

    /// <summary>2 unsigned shorts normalized to [0,1] (4 bytes)</summary>
    UShort2N = 11,

    /// <summary>4 unsigned shorts normalized to [0,1] (8 bytes)</summary>
    UShort4N = 12,

    /// <summary>3-component packed decimal format (4 bytes)</summary>
    Dec3N = 13,

    /// <summary>3-component packed unsigned decimal format (4 bytes)</summary>
    UDec3N = 14,

    /// <summary>2 half-precision floats (4 bytes)</summary>
    Float16x2 = 15,

    /// <summary>4 half-precision floats (8 bytes)</summary>
    Float16x4 = 16,

    /// <summary>Special format for drop shadow UV data (8 bytes)</summary>
    Short4DropShadow = 0xFF
}

/// <summary>
/// Utility methods for vertex element formats.
/// Source: s4pi Wrappers/MeshChunks/VRTF.cs lines 97-146
/// </summary>
public static class ElementFormatExtensions
{
    /// <summary>
    /// Returns the number of float values this format represents.
    /// </summary>
    public static int FloatCount(this ElementFormat format) => format switch
    {
        ElementFormat.Float1 => 1,
        ElementFormat.Float2 or ElementFormat.UShort2N or ElementFormat.Short2 => 2,
        ElementFormat.Short4 or ElementFormat.Short4N or ElementFormat.UByte4N
            or ElementFormat.UShort4N or ElementFormat.Float3 => 3,
        ElementFormat.ColorUByte4 or ElementFormat.Float4 or ElementFormat.Short4DropShadow => 4,
        _ => throw new NotSupportedException($"Unsupported element format: {format}")
    };

    /// <summary>
    /// Returns the size in bytes of this format.
    /// </summary>
    public static int ByteSize(this ElementFormat format) => format switch
    {
        ElementFormat.Float1 or ElementFormat.UByte4 or ElementFormat.ColorUByte4
            or ElementFormat.UByte4N or ElementFormat.UShort2N or ElementFormat.Short2 => 4,
        ElementFormat.UShort4N or ElementFormat.Float2 or ElementFormat.Short4
            or ElementFormat.Short4N or ElementFormat.Short4DropShadow => 8,
        ElementFormat.Float3 => 12,
        ElementFormat.Float4 => 16,
        _ => throw new NotSupportedException($"Unsupported element format: {format}")
    };
}
