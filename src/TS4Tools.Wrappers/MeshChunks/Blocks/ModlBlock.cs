using System.Buffers.Binary;

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// MODL (Model) block - contains model definition with LOD entries.
/// This is a simplified implementation that preserves raw data for round-trip.
/// Full parsing can be added later.
/// Resource Type: 0x01661233
/// Source: s4pi Wrappers/MeshChunks/MODL.cs
/// </summary>
public sealed class ModlBlock : RcolBlock
{
    /// <summary>Resource type identifier for MODL.</summary>
    public const uint TypeId = 0x01661233;

    /// <inheritdoc/>
    public override string Tag => "MODL";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version.</summary>
    public uint Version { get; private set; } = 256;

    /// <summary>Number of LOD entries.</summary>
    public int LodEntryCount { get; private set; }

    /// <summary>
    /// Creates an empty MODL block.
    /// </summary>
    public ModlBlock() : base()
    {
    }

    /// <summary>
    /// Creates a MODL block from raw data.
    /// </summary>
    public ModlBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses basic MODL header. Full parsing is not yet implemented.
    /// Source: s4pi Wrappers/MeshChunks/MODL.cs Parse() lines 267-293
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid MODL tag: expected '{Tag}', got '{tag}'");

        // Read version
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);

        // Read LOD entry count
        if (data.Length >= 12)
        {
            LodEntryCount = BinaryPrimitives.ReadInt32LittleEndian(data[8..]);
        }

        // Full parsing not yet implemented - raw data preserved by base class
    }

    /// <summary>
    /// Serializes the MODL block. Returns preserved raw data.
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Return preserved raw data for round-trip
        return RawData;
    }
}
