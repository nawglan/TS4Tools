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
/// Unit tests for EnvironmentResourceFactory implementation.
/// </summary>
public class EnvironmentResourceFactoryTests
{
    [Fact]
    public void CanCreate_WithSupportedResourceType_ShouldReturnTrue()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();
        var supportedTypes = new uint[] { 0xE5A5C6D8, 0x8E6B4F2A, 0x7C9D3E1B };

        // Act & Assert
        foreach (var resourceType in supportedTypes)
        {
            factory.CanCreate(resourceType).Should().BeTrue($"Resource type 0x{resourceType:X8} should be supported");
        }
    }

    [Fact]
    public void CanCreate_WithUnsupportedResourceType_ShouldReturnFalse()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();
        var unsupportedType = 0x12345678u;

        // Act
        var result = factory.CanCreate(unsupportedType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanCreate_WithStringResourceType_Environment_ShouldReturnTrue()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act
        var result = factory.CanCreate("ENVIRONMENT");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanCreate_WithStringResourceType_Weather_ShouldReturnTrue()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act
        var result = factory.CanCreate("WEATHER");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanCreate_WithStringResourceType_Season_ShouldReturnTrue()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act
        var result = factory.CanCreate("SEASON");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanCreate_WithStringResourceType_CaseInsensitive_ShouldReturnTrue()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act & Assert
        factory.CanCreate("environment").Should().BeTrue();
        factory.CanCreate("ENVIRONMENT").Should().BeTrue();
        factory.CanCreate("Environment").Should().BeTrue();
        factory.CanCreate("weather").Should().BeTrue();
        factory.CanCreate("WEATHER").Should().BeTrue();
        factory.CanCreate("Weather").Should().BeTrue();
        factory.CanCreate("season").Should().BeTrue();
        factory.CanCreate("SEASON").Should().BeTrue();
        factory.CanCreate("Season").Should().BeTrue();
    }

    [Fact]
    public void CanCreate_WithUnsupportedStringResourceType_ShouldReturnFalse()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act
        var result = factory.CanCreate("UNSUPPORTED");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutStream_ShouldReturnNewEnvironmentResource()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act
        var resource = factory.Create();

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<EnvironmentResource>();
        resource.Version.Should().Be(1u);
        resource.RegionalWeathers.Should().BeEmpty();
        resource.WeatherInterpolations.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithValidStream_ShouldReturnEnvironmentResourceWithData()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();
        var originalResource = new EnvironmentResource
        {
            Temperature = 25.0f,
            Humidity = 60.0f,
            WindSpeed = 10.0f,
            CurrentSeason = SeasonType.Summer,
            IsRaining = true
        };

        using var stream = new MemoryStream();
        originalResource.Save(stream);
        stream.Position = 0;

        // Act
        var resource = factory.Create(stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<EnvironmentResource>();
        resource.Temperature.Should().Be(25.0f);
        resource.Humidity.Should().Be(60.0f);
        resource.WindSpeed.Should().Be(10.0f);
        resource.CurrentSeason.Should().Be(SeasonType.Summer);
        resource.IsRaining.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act & Assert
        var action = () => factory.Create(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_WithoutStream_ShouldReturnNewEnvironmentResource()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act
        var resource = await factory.CreateAsync();

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<EnvironmentResource>();
        resource.Version.Should().Be(1u);
    }

    [Fact]
    public async Task CreateAsync_WithValidStream_ShouldReturnEnvironmentResourceWithData()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();
        var originalResource = new EnvironmentResource
        {
            Temperature = 22.0f,
            Humidity = 45.0f,
            WindSpeed = 7.5f,
            CurrentSeason = SeasonType.Winter,
            IsSnowing = true
        };

        using var stream = new MemoryStream();
        originalResource.Save(stream);
        stream.Position = 0;

        // Act
        var resource = await factory.CreateAsync(stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<EnvironmentResource>();
        resource.Temperature.Should().Be(22.0f);
        resource.Humidity.Should().Be(45.0f);
        resource.WindSpeed.Should().Be(7.5f);
        resource.CurrentSeason.Should().Be(SeasonType.Winter);
        resource.IsSnowing.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act & Assert
        var action = async () => await factory.CreateAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        var action = async () => await factory.CreateAsync(cancellationTokenSource.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CreateAsync_WithStreamAndCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();
        using var stream = new MemoryStream();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        var action = async () => await factory.CreateAsync(stream, cancellationTokenSource.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act
        var supportedTypes = factory.SupportedResourceTypes.ToArray();

        // Assert
        supportedTypes.Should().HaveCount(3);
        supportedTypes.Should().Contain($"0x{0xE5A5C6D8:X8}");
        supportedTypes.Should().Contain($"0x{0x8E6B4F2A:X8}");
        supportedTypes.Should().Contain($"0x{0x7C9D3E1B:X8}");
    }

    [Fact]
    public void SupportedResourceTypeNames_ShouldContainExpectedNames()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act
        var supportedNames = factory.SupportedResourceTypeNames.ToArray();

        // Assert
        supportedNames.Should().HaveCount(3);
        supportedNames.Should().Contain("ENVIRONMENT");
        supportedNames.Should().Contain("WEATHER");
        supportedNames.Should().Contain("SEASON");
    }

    [Fact]
    public void Factory_ShouldImplementIResourceFactory()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act & Assert
        factory.Should().BeAssignableTo<IResourceFactory<IEnvironmentResource>>();
    }

    [Fact]
    public void Factory_ShouldInheritFromResourceFactoryBase()
    {
        // Arrange
        var factory = new EnvironmentResourceFactory();

        // Act & Assert
        factory.Should().BeAssignableTo<ResourceFactoryBase<IEnvironmentResource>>();
    }
}
