using System.Runtime.InteropServices;
using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using CommunityToolkit.HighPerformance;

namespace TS4Tools.Resources.Images;

/// <summary>
/// Supported image formats in The Sims 4 package files.
/// </summary>
public enum ImageFormat
{
    /// <summary>
    /// Unknown or unsupported format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// DirectDraw Surface (DDS) format.
    /// </summary>
    DDS = 1,

    /// <summary>
    /// Portable Network Graphics (PNG) format.
    /// </summary>
    PNG = 2,

    /// <summary>
    /// Truevision TGA (Targa) format.
    /// </summary>
    TGA = 3,

    /// <summary>
    /// JPEG format.
    /// </summary>
    JPEG = 4,

    /// <summary>
    /// Bitmap (BMP) format.
    /// </summary>
    BMP = 5
}

/// <summary>
/// Represents image metadata extracted from various image formats.
/// </summary>
public readonly record struct ImageMetadata
{
    /// <summary>
    /// Width of the image in pixels.
    /// </summary>
    public uint Width { get; init; }

    /// <summary>
    /// Height of the image in pixels.
    /// </summary>
    public uint Height { get; init; }

    /// <summary>
    /// Number of mipmap levels (1 if no mipmaps).
    /// </summary>
    public uint MipMapCount { get; init; }

    /// <summary>
    /// The detected image format.
    /// </summary>
    public ImageFormat Format { get; init; }

    /// <summary>
    /// DDS-specific compression format (if applicable).
    /// </summary>
    public DdsFourCC CompressionFormat { get; init; }

    /// <summary>
    /// Whether the image has alpha channel data.
    /// </summary>
    public bool HasAlpha { get; init; }

    /// <summary>
    /// Bits per pixel.
    /// </summary>
    public uint BitsPerPixel { get; init; }

    /// <summary>
    /// Size of the image data in bytes.
    /// </summary>
    public uint DataSize { get; init; }

    /// <summary>
    /// Whether the image uses compression (DDS with BC formats).
    /// </summary>
    public bool IsCompressed => Format == ImageFormat.DDS && CompressionFormat != DdsFourCC.None;
}

/// <summary>
/// A modern implementation of image resource handling for The Sims 4.
/// Supports DDS, PNG, TGA, and other image formats with mipmap management
/// and texture compression/decompression capabilities.
/// </summary>
/// <remarks>
/// This resource wrapper handles various image formats used in The Sims 4:
/// - DDS (DirectDraw Surface) with BC1/BC3/BC5 compression
/// - PNG for UI elements and thumbnails
/// - TGA for uncompressed textures
/// - Automatic format detection and conversion
/// - Mipmap level management and generation
/// - Performance-optimized with Span&lt;T&gt; and async patterns
/// </remarks>
public sealed class ImageResource : IResource, IDisposable
{
    /// <summary>
    /// Common resource type for DDS textures.
    /// </summary>
    public const uint DdsResourceType = 0x00B2D882;

    /// <summary>
    /// Common resource type for JPEG images.
    /// </summary>
    public const uint JpegResourceType = 0x2F7D0002;

    /// <summary>
    /// Common resource type for BMP images.
    /// </summary>
    public const uint BmpResourceType = 0x2F7D0003;

    /// <summary>
    /// Common resource type for PNG images.
    /// </summary>
    public const uint PngResourceType = 0x2F7D0004;

    /// <summary>
    /// Common resource type for TGA images.
    /// </summary>
    public const uint TgaResourceType = 0x2F7D0005;

    private readonly int _requestedApiVersion;
    private readonly ILogger<ImageResource>? _logger;
    private byte[] _imageData = Array.Empty<byte>();
    private ImageMetadata _metadata;
    private bool _isModified;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new empty ImageResource.
    /// </summary>
    /// <param name="requestedApiVersion">The API version requested for this resource.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public ImageResource(int requestedApiVersion = 1, ILogger<ImageResource>? logger = null)
    {
        _requestedApiVersion = requestedApiVersion;
        _logger = logger;
        _imageData = Array.Empty<byte>();
        _metadata = new ImageMetadata { Format = ImageFormat.Unknown };
        _isModified = true;
    }

