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

namespace TS4Tools.Resources.Audio;

/// <summary>
/// Represents various video format types supported by The Sims 4.
/// </summary>
public enum VideoFormat
{
    /// <summary>
    /// Unknown or unsupported video format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// MP4 (MPEG-4 Part 14) - common container format.
    /// </summary>
    Mp4 = 1,

    /// <summary>
    /// AVI (Audio Video Interleave) - Microsoft container format.
    /// </summary>
    Avi = 2,

    /// <summary>
    /// MOV (QuickTime Movie) - Apple container format.
    /// </summary>
    Mov = 3,

    /// <summary>
    /// WMV (Windows Media Video) - Microsoft video format.
    /// </summary>
    Wmv = 4,

    /// <summary>
    /// FLV (Flash Video) - Adobe Flash video format.
    /// </summary>
    Flv = 5,

    /// <summary>
    /// WebM - Google's open container format.
    /// </summary>
    WebM = 6,

    /// <summary>
    /// Custom Sims video format - proprietary format used by EA.
    /// </summary>
    SimsVideo = 100
}

/// <summary>
/// Video codec types used in The Sims 4 video resources.
/// </summary>
public enum VideoCodec
{
    /// <summary>
    /// Unknown or unsupported codec.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// H.264/AVC - Advanced Video Coding.
    /// </summary>
    H264 = 1,

    /// <summary>
    /// H.265/HEVC - High Efficiency Video Coding.
    /// </summary>
    H265 = 2,

    /// <summary>
    /// VP8 - Google's video codec.
    /// </summary>
    VP8 = 3,

    /// <summary>
    /// VP9 - Google's improved video codec.
    /// </summary>
    VP9 = 4,

    /// <summary>
    /// MPEG-4 Visual - ISO/IEC 14496-2.
    /// </summary>
    Mpeg4 = 5,

    /// <summary>
    /// Custom Sims codec - EA's proprietary codec.
    /// </summary>
    SimsCodec = 100
}

/// <summary>
/// Metadata information about a video resource.
/// </summary>
public readonly record struct VideoMetadata
{
    /// <summary>
    /// The detected video format.
    /// </summary>
    public VideoFormat Format { get; init; }

    /// <summary>
    /// The video codec used.
    /// </summary>
    public VideoCodec Codec { get; init; }

    /// <summary>
    /// Video width in pixels.
    /// </summary>
    public uint Width { get; init; }

    /// <summary>
    /// Video height in pixels.
    /// </summary>
    public uint Height { get; init; }

    /// <summary>
    /// Frame rate in frames per second.
    /// </summary>
    public double FrameRate { get; init; }

    /// <summary>
    /// Duration of the video in seconds.
    /// </summary>
    public double Duration { get; init; }

    /// <summary>
    /// Bitrate in bits per second.
    /// </summary>
    public uint Bitrate { get; init; }

    /// <summary>
    /// Size of the video data in bytes.
    /// </summary>
    public uint DataSize { get; init; }

    /// <summary>
    /// Whether the video has an audio track.
    /// </summary>
    public bool HasAudio { get; init; }

    /// <summary>
    /// Whether the video uses compression.
    /// </summary>
    public bool IsCompressed => Format is not VideoFormat.Unknown;

    /// <summary>
    /// Aspect ratio of the video.
    /// </summary>
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;
}
