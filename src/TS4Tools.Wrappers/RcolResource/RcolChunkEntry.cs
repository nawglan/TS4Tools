namespace TS4Tools.Wrappers;

/// <summary>
/// Pairs a TGI block identifier with an RCOL block.
/// Source: GenericRCOLResource.cs lines 162-278 (ChunkEntry class)
/// </summary>
public sealed class RcolChunkEntry
{
    /// <summary>
    /// The TGI block identifying this chunk.
    /// </summary>
    public RcolTgiBlock TgiBlock { get; }

    /// <summary>
    /// The parsed RCOL block.
    /// </summary>
    public IRcolBlock Block { get; }

    /// <summary>
    /// The position of this chunk in the resource data.
    /// </summary>
    public uint Position { get; internal set; }

    /// <summary>
    /// The length of this chunk in bytes.
    /// </summary>
    public int Length { get; internal set; }

    /// <summary>
    /// Creates a new chunk entry.
    /// </summary>
    public RcolChunkEntry(RcolTgiBlock tgiBlock, IRcolBlock block)
    {
        TgiBlock = tgiBlock;
        Block = block;
    }

    /// <summary>
    /// Creates a new chunk entry with position and length.
    /// </summary>
    public RcolChunkEntry(RcolTgiBlock tgiBlock, IRcolBlock block, uint position, int length)
        : this(tgiBlock, block)
    {
        Position = position;
        Length = length;
    }

    /// <summary>
    /// Gets a display string for this chunk entry.
    /// </summary>
    public override string ToString() =>
        $"{Block.Tag} - {TgiBlock} ({Length} bytes)";
}
