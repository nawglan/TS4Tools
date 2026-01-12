// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 135-384

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz State Machine block - the root chunk containing state machine definitions.
/// Resource Type: 0x02D5DF13
/// Tag: "S_SM"
/// </summary>
public sealed class JazzStateMachineBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.StateMachine;

    private const uint DefaultVersion = 0x0202;

    /// <inheritdoc/>
    public override string Tag => "S_SM";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0202).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Name hash for this state machine.</summary>
    public uint NameHash { get; set; }

    /// <summary>Chunk reference indexes for actor definitions.</summary>
    public List<uint> ActorDefinitionIndexes { get; set; } = [];

    /// <summary>Chunk reference indexes for property/parameter definitions.</summary>
    public List<uint> PropertyDefinitionIndexes { get; set; } = [];

    /// <summary>Chunk reference indexes for states.</summary>
    public List<uint> StateIndexes { get; set; } = [];

    /// <summary>Animation entries in this state machine.</summary>
    public List<JazzAnimation> Animations { get; set; } = [];

    /// <summary>State machine flags.</summary>
    public JazzStateMachineFlags Properties { get; set; } = JazzStateMachineFlags.Default;

    /// <summary>Animation priority for automation.</summary>
    public JazzAnimationPriority AutomationPriority { get; set; } = JazzAnimationPriority.Unset;

    /// <summary>Awareness overlay level.</summary>
    public JazzAwarenessLevel AwarenessOverlayLevel { get; set; } = JazzAwarenessLevel.Unset;

    /// <summary>Unknown field 2.</summary>
    public uint Unknown2 { get; set; }

    /// <summary>Unknown field 3.</summary>
    public uint Unknown3 { get; set; }

    /// <summary>Unknown field 4.</summary>
    public uint Unknown4 { get; set; }

    /// <summary>Unknown field 5.</summary>
    public uint Unknown5 { get; set; }

    /// <summary>Creates an empty block.</summary>
    public JazzStateMachineBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzStateMachineBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 216-233
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        NameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        ActorDefinitionIndexes = JazzHelper.ReadChunkReferenceList(data, ref pos);
        PropertyDefinitionIndexes = JazzHelper.ReadChunkReferenceList(data, ref pos);
        StateIndexes = JazzHelper.ReadChunkReferenceList(data, ref pos);

        // Read animations list
        int animCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        Animations = new List<JazzAnimation>(animCount);
        for (int i = 0; i < animCount; i++)
        {
            Animations.Add(JazzAnimation.Parse(data, ref pos));
        }

        JazzHelper.ParseDeadBeef(data, ref pos);

        Properties = (JazzStateMachineFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        AutomationPriority = (JazzAnimationPriority)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        AwarenessOverlayLevel = (JazzAwarenessLevel)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown4 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown5 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
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
        writer.Write((byte)'M');

        // Source: JazzResource.cs lines 236-259
        writer.Write(Version);
        writer.Write(NameHash);

        JazzHelper.WriteChunkReferenceList(writer, ActorDefinitionIndexes);
        JazzHelper.WriteChunkReferenceList(writer, PropertyDefinitionIndexes);
        JazzHelper.WriteChunkReferenceList(writer, StateIndexes);

        // Write animations
        writer.Write(Animations.Count);
        foreach (var anim in Animations)
        {
            anim.Write(writer);
        }

        JazzHelper.WriteDeadBeef(writer);

        writer.Write((uint)Properties);
        writer.Write((uint)AutomationPriority);
        writer.Write((uint)AwarenessOverlayLevel);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        writer.Write(Unknown4);
        writer.Write(Unknown5);

        return ms.ToArray();
    }
}
