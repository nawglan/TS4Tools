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
/// Represents various audio format types supported by The Sims 4.
/// </summary>
public enum AudioFormat
{
    /// <summary>
    /// Unknown or unsupported audio format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// WAV (Waveform Audio File Format) - uncompressed audio.
    /// </summary>
    Wav = 1,

    /// <summary>
    /// MP3 (MPEG-1 Audio Layer III) - compressed audio.
    /// </summary>
    Mp3 = 2,

    /// <summary>
    /// OGG Vorbis - open-source compressed audio.
    /// </summary>
    Ogg = 3,

    /// <summary>
    /// AAC (Advanced Audio Coding) - compressed audio.
    /// </summary>
    Aac = 4,

    /// <summary>
    /// FLAC (Free Lossless Audio Codec) - lossless compressed audio.
    /// </summary>
    Flac = 5,

    /// <summary>
    /// Custom Sims audio format - proprietary format used by EA.
    /// </summary>
    SimsAudio = 100
}

/// <summary>
/// Metadata information about an audio resource.
/// </summary>
public readonly record struct AudioMetadata
{
    /// <summary>
    /// The detected audio format.
    /// </summary>
    public AudioFormat Format { get; init; }

    /// <summary>
    /// Sample rate in Hz (e.g., 44100, 48000).
    /// </summary>
    public uint SampleRate { get; init; }

    /// <summary>
    /// Number of audio channels (1 = mono, 2 = stereo, etc.).
    /// </summary>
    public uint Channels { get; init; }

    /// <summary>
    /// Bits per sample (e.g., 16, 24, 32).
    /// </summary>
    public uint BitsPerSample { get; init; }

    /// <summary>
    /// Duration of the audio in seconds.
    /// </summary>
    public double Duration { get; init; }

    /// <summary>
    /// Size of the audio data in bytes.
    /// </summary>
    public uint DataSize { get; init; }

    /// <summary>
    /// Whether the audio has compression applied.
    /// </summary>
    public bool IsCompressed => Format is AudioFormat.Mp3 or AudioFormat.Ogg or AudioFormat.Aac;

    /// <summary>
    /// Whether the audio is lossless.
    /// </summary>
    public bool IsLossless => Format is AudioFormat.Wav or AudioFormat.Flac;
}
