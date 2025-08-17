/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

namespace TS4Tools.Resources.Geometry;

/// <summary>
/// Shader types used in The Sims 4 geometry resources.
/// </summary>
public enum ShaderType
{
    /// <summary>
    /// No shader applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Standard vertex lit shader.
    /// </summary>
    VertexLit = unchecked((int)0x9DD6A9F9),

    /// <summary>
    /// Shadow map shader.
    /// </summary>
    ShadowMap = unchecked((int)0x57A47B7C),

    /// <summary>
    /// Drop shadow shader.
    /// </summary>
    DropShadow = unchecked((int)0x8D23AC8E),

    /// <summary>
    /// Skin shader for character models.
    /// </summary>
    Skin = unchecked((int)0x4ECD93F0),

    /// <summary>
    /// Hair shader.
    /// </summary>
    Hair = unchecked((int)0x1A3B0A87),

    /// <summary>
    /// Environment shader.
    /// </summary>
    Environment = unchecked((int)0x2E94596C),

    /// <summary>
    /// Glass shader for transparent materials.
    /// </summary>
    Glass = unchecked((int)0x8A8F1436),

    /// <summary>
    /// Water shader.
    /// </summary>
    Water = unchecked((int)0x3F583B6D)
}

/// <summary>
/// Usage types for vertex format elements in The Sims 4 geometry.
/// </summary>
public enum UsageType
{
    /// <summary>
    /// None/Unknown usage.
    /// </summary>
    None = 0,

    /// <summary>
    /// Vertex position data.
    /// </summary>
    Position = 0x01,

    /// <summary>
    /// Vertex normal vectors.
    /// </summary>
    Normal = 0x02,

    /// <summary>
    /// UV texture coordinates.
    /// </summary>
    UV = 0x03,

    /// <summary>
    /// Bone assignment indices for skeletal animation.
    /// </summary>
    BoneAssignment = 0x04,

    /// <summary>
    /// Bone weights for skeletal animation.
    /// </summary>
    Weights = 0x05,

    /// <summary>
    /// Tangent vectors for normal mapping.
    /// </summary>
    Tangent = 0x06,

    /// <summary>
    /// Vertex color data.
    /// </summary>
    Color = 0x07,

    /// <summary>
    /// Blend indices for multi-texture blending.
    /// </summary>
    BlendIndices = 0x08,

    /// <summary>
    /// Blend weights for multi-texture blending.
    /// </summary>
    BlendWeights = 0x09
}

/// <summary>
/// Data types for vertex format elements.
/// </summary>
public enum DataType
{
    /// <summary>
    /// None/Unknown type.
    /// </summary>
    None = 0,

    /// <summary>
    /// Single precision floating point.
    /// </summary>
    Float = 0x01,

    /// <summary>
    /// Two component floating point vector.
    /// </summary>
    Float2 = 0x02,

    /// <summary>
    /// Three component floating point vector.
    /// </summary>
    Float3 = 0x03,

    /// <summary>
    /// Four component floating point vector.
    /// </summary>
    Float4 = 0x04,

    /// <summary>
    /// Unsigned byte value.
    /// </summary>
    UByte = 0x05,

    /// <summary>
    /// Four component unsigned byte vector.
    /// </summary>
    UByte4 = 0x06,

    /// <summary>
    /// Signed byte value.
    /// </summary>
    Byte = 0x07,

    /// <summary>
    /// Four component signed byte vector.
    /// </summary>
    Byte4 = 0x08,

    /// <summary>
    /// Unsigned short value.
    /// </summary>
    UShort = 0x09,

    /// <summary>
    /// Four component unsigned short vector.
    /// </summary>
    UShort4 = 0x0A,

    /// <summary>
    /// Signed short value.
    /// </summary>
    Short = 0x0B,

    /// <summary>
    /// Four component signed short vector.
    /// </summary>
    Short4 = 0x0C
}

