using FluentAssertions;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Tests for CAS Part enums to verify they match legacy values.
/// </summary>
public class CaspEnumsTests
{
    [Theory]
    [InlineData(ParmFlag.DefaultForBodyType, 0x01)]
    [InlineData(ParmFlag.DefaultThumbnailPart, 0x02)]
    [InlineData(ParmFlag.AllowForRandom, 0x04)]
    [InlineData(ParmFlag.ShowInUI, 0x08)]
    [InlineData(ParmFlag.ShowInSimInfoDemo, 0x10)]
    [InlineData(ParmFlag.ShowInCASDemo, 0x20)]
    public void ParmFlag_HasCorrectValues(ParmFlag flag, byte expected)
    {
        ((byte)flag).Should().Be(expected);
    }

    [Theory]
    [InlineData(AgeGenderFlags.Baby, 0x00000001)]
    [InlineData(AgeGenderFlags.Toddler, 0x00000002)]
    [InlineData(AgeGenderFlags.Child, 0x00000004)]
    [InlineData(AgeGenderFlags.Teen, 0x00000008)]
    [InlineData(AgeGenderFlags.YoungAdult, 0x00000010)]
    [InlineData(AgeGenderFlags.Adult, 0x00000020)]
    [InlineData(AgeGenderFlags.Elder, 0x00000040)]
    [InlineData(AgeGenderFlags.Male, 0x00001000)]
    [InlineData(AgeGenderFlags.Female, 0x00002000)]
    public void AgeGenderFlags_HasCorrectValues(AgeGenderFlags flag, uint expected)
    {
        ((uint)flag).Should().Be(expected);
    }

    [Theory]
    [InlineData(BodyType.All, 0)]
    [InlineData(BodyType.Hat, 1)]
    [InlineData(BodyType.Hair, 2)]
    [InlineData(BodyType.Shoes, 8)]
    [InlineData(BodyType.FacialHair, 0x1C)]
    [InlineData(BodyType.SkinOverlay, 0x3A)]
    public void BodyType_HasCorrectValues(BodyType bodyType, uint expected)
    {
        ((uint)bodyType).Should().Be(expected);
    }

    [Theory]
    [InlineData(ExcludePartFlag.Hat, 1ul << 1)]
    [InlineData(ExcludePartFlag.Hair, 1ul << 2)]
    [InlineData(ExcludePartFlag.Shoes, 1ul << 8)]
    [InlineData(ExcludePartFlag.Eyeliner, 1ul << 31)]
    [InlineData(ExcludePartFlag.Blush, 1ul << 32)]
    [InlineData(ExcludePartFlag.SkinOverlay, 1ul << 58)]
    public void ExcludePartFlag_HasCorrectValues(ExcludePartFlag flag, ulong expected)
    {
        ((ulong)flag).Should().Be(expected);
    }

    [Fact]
    public void AgeGenderFlags_CanCombine()
    {
        var combined = AgeGenderFlags.Adult | AgeGenderFlags.Female;
        ((uint)combined).Should().Be(0x00002020);
    }

    [Fact]
    public void ExcludePartFlag_CanCombine64BitValues()
    {
        var combined = ExcludePartFlag.Hat | ExcludePartFlag.SkinOverlay;
        // Hat is bit 1, SkinOverlay is bit 58
        ((ulong)combined).Should().Be((1ul << 1) | (1ul << 58));
    }

    [Fact]
    public void ParmFlag_None_EqualsZero()
    {
        ((byte)ParmFlag.None).Should().Be(0);
    }

    [Fact]
    public void AgeGenderFlags_None_EqualsZero()
    {
        ((uint)AgeGenderFlags.None).Should().Be(0u);
    }

    [Fact]
    public void OccultTypesDisabled_None_EqualsZero()
    {
        ((uint)OccultTypesDisabled.None).Should().Be(0u);
    }

    [Fact]
    public void ExcludePartFlag_None_EqualsZero()
    {
        ((ulong)ExcludePartFlag.None).Should().Be(0ul);
    }

    [Fact]
    public void PackFlag_None_EqualsZero()
    {
        ((byte)PackFlag.None).Should().Be(0);
    }

    [Fact]
    public void BodyType_All_EqualsZero()
    {
        ((uint)BodyType.All).Should().Be(0);
    }

    [Fact]
    public void CASPartRegion_Base_EqualsZero()
    {
        ((uint)CASPartRegion.Base).Should().Be(0);
    }

    [Fact]
    public void ParmFlag_HasFlag_WorksCorrectly()
    {
        var combined = ParmFlag.AllowForRandom | ParmFlag.ShowInUI;

        combined.HasFlag(ParmFlag.AllowForRandom).Should().BeTrue();
        combined.HasFlag(ParmFlag.ShowInUI).Should().BeTrue();
        combined.HasFlag(ParmFlag.DefaultForBodyType).Should().BeFalse();
        combined.HasFlag(ParmFlag.None).Should().BeTrue(); // None is always present in any flag
    }