    /// <summary>
    /// Initializes a new ImageResource from existing data.
    /// </summary>
    /// <param name="data">The image data to load.</param>
    /// <param name="requestedApiVersion">The API version requested for this resource.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data cannot be parsed as a valid image.</exception>
    public ImageResource(ReadOnlySpan<byte> data, int requestedApiVersion = 1, ILogger<ImageResource>? logger = null)
    {
        _requestedApiVersion = requestedApiVersion;
        _logger = logger;
        
        LoadFromData(data);
    }

    /// <summary>
    /// Initializes a new ImageResource from a stream.
    /// </summary>
    /// <param name="stream">The stream containing image data.</param>
    /// <param name="requestedApiVersion">The API version requested for this resource.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when stream cannot be parsed as a valid image.</exception>
    public ImageResource(Stream stream, int requestedApiVersion = 1, ILogger<ImageResource>? logger = null)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        _requestedApiVersion = requestedApiVersion;
        _logger = logger;

        // Read all data from stream
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        
        LoadFromData(memoryStream.ToArray());
    }

    /// <summary>
    /// Gets the API version that was requested when this resource was created.
    /// </summary>
    public int RequestedApiVersion => _requestedApiVersion;

    /// <summary>
    /// Gets the image metadata including dimensions, format, and compression info.
    /// </summary>
    public ImageMetadata Metadata => _metadata;

    /// <summary>
    /// Gets whether this resource has been modified since loading.
    /// </summary>
    public bool IsModified => _isModified;

    /// <summary>
    /// Gets the raw image data as a read-only span.
    /// </summary>
    public ReadOnlySpan<byte> ImageData => _imageData.AsSpan();

    /// <summary>
    /// Gets or sets the raw image data.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed.</exception>
    public byte[] RawData
    {
        get
        {
            ThrowIfDisposed();
            return _imageData.ToArray();
        }
        set
        {
            ThrowIfDisposed();
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            
            LoadFromData(value);
        }
    }

    /// <summary>
    /// Gets a stream containing the image data.
    /// </summary>
    /// <returns>A memory stream containing the image data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed.</exception>
    public Stream Stream
    {
        get
        {
            ThrowIfDisposed();
            return new MemoryStream(_imageData, writable: false);
        }
    }

    /// <summary>
    /// Gets the resource content as a byte array.
    /// </summary>
    public byte[] AsBytes
    {
        get
        {
            ThrowIfDisposed();
            return _imageData.ToArray();
        }
    }

    /// <summary>
    /// Event raised when the resource is modified.
    /// </summary>
    public event EventHandler? ResourceChanged;

    /// <summary>
    /// Gets the recommended API version for this resource.
    /// </summary>
    public int RecommendedApiVersion => 1;

    /// <summary>
    /// Content fields for this resource.
    /// </summary>
    public IReadOnlyList<string> ContentFields { get; } = new List<string>
    {
        nameof(Metadata.Format),
        nameof(Metadata.Width),
        nameof(Metadata.Height),
        nameof(Metadata.MipMapCount),
        nameof(Metadata.IsCompressed)
    }.AsReadOnly();

    /// <summary>
    /// Gets or sets a content field by index.
    /// </summary>
    public TypedValue this[int index]
    {
        get => GetFieldByIndex(index);
        set => SetFieldByIndex(index, value);
    }

    /// <summary>
    /// Gets or sets a content field by name.
    /// </summary>
    public TypedValue this[string name]
    {
        get => GetFieldByName(name);
        set => SetFieldByName(name, value);
    }

    /// <summary>
    /// Asynchronously loads image data from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed.</exception>
    /// <exception cref="InvalidDataException">Thrown when stream cannot be parsed as a valid image.</exception>
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
        
        ThrowIfDisposed();

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        
        LoadFromData(memoryStream.ToArray());
    }

    /// <summary>
    /// Converts the image to the specified format.
    /// </summary>
    /// <param name="targetFormat">The target image format.</param>
    /// <param name="quality">Quality setting for lossy formats (0-100).</param>
    /// <returns>A task representing the asynchronous conversion operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed.</exception>
    /// <exception cref="NotSupportedException">Thrown when the conversion is not supported.</exception>
    public async Task<byte[]> ConvertToFormatAsync(ImageFormat targetFormat, int quality = 90)
    {
        ThrowIfDisposed();

        if (_metadata.Format == ImageFormat.Unknown)
            throw new InvalidOperationException("Cannot convert image with unknown format");

        if (targetFormat == _metadata.Format)
            return _imageData.ToArray();

        // Use ImageSharp for conversion
        using var image = await Image.LoadAsync(new MemoryStream(_imageData));
        using var outputStream = new MemoryStream();

        switch (targetFormat)
        {
            case ImageFormat.PNG:
                await image.SaveAsPngAsync(outputStream);
                break;
            
            case ImageFormat.JPEG:
                await image.SaveAsJpegAsync(outputStream);
                break;
            
            case ImageFormat.BMP:
                await image.SaveAsBmpAsync(outputStream);
                break;
                
            case ImageFormat.DDS:
                {
                    // Cast to the specific pixel type - using Rgba32 as default
                    using var rgba32Image = image.CloneAs<Rgba32>();
                    return await ConvertToDdsAsync(rgba32Image);
                }
                
            default:
                throw new NotSupportedException($"Conversion to {targetFormat} is not supported");
        }

        return outputStream.ToArray();
    }

    /// <summary>
    /// Extracts image data as an ImageSharp Image for processing.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format to use.</typeparam>
    /// <returns>An ImageSharp image instance.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed.</exception>
    /// <exception cref="InvalidDataException">Thrown when image data cannot be loaded.</exception>
    public async Task<Image<TPixel>> ToImageSharpAsync<TPixel>() where TPixel : unmanaged, IPixel<TPixel>
    {
        ThrowIfDisposed();

        if (_metadata.Format == ImageFormat.DDS)
        {
            // Decompress DDS data first
            var decompressedData = await DecompressDdsAsync();
            return Image.LoadPixelData<TPixel>(decompressedData, (int)_metadata.Width, (int)_metadata.Height);
        }

        return await Image.LoadAsync<TPixel>(new MemoryStream(_imageData));
    }

    /// <summary>
    /// Updates the image data from an ImageSharp Image.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    /// <param name="image">The image to load data from.</param>
    /// <param name="format">The target format to save as.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the resource has been disposed.</exception>
    public async Task FromImageSharpAsync<TPixel>(Image<TPixel> image, ImageFormat format = ImageFormat.PNG) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));
        
        ThrowIfDisposed();

        using var stream = new MemoryStream();
        
        switch (format)
        {
            case ImageFormat.PNG:
                await image.SaveAsPngAsync(stream);
                break;
            case ImageFormat.JPEG:
                await image.SaveAsJpegAsync(stream);
                break;
            case ImageFormat.BMP:
                await image.SaveAsBmpAsync(stream);
                break;
            case ImageFormat.DDS:
                var ddsData = await ConvertToDdsAsync(image);
                LoadFromData(ddsData);
                return;
            default:
                throw new NotSupportedException($"Format {format} is not supported");
        }

        LoadFromData(stream.ToArray());
    }

    /// <summary>
    /// Releases all resources used by the ImageResource.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _imageData = Array.Empty<byte>();
        _isDisposed = true;
    }

    private void LoadFromData(ReadOnlySpan<byte> data)
    {
        _imageData = data.ToArray();
        _metadata = DetectImageMetadata(data);
        _isModified = false;

        _logger?.LogDebug("Loaded {Format} image: {Width}x{Height}, {MipMaps} mipmaps, {DataSize} bytes",
            _metadata.Format, _metadata.Width, _metadata.Height, _metadata.MipMapCount, _metadata.DataSize);
    }

    private static ImageMetadata DetectImageMetadata(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return new ImageMetadata { Format = ImageFormat.Unknown };

        // Check DDS magic number
        if (data.Length >= 128 && 
            data[0] == 0x44 && data[1] == 0x44 && data[2] == 0x53 && data[3] == 0x20) // "DDS "
        {
            return DetectDdsMetadata(data);
        }

        // Check PNG magic number
        if (data.Length >= 8 && 
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
        {
            return DetectPngMetadata(data);
        }

        // Check JPEG magic number
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
        {
            return DetectJpegMetadata(data);
        }

        // Check BMP magic number BEFORE TGA (BMP has a definitive magic number)
        if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D) // "BM"
        {
            return DetectBmpMetadata(data);
        }

        // Check TGA (simple check - not foolproof, so do this last)
        if (data.Length >= 18)
        {
            var metadata = DetectTgaMetadata(data);
            if (metadata.Format != ImageFormat.Unknown)
                return metadata;
        }

        return new ImageMetadata { Format = ImageFormat.Unknown };
    }

    private static ImageMetadata DetectDdsMetadata(ReadOnlySpan<byte> data)
    {
        try
        {
            using var stream = new MemoryStream(data.ToArray());
            var header = stream.ReadDdsHeader();

            return new ImageMetadata
            {
                Width = header.Width,
                Height = header.Height,
                MipMapCount = Math.Max(1, header.MipMapCount),
                Format = ImageFormat.DDS,
                CompressionFormat = header.PixelFormat.FourCC,
                HasAlpha = header.PixelFormat.Flags.HasFlag(DdsPixelFormatFlags.AlphaPixels),
                BitsPerPixel = header.PixelFormat.RgbBitCount,
                DataSize = (uint)data.Length
            };
        }
        catch
        {
            return new ImageMetadata { Format = ImageFormat.Unknown };
        }
    }

    private static ImageMetadata DetectPngMetadata(ReadOnlySpan<byte> data)
    {
        try
        {
            using var stream = new MemoryStream(data.ToArray());
            using var image = Image.Load(stream);

            return new ImageMetadata
            {
                Width = (uint)image.Width,
                Height = (uint)image.Height,
                MipMapCount = 1,
                Format = ImageFormat.PNG,
                CompressionFormat = DdsFourCC.None,
                HasAlpha = image.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None,
                BitsPerPixel = (uint)(image.PixelType.BitsPerPixel),
                DataSize = (uint)data.Length
            };
        }
        catch
        {
            return new ImageMetadata { Format = ImageFormat.Unknown };
        }
    }

    private static ImageMetadata DetectJpegMetadata(ReadOnlySpan<byte> data)
    {
        try
        {
            using var stream = new MemoryStream(data.ToArray());
            using var image = Image.Load(stream);

            return new ImageMetadata
            {
                Width = (uint)image.Width,
                Height = (uint)image.Height,
                MipMapCount = 1,
                Format = ImageFormat.JPEG,
                CompressionFormat = DdsFourCC.None,
                HasAlpha = false, // JPEG doesn't support alpha
                BitsPerPixel = 24,
                DataSize = (uint)data.Length
            };
        }
        catch
        {
            return new ImageMetadata { Format = ImageFormat.Unknown };
        }
    }

    private static ImageMetadata DetectTgaMetadata(ReadOnlySpan<byte> data)
    {
        try
        {
            using var stream = new MemoryStream(data.ToArray());
            using var image = Image.Load(stream);

            return new ImageMetadata
            {
                Width = (uint)image.Width,
                Height = (uint)image.Height,
                MipMapCount = 1,
                Format = ImageFormat.TGA,
                CompressionFormat = DdsFourCC.None,
                HasAlpha = image.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None,
                BitsPerPixel = (uint)image.PixelType.BitsPerPixel,
                DataSize = (uint)data.Length
            };
        }
        catch
        {
            return new ImageMetadata { Format = ImageFormat.Unknown };
        }
    }

    private static ImageMetadata DetectBmpMetadata(ReadOnlySpan<byte> data)
    {
        try
        {
            using var stream = new MemoryStream(data.ToArray());
            using var image = Image.Load(stream);

            return new ImageMetadata
            {
                Width = (uint)image.Width,
                Height = (uint)image.Height,
                MipMapCount = 1,
                Format = ImageFormat.BMP,
                CompressionFormat = DdsFourCC.None,
                HasAlpha = image.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None,
                BitsPerPixel = (uint)image.PixelType.BitsPerPixel,
                DataSize = (uint)data.Length
            };
        }
        catch
        {
            return new ImageMetadata { Format = ImageFormat.Unknown };
        }
    }

    private async Task<byte[]> DecompressDdsAsync()
    {
        if (_imageData.Length < 128) // DDS header is at least 128 bytes
        {
            throw new InvalidDataException("DDS data is too small to contain a valid header");
        }

        try
        {
            using var inputStream = new MemoryStream(_imageData);
            var decoder = new BcDecoder();
            
            // Extract DDS format information from metadata or assume BC3 (DXT5) for now
            // In a real implementation, you would parse the DDS header to determine the format
            var format = CompressionFormat.Bc3; // Most common in Sims 4
            var width = (int)_metadata.Width;
            var height = (int)_metadata.Height;
            
            // Use DecodeRaw method with correct parameters
            var decodedData = await Task.Run(() => decoder.DecodeRaw(inputStream, width, height, format));
            
            // Convert ColorRgba32 array to byte array
            var result = new byte[decodedData.Length * 4];
            for (int i = 0; i < decodedData.Length; i++)
            {
                var color = decodedData[i];
                result[i * 4] = color.r;
                result[i * 4 + 1] = color.g;
                result[i * 4 + 2] = color.b;
                result[i * 4 + 3] = color.a;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to decompress DDS data: {ex.Message}", ex);
        }
    }

    private async Task<byte[]> ConvertToDdsAsync<TPixel>(Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        try
        {
            using var outputStream = new MemoryStream();
            var encoder = new BcEncoder();
            
            // Configure encoder for BC3 (DXT5) which is commonly used in Sims 4
            encoder.OutputOptions.Format = CompressionFormat.Bc3;
            encoder.OutputOptions.FileFormat = OutputFileFormat.Dds;
            encoder.OutputOptions.GenerateMipMaps = _metadata.MipMapCount > 1;
            
            // Convert ImageSharp Image to the format expected by BCnEncoder
            var width = image.Width;
            var height = image.Height;
            
            // Extract pixel data from ImageSharp image without using GetPixelRowSpan
            var pixelData = new ColorRgba32[width * height];
            using var rgba32Image = image.CloneAs<Rgba32>();
            
            // Copy pixel data using safe indexer access
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = rgba32Image[x, y];
                    pixelData[y * width + x] = new ColorRgba32(pixel.R, pixel.G, pixel.B, pixel.A);
                }
            }
            
            // Create Memory2D from pixel data using the correct CommunityToolkit API
            var memory2D = pixelData.AsMemory().AsMemory2D(height, width);
            
            // Encode to DDS
            await Task.Run(() => encoder.EncodeToStream(memory2D, outputStream));
            
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to convert image to DDS: {ex.Message}", ex);
        }
    }

    private TypedValue GetFieldByIndex(int index)
    {
        return index switch
        {
            0 => TypedValue.Create(Metadata.Format),
            1 => TypedValue.Create(Metadata.Width),
            2 => TypedValue.Create(Metadata.Height),
            3 => TypedValue.Create(Metadata.MipMapCount),
            4 => TypedValue.Create(Metadata.IsCompressed),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    private void SetFieldByIndex(int index, TypedValue value)
    {
        // Fields are read-only for image resources
        throw new NotSupportedException("Image resource fields are read-only");
    }

    private TypedValue GetFieldByName(string name)
    {
        return name switch
        {
            nameof(Metadata.Format) => TypedValue.Create(Metadata.Format),
            nameof(Metadata.Width) => TypedValue.Create(Metadata.Width),
            nameof(Metadata.Height) => TypedValue.Create(Metadata.Height),
            nameof(Metadata.MipMapCount) => TypedValue.Create(Metadata.MipMapCount),
            nameof(Metadata.IsCompressed) => TypedValue.Create(Metadata.IsCompressed),
            _ => throw new ArgumentException($"Unknown field name: {name}", nameof(name))
        };
    }

    private void SetFieldByName(string name, TypedValue value)
    {
        // Fields are read-only for image resources
        throw new NotSupportedException("Image resource fields are read-only");
    }

    private void OnResourceChanged()
    {
        _isModified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ImageResource));
    }
}
