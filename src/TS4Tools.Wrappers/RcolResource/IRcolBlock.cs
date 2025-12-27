namespace TS4Tools.Wrappers;

/// <summary>
/// Interface for RCOL chunk blocks.
/// </summary>
public interface IRcolBlock
{
    /// <summary>
    /// The 4-character tag identifying the block type (e.g., "MODL", "MLOD", "GEOM").
    /// </summary>
    string Tag { get; }

    /// <summary>
    /// The resource type code for this block.
    /// </summary>
    uint ResourceType { get; }

    /// <summary>
    /// Whether this is a known/parsed block type or an unknown block.
    /// </summary>
    bool IsKnownType { get; }

    /// <summary>
    /// The raw data for this block.
    /// </summary>
    ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Serializes the block back to bytes.
    /// </summary>
    ReadOnlyMemory<byte> Serialize();
}
