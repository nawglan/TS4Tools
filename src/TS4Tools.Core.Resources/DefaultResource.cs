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

using System.Collections;
using System.Diagnostics;
using System.Text;

namespace TS4Tools.Core.Resources;

/// <summary>
/// Enhanced default resource implementation that can handle any resource type.
/// Provides basic stream access with enhanced metadata, diagnostics, and performance optimization.
/// This is the fallback resource wrapper for unknown or unsupported resource types.
/// </summary>
/// <remarks>
/// Phase 4.1.2 Enhancements:
/// - Additional metadata detection and extraction
/// - Improved error handling and diagnostic information
/// - Resource type hint detection for better identification
/// - Performance optimizations for large file handling
/// - Enhanced validation and monitoring capabilities
/// </remarks>
internal sealed class DefaultResource : IResource, IDisposable
{
    private readonly MemoryStream _stream;
    private readonly int _apiVersion;
    private readonly DateTime _createdAt;
    private readonly long _originalSize;
    private readonly string? _detectedResourceTypeHint;
    private readonly Dictionary<string, object> _metadata;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResource"/> class.
    /// </summary>
    /// <param name="apiVersion">API version</param>
    /// <param name="stream">Resource data stream</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when API version is invalid</exception>
    /// <exception cref="InvalidDataException">Thrown when stream data is corrupted</exception>
    public DefaultResource(int apiVersion, Stream? stream = null)
    {
        if (apiVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(apiVersion), apiVersion, "API version must be 1 or greater");
        }

        _apiVersion = apiVersion;
        _createdAt = DateTime.UtcNow;
        _metadata = new Dictionary<string, object>();

        if (stream != null)
        {
            try
            {
                _originalSize = stream.Length;
                _stream = new MemoryStream();

                // Performance optimization: Use efficient copying for large streams
                if (stream.Length > 1024 * 1024) // > 1MB
                {
                    stream.CopyToAsync(_stream).Wait();
                }
                else
                {
                    stream.CopyTo(_stream);
                }

                _stream.Position = 0;

                // Enhanced: Detect resource type hints
                _detectedResourceTypeHint = DetectResourceTypeHint(_stream);

                // Enhanced: Extract additional metadata
                ExtractMetadata(_stream);
            }
            catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
            {
                _stream?.Dispose();
                throw new InvalidDataException($"Failed to initialize DefaultResource from stream: {ex.Message}", ex);
            }
        }
        else
        {
            _originalSize = 0;
            _stream = new MemoryStream();
            _detectedResourceTypeHint = null;
        }

