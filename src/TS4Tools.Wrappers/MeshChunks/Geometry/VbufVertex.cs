// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/VBUF.cs lines 31-41 (Vertex)

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// A decoded vertex read from (or to be written to) a VBUF according to a VRTF layout.
/// Each component is null when the vertex format does not contain it.
/// Source: VBUF.cs lines 31-41
/// </summary>
public sealed class VbufVertex
{
    /// <summary>Position components.</summary>
    public float[]? Position { get; set; }

    /// <summary>Normal components.</summary>
    public float[]? Normal { get; set; }

    /// <summary>UV sets, one array per UV element in the vertex format.</summary>
    public float[][]? UV { get; set; }

    /// <summary>Blend (bone) indices as raw bytes.</summary>
    public byte[]? BlendIndices { get; set; }

    /// <summary>Blend weights.</summary>
    public float[]? BlendWeights { get; set; }

    /// <summary>Tangent components.</summary>
    public float[]? Tangents { get; set; }

    /// <summary>Colour components.</summary>
    public float[]? Color { get; set; }
}
