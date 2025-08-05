namespace TS4Tools.Resources.Images;

/// <summary>
/// Factory for creating ImageResource instances from various data sources.
/// Supports automatic format detection and validation.
/// </summary>
public sealed class ImageResourceFactory : ResourceFactoryBase<ImageResource>
{
    private readonly ILogger<ImageResourceFactory>? _logger;

    /// <summary>
    /// Resource types that this factory can handle.
    /// </summary>
    public static readonly IReadOnlySet<string> SupportedResourceTypeStrings = new HashSet<string>
    {
        "DDS",      // DDS Texture
        "PNG",      // PNG Image
        "TGA",      // TGA Image
        "JPEG",     // JPEG Image
        "BMP",      // BMP Image
        "IMG",      // Generic Image
        "TEX",      // Texture
    }.ToHashSet();

    /// <summary>
    /// Initializes a new instance of the ImageResourceFactory.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public ImageResourceFactory(ILogger<ImageResourceFactory>? logger = null)
        : base(SupportedResourceTypeStrings, priority: 100)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates an ImageResource asynchronously.
    /// </summary>
    /// <param name="apiVersion">The API version for the resource.</param>
    /// <param name="stream">Optional stream containing image data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new ImageResource instance.</returns>
    public override async Task<ImageResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            // Create empty resource
            return new ImageResource(apiVersion, null);
        }

        // Read data from stream
        using var memoryStream = await CreateMemoryStreamAsync(stream, cancellationToken);
        if (memoryStream == null)
        {
            return new ImageResource(apiVersion, null);
        }

        var data = memoryStream.ToArray();
        if (data.Length == 0)
        {
            _logger?.LogWarning("Empty stream provided for image resource creation");
            return new ImageResource(apiVersion, null);
        }

        try
        {
            var resource = new ImageResource(data, apiVersion, null);
            
            if (resource.Metadata.Format == ImageFormat.Unknown)
            {
                _logger?.LogWarning("Could not detect image format from stream data");
            }

            _logger?.LogDebug("Created {Format} image resource: {Width}x{Height} ({DataSize} bytes)",
                resource.Metadata.Format, resource.Metadata.Width, resource.Metadata.Height, resource.Metadata.DataSize);

            return resource;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create image resource from stream");
            throw new InvalidDataException("Failed to parse image data from stream", ex);
        }
    }

    /// <summary>
    /// Detects the image format from the provided data.
    /// </summary>
    /// <param name="data">The image data to analyze.</param>
    /// <returns>The detected image format.</returns>
    public static ImageFormat DetectImageFormat(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return ImageFormat.Unknown;

        // Check DDS magic number
        if (data.Length >= 128 && 
            data[0] == 0x44 && data[1] == 0x44 && data[2] == 0x53 && data[3] == 0x20) // "DDS "
        {
            return ImageFormat.DDS;
        }

        // Check PNG magic number
        if (data.Length >= 8 && 
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
        {
            return ImageFormat.PNG;
        }

        // Check JPEG magic number
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
        {
            return ImageFormat.JPEG;
        }

        // Check BMP magic number
        if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D) // "BM"
        {
            return ImageFormat.BMP;
        }

        // Check TGA (basic heuristic - TGA has no standard magic number)
        if (data.Length >= 18)
        {
            byte imageType = data[2];
            // Common TGA image types: 0, 1, 2, 3, 9, 10, 11
            if (imageType <= 3 || (imageType >= 9 && imageType <= 11))
            {
                // Additional validation could be done here
                return ImageFormat.TGA;
            }
        }

        return ImageFormat.Unknown;
    }

    /// <summary>
    /// Gets information about supported image formats and their capabilities.
    /// </summary>
    /// <returns>A dictionary mapping image formats to their capabilities.</returns>
    public static IReadOnlyDictionary<ImageFormat, string> GetSupportedFormats()
    {
        return new Dictionary<ImageFormat, string>
        {
            [ImageFormat.DDS] = "DirectDraw Surface with BC1/BC3/BC5 compression support",
            [ImageFormat.PNG] = "Portable Network Graphics with alpha channel support",
            [ImageFormat.TGA] = "Truevision TGA with alpha channel support",
            [ImageFormat.JPEG] = "JPEG compressed images (no alpha channel)",
            [ImageFormat.BMP] = "Windows Bitmap format"
        };
    }
}
