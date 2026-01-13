
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// FTPT (Footprint) block - defines object placement footprints and slot areas.
/// Resource Type: 0xD382BF57
/// Source: s4pi Wrappers/s4piRCOLChunks/FTPT.cs
///
/// Note: This is a simplified implementation that parses the header and preserves
/// the detailed area data. Full area parsing can be added in a future iteration.
/// </summary>
public sealed class FtptBlock : RcolBlock
{
    /// <summary>Resource type identifier for FTPT.</summary>
    public const uint TypeId = 0xD382BF57;

    /// <inheritdoc/>
    public override string Tag => "FTPT";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 12).</summary>
    public uint Version { get; private set; } = 12;

    /// <summary>Instance identifier.</summary>
    public ulong Instance { get; private set; }

    /// <summary>Type value (determines parsing mode).</summary>
    public uint Type { get; private set; }

    /// <summary>Group identifier.</summary>
    public uint Group { get; private set; }

    /// <summary>Maximum height (when Type == 0).</summary>
    public float MaxHeight { get; private set; }

    /// <summary>Minimum height (when Type == 0).</summary>
    public float MinHeight { get; private set; }

    /// <summary>Whether Type != 0 (uses height overrides instead of areas).</summary>
    public bool HasHeightOverrides => Type != 0;

    /// <summary>Min height overrides (when Type != 0).</summary>
    public List<FtptPolygonHeightOverride> MinHeightOverrides { get; } = [];

    /// <summary>Max height overrides (when Type != 0).</summary>
    public List<FtptPolygonHeightOverride> MaxHeightOverrides { get; } = [];

    /// <summary>Footprint areas (when Type == 0).</summary>
    public List<FtptArea> FootprintAreas { get; } = [];

    /// <summary>Slot areas (when Type == 0).</summary>
    public List<FtptArea> SlotAreas { get; } = [];

    /// <summary>
    /// Creates an empty FTPT block.
    /// </summary>
    public FtptBlock() : base()
    {
    }

    /// <summary>
    /// Creates a FTPT block from raw data.
    /// </summary>
    public FtptBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the FTPT block data.
    /// Source: FTPT.cs lines 70-96
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid FTPT tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Instance = BinaryPrimitives.ReadUInt64LittleEndian(data[pos..]);
        pos += 8;

        Type = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Group = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        // Clear lists
        MinHeightOverrides.Clear();
        MaxHeightOverrides.Clear();
        FootprintAreas.Clear();
        SlotAreas.Clear();

        if (Type != 0)
        {
            // Parse height overrides
            // Source: FTPT.cs lines 82-85
            ParseHeightOverrides(data, ref pos, MinHeightOverrides);
            ParseHeightOverrides(data, ref pos, MaxHeightOverrides);
        }
        else
        {
            // Parse areas
            // Source: FTPT.cs lines 91-94
            ParseAreas(data, ref pos, FootprintAreas);
            ParseAreas(data, ref pos, SlotAreas);

            if (pos + 8 <= data.Length)
            {
                MaxHeight = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
                pos += 4;
                MinHeight = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
                pos += 4;
            }
        }
    }

    private static void ParseHeightOverrides(ReadOnlySpan<byte> data, ref int pos,
        List<FtptPolygonHeightOverride> list)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        for (int i = 0; i < count; i++)
        {
            var nameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
            var height = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;
            list.Add(new FtptPolygonHeightOverride(nameHash, height));
        }
    }

    private static void ParseAreas(ReadOnlySpan<byte> data, ref int pos, List<FtptArea> list)
    {
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
        pos += 4;

        for (int i = 0; i < count; i++)
        {
            // Each area has: nameHash(4), priority(1), type(4), points list, surface types list, bounding box
            var nameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;
            var priority = data[pos++];
            var areaType = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            pos += 4;

            // Parse polygon points
            int pointCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;
            var points = new List<FtptPolygonPoint>(pointCount);
            for (int j = 0; j < pointCount; j++)
            {
                var x = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
                pos += 4;
                var z = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
                pos += 4;
                points.Add(new FtptPolygonPoint(x, z));
            }

            // Parse surface types/intersection flags
            int surfaceCount = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]);
            pos += 4;
            var surfaceTypes = new List<uint>(surfaceCount);
            for (int j = 0; j < surfaceCount; j++)
            {
                surfaceTypes.Add(BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]));
                pos += 4;
            }

            // Bounding box (4 floats: minX, minZ, maxX, maxZ)
            var minX = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;
            var minZ = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;
            var maxX = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;
            var maxZ = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
            pos += 4;

            list.Add(new FtptArea(nameHash, priority, areaType, points, surfaceTypes, minX, minZ, maxX, maxZ));
        }
    }

    /// <summary>
    /// Serializes the FTPT block back to bytes.
    /// Source: FTPT.cs lines 98-126
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'F');
        writer.Write((byte)'T');
        writer.Write((byte)'P');
        writer.Write((byte)'T');

        // Write header
        writer.Write(Version);
        writer.Write(Instance);
        writer.Write(Type);
        writer.Write(Group);

        if (Type != 0)
        {
            // Write height overrides
            SerializeHeightOverrides(writer, MinHeightOverrides);
            SerializeHeightOverrides(writer, MaxHeightOverrides);
        }
        else
        {
            // Write areas
            SerializeAreas(writer, FootprintAreas);
            SerializeAreas(writer, SlotAreas);
            writer.Write(MaxHeight);
            writer.Write(MinHeight);
        }

        return ms.ToArray();
    }

    private static void SerializeHeightOverrides(BinaryWriter writer,
        List<FtptPolygonHeightOverride> list)
    {
        writer.Write(list.Count);
        foreach (var item in list)
        {
            writer.Write(item.NameHash);
            writer.Write(item.Height);
        }
    }

    private static void SerializeAreas(BinaryWriter writer, List<FtptArea> list)
    {
        writer.Write(list.Count);
        foreach (var area in list)
        {
            writer.Write(area.NameHash);
            writer.Write(area.Priority);
            writer.Write(area.AreaType);

            writer.Write(area.Points.Count);
            foreach (var point in area.Points)
            {
                writer.Write(point.X);
                writer.Write(point.Z);
            }

            writer.Write(area.SurfaceTypes.Count);
            foreach (var surfaceType in area.SurfaceTypes)
            {
                writer.Write(surfaceType);
            }

            writer.Write(area.MinX);
            writer.Write(area.MinZ);
            writer.Write(area.MaxX);
            writer.Write(area.MaxZ);
        }
    }
}

