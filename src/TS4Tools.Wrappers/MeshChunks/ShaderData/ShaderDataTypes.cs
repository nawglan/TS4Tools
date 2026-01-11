// Source: legacy_references/Sims4Tools/s4pi Wrappers/s4piRCOLChunks/ShaderData.cs

// Suppress CA1711, CA1720 - these match legacy s4pi naming conventions
#pragma warning disable CA1711, CA1720

namespace TS4Tools.Wrappers.MeshChunks;

/// <summary>
/// Data type for shader parameters.
/// Source: ShaderData.cs lines 234-241
/// </summary>
public enum ShaderDataType : uint
{
    Unknown = 0,
    Float = 1,
    Int = 2,
    Texture = 4,
    ImageMap = 0x00010004
}

/// <summary>
/// Field types for shader parameters.
/// Source: ShaderData.cs lines 78-232
/// </summary>
public enum ShaderFieldType : uint
{
    None = 0x00000000,

    // Float fields
    AlignAcrossDirection = 0x01885886,
    DimmingCenterHeight = 0x01ADACE0,
    Transparency = 0x05D22FD3,
    BlendSourceMode = 0x0995E96C,
    SharpSpecControl = 0x11483F01,
    RotateSpeedRadsSec = 0x16BF7A44,
    AlignToDirection = 0x17B78AF6,
    DropShadowStrength = 0x1B1AB4D5,
    ContourSmoothing = 0x1E27DCCD,
    BlendOperation = 0x2D13B939,
    RotationSpeed = 0x32003AD4,
    DimmingRadius = 0x32DFA298,
    IsGenericBox = 0x347C9E07,
    IsSolidObject = 0x3BBF99CF,
    NormalMapScale = 0x3C45E334,
    NoAutomaticDaylightDimming = 0x3CB5FA70,
    FramesPerSecond = 0x406ADE00,
    BloomFactor = 0x4168508B,
    EmissiveBloomMultiplier = 0x490E6EB4,
    IsObject = 0x4C12ECE8,
    IsPartition = 0x5250023D,
    RippleSpeed = 0x52DEC070,
    UseLampColor = 0x56B220CD,
    TextureSpeedScale = 0x583DF357,
    NoiseMapScale = 0x5E86DEA1,
    AutoRainbow = 0x5F7800EA,
    DebouncePower = 0x656025DF,
    SpeedStretchFactor = 0x66479028,
    WindSpeed = 0x66E9B6BC,
    DaytimeOnly = 0x6BB389BC,
    FramesRandomStartFactor = 0x7211F24F,
    DeflectionThreshold = 0x7D621D61,
    LifetimeSeconds = 0x84212733,
    NormalBumpScale = 0x88C64AE2,
    DeformerOffset = 0x8BDF4746,
    EdgeDarkening = 0x8C27D8C9,
    OverrideFactor = 0x8E35CCC0,
    EmissiveLightMultiplier = 0x8EF71C85,
    SharpSpecThreshold = 0x903BE4D3,
    RugSort = 0x906997A9,
    Layer2Shift = 0x92692CB2,
    SpecStyle = 0x9554D40F,
    FadeDistance = 0x957210EA,
    BlendDestMode = 0x9BDECB37,
    LightingEnabled = 0xA15E4594,
    OverrideSpeed = 0xA3D6342E,
    VisibleOnlyAtNight = 0xAC5D0A82,
    UseDiffuseForAlphaTest = 0xB597FA7F,
    SparkleSpeed = 0xBA13921E,
    WindStrength = 0xBC4A2544,
    HaloBlur = 0xC3AD4F50,
    RefractionDistortionScale = 0xC3C472A1,
    DiffuseMapUVChannel = 0xC45A5F41,
    SpecularMapUVChannel = 0xCB053686,
    ParticleCount = 0xCC31B828,
    RippleDistanceScale = 0xCCB35B98,
    DivetScale = 0xCE8C8311,
    ForceAmount = 0xD4D51D02,
    AnimSpeed = 0xD600CB63,
    BackFaceDiffuseContribution = 0xD641A1B1,
    BounceAmountMeters = 0xD8542D8B,
    IsFloor = 0xD9C05335,
    BloomScale = 0xE29BA4AC,
    AlphaMaskThreshold = 0xE77A2B60,
    LightingDirectScale = 0xEF270EE4,
    AlwaysOn = 0xF019641D,
    Shininess = 0xF755F7FF,
    FresnelOffset = 0xFB66A8CB,
    BouncePower = 0xFBA6B898,
    ShadowAlphaTest = 0xFEB1F9CB,

