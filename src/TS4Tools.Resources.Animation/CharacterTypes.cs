namespace TS4Tools.Resources.Animation;

/// <summary>
/// Types of character resources supported in The Sims 4.
/// </summary>
public enum CharacterType
{
    /// <summary>
    /// No character type specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Character Asset System part.
    /// </summary>
    CasPart = 0x00000001,

    /// <summary>
    /// Character outfit definition.
    /// </summary>
    Outfit = 0x00000002,

    /// <summary>
    /// Bone deformation data.
    /// </summary>
    Bone = 0x00000003,

    /// <summary>
    /// Skin tone definition.
    /// </summary>
    SkinTone = 0x00000004,

    /// <summary>
    /// Character preset.
    /// </summary>
    Preset = 0x00000005,

    /// <summary>
    /// Sim modifier data.
    /// </summary>
    SimModifier = 0x00000006,

    /// <summary>
    /// Deformer map data.
    /// </summary>
    DeformerMap = 0x00000007,

    /// <summary>
    /// Animal coat data.
    /// </summary>
    AnimalCoat = 0x00000008
}

/// <summary>
/// Age categories for Sim characters.
/// </summary>
public enum AgeCategory
{
    /// <summary>
    /// No age specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Baby age.
    /// </summary>
    Baby = 1,

    /// <summary>
    /// Toddler age.
    /// </summary>
    Toddler = 2,

    /// <summary>
    /// Child age.
    /// </summary>
    Child = 4,

    /// <summary>
    /// Teen age.
    /// </summary>
    Teen = 8,

    /// <summary>
    /// Young adult age.
    /// </summary>
    YoungAdult = 16,

    /// <summary>
    /// Adult age.
    /// </summary>
    Adult = 32,

    /// <summary>
    /// Elder age.
    /// </summary>
    Elder = 64,

    /// <summary>
    /// All age categories.
    /// </summary>
    All = 127
}

/// <summary>
/// Gender types for Sim characters.
/// </summary>
public enum Gender
{
    /// <summary>
    /// No gender specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Male gender.
    /// </summary>
    Male = 1,

    /// <summary>
    /// Female gender.
    /// </summary>
    Female = 2,

    /// <summary>
    /// Both genders (unisex).
    /// </summary>
    Unisex = 3
}

/// <summary>
/// Species types for characters.
/// </summary>
public enum Species
{
    /// <summary>
    /// No species specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Human Sim.
    /// </summary>
    Human = 1,

    /// <summary>
    /// Dog.
    /// </summary>
    Dog = 2,

    /// <summary>
    /// Cat.
    /// </summary>
    Cat = 3,

    /// <summary>
    /// Horse.
    /// </summary>
    Horse = 4,

    /// <summary>
    /// Other animal or creature.
    /// </summary>
    Other = 99,

    /// <summary>
    /// All species.
    /// </summary>
    All = 255
}

/// <summary>
/// Character part categories.
/// </summary>
public enum PartCategory
{
    /// <summary>
    /// No category specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Hair part.
    /// </summary>
    Hair = 1,

    /// <summary>
    /// Head part.
    /// </summary>
    Head = 2,

    /// <summary>
    /// Body part.
    /// </summary>
    Body = 3,

    /// <summary>
    /// Top clothing.
    /// </summary>
    Top = 4,

    /// <summary>
    /// Bottom clothing.
    /// </summary>
    Bottom = 5,

    /// <summary>
    /// Full body clothing.
    /// </summary>
    FullBody = 6,

    /// <summary>
    /// Shoes.
    /// </summary>
    Shoes = 7,

    /// <summary>
    /// Accessories.
    /// </summary>
    Accessories = 8,

    /// <summary>
    /// Makeup.
    /// </summary>
    Makeup = 9,

    /// <summary>
    /// Eyebrows.
    /// </summary>
    Eyebrows = 10,

    /// <summary>
    /// Eyes.
    /// </summary>
    Eyes = 11,

    /// <summary>
    /// Skin details.
    /// </summary>
    Skin = 12,

    /// <summary>
    /// Facial features.
    /// </summary>
    Facial = 13,

    /// <summary>
    /// Occult traits.
    /// </summary>
    Occult = 14
}

/// <summary>
/// Represents a character part used in CAS.
/// </summary>
/// <param name="InstanceId">Unique identifier for this part.</param>
/// <param name="Category">Category of the part.</param>
/// <param name="Name">Display name of the part.</param>
/// <param name="AgeCategory">Supported age categories.</param>
/// <param name="Gender">Supported genders.</param>
/// <param name="Species">Supported species.</param>
/// <param name="SortPriority">Display sort priority.</param>
public readonly record struct CharacterPart(
    uint InstanceId,
    PartCategory Category,
    string Name,
    AgeCategory AgeCategory,
    Gender Gender,
    Species Species,
    int SortPriority);

/// <summary>
/// Represents clothing or outfit flags.
/// </summary>
[Flags]
public enum OutfitFlags
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// Valid for random generation.
    /// </summary>
    ValidForRandom = 0x00000001,

    /// <summary>
    /// Valid for sleepwear.
    /// </summary>
    ValidForSleepwear = 0x00000002,

    /// <summary>
    /// Valid for swimwear.
    /// </summary>
    ValidForSwimwear = 0x00000004,

    /// <summary>
    /// Valid for athletic wear.
    /// </summary>
    ValidForAthletic = 0x00000008,

    /// <summary>
    /// Valid for party wear.
    /// </summary>
    ValidForParty = 0x00000010,

    /// <summary>
    /// Valid for maternity wear.
    /// </summary>
    ValidForMaternity = 0x00000020,

    /// <summary>
    /// Allows material variations.
    /// </summary>
    AllowsVariations = 0x00000040,

    /// <summary>
    /// Supports color customization.
    /// </summary>
    SupportsColors = 0x00000080
}
