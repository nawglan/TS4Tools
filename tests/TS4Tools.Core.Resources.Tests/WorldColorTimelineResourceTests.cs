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
/// Unit tests for WorldColorTimelineResource implementation.
/// </summary>
public class WorldColorTimelineResourceTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeCorrectly()
    {
        // Act
        var resource = new WorldColorTimelineResource();

        // Assert
        resource.Version.Should().Be(14u);
        resource.ColorTimelines.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new WorldColorTimelineResource(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddColorTimeline_WithValidTimeline_ShouldAddSuccessfully()
    {
        // Arrange
        var resource = new WorldColorTimelineResource();
        var timeline = new ColorTimeline
        {
            Version = 14,
            PointOfInterestId = 123,
            SunriseTime = 0.25f,
            SunsetTime = 0.75f
        };

        // Act
        resource.AddColorTimeline(timeline);

        // Assert
        resource.ColorTimelines.Should().HaveCount(1);
        resource.ColorTimelines[0].Should().Be(timeline);
    }

    [Fact]
    public void AddColorTimeline_WithNullTimeline_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resource = new WorldColorTimelineResource();

        // Act & Assert
        var action = () => resource.AddColorTimeline(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveColorTimeline_WithExistingTimeline_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var resource = new WorldColorTimelineResource();
        var timeline = new ColorTimeline();
        resource.AddColorTimeline(timeline);

        // Act
        var result = resource.RemoveColorTimeline(timeline);

        // Assert
        result.Should().BeTrue();
        resource.ColorTimelines.Should().BeEmpty();
    }

    [Fact]
    public void RemoveColorTimeline_WithNonExistentTimeline_ShouldReturnFalse()
    {
        // Arrange
        var resource = new WorldColorTimelineResource();
        var timeline = new ColorTimeline();

        // Act
        var result = resource.RemoveColorTimeline(timeline);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveColorTimeline_WithNullTimeline_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resource = new WorldColorTimelineResource();

        // Act & Assert
        var action = () => resource.RemoveColorTimeline(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClearColorTimelines_WithTimelines_ShouldRemoveAll()
    {
        // Arrange
        var resource = new WorldColorTimelineResource();
        resource.AddColorTimeline(new ColorTimeline());
        resource.AddColorTimeline(new ColorTimeline());

        // Act
        resource.ClearColorTimelines();

        // Assert
        resource.ColorTimelines.Should().BeEmpty();
    }

    [Fact]
    public void ClearColorTimelines_WithEmptyCollection_ShouldNotThrow()
    {
        // Arrange
        var resource = new WorldColorTimelineResource();

        // Act & Assert
        var action = () => resource.ClearColorTimelines();
        action.Should().NotThrow();
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalResource = CreateTestResource();

        // Act - Save
        using var saveStream = new MemoryStream();
        originalResource.Save(saveStream);

        // Act - Load
        saveStream.Position = 0;
        var loadedResource = new WorldColorTimelineResource(saveStream);

        // Assert
        loadedResource.Version.Should().Be(originalResource.Version);
        loadedResource.ColorTimelines.Should().HaveCount(originalResource.ColorTimelines.Count);

        var originalTimeline = originalResource.ColorTimelines[0];
        var loadedTimeline = loadedResource.ColorTimelines[0];

        loadedTimeline.Version.Should().Be(originalTimeline.Version);
        loadedTimeline.PointOfInterestId.Should().Be(originalTimeline.PointOfInterestId);
        loadedTimeline.SunriseTime.Should().Be(originalTimeline.SunriseTime);
        loadedTimeline.SunsetTime.Should().Be(originalTimeline.SunsetTime);
        loadedTimeline.StarsAppearTime.Should().Be(originalTimeline.StarsAppearTime);
        loadedTimeline.StarsDisappearTime.Should().Be(originalTimeline.StarsDisappearTime);
        loadedTimeline.RemapTimeline.Should().Be(originalTimeline.RemapTimeline);
    }

    [Fact]
    public void Load_WithUnsupportedVersion_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(99u); // Unsupported version
        writer.Write(0u);  // No timelines

        stream.Position = 0;

        // Act & Assert
        var action = () => new WorldColorTimelineResource(stream);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unsupported WorldColorTimeline version: 99*");
    }

    [Fact]
    public void Load_WithVersion13_ShouldLoadSuccessfully()
    {
        // Arrange
        using var stream = CreateTestStreamWithVersion(13);

        // Act
        var resource = new WorldColorTimelineResource(stream);

        // Assert
        resource.Version.Should().Be(13u);
        resource.ColorTimelines.Should().HaveCount(1);
    }

    [Fact]
    public void Load_WithVersion14_ShouldLoadWithRemapFeature()
    {
        // Arrange
        using var stream = CreateTestStreamWithVersion(14, includeRemapFeature: true);

        // Act
        var resource = new WorldColorTimelineResource(stream);

        // Assert
        resource.Version.Should().Be(14u);
        resource.ColorTimelines.Should().HaveCount(1);
        resource.ColorTimelines[0].RemapTimeline.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnDescriptiveString()
    {
        // Arrange
        var resource = new WorldColorTimelineResource();
        resource.AddColorTimeline(new ColorTimeline());
        resource.AddColorTimeline(new ColorTimeline());

        // Act
        var result = resource.ToString();

        // Assert
        result.Should().Be("WorldColorTimelineResource (v14) - 2 timelines");
    }

    private static WorldColorTimelineResource CreateTestResource()
    {
        var resource = new WorldColorTimelineResource { Version = 14 };

        var timeline = new ColorTimeline
        {
            Version = 14,
            PointOfInterestId = 42,
            SunriseTime = 0.2f,
            SunsetTime = 0.8f,
            StarsAppearTime = 0.9f,
            StarsDisappearTime = 0.1f,
            RemapTimeline = true
        };

        // Add test color data
        var testColor = new ColorData
        {
            Red = 1.0f,
            Green = 0.5f,
            Blue = 0.0f,
            Alpha = 0.8f,
            Time = 0.5f
        };

        timeline.AmbientColors.AddColorPoint(testColor);
        timeline.DirectionalColors.AddColorPoint(testColor);
        timeline.SunColors.AddColorPoint(testColor);

        resource.AddColorTimeline(timeline);
        return resource;
    }

    private static MemoryStream CreateTestStreamWithVersion(uint version, bool includeRemapFeature = false)
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, global::System.Text.Encoding.UTF8, leaveOpen: true);

        // Write resource version
        writer.Write(version);

        // Write timeline count
        writer.Write(1u);

        // Write timeline version
        writer.Write(version);

        // Write first 16 empty color timeline data blocks
        for (int i = 0; i < 16; i++)
        {
            writer.Write(0u); // Color point count = 0
        }

        // Write POI ID
        writer.Write(123u);

        // Write 2 more empty color timeline data blocks (bloom data)
        for (int i = 0; i < 2; i++)
        {
            writer.Write(0u); // Color point count = 0
        }

        // Write time values
        writer.Write(0.25f); // SunriseTime
        writer.Write(0.75f); // SunsetTime

        // Write 5 more empty color timeline data blocks (fog and radius data)
        for (int i = 0; i < 5; i++)
        {
            writer.Write(0u); // Color point count = 0
        }

        // Write more time values
        writer.Write(0.9f);  // StarsAppearTime
        writer.Write(0.1f);  // StarsDisappearTime

        // Version 14 feature (per timeline)
        if (version >= 14)
        {
            writer.Write(includeRemapFeature); // RemapTimeline for this timeline
        }

        stream.Position = 0;
        return stream;
    }
}

/// <summary>
/// Unit tests for ColorTimeline implementation.
/// </summary>
public class ColorTimelineTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var timeline = new ColorTimeline();

        // Assert
        timeline.Version.Should().Be(14u);
        timeline.PointOfInterestId.Should().Be(0u);
        timeline.SunriseTime.Should().Be(0f);
        timeline.SunsetTime.Should().Be(0f);
        timeline.StarsAppearTime.Should().Be(0f);
        timeline.StarsDisappearTime.Should().Be(0f);
        timeline.RemapTimeline.Should().BeFalse();

        // Check that all color timeline properties are initialized
        timeline.AmbientColors.Should().NotBeNull();
        timeline.DirectionalColors.Should().NotBeNull();
        timeline.ShadowColors.Should().NotBeNull();
        timeline.SkyHorizonColors.Should().NotBeNull();
        timeline.SunColors.Should().NotBeNull();
        timeline.BloomThresholds.Should().NotBeNull();
        timeline.DenseFogColors.Should().NotBeNull();
    }

    [Fact]
    public void Properties_ShouldBeReadWritable()
    {
        // Arrange
        var timeline = new ColorTimeline();
        const uint expectedVersion = 13;
        const uint expectedPoiId = 999;
        const float expectedSunrise = 0.3f;
        const float expectedSunset = 0.7f;

        // Act
        timeline.Version = expectedVersion;
        timeline.PointOfInterestId = expectedPoiId;
        timeline.SunriseTime = expectedSunrise;
        timeline.SunsetTime = expectedSunset;
        timeline.RemapTimeline = true;

        // Assert
        timeline.Version.Should().Be(expectedVersion);
        timeline.PointOfInterestId.Should().Be(expectedPoiId);
        timeline.SunriseTime.Should().Be(expectedSunrise);
        timeline.SunsetTime.Should().Be(expectedSunset);
        timeline.RemapTimeline.Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for ColorTimelineData implementation.
/// </summary>
public class ColorTimelineDataTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmpty()
    {
        // Act
        var data = new ColorTimelineData();

        // Assert
        data.ColorPoints.Should().BeEmpty();
    }

    [Fact]
    public void AddColorPoint_WithValidData_ShouldAddAndSort()
    {
        // Arrange
        var data = new ColorTimelineData();
        var point1 = new ColorData { Time = 0.8f, Red = 1.0f };
        var point2 = new ColorData { Time = 0.2f, Green = 1.0f };
        var point3 = new ColorData { Time = 0.5f, Blue = 1.0f };

        // Act
        data.AddColorPoint(point1);
        data.AddColorPoint(point2);
        data.AddColorPoint(point3);

        // Assert
        data.ColorPoints.Should().HaveCount(3);
        data.ColorPoints[0].Time.Should().Be(0.2f); // Sorted by time
        data.ColorPoints[1].Time.Should().Be(0.5f);
        data.ColorPoints[2].Time.Should().Be(0.8f);
    }

    [Fact]
    public void AddColorPoint_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var data = new ColorTimelineData();

        // Act & Assert
        var action = () => data.AddColorPoint(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveColorPoint_WithExistingPoint_ShouldReturnTrue()
    {
        // Arrange
        var data = new ColorTimelineData();
        var point = new ColorData { Time = 0.5f };
        data.AddColorPoint(point);

        // Act
        var result = data.RemoveColorPoint(point);

        // Assert
        result.Should().BeTrue();
        data.ColorPoints.Should().BeEmpty();
    }

    [Fact]
    public void GetColorAtTime_WithEmptyData_ShouldReturnDefaultColor()
    {
        // Arrange
        var data = new ColorTimelineData();

        // Act
        var result = data.GetColorAtTime(0.5f);

        // Assert
        result.Time.Should().Be(0.5f);
        result.Red.Should().Be(0f);
        result.Green.Should().Be(0f);
        result.Blue.Should().Be(0f);
        result.Alpha.Should().Be(0f);
    }

    [Fact]
    public void GetColorAtTime_WithSinglePoint_ShouldReturnThatColor()
    {
        // Arrange
        var data = new ColorTimelineData();
        var point = new ColorData { Time = 0.3f, Red = 1.0f, Green = 0.5f, Blue = 0.2f, Alpha = 0.8f };
        data.AddColorPoint(point);

        // Act
        var result = data.GetColorAtTime(0.7f);

        // Assert
        result.Time.Should().Be(0.7f);
        result.Red.Should().Be(1.0f);
        result.Green.Should().Be(0.5f);
        result.Blue.Should().Be(0.2f);
        result.Alpha.Should().Be(0.8f);
    }

    [Fact]
    public void GetColorAtTime_WithInterpolation_ShouldReturnLerpedColor()
    {
        // Arrange
        var data = new ColorTimelineData();
        var point1 = new ColorData { Time = 0.2f, Red = 0.0f, Green = 0.0f, Blue = 0.0f, Alpha = 0.0f };
        var point2 = new ColorData { Time = 0.8f, Red = 1.0f, Green = 1.0f, Blue = 1.0f, Alpha = 1.0f };
        data.AddColorPoint(point1);
        data.AddColorPoint(point2);

        // Act - Get color at midpoint
        var result = data.GetColorAtTime(0.5f);

        // Assert - Should be 50% interpolated
        result.Time.Should().Be(0.5f);
        result.Red.Should().BeApproximately(0.5f, 0.001f);
        result.Green.Should().BeApproximately(0.5f, 0.001f);
        result.Blue.Should().BeApproximately(0.5f, 0.001f);
        result.Alpha.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void GetColorAtTime_BeforeFirstPoint_ShouldReturnFirstPoint()
    {
        // Arrange
        var data = new ColorTimelineData();
        var point = new ColorData { Time = 0.5f, Red = 1.0f };
        data.AddColorPoint(point);

        // Act
        var result = data.GetColorAtTime(0.2f);

        // Assert
        result.Time.Should().Be(0.2f);
        result.Red.Should().Be(1.0f);
    }

    [Fact]
    public void GetColorAtTime_AfterLastPoint_ShouldReturnLastPoint()
    {
        // Arrange
        var data = new ColorTimelineData();
        var point = new ColorData { Time = 0.3f, Green = 1.0f };
        data.AddColorPoint(point);

        // Act
        var result = data.GetColorAtTime(0.8f);

        // Assert
        result.Time.Should().Be(0.8f);
        result.Green.Should().Be(1.0f);
    }

    [Fact]
    public void ClearColorPoints_ShouldRemoveAllPoints()
    {
        // Arrange
        var data = new ColorTimelineData();
        data.AddColorPoint(new ColorData { Time = 0.1f });
        data.AddColorPoint(new ColorData { Time = 0.5f });

        // Act
        data.ClearColorPoints();

        // Assert
        data.ColorPoints.Should().BeEmpty();
    }
}

/// <summary>
/// Unit tests for ColorData implementation.
/// </summary>
public class ColorDataTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var colorData = new ColorData();

        // Assert
        colorData.Red.Should().Be(0f);
        colorData.Green.Should().Be(0f);
        colorData.Blue.Should().Be(0f);
        colorData.Alpha.Should().Be(1.0f); // Alpha defaults to 1.0
        colorData.Time.Should().Be(0f);
    }

    [Fact]
    public void Properties_ShouldBeReadWritable()
    {
        // Arrange
        var colorData = new ColorData();
        const float expectedRed = 0.8f;
        const float expectedGreen = 0.6f;
        const float expectedBlue = 0.4f;
        const float expectedAlpha = 0.2f;
        const float expectedTime = 0.75f;

        // Act
        colorData.Red = expectedRed;
        colorData.Green = expectedGreen;
        colorData.Blue = expectedBlue;
        colorData.Alpha = expectedAlpha;
        colorData.Time = expectedTime;

        // Assert
        colorData.Red.Should().Be(expectedRed);
        colorData.Green.Should().Be(expectedGreen);
        colorData.Blue.Should().Be(expectedBlue);
        colorData.Alpha.Should().Be(expectedAlpha);
        colorData.Time.Should().Be(expectedTime);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var colorData = new ColorData
        {
            Red = 1.0f,
            Green = 0.5f,
            Blue = 0.25f,
            Alpha = 0.8f,
            Time = 0.333f
        };

        // Act
        var result = colorData.ToString();

        // Assert
        result.Should().Be("RGBA(1.000, 0.500, 0.250, 0.800) @ 0.333");
    }
}
