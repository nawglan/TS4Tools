// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 1267-1438

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Random Node block - randomly selects between outcomes.
/// Resource Type: 0x02EEDB70
/// Tag: "Rand"
/// </summary>
public sealed class JazzRandomNodeBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.RandomNode;

    private const uint DefaultVersion = 0x0101;

    /// <inheritdoc/>
    public override string Tag => "Rand";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0101).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>List of weighted outcomes.</summary>
    public List<JazzOutcome> Outcomes { get; set; } = [];

    /// <summary>Random node flags.</summary>
    public JazzRandomNodeFlags Flags { get; set; } = JazzRandomNodeFlags.None;

    /// <summary>Creates an empty block.</summary>
    public JazzRandomNodeBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzRandomNodeBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 1316-1325
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Read outcomes
        int outcomeCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;
        Outcomes = new List<JazzOutcome>(outcomeCount);
        for (int i = 0; i < outcomeCount; i++)
        {
            Outcomes.Add(JazzOutcome.Parse(data, ref pos));
        }

        JazzHelper.ParseDeadBeef(data, ref pos);

        Flags = (JazzRandomNodeFlags)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        JazzHelper.ParseCloseDgn(data, ref pos);
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'R');
        writer.Write((byte)'a');
        writer.Write((byte)'n');
        writer.Write((byte)'d');

        // Source: JazzResource.cs lines 1328-1338
        writer.Write(Version);

        writer.Write(Outcomes.Count);
        foreach (var outcome in Outcomes)
        {
            outcome.Write(writer);
        }

        JazzHelper.WriteDeadBeef(writer);
        writer.Write((uint)Flags);
        JazzHelper.WriteCloseDgn(writer);

        return ms.ToArray();
    }
}
