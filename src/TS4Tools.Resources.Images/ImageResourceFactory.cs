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
    public static readonly IReadOnlySet<string> SupportedResourceTypeStrings =
        new System.Collections.ObjectModel.ReadOnlySet<string>(new HashSet<string>
        {
            "DDS",      // DDS Texture
            "PNG",      // PNG Image
            "TGA",      // TGA Image
            "JPEG",     // JPEG Image
            "BMP",      // BMP Image
            "IMG",      // Generic Image
            "TEX",      // Texture
        });

    /// <summary>
    /// Resource types that this factory can handle (numeric IDs).
    /// </summary>
    public new IReadOnlySet<uint> ResourceTypes { get; }

    /// <summary>
    /// Initializes a new instance of the ImageResourceFactory.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public ImageResourceFactory(ILogger<ImageResourceFactory>? logger = null)
        : base(BuildSupportedResourceTypes(), priority: 100)
    {
        _logger = logger;

        // Build the correct resource types collection using our override method
        var resourceTypeIds = new HashSet<uint>();
        foreach (var typeString in SupportedResourceTypeStrings)
        {
            if (TryGetResourceTypeId(typeString, out var id))
            {
                resourceTypeIds.Add(id);
            }
        }
        ResourceTypes = resourceTypeIds;
    }

    /// <summary>
    /// Builds the complete list of supported resource types including both string names and hex equivalents.
    /// </summary>
    private static IEnumerable<string> BuildSupportedResourceTypes()
    {
        var supportedTypes = new HashSet<string>(SupportedResourceTypeStrings);

        // Add hex equivalents
        foreach (var typeString in SupportedResourceTypeStrings)
        {
            var hexValue = typeString.ToUpperInvariant() switch
            {
                "DDS" => $"0x{ImageResource.DdsResourceType:X8}",     // 0x00B2D882
                "PNG" => $"0x{ImageResource.PngResourceType:X8}",     // 0x2F7D0004  
                "TGA" => $"0x{ImageResource.TgaResourceType:X8}",     // 0x2F7D0005
                "JPEG" => $"0x{ImageResource.JpegResourceType:X8}",   // 0x2F7D0002
                "BMP" => $"0x{ImageResource.BmpResourceType:X8}",     // 0x2F7D0003
                "IMG" => $"0x{ImageResource.PngResourceType:X8}",     // Use PNG type for generic images
                "TEX" => $"0x{ImageResource.DdsResourceType:X8}",     // Use DDS type for textures
                _ => null
            };

            if (hexValue != null)
            {
                supportedTypes.Add(hexValue);
            }
        }

        return supportedTypes;
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
                throw new InvalidDataException($"Unable to detect image format for resource type 0x{ImageResource.PngResourceType:X8}");
            }

            _logger?.LogDebug("Created {Format} image resource: {Width}x{Height} ({DataSize} bytes)",
                resource.Metadata.Format, resource.Metadata.Width, resource.Metadata.Height, resource.Metadata.DataSize);

            return resource;
        }
        catch (InvalidDataException ex) when (ex.Message.StartsWith("Unable to detect image format"))
        {
            // Re-throw our specific format detection errors
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create image resource from stream");
            throw new InvalidDataException("Failed to parse image data from stream", ex);
        }
    }

    /// <summary>
    /// Creates an ImageResource from a stream with validation.
    /// </summary>
    /// <param name="stream">Stream containing image data.</param>
    /// <param name="resourceType">Resource type ID.</param>
    /// <returns>A new ImageResource instance.</returns>
    public override ImageResource CreateResource(Stream stream, uint resourceType)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!CanCreateResource(resourceType))
        {
            throw new ArgumentException($"Resource type 0x{resourceType:X8} is not supported by ImageResourceFactory", nameof(resourceType));
        }

        // Read and validate stream data
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var data = memoryStream.ToArray();

        if (data.Length == 0)
        {
            throw new ArgumentException("Image data cannot be empty", nameof(stream));
        }

        // Validate that the data is actually valid image data
        var detectedFormat = DetectImageFormat(data);
        if (detectedFormat == ImageFormat.Unknown)
        {
            throw new InvalidDataException($"Unable to detect image format for resource type 0x{resourceType:X8}");
        }

        try
        {
            var resource = new ImageResource(data, 1, null);

            _logger?.LogDebug("Created {Format} image resource: {Width}x{Height} ({DataSize} bytes)",
                resource.Metadata.Format, resource.Metadata.Width, resource.Metadata.Height, resource.Metadata.DataSize);

            return resource;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create image resource from stream");
            throw new InvalidDataException($"Unable to detect image format for resource type 0x{resourceType:X8}", ex);
        }
    }

    /// <summary>
    /// Detects the image format from the provided data.
    /// </summary>
    /// <param name="data">The image data to analyze.</param>
    /// <returns>The detected image format.</returns>
    public static ImageFormat DetectImageFormat(ReadOnlySpan<byte> data)
    {
        if (data.Length < 2)
            return ImageFormat.Unknown;

        // Check DDS magic number - only need 4 bytes
        if (data.Length >= 4 &&
            data[0] == 0x44 && data[1] == 0x44 && data[2] == 0x53 && data[3] == 0x20) // "DDS "
        {
            return ImageFormat.DDS;
        }

        // Check PNG magic number - need 8 bytes
        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
        {
            return ImageFormat.PNG;
        }

        // Check JPEG magic number - only need 2 bytes
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
        {
            return ImageFormat.JPEG;
        }

        // Check BMP magic number - only need 2 bytes
        if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D) // "BM"
        {
            return ImageFormat.BMP;
        }

        // Check TGA (basic heuristic - TGA has no standard magic number)
        // Be more conservative with TGA detection to avoid false positives
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

    /// <summary>
    /// Converts resource type strings to numeric IDs using ImageResource constants.
    /// </summary>
    /// <param name="resourceType">Resource type string</param>
    /// <param name="id">Resulting numeric ID</param>
    /// <returns>True if conversion was successful</returns>
    protected override bool TryGetResourceTypeId(string resourceType, out uint id)
    {
        // Use ImageResource constants for correct mappings
        id = resourceType.ToUpperInvariant() switch
        {
            "DDS" => ImageResource.DdsResourceType,     // 0x00B2D882
            "PNG" => ImageResource.PngResourceType,     // 0x2F7D0004  
            "TGA" => ImageResource.TgaResourceType,     // 0x2F7D0005
            "JPEG" => ImageResource.JpegResourceType,   // 0x2F7D0002
            "BMP" => ImageResource.BmpResourceType,     // 0x2F7D0003
            "IMG" => ImageResource.PngResourceType,     // Use PNG type for generic images
            "TEX" => ImageResource.DdsResourceType,     // Use DDS type for textures
            _ => 0
        };

        return id != 0;
    }

    /// <summary>
    /// Determines if this factory can create resources of the specified type.
    /// </summary>
    /// <param name="resourceType">The resource type ID to check</param>
    /// <returns>True if this factory supports the resource type</returns>
    public override bool CanCreateResource(uint resourceType)
    {
        return ResourceTypes.Contains(resourceType);
    }

    /// <summary>
    /// Creates an empty resource of the specified type.
    /// </summary>
    /// <param name="resourceType">The resource type ID</param>
    /// <returns>A new empty ImageResource instance</returns>
    /// <exception cref="ArgumentException">Thrown when the resource type is not supported</exception>
    public override ImageResource CreateEmptyResource(uint resourceType)
    {
        if (!CanCreateResource(resourceType))
        {
            throw new ArgumentException($"Resource type 0x{resourceType:X8} is not supported by ImageResourceFactory", nameof(resourceType));
        }

        // Use async method synchronously for compatibility
        return CreateResourceAsync(1, null).GetAwaiter().GetResult();
    }
}
