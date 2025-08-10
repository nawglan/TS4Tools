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

using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Audio;

/// <summary>
/// Defines the contract for a video resource in The Sims 4.
/// </summary>
public interface IVideoResource : IResource
{
    /// <summary>
    /// Gets the video metadata for this video resource.
    /// </summary>
    VideoMetadata Metadata { get; }

    /// <summary>
    /// Gets the video data as a read-only memory span.
    /// </summary>
    ReadOnlyMemory<byte> VideoData { get; }

    /// <summary>
    /// Gets the detected video format.
    /// </summary>
    VideoFormat Format { get; }

    /// <summary>
    /// Gets the video codec used.
    /// </summary>
    VideoCodec Codec { get; }

    /// <summary>
    /// Gets the video width in pixels.
    /// </summary>
    uint Width { get; }

    /// <summary>
    /// Gets the video height in pixels.
    /// </summary>
    uint Height { get; }

    /// <summary>
    /// Gets the frame rate in frames per second.
    /// </summary>
    double FrameRate { get; }

    /// <summary>
    /// Gets the duration in seconds.
    /// </summary>
    double Duration { get; }

    /// <summary>
    /// Gets whether the video has an audio track.
    /// </summary>
    bool HasAudio { get; }

    /// <summary>
    /// Analyzes the video data and extracts metadata asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous analysis operation</returns>
    Task<VideoMetadata> AnalyzeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the video data with new content.
    /// </summary>
    /// <param name="videoData">The new video data</param>
    /// <param name="format">The video format (optional, will be detected if not provided)</param>
    void UpdateVideoData(ReadOnlyMemory<byte> videoData, VideoFormat? format = null);

    /// <summary>
    /// Extracts a frame from the video at the specified time.
    /// </summary>
    /// <param name="timeSeconds">Time in seconds to extract the frame</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Frame data as raw image bytes, or null if extraction fails</returns>
    Task<ReadOnlyMemory<byte>?> ExtractFrameAsync(double timeSeconds, CancellationToken cancellationToken = default);
}
