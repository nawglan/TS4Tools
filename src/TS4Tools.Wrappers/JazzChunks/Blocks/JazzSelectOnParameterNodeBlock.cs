// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 1440-1605

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Select On Parameter Node block - selects based on parameter value.
/// Resource Type: 0x02EEDB92
/// Tag: "SoPn"
/// </summary>
public sealed class JazzSelectOnParameterNodeBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.SelectOnParameterNode;

    private const uint DefaultVersion = 0x0101;

    /// <inheritdoc/>
    public override string Tag => "SoPn";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0101).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Parameter definition chunk index.</summary>
    public uint ParameterDefinitionIndex { get; set; }

    /// <summary>List of parameter value matches.</summary>
    public List<JazzParameterMatch> Matches { get; set; } = [];

    /// <summary>Creates an empty block.</summary>
    public JazzSelectOnParameterNodeBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzSelectOnParameterNodeBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 1482-1491
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        ParameterDefinitionIndex = JazzHelper.ReadChunkReference(data, ref pos);

        // Read matches
        int matchCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        Matches = new List<JazzParameterMatch>(matchCount);
        for (int i = 0; i < matchCount; i++)
        {
            Matches.Add(JazzParameterMatch.Parse(data, ref pos));
        }

        JazzHelper.ParseDeadBeef(data, ref pos);
        JazzHelper.ParseCloseDgn(data, ref pos);
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'S');
        writer.Write((byte)'o');
        writer.Write((byte)'P');
        writer.Write((byte)'n');

        // Source: JazzResource.cs lines 1494-1505
        writer.Write(Version);
        writer.Write(ParameterDefinitionIndex);

        writer.Write(Matches.Count);
        foreach (var match in Matches)
        {
            match.Write(writer);
        }

        JazzHelper.WriteDeadBeef(writer);
        JazzHelper.WriteCloseDgn(writer);

        return ms.ToArray();
    }
}
