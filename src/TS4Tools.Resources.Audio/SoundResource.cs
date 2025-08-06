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
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common.Collections;

namespace TS4Tools.Resources.Audio;

/// <summary>
/// A modern implementation of sound resource handling for The Sims 4.
/// Supports various audio formats including WAV, MP3, OGG, and custom Sims audio formats
/// with metadata extraction and format detection capabilities.
/// </summary>
/// <remarks>
/// This resource wrapper handles various audio formats used in The Sims 4:
/// - WAV (Waveform Audio File Format) for uncompressed audio
/// - MP3 (MPEG-1 Audio Layer III) for compressed audio
/// - OGG Vorbis for open-source compressed audio
/// - Custom Sims audio formats used by EA
/// - Automatic format detection and metadata extraction
/// - Performance-optimized with Span&lt;T&gt; and async patterns
/// </remarks>
public sealed class SoundResource : ISoundResource, IDisposable
{
    /// <summary>
    /// Common resource type for audio controllers.
    /// </summary>
    public const uint AudioControllerResourceType = 0x029E333B;
    
    /// <summary>
    /// Common resource type for audio submix.
    /// </summary>
    public const uint AudioSubmixResourceType = 0x02C9EFF2;
    
    /// <summary>
    /// Common resource type for sound properties.
    /// </summary>
    public const uint SoundPropertiesResourceType = 0x1B25A024;
    
    /// <summary>
    /// Common resource type for music data.
    /// </summary>
    public const uint MusicDataResourceType = 0xC202C770;
    
    /// <summary>
    /// Common resource type for ambience audio.
    /// </summary>
    public const uint AmbienceResourceType = 0xD2DC5BAD;

    private readonly int _requestedApiVersion;
    private readonly ILogger<SoundResource>? _logger;
    private readonly ObservableList<TypedValue> _contentFields;
    
    private ReadOnlyMemory<byte> _audioData;
    private AudioMetadata _metadata;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the SoundResource class from a stream.
    /// </summary>
    /// <param name="stream">Stream containing sound data</param>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public SoundResource(Stream stream, int requestedApiVersion = 1, ILogger<SoundResource>? logger = null)
    {
        _requestedApiVersion = requestedApiVersion;
        _logger = logger;
        _contentFields = new ObservableList<TypedValue>();
        
        if (stream != null && stream.Length > 0)
        {
            ParseAudioData(stream);
        }
        
        _logger?.LogDebug("SoundResource initialized with API version {ApiVersion}", requestedApiVersion);
    }

    /// <summary>
    /// Initializes a new instance of the SoundResource class with audio data.
    /// </summary>
    /// <param name="audioData">The audio data</param>
    /// <param name="format">The audio format (optional, will be detected if not provided)</param>
    /// <param name="requestedApiVersion">The API version requested for this resource</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public SoundResource(ReadOnlyMemory<byte> audioData, AudioFormat? format = null, int requestedApiVersion = 1, ILogger<SoundResource>? logger = null)
    {
        _requestedApiVersion = requestedApiVersion;
        _logger = logger;
        _contentFields = new ObservableList<TypedValue>();
        _audioData = audioData;
        
        // Detect format if not provided
        var detectedFormat = format ?? DetectAudioFormat(_audioData.Span);
        _metadata = new AudioMetadata { Format = detectedFormat };
        
        _logger?.LogDebug("SoundResource initialized with {Size} bytes of {Format} audio data", 
                         audioData.Length, detectedFormat);
    }

    #region IResource Implementation

