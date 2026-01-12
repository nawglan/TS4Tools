using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.CatalogResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.CatalogResource;

/// <summary>
/// Tests for <see cref="ObjectDefinitionResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/CatalogResource/ObjectDefinitionResource.cs
/// - Type ID: 0xC0DB5AE7
/// - Table-based format with property IDs pointing to data at offsets
/// - TGI blocks use swapped instance (high/low 32 bits swapped)
/// </summary>
public class ObjectDefinitionResourceTests
{
    private static readonly ResourceKey TestKey = new(ObjectDefinitionResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        ObjectDefinitionResource.TypeId.Should().Be(0xC0DB5AE7);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(1);
        resource.Name.Should().BeNull();
        resource.Tuning.Should().BeNull();
        resource.SimoleonPrice.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_NameProperty_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Name);
        resource.Name = "TestObject";

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.HasProperty(ObjectDefinitionPropertyId.Name).Should().BeTrue();
        parsed.Name.Should().Be("TestObject");
    }

    [Fact]
    public void RoundTrip_BasicProperties_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.SimoleonPrice);
        resource.AddProperty(ObjectDefinitionPropertyId.PositiveEnvironmentScore);
        resource.AddProperty(ObjectDefinitionPropertyId.NegativeEnvironmentScore);
        resource.AddProperty(ObjectDefinitionPropertyId.ThumbnailGeometryState);
        resource.SimoleonPrice = 1234;
        resource.PositiveEnvironmentScore = 2.5f;
        resource.NegativeEnvironmentScore = -1.5f;
        resource.ThumbnailGeometryState = 0xABCDEF01;

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.SimoleonPrice.Should().Be(1234);
        parsed.PositiveEnvironmentScore.Should().Be(2.5f);
        parsed.NegativeEnvironmentScore.Should().Be(-1.5f);
        parsed.ThumbnailGeometryState.Should().Be(0xABCDEF01);
    }

    [Fact]
    public void RoundTrip_StringProperties_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Name);
        resource.AddProperty(ObjectDefinitionPropertyId.Tuning);
        resource.AddProperty(ObjectDefinitionPropertyId.MaterialVariant);
        resource.Name = "MyObject";
        resource.Tuning = "object_tuning_12345";
        resource.MaterialVariant = "wood_oak";

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.Name.Should().Be("MyObject");
        parsed.Tuning.Should().Be("object_tuning_12345");
        parsed.MaterialVariant.Should().Be("wood_oak");
    }

    [Fact]
    public void RoundTrip_TuningId_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.TuningId);
        resource.TuningId = 0x123456789ABCDEF0;

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.TuningId.Should().Be(0x123456789ABCDEF0);
    }

    [Fact]
    public void RoundTrip_Components_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Components);
        resource.Components = [0x11111111, 0x22222222, 0x33333333];

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.Components.Should().NotBeNull();
        parsed.Components.Should().HaveCount(3);
        parsed.Components![0].Should().Be(0x11111111);
        parsed.Components![1].Should().Be(0x22222222);
        parsed.Components![2].Should().Be(0x33333333);
    }

    [Fact]
    public void RoundTrip_BooleanProperties_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Unknown2);
        resource.AddProperty(ObjectDefinitionPropertyId.IsBaby);
        resource.Unknown2 = true;
        resource.IsBaby = false;

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.Unknown2.Should().BeTrue();
        parsed.IsBaby.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_EnvironmentScoreEmotionTags_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.EnvironmentScoreEmotionTags);
        resource.EnvironmentScoreEmotionTags = [0x1234, 0x5678, 0x9ABC];

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.EnvironmentScoreEmotionTags.Should().NotBeNull();
        parsed.EnvironmentScoreEmotionTags.Should().HaveCount(3);
        parsed.EnvironmentScoreEmotionTags![0].Should().Be(0x1234);
        parsed.EnvironmentScoreEmotionTags![1].Should().Be(0x5678);
        parsed.EnvironmentScoreEmotionTags![2].Should().Be(0x9ABC);
    }

    [Fact]
    public void RoundTrip_EnvironmentScores_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.EnvironmentScores);
        resource.EnvironmentScores = [1.0f, 2.5f, -0.5f];

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.EnvironmentScores.Should().NotBeNull();
        parsed.EnvironmentScores.Should().HaveCount(3);
        parsed.EnvironmentScores![0].Should().Be(1.0f);
        parsed.EnvironmentScores![1].Should().Be(2.5f);
        parsed.EnvironmentScores![2].Should().Be(-0.5f);
    }

    [Fact]
    public void RoundTrip_Unknown4_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Unknown4);
        resource.Unknown4 = [0x11, 0x22, 0x33, 0x44, 0x55];

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.Unknown4.Should().NotBeNull();
        parsed.Unknown4.Should().HaveCount(5);
        parsed.Unknown4.Should().BeEquivalentTo(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 });
    }

    [Fact]
    public void RoundTrip_TgiBlockList_PreservesSwappedInstance()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Icon);
        // Instance: 0xAABBCCDD11223344 should be stored swapped as 0x11223344AABBCCDD
        resource.Icon =
        [
            new TgiReference(0xAABBCCDD11223344, 0x12345678, 0x87654321)
        ];

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.Icon.Should().NotBeNull();
        parsed.Icon.Should().HaveCount(1);
        parsed.Icon![0].Instance.Should().Be(0xAABBCCDD11223344);
        parsed.Icon![0].Type.Should().Be(0x12345678);
        parsed.Icon![0].Group.Should().Be(0x87654321);
    }

    [Fact]
    public void RoundTrip_MultipleTgiBlockLists_PreservesData()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Icon);
        resource.AddProperty(ObjectDefinitionPropertyId.Model);
        resource.AddProperty(ObjectDefinitionPropertyId.Rig);

        resource.Icon = [new TgiReference(0x1111111122222222, 0x11111111, 0x11111111)];
        resource.Model = [new TgiReference(0x3333333344444444, 0x22222222, 0x22222222)];
        resource.Rig = [new TgiReference(0x5555555566666666, 0x33333333, 0x33333333)];

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.Icon![0].Instance.Should().Be(0x1111111122222222);
        parsed.Model![0].Instance.Should().Be(0x3333333344444444);
        parsed.Rig![0].Instance.Should().Be(0x5555555566666666);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);

        // Add various properties
        original.AddProperty(ObjectDefinitionPropertyId.Name);
        original.AddProperty(ObjectDefinitionPropertyId.TuningId);
        original.AddProperty(ObjectDefinitionPropertyId.SimoleonPrice);
        original.AddProperty(ObjectDefinitionPropertyId.Components);
        original.AddProperty(ObjectDefinitionPropertyId.Icon);

        original.Name = "TestChair";
        original.TuningId = 0x0123456789ABCDEF;
        original.SimoleonPrice = 500;
        original.Components = [0xAAAAAAAA, 0xBBBBBBBB];
        original.Icon = [new TgiReference(0xFEDCBA9876543210, 0x12345678, 0x87654321)];

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void PropertyId_Values_AreCorrect()
    {
        ((uint)ObjectDefinitionPropertyId.Name).Should().Be(0xE7F07786);
        ((uint)ObjectDefinitionPropertyId.SimoleonPrice).Should().Be(0xE4F4FAA4);
        ((uint)ObjectDefinitionPropertyId.Icon).Should().Be(0xCADED888);
        ((uint)ObjectDefinitionPropertyId.IsBaby).Should().Be(0xAEE67A1C);
    }

    [Fact]
    public void HasProperty_ReturnsFalseForMissingProperty()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Name);

        resource.HasProperty(ObjectDefinitionPropertyId.Name).Should().BeTrue();
        resource.HasProperty(ObjectDefinitionPropertyId.SimoleonPrice).Should().BeFalse();
    }

    [Fact]
    public void AddProperty_DoesNotDuplicateExisting()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Name);
        resource.AddProperty(ObjectDefinitionPropertyId.Name);
        resource.Name = "Test";

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        // Should only have one Name property
        parsed.Name.Should().Be("Test");
    }

    [Fact]
    public void RemoveProperty_RemovesProperty()
    {
        var resource = new ObjectDefinitionResource(TestKey, ReadOnlyMemory<byte>.Empty);
        resource.AddProperty(ObjectDefinitionPropertyId.Name);
        resource.AddProperty(ObjectDefinitionPropertyId.SimoleonPrice);
        resource.Name = "Test";
        resource.SimoleonPrice = 100;

        resource.RemoveProperty(ObjectDefinitionPropertyId.Name);

        var data = resource.Data.ToArray();
        var parsed = new ObjectDefinitionResource(TestKey, data);

        parsed.HasProperty(ObjectDefinitionPropertyId.Name).Should().BeFalse();
        parsed.HasProperty(ObjectDefinitionPropertyId.SimoleonPrice).Should().BeTrue();
        parsed.SimoleonPrice.Should().Be(100);
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new ObjectDefinitionResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<ObjectDefinitionResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new ObjectDefinitionResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as ObjectDefinitionResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(1);
    }
}
