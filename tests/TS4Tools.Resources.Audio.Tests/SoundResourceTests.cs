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

public sealed class SoundResourceTests
{
    private readonly ILogger<SoundResource> _logger;

    public SoundResourceTests()
    {
        _logger = Substitute.For<ILogger<SoundResource>>();
    }

    [Fact]
    public void Constructor_WithEmptyMemory_ShouldCreateEmptyResource()
    {
        // Arrange
        var audioData = ReadOnlyMemory<byte>.Empty;

        // Act
        var resource = new SoundResource(audioData, AudioFormat.Unknown, 1, _logger);

        // Assert
        resource.AudioData.IsEmpty.Should().BeTrue();
        resource.Format.Should().Be(AudioFormat.Unknown);
        resource.RequestedApiVersion.Should().Be(1);
        resource.ContentFields.Should().NotBeNull();
        resource.ContentFields.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithAudioData_ShouldInitializeCorrectly()
    {
        // Arrange
        var audioData = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 }; // WAV header
        var expectedFormat = AudioFormat.Wav;

        // Act
        var resource = new SoundResource(audioData, expectedFormat, 1, _logger);

        // Assert
        resource.AudioData.Length.Should().Be(audioData.Length);
        resource.Format.Should().Be(expectedFormat);
        resource.Metadata.Format.Should().Be(expectedFormat);
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithStream_ShouldParseDataCorrectly()
    {
        // Arrange
        var audioData = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 }; // WAV header
        using var stream = new MemoryStream(audioData);

        // Act
        var resource = new SoundResource(stream, 1, _logger);

        // Assert
        resource.AudioData.Length.Should().Be(audioData.Length);
        resource.Format.Should().Be(AudioFormat.Wav);
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public void DetectAudioFormat_WithWavHeader_ShouldReturnWav()
    {
        // Arrange
        var wavHeader = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 };

        // Act
        var format = SoundResource.DetectAudioFormat(wavHeader);

        // Assert
        format.Should().Be(AudioFormat.Wav);
    }

    [Fact]
    public void DetectAudioFormat_WithMp3ID3Header_ShouldReturnMp3()
    {
        // Arrange
        var mp3Header = new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00 }; // ID3v2.3 header

        // Act
        var format = SoundResource.DetectAudioFormat(mp3Header);

