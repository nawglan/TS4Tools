namespace TS4Tools.Wrappers;

/// <summary>
/// Abstract base class for RCOL chunk blocks.
/// Source: ARCOLBlock.cs from legacy s4pi
/// </summary>
public abstract class RcolBlock : IRcolBlock
{
    /// <summary>
    /// The raw data for this block, used for unknown blocks or round-trip preservation.
    /// </summary>
    protected byte[] RawData { get; set; } = [];

    /// <inheritdoc/>
    public abstract string Tag { get; }

    /// <inheritdoc/>
    public abstract uint ResourceType { get; }

    /// <inheritdoc/>
    public abstract bool IsKnownType { get; }

    /// <inheritdoc/>
    public virtual ReadOnlyMemory<byte> Data => RawData;

    /// <summary>
    /// Creates an empty block.
    /// </summary>
    protected RcolBlock()
    {
    }

    /// <summary>
    /// Creates a block from raw data.
    /// </summary>
    protected RcolBlock(ReadOnlySpan<byte> data)
    {
        RawData = data.ToArray();
        Parse(data);
    }

    /// <summary>
    /// Parses the block data. Override to implement format-specific parsing.
    /// </summary>
    protected virtual void Parse(ReadOnlySpan<byte> data)
    {
        // Default implementation stores raw data only
    }

    /// <summary>
    /// Serializes the block back to bytes. Override to implement format-specific serialization.
    /// </summary>
    public virtual ReadOnlyMemory<byte> Serialize()
    {
        return RawData;
    }

    /// <summary>
    /// Extracts the 4-character tag from block data.
    /// The tag is typically the first 4 bytes as a little-endian uint32 converted to characters.
    /// Source: DefaultRCOL.cs line 93
    /// </summary>
    public static string ExtractTag(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return "????";

        return new string([
            (char)data[0],
            (char)data[1],
            (char)data[2],
            (char)data[3]
        ]);
    }
}
