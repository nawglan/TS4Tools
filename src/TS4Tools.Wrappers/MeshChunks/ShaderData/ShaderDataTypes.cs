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
    /// <summary>Unknown shader data type.</summary>
    Unknown = 0,
    /// <summary>Float data type (1-4 components).</summary>
    Float = 1,
    /// <summary>Integer data type.</summary>
    Int = 2,
    /// <summary>Texture reference data type.</summary>
    Texture = 4,
    /// <summary>Image map data type.</summary>
    ImageMap = 0x00010004
}

/// <summary>
/// Field types for shader parameters.
/// Source: ShaderData.cs lines 78-232
/// </summary>
public enum ShaderFieldType : uint
{
    /// <summary>No field type.</summary>
    None = 0x00000000,

    // Float fields
    /// <summary>Align across direction (float).</summary>
    AlignAcrossDirection = 0x01885886,
    /// <summary>Dimming center height (float).</summary>
    DimmingCenterHeight = 0x01ADACE0,
    /// <summary>Transparency (float).</summary>
    Transparency = 0x05D22FD3,
    /// <summary>Blend source mode (float).</summary>
    BlendSourceMode = 0x0995E96C,
    /// <summary>Sharp specular control (float).</summary>
    SharpSpecControl = 0x11483F01,
    /// <summary>Rotation speed in radians per second (float).</summary>
    RotateSpeedRadsSec = 0x16BF7A44,
    /// <summary>Align to direction (float).</summary>
    AlignToDirection = 0x17B78AF6,
    /// <summary>Drop shadow strength (float).</summary>
    DropShadowStrength = 0x1B1AB4D5,
    /// <summary>Contour smoothing (float).</summary>
    ContourSmoothing = 0x1E27DCCD,
    /// <summary>Blend operation (float).</summary>
    BlendOperation = 0x2D13B939,
    /// <summary>Rotation speed (float).</summary>
    RotationSpeed = 0x32003AD4,
    /// <summary>Dimming radius (float).</summary>
    DimmingRadius = 0x32DFA298,
    /// <summary>Is generic box flag (float).</summary>
    IsGenericBox = 0x347C9E07,
    /// <summary>Is solid object flag (float).</summary>
    IsSolidObject = 0x3BBF99CF,
    /// <summary>Normal map scale (float).</summary>
    NormalMapScale = 0x3C45E334,
    /// <summary>No automatic daylight dimming flag (float).</summary>
    NoAutomaticDaylightDimming = 0x3CB5FA70,
    /// <summary>Frames per second (float).</summary>
    FramesPerSecond = 0x406ADE00,
    /// <summary>Bloom factor (float).</summary>
    BloomFactor = 0x4168508B,
    /// <summary>Emissive bloom multiplier (float).</summary>
    EmissiveBloomMultiplier = 0x490E6EB4,
    /// <summary>Is object flag (float).</summary>
    IsObject = 0x4C12ECE8,
    /// <summary>Is partition flag (float).</summary>
    IsPartition = 0x5250023D,
    /// <summary>Ripple speed (float).</summary>
    RippleSpeed = 0x52DEC070,
    /// <summary>Use lamp color flag (float).</summary>
    UseLampColor = 0x56B220CD,
    /// <summary>Texture speed scale (float).</summary>
    TextureSpeedScale = 0x583DF357,
    /// <summary>Noise map scale (float).</summary>
    NoiseMapScale = 0x5E86DEA1,
    /// <summary>Auto rainbow flag (float).</summary>
    AutoRainbow = 0x5F7800EA,
    /// <summary>Debounce power (float).</summary>
    DebouncePower = 0x656025DF,
    /// <summary>Speed stretch factor (float).</summary>
    SpeedStretchFactor = 0x66479028,
    /// <summary>Wind speed (float).</summary>
    WindSpeed = 0x66E9B6BC,
    /// <summary>Daytime only flag (float).</summary>
    DaytimeOnly = 0x6BB389BC,
    /// <summary>Frames random start factor (float).</summary>
    FramesRandomStartFactor = 0x7211F24F,
    /// <summary>Deflection threshold (float).</summary>
    DeflectionThreshold = 0x7D621D61,
    /// <summary>Lifetime in seconds (float).</summary>
    LifetimeSeconds = 0x84212733,
    /// <summary>Normal bump scale (float).</summary>
    NormalBumpScale = 0x88C64AE2,
    /// <summary>Deformer offset (float).</summary>
    DeformerOffset = 0x8BDF4746,
    /// <summary>Edge darkening (float).</summary>
    EdgeDarkening = 0x8C27D8C9,
    /// <summary>Override factor (float).</summary>
    OverrideFactor = 0x8E35CCC0,
    /// <summary>Emissive light multiplier (float).</summary>
    EmissiveLightMultiplier = 0x8EF71C85,
    /// <summary>Sharp specular threshold (float).</summary>
    SharpSpecThreshold = 0x903BE4D3,
    /// <summary>Rug sort (float).</summary>
    RugSort = 0x906997A9,
    /// <summary>Layer 2 shift (float).</summary>
    Layer2Shift = 0x92692CB2,
    /// <summary>Specular style (float).</summary>
    SpecStyle = 0x9554D40F,
    /// <summary>Fade distance (float).</summary>
    FadeDistance = 0x957210EA,
    /// <summary>Blend destination mode (float).</summary>
    BlendDestMode = 0x9BDECB37,
    /// <summary>Lighting enabled flag (float).</summary>
    LightingEnabled = 0xA15E4594,
    /// <summary>Override speed (float).</summary>
    OverrideSpeed = 0xA3D6342E,
    /// <summary>Visible only at night flag (float).</summary>
    VisibleOnlyAtNight = 0xAC5D0A82,
    /// <summary>Use diffuse for alpha test flag (float).</summary>
    UseDiffuseForAlphaTest = 0xB597FA7F,
    /// <summary>Sparkle speed (float).</summary>
    SparkleSpeed = 0xBA13921E,
    /// <summary>Wind strength (float).</summary>
    WindStrength = 0xBC4A2544,
    /// <summary>Halo blur (float).</summary>
    HaloBlur = 0xC3AD4F50,
    /// <summary>Refraction distortion scale (float).</summary>
    RefractionDistortionScale = 0xC3C472A1,
    /// <summary>Diffuse map UV channel (float).</summary>
    DiffuseMapUVChannel = 0xC45A5F41,
    /// <summary>Specular map UV channel (float).</summary>
    SpecularMapUVChannel = 0xCB053686,
    /// <summary>Particle count (float).</summary>
    ParticleCount = 0xCC31B828,
    /// <summary>Ripple distance scale (float).</summary>
    RippleDistanceScale = 0xCCB35B98,
    /// <summary>Divet scale (float).</summary>
    DivetScale = 0xCE8C8311,
    /// <summary>Force amount (float).</summary>
    ForceAmount = 0xD4D51D02,
    /// <summary>Animation speed (float).</summary>
    AnimSpeed = 0xD600CB63,
    /// <summary>Back face diffuse contribution (float).</summary>
    BackFaceDiffuseContribution = 0xD641A1B1,
    /// <summary>Bounce amount in meters (float).</summary>
    BounceAmountMeters = 0xD8542D8B,
    /// <summary>Is floor flag (float).</summary>
    IsFloor = 0xD9C05335,
    /// <summary>Bloom scale (float).</summary>
    BloomScale = 0xE29BA4AC,
    /// <summary>Alpha mask threshold (float).</summary>
    AlphaMaskThreshold = 0xE77A2B60,
    /// <summary>Lighting direct scale (float).</summary>
    LightingDirectScale = 0xEF270EE4,
    /// <summary>Always on flag (float).</summary>
    AlwaysOn = 0xF019641D,
    /// <summary>Shininess (float).</summary>
    Shininess = 0xF755F7FF,
    /// <summary>Fresnel offset (float).</summary>
    FresnelOffset = 0xFB66A8CB,
    /// <summary>Bounce power (float).</summary>
    BouncePower = 0xFBA6B898,
    /// <summary>Shadow alpha test (float).</summary>
    ShadowAlphaTest = 0xFEB1F9CB,

