// Source: legacy_references/Sims4Tools/s4pi Wrappers/s4piRCOLChunks/FTPT.cs lines 26-576

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// FTPT (Footprint) block - defines object placement footprints and slot areas.
/// Resource Type: 0xD382BF57
/// Source: FTPT.cs lines 26-576
/// </summary>
/// <remarks>
/// All lists in FTPT (height overrides, polygon points, areas) use a single-byte element count
/// (FTPT.cs lines 186-192, 260-266, 524-530), so at most 255 entries are representable.
/// </remarks>
public sealed class FtptBlock : RcolBlock
{
    /// <summary>Resource type identifier for FTPT.</summary>
    public const uint TypeId = 0xD382BF57;

    /// <summary>Maximum entries in any FTPT list (count is stored in one byte).</summary>
    public const int MaxListCount = byte.MaxValue;

    /// <inheritdoc/>
    public override string Tag => "FTPT";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (legacy default 12). Source: FTPT.cs line 31.</summary>
    public uint Version { get; set; } = 12;

    /// <summary>Template key instance. Source: FTPT.cs lines 32, 542 (TemplateKey).</summary>
    public ulong Instance { get; set; }

    /// <summary>Template key type; zero selects the area layout, non-zero the height override layout.</summary>
    public uint Type { get; set; }

    /// <summary>Template key group.</summary>
    public uint Group { get; set; }

    /// <summary>Maximum height (when Type == 0).</summary>
    public float MaxHeight { get; set; }

    /// <summary>Minimum height (when Type == 0).</summary>
    public float MinHeight { get; set; }

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
    /// Source: FTPT.cs lines 64-90
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        var r = new RcolSpanReader(data);

        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid FTPT tag: expected '{Tag}', got '{tag}'");
        r.ReadUInt32();

        Version = r.ReadUInt32();
        Instance = r.ReadUInt64();
        Type = r.ReadUInt32();
        Group = r.ReadUInt32();

        MinHeightOverrides.Clear();
        MaxHeightOverrides.Clear();
        FootprintAreas.Clear();
        SlotAreas.Clear();
        MaxHeight = 0;
        MinHeight = 0;

