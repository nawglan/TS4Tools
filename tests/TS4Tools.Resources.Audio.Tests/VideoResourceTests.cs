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

public sealed class VideoResourceTests
{
    private readonly ILogger<VideoResource> _logger;

    public VideoResourceTests()
    {
        _logger = Substitute.For<ILogger<VideoResource>>();
    }

    [Fact]
    public void Constructor_WithEmptyMemory_ShouldCreateEmptyResource()
    {
        // Arrange
        var videoData = ReadOnlyMemory<byte>.Empty;

        // Act
        var resource = new VideoResource(videoData, VideoFormat.Unknown, 1, _logger);

        // Assert
        resource.VideoData.IsEmpty.Should().BeTrue();
        resource.Format.Should().Be(VideoFormat.Unknown);
        resource.RequestedApiVersion.Should().Be(1);
        resource.ContentFields.Should().NotBeNull();
        resource.ContentFields.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithVideoData_ShouldInitializeCorrectly()
    {
        // Arrange
        var videoData = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D }; // MP4 ftyp box
        var expectedFormat = VideoFormat.Mp4;

        // Act
        var resource = new VideoResource(videoData, expectedFormat, 1, _logger);

        // Assert
        resource.VideoData.Length.Should().Be(videoData.Length);
        resource.Format.Should().Be(expectedFormat);
        resource.Metadata.Format.Should().Be(expectedFormat);
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithStream_ShouldParseDataCorrectly()
    {
        // Arrange
        var videoData = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D }; // MP4 ftyp box
        using var stream = new MemoryStream(videoData);

        // Act
        var resource = new VideoResource(stream, 1, _logger);

        // Assert
        resource.VideoData.Length.Should().Be(videoData.Length);
        resource.Format.Should().Be(VideoFormat.Mp4);
        resource.RequestedApiVersion.Should().Be(1);
    }

    [Fact]
    public void DetectVideoFormat_WithMp4Header_ShouldReturnMp4()
    {
        // Arrange
        var mp4Header = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D }; // ftyp box

        // Act
        var format = VideoResource.DetectVideoFormat(mp4Header);