    // Float2 fields
    /// <summary>Diffuse UV scale (float2).</summary>
    DiffuseUVScale = 0x2D4E507E,
    /// <summary>Ripple heights (float2).</summary>
    RippleHeights = 0x6A07D7E1,
    /// <summary>Cutout valid heights (float2).</summary>
    CutoutValidHeights = 0x6D43D7B7,
    /// <summary>UV tiling (float2).</summary>
    UVTiling = 0x773CAB85,
    /// <summary>Size scale end (float2).</summary>
    SizeScaleEnd = 0x891A3133,
    /// <summary>Stretch rectangle (float2).</summary>
    StretchRect = 0x8D38D12E,
    /// <summary>Size scale start (float2).</summary>
    SizeScaleStart = 0x9A6C2EC8,
    /// <summary>Water scroll speed layer 2 (float2).</summary>
    WaterScrollSpeedLayer2 = 0xAFA11435,
    /// <summary>Water scroll speed layer 1 (float2).</summary>
    WaterScrollSpeedLayer1 = 0xAFA11436,
    /// <summary>Normal UV scale (float2).</summary>
    NormalUVScale = 0xBA2D1AB9,
    /// <summary>Detail UV scale (float2).</summary>
    DetailUVScale = 0xCD985A0B,
    /// <summary>Specular UV scale (float2).</summary>
    SpecularUVScale = 0xF12E27C3,
    /// <summary>UV scroll speed (float2).</summary>
    UVScrollSpeed = 0xF2EEA6EC,

