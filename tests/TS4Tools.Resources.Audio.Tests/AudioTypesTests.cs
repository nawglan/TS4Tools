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
using TS4Tools.Resources.Audio;

namespace TS4Tools.Resources.Audio.Tests;

public sealed class AudioTypesTests
{
    [Fact]
    public void AudioFormat_Values_ShouldBeCorrect()
    {
        // Arrange & Assert
        AudioFormat.Unknown.Should().Be((AudioFormat)0);
        AudioFormat.Wav.Should().Be((AudioFormat)1);
        AudioFormat.Mp3.Should().Be((AudioFormat)2);
        AudioFormat.Ogg.Should().Be((AudioFormat)3);
        AudioFormat.Aac.Should().Be((AudioFormat)4);
        AudioFormat.Flac.Should().Be((AudioFormat)5);
        AudioFormat.SimsAudio.Should().Be((AudioFormat)100);
    }

    [Theory]
    [InlineData(AudioFormat.Mp3, true)]
    [InlineData(AudioFormat.Ogg, true)]
    [InlineData(AudioFormat.Aac, true)]
    [InlineData(AudioFormat.Wav, false)]
    [InlineData(AudioFormat.Flac, false)]
    [InlineData(AudioFormat.Unknown, false)]
    [InlineData(AudioFormat.SimsAudio, false)]
    public void AudioMetadata_IsCompressed_ShouldReturnExpectedValue(AudioFormat format, bool expectedCompressed)
    {
        // Arrange
        var metadata = new AudioMetadata { Format = format };

        // Act & Assert
        metadata.IsCompressed.Should().Be(expectedCompressed);
    }

    [Theory]
    [InlineData(AudioFormat.Wav, true)]
    [InlineData(AudioFormat.Flac, true)]
    [InlineData(AudioFormat.Mp3, false)]
    [InlineData(AudioFormat.Ogg, false)]
    [InlineData(AudioFormat.Aac, false)]
    [InlineData(AudioFormat.Unknown, false)]
    [InlineData(AudioFormat.SimsAudio, false)]
    public void AudioMetadata_IsLossless_ShouldReturnExpectedValue(AudioFormat format, bool expectedLossless)
    {
        // Arrange
        var metadata = new AudioMetadata { Format = format };

        // Act & Assert
        metadata.IsLossless.Should().Be(expectedLossless);
    }

    [Fact]
    public void AudioMetadata_DefaultValues_ShouldBeZero()
    {
        // Arrange & Act
        var metadata = new AudioMetadata();

        // Assert
        metadata.Format.Should().Be(AudioFormat.Unknown);
        metadata.SampleRate.Should().Be(0u);
        metadata.Channels.Should().Be(0u);
        metadata.BitsPerSample.Should().Be(0u);
        metadata.Duration.Should().Be(0.0);
        metadata.DataSize.Should().Be(0u);
    }

    [Fact]
    public void AudioMetadata_WithValues_ShouldRetainValues()
    {
        // Arrange
        var metadata = new AudioMetadata
        {
            Format = AudioFormat.Mp3,
            SampleRate = 44100,
            Channels = 2,
            BitsPerSample = 16,
            Duration = 120.5,
            DataSize = 1024
        };

        // Act & Assert
        metadata.Format.Should().Be(AudioFormat.Mp3);
        metadata.SampleRate.Should().Be(44100u);
        metadata.Channels.Should().Be(2u);
        metadata.BitsPerSample.Should().Be(16u);
        metadata.Duration.Should().Be(120.5);
        metadata.DataSize.Should().Be(1024u);
        metadata.IsCompressed.Should().BeTrue();
        metadata.IsLossless.Should().BeFalse();
    }

    [Fact]
    public void AudioMetadata_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var metadata1 = new AudioMetadata
        {
            Format = AudioFormat.Wav,
            SampleRate = 48000,
            Channels = 2,
            BitsPerSample = 24,
            Duration = 60.0,
            DataSize = 512
        };

        var metadata2 = new AudioMetadata
        {
            Format = AudioFormat.Wav,
            SampleRate = 48000,
            Channels = 2,
            BitsPerSample = 24,
            Duration = 60.0,
            DataSize = 512
        };

        var metadata3 = new AudioMetadata
        {
            Format = AudioFormat.Mp3,
            SampleRate = 48000,
            Channels = 2,
            BitsPerSample = 24,
            Duration = 60.0,
            DataSize = 512
        };

        // Act & Assert
        metadata1.Should().Be(metadata2);
        metadata1.Should().NotBe(metadata3);
    }
}
