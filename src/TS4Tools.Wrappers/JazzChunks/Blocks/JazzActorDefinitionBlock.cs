// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 594-668

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Actor Definition block - defines an actor in the state machine.
/// Resource Type: 0x02EEDB2F
/// Tag: "S_AD"
/// </summary>
public sealed class JazzActorDefinitionBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.ActorDefinition;

    private const uint DefaultVersion = 0x0100;

    /// <inheritdoc/>
    public override string Tag => "S_AD";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0100).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Name hash for this actor.</summary>
    public uint NameHash { get; set; }

    /// <summary>Unknown field 1.</summary>
    public uint Unknown1 { get; set; }

    /// <summary>Creates an empty block.</summary>
    public JazzActorDefinitionBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzActorDefinitionBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 634-641
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        NameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
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
        writer.Write((byte)'A');
        writer.Write((byte)'D');

        // Source: JazzResource.cs lines 644-651
        writer.Write(Version);
        writer.Write(NameHash);
        writer.Write(Unknown1);

        return ms.ToArray();
    }
}