    // Float3 fields
    /// <summary>Override direction (float3).</summary>
    OverrideDirection = 0x0C12DED8,
    /// <summary>Override velocity (float3).</summary>
    OverrideVelocity = 0x14677578,
    /// <summary>Counter matrix row 1 (float3).</summary>
    CounterMatrixRow1 = 0x1EF8655D,
    /// <summary>Counter matrix row 2 (float3).</summary>
    CounterMatrixRow2 = 0x1EF8655E,
    /// <summary>Force direction (float3).</summary>
    ForceDirection = 0x29881F55,
    /// <summary>Specular color (float3).</summary>
    Specular = 0x2CE11842,
    /// <summary>Halo low color (float3).</summary>
    HaloLowColor = 0x2EB8E8D4,
    /// <summary>Normal map UV selector (float3).</summary>
    NormalMapUVSelector = 0x415368B4,
    /// <summary>UV scales (float3).</summary>
    UVScales = 0x420520E9,
    /// <summary>Light map scale (float3).</summary>
    LightMapScale = 0x4F7DCB9B,
    /// <summary>Diffuse color (float3).</summary>
    Diffuse = 0x637DAA05,
    /// <summary>Ambient UV selector (float3).</summary>
    AmbientUVSelector = 0x797F8E81,
    /// <summary>Highlight color (float3).</summary>
    HighlightColor = 0x90F8DCF0,
    /// <summary>Diffuse UV selector (float3).</summary>
    DiffuseUVSelector = 0x91EEBAFF,
    /// <summary>Vertex color scale (float3).</summary>
    VertexColorScale = 0xA2FD73CA,
    /// <summary>Specular UV selector (float3).</summary>
    SpecularUVSelector = 0xB63546AC,
    /// <summary>Emission map UV selector (float3).</summary>
    EmissionMapUVSelector = 0xBC823DDC,
    /// <summary>Halo high color (float3).</summary>
    HaloHighColor = 0xD4043258,
    /// <summary>Root color (float3).</summary>
    RootColor = 0xE90599F6,
    /// <summary>Force vector (float3).</summary>
    ForceVector = 0xEBA4727B,
    /// <summary>Position tweak (float3).</summary>
    PositionTweak = 0xEF36D180,

