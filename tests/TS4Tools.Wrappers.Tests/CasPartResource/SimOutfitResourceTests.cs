using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CasPartResource;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

/// <summary>
/// Tests for <see cref="SimOutfitResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CASPartResource/SimOutfitResource.cs
/// - Type ID: 0x025ED6F4
/// - Contains sim outfit data with slider references and TGI lists
/// </summary>
public class SimOutfitResourceTests
{
    private static readonly ResourceKey TestKey = new(SimOutfitResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        SimOutfitResource.TypeId.Should().Be(0x025ED6F4);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(1);
        resource.Unknown10.Should().HaveCount(24);
        resource.Unknown12.Should().HaveCount(16);
        resource.Unknown13.Should().HaveCount(9);
        resource.SliderReferences1.Should().BeEmpty();
        resource.TgiList.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_BasicFields_PreservesData()
    {
        var original = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 5;
        original.Unknown1 = 1.5f;
        original.Unknown2 = 2.5f;
        original.Unknown3 = 3.5f;
        original.Unknown4 = 4.5f;
        original.Unknown5 = 5.5f;
        original.Unknown6 = 6.5f;
        original.Unknown7 = 7.5f;
        original.Unknown8 = 8.5f;
        original.Age = AgeGenderFlags.Teen | AgeGenderFlags.Adult;
        original.Gender = AgeGenderFlags.Female;
        original.SkinToneReference = 0x123456789ABCDEF0;
        original.CaspReference = 0xFEDCBA9876543210;

        var data = original.Data.ToArray();
        var parsed = new SimOutfitResource(TestKey, data);

        parsed.Version.Should().Be(5);
        parsed.Unknown1.Should().Be(1.5f);
        parsed.Unknown2.Should().Be(2.5f);
        parsed.Unknown3.Should().Be(3.5f);
        parsed.Unknown4.Should().Be(4.5f);
        parsed.Unknown5.Should().Be(5.5f);
        parsed.Unknown6.Should().Be(6.5f);
        parsed.Unknown7.Should().Be(7.5f);
        parsed.Unknown8.Should().Be(8.5f);
        parsed.Age.Should().Be(AgeGenderFlags.Teen | AgeGenderFlags.Adult);
        parsed.Gender.Should().Be(AgeGenderFlags.Female);
        parsed.SkinToneReference.Should().Be(0x123456789ABCDEF0);
        parsed.CaspReference.Should().Be(0xFEDCBA9876543210);
    }

    [Fact]
    public void RoundTrip_TgiList_PreservesData()
    {
        var original = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.TgiList.Add(new TgiReference(0x1111111111111111, 0x11111111, 0x11111111));
        original.TgiList.Add(new TgiReference(0x2222222222222222, 0x22222222, 0x22222222));

        var data = original.Data.ToArray();
        var parsed = new SimOutfitResource(TestKey, data);

        parsed.TgiList.Should().HaveCount(2);
        parsed.TgiList[0].Instance.Should().Be(0x1111111111111111);
        parsed.TgiList[0].Type.Should().Be(0x11111111);
        parsed.TgiList[0].Group.Should().Be(0x11111111);
        parsed.TgiList[1].Instance.Should().Be(0x2222222222222222);
    }

    [Fact]
    public void RoundTrip_SliderReferences_PreservesData()
    {
        var original = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.SliderReferences1.Add(new SliderReference(0, 0.5f));
        original.SliderReferences1.Add(new SliderReference(1, 0.75f));
        original.SliderReferences2.Add(new SliderReference(2, -0.25f));

        var data = original.Data.ToArray();
        var parsed = new SimOutfitResource(TestKey, data);

        parsed.SliderReferences1.Should().HaveCount(2);
        parsed.SliderReferences1[0].Index.Should().Be(0);
        parsed.SliderReferences1[0].Value.Should().Be(0.5f);
        parsed.SliderReferences1[1].Index.Should().Be(1);
        parsed.SliderReferences1[1].Value.Should().Be(0.75f);
        parsed.SliderReferences2.Should().HaveCount(1);
        parsed.SliderReferences2[0].Value.Should().Be(-0.25f);
    }

    [Fact]
    public void RoundTrip_DataReferenceList_PreservesData()
    {
        var original = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.DataReferenceList.Add(0xAAAAAAAAAAAAAAAA);
        original.DataReferenceList.Add(0xBBBBBBBBBBBBBBBB);
        original.DataReferenceList.Add(0xCCCCCCCCCCCCCCCC);

        var data = original.Data.ToArray();
        var parsed = new SimOutfitResource(TestKey, data);

        parsed.DataReferenceList.Should().HaveCount(3);
        parsed.DataReferenceList[0].Should().Be(0xAAAAAAAAAAAAAAAA);
        parsed.DataReferenceList[1].Should().Be(0xBBBBBBBBBBBBBBBB);
        parsed.DataReferenceList[2].Should().Be(0xCCCCCCCCCCCCCCCC);
    }

    [Fact]
    public void RoundTrip_Unknown9_PreservesData()
    {
        var original = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Unknown9.Add(0x11);
        original.Unknown9.Add(0x22);
        original.Unknown9.Add(0x33);

        var data = original.Data.ToArray();
        var parsed = new SimOutfitResource(TestKey, data);

        parsed.Unknown9.Should().HaveCount(3);
        parsed.Unknown9[0].Should().Be(0x11);
        parsed.Unknown9[1].Should().Be(0x22);
        parsed.Unknown9[2].Should().Be(0x33);
    }

    [Fact]
    public void RoundTrip_UnknownByteLists_PreservesData()
    {
        var original = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.UnknownByteList.Add(0xAA);
        original.UnknownByteList.Add(0xBB);

        var data = original.Data.ToArray();
        var parsed = new SimOutfitResource(TestKey, data);

        parsed.UnknownByteList.Should().HaveCount(2);
        parsed.UnknownByteList[0].Should().Be(0xAA);
        parsed.UnknownByteList[1].Should().Be(0xBB);
    }

    [Fact]
    public void RoundTrip_DataBlobs_PreservesData()
    {
        var original = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);
        for (int i = 0; i < 24; i++) original.Unknown10[i] = (byte)(i * 10);
        for (int i = 0; i < 16; i++) original.Unknown12[i] = (byte)(i * 15);
        for (int i = 0; i < 9; i++) original.Unknown13[i] = (byte)(i * 20);

        var data = original.Data.ToArray();
        var parsed = new SimOutfitResource(TestKey, data);

        parsed.Unknown10.Should().HaveCount(24);
        parsed.Unknown10[0].Should().Be(0);
        parsed.Unknown10[1].Should().Be(10);
        parsed.Unknown12.Should().HaveCount(16);
        parsed.Unknown12[0].Should().Be(0);
        parsed.Unknown12[1].Should().Be(15);
        parsed.Unknown13.Should().HaveCount(9);
        parsed.Unknown13[0].Should().Be(0);
        parsed.Unknown13[1].Should().Be(20);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new SimOutfitResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Set various fields
        original.Version = 2;
        original.Unknown1 = 1.0f;
        original.Unknown2 = 2.0f;
        original.Age = AgeGenderFlags.Adult;
        original.Gender = AgeGenderFlags.Male;
        original.SkinToneReference = 0x1234567812345678;
        original.CaspReference = 0x8765432187654321;

        // Add some data
        original.SliderReferences1.Add(new SliderReference(0, 0.5f));
        original.TgiList.Add(new TgiReference(0xAAAAAAAABBBBBBBB, 0xCCCCCCCC, 0xDDDDDDDD));
        original.DataReferenceList.Add(0xEEEEEEEEEEEEEEEE);

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new SimOutfitResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new SimOutfitResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<SimOutfitResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new SimOutfitResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as SimOutfitResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(1);
    }
}
