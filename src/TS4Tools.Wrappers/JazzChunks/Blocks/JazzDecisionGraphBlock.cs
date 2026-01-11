// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 504-592

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Decision Graph block - represents a decision graph node.
/// Resource Type: 0x02EEDB18
/// Tag: "S_DG"
/// </summary>
public sealed class JazzDecisionGraphBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.DecisionGraph;

    private const uint DefaultVersion = 0x0101;

    /// <inheritdoc/>
    public override string Tag => "S_DG";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0101).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Unknown field 1.</summary>
    public uint Unknown1 { get; set; }

    /// <summary>Chunk reference indexes for outbound decision graphs.</summary>
    public List<uint> OutboundDecisionGraphIndexes { get; set; } = [];

    /// <summary>Chunk reference indexes for inbound decision graphs.</summary>
    public List<uint> InboundDecisionGraphIndexes { get; set; } = [];

    /// <summary>Creates an empty block.</summary>
    public JazzDecisionGraphBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzDecisionGraphBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 549-558
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        OutboundDecisionGraphIndexes = JazzHelper.ReadChunkReferenceList(data, ref pos);
        InboundDecisionGraphIndexes = JazzHelper.ReadChunkReferenceList(data, ref pos);

        JazzHelper.ParseDeadBeef(data, ref pos);
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'S');
        writer.Write((byte)'_');
        writer.Write((byte)'D');
        writer.Write((byte)'G');

        // Source: JazzResource.cs lines 561-572
        writer.Write(Version);
        writer.Write(Unknown1);
        JazzHelper.WriteChunkReferenceList(writer, OutboundDecisionGraphIndexes);
        JazzHelper.WriteChunkReferenceList(writer, InboundDecisionGraphIndexes);
        JazzHelper.WriteDeadBeef(writer);

        return ms.ToArray();
    }
}
