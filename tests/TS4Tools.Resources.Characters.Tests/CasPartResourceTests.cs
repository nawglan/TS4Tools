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

namespace TS4Tools.Resources.Characters.Tests;

/// <summary>
/// Tests for the <see cref="CasPartResourceFactory"/> class.
/// </summary>
public class CasPartResourceFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CasPartResourceFactory> _logger;
    private readonly ILogger<CasPartResource> _resourceLogger;

    public CasPartResourceFactoryTests()
    {
        // Setup mock services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _serviceProvider = services.BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<ILogger<CasPartResourceFactory>>();
        _resourceLogger = _serviceProvider.GetRequiredService<ILogger<CasPartResource>>();
    }

    [Fact]
    public void Constructor_ValidArguments_ShouldNotThrow()
    {
        // Act & Assert
        var factory = new CasPartResourceFactory(_serviceProvider, _logger);
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CasPartResourceFactory(null!, _logger);
        action.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CasPartResourceFactory(_serviceProvider, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainCasPartResourceType()
    {
        // Arrange
        var factory = new CasPartResourceFactory(_serviceProvider, _logger);

        // Assert
        factory.SupportedResourceTypes.Should().Contain("0x034AEECB");
    }

    [Fact]
    public void Priority_ShouldReturn100()
    {
        // Arrange
        var factory = new CasPartResourceFactory(_serviceProvider, _logger);

        // Assert
        factory.Priority.Should().Be(100);
    }

    [Fact]
    public async Task CreateResourceAsync_NullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = new CasPartResourceFactory(_serviceProvider, _logger);

        // Act & Assert
        var action = () => factory.CreateResourceAsync(1, null);
        await action.Should().ThrowAsync<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public async Task CreateResourceAsync_InvalidStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        var factory = new CasPartResourceFactory(_serviceProvider, _logger);
        var invalidStream = new MemoryStream([0x00, 0x01, 0x02]); // Invalid CAS part data

        // Act & Assert
        var action = () => factory.CreateResourceAsync(1, invalidStream);
        await action.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public void GetDiagnosticInfo_ShouldReturnValidDictionary()
    {
        // Arrange
        var factory = new CasPartResourceFactory(_serviceProvider, _logger);

        // Act
        var diagnostics = factory.GetDiagnosticInfo();

        // Assert
        diagnostics.Should().NotBeNull();
        diagnostics.Should().ContainKey("FactoryType");
        diagnostics.Should().ContainKey("SupportedResourceTypes");
        diagnostics.Should().ContainKey("Priority");
        diagnostics.Should().ContainKey("CreatedAt");

        diagnostics["FactoryType"].Should().Be("CasPartResourceFactory");
        diagnostics["Priority"].Should().Be(100);
    }
}

/// <summary>
/// Tests for the <see cref="CasPartFlags"/> enumeration and related types.
/// </summary>
public class CasPartFlagsTests
{
    [Fact]
    public void AgeGenderFlags_AllAges_ShouldCombineAllAgeFlags()
    {
        // Act
        var allAges = AgeGenderFlags.AllAges;

        // Assert
        allAges.Should().HaveFlag(AgeGenderFlags.Baby);
        allAges.Should().HaveFlag(AgeGenderFlags.Toddler);
        allAges.Should().HaveFlag(AgeGenderFlags.Child);
        allAges.Should().HaveFlag(AgeGenderFlags.Teen);
        allAges.Should().HaveFlag(AgeGenderFlags.YoungAdult);
        allAges.Should().HaveFlag(AgeGenderFlags.Adult);
        allAges.Should().HaveFlag(AgeGenderFlags.Elder);
    }

    [Fact]
    public void AgeGenderFlags_AllGenders_ShouldCombineMaleAndFemale()
    {
        // Act
        var allGenders = AgeGenderFlags.AllGenders;

        // Assert
        allGenders.Should().HaveFlag(AgeGenderFlags.Male);
        allGenders.Should().HaveFlag(AgeGenderFlags.Female);
    }

    [Fact]
    public void AgeGenderFlags_All_ShouldCombineAllAgesAndGenders()
    {
        // Act
        var all = AgeGenderFlags.All;

        // Assert
        all.Should().HaveFlag(AgeGenderFlags.AllAges);
        all.Should().HaveFlag(AgeGenderFlags.AllGenders);
    }

    [Theory]
    [InlineData(BodyType.Hat, 1)]
    [InlineData(BodyType.Hair, 2)]
    [InlineData(BodyType.Face, 4)]
    [InlineData(BodyType.Body, 5)]
    public void BodyType_Values_ShouldHaveCorrectValues(BodyType bodyType, uint expectedValue)
    {
        // Assert
        ((uint)bodyType).Should().Be(expectedValue);
    }
}

/// <summary>
/// Tests for the <see cref="SwatchColor"/> structure.
/// </summary>
public class SwatchColorTests
{
    [Fact]
    public void SwatchColor_Constructor_ShouldSetProperties()
    {
        // Act
        var swatch = new SwatchColor(255, 128, 64, 192);

        // Assert
        swatch.Red.Should().Be(255);
        swatch.Green.Should().Be(128);
        swatch.Blue.Should().Be(64);
        swatch.Alpha.Should().Be(192);
    }

    [Fact]
    public void SwatchColor_DefaultAlpha_ShouldBe255()
    {
        // Act
        var swatch = new SwatchColor(255, 128, 64);

        // Assert
        swatch.Alpha.Should().Be(255);
    }

    [Fact]
    public void ToArgb_ShouldReturnCorrectValue()
    {
        // Arrange
        var swatch = new SwatchColor(255, 128, 64, 192);

        // Act
        var argb = swatch.ToArgb();

        // Assert - ARGB format: Alpha << 24 | Red << 16 | Green << 8 | Blue
        var expected = unchecked((uint)((192 << 24) | (255 << 16) | (128 << 8) | 64));
        argb.Should().Be(expected);
    }

    [Fact]
    public void FromArgb_ShouldCreateCorrectSwatchColor()
    {
        // Arrange
        var argb = unchecked((uint)((192 << 24) | (255 << 16) | (128 << 8) | 64));

        // Act
        var swatch = SwatchColor.FromArgb(argb);

        // Assert
        swatch.Alpha.Should().Be(192);
        swatch.Red.Should().Be(255);
        swatch.Green.Should().Be(128);
        swatch.Blue.Should().Be(64);
    }
}
