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
using System.Text;

namespace TS4Tools.Core.Resources.Tests;

/// <summary>
/// Unit tests for EnvironmentResource implementation.
/// </summary>
public class EnvironmentResourceTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeCorrectly()
    {
        // Act
        var resource = new EnvironmentResource();

        // Assert
        resource.Version.Should().Be(1u);
        resource.RegionalWeathers.Should().BeEmpty();
        resource.WeatherInterpolations.Should().BeEmpty();
        resource.CurrentSeason.Should().Be(SeasonType.Spring);
        resource.CurrentMoonPhase.Should().Be(MoonPhase.NewMoon);
        resource.Temperature.Should().Be(20.0f);
        resource.Humidity.Should().Be(50.0f);
        resource.WindSpeed.Should().Be(5.0f);
        resource.IsRaining.Should().BeFalse();
        resource.IsSnowing.Should().BeFalse();
        resource.ThunderActive.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new EnvironmentResource(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddRegionalWeather_WithValidWeather_ShouldAddSuccessfully()
    {
        // Arrange
        var resource = new EnvironmentResource();
        var regionalWeather = new RegionalWeather
        {
            RegionId = 123,
            Temperature = 25.0f,
            Humidity = 60.0f,
            WindSpeed = 10.0f,
            IsRaining = true
        };

        // Act
        resource.AddRegionalWeather(regionalWeather);

        // Assert
        resource.RegionalWeathers.Should().HaveCount(1);
        resource.RegionalWeathers[0].Should().Be(regionalWeather);
    }

    [Fact]
    public void AddRegionalWeather_WithNullWeather_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resource = new EnvironmentResource();

        // Act & Assert
        var action = () => resource.AddRegionalWeather(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveRegionalWeather_WithExistingWeather_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var resource = new EnvironmentResource();
        var regionalWeather = new RegionalWeather { RegionId = 123 };
        resource.AddRegionalWeather(regionalWeather);

        // Act
        var result = resource.RemoveRegionalWeather(regionalWeather);

        // Assert
        result.Should().BeTrue();
        resource.RegionalWeathers.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRegionalWeather_WithNonExistentWeather_ShouldReturnFalse()
    {
        // Arrange
        var resource = new EnvironmentResource();
        var regionalWeather = new RegionalWeather { RegionId = 123 };

        // Act
        var result = resource.RemoveRegionalWeather(regionalWeather);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetRegionalWeatherByRegionId_WithExistingRegion_ShouldReturnWeather()
    {
        // Arrange
        var resource = new EnvironmentResource();
        var regionalWeather = new RegionalWeather { RegionId = 123, Temperature = 25.0f };
        resource.AddRegionalWeather(regionalWeather);

        // Act
        var result = resource.GetRegionalWeatherByRegionId(123);

        // Assert
        result.Should().Be(regionalWeather);
    }

    [Fact]
    public void GetRegionalWeatherByRegionId_WithNonExistentRegion_ShouldReturnNull()
    {
        // Arrange
        var resource = new EnvironmentResource();

        // Act
        var result = resource.GetRegionalWeatherByRegionId(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AddWeatherInterpolation_WithValidInterpolation_ShouldAddSuccessfully()
    {
        // Arrange
        var resource = new EnvironmentResource();
        var interpolation = new WeatherInterpolation
        {
            StartTime = (ulong)TimeSpan.FromHours(6).Ticks,
            EndTime = (ulong)TimeSpan.FromHours(18).Ticks,
            InterpolationType = WeatherInterpolationType.Temperature,
            TemperatureStart = 15.0f,
            TemperatureEnd = 30.0f
        };

        // Act
        resource.AddWeatherInterpolation(interpolation);

        // Assert
        resource.WeatherInterpolations.Should().HaveCount(1);
        resource.WeatherInterpolations[0].Should().Be(interpolation);
    }

    [Fact]
    public void AddWeatherInterpolation_WithNullInterpolation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resource = new EnvironmentResource();

        // Act & Assert
        var action = () => resource.AddWeatherInterpolation(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveWeatherInterpolation_WithExistingInterpolation_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var resource = new EnvironmentResource();
        var interpolation = new WeatherInterpolation();
        resource.AddWeatherInterpolation(interpolation);

        // Act
        var result = resource.RemoveWeatherInterpolation(interpolation);

        // Assert
        result.Should().BeTrue();
        resource.WeatherInterpolations.Should().BeEmpty();
    }

    [Fact]
    public void SeasonType_Enum_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<SeasonType>().Should().Contain([
            SeasonType.Spring,
            SeasonType.Summer,
            SeasonType.Fall,
            SeasonType.Winter
        ]);
    }

    [Fact]
    public void MoonPhase_Enum_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<MoonPhase>().Should().Contain([
            MoonPhase.NewMoon,
            MoonPhase.WaxingCrescent,
            MoonPhase.FirstQuarter,
            MoonPhase.WaxingGibbous,
            MoonPhase.FullMoon,
            MoonPhase.WaningGibbous,
            MoonPhase.ThirdQuarter,
            MoonPhase.WaningCrescent
        ]);
    }

    [Fact]
    public void WeatherInterpolationType_Enum_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<WeatherInterpolationType>().Should().Contain([
            WeatherInterpolationType.Season,
            WeatherInterpolationType.Temperature,
            WeatherInterpolationType.Rainfall,
            WeatherInterpolationType.Snowfall
        ]);
    }

    [Fact]
    public void GetWeatherForecast_WithValidForecastId_ShouldReturnForecastData()
    {
        // Arrange
        var resource = new EnvironmentResource();
        var forecastId = 12345UL;

        // Add some weather interpolations that should match this forecast
        var interpolation = new WeatherInterpolation
        {
            StartTime = (ulong)TimeSpan.FromHours(8).Ticks,
            EndTime = (ulong)TimeSpan.FromHours(20).Ticks,
            InterpolationType = WeatherInterpolationType.Temperature,
            TemperatureStart = 15.0f,
            TemperatureEnd = 30.0f
        };
        resource.AddWeatherInterpolation(interpolation);

        // Act
        var forecast = resource.GetWeatherForecast(forecastId);

        // Assert
        forecast.Should().NotBeNull();
    }

    [Fact]
    public void UpdateWeatherConditions_WithValidParameters_ShouldUpdateCorrectly()
    {
        // Arrange
        var resource = new EnvironmentResource();
        var temperature = 25.5f;
        var humidity = 65.0f;
        var windSpeed = 15.0f;

        // Act
        resource.UpdateWeatherConditions(temperature, humidity, windSpeed);

        // Assert
        resource.Temperature.Should().Be(temperature);
        resource.Humidity.Should().Be(humidity);
        resource.WindSpeed.Should().Be(windSpeed);
    }

    [Fact]
    public void IsWeatherEvent_WhenRaining_ShouldReturnTrue()
    {
        // Arrange
        var resource = new EnvironmentResource();

        // Act
        resource.IsRaining = true;

        // Assert
        resource.IsWeatherEvent("rain").Should().BeTrue();
    }

    [Fact]
    public void IsWeatherEvent_WhenSnowing_ShouldReturnTrue()
    {
        // Arrange
        var resource = new EnvironmentResource();

        // Act
        resource.IsSnowing = true;

        // Assert
        resource.IsWeatherEvent("snow").Should().BeTrue();
    }

    [Fact]
    public void IsWeatherEvent_WhenThunderActive_ShouldReturnTrue()
    {
        // Arrange
        var resource = new EnvironmentResource();

        // Act
        resource.ThunderActive = true;

        // Assert
        resource.IsWeatherEvent("thunder").Should().BeTrue();
    }

    [Fact]
    public void IsWeatherEvent_WhenNoEvents_ShouldReturnFalse()
    {
        // Arrange
        var resource = new EnvironmentResource();

        // Act & Assert (all weather events should be false by default)
        resource.IsWeatherEvent("any").Should().BeFalse();
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalResource = new EnvironmentResource
        {
            Version = 2u,
            CurrentSeason = SeasonType.Summer,
            CurrentMoonPhase = MoonPhase.FullMoon,
            Temperature = 28.5f,
            Humidity = 75.0f,
            WindSpeed = 12.5f,
            IsRaining = true,
            IsSnowing = false,
            ThunderActive = true
        };

        var regionalWeather = new RegionalWeather
        {
            RegionId = 456,
            Temperature = 30.0f,
            Humidity = 80.0f,
            WindSpeed = 15.0f,
            IsRaining = true
        };
        originalResource.AddRegionalWeather(regionalWeather);

        var interpolation = new WeatherInterpolation
        {
            StartTime = (ulong)TimeSpan.FromHours(8).Ticks,
            EndTime = (ulong)TimeSpan.FromHours(20).Ticks,
            InterpolationType = WeatherInterpolationType.Temperature,
            TemperatureStart = 20.0f,
            TemperatureEnd = 35.0f
        };
        originalResource.AddWeatherInterpolation(interpolation);

        using var stream = new MemoryStream();

        // Act
        originalResource.Save(stream);
        stream.Position = 0;
        var loadedResource = new EnvironmentResource(stream);

        // Assert
        loadedResource.Version.Should().Be(originalResource.Version);
        loadedResource.CurrentSeason.Should().Be(originalResource.CurrentSeason);
        loadedResource.CurrentMoonPhase.Should().Be(originalResource.CurrentMoonPhase);
        loadedResource.Temperature.Should().Be(originalResource.Temperature);
        loadedResource.Humidity.Should().Be(originalResource.Humidity);
        loadedResource.WindSpeed.Should().Be(originalResource.WindSpeed);
        loadedResource.IsRaining.Should().Be(originalResource.IsRaining);
        loadedResource.IsSnowing.Should().Be(originalResource.IsSnowing);
        loadedResource.ThunderActive.Should().Be(originalResource.ThunderActive);

        loadedResource.RegionalWeathers.Should().HaveCount(1);
        var loadedRegionalWeather = loadedResource.RegionalWeathers[0];
        loadedRegionalWeather.RegionId.Should().Be(regionalWeather.RegionId);
        loadedRegionalWeather.Temperature.Should().Be(regionalWeather.Temperature);
        loadedRegionalWeather.Humidity.Should().Be(regionalWeather.Humidity);
        loadedRegionalWeather.WindSpeed.Should().Be(regionalWeather.WindSpeed);
        loadedRegionalWeather.IsRaining.Should().Be(regionalWeather.IsRaining);

        loadedResource.WeatherInterpolations.Should().HaveCount(1);
        var loadedInterpolation = loadedResource.WeatherInterpolations[0];
        loadedInterpolation.StartTime.Should().Be(interpolation.StartTime);
        loadedInterpolation.EndTime.Should().Be(interpolation.EndTime);
        loadedInterpolation.InterpolationType.Should().Be(interpolation.InterpolationType);
        loadedInterpolation.TemperatureStart.Should().Be(interpolation.TemperatureStart);
        loadedInterpolation.TemperatureEnd.Should().Be(interpolation.TemperatureEnd);
    }

    [Fact]
    public void Save_WithEmptyData_ShouldCreateValidStream()
    {
        // Arrange
        var resource = new EnvironmentResource();
        using var stream = new MemoryStream();

        // Act
        resource.Save(stream);

        // Assert
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Load_WithValidStream_ShouldLoadCorrectly()
    {
        // Arrange
        var originalResource = new EnvironmentResource
        {
            Temperature = 22.0f,
            Humidity = 55.0f,
            WindSpeed = 8.0f,
            CurrentSeason = SeasonType.Fall
        };

        using var stream = new MemoryStream();
        originalResource.Save(stream);
        stream.Position = 0;

        // Act
        var loadedResource = new EnvironmentResource();
        var loadTask = loadedResource.LoadAsync(stream);

        // Assert
        loadTask.Should().NotBeNull();
        loadTask.IsCompletedSuccessfully.Should().BeTrue();
        loadedResource.Temperature.Should().Be(22.0f);
        loadedResource.Humidity.Should().Be(55.0f);
        loadedResource.WindSpeed.Should().Be(8.0f);
        loadedResource.CurrentSeason.Should().Be(SeasonType.Fall);
    }

    [Fact]
    public void Dispose_ShouldClearCollections()
    {
        // Arrange
        var resource = new EnvironmentResource();
        resource.AddRegionalWeather(new RegionalWeather { RegionId = 1 });
        resource.AddWeatherInterpolation(new WeatherInterpolation());

        // Act
        resource.Dispose();

        // Assert
        resource.RegionalWeathers.Should().BeEmpty();
        resource.WeatherInterpolations.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var resource = new EnvironmentResource();

        // Act & Assert
        var action = () =>
        {
            resource.Dispose();
            resource.Dispose();
        };
        action.Should().NotThrow();
    }
}