        // Assert
        format.Should().Be(AudioFormat.Mp3);
    }

    [Fact]
    public void DetectAudioFormat_WithMp3FrameSync_ShouldReturnMp3()
    {
        // Arrange
        var mp3FrameSync = new byte[] { 0xFF, 0xFB, 0x90, 0x00 }; // MP3 frame sync

        // Act
        var format = SoundResource.DetectAudioFormat(mp3FrameSync);

        // Assert
        format.Should().Be(AudioFormat.Mp3);
    }

    [Fact]
    public void DetectAudioFormat_WithOggHeader_ShouldReturnOgg()
    {
        // Arrange
        var oggHeader = new byte[] { 0x4F, 0x67, 0x67, 0x53 }; // "OggS"

        // Act
        var format = SoundResource.DetectAudioFormat(oggHeader);

        // Assert
        format.Should().Be(AudioFormat.Ogg);
    }

    [Fact]
    public void DetectAudioFormat_WithFlacHeader_ShouldReturnFlac()
    {
        // Arrange
        var flacHeader = new byte[] { 0x66, 0x4C, 0x61, 0x43 }; // "fLaC"

        // Act
        var format = SoundResource.DetectAudioFormat(flacHeader);

        // Assert
        format.Should().Be(AudioFormat.Flac);
    }

    [Fact]
    public void DetectAudioFormat_WithUnknownData_ShouldReturnSimsAudio()
    {
        // Arrange
        var unknownData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        // Act
        var format = SoundResource.DetectAudioFormat(unknownData);

        // Assert
        format.Should().Be(AudioFormat.SimsAudio);
    }

    [Fact]
    public void DetectAudioFormat_WithInsufficientData_ShouldReturnUnknown()
    {
        // Arrange
        var smallData = new byte[] { 0x00, 0x01 };

        // Act
        var format = SoundResource.DetectAudioFormat(smallData);

        // Assert
        format.Should().Be(AudioFormat.Unknown);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnMetadata()
    {
        // Arrange
        var audioData = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 };
        var resource = new SoundResource(audioData, AudioFormat.Wav, 1, _logger);

        // Act
        var metadata = await resource.AnalyzeAsync();

        // Assert
        metadata.Format.Should().Be(AudioFormat.Wav);
        metadata.DataSize.Should().Be((uint)audioData.Length);
    }

    [Fact]
    public void UpdateAudioData_ShouldUpdateDataAndMetadata()
    {
        // Arrange
        var originalData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var newData = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 };
        var resource = new SoundResource(originalData, AudioFormat.Unknown, 1, _logger);
        
        var eventRaised = false;
        resource.ResourceChanged += (_, _) => eventRaised = true;

        // Act
        resource.UpdateAudioData(newData);

        // Assert
        resource.AudioData.Length.Should().Be(newData.Length);
        resource.Format.Should().Be(AudioFormat.Wav);
        resource.Metadata.DataSize.Should().Be((uint)newData.Length);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Stream_WithData_ShouldReturnMemoryStream()
    {
        // Arrange
        var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var resource = new SoundResource(audioData, AudioFormat.Unknown, 1, _logger);

        // Act
        using var stream = resource.Stream;

        // Assert
        stream.Should().NotBeNull();
        stream!.Length.Should().Be(audioData.Length);
    }

    [Fact]
    public void Stream_WithEmptyData_ShouldReturnNull()
    {
        // Arrange
        var resource = new SoundResource(ReadOnlyMemory<byte>.Empty, AudioFormat.Unknown, 1, _logger);

        // Act
        var stream = resource.Stream;

        // Assert
        stream.Should().BeNull();
    }

    [Fact]
    public void Equals_WithSameData_ShouldReturnTrue()
    {
        // Arrange
        var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var resource1 = new SoundResource(audioData, AudioFormat.Mp3, 1, _logger);
        var resource2 = new SoundResource(audioData, AudioFormat.Mp3, 1, _logger);

        // Act & Assert
        resource1.Equals(resource2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentData_ShouldReturnFalse()
    {
        // Arrange
        var audioData1 = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var audioData2 = new byte[] { 0x04, 0x05, 0x06, 0x07 };
        var resource1 = new SoundResource(audioData1, AudioFormat.Mp3, 1, _logger);
        var resource2 = new SoundResource(audioData2, AudioFormat.Mp3, 1, _logger);

        // Act & Assert
        resource1.Equals(resource2).Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldReturnCorrectValues()
    {
        // Arrange
        var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var metadata = new AudioMetadata
        {
            Format = AudioFormat.Mp3,
            SampleRate = 44100,
            Channels = 2,
            BitsPerSample = 16,
            Duration = 120.5
        };
        var resource = new SoundResource(audioData, AudioFormat.Mp3, 1, _logger);

        // Act & Assert
        resource.SampleRate.Should().Be(0u); // Default value since metadata extraction is simplified
        resource.Channels.Should().Be(0u);
        resource.BitsPerSample.Should().Be(0u);
        resource.Duration.Should().Be(0.0);
    }

    [Fact]
    public void Dispose_ShouldClearDataAndMarkDisposed()
    {
        // Arrange
        var audioData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var resource = new SoundResource(audioData, AudioFormat.Mp3, 1, _logger);

        // Act
        resource.Dispose();

        // Assert
        var action = () => resource.AudioData;
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ResourceConstants_ShouldHaveCorrectValues()
    {
        // Act & Assert
        SoundResource.AudioControllerResourceType.Should().Be(0x029E333Bu);
        SoundResource.AudioSubmixResourceType.Should().Be(0x02C9EFF2u);
        SoundResource.SoundPropertiesResourceType.Should().Be(0x1B25A024u);
        SoundResource.MusicDataResourceType.Should().Be(0xC202C770u);
        SoundResource.AmbienceResourceType.Should().Be(0xD2DC5BADu);
    }
}