/// <summary>
/// Represents a vertex format element definition in The Sims 4 geometry.
/// </summary>
public readonly record struct VertexFormat
{
    /// <summary>
    /// The usage type of this vertex element.
    /// </summary>
    public UsageType Usage { get; init; }

    /// <summary>
    /// The data type of this vertex element.
    /// </summary>
    public DataType DataType { get; init; }

    /// <summary>
    /// The sub-usage index for elements that can have multiple instances.
    /// </summary>
    public byte SubUsage { get; init; }

    /// <summary>
    /// Reserved padding bytes.
    /// </summary>
    public byte Reserved { get; init; }

    /// <summary>
    /// Initializes a new vertex format element.
    /// </summary>
    /// <param name="usage">The usage type.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="subUsage">The sub-usage index.</param>
    /// <param name="reserved">Reserved padding.</param>
    public VertexFormat(UsageType usage, DataType dataType, byte subUsage = 0, byte reserved = 0)
    {
        Usage = usage;
        DataType = dataType;
        SubUsage = subUsage;
        Reserved = reserved;
    }

    /// <summary>
    /// Gets the size in bytes of this vertex element.
    /// </summary>
#pragma warning disable CA1024 // Use properties where appropriate - Method is preferred to avoid exceptions
    public readonly int GetElementSize() => DataType switch
    {
        DataType.Float => 4,
        DataType.Float2 => 8,
        DataType.Float3 => 12,
        DataType.Float4 => 16,
        DataType.UByte => 1,
        DataType.UByte4 => 4,
        DataType.Byte => 1,
        DataType.Byte4 => 4,
        DataType.UShort => 2,
        DataType.UShort4 => 8,
        DataType.Short => 2,
        DataType.Short4 => 8,
        _ => 0 // Return 0 for unknown types instead of throwing
    };
#pragma warning restore CA1024
}

/// <summary>
/// Represents a triangle face in The Sims 4 geometry.
/// </summary>
public readonly record struct Face
{
    /// <summary>
    /// First vertex index.
    /// </summary>
    public ushort A { get; init; }

    /// <summary>
    /// Second vertex index.
    /// </summary>
    public ushort B { get; init; }

    /// <summary>
    /// Third vertex index.
    /// </summary>
    public ushort C { get; init; }

    /// <summary>
    /// Initializes a new triangle face.
    /// </summary>
    /// <param name="a">First vertex index.</param>
    /// <param name="b">Second vertex index.</param>
    /// <param name="c">Third vertex index.</param>
    public Face(ushort a, ushort b, ushort c)
    {
        A = a;
        B = b;
        C = c;
    }
}

/// <summary>
/// Represents UV stitching information for seamless texture mapping.
/// </summary>
public readonly record struct UVStitch
{
    /// <summary>
    /// First vertex index to stitch.
    /// </summary>
    public uint VertexA { get; init; }

    /// <summary>
    /// Second vertex index to stitch.
    /// </summary>
    public uint VertexB { get; init; }

    /// <summary>
    /// Initializes a new UV stitch.
    /// </summary>
    /// <param name="vertexA">First vertex index.</param>
    /// <param name="vertexB">Second vertex index.</param>
    public UVStitch(uint vertexA, uint vertexB)
    {
        VertexA = vertexA;
        VertexB = vertexB;
    }
}

/// <summary>
/// Represents seam stitching information for mesh optimization.
/// </summary>
public readonly record struct SeamStitch
{
    /// <summary>
    /// First vertex index to stitch.
    /// </summary>
    public uint VertexA { get; init; }

    /// <summary>
    /// Second vertex index to stitch.
    /// </summary>
    public uint VertexB { get; init; }

    /// <summary>
    /// Initializes a new seam stitch.
    /// </summary>
    /// <param name="vertexA">First vertex index.</param>
    /// <param name="vertexB">Second vertex index.</param>
    public SeamStitch(uint vertexA, uint vertexB)
    {
        VertexA = vertexA;
        VertexB = vertexB;
    }
}

/// <summary>
/// Represents a slot ray intersection for object placement.
/// </summary>
public readonly record struct SlotrayIntersection
{
    /// <summary>
    /// Intersection position.
    /// </summary>
    public uint Position { get; init; }

    /// <summary>
    /// Normal vector at intersection.
    /// </summary>
    public uint Normal { get; init; }

    /// <summary>
    /// Initializes a new slot ray intersection.
    /// </summary>
    /// <param name="position">Intersection position.</param>
    /// <param name="normal">Normal vector.</param>
    public SlotrayIntersection(uint position, uint normal)
    {
        Position = position;
        Normal = normal;
    }
}
