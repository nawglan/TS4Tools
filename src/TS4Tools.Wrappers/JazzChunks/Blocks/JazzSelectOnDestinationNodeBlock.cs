// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 1607-1764

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Select On Destination Node block - selects based on destination state.
/// Resource Type: 0x02EEDBA5
/// Tag: "DG00"
/// </summary>
public sealed class JazzSelectOnDestinationNodeBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.SelectOnDestinationNode;

    private const uint DefaultVersion = 0x0101;

    /// <inheritdoc/>
    public override string Tag => "DG00";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0101).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>List of destination state matches.</summary>
    public List<JazzDestinationMatch> Matches { get; set; } = [];

    /// <summary>Creates an empty block.</summary>
    public JazzSelectOnDestinationNodeBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzSelectOnDestinationNodeBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 1645-1653
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read matches
        int matchCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        Matches = new List<JazzDestinationMatch>(matchCount);
        for (int i = 0; i < matchCount; i++)
        {
            Matches.Add(JazzDestinationMatch.Parse(data, ref pos));
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
        writer.Write((byte)'D');
        writer.Write((byte)'G');
        writer.Write((byte)'0');
        writer.Write((byte)'0');

        // Source: JazzResource.cs lines 1656-1665
        writer.Write(Version);

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
