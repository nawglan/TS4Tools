// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 2110-2308

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Stop Animation Node block - stops an animation.
/// Resource Type: 0x0344D438
/// Tag: "Stop"
/// </summary>
public sealed class JazzStopAnimationNodeBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.StopAnimationNode;

    private const uint DefaultVersion = 0x0104;

    /// <inheritdoc/>
    public override string Tag => "Stop";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0104).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Animation node flags.</summary>
    public JazzAnimationNodeFlags AnimationFlags { get; set; } = JazzAnimationNodeFlags.Default;

    /// <summary>Animation priority.</summary>
    public JazzAnimationPriority AnimationPriority { get; set; } = JazzAnimationPriority.Unset;

    /// <summary>Unknown field 1.</summary>
    public float Unknown1 { get; set; }

    /// <summary>Blend in time.</summary>
    public float BlendInTime { get; set; }

    /// <summary>Blend out time.</summary>
    public float BlendOutTime { get; set; }

    /// <summary>Unknown field 4.</summary>
    public float Unknown4 { get; set; }

    /// <summary>Animation speed.</summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>Actor definition chunk index.</summary>
    public uint ActorDefinitionIndex { get; set; }

    /// <summary>Timing priority.</summary>
    public JazzAnimationPriority TimingPriority { get; set; } = JazzAnimationPriority.Unset;

    /// <summary>Unknown fields 6-11.</summary>
    public uint Unknown6 { get; set; }
    public uint Unknown7 { get; set; }
    public uint Unknown8 { get; set; }
    public uint Unknown9 { get; set; }
    public uint Unknown10 { get; set; }
    public uint Unknown11 { get; set; }

    /// <summary>Decision graph chunk indexes.</summary>
    public List<uint> DecisionGraphIndexes { get; set; } = [];

    /// <summary>Creates an empty block.</summary>
    public JazzStopAnimationNodeBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzStopAnimationNodeBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 2208-2232
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        AnimationFlags = (JazzAnimationNodeFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        AnimationPriority = (JazzAnimationPriority)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown1 = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        BlendInTime = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        BlendOutTime = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        Unknown4 = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        Speed = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;

        ActorDefinitionIndex = JazzHelper.ReadChunkReference(data, ref pos);

        TimingPriority = (JazzAnimationPriority)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown6 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown7 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown8 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown9 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown10 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown11 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        JazzHelper.ParseDeadBeef(data, ref pos);

        DecisionGraphIndexes = JazzHelper.ReadChunkReferenceList(data, ref pos);

        JazzHelper.ParseCloseDgn(data, ref pos);
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'S');
        writer.Write((byte)'t');
        writer.Write((byte)'o');
        writer.Write((byte)'p');

        // Source: JazzResource.cs lines 2235-2260
        writer.Write(Version);
        writer.Write((uint)AnimationFlags);
        writer.Write((uint)AnimationPriority);
        writer.Write(Unknown1);
        writer.Write(BlendInTime);
        writer.Write(BlendOutTime);
        writer.Write(Unknown4);
        writer.Write(Speed);
        writer.Write(ActorDefinitionIndex);
        writer.Write((uint)TimingPriority);
        writer.Write(Unknown6);
        writer.Write(Unknown7);
        writer.Write(Unknown8);
        writer.Write(Unknown9);
        writer.Write(Unknown10);
        writer.Write(Unknown11);
        JazzHelper.WriteDeadBeef(writer);
        JazzHelper.WriteChunkReferenceList(writer, DecisionGraphIndexes);
        JazzHelper.WriteCloseDgn(writer);

        return ms.ToArray();
    }
}