        if (Type != 0)
        {
            // Source: FTPT.cs lines 74-80
            ParseHeightOverrides(ref r, MinHeightOverrides);
            ParseHeightOverrides(ref r, MaxHeightOverrides);
        }
        else
        {
            // Source: FTPT.cs lines 81-89
            ParseAreas(ref r, FootprintAreas);
            ParseAreas(ref r, SlotAreas);
            MaxHeight = r.ReadSingle();
            MinHeight = r.ReadSingle();
        }
    }

    /// <summary>Source: FTPT.cs lines 183-195 (byte count, then nameHash + height).</summary>
    private static void ParseHeightOverrides(ref RcolSpanReader r, List<FtptPolygonHeightOverride> list)
    {
        int count = r.ReadByte();
        r.CheckCount(count, 8);
        for (int i = 0; i < count; i++)
        {
            uint nameHash = r.ReadUInt32();
            float height = r.ReadSingle();
            list.Add(new FtptPolygonHeightOverride(nameHash, height));
        }
    }

    /// <summary>Source: FTPT.cs lines 322-395 (Area.Parse) and 515-535 (AreaList, byte count).</summary>
    private static void ParseAreas(ref RcolSpanReader r, List<FtptArea> list)
    {
        int count = r.ReadByte();
        // Minimum area size: name(4) + priority(1) + flags(4) + pointCount(1) + 4 flag words(16) + level(1) + bbox(24)
        r.CheckCount(count, 51);
        for (int i = 0; i < count; i++)
        {
            uint name = r.ReadUInt32();
            byte priority = r.ReadByte();
            var areaTypeFlags = (FtptFootprintPolyFlags)r.ReadUInt32();

            // PolygonPointList: byte count, then (x, z) floats. Source: FTPT.cs lines 257-269
            int pointCount = r.ReadByte();
            r.CheckCount(pointCount, 8);
            var points = new List<FtptPolygonPoint>(pointCount);
            for (int j = 0; j < pointCount; j++)
            {
                float x = r.ReadSingle();
                float z = r.ReadSingle();
                points.Add(new FtptPolygonPoint(x, z));
            }

            var intersectionObjectType = (FtptIntersectionFlags)r.ReadUInt32();
            var allowIntersectionTypes = (FtptIntersectionFlags)r.ReadUInt32();
            var surfaceTypeFlags = (FtptSurfaceTypeFlags)r.ReadUInt32();
            var surfaceAttributeFlags = (FtptSurfaceAttribute)r.ReadUInt32();
            byte deprecatedLevelOffset = r.ReadByte();

            // Stored as minX, minZ, maxX, maxZ, minY, maxY. Source: FTPT.cs lines 387-394
            float minX = r.ReadSingle();
            float minZ = r.ReadSingle();
            float maxX = r.ReadSingle();
            float maxZ = r.ReadSingle();
            float minY = r.ReadSingle();
            float maxY = r.ReadSingle();
            var bounds = new BoundingBox(minX, minY, minZ, maxX, maxY, maxZ);

            list.Add(new FtptArea(name, priority, areaTypeFlags, points, intersectionObjectType,
                allowIntersectionTypes, surfaceTypeFlags, surfaceAttributeFlags, deprecatedLevelOffset, bounds));
        }
    }

    /// <summary>
    /// Serializes the FTPT block back to bytes.
    /// Source: FTPT.cs lines 91-117
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((byte)'F');
        writer.Write((byte)'T');
        writer.Write((byte)'P');
        writer.Write((byte)'T');

        writer.Write(Version);
        writer.Write(Instance);
        writer.Write(Type);
        writer.Write(Group);

        if (Type != 0)
        {
            SerializeHeightOverrides(writer, MinHeightOverrides);
            SerializeHeightOverrides(writer, MaxHeightOverrides);
        }
        else
        {
            SerializeAreas(writer, FootprintAreas);
            SerializeAreas(writer, SlotAreas);
            writer.Write(MaxHeight);
            writer.Write(MinHeight);
        }

        return ms.ToArray();
    }

    private static void WriteCount(BinaryWriter writer, int count, string what)
    {
        if (count > MaxListCount)
            throw new InvalidOperationException($"FTPT {what} list holds {count} entries; the format allows at most {MaxListCount}");
        writer.Write((byte)count);
    }

    private static void SerializeHeightOverrides(BinaryWriter writer, List<FtptPolygonHeightOverride> list)
    {
        WriteCount(writer, list.Count, "height override");
        foreach (var item in list)
        {
            writer.Write(item.NameHash);
            writer.Write(item.Height);
        }
    }

    private static void SerializeAreas(BinaryWriter writer, List<FtptArea> list)
    {
        WriteCount(writer, list.Count, "area");
        foreach (var area in list)
        {
            writer.Write(area.Name);
            writer.Write(area.Priority);
            writer.Write((uint)area.AreaTypeFlags);

            WriteCount(writer, area.PolygonPoints.Count, "polygon point");
            foreach (var point in area.PolygonPoints)
            {
                writer.Write(point.X);
                writer.Write(point.Z);
            }

            writer.Write((uint)area.IntersectionObjectType);
            writer.Write((uint)area.AllowIntersectionTypes);
            writer.Write((uint)area.SurfaceTypeFlags);
            writer.Write((uint)area.SurfaceAttributeFlags);
            writer.Write(area.DeprecatedLevelOffset);

            writer.Write(area.BoundingBox3D.Min.X);
            writer.Write(area.BoundingBox3D.Min.Z);
            writer.Write(area.BoundingBox3D.Max.X);
            writer.Write(area.BoundingBox3D.Max.Z);
            writer.Write(area.BoundingBox3D.Min.Y);
            writer.Write(area.BoundingBox3D.Max.Y);
        }
    }
}

/// <summary>
/// Footprint polygon flags.
/// Source: FTPT.cs lines 271-283 (FootprintPolyFlags)
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum FtptFootprintPolyFlags : uint
{
    /// <summary>No flags.</summary>
    None = 0,
    /// <summary>Placement polygon.</summary>
    Placement = 0x01,
    /// <summary>Pathing polygon.</summary>
    Pathing = 0x02,
    /// <summary>Enabled.</summary>
    Enabled = 0x04,
    /// <summary>Discouraged.</summary>
    Discouraged = 0x08,
    /// <summary>Landing strip.</summary>
    LandingStrip = 0x10,
    /// <summary>No raycast.</summary>
    NoRaycast = 0x20,
    /// <summary>Placement slotted.</summary>
    PlacementSlotted = 0x40,
    /// <summary>Encouraged.</summary>
    Encouraged = 0x80,
    /// <summary>Terrain cutout.</summary>
    TerrainCutout = 0x100,
}

/// <summary>
/// Intersection flags.
/// Source: FTPT.cs lines 284-300 (IntersectionFlags)
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum FtptIntersectionFlags : uint
{
    /// <summary>None.</summary>
    None = 0,
    /// <summary>Walls.</summary>
    Walls = 1 << 1,
    /// <summary>Objects.</summary>
    Objects = 1 << 2,
    /// <summary>Sims.</summary>
    Sims = 1 << 3,
    /// <summary>Roofs.</summary>
    Roofs = 1 << 4,
    /// <summary>Fences.</summary>
    Fences = 1 << 5,
    /// <summary>Modular stairs.</summary>
    ModularStairs = 1 << 6,
    /// <summary>Objects of same type.</summary>
    ObjectsOfSameType = 1 << 7,
    /// <summary>Columns.</summary>
    Columns = 1 << 8,
    /// <summary>Reserved space.</summary>
    ReservedSpace = 1 << 9,
    /// <summary>Foundations.</summary>
    Foundations = 1 << 10,
    /// <summary>Fenestration node.</summary>
    FenestrationNode = 1 << 11,
    /// <summary>Trim.</summary>
    Trim = 1 << 12,
}

