
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Light source types for LITE blocks.
/// Source: LITE.cs LightSourceType enum, lines 255-269
/// </summary>
public enum LightSourceType : uint
{
    /// <summary>Unknown light type.</summary>
    Unknown = 0x00,
    /// <summary>Ambient light.</summary>
    Ambient = 0x01,
    /// <summary>Directional light.</summary>
    Directional = 0x02,
    /// <summary>Point light.</summary>
    Point = 0x03,
    /// <summary>Spot light.</summary>
    Spot = 0x04,
    /// <summary>Lamp shade light.</summary>
    LampShade = 0x05,
    /// <summary>Tube light.</summary>
    TubeLight = 0x06,
    /// <summary>Square window light.</summary>
    SquareWindow = 0x07,
    /// <summary>Circular window light.</summary>
    CircularWindow = 0x08,
    /// <summary>Square area light.</summary>
    SquareAreaLight = 0x09,
    /// <summary>Disc area light.</summary>
    DiscAreaLight = 0x0A,
    /// <summary>World light.</summary>
    WorldLight = 0x0B,
}

/// <summary>
/// Occluder types for LITE blocks.
/// Source: LITE.cs OccluderType enum, lines 703-707
/// </summary>
public enum OccluderType : uint
{
    /// <summary>Disc-shaped occluder.</summary>
    Disc = 0x00,
    /// <summary>Rectangle-shaped occluder.</summary>
    Rectangle = 0x01,
}

/// <summary>
/// LITE (Light) block - defines light sources and shadow occluders.
/// Resource Type: 0x03B4C61D
/// Source: s4pi Wrappers/s4piRCOLChunks/LITE.cs
/// </summary>
public sealed class LiteBlock : RcolBlock
{
    /// <summary>Resource type identifier for LITE.</summary>
    public const uint TypeId = 0x03B4C61D;

    /// <summary>Size of a light source in bytes.</summary>
    private const int LightSourceSize = 128; // type(4) + transform(12) + color(12) + intensity(4) + data(96)

    /// <summary>Size of an occluder in bytes.</summary>
    private const int OccluderSize = 56; // type(4) + origin(12) + normal(12) + xAxis(12) + yAxis(12) + pairOffset(4)

    /// <inheritdoc/>
    public override string Tag => "LITE";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 4).</summary>
    public uint Version { get; private set; } = 4;

    /// <summary>Unknown value 1 (typically 0x84).</summary>
    public uint Unknown1 { get; private set; } = 0x84;

    /// <summary>Unknown value 2.</summary>
    public ushort Unknown2 { get; private set; }

    /// <summary>List of light sources.</summary>
    public List<LiteLightSource> LightSources { get; } = [];

    /// <summary>List of shadow occluders.</summary>
    public List<LiteOccluder> Occluders { get; } = [];

    /// <summary>
    /// Creates an empty LITE block.
    /// </summary>
    public LiteBlock() : base()
    {
    }

    /// <summary>
    /// Creates a LITE block from raw data.
    /// </summary>
    public LiteBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the LITE block data.
    /// Source: LITE.cs lines 62-75
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid LITE tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Unknown1 = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        byte lightSourceCount = data[pos++];
        byte occluderCount = data[pos++];

        Unknown2 = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]);
        pos += 2;

        // Validate counts
        if (lightSourceCount > 255 || occluderCount > 255)
            throw new InvalidDataException($"Invalid LITE counts: lights={lightSourceCount}, occluders={occluderCount}");

        // Parse light sources
        // Source: LITE.cs lines 163-173
        LightSources.Clear();
        for (int i = 0; i < lightSourceCount; i++)
        {
            if (pos + LightSourceSize > data.Length)
                throw new InvalidDataException($"LITE data too short for light source {i}");

            LightSources.Add(ParseLightSource(data, ref pos));
        }

        // Parse occluders
        // Source: LITE.cs lines 639-648
        Occluders.Clear();
        for (int i = 0; i < occluderCount; i++)
        {
            if (pos + OccluderSize > data.Length)
                throw new InvalidDataException($"LITE data too short for occluder {i}");

            Occluders.Add(ParseOccluder(data, ref pos));
        }
    }

    private static LiteLightSource ParseLightSource(ReadOnlySpan<byte> data, ref int pos)
    {
        var lightType = (LightSourceType)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        var transform = MeshVector3.Read(data, ref pos);
        var color = MeshVector3.Read(data, ref pos);
        var intensity = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;

        // Read 24 floats of light source data
        var lightData = new float[24];
        for (int j = 0; j < 24; j++)
        {
            lightData[j] = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;
        }

        return new LiteLightSource(lightType, transform, color, intensity, lightData);
    }

    private static LiteOccluder ParseOccluder(ReadOnlySpan<byte> data, ref int pos)
    {
        var occluderType = (OccluderType)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        var origin = MeshVector3.Read(data, ref pos);
        var normal = MeshVector3.Read(data, ref pos);
        var xAxis = MeshVector3.Read(data, ref pos);
        var yAxis = MeshVector3.Read(data, ref pos);

        var pairOffset = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
        pos += 4;

        return new LiteOccluder(occluderType, origin, normal, xAxis, yAxis, pairOffset);
    }

    /// <summary>
    /// Serializes the LITE block back to bytes.
    /// Source: LITE.cs lines 77-94
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'L');
        writer.Write((byte)'I');
        writer.Write((byte)'T');
        writer.Write((byte)'E');

        // Write header
        writer.Write(Version);
        writer.Write(Unknown1);
        writer.Write((byte)LightSources.Count);
        writer.Write((byte)Occluders.Count);
        writer.Write(Unknown2);

        // Write light sources
        foreach (var ls in LightSources)
        {
            SerializeLightSource(writer, ls);
        }

        // Write occluders
        foreach (var occ in Occluders)
        {
            SerializeOccluder(writer, occ);
        }

        return ms.ToArray();
    }

    private static void SerializeLightSource(BinaryWriter writer, LiteLightSource ls)
    {
        writer.Write((uint)ls.LightType);
        writer.Write(ls.Transform.X);
        writer.Write(ls.Transform.Y);
        writer.Write(ls.Transform.Z);
        writer.Write(ls.Color.X);
        writer.Write(ls.Color.Y);
        writer.Write(ls.Color.Z);
        writer.Write(ls.Intensity);
        foreach (var f in ls.LightData)
        {
            writer.Write(f);
        }
    }

    private static void SerializeOccluder(BinaryWriter writer, LiteOccluder occ)
    {
        writer.Write((uint)occ.OccluderType);
        writer.Write(occ.Origin.X);
        writer.Write(occ.Origin.Y);
        writer.Write(occ.Origin.Z);
        writer.Write(occ.Normal.X);
        writer.Write(occ.Normal.Y);
        writer.Write(occ.Normal.Z);
        writer.Write(occ.XAxis.X);
        writer.Write(occ.XAxis.Y);
        writer.Write(occ.XAxis.Z);
        writer.Write(occ.YAxis.X);
        writer.Write(occ.YAxis.Y);
        writer.Write(occ.YAxis.Z);
        writer.Write(occ.PairOffset);
    }
}

