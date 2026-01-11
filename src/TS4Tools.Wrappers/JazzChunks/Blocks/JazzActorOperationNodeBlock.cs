// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 1980-2108

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Actor Operation Node block - performs an operation on an actor.
/// Resource Type: 0x02EEEBDE
/// Tag: "AcOp"
/// </summary>
public sealed class JazzActorOperationNodeBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.ActorOperationNode;

    private const uint DefaultVersion = 0x0100;

    /// <inheritdoc/>
    public override string Tag => "AcOp";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0100).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Actor definition chunk index.</summary>
    public uint ActorDefinitionIndex { get; set; }

    /// <summary>Operation to perform.</summary>
    public JazzActorOperation ActorOp { get; set; } = JazzActorOperation.None;

    /// <summary>Operand value for the operation.</summary>
    public uint Operand { get; set; }

    /// <summary>Unknown field 1.</summary>
    public uint Unknown1 { get; set; }

    /// <summary>Unknown field 2.</summary>
    public uint Unknown2 { get; set; }

    /// <summary>Unknown field 3.</summary>
    public uint Unknown3 { get; set; }

    /// <summary>Decision graph chunk indexes.</summary>
    public List<uint> DecisionGraphIndexes { get; set; } = [];

    /// <summary>Creates an empty block.</summary>
    public JazzActorOperationNodeBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzActorOperationNodeBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 2041-2054
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        ActorDefinitionIndex = JazzHelper.ReadChunkReference(data, ref pos);

        ActorOp = (JazzActorOperation)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Operand = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        DecisionGraphIndexes = JazzHelper.ReadChunkReferenceList(data, ref pos);

        JazzHelper.ParseCloseDgn(data, ref pos);
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'A');
        writer.Write((byte)'c');
        writer.Write((byte)'O');
        writer.Write((byte)'p');

        // Source: JazzResource.cs lines 2057-2072
        writer.Write(Version);
        writer.Write(ActorDefinitionIndex);
        writer.Write((uint)ActorOp);
        writer.Write(Operand);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        JazzHelper.WriteChunkReferenceList(writer, DecisionGraphIndexes);
        JazzHelper.WriteCloseDgn(writer);

        return ms.ToArray();
    }
}