    // Float4 fields
    /// <summary>Timeline length (float4).</summary>
    TimelineLength = 0x0081AE98,
    /// <summary>UV scale (float4).</summary>
    UVScale = 0x159BA53E,
    /// <summary>Frame data (float4).</summary>
    FrameData = 0x1E5B2324,
    /// <summary>Animation direction (float4).</summary>
    AnimDir = 0x3F89C2EF,
    /// <summary>Position scale (float4).</summary>
    PosScale = 0x487648E5,
    /// <summary>Births (float4).</summary>
    Births = 0x568E0367,
    /// <summary>UV offset (float4).</summary>
    UVOffset = 0x57582869,
    /// <summary>Position offset (float4).</summary>
    PosOffset = 0x790EBF2C,

    // Int fields
    /// <summary>Average color (int).</summary>
    AverageColor = 0x449A3A67,
    /// <summary>Mask width (int).</summary>
    MaskWidth = 0x707F712F,
    /// <summary>Mask height (int).</summary>
    MaskHeight = 0x849CDADC,

    // Texture fields
    /// <summary>Sparkle cube texture.</summary>
    SparkleCube = 0x1D90C086,
    /// <summary>Drop shadow atlas texture.</summary>
    DropShadowAtlas = 0x22AD8507,
    /// <summary>Dirt overlay texture.</summary>
    DirtOverlay = 0x48372E62,
    /// <summary>Overlay texture.</summary>
    OverlayTexture = 0x4DC0C8BC,
    /// <summary>Jet texture.</summary>
    JetTexture = 0x52CE211B,
    /// <summary>Color ramp texture.</summary>
    ColorRamp = 0x581835D6,
    /// <summary>Diffuse map texture.</summary>
    DiffuseMap = 0x6CC0FD85,
    /// <summary>Self-illumination map texture.</summary>
    SelfIlluminationMap = 0x6E067554,
    /// <summary>Normal map texture.</summary>
    NormalMap = 0x6E56548A,
    /// <summary>Halo ramp texture.</summary>
    HaloRamp = 0x84F6E0FB,
    /// <summary>Detail map texture.</summary>
    DetailMap = 0x9205DAA8,
    /// <summary>Specular map texture.</summary>
    SpecularMap = 0xAD528A60,
    /// <summary>Ambient occlusion map texture.</summary>
    AmbientOcclusionMap = 0xB01CBA60,
    /// <summary>Alpha map texture.</summary>
    AlphaMap = 0xC3FAAC4F,
    /// <summary>Multiply map texture.</summary>
    MultiplyMap = 0xCD869A45,
    /// <summary>Specular composite texture.</summary>
    SpecCompositeTexture = 0xD652FADE,
    /// <summary>Noise map texture.</summary>
    NoiseMap = 0xE19FD579,
    /// <summary>Room light map texture.</summary>
    RoomLightMap = 0xE7CA9166,
    /// <summary>Emission map texture.</summary>
    EmissionMap = 0xF303D152,
    /// <summary>Reveal map texture.</summary>
    RevealMap = 0xF3F22AC4,

    // TextureKey fields
    /// <summary>Imposter texture with ambient occlusion and self-illumination.</summary>
    ImposterTextureAOandSI = 0x15C9D298,
    /// <summary>Impostor detail texture.</summary>
    ImpostorDetailTexture = 0x56E1C6B2,
    /// <summary>Imposter texture.</summary>
    ImposterTexture = 0xBDCF71C5,
    /// <summary>Imposter texture for water.</summary>
    ImposterTextureWater = 0xBF3FB9FA,
}

#pragma warning restore CA1711
