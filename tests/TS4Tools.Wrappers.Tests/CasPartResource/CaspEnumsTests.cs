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
}