/// <summary>
/// Polygon height override entry.
/// Source: FTPT.cs PolygonHeightOverride class
/// </summary>
public readonly struct FtptPolygonHeightOverride
{
    /// <summary>FNV32 hash of the polygon name.</summary>
    public uint NameHash { get; }
    /// <summary>Height override value.</summary>
    public float Height { get; }

    /// <summary>
    /// Creates a polygon height override.
    /// </summary>
    public FtptPolygonHeightOverride(uint nameHash, float height)
    {
        NameHash = nameHash;
        Height = height;
    }
}

/// <summary>
/// Polygon point (X, Z coordinates).
/// Source: FTPT.cs PolygonPoint class
/// </summary>
public readonly struct FtptPolygonPoint
{
    /// <summary>X coordinate.</summary>
    public float X { get; }
    /// <summary>Z coordinate.</summary>
    public float Z { get; }

    /// <summary>
    /// Creates a polygon point.
    /// </summary>
    public FtptPolygonPoint(float x, float z)
    {
        X = x;
        Z = z;
    }
}

/// <summary>
/// Footprint/slot area definition.
/// Source: FTPT.cs Area class
/// </summary>
public sealed class FtptArea
{
    /// <summary>FNV32 hash of the area name.</summary>
    public uint NameHash { get; }
    /// <summary>Area priority value.</summary>
    public byte Priority { get; }
    /// <summary>Type of area.</summary>
    public uint AreaType { get; }
    /// <summary>List of polygon points defining the area boundary.</summary>
    public IReadOnlyList<FtptPolygonPoint> Points { get; }
    /// <summary>List of surface type flags.</summary>
    public IReadOnlyList<uint> SurfaceTypes { get; }
    /// <summary>Minimum X coordinate of bounding box.</summary>
    public float MinX { get; }
    /// <summary>Minimum Z coordinate of bounding box.</summary>
    public float MinZ { get; }
    /// <summary>Maximum X coordinate of bounding box.</summary>
    public float MaxX { get; }
    /// <summary>Maximum Z coordinate of bounding box.</summary>
    public float MaxZ { get; }

    /// <summary>
    /// Creates a footprint/slot area.
    /// </summary>
    public FtptArea(uint nameHash, byte priority, uint areaType,
        IReadOnlyList<FtptPolygonPoint> points, IReadOnlyList<uint> surfaceTypes,
        float minX, float minZ, float maxX, float maxZ)
    {
        NameHash = nameHash;
        Priority = priority;
        AreaType = areaType;
        Points = points;
        SurfaceTypes = surfaceTypes;
        MinX = minX;
        MinZ = minZ;
        MaxX = maxX;
        MaxZ = maxZ;
    }
}