/// <summary>
/// Surface type flags.
/// Source: FTPT.cs lines 301-312 (SurfaceTypeFlags)
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum FtptSurfaceTypeFlags : uint
{
    /// <summary>None.</summary>
    None = 0,
    /// <summary>Terrain.</summary>
    Terrain = 1 << 0,
    /// <summary>Floor.</summary>
    Floor = 1 << 1,
    /// <summary>Pool.</summary>
    Pool = 1 << 2,
    /// <summary>Pond.</summary>
    Pond = 1 << 3,
    /// <summary>Fence post.</summary>
    FencePost = 1 << 4,
    /// <summary>Any surface.</summary>
    AnySurface = 1 << 5,
    /// <summary>Air.</summary>
    Air = 1 << 6,
    /// <summary>Roof.</summary>
    Roof = 1 << 7,
}

/// <summary>
/// Surface attribute flags.
/// Source: FTPT.cs lines 313-321 (SurfaceAttribute)
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum FtptSurfaceAttribute : uint
{
    /// <summary>No attribute.</summary>
    None = 0x00,
    /// <summary>Inside.</summary>
    Inside = 0x01,
    /// <summary>Outside.</summary>
    Outside = 0x02,
    /// <summary>Slope.</summary>
    Slope = 0x04,
    /// <summary>Unknown 0x08.</summary>
    Unknown08 = 0x08,
}

/// <summary>
/// Polygon height override entry.
/// Source: FTPT.cs lines 120-182 (PolygonHeightOverride)
/// </summary>
public readonly record struct FtptPolygonHeightOverride(uint NameHash, float Height);

/// <summary>
/// Polygon point (X, Z coordinates).
/// Source: FTPT.cs lines 197-256 (PolygonPoint)
/// </summary>
public readonly record struct FtptPolygonPoint(float X, float Z);

/// <summary>
/// Footprint/slot area definition.
/// Source: FTPT.cs lines 322-514 (Area)
/// </summary>
public sealed class FtptArea
{
    /// <summary>Area name (FNV32 hash).</summary>
    public uint Name { get; set; }

    /// <summary>Area priority.</summary>
    public byte Priority { get; set; }

    /// <summary>Area type flags.</summary>
    public FtptFootprintPolyFlags AreaTypeFlags { get; set; }

    /// <summary>Closed polygon outline (X, Z points, at most 255).</summary>
    public List<FtptPolygonPoint> PolygonPoints { get; } = [];

    /// <summary>Intersection object type flags.</summary>
    public FtptIntersectionFlags IntersectionObjectType { get; set; }

    /// <summary>Allowed intersection types.</summary>
    public FtptIntersectionFlags AllowIntersectionTypes { get; set; }

    /// <summary>Surface type flags.</summary>
    public FtptSurfaceTypeFlags SurfaceTypeFlags { get; set; }

    /// <summary>Surface attribute flags.</summary>
    public FtptSurfaceAttribute SurfaceAttributeFlags { get; set; }

    /// <summary>Deprecated level offset (single byte).</summary>
    public byte DeprecatedLevelOffset { get; set; }

    /// <summary>3D bounding box (stored on disk as minX, minZ, maxX, maxZ, minY, maxY).</summary>
    public BoundingBox BoundingBox3D { get; set; }

    /// <summary>
    /// Creates an empty area.
    /// </summary>
    public FtptArea()
    {
    }

    /// <summary>
    /// Creates an area with all fields.
    /// </summary>
    public FtptArea(uint name, byte priority, FtptFootprintPolyFlags areaTypeFlags,
        IEnumerable<FtptPolygonPoint> polygonPoints, FtptIntersectionFlags intersectionObjectType,
        FtptIntersectionFlags allowIntersectionTypes, FtptSurfaceTypeFlags surfaceTypeFlags,
        FtptSurfaceAttribute surfaceAttributeFlags, byte deprecatedLevelOffset, BoundingBox boundingBox3D)
    {
        Name = name;
        Priority = priority;
        AreaTypeFlags = areaTypeFlags;
        PolygonPoints.AddRange(polygonPoints);
        IntersectionObjectType = intersectionObjectType;
        AllowIntersectionTypes = allowIntersectionTypes;
        SurfaceTypeFlags = surfaceTypeFlags;
        SurfaceAttributeFlags = surfaceAttributeFlags;
        DeprecatedLevelOffset = deprecatedLevelOffset;
        BoundingBox3D = boundingBox3D;
    }
}