        // Assert
        format.Should().Be(VideoFormat.Mp4);
    }

    [Fact]
    public void DetectVideoFormat_WithAviHeader_ShouldReturnAvi()
    {
        // Arrange
        var aviHeader = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x41, 0x56, 0x49, 0x20 }; // RIFF...AVI 

        // Act
        var format = VideoResource.DetectVideoFormat(aviHeader);

        // Assert
        format.Should().Be(VideoFormat.Avi);
    }

    [Fact]
    public void DetectVideoFormat_WithMovHeader_ShouldReturnMov()
    {
        // Arrange
        var movHeader = new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20 }; // ftyp with "qt  " brand

        // Act
        var format = VideoResource.DetectVideoFormat(movHeader);

        // Assert
        format.Should().Be(VideoFormat.Mov);
    }

    [Fact]
    public void DetectVideoFormat_WithFlvHeader_ShouldReturnFlv()
    {
        // Arrange
        var flvHeader = new byte[] { 0x46, 0x4C, 0x56, 0x01 }; // "FLV" + version

        // Act
        var format = VideoResource.DetectVideoFormat(flvHeader);

        // Assert
        format.Should().Be(VideoFormat.Flv);
    }

    [Fact]
    public void DetectVideoFormat_WithWebMHeader_ShouldReturnWebM()
    {
        // Arrange
        var webmHeader = new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x00, 0x00, 0x00 }; // EBML header

        // Act
        var format = VideoResource.DetectVideoFormat(webmHeader);

        // Assert
        format.Should().Be(VideoFormat.WebM);
    }

    [Fact]
    public void DetectVideoFormat_WithUnknownData_ShouldReturnSimsVideo()
    {
        // Arrange
        var unknownData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

        // Act
        var format = VideoResource.DetectVideoFormat(unknownData);

        // Assert
        format.Should().Be(VideoFormat.SimsVideo);
    }

    [Fact]
    public void DetectVideoFormat_WithInsufficientData_ShouldReturnUnknown()
    {
        // Arrange
        var smallData = new byte[] { 0x00, 0x01 };

        // Act
        var format = VideoResource.DetectVideoFormat(smallData);

        // Assert
        format.Should().Be(VideoFormat.Unknown);
    }

    [Theory]
    [InlineData("avc1", VideoCodec.H264)]
    [InlineData("h264", VideoCodec.H264)]
    [InlineData("hvc1", VideoCodec.H265)]
    [InlineData("h265", VideoCodec.H265)]
    [InlineData("vp08", VideoCodec.VP8)]
    [InlineData("vp09", VideoCodec.VP9)]
    [InlineData("mp4v", VideoCodec.Mpeg4)]
    [InlineData("unknown", VideoCodec.Unknown)]
    public void DetectMp4Codec_WithVariousCodecs_ShouldReturnCorrectCodec(string codecString, VideoCodec expectedCodec)
    {
        // Arrange
        var data = System.Text.Encoding.ASCII.GetBytes(codecString + " some other data");

        // Act
        var codec = VideoResource.DetectMp4Codec(data);

        // Assert
        codec.Should().Be(expectedCodec);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnMetadata()
    {
        // Arrange
        var videoData = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D };
        var resource = new VideoResource(videoData, VideoFormat.Mp4, 1, _logger);

        // Act
        var metadata = await resource.AnalyzeAsync();

        // Assert
        metadata.Format.Should().Be(VideoFormat.Mp4);
        metadata.DataSize.Should().Be((uint)videoData.Length);
    }

    [Fact]
    public void UpdateVideoData_ShouldUpdateDataAndMetadata()
    {
        // Arrange
        var originalData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var newData = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D };
        var resource = new VideoResource(originalData, VideoFormat.Unknown, 1, _logger);

        var eventRaised = false;
        resource.ResourceChanged += (_, _) => eventRaised = true;

        // Act
        resource.UpdateVideoData(newData);

        // Assert
        resource.VideoData.Length.Should().Be(newData.Length);
        resource.Format.Should().Be(VideoFormat.Mp4);
        resource.Metadata.DataSize.Should().Be((uint)newData.Length);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractFrameAsync_ShouldReturnNull()
    {
        // Arrange
        var videoData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var resource = new VideoResource(videoData, VideoFormat.Mp4, 1, _logger);

        // Act
        var frame = await resource.ExtractFrameAsync(1.5);

        // Assert
        frame.Should().BeNull();
    }

    [Fact]
    public void Stream_WithData_ShouldReturnMemoryStream()
    {
        // Arrange
        var videoData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var resource = new VideoResource(videoData, VideoFormat.Unknown, 1, _logger);

        // Act
        using var stream = resource.Stream;

        // Assert
        stream.Should().NotBeNull();
        stream!.Length.Should().Be(videoData.Length);
    }

    [Fact]
    public void Stream_WithEmptyData_ShouldReturnNull()
    {
        // Arrange
        var resource = new VideoResource(ReadOnlyMemory<byte>.Empty, VideoFormat.Unknown, 1, _logger);

        // Act
        var stream = resource.Stream;

        // Assert
        stream.Should().BeNull();
    }

    [Fact]
    public void Equals_WithSameData_ShouldReturnTrue()
    {
        // Arrange
        var videoData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var resource1 = new VideoResource(videoData, VideoFormat.Mp4, 1, _logger);
        var resource2 = new VideoResource(videoData, VideoFormat.Mp4, 1, _logger);

        // Act & Assert
        resource1.Equals(resource2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentData_ShouldReturnFalse()
    {
        // Arrange
        var videoData1 = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var videoData2 = new byte[] { 0x04, 0x05, 0x06, 0x07 };
        var resource1 = new VideoResource(videoData1, VideoFormat.Mp4, 1, _logger);
        var resource2 = new VideoResource(videoData2, VideoFormat.Mp4, 1, _logger);

        // Act & Assert
        resource1.Equals(resource2).Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldReturnCorrectDefaultValues()
    {
        // Arrange
        var videoData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var resource = new VideoResource(videoData, VideoFormat.Mp4, 1, _logger);

        // Act & Assert
        resource.Width.Should().Be(0u); // Default value since metadata extraction is simplified
        resource.Height.Should().Be(0u);
        resource.FrameRate.Should().Be(0.0);
        resource.Duration.Should().Be(0.0);
        resource.HasAudio.Should().BeFalse();
        resource.Codec.Should().Be(VideoCodec.Unknown);
    }

    [Fact]
    public void Dispose_ShouldClearDataAndMarkDisposed()
    {
        // Arrange
        var videoData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var resource = new VideoResource(videoData, VideoFormat.Mp4, 1, _logger);

        // Act
        resource.Dispose();

        // Assert
        var action = () => resource.VideoData;
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ResourceConstants_ShouldHaveCorrectValues()
    {
        // Act & Assert
        VideoResource.VideoGlobalTuningResourceType.Should().Be(0xDE6AD3CFu);
        VideoResource.VideoPlaylistResourceType.Should().Be(0xE55EEACBu);
    }
}
