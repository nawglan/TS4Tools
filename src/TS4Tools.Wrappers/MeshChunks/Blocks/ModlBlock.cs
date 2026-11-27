// Source: legacy_references/Sims4Tools/s4pi Wrappers/MeshChunks/MODL.cs lines 45-351

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// MODL (Model) block - contains model definition with LOD entries.
/// Resource Type: 0x01661233
/// Source: MODL.cs lines 45-351
/// </summary>
/// <remarks>
/// Layout: tag, version, int32 LOD entry count, bounding box, then
/// version 258..0x2FF: extra bounds list (int32 count + boxes), fade type, custom fade distance;
/// version &gt;= 0x300: 20 unknown bytes; followed by the LOD entries (no separate count).
/// Source: MODL.cs lines 267-293 (Parse) and 295-320 (UnParse).
/// </remarks>
public sealed class ModlBlock : RcolBlock
{
    /// <summary>Resource type identifier for MODL.</summary>
    public const uint TypeId = 0x01661233;

    /// <summary>Default version. Source: MODL.cs line 197 (constructor passes 256).</summary>
    public const uint DefaultVersion = 256;

    /// <summary>Length of the version 0x300+ unknown block. Source: MODL.cs line 263.</summary>
    public const int UnknownLength = 20;

    /// <inheritdoc/>
    public override string Tag => "MODL";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version.</summary>
    public uint Version { get; set; } = DefaultVersion;

    /// <summary>Model bounding box.</summary>
    public BoundingBox Bounds { get; set; }

    /// <summary>Extra bounding boxes (versions 258 to 0x2FF).</summary>
    public List<BoundingBox> ExtraBounds { get; } = [];

    /// <summary>Fade type (versions 258 to 0x2FF).</summary>
    public uint FadeType { get; set; }

    /// <summary>Custom fade distance (versions 258 to 0x2FF).</summary>
    public float CustomFadeDistance { get; set; }

    /// <summary>LOD entries.</summary>
    public List<ModlLodEntry> Entries { get; } = [];

    /// <summary>Unknown 20 bytes (versions 0x300 and above).</summary>
    public byte[] Unknown { get; private set; } = new byte[UnknownLength];

    /// <summary>Number of LOD entries.</summary>
    public int LodEntryCount => Entries.Count;

    /// <summary>
    /// Creates an empty MODL block.
    /// </summary>
    public ModlBlock() : base()
    {
    }

    /// <summary>
    /// Creates a MODL block from raw data.
    /// </summary>
    public ModlBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Sets the unknown block used by version 0x300 and above. Must be exactly 20 bytes.
    /// </summary>
    public void SetUnknown(ReadOnlySpan<byte> value)
    {
        if (value.Length != UnknownLength)
            throw new ArgumentException($"MODL unknown block must be {UnknownLength} bytes", nameof(value));
        Unknown = value.ToArray();
    }

    /// <summary>
    /// Parses the MODL block.
    /// Source: MODL.cs lines 267-293
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid MODL tag: expected '{Tag}', got '{tag}'");

        var r = new RcolSpanReader(data, 4);
        Version = r.ReadUInt32();
        int count = r.ReadInt32();
        Bounds = r.ReadBoundingBox();

        ExtraBounds.Clear();
        Entries.Clear();
        FadeType = 0;
        CustomFadeDistance = 0;
        Unknown = new byte[UnknownLength];

        if (Version >= 258 && Version < 0x300)
        {
            int extraCount = r.ReadCount(BoundingBox.Size);
            for (int i = 0; i < extraCount; i++)
                ExtraBounds.Add(r.ReadBoundingBox());
            FadeType = r.ReadUInt32();
            CustomFadeDistance = r.ReadSingle();
        }
        else if (Version >= 0x300)
        {
            Unknown = r.ReadBytes(UnknownLength);
        }

        r.CheckCount(count, ModlLodEntry.Size);
        for (int i = 0; i < count; i++)
        {
            Entries.Add(new ModlLodEntry(
                new RcolChunkReference(r.ReadUInt32()),
                (LodInfo)r.ReadUInt32(),
                (LodId)r.ReadUInt32(),
                r.ReadSingle(),
                r.ReadSingle()));
        }
    }

    /// <summary>
    /// Serializes the MODL block.
    /// Source: MODL.cs lines 295-320
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        // Source: MODL.cs line 302 - extra bounds force at least version 258
        if (Version < 258 && ExtraBounds.Count > 0)
            Version = 258;

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'M');
        w.Write((byte)'O');
        w.Write((byte)'D');
        w.Write((byte)'L');
        w.Write(Version);
        w.Write(Entries.Count);
        w.Write(Bounds);

        if (Version >= 258 && Version < 0x300)
        {
            w.Write(ExtraBounds.Count);
            foreach (var box in ExtraBounds)
                w.Write(box);
            w.Write(FadeType);
            w.Write(CustomFadeDistance);
        }

        if (Version >= 0x300)
            w.Write(Unknown);

        foreach (var e in Entries)
        {
            w.Write(e.ModelLodIndex.RawValue);
            w.Write((uint)e.Flags);
            w.Write((uint)e.Id);
            w.Write(e.MinZValue);
            w.Write(e.MaxZValue);
        }

        return ms.ToArray();
    }
}

/// <summary>
/// A LOD entry in a MODL block.
/// Source: MODL.cs lines 86-178 (LODEntry)
/// </summary>
public readonly record struct ModlLodEntry(
    RcolChunkReference ModelLodIndex, LodInfo Flags, LodId Id, float MinZValue, float MaxZValue)
{
    /// <summary>Serialized size in bytes (five 4-byte fields).</summary>
    public const int Size = 20;
}
