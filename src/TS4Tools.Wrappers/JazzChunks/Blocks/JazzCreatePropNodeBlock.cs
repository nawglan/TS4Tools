// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 1839-1978

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Create Prop Node block - creates a prop in the scene.
/// Resource Type: 0x02EEEBDD
/// Tag: "Prop"
/// </summary>
public sealed class JazzCreatePropNodeBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.CreatePropNode;

    private const uint DefaultVersion = 0x0100;

    /// <inheritdoc/>
    public override string Tag => "Prop";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0100).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Actor definition chunk index.</summary>
    public uint ActorDefinitionIndex { get; set; }

    /// <summary>Parameter definition chunk index.</summary>
    public uint ParameterDefinitionIndex { get; set; }

    /// <summary>Prop resource TGI in ITG order.</summary>
    public ResourceKey PropResource { get; set; }

    /// <summary>Unknown field 2.</summary>
    public uint Unknown2 { get; set; }

    /// <summary>Unknown field 3.</summary>
    public uint Unknown3 { get; set; }

    /// <summary>Unknown field 4.</summary>
    public uint Unknown4 { get; set; }

    /// <summary>Unknown field 5.</summary>
    public uint Unknown5 { get; set; }

    /// <summary>Decision graph chunk indexes.</summary>
    public List<uint> DecisionGraphIndexes { get; set; } = [];

    /// <summary>Creates an empty block.</summary>
    public JazzCreatePropNodeBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzCreatePropNodeBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 1908-1924
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        ActorDefinitionIndex = JazzHelper.ReadChunkReference(data, ref pos);
        ParameterDefinitionIndex = JazzHelper.ReadChunkReference(data, ref pos);
        PropResource = JazzHelper.ReadTgiItg(data, ref pos);

        Unknown2 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown3 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown4 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;
        Unknown5 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
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
        writer.Write((byte)'P');
        writer.Write((byte)'r');
        writer.Write((byte)'o');
        writer.Write((byte)'p');

        // Source: JazzResource.cs lines 1927-1946
        writer.Write(Version);
        writer.Write(ActorDefinitionIndex);
        writer.Write(ParameterDefinitionIndex);
        JazzHelper.WriteTgiItg(writer, PropResource);
        writer.Write(Unknown2);
        writer.Write(Unknown3);
        writer.Write(Unknown4);
        writer.Write(Unknown5);
        JazzHelper.WriteChunkReferenceList(writer, DecisionGraphIndexes);
        JazzHelper.WriteCloseDgn(writer);

        return ms.ToArray();
    }
}
