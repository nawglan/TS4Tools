
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// GEOM (Geometry) block - contains vertex and face data for a mesh.
/// This is a simplified implementation that preserves raw data for round-trip.
/// Full parsing can be added later.
/// Resource Type: 0x015A1849
/// Source: s4pi Wrappers/MeshChunks/GEOM.cs
/// </summary>
public sealed class GeomBlock : RcolBlock
{
    /// <summary>Resource type identifier for GEOM.</summary>
    public const uint TypeId = 0x015A1849;

    /// <inheritdoc/>
    public override string Tag => "GEOM";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (0x00000005 or 0x0000000C).</summary>
    public uint Version { get; private set; }

    /// <summary>Number of vertices in this geometry.</summary>
    public int VertexCount { get; private set; }

    /// <summary>
    /// Creates an empty GEOM block.
    /// </summary>
    public GeomBlock() : base()
    {
    }

    /// <summary>
    /// Creates a GEOM block from raw data.
    /// </summary>
    public GeomBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses basic GEOM header. Full vertex/face parsing is not yet implemented.
    /// Source: s4pi Wrappers/MeshChunks/GEOM.cs
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid GEOM tag: expected '{Tag}', got '{tag}'");

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);

        // Skip TGI offset (4 bytes) and TGI size (4 bytes) and shader type (4 bytes)
        // Then read vertex count
        int pos = 20; // After tag(4) + version(4) + tgiOffset(4) + tgiSize(4) + shaderType(4)

        // Check for shader data
        uint shaderType = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        if (shaderType != 0 && data.Length > pos + 4)
        {
            // Skip MTNF data size and MTNF data
            uint mtnfSize = BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
            pos = 20 + (int)mtnfSize;
        }

        // Skip mergeGroup(4) + sortOrder(4) then read vertex count
        if (data.Length >= pos + 12)
        {
            VertexCount = BinaryPrimitives.ReadInt32LittleEndian(data[(pos + 8)..]);
        }

        // Full parsing not yet implemented - raw data preserved by base class
    }

    /// <summary>
    /// Serializes the GEOM block. Returns preserved raw data.
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Return preserved raw data for round-trip
        return RawData;
    }
}
