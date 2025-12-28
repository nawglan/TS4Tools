// Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/CASPFlags.cs

using System.Diagnostics.CodeAnalysis;

namespace TS4Tools.Wrappers.CasPartResource;

/// <summary>
/// Pack icon visibility flag.
/// Source: CASPFlags.cs lines 26-29
/// </summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Legacy name compatibility")]
public enum PackFlag : byte
{
    None = 0,
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
    None = 0,
    DefaultForBodyType = 1 << 0,
    DefaultThumbnailPart = 1 << 1,
    AllowForRandom = 1 << 2,
    ShowInUI = 1 << 3,
    ShowInSimInfoDemo = 1 << 4,
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
    None = 0,
    Baby = 0x00000001,
    Toddler = 0x00000002,
    Child = 0x00000004,
    Teen = 0x00000008,
    YoungAdult = 0x00000010,
    Adult = 0x00000020,
    Elder = 0x00000040,
    Male = 0x00001000,
    Female = 0x00002000
}

/// <summary>
/// Body type for CAS parts.
/// Source: CASPFlags.cs lines 56-117
/// </summary>
public enum BodyType : uint
{
    All = 0,
    Hat = 1,
    Hair = 2,
    Head = 3,
    Face = 4,
    Body = 5,
    Top = 6,
    Bottom = 7,
    Shoes = 8,
    Accessories = 9,
    Earrings = 0x0A,
    Glasses = 0x0B,
    Necklace = 0x0C,
    Gloves = 0x0D,
    BraceletLeft = 0x0E,
    BraceletRight = 0x0F,
    LipRingLeft = 0x10,
    LipRingRight = 0x11,
    NoseRingLeft = 0x12,
    NoseRingRight = 0x13,
    BrowRingLeft = 0x14,
    BrowRingRight = 0x15,
    RingIndexLeft = 0x16,
    RingIndexRight = 0x17,
    RingThirdLeft = 0x18,
    RingThirdRight = 0x19,
    RingMidLeft = 0x1A,
    RingMidRight = 0x1B,
    FacialHair = 0x1C,
    Lipstick = 0x1D,
    Eyeshadow = 0x1E,
    Eyeliner = 0x1F,
    Blush = 0x20,
    Facepaint = 0x21,
    Eyebrows = 0x22,
    Eyecolor = 0x23,
    Socks = 0x24,
    Mascara = 0x25,
    ForeheadCrease = 0x26,
    Freckles = 0x27,
    DimpleLeft = 0x28,
    DimpleRight = 0x29,
    Tights = 0x2A,
    MoleLeftLip = 0x2B,
    MoleRightLip = 0x2C,
    TattooArmLowerLeft = 0x2D,
    TattooArmUpperLeft = 0x2E,
    TattooArmLowerRight = 0x2F,
    TattooArmUpperRight = 0x30,
    TattooLegLeft = 0x31,
    TattooLegRight = 0x32,
    TattooTorsoBackLower = 0x33,
    TattooTorsoBackUpper = 0x34,
    TattooTorsoFrontLower = 0x35,
    TattooTorsoFrontUpper = 0x36,
    MoleLeftCheek = 0x37,
    MoleRightCheek = 0x38,
    MouthCrease = 0x39,
    SkinOverlay = 0x3A
}