        // Populate base metadata
        _metadata["CreatedAt"] = _createdAt;
        _metadata["OriginalSize"] = _originalSize;
        _metadata["ApiVersion"] = _apiVersion;
        if (_detectedResourceTypeHint != null)
        {
            _metadata["DetectedTypeHint"] = _detectedResourceTypeHint;
        }
    }

    /// <inheritdoc />
    public int RequestedApiVersion => _apiVersion;

    /// <inheritdoc />
    public int RecommendedApiVersion => _apiVersion;

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => Array.Empty<string>();

    /// <inheritdoc />
    public Stream Stream
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _stream.Position = 0;
            return _stream;
        }
    }

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _stream.ToArray();
        }
    }

    /// <summary>
    /// Gets metadata about this resource instance.
    /// Enhanced feature providing additional resource information.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _metadata;
        }
    }

    /// <summary>
    /// Gets the detected resource type hint if available.
    /// Enhanced feature for better resource identification.
    /// </summary>
    public string? DetectedResourceTypeHint
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _detectedResourceTypeHint;
        }
    }

    /// <summary>
    /// Gets the original size of the resource data.
    /// Enhanced feature for performance monitoring.
    /// </summary>
    public long OriginalSize
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _originalSize;
        }
    }

    /// <summary>
    /// Gets the creation timestamp of this resource instance.
    /// Enhanced feature for diagnostics and tracking.
    /// </summary>
    public DateTime CreatedAt
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _createdAt;
        }
    }

    /// <inheritdoc />
    public event EventHandler? ResourceChanged
    {
        add { /* This event is not triggered by DefaultResource as it represents immutable resource data */ }
        remove { /* This event is not triggered by DefaultResource as it represents immutable resource data */ }
    }

    /// <inheritdoc />
    public TypedValue this[string index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            throw new NotSupportedException("Default resource does not support content field access by string index");
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            throw new NotSupportedException("Default resource does not support content field access by string index");
        }
    }

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            throw new NotSupportedException("Default resource does not support content field access by integer index");
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            throw new NotSupportedException("Default resource does not support content field access by integer index");
        }
    }

    /// <summary>
    /// Enhanced method to detect resource type hints from stream data.
    /// Analyzes the first few bytes to provide hints about the resource type.
    /// </summary>
    /// <param name="stream">Stream to analyze</param>
    /// <returns>Resource type hint if detected, null otherwise</returns>
    private static string? DetectResourceTypeHint(Stream stream)
    {
        if (stream.Length < 4)
        {
            return null;
        }

        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            var buffer = new byte[16];
            var bytesRead = stream.Read(buffer, 0, Math.Min(16, (int)stream.Length));

            if (bytesRead >= 4)
            {
                // Check for common Sims 4 resource signatures
                var signature = Encoding.ASCII.GetString(buffer, 0, 4);

                return signature switch
                {
                    "DATA" => "DataResource",
                    "TRIM" => "TrimResource",
                    "STBL" => "StringTableResource",
                    "THUM" => "ThumbnailResource",
                    "MTBL" => "MaterialTableResource",
                    "VPXY" => "ViewportResource",
                    "MLOD" => "ModelResource",
                    "JAZZ" => "JazzResource",
                    "GEOM" => "GeometryResource",
                    "MATD" => "MaterialDefinitionResource",
                    _ when IsImageFormat(buffer, bytesRead) => "ImageResource",
                    _ when IsXmlFormat(buffer, bytesRead) => "XmlResource",
                    _ when IsBinaryFormat(buffer, bytesRead) => "BinaryResource",
                    _ => null
                };
            }

            return null;
        }
        catch
        {
            // If detection fails, return null rather than throwing
            return null;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Enhanced method to extract metadata from stream data.
    /// Populates the metadata dictionary with useful information.
    /// </summary>
    /// <param name="stream">Stream to analyze</param>
    private void ExtractMetadata(Stream stream)
    {
        try
        {
            var originalPosition = stream.Position;
            stream.Position = 0;

            // Calculate basic statistics
            _metadata["DataLength"] = stream.Length;
            _metadata["HasData"] = stream.Length > 0;

            if (stream.Length > 0)
            {
                // Calculate hash for data integrity
                var buffer = new byte[Math.Min(1024, stream.Length)];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                _metadata["SampleDataHash"] = CalculateSimpleHash(buffer, bytesRead);

                // Analyze data characteristics
                var stats = AnalyzeDataStatistics(buffer, bytesRead);
                _metadata["DataStatistics"] = stats;

                // Check for null bytes (indicates binary data)
                _metadata["ContainsNullBytes"] = Array.IndexOf(buffer, (byte)0, 0, bytesRead) >= 0;

                // Check for common text indicators
                _metadata["LikelyTextData"] = IsLikelyTextData(buffer, bytesRead);
            }

            stream.Position = originalPosition;
        }
        catch
        {
            // If metadata extraction fails, continue with basic metadata only
        }
    }

    /// <summary>
    /// Checks if the data appears to be an image format.
    /// </summary>
    private static bool IsImageFormat(byte[] buffer, int length)
    {
        if (length < 4) return false;

        // PNG signature
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            return true;

        // JPEG signature
        if (buffer[0] == 0xFF && buffer[1] == 0xD8)
            return true;

        // DDS signature
        if (length >= 4 && Encoding.ASCII.GetString(buffer, 0, 4) == "DDS ")
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the data appears to be XML format.
    /// </summary>
    private static bool IsXmlFormat(byte[] buffer, int length)
    {
        if (length < 5) return false;

        var text = Encoding.UTF8.GetString(buffer, 0, Math.Min(length, 16));
        return text.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
               text.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the data appears to be a binary format.
    /// </summary>
    private static bool IsBinaryFormat(byte[] buffer, int length)
    {
        if (length < 4) return false;

        // Check for high percentage of non-printable characters
        var nonPrintableCount = 0;
        for (var i = 0; i < Math.Min(length, 64); i++)
        {
            var b = buffer[i];
            if (b < 32 && b != 9 && b != 10 && b != 13) // Not tab, LF, CR
            {
                nonPrintableCount++;
            }
        }

        return (double)nonPrintableCount / Math.Min(length, 64) > 0.3; // >30% non-printable
    }

    /// <summary>
    /// Checks if the data is likely text data.
    /// </summary>
    private static bool IsLikelyTextData(byte[] buffer, int length)
    {
        if (length == 0) return false;

        var printableCount = 0;
        for (var i = 0; i < Math.Min(length, 64); i++)
        {
            var b = buffer[i];
            if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13) // Printable ASCII or whitespace
            {
                printableCount++;
            }
        }

        return (double)printableCount / Math.Min(length, 64) > 0.8; // >80% printable
    }

    /// <summary>
    /// Calculates a simple hash for data integrity checking.
    /// </summary>
    private static uint CalculateSimpleHash(byte[] buffer, int length)
    {
        uint hash = 0;
        for (var i = 0; i < length; i++)
        {
            hash = hash * 31 + buffer[i];
        }
        return hash;
    }

    /// <summary>
    /// Analyzes basic statistics about the data.
    /// </summary>
    private static Dictionary<string, object> AnalyzeDataStatistics(byte[] buffer, int length)
    {
        var stats = new Dictionary<string, object>();

        if (length == 0)
        {
            stats["IsEmpty"] = true;
            return stats;
        }

        var min = buffer[0];
        var max = buffer[0];
        var sum = 0L;

        for (var i = 0; i < length; i++)
        {
            var b = buffer[i];
            if (b < min) min = b;
            if (b > max) max = b;
            sum += b;
        }

        stats["MinByte"] = min;
        stats["MaxByte"] = max;
        stats["AverageByte"] = sum / (double)length;
        stats["ByteRange"] = max - min;
        stats["IsEmpty"] = false;

        return stats;
    }

    /// <summary>
    /// Gets diagnostic information about this resource.
    /// Enhanced feature for debugging and analysis.
    /// </summary>
    /// <returns>Diagnostic information string</returns>
    public string GetDiagnosticInfo()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var sb = new StringBuilder();
        sb.AppendLine($"DefaultResource Diagnostic Information:");
        sb.AppendLine($"  API Version: {_apiVersion}");
        sb.AppendLine($"  Created At: {_createdAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"  Original Size: {_originalSize:N0} bytes");
        sb.AppendLine($"  Current Size: {_stream.Length:N0} bytes");
        sb.AppendLine($"  Detected Type Hint: {_detectedResourceTypeHint ?? "None"}");
        sb.AppendLine($"  Metadata Count: {_metadata.Count}");

        foreach (var kvp in _metadata)
        {
            sb.AppendLine($"    {kvp.Key}: {kvp.Value}");
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _stream?.Dispose();
            _metadata.Clear();
        }
        catch
        {
            // Suppress exceptions during disposal
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Returns a string representation of this resource with enhanced information.
    /// </summary>
    public override string ToString()
    {
        if (_disposed) return "DefaultResource (Disposed)";

        var typeHint = string.IsNullOrEmpty(_detectedResourceTypeHint) ? "Unknown" : _detectedResourceTypeHint;
        return $"DefaultResource ({typeHint}, {_stream.Length:N0} bytes, API v{_apiVersion})";
    }
}
