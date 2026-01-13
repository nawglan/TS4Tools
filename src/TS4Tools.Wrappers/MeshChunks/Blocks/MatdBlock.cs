
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Shader types for MATD blocks.
/// Source: ShaderData.cs ShaderType enum
/// </summary>
public enum MatdShaderType : uint
{
    /// <summary>No shader.</summary>
    None = 0x00000000,
    /// <summary>Subtractive blending shader.</summary>
    Subtractive = 0x0B272CC5,
    /// <summary>Instanced rendering shader.</summary>
    Instanced = 0x0CB82EB8,
    /// <summary>Full bright shader.</summary>
    FullBright = 0x14FA335E,
    /// <summary>Preview walls and floors shader.</summary>
    PreviewWallsAndFloors = 0x213D6300,
    /// <summary>Shadow map shader.</summary>
    ShadowMap = 0x21FE207D,
    /// <summary>Glass for rabbit holes shader.</summary>
    GlassForRabbitHoles = 0x265FFAA1,
    /// <summary>Impostor water shader.</summary>
    ImpostorWater = 0x277CF8EB,
    /// <summary>Rug shader.</summary>
    Rug = 0x2A72B9A1,
    /// <summary>Trampoline shader.</summary>
    Trampoline = 0x3939E094,
    /// <summary>Foliage shader.</summary>
    Foliage = 0x4549E22E,
    /// <summary>Particle animation shader.</summary>
    ParticleAnim = 0x460E93F4,
    /// <summary>Solid Phong shader.</summary>
    SolidPhong = 0x47C6638C,
    /// <summary>Glass for objects shader.</summary>
    GlassForObjects = 0x492ECA7C,
    /// <summary>Stairs shader.</summary>
    Stairs = 0x4CE2F497,
    /// <summary>Outdoor prop shader.</summary>
    OutdoorProp = 0x4D26BEC0,
    /// <summary>Glass for fences shader.</summary>
    GlassForFences = 0x52986C62,
    /// <summary>Sim skin shader.</summary>
    SimSkin = 0x548394B9,
    /// <summary>Additive blending shader.</summary>
    Additive = 0x5AF16731,
    /// <summary>Sim glass shader.</summary>
    SimGlass = 0x5EDA9CDE,
    /// <summary>Fence shader.</summary>
    Fence = 0x67107FE8,
    /// <summary>Lot imposter shader.</summary>
    LotImposter = 0x68601DE3,
    /// <summary>Blueprint shader.</summary>
    Blueprint = 0x6864A45E,
    /// <summary>Basin water shader.</summary>
    BasinWater = 0x6AAD2AD5,
    /// <summary>Standing water shader.</summary>
    StandingWater = 0x70FDE012,
    /// <summary>Building window shader.</summary>
    BuildingWindow = 0x7B036C01,
    /// <summary>Roof shader.</summary>
    Roof = 0x7BD05F63,
    /// <summary>Glass for portals shader.</summary>
    GlassForPortals = 0x81DD204D,
    /// <summary>Glass for objects translucent shader.</summary>
    GlassForObjectsTranslucent = 0x849CF021,
    /// <summary>Sim hair shader.</summary>
    SimHair = 0x84FD7152,
    /// <summary>Landmark shader.</summary>
    Landmark = 0x8A60B969,
    /// <summary>Rabbit hole high detail shader.</summary>
    RabbitHoleHighDetail = 0x8D346BBC,
    /// <summary>CAS room shader.</summary>
    CASRoom = 0x94B9A835,
    /// <summary>Sim eyelashes shader.</summary>
    SimEyelashes = 0x9D9DA161,
    /// <summary>Gemstones shader.</summary>
    Gemstones = 0xA063C1D0,
    /// <summary>Counters shader.</summary>
    Counters = 0xA4172F62,
    /// <summary>Flat mirror shader.</summary>
    FlatMirror = 0xA68D9E29,
    /// <summary>Painting shader.</summary>
    Painting = 0xAA495821,
    /// <summary>Rabbit hole medium detail shader.</summary>
    RabbitHoleMediumDetail = 0xAEDE7105,
    /// <summary>Phong shader.</summary>
    Phong = 0xB9105A6D,
    /// <summary>Floors shader.</summary>
    Floors = 0xBC84D000,
    /// <summary>Drop shadow shader.</summary>
    DropShadow = 0xC09C7582,
    /// <summary>Sim eyes shader.</summary>
    SimEyes = 0xCF8A70B4,
    /// <summary>Plumbob shader.</summary>
    Plumbob = 0xDEF16564,
    /// <summary>Sculpture ice shader.</summary>
    SculptureIce = 0xE5D98507,
    /// <summary>Phong alpha shader.</summary>
    PhongAlpha = 0xFC5FC212,
    /// <summary>Particle jet shader.</summary>
    ParticleJet = 0xFF5E6908,
}

