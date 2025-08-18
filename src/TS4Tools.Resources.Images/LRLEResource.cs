using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TS4Tools.Core.Resources;
using TS4Tools.Resources.Common;
using TS4Tools.Core.Interfaces.Resources;
using TS4Tools.Core.Interfaces;
using CoreLRLE = TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Images;

/// <summary>
/// Represents a Lossless Run-Length Encoded (LRLE) image resource.
/// LRLE format provides lossless compression for images with color palettes.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Exception handling for various image processing scenarios")]
[SuppressMessage("Design", "CA1034:Nested types should not be visible",
    Justification = "Nested types provide logical grouping for LRLE-specific data")]
public sealed class LRLEResource : CoreLRLE.ILRLEResource, IDisposable
{
    private const uint MagicNumber = 0x454C524C; // 'LRLE'
    private const uint Version1 = 0x32303056; // 'V002'
    private const uint Version0 = 0x0; // Alternative version marker

    /// <summary>
    /// LRLE compression state machine for encoding algorithm.
    /// </summary>
    private enum EncodingState
    {
        StartNew,
        Unknown,
        ColorRun,
        RepeatRun
    }

    /// <summary>
    /// LRLE compressed data header structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct LRLEHeader
    {
        public readonly uint Magic;
        public readonly uint Version;
        public readonly ushort Width;
        public readonly ushort Height;
        public readonly byte MipMapCount;
        public readonly byte Reserved1;
        public readonly byte Reserved2;
        public readonly byte Reserved3;

        public LRLEHeader(ushort width, ushort height, byte mipMapCount, CoreLRLE.LRLEVersion version)
        {
            Magic = MagicNumber;
            Version = version == CoreLRLE.LRLEVersion.Version2 ? Version1 : Version0;
            Width = width;
            Height = height;
            MipMapCount = mipMapCount;
            Reserved1 = 0;
            Reserved2 = 0;
            Reserved3 = 0;
        }
    }

    private byte[]? _rawData;
    private LRLEColorTable? _colorTable;
    private Image<Rgba32>? _cachedBitmap;
    private bool _disposed;
    private readonly object _lockObject = new();

