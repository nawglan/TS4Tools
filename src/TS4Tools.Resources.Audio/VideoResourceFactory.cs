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

using Microsoft.Extensions.Logging;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Audio;

/// <summary>
/// Factory for creating VideoResource instances.
/// Supports various video formats including MP4, AVI, MOV, and custom Sims video formats.
/// </summary>
public sealed class VideoResourceFactory : ResourceFactoryBase<IVideoResource>
{
    private readonly ILogger<VideoResource> _videoResourceLogger;

    /// <summary>
    /// Initializes a new instance of the VideoResourceFactory class.
    /// </summary>
    /// <param name="logger">Logger for created VideoResource instances</param>
    public VideoResourceFactory(ILogger<VideoResource> logger) 
        : base(GetSupportedResourceTypes(), priority: 100)
    {
        _videoResourceLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    private static IEnumerable<string> GetSupportedResourceTypes()
    {
        return new[]
        {
            "0xDE6AD3CF", // Video_GlobalTuning
            "0xE55EEACB"  // Video_Playlist
        };
    }

    /// <inheritdoc />
    public override async Task<IVideoResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        if (stream != null)
        {
            var videoResource = new VideoResource(stream, apiVersion, _videoResourceLogger);
            return await Task.FromResult(videoResource);
        }

        // Create empty video resource
        var emptyResource = new VideoResource(ReadOnlyMemory<byte>.Empty, VideoFormat.Unknown, apiVersion, _videoResourceLogger);
        return await Task.FromResult(emptyResource);
    }

    /// <summary>
    /// Detects the video format from the provided data.
    /// </summary>
    /// <param name="data">The video data to analyze</param>
    /// <returns>The detected video format</returns>
    public static VideoFormat DetectVideoFormat(ReadOnlySpan<byte> data)
    {
        return VideoResource.DetectVideoFormat(data);
    }

    /// <summary>
    /// Gets information about supported video formats and their capabilities.
    /// </summary>
    /// <returns>A dictionary of format information</returns>
    public static Dictionary<VideoFormat, string> GetSupportedFormats()
    {
        return new Dictionary<VideoFormat, string>
        {
            { VideoFormat.Mp4, "MP4 - MPEG-4 Part 14 container format" },
            { VideoFormat.Avi, "AVI - Audio Video Interleave format" },
            { VideoFormat.Mov, "MOV - QuickTime Movie format" },
            { VideoFormat.Wmv, "WMV - Windows Media Video format" },
            { VideoFormat.Flv, "FLV - Flash Video format" },
            { VideoFormat.WebM, "WebM - Google's open container format" },
            { VideoFormat.SimsVideo, "Sims Video - EA's proprietary format" }
        };
    }

    /// <summary>
    /// Gets information about supported video codecs and their capabilities.
    /// </summary>
    /// <returns>A dictionary of codec information</returns>
    public static Dictionary<VideoCodec, string> GetSupportedCodecs()
    {
        return new Dictionary<VideoCodec, string>
        {
            { VideoCodec.H264, "H.264/AVC - Advanced Video Coding" },
            { VideoCodec.H265, "H.265/HEVC - High Efficiency Video Coding" },
            { VideoCodec.VP8, "VP8 - Google's video codec" },
            { VideoCodec.VP9, "VP9 - Google's improved video codec" },
            { VideoCodec.Mpeg4, "MPEG-4 Visual - ISO/IEC 14496-2" },
            { VideoCodec.SimsCodec, "Sims Codec - EA's proprietary codec" }
        };
    }
}
