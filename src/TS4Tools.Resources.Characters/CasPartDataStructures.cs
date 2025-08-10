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

using System.Drawing;

namespace TS4Tools.Resources.Characters;

/// <summary>
/// Represents a swatch color entry for a CAS part.
/// Contains RGB color information for texture recoloring.
/// </summary>
public readonly record struct SwatchColor(
    /// <summary>Red component (0-255)</summary>
    byte Red,
    /// <summary>Green component (0-255)</summary>
    byte Green,
    /// <summary>Blue component (0-255)</summary>
    byte Blue,
    /// <summary>Alpha component (0-255)</summary>
    byte Alpha = 255)
{
    /// <summary>
    /// Converts the swatch color to a .NET Color structure.
    /// </summary>
    /// <returns>Color representation of this swatch</returns>
    public Color ToColor() => Color.FromArgb(Alpha, Red, Green, Blue);

    /// <summary>
    /// Creates a SwatchColor from a .NET Color.
    /// </summary>
    /// <param name="color">The color to convert</param>
    /// <returns>SwatchColor representation</returns>
    public static SwatchColor FromColor(Color color) => new(color.R, color.G, color.B, color.A);

    /// <summary>
    /// Gets the color as a 32-bit ARGB value.
    /// </summary>
    /// <returns>ARGB color value</returns>
    public uint ToArgb() => (uint)((Alpha << 24) | (Red << 16) | (Green << 8) | Blue);

    /// <summary>
    /// Creates a SwatchColor from a 32-bit ARGB value.
    /// </summary>
    /// <param name="argb">ARGB color value</param>
    /// <returns>SwatchColor representation</returns>
    public static SwatchColor FromArgb(uint argb) => new(
        (byte)((argb >> 16) & 0xFF),
        (byte)((argb >> 8) & 0xFF),
        (byte)(argb & 0xFF),
        (byte)((argb >> 24) & 0xFF));
}

/// <summary>
/// Level-of-Detail information for a CAS part mesh.
/// Defines different quality levels for rendering optimization.
/// </summary>
public sealed record LodBlock
{
    /// <summary>
    /// Gets the LOD level (0 = highest quality, higher numbers = lower quality).
    /// </summary>
    public byte LodLevel { get; init; }

    /// <summary>
    /// Gets the vertex count for this LOD level.
    /// </summary>
    public uint VertexCount { get; init; }

    /// <summary>
    /// Gets the triangle/face count for this LOD level.
    /// </summary>
    public uint FaceCount { get; init; }

    /// <summary>
    /// Gets the TGI reference to the mesh geometry resource.
    /// </summary>
    public TgiReference? GeometryReference { get; init; }

    /// <summary>
    /// Gets the TGI reference to the vertex buffer resource.
    /// </summary>
    public TgiReference? VertexBufferReference { get; init; }

    /// <summary>
    /// Gets additional metadata for this LOD level.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Initializes a new LOD block with the specified parameters.
    /// </summary>
    /// <param name="lodLevel">LOD quality level</param>
    /// <param name="vertexCount">Number of vertices</param>
    /// <param name="faceCount">Number of faces/triangles</param>
    public LodBlock(byte lodLevel, uint vertexCount, uint faceCount)
    {
        LodLevel = lodLevel;
        VertexCount = vertexCount;
        FaceCount = faceCount;
    }
}

/// <summary>
/// TGI (Type, Group, Instance) reference for linking to other resources.
/// Used to reference meshes, textures, and other assets.
/// </summary>
public sealed record TgiReference
{
    /// <summary>
    /// Gets the resource type identifier.
    /// </summary>
    public uint ResourceType { get; init; }

    /// <summary>
    /// Gets the resource group identifier.
    /// </summary>
    public uint ResourceGroup { get; init; }

    /// <summary>
    /// Gets the resource instance identifier.
    /// </summary>
    public ulong ResourceInstance { get; init; }

    /// <summary>
    /// Initializes a new TGI reference.
    /// </summary>
    /// <param name="resourceType">Resource type</param>
    /// <param name="resourceGroup">Resource group</param>
    /// <param name="resourceInstance">Resource instance</param>
    public TgiReference(uint resourceType, uint resourceGroup, ulong resourceInstance)
    {
        ResourceType = resourceType;
        ResourceGroup = resourceGroup;
        ResourceInstance = resourceInstance;
    }

    /// <summary>
    /// Gets a string representation of this TGI reference.
    /// </summary>
    /// <returns>Formatted TGI string</returns>
    public override string ToString() => $"TGI({ResourceType:X8}-{ResourceGroup:X8}-{ResourceInstance:X16})";
}

/// <summary>
/// Represents a texture slot assignment for a CAS part.
/// Maps texture types to specific TGI references.
/// </summary>
public sealed record TextureSlot
{
    /// <summary>
    /// Gets the slot identifier (diffuse, normal, specular, etc.).
    /// </summary>
    public byte SlotId { get; init; }

    /// <summary>
    /// Gets the TGI reference to the texture resource.
    /// </summary>
    public TgiReference? TextureReference { get; init; }

    /// <summary>
    /// Gets the texture slot type/purpose.
    /// </summary>
    public TextureSlotType SlotType { get; init; }

    /// <summary>
    /// Initializes a new texture slot.
    /// </summary>
    /// <param name="slotId">Slot identifier</param>
    /// <param name="slotType">Slot type/purpose</param>
    /// <param name="textureReference">Texture reference (optional)</param>
    public TextureSlot(byte slotId, TextureSlotType slotType, TgiReference? textureReference = null)
    {
        SlotId = slotId;
        SlotType = slotType;
        TextureReference = textureReference;
    }
}

/// <summary>
/// Types of texture slots supported by CAS parts.
/// </summary>
public enum TextureSlotType : byte
{
    /// <summary>Diffuse/albedo texture (base color)</summary>
    Diffuse = 0,

    /// <summary>Normal map for surface detail</summary>
    Normal = 1,

    /// <summary>Specular map for reflectivity</summary>
    Specular = 2,

    /// <summary>Shadow map for ambient occlusion</summary>
    Shadow = 3,

    /// <summary>Region map for multi-channel effects</summary>
    Region = 4,

    /// <summary>Overlay texture for additional details</summary>
    Overlay = 5,

    /// <summary>Multiplier texture for color modulation</summary>
    Multiplier = 6,

    /// <summary>Emission texture for glowing effects</summary>
    Emission = 7
}

/// <summary>
/// Composition method for combining textures.
/// </summary>
public enum CompositionMethod : byte
{
    /// <summary>Standard texture composition</summary>
    Standard = 0,

    /// <summary>Additive blending</summary>
    Additive = 1,

    /// <summary>Multiplicative blending</summary>
    Multiplicative = 2,

    /// <summary>Overlay blending</summary>
    Overlay = 3,

    /// <summary>Alpha blending</summary>
    AlphaBlend = 4
}
