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
}
