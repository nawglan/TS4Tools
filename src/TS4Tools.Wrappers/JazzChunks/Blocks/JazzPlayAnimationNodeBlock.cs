// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 778-1265

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Play Animation Node block - plays an animation.
/// Resource Type: 0x02EEDB5F
/// Tag: "Play"
/// </summary>
public sealed class JazzPlayAnimationNodeBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.PlayAnimationNode;

    private const uint DefaultVersion = 0x0105;

    /// <inheritdoc/>
    public override string Tag => "Play";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0105).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Clip resource TGI in ITG order.</summary>
    public ResourceKey ClipResource { get; set; }

    /// <summary>TKMK resource TGI in ITG order.</summary>
    public ResourceKey TkmkResource { get; set; }

    /// <summary>Unknown field 1.</summary>
    public uint Unknown1 { get; set; }

    /// <summary>Unknown field 2.</summary>
    public uint Unknown2 { get; set; }

    /// <summary>Unknown field 3.</summary>
    public uint Unknown3 { get; set; }

    /// <summary>Actor slot entries.</summary>
    public List<JazzActorSlot> ActorSlots { get; set; } = [];

    /// <summary>Actor suffix entries.</summary>
    public List<JazzActorSuffix> ActorSuffixes { get; set; } = [];

    /// <summary>Additive clip resource TGI in ITG order.</summary>
    public ResourceKey AdditiveClipResource { get; set; }

    /// <summary>Animation name string.</summary>
    public string Animation { get; set; } = string.Empty;

    /// <summary>Additive animation name string.</summary>
    public string AdditiveAnimation { get; set; } = string.Empty;

    /// <summary>Animation node flags.</summary>
    public JazzAnimationNodeFlags AnimationNodeFlags { get; set; } = JazzAnimationNodeFlags.Default;

    /// <summary>Animation priority.</summary>
    public JazzAnimationPriority AnimationPriority { get; set; } = JazzAnimationPriority.Unset;

    /// <summary>Unknown field 9.</summary>
    public float Unknown9 { get; set; }

    /// <summary>Blend in time.</summary>
    public float BlendInTime { get; set; }

    /// <summary>Blend out time.</summary>
    public float BlendOutTime { get; set; }

    /// <summary>Unknown field 11.</summary>
    public float Unknown11 { get; set; }

    /// <summary>Animation speed.</summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>Actor definition chunk index.</summary>
    public uint ActorDefinitionIndex { get; set; }

    /// <summary>Timing priority.</summary>
    public JazzAnimationPriority TimingPriority { get; set; } = JazzAnimationPriority.Unset;

    /// <summary>Unknown fields 13-18.</summary>
    public uint Unknown13 { get; set; }
    public uint Unknown14 { get; set; }
    public uint Unknown15 { get; set; }
    public uint Unknown16 { get; set; }
    public uint Unknown17 { get; set; }
    public uint Unknown18 { get; set; }

    /// <summary>Decision graph chunk indexes.</summary>
    public List<uint> DecisionGraphIndexes { get; set; } = [];

    /// <summary>Creates an empty block.</summary>
    public JazzPlayAnimationNodeBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzPlayAnimationNodeBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 921-963
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        ClipResource = JazzHelper.ReadTgiItg(data, ref pos);
        TkmkResource = JazzHelper.ReadTgiItg(data, ref pos);

        int actorSlotCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read actor slots (count was read earlier)
        ActorSlots = new List<JazzActorSlot>(actorSlotCount);
        for (int i = 0; i < actorSlotCount; i++)
        {
            ActorSlots.Add(JazzActorSlot.Parse(data, ref pos));
        }

        // Read actor suffixes (has its own count)
        int suffixCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        ActorSuffixes = new List<JazzActorSuffix>(suffixCount);
        for (int i = 0; i < suffixCount; i++)
        {
            ActorSuffixes.Add(JazzActorSuffix.Parse(data, ref pos));
        }

        JazzHelper.ParseDeadBeef(data, ref pos);

        AdditiveClipResource = JazzHelper.ReadTgiItg(data, ref pos);

        Animation = JazzHelper.ReadUnicodeString(data, ref pos);
        JazzHelper.ExpectZero(data, ref pos);

        AdditiveAnimation = JazzHelper.ReadUnicodeString(data, ref pos);
        JazzHelper.ExpectZero(data, ref pos);

        JazzHelper.ParseDeadBeef(data, ref pos);

        AnimationNodeFlags = (JazzAnimationNodeFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        AnimationPriority = (JazzAnimationPriority)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown9 = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        BlendInTime = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        BlendOutTime = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        Unknown11 = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;
        Speed = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;

        ActorDefinitionIndex = JazzHelper.ReadChunkReference(data, ref pos);

        TimingPriority = (JazzAnimationPriority)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown13 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown14 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown15 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown16 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown17 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown18 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
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
        writer.Write((byte)'P');
        writer.Write((byte)'l');
        writer.Write((byte)'a');
        writer.Write((byte)'y');

        // Source: JazzResource.cs lines 966-1015
        writer.Write(Version);

        JazzHelper.WriteTgiItg(writer, ClipResource);
        JazzHelper.WriteTgiItg(writer, TkmkResource);

        writer.Write(ActorSlots.Count);
        writer.Write(Unknown1);
        writer.Write(Unknown2);
        writer.Write(Unknown3);

        foreach (var slot in ActorSlots)
        {
            slot.Write(writer);
        }

        writer.Write(ActorSuffixes.Count);
        foreach (var suffix in ActorSuffixes)
        {
            suffix.Write(writer);
        }

        JazzHelper.WriteDeadBeef(writer);

        JazzHelper.WriteTgiItg(writer, AdditiveClipResource);

        JazzHelper.WriteUnicodeString(writer, Animation);
        JazzHelper.PadZero(writer);

        JazzHelper.WriteUnicodeString(writer, AdditiveAnimation);
        JazzHelper.PadZero(writer);

        JazzHelper.WriteDeadBeef(writer);

        writer.Write((uint)AnimationNodeFlags);
        writer.Write((uint)AnimationPriority);
        writer.Write(Unknown9);
        writer.Write(BlendInTime);
        writer.Write(BlendOutTime);
        writer.Write(Unknown11);
        writer.Write(Speed);
        writer.Write(ActorDefinitionIndex);
        writer.Write((uint)TimingPriority);
        writer.Write(Unknown13);
        writer.Write(Unknown14);
        writer.Write(Unknown15);
        writer.Write(Unknown16);
        writer.Write(Unknown17);
        writer.Write(Unknown18);

        JazzHelper.WriteDeadBeef(writer);

        JazzHelper.WriteChunkReferenceList(writer, DecisionGraphIndexes);

        JazzHelper.WriteCloseDgn(writer);

        return ms.ToArray();
    }
}
