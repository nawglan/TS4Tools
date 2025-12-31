
namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Shader types for MATD blocks.
/// Source: ShaderData.cs ShaderType enum
/// </summary>
public enum MatdShaderType : uint
{
    None = 0x00000000,
    Subtractive = 0x0B272CC5,
    Instanced = 0x0CB82EB8,
    FullBright = 0x14FA335E,
    PreviewWallsAndFloors = 0x213D6300,
    ShadowMap = 0x21FE207D,
    GlassForRabbitHoles = 0x265FFAA1,
    ImpostorWater = 0x277CF8EB,
    Rug = 0x2A72B9A1,
    Trampoline = 0x3939E094,
    Foliage = 0x4549E22E,
    ParticleAnim = 0x460E93F4,
    SolidPhong = 0x47C6638C,
    GlassForObjects = 0x492ECA7C,
    Stairs = 0x4CE2F497,
    OutdoorProp = 0x4D26BEC0,
    GlassForFences = 0x52986C62,
    SimSkin = 0x548394B9,
    Additive = 0x5AF16731,
    SimGlass = 0x5EDA9CDE,
    Fence = 0x67107FE8,
    LotImposter = 0x68601DE3,
    Blueprint = 0x6864A45E,
    BasinWater = 0x6AAD2AD5,
    StandingWater = 0x70FDE012,
    BuildingWindow = 0x7B036C01,
    Roof = 0x7BD05F63,
    GlassForPortals = 0x81DD204D,
    GlassForObjectsTranslucent = 0x849CF021,
    SimHair = 0x84FD7152,
    Landmark = 0x8A60B969,
    RabbitHoleHighDetail = 0x8D346BBC,
    CASRoom = 0x94B9A835,
    SimEyelashes = 0x9D9DA161,
    Gemstones = 0xA063C1D0,
    Counters = 0xA4172F62,
    FlatMirror = 0xA68D9E29,
    Painting = 0xAA495821,
    RabbitHoleMediumDetail = 0xAEDE7105,
    Phong = 0xB9105A6D,
    Floors = 0xBC84D000,
    DropShadow = 0xC09C7582,
    SimEyes = 0xCF8A70B4,
    Plumbob = 0xDEF16564,
    SculptureIce = 0xE5D98507,
    PhongAlpha = 0xFC5FC212,
    ParticleJet = 0xFF5E6908,
}

/// <summary>
/// MATD (Material Definition) block - defines material and shader properties.
/// Resource Type: 0x01D0E75D
/// Source: s4pi Wrappers/s4piRCOLChunks/MATD.cs
///
/// Note: This is a simplified implementation that parses the header and preserves
/// the MTRL data as raw bytes for round-tripping. Full shader data parsing can be
/// added in a future iteration.
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
    public uint Version { get; private set; } = 0x103;

    /// <summary>Material name hash (FNV hash of material name).</summary>
    public uint MaterialNameHash { get; private set; }

    /// <summary>Shader type used by this material.</summary>
    public MatdShaderType Shader { get; private set; }

    /// <summary>Whether this is a video surface (version >= 0x103 only).</summary>
    public bool IsVideoSurface { get; private set; }

    /// <summary>Whether this is a painting surface (version >= 0x103 only).</summary>
    public bool IsPaintingSurface { get; private set; }

    /// <summary>Whether the new format is used (version >= 0x103).</summary>
    public bool IsNewFormat => Version >= NewFormatVersion;

    /// <summary>The raw MTRL block data (for round-tripping).</summary>
    public byte[] MtrlData { get; private set; } = [];

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

        // Calculate MTRL length
        int mtrlLength = MtrlData.Length;
        writer.Write((uint)mtrlLength);

        if (Version >= NewFormatVersion)
        {
            writer.Write(IsVideoSurface ? 1 : 0);
            writer.Write(IsPaintingSurface ? 1 : 0);
        }

        // Write MTRL data
        if (MtrlData.Length > 0)
        {
            writer.Write(MtrlData);
        }

        return ms.ToArray();
    }
}