    // Property backing fields
    private int _width;
    private int _height;
    private byte _mipMapCount;
    private CoreLRLE.LRLEVersion _version;

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int Width
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _width;
        }
        private set => _width = value;
    }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int Height
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _height;
        }
        private set => _height = value;
    }

    /// <summary>
    /// Gets the number of mip map levels.
    /// </summary>
    public byte MipMapCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _mipMapCount;
        }
        private set => _mipMapCount = value;
    }

    /// <summary>
    /// Gets the LRLE format version.
    /// </summary>
    public CoreLRLE.LRLEVersion Version
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _version;
        }
        private set => _version = value;
    }

    /// <summary>
    /// Gets the number of mip map levels as uint (interface requirement).
    /// </summary>
    public uint MipCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return MipMapCount;
        }
    }

    /// <summary>
    /// Gets the magic number identifier for this LRLE resource.
    /// </summary>
    public uint Magic
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return MagicNumber;
        }
    }

    /// <summary>
    /// Gets the number of colors in the palette.
    /// </summary>
    public uint ColorCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return (uint)(ColorTable?.Count ?? 0);
        }
    }

    /// <summary>
    /// Gets whether the resource has been loaded with data.
    /// </summary>
    public bool HasData => _rawData != null;

    /// <summary>
    /// Gets the color table for this LRLE resource.
    /// </summary>
    public LRLEColorTable? ColorTable
    {
        get
        {
            ThrowIfDisposed();
            return _colorTable;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LRLEResource"/> class.
    /// </summary>
    public LRLEResource()
    {
        _colorTable = new LRLEColorTable();
    }

    /// <summary>
    /// Parses LRLE resource data from a byte array.
    /// </summary>
    /// <param name="data">The raw LRLE data to parse.</param>
    /// <returns>A task representing the asynchronous parse operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data is invalid or corrupted.</exception>
    public async Task ParseAsync(byte[] data)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 16)
        {
            throw new InvalidDataException("Insufficient data for LRLE header");
        }

        await Task.Run(() =>
        {
            lock (_lockObject)
            {
                try
                {
                    ParseLRLEData(data);
                    _rawData = data;
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Failed to parse LRLE data: {ex.Message}", ex);
                }
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts the LRLE resource to a bitmap asynchronously.
    /// </summary>
    /// <returns>A task containing the converted bitmap.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no data is loaded.</exception>
    public async Task<Image<Rgba32>> ToBitmapAsync()
    {
        ThrowIfDisposed();

        if (!HasData)
        {
            throw new InvalidOperationException("No LRLE data loaded");
        }

        // Return cached bitmap if available
        if (_cachedBitmap != null)
        {
            return _cachedBitmap.Clone();
        }

        return await Task.Run(() =>
        {
            lock (_lockObject)
            {
                if (_cachedBitmap != null)
                {
                    return _cachedBitmap.Clone();
                }

                var bitmap = DecompressLRLE();
                _cachedBitmap = bitmap.Clone();
                return bitmap;
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the raw LRLE compressed data.
    /// </summary>
    /// <returns>The raw data bytes, or null if no data is loaded.</returns>
    public byte[]? GetRawData()
    {
        ThrowIfDisposed();
        return _rawData?.ToArray();
    }

    /// <summary>
    /// Creates an LRLE resource from an image asynchronously.
    /// </summary>
    /// <param name="image">The source image to compress.</param>
    /// <param name="version">The LRLE version to use.</param>
    /// <param name="generateMipMaps">Whether to generate mip map levels.</param>
    /// <returns>A task containing the created LRLE resource.</returns>
    /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
    public static async Task<CoreLRLE.ILRLEResource> CreateFromImageAsync(Image<Rgba32> image, CoreLRLE.LRLEVersion version = CoreLRLE.LRLEVersion.Version2, bool generateMipMaps = false)
    {
        ArgumentNullException.ThrowIfNull(image);

        return await Task.Run(() =>
        {
            var resource = new LRLEResource();
            resource.CompressFromImage(image, version, generateMipMaps);
            return resource;
        });
    }

    private void ParseLRLEData(byte[] data)
    {
        var span = data.AsSpan();

        // Read header
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(span[0..4]);
        var version = BinaryPrimitives.ReadUInt32LittleEndian(span[4..8]);

        if (magic != MagicNumber)
        {
            throw new InvalidDataException($"Invalid LRLE magic number: 0x{magic:X8}");
        }

        Version = version switch
        {
            Version1 => CoreLRLE.LRLEVersion.Version2,
            Version0 => CoreLRLE.LRLEVersion.Version1,
            _ => throw new InvalidDataException($"Unsupported LRLE version: 0x{version:X8}")
        };

        Width = BinaryPrimitives.ReadUInt16LittleEndian(span[8..10]);
        Height = BinaryPrimitives.ReadUInt16LittleEndian(span[10..12]);
        MipMapCount = span[12];

        if (Width == 0 || Height == 0)
        {
            throw new InvalidDataException("Invalid LRLE dimensions");
        }

        if (MipMapCount > 9) // Reasonable limit for mip maps
        {
            throw new InvalidDataException($"Invalid mip map count: {MipMapCount}");
        }

        // Parse compressed data (implementation depends on specific LRLE algorithm)
        ParseCompressedData(span[16..]);
    }

    private void ParseCompressedData(ReadOnlySpan<byte> compressedData)
    {
        // Initialize color table for this parsing session
        _colorTable?.Clear();
        _colorTable ??= new LRLEColorTable();

        int offset = 0;
        byte runLength = 0;
        LRLEColorEntry currentColor = default;

        // Parse color palette first (if version 1)
        if (Version == CoreLRLE.LRLEVersion.Version2 && compressedData.Length > 4)
        {
            var paletteSize = BinaryPrimitives.ReadUInt16LittleEndian(compressedData[offset..(offset + 2)]);
            offset += 2;

            for (int i = 0; i < paletteSize && offset + 4 <= compressedData.Length; i++)
            {
                var colorBytes = compressedData.Slice(offset, 4);
                var color = LRLEColorEntry.FromBytes(colorBytes);
                _colorTable.AddColor(color);
                offset += 4;
            }

            _colorTable.SortByUsage();
        }

        // Parse run-length encoded data
        // (This is a simplified implementation - the actual LRLE algorithm would be more complex)
        while (offset < compressedData.Length)
        {
            var controlByte = compressedData[offset++];

            if ((controlByte & 0x80) != 0) // Run of repeated color
            {
                runLength = (byte)(controlByte & 0x7F);
                if (offset + 4 <= compressedData.Length)
                {
                    currentColor = LRLEColorEntry.FromBytes(compressedData.Slice(offset, 4));
                    offset += 4;

                    for (int i = 0; i < runLength; i++)
                    {
                        _colorTable.AddColor(currentColor);
                    }
                }
            }
            else // Run of different colors
            {
                runLength = controlByte;
                for (int i = 0; i < runLength && offset + 4 <= compressedData.Length; i++)
                {
                    currentColor = LRLEColorEntry.FromBytes(compressedData.Slice(offset, 4));
                    _colorTable.AddColor(currentColor);
                    offset += 4;
                }
            }
        }
    }

    private Image<Rgba32> DecompressLRLE()
    {
        if (_rawData == null)
        {
            throw new InvalidOperationException("No raw data available for decompression");
        }

        var bitmap = new Image<Rgba32>(Width, Height);

        // Simple decompression algorithm (actual implementation would be more sophisticated)
        var palette = _colorTable?.GetPalette() ?? [];

        // For now, create a simple gradient pattern as placeholder
        // In a real implementation, this would decode the actual LRLE data
        bitmap.ProcessPixelRows(accessor =>
        {
            int pixelIndex = 0;
            for (int y = 0; y < accessor.Height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < pixelRow.Length; x++)
                {
                    // Use color from palette if available, otherwise generate pattern
                    if (palette.Length > 0)
                    {
                        var colorEntry = palette[pixelIndex % palette.Length];
                        pixelRow[x] = new Rgba32(colorEntry.R, colorEntry.G, colorEntry.B, colorEntry.A);
                    }
                    else
                    {
                        // Default pattern for empty palette
                        byte intensity = (byte)((x + y) % 256);
                        pixelRow[x] = new Rgba32(intensity, intensity, intensity, 255);
                    }
                    pixelIndex++;
                }
            }
        });

        return bitmap;
    }

    private void CompressFromImage(Image<Rgba32> image, CoreLRLE.LRLEVersion version, bool generateMipMaps)
    {
        Width = image.Width;
        Height = image.Height;
        Version = version;
        MipMapCount = generateMipMaps ? CalculateMipMapCount(Width, Height) : (byte)1;

        _colorTable?.Clear();
        _colorTable ??= new LRLEColorTable();

        // Analyze image for color palette
        var pixels = new List<Rgba32>();
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < pixelRow.Length; x++)
                {
                    pixels.Add(pixelRow[x]);
                }
            }
        });

        foreach (var pixel in pixels)
        {
            var colorEntry = new LRLEColorEntry(pixel.R, pixel.G, pixel.B, pixel.A);
            _colorTable.AddColor(colorEntry);
        }

        _colorTable.SortByUsage();

        // Create compressed data
        var compressedData = CompressPixelData(pixels.ToArray());

        // Build final LRLE data with header
        var headerSize = 16;
        var paletteSize = Version == CoreLRLE.LRLEVersion.Version2 ? (_colorTable.Count * 4) + 2 : 0;
        var totalSize = headerSize + paletteSize + compressedData.Length;

        _rawData = new byte[totalSize];
        var span = _rawData.AsSpan();

        // Write header
        var header = new LRLEHeader((ushort)Width, (ushort)Height, MipMapCount, Version);
        MemoryMarshal.Write(span[0..16], in header);

        int offset = headerSize;

        // Write palette (version 1 only)
        if (Version == CoreLRLE.LRLEVersion.Version2)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span[offset..(offset + 2)], (ushort)_colorTable.Count);
            offset += 2;

            var palette = _colorTable.GetPalette();
            foreach (var color in palette)
            {
                var colorBytes = color.ToByteArray();
                colorBytes.CopyTo(span[offset..(offset + 4)]);
                offset += 4;
            }
        }

        // Write compressed data
        compressedData.CopyTo(span[offset..]);
    }

    private byte[] CompressPixelData(ReadOnlySpan<Rgba32> pixels)
    {
        var compressed = new List<byte>();
        int index = 0;

        while (index < pixels.Length)
        {
            var currentPixel = pixels[index];
            var currentColor = new LRLEColorEntry(currentPixel.R, currentPixel.G, currentPixel.B, currentPixel.A);

            // Look for run of same color
            int runLength = 1;
            while (index + runLength < pixels.Length && runLength < 127)
            {
                var nextPixel = pixels[index + runLength];
                var nextColor = new LRLEColorEntry(nextPixel.R, nextPixel.G, nextPixel.B, nextPixel.A);

                if (currentColor.Equals(nextColor))
                {
                    runLength++;
                }
                else
                {
                    break;
                }
            }

            int advanceBy;

            if (runLength > 1) // Repeated color run
            {
                compressed.Add((byte)(0x80 | runLength));
                compressed.AddRange(currentColor.ToByteArray());
                advanceBy = runLength;
            }
            else // Single color or start of different colors run
            {
                // Look for run of different colors
                int differentRunLength = 1;
                while (index + differentRunLength < pixels.Length && differentRunLength < 127)
                {
                    if (index + differentRunLength + 1 < pixels.Length)
                    {
                        var nextPixel = pixels[index + differentRunLength];
                        var nextNextPixel = pixels[index + differentRunLength + 1];
                        var nextColor = new LRLEColorEntry(nextPixel.R, nextPixel.G, nextPixel.B, nextPixel.A);
                        var nextNextColor = new LRLEColorEntry(nextNextPixel.R, nextNextPixel.G, nextNextPixel.B, nextNextPixel.A);

                        // If next two colors are the same, stop the different run
                        if (nextColor.Equals(nextNextColor))
                        {
                            break;
                        }
                    }
                    differentRunLength++;
                }

                compressed.Add((byte)differentRunLength);
                for (int i = 0; i < differentRunLength; i++)
                {
                    var pixel = pixels[index + i];
                    var color = new LRLEColorEntry(pixel.R, pixel.G, pixel.B, pixel.A);
                    compressed.AddRange(color.ToByteArray());
                }
                advanceBy = differentRunLength;
            }

            index += advanceBy;
        }

        return compressed.ToArray();
    }

    private static byte CalculateMipMapCount(int width, int height)
    {
        int maxDimension = Math.Max(width, height);
        byte count = 1;

        while (maxDimension > 1 && count < 9)
        {
            maxDimension /= 2;
            count++;
        }

        return count;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LRLEResource));
        }
    }

    #region IResource Interface Members

    /// <summary>
    /// Gets the resource content as a stream.
    /// </summary>
    public Stream Stream
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return HasData ? new MemoryStream(_rawData!) : new MemoryStream();
        }
    }

    /// <summary>
    /// Gets the resource content as a byte array.
    /// </summary>
    public byte[] AsBytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return HasData ? (byte[])_rawData!.Clone() : Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Raised if the resource is changed.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Gets the requested API version.
    /// </summary>
    public int RequestedApiVersion => 1;

    /// <summary>
    /// Gets the recommended API version.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Gets the content fields for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields => new List<string> { "Width", "Height", "Version", "MipCount", "Magic", "ColorCount" };

    /// <summary>
    /// Gets a content field by index.
    /// </summary>
    public TypedValue this[int index]
    {
        get => index switch
        {
            0 => new TypedValue(typeof(int), Width),
            1 => new TypedValue(typeof(int), Height),
            2 => new TypedValue(typeof(CoreLRLE.LRLEVersion), Version),
            3 => new TypedValue(typeof(uint), MipCount),
            4 => new TypedValue(typeof(uint), Magic),
            5 => new TypedValue(typeof(uint), ColorCount),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set => throw new NotSupportedException("LRLE resource fields are read-only");
    }

    /// <summary>
    /// Gets a content field by name.
    /// </summary>
    public TypedValue this[string fieldName]
    {
        get => fieldName switch
        {
            "Width" => new TypedValue(typeof(int), Width),
            "Height" => new TypedValue(typeof(int), Height),
            "Version" => new TypedValue(typeof(CoreLRLE.LRLEVersion), Version),
            "MipCount" => new TypedValue(typeof(uint), MipCount),
            "Magic" => new TypedValue(typeof(uint), Magic),
            "ColorCount" => new TypedValue(typeof(uint), ColorCount),
            _ => throw new ArgumentException($"Unknown field name: {fieldName}")
        };
        set => throw new NotSupportedException("LRLE resource fields are read-only");
    }

    #endregion

    #region ILRLEResource Interface Methods

    /// <summary>
    /// Converts the LRLE resource to a bitmap image.
    /// </summary>
    public async Task<Stream> ToBitmapAsync(int mipLevel = 0, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (mipLevel < 0 || mipLevel >= MipMapCount)
        {
            throw new ArgumentOutOfRangeException(nameof(mipLevel),
                $"Mip level must be between 0 and {MipMapCount - 1}");
        }

        var image = await ToBitmapAsync();
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream, cancellationToken);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Converts the LRLE resource to a bitmap image synchronously.
    /// </summary>
    public Stream ToBitmap(int mipLevel = 0)
    {
        return Task.Run(async () => await ToBitmapAsync(mipLevel).ConfigureAwait(false)).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the raw LRLE data for a specific mip level.
    /// </summary>
    public ReadOnlySpan<byte> GetRawData(int mipLevel = 0)
    {
        return HasData ? _rawData.AsSpan() : ReadOnlySpan<byte>.Empty;
    }

    /// <summary>
    /// Sets the LRLE data from a stream.
    /// </summary>
    public async Task SetDataAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        await ParseAsync(ms.ToArray());
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Creates LRLE resource from a bitmap image.
    /// </summary>
    public async Task CreateFromImageAsync(Stream imageStream, bool generateMipmaps = true, CancellationToken cancellationToken = default)
    {
        var image = await Image.LoadAsync<Rgba32>(imageStream, cancellationToken);
        var version = generateMipmaps ? CoreLRLE.LRLEVersion.Version2 : CoreLRLE.LRLEVersion.Version1;

        // Use the compression method directly instead of calling the static method
        CompressFromImage(image, version, generateMipmaps);
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    /// <summary>
    /// Releases all resources used by the <see cref="LRLEResource"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lockObject)
            {
                _cachedBitmap?.Dispose();
                _cachedBitmap = null;
                _colorTable?.Dispose();
                _colorTable = null;
                _rawData = null;
                _disposed = true;
            }
        }
    }
}
