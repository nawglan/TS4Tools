// Source: legacy_references/Sims4Tools/s4pi Wrappers/JazzResource/JazzResource.cs lines 1766-1837

using TS4Tools.Resources;

namespace TS4Tools.Wrappers.JazzChunks;

/// <summary>
/// Jazz Next State Node block - transitions to the next state.
/// Resource Type: 0x02EEEBDC
/// Tag: "SNSN"
/// </summary>
public sealed class JazzNextStateNodeBlock : RcolBlock
{
    /// <summary>Resource type identifier.</summary>
    public const uint TypeId = JazzConstants.NextStateNode;

    private const uint DefaultVersion = 0x0101;

    /// <inheritdoc/>
    public override string Tag => "SNSN";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (default 0x0101).</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>State chunk index to transition to.</summary>
    public uint StateIndex { get; set; }

    /// <summary>Creates an empty block.</summary>
    public JazzNextStateNodeBlock() : base() { }

    /// <summary>Creates a block from raw data.</summary>
    public JazzNextStateNodeBlock(ReadOnlySpan<byte> data) : base(data) { }

    /// <inheritdoc/>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Source: JazzResource.cs lines 1803-1810
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        StateIndex = JazzHelper.ReadChunkReference(data, ref pos);

        JazzHelper.ParseCloseDgn(data, ref pos);
    }

    /// <inheritdoc/>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'S');
        writer.Write((byte)'N');
        writer.Write((byte)'S');
        writer.Write((byte)'N');

        // Source: JazzResource.cs lines 1813-1821
        writer.Write(Version);
        writer.Write(StateIndex);
        JazzHelper.WriteCloseDgn(writer);

        return ms.ToArray();
    }
}
