using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using TS4Tools.Wrappers;
using TS4Tools.Wrappers.CasPartResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CasPartResource;

public class StyleLookResourceTests
{
    private static ResourceKey TestKey => new(StyleLookResource.TypeId, 0, 0x12345678);

    [Fact]
    public void EmptyResource_ShouldNotBeValid()
    {
        // Arrange & Act
        var resource = new StyleLookResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TruncatedData_ShouldNotBeValid()
    {
        // Arrange - Less than minimum size
        byte[] truncatedData = new byte[50];

        // Act
        var resource = new StyleLookResource(TestKey, truncatedData);

        // Assert
        resource.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MinimalValidResource_ShouldParse()
    {
        // Arrange
        byte[] data = CreateMinimalStyleLookData();

        // Act
        var resource = new StyleLookResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
    }

    [Fact]
    public void HeaderFields_ShouldParseCorrectly()
    {
        // Arrange
        byte[] data = CreateStyleLookDataWithFields(
            version: 5,
            ageGender: AgeGenderFlags.Adult | AgeGenderFlags.Female,
            groupingId: 0x1234567890ABCDEF,
            unknown1: 42,
            simOutfitRef: 0xAAAAAAAAAAAAAAAA,
            textureRef: 0xBBBBBBBBBBBBBBBB,
            simDataRef: 0xCCCCCCCCCCCCCCCC,
            nameHash: 0x11111111,
            descHash: 0x22222222,
            unknown3: 0x33333333);

        // Act
        var resource = new StyleLookResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.Version.Should().Be(5);
        resource.AgeGender.Should().Be(AgeGenderFlags.Adult | AgeGenderFlags.Female);
        resource.GroupingId.Should().Be(0x1234567890ABCDEF);
        resource.Unknown1.Should().Be(42);
        resource.SimOutfitReference.Should().Be(0xAAAAAAAAAAAAAAAA);
        resource.TextureReference.Should().Be(0xBBBBBBBBBBBBBBBB);
        resource.SimDataReference.Should().Be(0xCCCCCCCCCCCCCCCC);
        resource.NameHash.Should().Be(0x11111111);
        resource.DescHash.Should().Be(0x22222222);
        resource.Unknown3.Should().Be(0x33333333);
    }

    [Fact]
    public void AnimationNames_ShouldParseCorrectly()
    {
        // Arrange
        byte[] data = CreateStyleLookDataWithAnimations(
            animRef1: 0x1111111111111111,
            animName1: "IdleAnimation",
            animRef2: 0x2222222222222222,
            animName2: "WalkAnimation");

        // Act
        var resource = new StyleLookResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.AnimationReference1.Should().Be(0x1111111111111111);
        resource.AnimationStateName1.Should().Be("IdleAnimation");
        resource.AnimationReference2.Should().Be(0x2222222222222222);
        resource.AnimationStateName2.Should().Be("WalkAnimation");
    }

    [Fact]
    public void EmptyAnimationNames_ShouldParseCorrectly()
    {
        // Arrange
        byte[] data = CreateStyleLookDataWithAnimations(
            animRef1: 0,
            animName1: "",
            animRef2: 0,
            animName2: "");

        // Act
        var resource = new StyleLookResource(TestKey, data);

        // Assert
        resource.IsValid.Should().BeTrue();
        resource.AnimationStateName1.Should().BeEmpty();
        resource.AnimationStateName2.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAllData()
    {
        // Arrange
        byte[] originalData = CreateStyleLookDataWithFields(
            version: 3,
            ageGender: AgeGenderFlags.Teen | AgeGenderFlags.Male,
            groupingId: 0xDEADBEEFCAFEBABE,
            unknown1: 255,
            simOutfitRef: 123456789,
            textureRef: 987654321,
            simDataRef: 111222333,
            nameHash: 0xABCDEF01,
            descHash: 0x12345678,
            unknown3: 0x99999999);

        var resource = new StyleLookResource(TestKey, originalData);
        resource.IsValid.Should().BeTrue();

        // Act
        var serialized = resource.Data;
        var reparsed = new StyleLookResource(TestKey, serialized);

        // Assert
        reparsed.IsValid.Should().BeTrue();
        reparsed.Version.Should().Be(resource.Version);
        reparsed.AgeGender.Should().Be(resource.AgeGender);
        reparsed.GroupingId.Should().Be(resource.GroupingId);
        reparsed.Unknown1.Should().Be(resource.Unknown1);
        reparsed.SimOutfitReference.Should().Be(resource.SimOutfitReference);
        reparsed.TextureReference.Should().Be(resource.TextureReference);
        reparsed.SimDataReference.Should().Be(resource.SimDataReference);
        reparsed.NameHash.Should().Be(resource.NameHash);
        reparsed.DescHash.Should().Be(resource.DescHash);
        reparsed.Unknown3.Should().Be(resource.Unknown3);
    }

    [Fact]
    public void RoundTrip_WithAnimations_ShouldPreserveData()
    {
        // Arrange
        byte[] originalData = CreateStyleLookDataWithAnimations(
            animRef1: 0xFFFFFFFFFFFFFFFF,
            animName1: "TestAnimation1",
            animRef2: 0x0123456789ABCDEF,
            animName2: "AnotherAnim");

        var resource = new StyleLookResource(TestKey, originalData);

        // Act
        var serialized = resource.Data;
        var reparsed = new StyleLookResource(TestKey, serialized);

        // Assert
        reparsed.IsValid.Should().BeTrue();
        reparsed.AnimationReference1.Should().Be(resource.AnimationReference1);
        reparsed.AnimationStateName1.Should().Be(resource.AnimationStateName1);
        reparsed.AnimationReference2.Should().Be(resource.AnimationReference2);
        reparsed.AnimationStateName2.Should().Be(resource.AnimationStateName2);
    }

    [Fact]
    public void TypeId_ShouldMatchExpectedValue()
    {
        // Assert
        StyleLookResource.TypeId.Should().Be(0x71BDB8A2);
    }

    [Fact]
    public void UnknownBlob_ShouldBe14Bytes()
    {
        // Arrange
        byte[] data = CreateMinimalStyleLookData();
        var resource = new StyleLookResource(TestKey, data);

        // Assert
        resource.UnknownBlob.Length.Should().Be(14);
    }

    [Fact]
    public void SetUnknownBlob_WithCorrectSize_ShouldSucceed()
    {
        // Arrange
        var resource = new StyleLookResource(TestKey, ReadOnlyMemory<byte>.Empty);
        byte[] newBlob = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14];

        // Act
        resource.SetUnknownBlob(newBlob);

        // Assert
        resource.UnknownBlob.ToArray().Should().Equal(newBlob);
    }

    [Fact]
    public void SetUnknownBlob_WithWrongSize_ShouldThrow()
    {
        // Arrange
        var resource = new StyleLookResource(TestKey, ReadOnlyMemory<byte>.Empty);
        byte[] wrongSize = [1, 2, 3];

        // Act & Assert
        var act = () => resource.SetUnknownBlob(wrongSize);
        act.Should().Throw<ArgumentException>();
    }

    // Helper methods to create test data

    private static byte[] CreateMinimalStyleLookData()
    {
        return CreateStyleLookDataWithFields(
            version: 1,
            ageGender: AgeGenderFlags.Adult,
            groupingId: 0,
            unknown1: 0,
            simOutfitRef: 0,
            textureRef: 0,
            simDataRef: 0,
            nameHash: 0,
            descHash: 0,
            unknown3: 0);
    }

    private static byte[] CreateStyleLookDataWithFields(
        uint version,
        AgeGenderFlags ageGender,
        ulong groupingId,
        byte unknown1,
        ulong simOutfitRef,
        ulong textureRef,
        ulong simDataRef,
        uint nameHash,
        uint descHash,
        uint unknown3)
    {
        return CreateStyleLookDataWithAnimations(
            version, ageGender, groupingId, unknown1,
            simOutfitRef, textureRef, simDataRef,
            nameHash, descHash, unknown3,
            0, "", 0, "");
    }

    private static byte[] CreateStyleLookDataWithAnimations(
        ulong animRef1,
        string animName1,
        ulong animRef2,
        string animName2)
    {
        return CreateStyleLookDataWithAnimations(
            1, AgeGenderFlags.Adult, 0, 0,
            0, 0, 0, 0, 0, 0,
            animRef1, animName1, animRef2, animName2);
    }

    private static byte[] CreateStyleLookDataWithAnimations(
        uint version,
        AgeGenderFlags ageGender,
        ulong groupingId,
        byte unknown1,
        ulong simOutfitRef,
        ulong textureRef,
        ulong simDataRef,
        uint nameHash,
        uint descHash,
        uint unknown3,
        ulong animRef1,
        string animName1,
        ulong animRef2,
        string animName2)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header
        writer.Write(version);
        writer.Write((uint)ageGender);
        writer.Write(groupingId);
        writer.Write(unknown1);
        writer.Write(simOutfitRef);
        writer.Write(textureRef);
        writer.Write(simDataRef);
        writer.Write(nameHash);
        writer.Write(descHash);

        // Unknown blob (14 bytes)
        writer.Write(new byte[14]);

        writer.Write(unknown3);

        // Animation 1
        writer.Write(animRef1);
        byte[] animName1Bytes = Encoding.ASCII.GetBytes(animName1);
        writer.Write(animName1Bytes.Length);
        writer.Write(animName1Bytes);

        // Animation 2
        writer.Write(animRef2);
        byte[] animName2Bytes = Encoding.ASCII.GetBytes(animName2);
        writer.Write(animName2Bytes.Length);
        writer.Write(animName2Bytes);

        // Empty color list (1 byte count = 0)
        writer.Write((byte)0);

        // Empty flag list (4 byte count = 0)
        writer.Write(0);

        return ms.ToArray();
    }
}
