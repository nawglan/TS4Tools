// Source: legacy_references/Sims4Tools/s4pi Wrappers/AnimationResources/TrackMask.cs lines 27-263

namespace TS4Tools.Wrappers;

/// <summary>
/// TkMk (TrackMask) RCOL block - per-track blend weights for animations.
/// Resource Type: 0x033260E3
/// Source: TrackMask.cs lines 27-263
/// </summary>
/// <remarks>
/// Layout: tag, version; for version &lt; 0x201 a 48 byte unused block, otherwise a rig key (TGI in
/// ITG order), a vertex animation blend weight and a 28 byte unused block; then an int32 count of
/// float track masks. No trailing data is allowed (TrackMask.cs lines 188-189).
/// </remarks>
public sealed class TrackMaskBlock : RcolBlock
{
    /// <summary>Resource type identifier for TkMk. Source: TrackMask.cs line 163.</summary>
    public const uint TypeId = 0x033260E3;

    /// <summary>Default version. Source: TrackMask.cs line 29.</summary>
    public const uint DefaultVersion = 0x00000200;

    /// <summary>Unused block length for versions below 0x201.</summary>
    public const int LegacyUnusedLength = 48;

    /// <summary>Unused block length for versions 0x201 and above.</summary>
    public const int UnusedLength = 28;

    /// <inheritdoc/>
    public override string Tag => "TkMk";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version.</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Rig key (versions 0x201 and above).</summary>
    public RcolTgiBlock RigKey { get; set; }

    /// <summary>Vertex animation blend weight (versions 0x201 and above).</summary>
    public float VertexAnimBlendWeight { get; set; }

    /// <summary>
    /// Unused bytes: 48 bytes below version 0x201, 28 bytes otherwise. Shorter or longer values are
    /// padded or truncated on write, as legacy does (TrackMask.cs lines 198-222).
    /// </summary>
    public byte[] Unused { get; set; } = new byte[LegacyUnusedLength];

    /// <summary>Track mask weights.</summary>
    public List<float> TrackMasks { get; } = [];

    /// <summary>
    /// Creates an empty TkMk block.
    /// </summary>
    public TrackMaskBlock() : base()
    {
        Unused = new byte[Version < 0x201 ? LegacyUnusedLength : UnusedLength];
    }

    /// <summary>
    /// Creates a TkMk block from raw data.
    /// </summary>
    public TrackMaskBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the block. Source: TrackMask.cs lines 171-190
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid TkMk tag: expected '{Tag}', got '{tag}'");

        var r = new RcolSpanReader(data, 4);
        Version = r.ReadUInt32();
        if (Version < 0x201)
        {
            RigKey = default;
            VertexAnimBlendWeight = 0;
            Unused = r.ReadBytes(LegacyUnusedLength);
        }
        else
        {
            RigKey = RcolTgiBlock.Read(r.ReadBytes(RcolTgiBlock.Size));
            VertexAnimBlendWeight = r.ReadSingle();
            Unused = r.ReadBytes(UnusedLength);
        }

        TrackMasks.Clear();
        int count = r.ReadCount(4);
        for (int i = 0; i < count; i++)
            TrackMasks.Add(r.ReadSingle());

        if (r.Remaining != 0)
            throw new InvalidDataException("Unexpected end of TkMk block: trailing data found");
    }

    /// <summary>
    /// Serializes the block. Source: TrackMask.cs lines 192-227
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'T');
        w.Write((byte)'k');
        w.Write((byte)'M');
        w.Write((byte)'k');
        w.Write(Version);
        if (Version < 0x201)
        {
            w.Write(Resize(Unused, LegacyUnusedLength));
        }
        else
        {
            var tgi = new byte[RcolTgiBlock.Size];
            RigKey.Write(tgi);
            w.Write(tgi);
            w.Write(VertexAnimBlendWeight);
            w.Write(Resize(Unused, UnusedLength));
        }

        w.Write(TrackMasks.Count);
        foreach (float f in TrackMasks)
            w.Write(f);
        return ms.ToArray();
    }

    private static byte[] Resize(byte[]? value, int length)
    {
        var result = new byte[length];
        if (value is not null)
            Array.Copy(value, result, Math.Min(value.Length, length));
        return result;
    }
}