    /// <inheritdoc />
    public int RequestedApiVersion => _requestedApiVersion;

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    /// <inheritdoc />
    public byte[] AsBytes => _audioData.ToArray();

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => Array.Empty<string>().AsReadOnly();

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get => index >= 0 && index < _contentFields.Count ? _contentFields[index] : throw new ArgumentOutOfRangeException(nameof(index));
        set
        {
            if (index >= 0 && index < _contentFields.Count)
            {
                _contentFields[index] = value;
                OnResourceChanged();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get 
        {
            // For now, we don't have named fields, so throw KeyNotFoundException
            throw new KeyNotFoundException($"Content field '{name}' not found");
        }
        set
        {
            // For now, we don't have named fields, so throw KeyNotFoundException
            throw new KeyNotFoundException($"Content field '{name}' not found");
        }
    }

    /// <inheritdoc />
    public Stream Stream => _audioData.IsEmpty ? null! : new MemoryStream(_audioData.ToArray());

    /// <inheritdoc />
    public bool Equals(IResource? other)
    {
        return other is SoundResource sound &&
               sound._audioData.Span.SequenceEqual(_audioData.Span) &&
               sound._metadata.Equals(_metadata);
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    #endregion

    #region ISoundResource Implementation

    /// <inheritdoc />
    public AudioMetadata Metadata => _metadata;

    /// <inheritdoc />
    public ReadOnlyMemory<byte> AudioData
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _audioData;
        }
    }

    /// <inheritdoc />
    public AudioFormat Format => _metadata.Format;

    /// <inheritdoc />
    public uint SampleRate => _metadata.SampleRate;

    /// <inheritdoc />
    public uint Channels => _metadata.Channels;

    /// <inheritdoc />
    public uint BitsPerSample => _metadata.BitsPerSample;

    /// <inheritdoc />
    public double Duration => _metadata.Duration;

    /// <inheritdoc />
    public async Task<AudioMetadata> AnalyzeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        _logger?.LogDebug("Analyzing audio data for metadata extraction");
        
        // Calculate data size and return metadata with current values
        var metadata = new AudioMetadata
        {
            Format = _metadata.Format,
            Duration = _metadata.Duration,
            SampleRate = _metadata.SampleRate,
            Channels = _metadata.Channels,
            BitsPerSample = _metadata.BitsPerSample,
            DataSize = (uint)_audioData.Length // Calculate from actual data
        };
        
