// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 386-502

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz State block - represents a single state in the state machine.
/// Resource Type: 0x02EEDAFE
/// Tag: "S_St"
/// </summary>
public sealed class JazzStateBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.State;

    private const uint DefaultVersion = 0x0101;

    /// <inheritdoc/>
    public override string Tag => "S_St";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0101).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Name hash for this state.</summary>
    public uint NameHash { get; set; }

    /// <summary>State flags.</summary>
    public JazzStateFlags Flags { get; set; } = JazzStateFlags.None;

    /// <summary>Chunk reference index for the decision graph.</summary>
    public uint DecisionGraphIndex { get; set; }

    /// <summary>Chunk reference indexes for outbound states.</summary>
    public List<uint> OutboundStateIndexes { get; set; } = [];

    /// <summary>Awareness overlay level.</summary>
    public JazzAwarenessLevel AwarenessOverlayLevel { get; set; } = JazzAwarenessLevel.Unset;

    /// <summary>Creates an empty block.</summary>
    public JazzStateBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzStateBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 453-463
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        NameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Flags = (JazzStateFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        DecisionGraphIndex = JazzHelper.ReadChunkReference(data, ref pos);
        OutboundStateIndexes = JazzHelper.ReadChunkReferenceList(data, ref pos);

        AwarenessOverlayLevel = (JazzAwarenessLevel)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'S');
        writer.Write((byte)'_');
        writer.Write((byte)'S');
        writer.Write((byte)'t');

        // Source: JazzResource.cs lines 466-478
        writer.Write(Version);
        writer.Write(NameHash);
        writer.Write((uint)Flags);
        writer.Write(DecisionGraphIndex);
        JazzHelper.WriteChunkReferenceList(writer, OutboundStateIndexes);
        writer.Write((uint)AwarenessOverlayLevel);

        return ms.ToArray();
    }
}
