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

using TS4Tools.Resources.Common;

namespace TS4Tools.Resources.Textures;

/// <summary>
/// Represents texture formats supported by TXTC resources.
/// </summary>
public enum TextureFormat
{
    /// <summary>Unknown or unsupported format.</summary>
    Unknown = 0,

    /// <summary>DXT1 compression format.</summary>
    DXT1 = 1,

    /// <summary>DXT3 compression format.</summary>
    DXT3 = 2,

    /// <summary>DXT5 compression format.</summary>
    DXT5 = 3,

    /// <summary>Uncompressed ARGB format.</summary>
    ARGB = 4,

    /// <summary>RGB format.</summary>
    RGB = 5,

    /// <summary>Luminance (grayscale) format.</summary>
    Luminance = 6,

    /// <summary>Luminance with alpha format.</summary>
    LuminanceAlpha = 7
}

/// <summary>
/// Represents texture mipmapping configuration.
/// </summary>
public enum MipmapLevel
{
    /// <summary>No mipmaps.</summary>
    None = 0,

    /// <summary>Single level (base texture only).</summary>
    Single = 1,

    /// <summary>Full mipmap chain.</summary>
    Full = 2
}

/// <summary>
/// Configuration flags for TXTC resources.
/// </summary>
[Flags]
public enum TxtcFlags
{
    /// <summary>No special flags.</summary>
    None = 0,

    /// <summary>Texture uses external reference.</summary>
    ExternalReference = 1 << 0,

    /// <summary>Texture has embedded data.</summary>
    EmbeddedData = 1 << 1,

    /// <summary>Texture uses compression.</summary>
    Compressed = 1 << 2,

    /// <summary>Texture has alpha channel.</summary>
    HasAlpha = 1 << 3,

    /// <summary>Texture uses cube mapping.</summary>
    CubeMap = 1 << 4,

    /// <summary>Texture uses volume mapping.</summary>
    VolumeMap = 1 << 5,

    /// <summary>Texture is rendered to target.</summary>
    RenderTarget = 1 << 6
}

/// <summary>
/// Represents a texture compositor reference to external texture data.
/// </summary>
public struct TextureReference
{
    /// <summary>Gets or sets the Type-Group-Instance identifier.</summary>
    public TgiReference Tgi { get; set; }

    /// <summary>Gets or sets the texture format.</summary>
    public TextureFormat Format { get; set; }

    /// <summary>Gets or sets the texture width in pixels.</summary>
    public uint Width { get; set; }

    /// <summary>Gets or sets the texture height in pixels.</summary>
    public uint Height { get; set; }

    /// <summary>Gets or sets the mipmap level configuration.</summary>
    public MipmapLevel MipmapLevel { get; set; }

    /// <summary>Initializes a new instance of the <see cref="TextureReference"/> struct.</summary>
    /// <param name="tgi">The TGI reference.</param>
    /// <param name="format">The texture format.</param>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="mipmapLevel">The mipmap level.</param>
    public TextureReference(TgiReference tgi, TextureFormat format, uint width, uint height, MipmapLevel mipmapLevel = MipmapLevel.None)
    {
        Tgi = tgi;
        Format = format;
        Width = width;
        Height = height;
        MipmapLevel = mipmapLevel;
    }
}

/// <summary>
/// Represents embedded texture data within a TXTC resource.
/// </summary>
public struct EmbeddedTextureData
{
    /// <summary>Gets or sets the texture format.</summary>
    public TextureFormat Format { get; set; }

    /// <summary>Gets or sets the texture width in pixels.</summary>
    public uint Width { get; set; }

    /// <summary>Gets or sets the texture height in pixels.</summary>
    public uint Height { get; set; }

    /// <summary>Gets or sets the raw texture data.</summary>
    public byte[] Data { get; set; }

    /// <summary>Gets or sets the data size in bytes.</summary>
    public uint DataSize { get; set; }

    /// <summary>Initializes a new instance of the <see cref="EmbeddedTextureData"/> struct.</summary>
    /// <param name="format">The texture format.</param>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="data">The raw texture data.</param>
    public EmbeddedTextureData(TextureFormat format, uint width, uint height, byte[] data)
    {
        Format = format;
        Width = width;
        Height = height;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        DataSize = (uint)data.Length;
    }
}

/// <summary>
/// Represents texture composition parameters.
/// </summary>
public struct CompositionParameters
{
    /// <summary>Gets or sets the blend mode.</summary>
    public uint BlendMode { get; set; }

    /// <summary>Gets or sets the opacity value (0-255).</summary>
    public byte Opacity { get; set; }

    /// <summary>Gets or sets the U-axis tiling factor.</summary>
    public float TileU { get; set; }

    /// <summary>Gets or sets the V-axis tiling factor.</summary>
    public float TileV { get; set; }

    /// <summary>Gets or sets the U-axis offset.</summary>
    public float OffsetU { get; set; }

    /// <summary>Gets or sets the V-axis offset.</summary>
    public float OffsetV { get; set; }

    /// <summary>Gets or sets the rotation angle in radians.</summary>
    public float Rotation { get; set; }

    /// <summary>Initializes a new instance with default composition parameters.</summary>
    public static CompositionParameters Default => new()
    {
        BlendMode = 0,
        Opacity = 255,
        TileU = 1.0f,
        TileV = 1.0f,
        OffsetU = 0.0f,
        OffsetV = 0.0f,
        Rotation = 0.0f
    };
}

/// <summary>
/// Represents a texture layer within a TXTC composition.
/// </summary>
public struct TextureLayer
{
    /// <summary>Gets or sets the layer index.</summary>
    public uint Index { get; set; }

    /// <summary>Gets or sets the texture reference for this layer.</summary>
    public TextureReference? Reference { get; set; }

    /// <summary>Gets or sets the embedded texture data for this layer.</summary>
    public EmbeddedTextureData? EmbeddedData { get; set; }

    /// <summary>Gets or sets the composition parameters.</summary>
    public CompositionParameters CompositionParams { get; set; }

    /// <summary>Gets or sets additional layer-specific flags.</summary>
    public uint LayerFlags { get; set; }

    /// <summary>Gets a value indicating whether this layer uses external reference.</summary>
    public readonly bool UsesExternalReference => Reference.HasValue;

    /// <summary>Gets a value indicating whether this layer has embedded data.</summary>
    public readonly bool HasEmbeddedData => EmbeddedData.HasValue;
}
