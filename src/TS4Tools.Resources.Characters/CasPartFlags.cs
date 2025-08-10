/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

namespace TS4Tools.Resources.Characters;

/// <summary>
/// Flags that control how a CAS part behaves and appears in the UI.
/// These flags determine visibility, randomization, and accessibility.
/// </summary>
[Flags]
public enum CasPartFlags : byte
{
    /// <summary>Disables this part for the opposite gender (legacy flag)</summary>
    DisableForOppositeGender = 1 << 7,

    /// <summary>Allows this part to be used in live mode randomization</summary>
    AllowForLiveRandom = 1 << 6,

    /// <summary>Shows this part in CAS demo mode</summary>
    ShowInCasDemo = 1 << 5,

    /// <summary>Shows this part in the Sim info panel</summary>
    ShowInSimInfoPanel = 1 << 4,

    /// <summary>Shows this part in the UI (visible to players)</summary>
    ShowInUi = 1 << 3,

    /// <summary>Allows this part for CAS randomization</summary>
    AllowForCasRandom = 1 << 2,

    /// <summary>Uses this part as the default thumbnail</summary>
    DefaultThumbnailPart = 1 << 1,

    /// <summary>Default for body type (deprecated, use specific gender flags)</summary>
    DefaultForBodyTypeDeprecated = 1
}

/// <summary>
/// Additional parameter flags for CAS parts.
/// </summary>
[Flags]
public enum CasPartFlags2 : byte
{
    /// <summary>Default for female body type</summary>
    DefaultForBodyTypeFemale = 1 << 2,

    /// <summary>Default for male body type</summary>
    DefaultForBodyTypeMale = 1 << 1,

    /// <summary>Disable for opposite frame (slim vs normal)</summary>
    DisableForOppositeFrame = 1
}

/// <summary>
/// Age and gender flags that determine which Sims can use this CAS part.
/// Multiple flags can be combined to support multiple demographics.
/// </summary>
[Flags]
public enum AgeGenderFlags : uint
{
    /// <summary>Available for baby Sims</summary>
    Baby = 0x00000001,

    /// <summary>Available for toddler Sims</summary>
    Toddler = 0x00000002,

    /// <summary>Available for child Sims</summary>
    Child = 0x00000004,

    /// <summary>Available for teen Sims</summary>
    Teen = 0x00000008,

    /// <summary>Available for young adult Sims</summary>
    YoungAdult = 0x00000010,

    /// <summary>Available for adult Sims</summary>
    Adult = 0x00000020,

    /// <summary>Available for elder Sims</summary>
    Elder = 0x00000040,

    /// <summary>Available for male Sims</summary>
    Male = 0x00001000,

    /// <summary>Available for female Sims</summary>
    Female = 0x00002000,

    /// <summary>All age groups combined</summary>
    AllAges = Baby | Toddler | Child | Teen | YoungAdult | Adult | Elder,

    /// <summary>All genders combined</summary>
    AllGenders = Male | Female,

    /// <summary>All demographics (age and gender) combined</summary>
    All = AllAges | AllGenders
}

/// <summary>
/// Species types for CAS parts.
/// Determines which species can use the part.
/// </summary>
public enum SpeciesType : uint
{
    /// <summary>Human Sims</summary>
    Human = 1,

    /// <summary>Large dog pets</summary>
    Dog = 2,

    /// <summary>Cat pets</summary>
    Cat = 3,

    /// <summary>Small dog pets</summary>
    LittleDog = 4
}

/// <summary>
/// Species flags for multi-species compatibility.
/// </summary>
[Flags]
public enum SpeciesFlags : uint
{
    /// <summary>Large dog compatibility</summary>
    LargeDog = 4,

    /// <summary>Cat compatibility</summary>
    Cat = 8,

    /// <summary>Small dog compatibility</summary>
    SmallDog = 16
}

/// <summary>
/// Body type categories that define what part of the Sim this affects.
/// Determines placement and conflict resolution with other parts.
/// </summary>
public enum BodyType : uint
{
    /// <summary>Affects all body parts (rare)</summary>
    All = 0,

