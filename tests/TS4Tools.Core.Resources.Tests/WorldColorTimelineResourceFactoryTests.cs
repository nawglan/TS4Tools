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

using FluentAssertions;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Resources;

namespace TS4Tools.Core.Resources.Tests;

/// <summary>
/// Unit tests for WorldColorTimelineResourceFactory implementation.
/// </summary>
public class WorldColorTimelineResourceFactoryTests
{
    private readonly WorldColorTimelineResourceFactory _factory = new();

    [Fact]
    public void SupportedResourceTypes_ShouldReturnCorrectTypes()
    {
        // Act
        var result = _factory.SupportedResourceTypes;

        // Assert
        result.Should().Contain("0x19301120");
    }

    [Fact]
    public void Priority_ShouldReturnCorrectPriority()
    {
        // Act
        var result = _factory.Priority;

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public async Task CreateResourceAsync_WithoutStream_ShouldReturnValidResource()
    {
        // Act
        var result = await _factory.CreateResourceAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IWorldColorTimelineResource>();

        var worldColorResource = result;
        worldColorResource.Version.Should().Be(14u);
        worldColorResource.ColorTimelines.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateResourceAsync_WithValidStream_ShouldReturnValidResource()
    {
        // Arrange
        var stream = CreateTestStream();

        // Act
        var result = await _factory.CreateResourceAsync(1, stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IWorldColorTimelineResource>();

        var worldColorResource = result;
        worldColorResource.Version.Should().Be(14u);
        worldColorResource.ColorTimelines.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ShouldNotThrow()
    {
        // Act & Assert - null stream should be valid for creating empty resource
        var action = async () => await _factory.CreateResourceAsync(1, null);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void CanCreateResource_WithMatchingTypeId_ShouldReturnTrue()
    {
        // Act
        var result = _factory.CanCreateResource(0x19301120u);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanCreateResource_WithDifferentTypeId_ShouldReturnFalse()
    {
        // Act
        var result = _factory.CanCreateResource(0x12345678u);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateResource_WithStreamAndType_ShouldReturnValidResource()
    {
        // Arrange
        var stream = CreateTestStream();

        // Act
        var result = _factory.CreateResource(stream, 0x19301120u);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IWorldColorTimelineResource>();
        result.Version.Should().Be(14u);
    }

    [Fact]
    public void CreateEmptyResource_WithType_ShouldReturnValidResource()
    {
        // Act
        var result = _factory.CreateEmptyResource(0x19301120u);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IWorldColorTimelineResource>();
        result.Version.Should().Be(14u);
        result.ColorTimelines.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatedResource_ShouldSupportRoundTripSerialization()
    {
        // Arrange
        var originalResource = await _factory.CreateResourceAsync(1);

        // Add test data
        var timeline = new WorldColorTimeline
        {
            Version = 14,
            PointOfInterestId = 42,
            SunriseTime = 0.25f,
            SunsetTime = 0.75f,
            RemapTimeline = true
        };

        var testColor = new TestColorData
        {
            Red = 1.0f,
            Green = 0.5f,
            Blue = 0.2f,
            Alpha = 0.8f,
            Time = 0.5f
        };

        timeline.AmbientColors.AddColorPoint(testColor);
        originalResource.AddColorTimeline(timeline);

        // Act - Save and reload
        using var saveStream = new MemoryStream();
        originalResource.Save(saveStream);

        saveStream.Position = 0;
        var reloadedResource = _factory.CreateResource(saveStream, 0x19301120u);

        // Assert
        reloadedResource.Version.Should().Be(originalResource.Version);
        reloadedResource.ColorTimelines.Should().HaveCount(1);

        var reloadedTimeline = reloadedResource.ColorTimelines[0];
        reloadedTimeline.Version.Should().Be(timeline.Version);
        reloadedTimeline.PointOfInterestId.Should().Be(timeline.PointOfInterestId);
        reloadedTimeline.SunriseTime.Should().Be(timeline.SunriseTime);
        reloadedTimeline.SunsetTime.Should().Be(timeline.SunsetTime);
        reloadedTimeline.RemapTimeline.Should().Be(timeline.RemapTimeline);

        reloadedTimeline.AmbientColors.ColorPoints.Should().HaveCount(1);
        var reloadedColor = reloadedTimeline.AmbientColors.ColorPoints[0];
        reloadedColor.Red.Should().Be(testColor.Red);
        reloadedColor.Green.Should().Be(testColor.Green);
        reloadedColor.Blue.Should().Be(testColor.Blue);
        reloadedColor.Alpha.Should().Be(testColor.Alpha);
        reloadedColor.Time.Should().Be(testColor.Time);
    }

    private static MemoryStream CreateTestStream()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, global::System.Text.Encoding.UTF8, leaveOpen: true);

        // Write resource version
        writer.Write(14u);

        // Write timeline count
        writer.Write(1u);

        // Write timeline version
        writer.Write(14u);

        // Write 29 empty color timeline data blocks
        for (int i = 0; i < 29; i++)
        {
            writer.Write(0u); // Color point count = 0
        }

        // Write POI ID (this comes after the 16th color timeline data block)
        writer.Write(123u);

        // Write time values
        writer.Write(0.25f); // SunriseTime
        writer.Write(0.75f); // SunsetTime
        writer.Write(0.9f);  // StarsAppearTime
        writer.Write(0.1f);  // StarsDisappearTime

        // Version 14 feature
        writer.Write(true); // RemapTimeline

        stream.Position = 0;
        return stream;
    }

    // Helper classes for testing - these would normally be the actual implementation classes
    private sealed class WorldColorTimeline : IColorTimeline
    {
        public uint Version { get; set; } = 14;
        public IColorTimelineData AmbientColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData DirectionalColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData ShadowColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData SkyHorizonColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData FogStartRange { get; set; } = new TestColorTimelineData();
        public IColorTimelineData FogEndRange { get; set; } = new TestColorTimelineData();
        public IColorTimelineData SkyHorizonDarkColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData SkyLightColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData SkyDarkColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData SunColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData HaloColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData SunDarkCloudColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData SunLightCloudColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData HorizonDarkCloudColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData HorizonLightCloudColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData CloudShadowCloudColors { get; set; } = new TestColorTimelineData();
        public uint PointOfInterestId { get; set; }
        public IColorTimelineData BloomThresholds { get; set; } = new TestColorTimelineData();
        public IColorTimelineData BloomIntensities { get; set; } = new TestColorTimelineData();
        public float SunriseTime { get; set; }
        public float SunsetTime { get; set; }
        public IColorTimelineData DenseFogColors { get; set; } = new TestColorTimelineData();
        public IColorTimelineData DenseFogStartRange { get; set; } = new TestColorTimelineData();
        public IColorTimelineData DenseFogEndRange { get; set; } = new TestColorTimelineData();
        public IColorTimelineData MoonRadiusMultipliers { get; set; } = new TestColorTimelineData();
        public IColorTimelineData SunRadiusMultipliers { get; set; } = new TestColorTimelineData();
        public float StarsAppearTime { get; set; }
        public float StarsDisappearTime { get; set; }
        public bool RemapTimeline { get; set; }
    }

    private sealed class TestColorTimelineData : IColorTimelineData
    {
        private readonly List<IColorData> _colorPoints = new();

        public IReadOnlyList<IColorData> ColorPoints => _colorPoints.AsReadOnly();

        public void AddColorPoint(IColorData colorData)
        {
            _colorPoints.Add(colorData);
        }

        public bool RemoveColorPoint(IColorData colorData)
        {
            return _colorPoints.Remove(colorData);
        }

        public void ClearColorPoints()
        {
            _colorPoints.Clear();
        }

        public IColorData GetColorAtTime(float time)
        {
            return new TestColorData { Time = time };
        }
    }

    private sealed class TestColorData : IColorData
    {
        public float Red { get; set; }
        public float Green { get; set; }
        public float Blue { get; set; }
        public float Alpha { get; set; } = 1.0f;
        public float Time { get; set; }
    }
}
