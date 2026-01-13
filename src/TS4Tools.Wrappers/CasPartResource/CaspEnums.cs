// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPFlags.cs


namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Pack icon visibility flag.
/// Source: CASPFlags.cs lines 26-29
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum PackFlag : byte
{
    /// <summary>No flags set.</summary>
    None = 0,
    /// <summary>Hide the pack icon in CAS.</summary>
    HidePackIcon = 1
}

/// <summary>
/// CAS part visibility and behavior flags.
/// Source: CASPFlags.cs lines 31-40
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum ParmFlag : byte
{
    /// <summary>No flags set.</summary>
    None = 0,
    /// <summary>Default part for body type.</summary>
    DefaultForBodyType = 1 << 0,
    /// <summary>Default thumbnail part.</summary>
    DefaultThumbnailPart = 1 << 1,
    /// <summary>Allow for random selection.</summary>
    AllowForRandom = 1 << 2,
    /// <summary>Show in UI.</summary>
    ShowInUI = 1 << 3,
    /// <summary>Show in Sim info demo.</summary>
    ShowInSimInfoDemo = 1 << 4,
    /// <summary>Show in CAS demo.</summary>
    ShowInCASDemo = 1 << 5
}

/// <summary>
/// Age and gender flags for CAS parts.
/// Source: CASPFlags.cs lines 42-54
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum AgeGenderFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,
    /// <summary>Baby age.</summary>
    Baby = 0x00000001,
    /// <summary>Toddler age.</summary>
    Toddler = 0x00000002,
    /// <summary>Child age.</summary>
    Child = 0x00000004,
    /// <summary>Teen age.</summary>
    Teen = 0x00000008,
    /// <summary>Young adult age.</summary>
    YoungAdult = 0x00000010,
    /// <summary>Adult age.</summary>
    Adult = 0x00000020,
    /// <summary>Elder age.</summary>
    Elder = 0x00000040,
    /// <summary>Male gender.</summary>
    Male = 0x00001000,
    /// <summary>Female gender.</summary>
    Female = 0x00002000
}

