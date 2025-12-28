using System.Buffers.Binary;

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// VBSI (Vertex Buffer Swizzle Info) block - contains swizzle data for vertex buffers.
/// This is a simplified implementation that preserves raw data for round-trip.
/// Resource Type: 0x01D10F3B
/// Source: s4pi Wrappers/MeshChunks/VBSI.cs
/// </summary>
public sealed class VbsiBlock : RcolBlock
{
    /// <summary>Resource type identifier for VBSI.</summary>
    public const uint TypeId = 0x01D10F3B;

    /// <inheritdoc/>
    public override string Tag => "VBSI";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version.</summary>
    public uint Version { get; private set; }

    /// <summary>
    /// Creates an empty VBSI block.
    /// </summary>
    public VbsiBlock() : base()
    {
    }

    /// <summary>
    /// Creates a VBSI block from raw data.
    /// </summary>
    public VbsiBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses basic VBSI header. Full parsing is not yet implemented.
    /// Source: s4pi Wrappers/MeshChunks/VBSI.cs
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid VBSI tag: expected '{Tag}', got '{tag}'");

        // Read version
        if (data.Length >= 8)
        {
            Version = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        }

        // Full parsing not yet implemented - raw data preserved by base class
    }

    /// <summary>
    /// Serializes the VBSI block. Returns preserved raw data.
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Return preserved raw data for round-trip
        return RawData;
    }
}