        return await Task.FromResult(metadata);
    }

    /// <inheritdoc />
    public void UpdateAudioData(ReadOnlyMemory<byte> audioData, AudioFormat? format = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        _audioData = audioData;
        var detectedFormat = format ?? DetectAudioFormat(audioData.Span);
        
        // Update metadata with new format
        _metadata = _metadata with 
        { 
            Format = detectedFormat,
            DataSize = (uint)audioData.Length
        };
        
        ResourceChanged?.Invoke(this, EventArgs.Empty);
        
        _logger?.LogDebug("Audio data updated: {Size} bytes, format: {Format}", 
                         audioData.Length, detectedFormat);
    }

    #endregion

    #region Audio Format Detection

    /// <summary>
    /// Detects the audio format from the provided data.
    /// </summary>
    /// <param name="data">The audio data to analyze</param>
    /// <returns>The detected audio format</returns>
    public static AudioFormat DetectAudioFormat(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return AudioFormat.Unknown;

        // Check for WAV format (RIFF header)
        if (data.Length >= 12 &&
            data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' &&
            data[8] == 'W' && data[9] == 'A' && data[10] == 'V' && data[11] == 'E')
        {
            return AudioFormat.Wav;
        }

        // Check for MP3 format (ID3 header or frame sync)
        if (data.Length >= 3 &&
            ((data[0] == 'I' && data[1] == 'D' && data[2] == '3') ||
             (data[0] == 0xFF && (data[1] & 0xE0) == 0xE0)))
        {
            return AudioFormat.Mp3;
        }

        // Check for OGG format
        if (data.Length >= 4 &&
            data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S')
        {
            return AudioFormat.Ogg;
        }

        // Check for AAC format (ADTS header)
        if (data.Length >= 2 &&
            data[0] == 0xFF && (data[1] & 0xF0) == 0xF0)
        {
            return AudioFormat.Aac;
        }

        // Check for FLAC format
        if (data.Length >= 4 &&
            data[0] == 'f' && data[1] == 'L' && data[2] == 'a' && data[3] == 'C')
        {
            return AudioFormat.Flac;
        }

        // Default to SimsAudio for unrecognized formats (EA's custom format)
        return AudioFormat.SimsAudio;
    }

    #endregion

    #region Data Parsing

    private void ParseAudioData(Stream stream)
    {
        if (stream.Length == 0)
        {
            _audioData = ReadOnlyMemory<byte>.Empty;
            _metadata = new AudioMetadata { Format = AudioFormat.Unknown };
            return;
        }

        // Read all audio data
        var data = new byte[stream.Length];
        stream.Position = 0;
        var bytesRead = stream.Read(data, 0, data.Length);
        
        if (bytesRead != stream.Length)
        {
            _logger?.LogWarning("Expected to read {Expected} bytes but read {Actual} bytes", 
                              stream.Length, bytesRead);
        }

        _audioData = data;
        
        // Detect format and extract basic metadata
        var format = DetectAudioFormat(data.AsSpan());
        _metadata = ExtractMetadata(data.AsSpan(), format);
        
        _logger?.LogDebug("Parsed audio data: {Size} bytes, format: {Format}", 
                         data.Length, format);
    }

    private static AudioMetadata ExtractMetadata(ReadOnlySpan<byte> data, AudioFormat format)
    {
        var metadata = new AudioMetadata
        {
            Format = format,
            DataSize = (uint)data.Length
        };

        // Extract format-specific metadata
        switch (format)
        {
            case AudioFormat.Wav:
                return ExtractWavMetadata(data) ?? metadata;
            
            case AudioFormat.Mp3:
                return ExtractMp3Metadata(data) ?? metadata;
            
            case AudioFormat.Ogg:
                return ExtractOggMetadata(data) ?? metadata;
            
            default:
                return metadata;
        }
    }

    private static AudioMetadata? ExtractWavMetadata(ReadOnlySpan<byte> data)
    {
        if (data.Length < 44) // Minimum WAV header size
            return null;

        try
        {
            // Skip RIFF header (12 bytes) and find fmt chunk
            var position = 12;
            while (position < data.Length - 8)
            {
                var chunkId = data.Slice(position, 4);
                var chunkSize = BitConverter.ToUInt32(data.Slice(position + 4, 4));
                
                if (chunkId[0] == 'f' && chunkId[1] == 'm' && chunkId[2] == 't' && chunkId[3] == ' ')
                {
                    // Found fmt chunk
                    var channels = BitConverter.ToUInt16(data.Slice(position + 10, 2));
                    var sampleRate = BitConverter.ToUInt32(data.Slice(position + 12, 4));
                    var bitsPerSample = BitConverter.ToUInt16(data.Slice(position + 22, 2));
                    
                    // Calculate duration (rough estimate)
                    var dataSize = (uint)data.Length - 44; // Approximate data size
                    var bytesPerSample = (bitsPerSample / 8) * channels;
                    var duration = bytesPerSample > 0 ? dataSize / (double)(sampleRate * bytesPerSample) : 0;
                    
                    return new AudioMetadata
                    {
                        Format = AudioFormat.Wav,
                        SampleRate = sampleRate,
                        Channels = channels,
                        BitsPerSample = bitsPerSample,
                        Duration = duration,
                        DataSize = (uint)data.Length
                    };
                }
                
                position += 8 + (int)chunkSize;
            }
        }
        catch
        {
            // If parsing fails, return null to use default metadata
        }

        return null;
    }

    private static AudioMetadata? ExtractMp3Metadata(ReadOnlySpan<byte> data)
    {
        // MP3 metadata extraction is complex and would require a full MP3 parser
        // For now, return basic metadata
        return new AudioMetadata
        {
            Format = AudioFormat.Mp3,
            DataSize = (uint)data.Length,
            // Default values for MP3
            SampleRate = 44100,
            Channels = 2,
            BitsPerSample = 16
        };
    }

    private static AudioMetadata? ExtractOggMetadata(ReadOnlySpan<byte> data)
    {
        // OGG metadata extraction is complex and would require a full OGG parser
        // For now, return basic metadata
        return new AudioMetadata
        {
            Format = AudioFormat.Ogg,
            DataSize = (uint)data.Length,
            // Default values for OGG
            SampleRate = 44100,
            Channels = 2,
            BitsPerSample = 16
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Raises the ResourceChanged event
    /// </summary>
    private void OnResourceChanged()
    {
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        // Clear audio data
        _audioData = ReadOnlyMemory<byte>.Empty;
        _metadata = default;
        
        _disposed = true;
        _logger?.LogDebug("SoundResource disposed");
    }

    #endregion
}