    // Float2 fields
    DiffuseUVScale = 0x2D4E507E,
    RippleHeights = 0x6A07D7E1,
    CutoutValidHeights = 0x6D43D7B7,
    UVTiling = 0x773CAB85,
    SizeScaleEnd = 0x891A3133,
    StretchRect = 0x8D38D12E,
    SizeScaleStart = 0x9A6C2EC8,
    WaterScrollSpeedLayer2 = 0xAFA11435,
    WaterScrollSpeedLayer1 = 0xAFA11436,
    NormalUVScale = 0xBA2D1AB9,
    DetailUVScale = 0xCD985A0B,
    SpecularUVScale = 0xF12E27C3,
    UVScrollSpeed = 0xF2EEA6EC,

    // Float3 fields
    OverrideDirection = 0x0C12DED8,
    OverrideVelocity = 0x14677578,
    CounterMatrixRow1 = 0x1EF8655D,
    CounterMatrixRow2 = 0x1EF8655E,
    ForceDirection = 0x29881F55,
    Specular = 0x2CE11842,
    HaloLowColor = 0x2EB8E8D4,
    NormalMapUVSelector = 0x415368B4,
    UVScales = 0x420520E9,
    LightMapScale = 0x4F7DCB9B,
    Diffuse = 0x637DAA05,
    AmbientUVSelector = 0x797F8E81,
    HighlightColor = 0x90F8DCF0,
    DiffuseUVSelector = 0x91EEBAFF,
    VertexColorScale = 0xA2FD73CA,
    SpecularUVSelector = 0xB63546AC,
    EmissionMapUVSelector = 0xBC823DDC,
    HaloHighColor = 0xD4043258,
    RootColor = 0xE90599F6,
    ForceVector = 0xEBA4727B,
    PositionTweak = 0xEF36D180,

    // Float4 fields
    TimelineLength = 0x0081AE98,
    UVScale = 0x159BA53E,
    FrameData = 0x1E5B2324,
    AnimDir = 0x3F89C2EF,
    PosScale = 0x487648E5,
    Births = 0x568E0367,
    UVOffset = 0x57582869,
    PosOffset = 0x790EBF2C,

    // Int fields
    AverageColor = 0x449A3A67,
    MaskWidth = 0x707F712F,
    MaskHeight = 0x849CDADC,

    // Texture fields
    SparkleCube = 0x1D90C086,
    DropShadowAtlas = 0x22AD8507,
    DirtOverlay = 0x48372E62,
    OverlayTexture = 0x4DC0C8BC,
    JetTexture = 0x52CE211B,
    ColorRamp = 0x581835D6,
    DiffuseMap = 0x6CC0FD85,
    SelfIlluminationMap = 0x6E067554,
    NormalMap = 0x6E56548A,
    HaloRamp = 0x84F6E0FB,
    DetailMap = 0x9205DAA8,
    SpecularMap = 0xAD528A60,
    AmbientOcclusionMap = 0xB01CBA60,
    AlphaMap = 0xC3FAAC4F,
    MultiplyMap = 0xCD869A45,
    SpecCompositeTexture = 0xD652FADE,
    NoiseMap = 0xE19FD579,
    RoomLightMap = 0xE7CA9166,
    EmissionMap = 0xF303D152,
    RevealMap = 0xF3F22AC4,

    // TextureKey fields
    ImposterTextureAOandSI = 0x15C9D298,
    ImpostorDetailTexture = 0x56E1C6B2,
    ImposterTexture = 0xBDCF71C5,
    ImposterTextureWater = 0xBF3FB9FA,
}

#pragma warning restore CA1711