/// <summary>
/// MATD (Material Definition) block - defines material and shader properties.
/// Resource Type: 0x01D0E75D
/// Source: s4pi Wrappers/s4piRCOLChunks/MATD.cs
/// </summary>
public sealed class MatdBlock : RcolBlock
{
    /// <summary>Resource type identifier for MATD.</summary>
    public const uint TypeId = 0x01D0E75D;

    /// <summary>Version threshold for new format with video/painting surface flags.</summary>
    private const uint NewFormatVersion = 0x00000103;

    /// <inheritdoc/>
    public override string Tag => "MATD";

    /// <inheritdoc/>
    public override uint ResourceType => TypeId;

    /// <inheritdoc/>
    public override bool IsKnownType => true;

    /// <summary>Format version (typically 0x103 for TS4).</summary>
    public uint Version { get; set; } = 0x103;

    /// <summary>Material name hash (FNV hash of material name).</summary>
    public uint MaterialNameHash { get; set; }

    /// <summary>Shader type used by this material.</summary>
    public MatdShaderType Shader { get; set; }

    /// <summary>Whether this is a video surface (version >= 0x103 only).</summary>
    public bool IsVideoSurface { get; set; }

    /// <summary>Whether this is a painting surface (version >= 0x103 only).</summary>
    public bool IsPaintingSurface { get; set; }

    /// <summary>Whether the new format is used (version >= 0x103).</summary>
    public bool IsNewFormat => Version >= NewFormatVersion;

    /// <summary>The raw MTRL block data (for round-tripping when parsing fails).</summary>
    public byte[] MtrlData { get; set; } = [];

    /// <summary>Parsed shader data from MTNF block. Null if parsing failed.</summary>
    public MtnfData? MaterialData { get; set; }

    /// <summary>
    /// Creates an empty MATD block.
    /// </summary>
    public MatdBlock() : base()
    {
    }

    /// <summary>
    /// Creates a MATD block from raw data.
    /// </summary>
    public MatdBlock(ReadOnlySpan<byte> data) : base(data)
    {
    }

    /// <summary>
    /// Parses the MATD block data.
    /// Source: MATD.cs lines 104-130
    /// </summary>
    protected override void Parse(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        // Validate tag
        string tag = ExtractTag(data);
        if (tag != Tag)
            throw new InvalidDataException($"Invalid MATD tag: expected '{Tag}', got '{tag}'");
        pos += 4;

        // Read header
        Version = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        MaterialNameHash = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        Shader = (MatdShaderType)BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        uint mtrlLength = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
        pos += 4;

        if (Version >= NewFormatVersion)
        {
            // New format has video/painting surface flags before MTRL
            IsVideoSurface = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]) != 0;
            pos += 4;
            IsPaintingSurface = BinaryPrimitives.ReadInt32LittleEndian(data[pos..]) != 0;
            pos += 4;
        }
        else
        {
            IsVideoSurface = false;
            IsPaintingSurface = false;
        }

        // Store the MTRL data for round-tripping
        // mtrlLength is the length of the MTRL block data
        if (mtrlLength > 0 && pos + mtrlLength <= data.Length)
        {
            MtrlData = data.Slice(pos, (int)mtrlLength).ToArray();
        }
        else if (pos < data.Length)
        {
            // If length seems wrong, take remaining data
            MtrlData = data[pos..].ToArray();
        }

        // Try to parse the MTNF/MTRL data into structured form
        if (MtrlData.Length > 0)
        {
            try
            {
                MaterialData = MtnfData.Parse(MtrlData);
            }
            catch
            {
                // Parsing failed, but we have raw data for round-tripping
                MaterialData = null;
            }
        }
    }

    /// <summary>
    /// Serializes the MATD block back to bytes.
    /// Source: MATD.cs lines 132-165
    /// </summary>
    public override ReadOnlyMemory<byte> Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write tag
        writer.Write((byte)'M');
        writer.Write((byte)'A');
        writer.Write((byte)'T');
        writer.Write((byte)'D');

        // Write header
        writer.Write(Version);
        writer.Write(MaterialNameHash);
        writer.Write((uint)Shader);

        // Serialize MTRL data - prefer structured MaterialData if available
        byte[] mtrlBytes = MaterialData?.Serialize() ?? MtrlData;
        writer.Write((uint)mtrlBytes.Length);

        if (Version >= NewFormatVersion)
        {
            writer.Write(IsVideoSurface ? 1 : 0);
            writer.Write(IsPaintingSurface ? 1 : 0);
        }

        // Write MTRL data
        if (mtrlBytes.Length > 0)
        {
            writer.Write(mtrlBytes);
        }

        return ms.ToArray();
    }
}
