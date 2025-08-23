using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for material resources (resource type 0x545AC67A).
/// Materials control the visual appearance of 3D objects, including textures, lighting, and surface properties.
/// Also known as SWB (Surface Wetness Buffer) resources.
/// </summary>
public interface IMaterialResource : IResource
{
    /// <summary>
    /// Gets the raw binary data of this material resource.
    /// </summary>
    ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Gets the size of the material data in bytes.
    /// </summary>
    long Size { get; }

    /// <summary>
    /// Gets the material identifier/hash if available.
    /// </summary>
    uint MaterialId { get; }

    /// <summary>
    /// Gets the shader type used by this material.
    /// </summary>
    string ShaderType { get; }

    /// <summary>
    /// Gets whether this material has transparency/alpha channel.
    /// </summary>
    bool HasTransparency { get; }
}