    [Fact]
    public void AgeGenderFlags_HasFlag_WorksCorrectly()
    {
        var combined = AgeGenderFlags.Adult | AgeGenderFlags.Elder | AgeGenderFlags.Female;

        combined.HasFlag(AgeGenderFlags.Adult).Should().BeTrue();
        combined.HasFlag(AgeGenderFlags.Female).Should().BeTrue();
        combined.HasFlag(AgeGenderFlags.Male).Should().BeFalse();
        combined.HasFlag(AgeGenderFlags.Child).Should().BeFalse();
    }

    [Fact]
    public void ExcludePartFlag_HasFlag_Works64Bit()
    {
        var combined = ExcludePartFlag.Blush | ExcludePartFlag.SkinOverlay;

        // Blush is bit 32, SkinOverlay is bit 58 - both > 32 bits
        combined.HasFlag(ExcludePartFlag.Blush).Should().BeTrue();
        combined.HasFlag(ExcludePartFlag.SkinOverlay).Should().BeTrue();
        combined.HasFlag(ExcludePartFlag.Hat).Should().BeFalse();
    }

    [Fact]
    public void AgeGenderFlags_AllAges_CanCombine()
    {
        var allAges = AgeGenderFlags.Baby | AgeGenderFlags.Toddler | AgeGenderFlags.Child |
                      AgeGenderFlags.Teen | AgeGenderFlags.YoungAdult | AgeGenderFlags.Adult |
                      AgeGenderFlags.Elder;

        ((uint)allAges).Should().Be(0x0000007F);
    }

    [Fact]
    public void AgeGenderFlags_AllGenders_CanCombine()
    {
        var allGenders = AgeGenderFlags.Male | AgeGenderFlags.Female;

        ((uint)allGenders).Should().Be(0x00003000);
    }

    [Fact]
    public void BodyType_MaxValue_IsSkinOverlay()
    {
        // SkinOverlay (0x3A = 58) is the highest defined body type
        ((uint)BodyType.SkinOverlay).Should().Be(0x3A);
    }

    [Fact]
    public void CASPartRegion_MaxValue_IsMax()
    {
        // Max is the sentinel value for the enum
        ((uint)CASPartRegion.Max).Should().Be(20);
    }

    [Theory]
    [InlineData((uint)0xFFFFFFFF)]
    [InlineData((uint)0x12345678)]
    public void AgeGenderFlags_InvalidValues_CanBeCast(uint value)
    {
        // Enum should accept any uint value even if not defined
        var flag = (AgeGenderFlags)value;
        ((uint)flag).Should().Be(value);
    }

    [Theory]
    [InlineData((ulong)0xFFFFFFFFFFFFFFFF)]
    [InlineData((ulong)0x123456789ABCDEF0)]
    public void ExcludePartFlag_InvalidValues_CanBeCast(ulong value)
    {
        // Enum should accept any ulong value even if not defined
        var flag = (ExcludePartFlag)value;
        ((ulong)flag).Should().Be(value);
    }

    #region Edge cases

    [Fact]
    public void ParmFlag_SingleBitMembers_AreDisjoint()
    {
        var bits = Enum.GetValues<ParmFlag>().Where(f => f != ParmFlag.None).Select(f => (byte)f).ToList();

        bits.Should().OnlyContain(b => (b & (b - 1)) == 0, "each flag is exactly one bit");
        bits.Distinct().Should().HaveCount(bits.Count);
        bits.Aggregate(0, (acc, b) => acc | b).Should().Be(0x3F);
    }

    [Fact]
    public void ParmFlag_UndefinedHighBits_SurviveCastAndAreNotDefined()
    {
        var flag = (ParmFlag)0xFF;

        ((byte)flag).Should().Be(0xFF);
        flag.HasFlag(ParmFlag.ShowInCASDemo).Should().BeTrue();
        Enum.IsDefined(flag).Should().BeFalse("0xFF is a bit-combination, not a named member");
        (flag & ~(ParmFlag.DefaultForBodyType | ParmFlag.DefaultThumbnailPart | ParmFlag.AllowForRandom |
                  ParmFlag.ShowInUI | ParmFlag.ShowInSimInfoDemo | ParmFlag.ShowInCASDemo))
            .Should().Be((ParmFlag)0xC0);
    }

    [Fact]
    public void PackFlag_OnlyHidePackIcon_IsDefined()
    {
        Enum.GetValues<PackFlag>().Should().BeEquivalentTo([PackFlag.None, PackFlag.HidePackIcon]);
        ((byte)PackFlag.HidePackIcon).Should().Be(1);
        Enum.IsDefined((PackFlag)2).Should().BeFalse();
    }

    [Fact]
    public void AgeGenderFlags_AgeBitsAndGenderBits_DoNotOverlap()
    {
        const uint ageMask = 0x7F;
        const uint genderMask = 0x3000;

        (ageMask & genderMask).Should().Be(0u);
        ((uint)AgeGenderFlags.Male & ageMask).Should().Be(0u);
        ((uint)AgeGenderFlags.Female & ageMask).Should().Be(0u);
        ((uint)AgeGenderFlags.Male & (uint)AgeGenderFlags.Female).Should().Be(0u);
    }