    /// <summary>Hat/headwear</summary>
    Hat = 1,

    /// <summary>Hair</summary>
    Hair = 2,

    /// <summary>Head shape/structure</summary>
    Head = 3,

    /// <summary>Face shape/structure</summary>
    Face = 4,

    /// <summary>Body shape/clothing</summary>
    Body = 5,

    /// <summary>Top clothing</summary>
    Top = 6,

    /// <summary>Bottom clothing</summary>
    Bottom = 7,

    /// <summary>Shoes</summary>
    Shoes = 8,

    /// <summary>General accessories</summary>
    Accessories = 9,

    /// <summary>Earrings</summary>
    Earrings = 0x0A,

    /// <summary>Glasses</summary>
    Glasses = 0x0B,

    /// <summary>Necklace</summary>
    Necklace = 0x0C,

    /// <summary>Gloves</summary>
    Gloves = 0x0D,

    /// <summary>Left wrist bracelet</summary>
    BraceletLeft = 0x0E,

    /// <summary>Right wrist bracelet</summary>
    BraceletRight = 0x0F,

    /// <summary>Left lip piercing</summary>
    LipRingLeft = 0x10,

    /// <summary>Right lip piercing</summary>
    LipRingRight = 0x11,

    /// <summary>Nose piercing</summary>
    NoseRing = 0x12,

    /// <summary>Left eyebrow piercing</summary>
    EyebrowPiercingLeft = 0x13,

    /// <summary>Right eyebrow piercing</summary>
    EyebrowPiercingRight = 0x14,

    /// <summary>Lip color/makeup</summary>
    Lipstick = 0x15,

    /// <summary>Facial hair</summary>
    FacialHair = 0x16,

    /// <summary>Eyelashes</summary>
    Eyelashes = 0x17,

    /// <summary>Eyeshadow</summary>
    Eyeshadow = 0x18,

    /// <summary>Mascara</summary>
    Mascara = 0x19,

    /// <summary>Eyeliner</summary>
    Eyeliner = 0x1A,

    /// <summary>Blush</summary>
    Blush = 0x1B,

    /// <summary>Foundation</summary>
    Foundation = 0x1C,

    /// <summary>Nail polish</summary>
    NailPolish = 0x1D,

    /// <summary>Toenail polish</summary>
    ToenailPolish = 0x1E,

    /// <summary>Body hair</summary>
    BodyHair = 0x1F
}

/// <summary>
/// Flags that control which parts should be excluded when this part is active.
/// Used for conflict resolution between overlapping parts.
/// </summary>
[Flags]
public enum ExcludePartFlags : ulong
{
    /// <summary>No exclusions</summary>
    None = 0,

    /// <summary>Exclude hat parts</summary>
    Hat = 1UL << (int)BodyType.Hat,

    /// <summary>Exclude hair parts</summary>
    Hair = 1UL << (int)BodyType.Hair,

    /// <summary>Exclude head parts</summary>
    Head = 1UL << (int)BodyType.Head,

    /// <summary>Exclude face parts</summary>
    Face = 1UL << (int)BodyType.Face,

    /// <summary>Exclude body parts</summary>
    Body = 1UL << (int)BodyType.Body,

    /// <summary>Exclude top clothing</summary>
    Top = 1UL << (int)BodyType.Top,

    /// <summary>Exclude bottom clothing</summary>
    Bottom = 1UL << (int)BodyType.Bottom,

    /// <summary>Exclude shoes</summary>
    Shoes = 1UL << (int)BodyType.Shoes,

    /// <summary>Exclude accessories</summary>
    Accessories = 1UL << (int)BodyType.Accessories,

    /// <summary>Exclude earrings</summary>
    Earrings = 1UL << (int)BodyType.Earrings,

    /// <summary>Exclude glasses</summary>
    Glasses = 1UL << (int)BodyType.Glasses,

    /// <summary>Exclude necklaces</summary>
    Necklace = 1UL << (int)BodyType.Necklace
}
