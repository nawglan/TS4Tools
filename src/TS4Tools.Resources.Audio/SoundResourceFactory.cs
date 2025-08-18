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
/// Factory for creating sound resources in The Sims 4 packages
/// </summary>
public sealed class SoundResourceFactory : ResourceFactoryBase<ISoundResource>
{
    private readonly ILogger<SoundResource> _logger;

    /// <summary>
    /// Initializes a new instance of the SoundResourceFactory class
    /// </summary>
    /// <param name="logger">Logger for created sound resources</param>
    public SoundResourceFactory(ILogger<SoundResource> logger)
        : base(GetSupportedResourceTypes(), priority: 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static IEnumerable<string> GetSupportedResourceTypes()
    {
        return new[]
        {
            "0x029E333B", // Audio Controller Resource Type
            "0x02C9EFF2", // Audio Tuning Resource Type  
            "0x1B25A024", // Sound properties
            "0xC202C770", // Music data file
            "0xD2DC5BAD"  // Ambience
        };
    }

    /// <inheritdoc />
    public override async Task<ISoundResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        ValidateApiVersion(apiVersion);

        if (stream == null)
        {
            return new SoundResource(ReadOnlyMemory<byte>.Empty, AudioFormat.Unknown, apiVersion, _logger);
        }

        try
        {
            return await Task.FromResult(new SoundResource(stream, apiVersion, _logger));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ArgumentException($"Failed to create sound resource: {ex.Message}", nameof(stream), ex);
        }
    }

    /// <inheritdoc />
    public override ISoundResource CreateResource(Stream stream, uint resourceType)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!CanCreateResource(resourceType))
        {
            throw new ArgumentException($"Unsupported resource type: 0x{resourceType:X8}", nameof(resourceType));
        }

        // Use async method synchronously for compatibility - deadlock-safe pattern
        return Task.Run(async () => await CreateResourceAsync(1, stream).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Detects the audio format from raw byte data
    /// </summary>
    /// <param name="data">Audio data bytes</param>
    /// <returns>Detected audio format</returns>
    public static AudioFormat DetectAudioFormat(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 4)
            return AudioFormat.Unknown;

        // WAV format detection
        if (data.Length >= 12 &&
            data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 && // "RIFF"
            data[8] == 0x57 && data[9] == 0x41 && data[10] == 0x56 && data[11] == 0x45) // "WAVE"
        {
            return AudioFormat.Wav;
        }

        // MP3 format detection
        if ((data[0] == 0xFF && (data[1] & 0xFE) == 0xFA) || // MPEG sync
            (data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)) // ID3 tag
        {
            return AudioFormat.Mp3;
        }

        // OGG format detection
        if (data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67 && data[3] == 0x53) // "OggS"
        {
            return AudioFormat.Ogg;
        }

        // AAC format detection (simplified)
        if (data.Length >= 7 &&
            data[0] == 0xFF && (data[1] & 0xF0) == 0xF0) // ADTS sync word
        {
            return AudioFormat.Aac;
        }

        // FLAC format detection
        if (data[0] == 0x66 && data[1] == 0x4C && data[2] == 0x61 && data[3] == 0x43) // "fLaC"
        {
            return AudioFormat.Flac;
        }

        // Default to SimsAudio for unrecognized formats
        return AudioFormat.SimsAudio;
    }

    /// <summary>
    /// Gets supported audio formats with their descriptions
    /// </summary>
    /// <returns>Dictionary of audio formats and descriptions</returns>
    public static Dictionary<AudioFormat, string> GetSupportedFormats()
    {
        return new Dictionary<AudioFormat, string>
        {
            { AudioFormat.Wav, "WAV - Waveform Audio File Format" },
            { AudioFormat.Mp3, "MP3 - MPEG Audio Layer III" },
            { AudioFormat.Ogg, "OGG - Ogg Vorbis" },
            { AudioFormat.Aac, "AAC - Advanced Audio Coding" },
            { AudioFormat.Flac, "FLAC - Free Lossless Audio Codec" },
            { AudioFormat.SimsAudio, "SIMS - The Sims 4 Audio Format" }
        };
    }
}
