namespace TS4Tools.Wrappers;

/// <summary>
/// Default RCOL block handler for unknown/unrecognized chunk types.
/// Preserves raw data for round-trip integrity.
/// Source: DefaultRCOL.cs from legacy s4pi
/// </summary>
public sealed class UnknownRcolBlock : RcolBlock
{
    private readonly string _tag;
    private readonly uint _resourceType;

    /// <inheritdoc/>
    public override string Tag => _tag;

    /// <inheritdoc/>
    public override uint ResourceType => _resourceType;

    /// <inheritdoc/>
    public override bool IsKnownType => false;

    /// <summary>
    /// Creates an unknown block from raw data.
    /// </summary>
    /// <param name="resourceType">The resource type from the TGI block.</param>
    /// <param name="data">The raw chunk data.</param>
    public UnknownRcolBlock(uint resourceType, ReadOnlySpan<byte> data)
        : base(data)
    {
        _resourceType = resourceType;
        _tag = ExtractTag(data);
    }

    /// <summary>
    /// Creates an unknown block with explicit tag.
    /// </summary>
    /// <param name="resourceType">The resource type from the TGI block.</param>
    /// <param name="tag">The 4-character tag.</param>
    /// <param name="data">The raw chunk data.</param>
    public UnknownRcolBlock(uint resourceType, string tag, ReadOnlySpan<byte> data)
        : base(data)
    {
        _resourceType = resourceType;
        _tag = tag;
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize() => RawData;
}
