
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// MLOD (Model LOD) block - contains mesh definitions with buffer references.
/// This is a simplified implementation that preserves raw data for round-trip.
/// Full parsing can be added later.
/// Resource Type: 0x01D10F34
/// Source: s4pi Wrappers/MeshChunks/MLOD.cs
/// </summary>
public sealed class MlodBlock : RcolBlock
{
    /// <summary>Resource type identifier for MLOD.</summary>
    public const uint TypeId = 0x01D10F34;

    /// <inheritdoc/>
    public override string Tag => "MLOD";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 0x00000202).</summary>
    public uint Version { get; private set; } = 0x00000202;

    /// <summary>Number of meshes in this LOD.</summary>
    public int MeshCount { get; private set; }

    /// <summary>
    /// Creates an empty MLOD block.
    /// </summary>
    public MlodBlock() : base()
    {
    }

    /// <summary>
    /// Creates an MLOD block from raw data.
    /// </summary>
    public MlodBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses basic MLOD header. Full mesh parsing is not yet implemented.
    /// Source: s4pi Wrappers/MeshChunks/MLOD.cs Parse() lines 60-68
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid MLOD tag: expected '{Tag}', got '{tag}'");

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);

        // Read mesh count (from the mesh list header)
        if (data.Length >= 12)
        {
            MeshCount = BinaryPrimitives.ReadInt32LittleEndian(data[8..]);
        }

        // Full mesh parsing not yet implemented - raw data preserved by base class
    }

    /// <summary>
    /// Serializes the MLOD block. Returns preserved raw data.
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Return preserved raw data for round-trip
        return RawData;
    }
}