    [Fact]
    public void AgeGenderFlags_RemovingFlag_ClearsOnlyThatBit()
    {
        var all = AgeGenderFlags.Adult | AgeGenderFlags.Elder | AgeGenderFlags.Male | AgeGenderFlags.Female;

        var withoutFemale = all & ~AgeGenderFlags.Female;

        withoutFemale.Should().Be(AgeGenderFlags.Adult | AgeGenderFlags.Elder | AgeGenderFlags.Male);
        (all ^ all).Should().Be(AgeGenderFlags.None);
    }

    [Fact]
    public void AgeGenderFlags_ToString_ListsCombinedNames()
    {
        (AgeGenderFlags.Adult | AgeGenderFlags.Female).ToString().Should().Be("Adult, Female");
        AgeGenderFlags.None.ToString().Should().Be("None");
        ((AgeGenderFlags)0x80).ToString().Should().Be("128", "an undefined bit prints numerically");
    }

    [Fact]
    public void AgeGenderFlags_Parse_RoundTripsNames()
    {
        Enum.Parse<AgeGenderFlags>("Teen, Male").Should().Be(AgeGenderFlags.Teen | AgeGenderFlags.Male);
        Enum.TryParse<AgeGenderFlags>("NotAnAge", out _).Should().BeFalse();
    }

    [Fact]
    public void BodyType_AllValues_AreUnique()
    {
        var values = Enum.GetValues<BodyType>().Select(v => (uint)v).ToList();

        values.Distinct().Count().Should().Be(values.Count, "no two BodyType names may share a value");
    }

    [Fact]
    public void BodyType_ValuesBeyondSkinOverlay_AreNotDefined()
    {
        Enum.IsDefined((BodyType)0x3B).Should().BeFalse();
        Enum.IsDefined((BodyType)uint.MaxValue).Should().BeFalse();
        Enum.IsDefined(BodyType.SkinOverlay).Should().BeTrue();
    }

    [Fact]
    public void OccultTypesDisabled_BothFlags_CombineToThree()
    {
        var both = OccultTypesDisabled.Human | OccultTypesDisabled.Alien;

        ((uint)both).Should().Be(3u);
        both.HasFlag(OccultTypesDisabled.Alien).Should().BeTrue();
        (both & ~OccultTypesDisabled.Human).Should().Be(OccultTypesDisabled.Alien);
    }

    [Fact]
    public void ExcludePartFlag_AllNamedMembers_AreSingleBitsAndUnique()
    {
        var values = Enum.GetValues<ExcludePartFlag>().Where(f => f != ExcludePartFlag.None).Select(f => (ulong)f).ToList();

        values.Should().NotBeEmpty();
        values.Distinct().Count().Should().Be(values.Count);
        values.Should().OnlyContain(v => (v & (v - 1)) == 0, "legacy defines one bit per body part");
    }

    [Fact]
    public void ExcludePartFlag_BitsAbove32_DoNotTruncate()
    {
        var flag = ExcludePartFlag.SkinOverlay;

        ((uint)((ulong)flag & 0xFFFFFFFF)).Should().Be(0u, "SkinOverlay lives entirely in the upper 32 bits");
        ((ulong)flag >> 32).Should().Be(1ul << 26);
    }

    [Theory]
    [InlineData(CASPanelGroupType.HeadAndEars, 2u)]
    [InlineData(CASPanelGroupType.Mouth, 8u)]
    [InlineData(CASPanelGroupType.Nose, 16u)]
    [InlineData(CASPanelGroupType.Eyelash, 64u)]
    [InlineData(CASPanelGroupType.Eyes, 128u)]
    public void CASPanelGroupType_NamedMembers_HaveExpectedBits(CASPanelGroupType type, uint expected)
    {
        ((uint)type).Should().Be(expected);
    }

    [Fact]
    public void CASPanelSortType_Members_AreContiguousPowersOfTwo()
    {
        var values = Enum.GetValues<CASPanelSortType>().Where(v => v != CASPanelSortType.Unknown0).Select(v => (uint)v).OrderBy(v => v).ToList();

        values.Should().HaveCount(15);
        for (var i = 0; i < values.Count; i++)
            values[i].Should().Be(1u << i);
    }

    [Fact]
    public void CASPartRegion_Members_AreSequentialFromZero()
    {
        var values = Enum.GetValues<CASPartRegion>().Select(v => (uint)v).Distinct().OrderBy(v => v).ToList();

        values.First().Should().Be(0u);
        values.Last().Should().Be((uint)CASPartRegion.Max);
        values.Zip(values.Skip(1), (a, b) => b - a).Should().OnlyContain(d => d == 1, "regions are declared without gaps");
    }

    #endregion
}