/// <summary>
/// A light source in a LITE block.
/// Source: LITE.cs LightSource class, lines 98-584
/// </summary>
public sealed class LiteLightSource
{
    /// <summary>Type of light source.</summary>
    public LightSourceType LightType { get; }

    /// <summary>Position transform.</summary>
    public MeshVector3 Transform { get; }

    /// <summary>RGB color (as X=R, Y=G, Z=B).</summary>
    public MeshVector3 Color { get; }

    /// <summary>Light intensity.</summary>
    public float Intensity { get; }

    /// <summary>Type-specific light data (24 floats).</summary>
    public float[] LightData { get; }

    /// <summary>
    /// Creates a light source.
    /// </summary>
    public LiteLightSource(LightSourceType lightType, MeshVector3 transform, MeshVector3 color,
        float intensity, float[] lightData)
    {
        if (lightData.Length != 24)
            throw new ArgumentException("LightData must have exactly 24 elements", nameof(lightData));

        LightType = lightType;
        Transform = transform;
        Color = color;
        Intensity = intensity;
        LightData = lightData;
    }
}

/// <summary>
/// A shadow occluder in a LITE block.
/// Source: LITE.cs Occluder class, lines 606-726
/// </summary>
public sealed class LiteOccluder
{
    /// <summary>Type of occluder (Disc or Rectangle).</summary>
    public OccluderType OccluderType { get; }

    /// <summary>Origin point.</summary>
    public MeshVector3 Origin { get; }

    /// <summary>Normal vector.</summary>
    public MeshVector3 Normal { get; }

    /// <summary>X axis vector.</summary>
    public MeshVector3 XAxis { get; }

    /// <summary>Y axis vector.</summary>
    public MeshVector3 YAxis { get; }

    /// <summary>Pair offset distance.</summary>
    public float PairOffset { get; }

    /// <summary>
    /// Creates a shadow occluder.
    /// </summary>
    public LiteOccluder(OccluderType occluderType, MeshVector3 origin, MeshVector3 normal,
        MeshVector3 xAxis, MeshVector3 yAxis, float pairOffset)
    {
        OccluderType = occluderType;
        Origin = origin;
        Normal = normal;
        XAxis = xAxis;
        YAxis = yAxis;
        PairOffset = pairOffset;
    }
}
