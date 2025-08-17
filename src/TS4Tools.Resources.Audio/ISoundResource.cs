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
/// Defines the contract for a sound resource in The Sims 4.
/// </summary>
public interface ISoundResource : IResource
{
    /// <summary>
    /// Gets the audio metadata for this sound resource.
    /// </summary>
    AudioMetadata Metadata { get; }

    /// <summary>
    /// Gets the audio data as a read-only memory span.
    /// </summary>
    ReadOnlyMemory<byte> AudioData { get; }

    /// <summary>
    /// Gets the detected audio format.
    /// </summary>
    AudioFormat Format { get; }

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    uint SampleRate { get; }

    /// <summary>
    /// Gets the number of audio channels.
    /// </summary>
    uint Channels { get; }

    /// <summary>
    /// Gets the bits per sample.
    /// </summary>
    uint BitsPerSample { get; }

    /// <summary>
    /// Gets the duration in seconds.
    /// </summary>
    double Duration { get; }

    /// <summary>
    /// Analyzes the audio data and extracts metadata asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous analysis operation</returns>
    Task<AudioMetadata> AnalyzeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the audio data with new content.
    /// </summary>
    /// <param name="audioData">The new audio data</param>
    /// <param name="format">The audio format (optional, will be detected if not provided)</param>
    void UpdateAudioData(ReadOnlyMemory<byte> audioData, AudioFormat? format = null);
}