/// <summary>
/// Body type for CAS parts.
/// Source: CASPFlags.cs lines 56-117
/// </summary>
public enum BodyType : uint
{
    /// <summary>All body types.</summary>
    All = 0,
    /// <summary>Hat body type.</summary>
    Hat = 1,
    /// <summary>Hair body type.</summary>
    Hair = 2,
    /// <summary>Head body type.</summary>
    Head = 3,
    /// <summary>Face body type.</summary>
    Face = 4,
    /// <summary>Body (full body) body type.</summary>
    Body = 5,
    /// <summary>Top (upper body) body type.</summary>
    Top = 6,
    /// <summary>Bottom (lower body) body type.</summary>
    Bottom = 7,
    /// <summary>Shoes body type.</summary>
    Shoes = 8,
    /// <summary>Accessories body type.</summary>
    Accessories = 9,
    /// <summary>Earrings body type.</summary>
    Earrings = 0x0A,
    /// <summary>Glasses body type.</summary>
    Glasses = 0x0B,
    /// <summary>Necklace body type.</summary>
    Necklace = 0x0C,
    /// <summary>Gloves body type.</summary>
    Gloves = 0x0D,
    /// <summary>Left bracelet body type.</summary>
    BraceletLeft = 0x0E,
    /// <summary>Right bracelet body type.</summary>
    BraceletRight = 0x0F,
    /// <summary>Left lip ring body type.</summary>
    LipRingLeft = 0x10,
    /// <summary>Right lip ring body type.</summary>
    LipRingRight = 0x11,
    /// <summary>Left nose ring body type.</summary>
    NoseRingLeft = 0x12,
    /// <summary>Right nose ring body type.</summary>
    NoseRingRight = 0x13,
    /// <summary>Left brow ring body type.</summary>
    BrowRingLeft = 0x14,
    /// <summary>Right brow ring body type.</summary>
    BrowRingRight = 0x15,
    /// <summary>Left index finger ring body type.</summary>
    RingIndexLeft = 0x16,
    /// <summary>Right index finger ring body type.</summary>
    RingIndexRight = 0x17,
    /// <summary>Left third finger ring body type.</summary>
    RingThirdLeft = 0x18,
    /// <summary>Right third finger ring body type.</summary>
    RingThirdRight = 0x19,
    /// <summary>Left middle finger ring body type.</summary>
    RingMidLeft = 0x1A,
    /// <summary>Right middle finger ring body type.</summary>
    RingMidRight = 0x1B,
    /// <summary>Facial hair body type.</summary>
    FacialHair = 0x1C,
    /// <summary>Lipstick body type.</summary>
    Lipstick = 0x1D,
    /// <summary>Eyeshadow body type.</summary>
    Eyeshadow = 0x1E,
    /// <summary>Eyeliner body type.</summary>
    Eyeliner = 0x1F,
    /// <summary>Blush body type.</summary>
    Blush = 0x20,
    /// <summary>Face paint body type.</summary>
    Facepaint = 0x21,
    /// <summary>Eyebrows body type.</summary>
    Eyebrows = 0x22,
    /// <summary>Eye color body type.</summary>
    Eyecolor = 0x23,
    /// <summary>Socks body type.</summary>
    Socks = 0x24,
    /// <summary>Mascara body type.</summary>
    Mascara = 0x25,
    /// <summary>Forehead crease body type.</summary>
    ForeheadCrease = 0x26,
    /// <summary>Freckles body type.</summary>
    Freckles = 0x27,
    /// <summary>Left dimple body type.</summary>
    DimpleLeft = 0x28,
    /// <summary>Right dimple body type.</summary>
    DimpleRight = 0x29,
    /// <summary>Tights body type.</summary>
    Tights = 0x2A,
    /// <summary>Left lip mole body type.</summary>
    MoleLeftLip = 0x2B,
    /// <summary>Right lip mole body type.</summary>
    MoleRightLip = 0x2C,
    /// <summary>Lower left arm tattoo body type.</summary>
    TattooArmLowerLeft = 0x2D,
    /// <summary>Upper left arm tattoo body type.</summary>
    TattooArmUpperLeft = 0x2E,
    /// <summary>Lower right arm tattoo body type.</summary>
    TattooArmLowerRight = 0x2F,
    /// <summary>Upper right arm tattoo body type.</summary>
    TattooArmUpperRight = 0x30,
    /// <summary>Left leg tattoo body type.</summary>
    TattooLegLeft = 0x31,
    /// <summary>Right leg tattoo body type.</summary>
    TattooLegRight = 0x32,
    /// <summary>Lower back torso tattoo body type.</summary>
    TattooTorsoBackLower = 0x33,
    /// <summary>Upper back torso tattoo body type.</summary>
    TattooTorsoBackUpper = 0x34,
    /// <summary>Lower front torso tattoo body type.</summary>
    TattooTorsoFrontLower = 0x35,
    /// <summary>Upper front torso tattoo body type.</summary>
    TattooTorsoFrontUpper = 0x36,
    /// <summary>Left cheek mole body type.</summary>
    MoleLeftCheek = 0x37,
    /// <summary>Right cheek mole body type.</summary>
    MoleRightCheek = 0x38,
    /// <summary>Mouth crease body type.</summary>
    MouthCrease = 0x39,
    /// <summary>Skin overlay body type.</summary>
    SkinOverlay = 0x3A
}

/// <summary>
/// Occult types that can be disabled for a CAS part.
/// Source: CASPFlags.cs lines 119-124
/// </summary>
[Flags]
public enum OccultTypesDisabled : uint
{
    /// <summary>No occult types disabled.</summary>
    None = 0,
    /// <summary>Human occult type.</summary>
    Human = 1,
    /// <summary>Alien occult type.</summary>
    Alien = 1 << 1
}

