using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Tests for <see cref="SkinToneResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SkinToneResource.cs
/// - Type ID: 0x0354796A
/// - Contains skin tone data with overlay references, colorize settings, and swatch colors
/// </summary>
public class SkinToneResourceTests
{
    private static readonly ResourceKey TestKey = new(SkinToneResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        SkinToneResource.TypeId.Should().Be(0x0354796A);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new SkinToneResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(7);
        resource.TextureInstance.Should().Be(0);
        resource.OverlayList.Should().BeEmpty();
        resource.ColorizeSaturation.Should().Be(0);
        resource.ColorizeHue.Should().Be(0);
        resource.ColorizeOpacity.Should().Be(0);
        resource.FlagList.Should().BeEmpty();
        resource.MakeupOpacity.Should().Be(0);
        resource.SwatchList.Should().BeEmpty();
        resource.SortOrder.Should().Be(0);
        resource.MakeupOpacity2.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_BasicFields_Version7_PreservesData()
    {
        var original = new SkinToneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 7;
        original.TextureInstance = 0x123456789ABCDEF0;
        original.ColorizeSaturation = 100;
        original.ColorizeHue = 200;
        original.ColorizeOpacity = 0xAABBCCDD;
        original.MakeupOpacity = 0.5f;
        original.SortOrder = 1.5f;
        original.MakeupOpacity2 = 0.75f;

        var data = original.Data.ToArray();
        var parsed = new SkinToneResource(TestKey, data);

        parsed.Version.Should().Be(7);
        parsed.TextureInstance.Should().Be(0x123456789ABCDEF0);
        parsed.ColorizeSaturation.Should().Be(100);
        parsed.ColorizeHue.Should().Be(200);
        parsed.ColorizeOpacity.Should().Be(0xAABBCCDD);
        parsed.MakeupOpacity.Should().Be(0.5f);
        parsed.SortOrder.Should().Be(1.5f);
        parsed.MakeupOpacity2.Should().Be(0.75f);
    }

    [Fact]
    public void RoundTrip_OverlayList_PreservesData()
    {
        var original = new SkinToneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 7;
        original.OverlayList.Add(new OverlayReference(AgeGenderFlags.Adult | AgeGenderFlags.Male, 0x1111111111111111));
        original.OverlayList.Add(new OverlayReference(AgeGenderFlags.Teen | AgeGenderFlags.Female, 0x2222222222222222));

        var data = original.Data.ToArray();
        var parsed = new SkinToneResource(TestKey, data);

        parsed.OverlayList.Should().HaveCount(2);
        parsed.OverlayList[0].AgeGender.Should().Be(AgeGenderFlags.Adult | AgeGenderFlags.Male);
        parsed.OverlayList[0].TextureReference.Should().Be(0x1111111111111111);
        parsed.OverlayList[1].AgeGender.Should().Be(AgeGenderFlags.Teen | AgeGenderFlags.Female);
        parsed.OverlayList[1].TextureReference.Should().Be(0x2222222222222222);
    }

    [Fact]
    public void RoundTrip_FlagList_Version7_PreservesData()
    {
        var original = new SkinToneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 7;
        original.FlagList.Add(new CaspFlag(1, 100));
        original.FlagList.Add(new CaspFlag(2, 200));

        var data = original.Data.ToArray();
        var parsed = new SkinToneResource(TestKey, data);

        parsed.FlagList.Should().HaveCount(2);
        parsed.FlagList[0].Category.Should().Be(1);
        parsed.FlagList[0].Value.Should().Be(100);
        parsed.FlagList[1].Category.Should().Be(2);
        parsed.FlagList[1].Value.Should().Be(200);
    }

    [Fact]
    public void RoundTrip_SwatchList_PreservesData()
    {
        var original = new SkinToneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 7;
        original.SwatchList.Add(new SwatchColor(unchecked((int)0xAABBCCDD)));
        original.SwatchList.Add(new SwatchColor(0x11223344));

        var data = original.Data.ToArray();
        var parsed = new SkinToneResource(TestKey, data);

        parsed.SwatchList.Should().HaveCount(2);
        parsed.SwatchList[0].Argb.Should().Be(unchecked((int)0xAABBCCDD));
        parsed.SwatchList[1].Argb.Should().Be(0x11223344);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new SkinToneResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 7;
        original.TextureInstance = 0xFEDCBA9876543210;
        original.ColorizeSaturation = 50;
        original.ColorizeHue = 150;
        original.ColorizeOpacity = 255;
        original.MakeupOpacity = 0.25f;
        original.SortOrder = 2.0f;
        original.MakeupOpacity2 = 0.9f;

        original.OverlayList.Add(new OverlayReference(AgeGenderFlags.Child | AgeGenderFlags.Female, 0x3333333333333333));
        original.FlagList.Add(new CaspFlag(10, 500));
        original.SwatchList.Add(new SwatchColor(unchecked((int)0xFF00FF00)));

        var data1 = original.Data.ToArray();
        var parsed = new SkinToneResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new SkinToneResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<SkinToneResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new SkinToneResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as SkinToneResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(7);
    }

    [Fact]
    public void OverlayReference_RecordEquality_Works()
    {
        var ref1 = new OverlayReference(AgeGenderFlags.Adult, 0x1234567890ABCDEF);
        var ref2 = new OverlayReference(AgeGenderFlags.Adult, 0x1234567890ABCDEF);
        var ref3 = new OverlayReference(AgeGenderFlags.Teen, 0x1234567890ABCDEF);

        ref1.Should().Be(ref2);
        ref1.Should().NotBe(ref3);
    }
}
