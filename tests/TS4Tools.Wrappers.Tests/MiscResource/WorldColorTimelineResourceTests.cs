using FluentAssertions;
using TS4Tools.Resources;
using TS4Tools.Wrappers.MiscResource;
using Xunit;

namespace TS4Tools.Wrappers.Tests.MiscResource;

/// <summary>
/// Tests for <see cref="WorldColorTimelineResource"/>.
///
/// LEGACY ANALYSIS:
/// - Source: legacy_references/Sims4Tools/s4pi Wrappers/MiscellaneousResource/WorldColorTimelineResource.cs
/// - Type ID: 0x19301120
/// - Contains color data for world lighting over day/night cycle
/// </summary>
public class WorldColorTimelineResourceTests
{
    private static readonly ResourceKey TestKey = new(WorldColorTimelineResource.TypeId, 0, 0);

    [Fact]
    public void TypeId_IsCorrect()
    {
        WorldColorTimelineResource.TypeId.Should().Be(0x19301120);
    }

    [Fact]
    public void CreateEmpty_HasDefaultValues()
    {
        var resource = new WorldColorTimelineResource(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Version.Should().Be(1);
        resource.ColorTimelines.Should().NotBeNull();
        resource.ColorTimelines.Should().BeEmpty();
    }

    [Fact]
    public void TimelineColorData_SerializedSize_IsCorrect()
    {
        TimelineColorData.SerializedSize.Should().Be(20);
    }

    [Fact]
    public void RoundTrip_EmptyTimelines_PreservesData()
    {
        var original = new WorldColorTimelineResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 5;

        var data = original.Data.ToArray();
        var parsed = new WorldColorTimelineResource(TestKey, data);

        parsed.Version.Should().Be(5);
        parsed.ColorTimelines.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_SingleTimeline_PreservesData()
    {
        var original = new WorldColorTimelineResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;
        original.ColorTimelines.Add(new ColorTimeline
        {
            Version = 13,
            PointOfInterestId = 0x12345678,
            SunriseTime = 6.0f,
            SunsetTime = 18.0f,
            StarsAppearTime = 20.0f,
            StarsDisappearTime = 5.0f,
        });

        var data = original.Data.ToArray();
        var parsed = new WorldColorTimelineResource(TestKey, data);

        parsed.Version.Should().Be(1);
        parsed.ColorTimelines.Should().HaveCount(1);
        parsed.ColorTimelines[0].Version.Should().Be(13);
        parsed.ColorTimelines[0].PointOfInterestId.Should().Be(0x12345678);
        parsed.ColorTimelines[0].SunriseTime.Should().Be(6.0f);
        parsed.ColorTimelines[0].SunsetTime.Should().Be(18.0f);
        parsed.ColorTimelines[0].StarsAppearTime.Should().Be(20.0f);
        parsed.ColorTimelines[0].StarsDisappearTime.Should().Be(5.0f);
    }

    [Fact]
    public void RoundTrip_Version14_WithRemapTimeline_PreservesData()
    {
        var original = new WorldColorTimelineResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;
        original.ColorTimelines.Add(new ColorTimeline
        {
            Version = 14,
            RemapTimeline = true,
        });

        var data = original.Data.ToArray();
        var parsed = new WorldColorTimelineResource(TestKey, data);

        parsed.ColorTimelines[0].Version.Should().Be(14);
        parsed.ColorTimelines[0].RemapTimeline.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_Version13_WithoutRemapTimeline_PreservesData()
    {
        var original = new WorldColorTimelineResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;
        original.ColorTimelines.Add(new ColorTimeline
        {
            Version = 13,
        });

        var data = original.Data.ToArray();
        var parsed = new WorldColorTimelineResource(TestKey, data);

        parsed.ColorTimelines[0].Version.Should().Be(13);
        // RemapTimeline not serialized for version < 14
    }

    [Fact]
    public void RoundTrip_ColorData_PreservesValues()
    {
        var original = new WorldColorTimelineResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;

        var timeline = new ColorTimeline { Version = 13 };
        timeline.AmbientColors.ColorData.Add(new TimelineColorData(1.0f, 0.5f, 0.25f, 1.0f, 0.0f));
        timeline.AmbientColors.ColorData.Add(new TimelineColorData(0.8f, 0.4f, 0.2f, 1.0f, 12.0f));
        timeline.SunColors.ColorData.Add(new TimelineColorData(1.0f, 0.9f, 0.7f, 1.0f, 6.0f));

        original.ColorTimelines.Add(timeline);

        var data = original.Data.ToArray();
        var parsed = new WorldColorTimelineResource(TestKey, data);

        parsed.ColorTimelines[0].AmbientColors.ColorData.Should().HaveCount(2);
        parsed.ColorTimelines[0].AmbientColors.ColorData[0].R.Should().Be(1.0f);
        parsed.ColorTimelines[0].AmbientColors.ColorData[0].G.Should().Be(0.5f);
        parsed.ColorTimelines[0].AmbientColors.ColorData[0].B.Should().Be(0.25f);
        parsed.ColorTimelines[0].AmbientColors.ColorData[0].A.Should().Be(1.0f);
        parsed.ColorTimelines[0].AmbientColors.ColorData[0].Time.Should().Be(0.0f);

        parsed.ColorTimelines[0].AmbientColors.ColorData[1].Time.Should().Be(12.0f);

        parsed.ColorTimelines[0].SunColors.ColorData.Should().HaveCount(1);
        parsed.ColorTimelines[0].SunColors.ColorData[0].R.Should().Be(1.0f);
    }

    [Fact]
    public void RoundTrip_MultipleTimelines_PreservesData()
    {
        var original = new WorldColorTimelineResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 2;

        for (int i = 0; i < 3; i++)
        {
            original.ColorTimelines.Add(new ColorTimeline
            {
                Version = 13,
                PointOfInterestId = (uint)(i + 1),
                SunriseTime = 5.0f + i,
            });
        }

        var data = original.Data.ToArray();
        var parsed = new WorldColorTimelineResource(TestKey, data);

        parsed.Version.Should().Be(2);
        parsed.ColorTimelines.Should().HaveCount(3);
        parsed.ColorTimelines[0].PointOfInterestId.Should().Be(1);
        parsed.ColorTimelines[1].PointOfInterestId.Should().Be(2);
        parsed.ColorTimelines[2].PointOfInterestId.Should().Be(3);
        parsed.ColorTimelines[0].SunriseTime.Should().Be(5.0f);
        parsed.ColorTimelines[1].SunriseTime.Should().Be(6.0f);
        parsed.ColorTimelines[2].SunriseTime.Should().Be(7.0f);
    }

    [Fact]
    public void RoundTrip_FullResource_ByteForByte()
    {
        var original = new WorldColorTimelineResource(TestKey, ReadOnlyMemory<byte>.Empty);
        original.Version = 1;

        var timeline = new ColorTimeline
        {
            Version = 13,
            PointOfInterestId = 0xDEADBEEF,
            SunriseTime = 6.5f,
            SunsetTime = 18.75f,
            StarsAppearTime = 20.0f,
            StarsDisappearTime = 4.5f,
        };

        // Add some color data
        timeline.AmbientColors.ColorData.Add(new TimelineColorData(0.1f, 0.2f, 0.3f, 1.0f, 0.0f));
        timeline.DirectionalColors.ColorData.Add(new TimelineColorData(0.5f, 0.6f, 0.7f, 0.8f, 12.0f));

        original.ColorTimelines.Add(timeline);

        // Serialize twice
        var data1 = original.Data.ToArray();
        var parsed = new WorldColorTimelineResource(TestKey, data1);
        var data2 = parsed.Data.ToArray();

        data2.Should().BeEquivalentTo(data1, "round-trip should produce identical bytes");
    }

    [Fact]
    public void Factory_Create_ReturnsCorrectType()
    {
        var factory = new WorldColorTimelineResourceFactory();
        var resource = factory.Create(TestKey, ReadOnlyMemory<byte>.Empty);

        resource.Should().BeOfType<WorldColorTimelineResource>();
    }

    [Fact]
    public void Factory_CreateEmpty_ReturnsResourceWithDefaults()
    {
        var factory = new WorldColorTimelineResourceFactory();
        var resource = factory.CreateEmpty(TestKey) as WorldColorTimelineResource;

        resource.Should().NotBeNull();
        resource!.Version.Should().Be(1);
        resource.ColorTimelines.Should().BeEmpty();
    }
}