/// <summary>
/// Body part exclusion flags (64-bit).
/// Used to specify which body types this part excludes.
/// Source: CASPFlags.cs lines 126-188
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum ExcludePartFlag : ulong
{
    /// <summary>No parts excluded.</summary>
    None = 0,
    /// <summary>Exclude hat parts.</summary>
    Hat = 1ul << 1,
    /// <summary>Exclude hair parts.</summary>
    Hair = 1ul << 2,
    /// <summary>Exclude head parts.</summary>
    Head = 1ul << 3,
    /// <summary>Exclude face parts.</summary>
    Face = 1ul << 4,
    /// <summary>Exclude full body parts.</summary>
    FullBody = 1ul << 5,
    /// <summary>Exclude upper body parts.</summary>
    UpperBody = 1ul << 6,
    /// <summary>Exclude lower body parts.</summary>
    LowerBody = 1ul << 7,
    /// <summary>Exclude shoes parts.</summary>
    Shoes = 1ul << 8,
    /// <summary>Exclude accessories parts.</summary>
    Accessories = 1ul << 9,
    /// <summary>Exclude earrings parts.</summary>
    Earrings = 1ul << 10,
    /// <summary>Exclude glasses parts.</summary>
    Glasses = 1ul << 11,
    /// <summary>Exclude necklace parts.</summary>
    Necklace = 1ul << 12,
    /// <summary>Exclude gloves parts.</summary>
    Gloves = 1ul << 13,
    /// <summary>Exclude left wrist parts.</summary>
    WristLeft = 1ul << 14,
    /// <summary>Exclude right wrist parts.</summary>
    WristRight = 1ul << 15,
    /// <summary>Exclude left lip ring parts.</summary>
    LipRingLeft = 1ul << 16,
    /// <summary>Exclude right lip ring parts.</summary>
    LipRingRight = 1ul << 17,
    /// <summary>Exclude left nose ring parts.</summary>
    NoseRingLeft = 1ul << 18,
    /// <summary>Exclude right nose ring parts.</summary>
    NoseRingRight = 1ul << 19,
    /// <summary>Exclude left brow ring parts.</summary>
    BrowRingLeft = 1ul << 20,
    /// <summary>Exclude right brow ring parts.</summary>
    BrowRingRight = 1ul << 21,
    /// <summary>Exclude left index finger parts.</summary>
    IndexFingerLeft = 1ul << 22,
    /// <summary>Exclude right index finger parts.</summary>
    IndexFingerRight = 1ul << 23,
    /// <summary>Exclude left ring finger parts.</summary>
    RingFingerLeft = 1ul << 24,
    /// <summary>Exclude right ring finger parts.</summary>
    RingFingerRight = 1ul << 25,
    /// <summary>Exclude left middle finger parts.</summary>
    MiddleFingerLeft = 1ul << 26,
    /// <summary>Exclude right middle finger parts.</summary>
    MiddleFingerRight = 1ul << 27,
    /// <summary>Exclude facial hair parts.</summary>
    FacialHair = 1ul << 28,
    /// <summary>Exclude lipstick parts.</summary>
    Lipstick = 1ul << 29,
    /// <summary>Exclude eyeshadow parts.</summary>
    Eyeshadow = 1ul << 30,
    /// <summary>Exclude eyeliner parts.</summary>
    Eyeliner = 1ul << 31,
    /// <summary>Exclude blush parts.</summary>
    Blush = 1ul << 32,
    /// <summary>Exclude face paint parts.</summary>
    Facepaint = 1ul << 33,
    /// <summary>Exclude eyebrows parts.</summary>
    Eyebrows = 1ul << 34,
    /// <summary>Exclude eye color parts.</summary>
    Eyecolor = 1ul << 35,
    /// <summary>Exclude socks parts.</summary>
    Socks = 1ul << 36,
    /// <summary>Exclude mascara parts.</summary>
    Mascara = 1ul << 37,
    /// <summary>Exclude forehead crease parts.</summary>
    CreaseForehead = 1ul << 38,
    /// <summary>Exclude freckles parts.</summary>
    Freckles = 1ul << 39,
    /// <summary>Exclude left dimple parts.</summary>
    DimpleLeft = 1ul << 40,
    /// <summary>Exclude right dimple parts.</summary>
    DimpleRight = 1ul << 41,
    /// <summary>Exclude tights parts.</summary>
    Tights = 1ul << 42,
    /// <summary>Exclude left lip mole parts.</summary>
    MoleLipLeft = 1ul << 43,
    /// <summary>Exclude right lip mole parts.</summary>
    MoleLipRight = 1ul << 44,
    /// <summary>Exclude lower left arm tattoo parts.</summary>
    TattooArmLowerLeft = 1ul << 45,
    /// <summary>Exclude upper left arm tattoo parts.</summary>
    TattooArmUpperLeft = 1ul << 46,
    /// <summary>Exclude lower right arm tattoo parts.</summary>
    TattooArmLowerRight = 1ul << 47,
    /// <summary>Exclude upper right arm tattoo parts.</summary>
    TattooArmUpperRight = 1ul << 48,
    /// <summary>Exclude left leg tattoo parts.</summary>
    TattooLegLeft = 1ul << 49,
    /// <summary>Exclude right leg tattoo parts.</summary>
    TattooLegRight = 1ul << 50,
    /// <summary>Exclude lower back torso tattoo parts.</summary>
    TattooTorsoBackLower = 1ul << 51,
    /// <summary>Exclude upper back torso tattoo parts.</summary>
    TattooTorsoBackUpper = 1ul << 52,
    /// <summary>Exclude lower front torso tattoo parts.</summary>
    TattooTorsoFrontLower = 1ul << 53,
    /// <summary>Exclude upper front torso tattoo parts.</summary>
    TattooTorsoFrontUpper = 1ul << 54,
    /// <summary>Exclude left cheek mole parts.</summary>
    MoleCheekLeft = 1ul << 55,
    /// <summary>Exclude right cheek mole parts.</summary>
    MoleCheekRight = 1ul << 56,
    /// <summary>Exclude mouth crease parts.</summary>
    CreaseMouth = 1ul << 57,
    /// <summary>Exclude skin overlay parts.</summary>
    SkinOverlay = 1ul << 58
}