/// <summary>
/// Occult types that can be disabled for a CAS part.
/// Source: CASPFlags.cs lines 119-124
/// </summary>
[Flags]
public enum OccultTypesDisabled : uint
{
    None = 0,
    Human = 1,
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
    None = 0,
    Hat = 1ul << 1,
    Hair = 1ul << 2,
    Head = 1ul << 3,
    Face = 1ul << 4,
    FullBody = 1ul << 5,
    UpperBody = 1ul << 6,
    LowerBody = 1ul << 7,
    Shoes = 1ul << 8,
    Accessories = 1ul << 9,
    Earrings = 1ul << 10,
    Glasses = 1ul << 11,
    Necklace = 1ul << 12,
    Gloves = 1ul << 13,
    WristLeft = 1ul << 14,
    WristRight = 1ul << 15,
    LipRingLeft = 1ul << 16,
    LipRingRight = 1ul << 17,
    NoseRingLeft = 1ul << 18,
    NoseRingRight = 1ul << 19,
    BrowRingLeft = 1ul << 20,
    BrowRingRight = 1ul << 21,
    IndexFingerLeft = 1ul << 22,
    IndexFingerRight = 1ul << 23,
    RingFingerLeft = 1ul << 24,
    RingFingerRight = 1ul << 25,
    MiddleFingerLeft = 1ul << 26,
    MiddleFingerRight = 1ul << 27,
    FacialHair = 1ul << 28,
    Lipstick = 1ul << 29,
    Eyeshadow = 1ul << 30,
    Eyeliner = 1ul << 31,
    Blush = 1ul << 32,
    Facepaint = 1ul << 33,
    Eyebrows = 1ul << 34,
    Eyecolor = 1ul << 35,
    Socks = 1ul << 36,
    Mascara = 1ul << 37,
    CreaseForehead = 1ul << 38,
    Freckles = 1ul << 39,
    DimpleLeft = 1ul << 40,
    DimpleRight = 1ul << 41,
    Tights = 1ul << 42,
    MoleLipLeft = 1ul << 43,
    MoleLipRight = 1ul << 44,
    TattooArmLowerLeft = 1ul << 45,
    TattooArmUpperLeft = 1ul << 46,
    TattooArmLowerRight = 1ul << 47,
    TattooArmUpperRight = 1ul << 48,
    TattooLegLeft = 1ul << 49,
    TattooLegRight = 1ul << 50,
    TattooTorsoBackLower = 1ul << 51,
    TattooTorsoBackUpper = 1ul << 52,
    TattooTorsoFrontLower = 1ul << 53,
    TattooTorsoFrontUpper = 1ul << 54,
    MoleCheekLeft = 1ul << 55,
    MoleCheekRight = 1ul << 56,
    CreaseMouth = 1ul << 57,
    SkinOverlay = 1ul << 58
}

/// <summary>
/// CAS panel group type flags.
/// Source: CASPFlags.cs lines 190-212
/// </summary>
[Flags]
public enum CASPanelGroupType : uint
{
    Unknown0 = 0,
    Unknown1 = 1,
    HeadAndEars = 2,
    Unknown3 = 4,
    Mouth = 8,
    Nose = 16,
    Unknown6 = 32,
    Eyelash = 64,
    Eyes = 128,
    Unknown9 = 256,
    UnknownA = 512,
    UnknownB = 1024,
    UnknownC = 2048,
    UnknownD = 4096,
    UnknownE = 8192,
    UnknownF = 16384
}

/// <summary>
/// CAS panel sort type flags.
/// Source: CASPFlags.cs lines 214-236
/// </summary>
[Flags]
public enum CASPanelSortType : uint
{
    Unknown0 = 0,
    Unknown1 = 1,
    Unknown2 = 2,
    Unknown3 = 4,
    Unknown4 = 8,
    Unknown5 = 16,
    Unknown6 = 32,
    Unknown7 = 64,
    Unknown8 = 128,
    Unknown9 = 256,
    UnknownA = 512,
    UnknownB = 1024,
    UnknownC = 2048,
    UnknownD = 4096,
    UnknownE = 8192,
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
    Ankle,
    Calf,
    Knee,
    HandLeft,
    WristLeft,
    BicepLeft,
    BeltLow,
    BeltHigh,
    HairHatA,
    HairHatB,
    HairHatC,
    HairHatD,
    Neck,
    Chest,
    Stomach,
    HandRight,
    WristRight,
    BicepRight,
    NecklaceShadow,
    Max
}
