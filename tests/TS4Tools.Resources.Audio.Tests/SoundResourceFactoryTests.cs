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
using Microsoft.Extensions.Logging;
using NSubstitute;
using TS4Tools.Resources.Audio;

namespace TS4Tools.Resources.Audio.Tests;

public sealed class SoundResourceFactoryTests
{
    private readonly ILogger<SoundResource> _logger;
    private readonly SoundResourceFactory _factory;

    public SoundResourceFactoryTests()
    {
        _logger = Substitute.For<ILogger<SoundResource>>();
        _factory = new SoundResourceFactory(_logger);
    }

    [Fact]
    public void Constructor_WithNullSoundResourceLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new SoundResourceFactory(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SupportedResourceTypes_ShouldContainExpectedTypes()
    {
        // Arrange
        var expectedTypes = new HashSet<string>
        {
            "0x029E333B", // Audio controllers
            "0x02C9EFF2", // Audio Submix
            "0x1B25A024", // Sound properties
            "0xC202C770", // Music data file
            "0xD2DC5BAD"  // Ambience
        };

        // Act & Assert
        _factory.SupportedResourceTypes.Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void Priority_ShouldBe100()
    {
        // Act & Assert
        _factory.Priority.Should().Be(100);
    }

    [Fact]
    public async Task CreateResourceAsync_WithNullStream_ShouldCreateEmptyResource()
    {
        // Act
        var resource = await _factory.CreateResourceAsync(1, null);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<SoundResource>();
        resource.AudioData.IsEmpty.Should().BeTrue();
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public async Task CreateResourceAsync_WithStream_ShouldCreateResourceFromStream()
    {
        // Arrange
        var audioData = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 }; // WAV header
        using var stream = new MemoryStream(audioData);

        // Act
        var resource = await _factory.CreateResourceAsync(1, stream);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<SoundResource>();
        resource.AudioData.Length.Should().Be(audioData.Length);
        resource.Format.Should().Be(AudioFormat.Wav);
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Theory]
    [InlineData(0x029E333Bu)] // Audio controllers
    [InlineData(0x02C9EFF2u)] // Audio Submix
    [InlineData(0x1B25A024u)] // Sound properties
    [InlineData(0xC202C770u)] // Music data file
    [InlineData(0xD2DC5BADu)] // Ambience
    public void CreateResource_WithSupportedResourceType_ShouldCreateResource(uint resourceType)
    {
        // Arrange
        var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        using var stream = new MemoryStream(audioData);

        // Act
        var resource = _factory.CreateResource(stream, resourceType);

        // Assert
        resource.Should().NotBeNull();
        resource.Should().BeOfType<SoundResource>();
        resource.AudioData.Length.Should().Be(audioData.Length);
    }

    [Fact]
    public void CreateResource_WithUnsupportedResourceType_ShouldThrowArgumentException()
    {
        // Arrange
        var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        using var stream = new MemoryStream(audioData);
        var unsupportedType = 0x12345678u;

        // Act & Assert
        var action = () => _factory.CreateResource(stream, unsupportedType);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Unsupported resource type: 0x12345678*");
    }

    [Fact]
    public void CreateResource_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var supportedType = 0x029E333Bu;

        // Act & Assert
        var action = () => _factory.CreateResource(null!, supportedType);
        action.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 }, AudioFormat.Wav)]
    [InlineData(new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00 }, AudioFormat.Mp3)]
    [InlineData(new byte[] { 0x4F, 0x67, 0x67, 0x53 }, AudioFormat.Ogg)]
    [InlineData(new byte[] { 0x66, 0x4C, 0x61, 0x43 }, AudioFormat.Flac)]
    [InlineData(new byte[] { 0x00, 0x01, 0x02, 0x03 }, AudioFormat.SimsAudio)]
    public void DetectAudioFormat_WithVariousFormats_ShouldReturnCorrectFormat(byte[] data, AudioFormat expectedFormat)
    {
        // Act
        var format = SoundResourceFactory.DetectAudioFormat(data);

        // Assert
        format.Should().Be(expectedFormat);
    }

    [Fact]
    public void GetSupportedFormats_ShouldReturnFormatsWithDescriptions()
    {
        // Act
        var formats = SoundResourceFactory.GetSupportedFormats();

        // Assert
        formats.Should().NotBeEmpty();
        formats.Should().ContainKey(AudioFormat.Wav);
        formats.Should().ContainKey(AudioFormat.Mp3);
        formats.Should().ContainKey(AudioFormat.Ogg);
        formats.Should().ContainKey(AudioFormat.Aac);
        formats.Should().ContainKey(AudioFormat.Flac);
        formats.Should().ContainKey(AudioFormat.SimsAudio);

        formats[AudioFormat.Wav].Should().Contain("WAV");
        formats[AudioFormat.Mp3].Should().Contain("MP3");
        formats[AudioFormat.Ogg].Should().Contain("OGG");
    }

    [Fact]
    public void GetSupportedFormats_AllFormats_ShouldHaveDescriptions()
    {
        // Act
        var formats = SoundResourceFactory.GetSupportedFormats();

        // Assert
        foreach (var format in formats)
        {
            format.Value.Should().NotBeNullOrWhiteSpace($"Format {format.Key} should have a description");
        }
    }
}