/// <summary>
/// CAS panel group type flags.
/// Source: CASPFlags.cs lines 190-212
/// </summary>
[Flags]
public enum CASPanelGroupType : uint
{
    /// <summary>Unknown value 0.</summary>
    Unknown0 = 0,
    /// <summary>Unknown value 1.</summary>
    Unknown1 = 1,
    /// <summary>Head and ears panel group.</summary>
    HeadAndEars = 2,
    /// <summary>Unknown value 3.</summary>
    Unknown3 = 4,
    /// <summary>Mouth panel group.</summary>
    Mouth = 8,
    /// <summary>Nose panel group.</summary>
    Nose = 16,
    /// <summary>Unknown value 6.</summary>
    Unknown6 = 32,
    /// <summary>Eyelash panel group.</summary>
    Eyelash = 64,
    /// <summary>Eyes panel group.</summary>
    Eyes = 128,
    /// <summary>Unknown value 9.</summary>
    Unknown9 = 256,
    /// <summary>Unknown value A.</summary>
    UnknownA = 512,
    /// <summary>Unknown value B.</summary>
    UnknownB = 1024,
    /// <summary>Unknown value C.</summary>
    UnknownC = 2048,
    /// <summary>Unknown value D.</summary>
    UnknownD = 4096,
    /// <summary>Unknown value E.</summary>
    UnknownE = 8192,
    /// <summary>Unknown value F.</summary>
    UnknownF = 16384
}

/// <summary>
/// CAS panel sort type flags.
/// Source: CASPFlags.cs lines 214-236
/// </summary>
[Flags]
public enum CASPanelSortType : uint
{
    /// <summary>Unknown value 0.</summary>
    Unknown0 = 0,
    /// <summary>Unknown value 1.</summary>
    Unknown1 = 1,
    /// <summary>Unknown value 2.</summary>
    Unknown2 = 2,
    /// <summary>Unknown value 3.</summary>
    Unknown3 = 4,
    /// <summary>Unknown value 4.</summary>
    Unknown4 = 8,
    /// <summary>Unknown value 5.</summary>
    Unknown5 = 16,
    /// <summary>Unknown value 6.</summary>
    Unknown6 = 32,
    /// <summary>Unknown value 7.</summary>
    Unknown7 = 64,
    /// <summary>Unknown value 8.</summary>
    Unknown8 = 128,
    /// <summary>Unknown value 9.</summary>
    Unknown9 = 256,
    /// <summary>Unknown value A.</summary>
    UnknownA = 512,
    /// <summary>Unknown value B.</summary>
    UnknownB = 1024,
    /// <summary>Unknown value C.</summary>
    UnknownC = 2048,
    /// <summary>Unknown value D.</summary>
    UnknownD = 4096,
    /// <summary>Unknown value E.</summary>
    UnknownE = 8192,
    /// <summary>Unknown value F.</summary>
    UnknownF = 16384
}

/// <summary>
/// CAS part region enum used in RegionMap / GEOMListResource.
/// Source: CASPFlags.cs lines 238-264
/// </summary>
public enum CASPartRegion : uint
{
    /// <summary>
    /// "Base" sub-part by definition does not compete with any other subparts.
    /// </summary>
    Base = 0,
    /// <summary>Ankle region.</summary>
    Ankle,
    /// <summary>Calf region.</summary>
    Calf,
    /// <summary>Knee region.</summary>
    Knee,
    /// <summary>Left hand region.</summary>
    HandLeft,
    /// <summary>Left wrist region.</summary>
    WristLeft,
    /// <summary>Left bicep region.</summary>
    BicepLeft,
    /// <summary>Low belt region.</summary>
    BeltLow,
    /// <summary>High belt region.</summary>
    BeltHigh,
    /// <summary>Hair/hat region A.</summary>
    HairHatA,
    /// <summary>Hair/hat region B.</summary>
    HairHatB,
    /// <summary>Hair/hat region C.</summary>
    HairHatC,
    /// <summary>Hair/hat region D.</summary>
    HairHatD,
    /// <summary>Neck region.</summary>
    Neck,
    /// <summary>Chest region.</summary>
    Chest,
    /// <summary>Stomach region.</summary>
    Stomach,
    /// <summary>Right hand region.</summary>
    HandRight,
    /// <summary>Right wrist region.</summary>
    WristRight,
    /// <summary>Right bicep region.</summary>
    BicepRight,
    /// <summary>Necklace shadow region.</summary>
    NecklaceShadow,
    /// <summary>Maximum region value.</summary>
    Max
}
