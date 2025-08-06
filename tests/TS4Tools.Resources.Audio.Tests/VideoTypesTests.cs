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

public sealed class VideoTypesTests
{
    [Fact]
    public void VideoFormat_Values_ShouldBeCorrect()
    {
        // Arrange & Assert
        VideoFormat.Unknown.Should().Be((VideoFormat)0);
        VideoFormat.Mp4.Should().Be((VideoFormat)1);
        VideoFormat.Avi.Should().Be((VideoFormat)2);
        VideoFormat.Mov.Should().Be((VideoFormat)3);
        VideoFormat.Wmv.Should().Be((VideoFormat)4);
        VideoFormat.Flv.Should().Be((VideoFormat)5);
        VideoFormat.WebM.Should().Be((VideoFormat)6);
        VideoFormat.SimsVideo.Should().Be((VideoFormat)100);
    }

    [Fact]
    public void VideoCodec_Values_ShouldBeCorrect()
    {
        // Arrange & Assert
        VideoCodec.Unknown.Should().Be((VideoCodec)0);
        VideoCodec.H264.Should().Be((VideoCodec)1);
        VideoCodec.H265.Should().Be((VideoCodec)2);
        VideoCodec.VP8.Should().Be((VideoCodec)3);
        VideoCodec.VP9.Should().Be((VideoCodec)4);
        VideoCodec.Mpeg4.Should().Be((VideoCodec)5);
        VideoCodec.SimsCodec.Should().Be((VideoCodec)100);
    }

    [Theory]
    [InlineData(VideoFormat.Mp4, true)]
    [InlineData(VideoFormat.Avi, true)]
    [InlineData(VideoFormat.Mov, true)]
    [InlineData(VideoFormat.Wmv, true)]
    [InlineData(VideoFormat.Flv, true)]
    [InlineData(VideoFormat.WebM, true)]
    [InlineData(VideoFormat.SimsVideo, true)]
    [InlineData(VideoFormat.Unknown, false)]
    public void VideoMetadata_IsCompressed_ShouldReturnExpectedValue(VideoFormat format, bool expectedCompressed)
    {
        // Arrange
        var metadata = new VideoMetadata { Format = format };

        // Act & Assert
        metadata.IsCompressed.Should().Be(expectedCompressed);
    }

    [Theory]
    [InlineData(1920u, 1080u, 1.7777777777777777)]
    [InlineData(1280u, 720u, 1.7777777777777777)]
    [InlineData(640u, 480u, 1.3333333333333333)]
    [InlineData(1920u, 0u, 0.0)]
    [InlineData(0u, 1080u, 0.0)]
    public void VideoMetadata_AspectRatio_ShouldCalculateCorrectly(uint width, uint height, double expectedRatio)
    {
        // Arrange
        var metadata = new VideoMetadata { Width = width, Height = height };

        // Act & Assert
        metadata.AspectRatio.Should().BeApproximately(expectedRatio, 0.0001);
    }

    [Fact]
    public void VideoMetadata_DefaultValues_ShouldBeZero()
    {
        // Arrange & Act
        var metadata = new VideoMetadata();

        // Assert
        metadata.Format.Should().Be(VideoFormat.Unknown);
        metadata.Codec.Should().Be(VideoCodec.Unknown);
        metadata.Width.Should().Be(0u);
        metadata.Height.Should().Be(0u);
        metadata.FrameRate.Should().Be(0.0);
        metadata.Duration.Should().Be(0.0);
        metadata.Bitrate.Should().Be(0u);
        metadata.DataSize.Should().Be(0u);
        metadata.HasAudio.Should().BeFalse();
    }

    [Fact]
    public void VideoMetadata_WithValues_ShouldRetainValues()
    {
        // Arrange
        var metadata = new VideoMetadata
        {
            Format = VideoFormat.Mp4,
            Codec = VideoCodec.H264,
            Width = 1920,
            Height = 1080,
            FrameRate = 29.97,
            Duration = 120.5,
            Bitrate = 5000000,
            DataSize = 1024,
            HasAudio = true
        };

        // Act & Assert
        metadata.Format.Should().Be(VideoFormat.Mp4);
        metadata.Codec.Should().Be(VideoCodec.H264);
        metadata.Width.Should().Be(1920u);
        metadata.Height.Should().Be(1080u);
        metadata.FrameRate.Should().Be(29.97);
        metadata.Duration.Should().Be(120.5);
        metadata.Bitrate.Should().Be(5000000u);
        metadata.DataSize.Should().Be(1024u);
        metadata.HasAudio.Should().BeTrue();
        metadata.IsCompressed.Should().BeTrue();
        metadata.AspectRatio.Should().BeApproximately(1.7777777777777777, 0.0001);
    }

    [Fact]
    public void VideoMetadata_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var metadata1 = new VideoMetadata
        {
            Format = VideoFormat.Mp4,
            Codec = VideoCodec.H264,
            Width = 1920,
            Height = 1080,
            FrameRate = 30.0,
            Duration = 60.0,
            Bitrate = 2000000,
            DataSize = 512,
            HasAudio = true
        };

        var metadata2 = new VideoMetadata
        {
            Format = VideoFormat.Mp4,
            Codec = VideoCodec.H264,
            Width = 1920,
            Height = 1080,
            FrameRate = 30.0,
            Duration = 60.0,
            Bitrate = 2000000,
            DataSize = 512,
            HasAudio = true
        };

        var metadata3 = new VideoMetadata
        {
            Format = VideoFormat.Avi,
            Codec = VideoCodec.H264,
            Width = 1920,
            Height = 1080,
            FrameRate = 30.0,
            Duration = 60.0,
            Bitrate = 2000000,
            DataSize = 512,
            HasAudio = true
        };

        // Act & Assert
        metadata1.Should().Be(metadata2);
        metadata1.Should().NotBe(metadata3);
    }

    [Theory]
    [InlineData(0u, 0u, 0.0)]
    [InlineData(1920u, 1080u, 1.7777777777777777)]
    [InlineData(4096u, 2160u, 1.8962962962962962)]
    [InlineData(720u, 480u, 1.5)]
    public void VideoMetadata_AspectRatio_VariousResolutions_ShouldCalculateCorrectly(uint width, uint height, double expectedRatio)
    {
        // Arrange
        var metadata = new VideoMetadata { Width = width, Height = height };

        // Act
        var aspectRatio = metadata.AspectRatio;

        // Assert
        aspectRatio.Should().BeApproximately(expectedRatio, 0.0001);
    }
}
