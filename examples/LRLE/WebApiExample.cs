// Example: Integration with ASP.NET Core Web API
// Demonstrates how to use LRLE resources in a web service

using Microsoft.AspNetCore.Mvc;
using TS4Tools.Resources.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TS4Tools.Examples.LRLE.WebApi;

/// <summary>
/// Web API controller for LRLE texture processing services
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TextureController : ControllerBase
{
    private readonly ILRLEResourceFactory _lrleFactory;
    private readonly ILogger<TextureController> _logger;

    public TextureController(ILRLEResourceFactory lrleFactory, ILogger<TextureController> logger)
    {
        _lrleFactory = lrleFactory;
        _logger = logger;
    }

    /// <summary>
    /// Uploads and compresses an image to LRLE format
    /// </summary>
    [HttpPost("compress")]
    public async Task<IActionResult> CompressImageAsync(
        IFormFile imageFile,
        [FromQuery] bool generateMipmaps = true,
        CancellationToken cancellationToken = default)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            return BadRequest("No image file provided");
        }

        // Validate file type
        var allowedTypes = new[] { "image/png", "image/jpeg", "image/bmp" };
        if (!allowedTypes.Contains(imageFile.ContentType))
        {
            return BadRequest($"Unsupported image type: {imageFile.ContentType}");
        }

        // Validate file size (max 10MB)
        if (imageFile.Length > 10 * 1024 * 1024)
        {
            return BadRequest("Image file too large (max 10MB)");
        }

        try
        {
            _logger.LogInformation("Processing image: {FileName} ({Size} bytes)",
                imageFile.FileName, imageFile.Length);

            // Compress image to LRLE
            using var imageStream = imageFile.OpenReadStream();
            using var lrleResource = await _lrleFactory.CreateResourceAsync(1, imageStream, cancellationToken);

            // Get compressed data
            var compressedData = lrleResource.GetRawData();
            var compressionRatio = (double)imageFile.Length / compressedData.Length;

            _logger.LogInformation("Compression completed: {Original} -> {Compressed} bytes ({Ratio:F2}x)",
                imageFile.Length, compressedData.Length, compressionRatio);

            // Return compressed LRLE data
            var result = new CompressedImageResponse
            {
                OriginalFileName = imageFile.FileName,
                OriginalSize = imageFile.Length,
                CompressedSize = compressedData.Length,
                CompressionRatio = compressionRatio,
                Width = lrleResource.Width,
                Height = lrleResource.Height,
                MipLevels = lrleResource.MipMapCount,
                Version = lrleResource.Version.ToString(),
                CompressedData = Convert.ToBase64String(compressedData.ToArray())
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image: {FileName}", imageFile.FileName);
            return StatusCode(500, $"Compression failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Decompresses LRLE data back to a PNG image
    /// </summary>
    [HttpPost("decompress")]
    public async Task<IActionResult> DecompressLrleAsync(
        [FromBody] DecompressionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.LrleData))
        {
            return BadRequest("No LRLE data provided");
        }

        try
        {
            // Decode base64 LRLE data
            var lrleBytes = Convert.FromBase64String(request.LrleData);

            _logger.LogInformation("Decompressing LRLE data: {Size} bytes", lrleBytes.Length);

            // Create LRLE resource from data
            using var lrleStream = new MemoryStream(lrleBytes);
            using var lrleResource = await _lrleFactory.CreateResourceAsync(1, lrleStream, cancellationToken);

            // Extract specified mip level
            var mipLevel = Math.Min(request.MipLevel, lrleResource.MipMapCount - 1);
            using var bitmapStream = await lrleResource.ToBitmapAsync(mipLevel, cancellationToken);

            // Convert to PNG
            var pngData = new byte[bitmapStream.Length];
            await bitmapStream.ReadExactlyAsync(pngData, cancellationToken);

            _logger.LogInformation("Decompression completed: {Width}x{Height} at mip level {MipLevel}",
                lrleResource.Width, lrleResource.Height, mipLevel);

            // Return PNG image
            return File(pngData, "image/png", $"decompressed_mip{mipLevel}.png");
        }
        catch (FormatException)
        {
            return BadRequest("Invalid base64 LRLE data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decompressing LRLE data");
            return StatusCode(500, $"Decompression failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets information about an LRLE resource without decompressing
    /// </summary>
    [HttpPost("info")]
    public async Task<IActionResult> GetLrleInfoAsync(
        [FromBody] InfoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.LrleData))
        {
            return BadRequest("No LRLE data provided");
        }

        try
        {
            var lrleBytes = Convert.FromBase64String(request.LrleData);

            using var lrleStream = new MemoryStream(lrleBytes);
            using var lrleResource = await _lrleFactory.CreateResourceAsync(1, lrleStream, cancellationToken);

            var info = new LrleInfoResponse
            {
                Width = lrleResource.Width,
                Height = lrleResource.Height,
                MipLevels = lrleResource.MipMapCount,
                Version = lrleResource.Version.ToString(),
                CompressedSize = lrleBytes.Length,
                ColorCount = lrleResource.ContentFields.Contains("ColorCount")
                    ? lrleResource["ColorCount"] as uint? ?? 0
                    : 0
            };

            return Ok(info);
        }
        catch (FormatException)
        {
            return BadRequest("Invalid base64 LRLE data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LRLE info");
            return StatusCode(500, $"Info extraction failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Batch processing endpoint for multiple images
    /// </summary>
    [HttpPost("batch-compress")]
    public async Task<IActionResult> BatchCompressAsync(
        IFormFileCollection imageFiles,
        [FromQuery] bool generateMipmaps = true,
        CancellationToken cancellationToken = default)
    {
        if (!imageFiles.Any())
        {
            return BadRequest("No image files provided");
        }

        if (imageFiles.Count > 10)
        {
            return BadRequest("Maximum 10 files allowed per batch");
        }

        var results = new List<BatchCompressResult>();
        var semaphore = new SemaphoreSlim(3); // Limit concurrent processing

        try
        {
            var tasks = imageFiles.Select(async file =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await ProcessSingleFileAsync(file, generateMipmaps, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var batchResults = await Task.WhenAll(tasks);
            results.AddRange(batchResults);

            var response = new BatchCompressResponse
            {
                ProcessedFiles = results.Count,
                SuccessfulFiles = results.Count(r => r.Success),
                FailedFiles = results.Count(r => !r.Success),
                TotalOriginalSize = results.Where(r => r.Success).Sum(r => r.OriginalSize),
                TotalCompressedSize = results.Where(r => r.Success).Sum(r => r.CompressedSize),
                Results = results
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch compression");
            return StatusCode(500, $"Batch processing failed: {ex.Message}");
        }
    }

    private async Task<BatchCompressResult> ProcessSingleFileAsync(
        IFormFile file,
        bool generateMipmaps,
        CancellationToken cancellationToken)
    {
        try
        {
            using var imageStream = file.OpenReadStream();
            using var lrleResource = await _lrleFactory.CreateResourceAsync(1, imageStream, cancellationToken);

            var compressedData = lrleResource.GetRawData();

            return new BatchCompressResult
            {
                FileName = file.FileName,
                Success = true,
                OriginalSize = file.Length,
                CompressedSize = compressedData.Length,
                CompressionRatio = (double)file.Length / compressedData.Length,
                Width = lrleResource.Width,
                Height = lrleResource.Height,
                CompressedData = Convert.ToBase64String(compressedData.ToArray())
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process file: {FileName}", file.FileName);

            return new BatchCompressResult
            {
                FileName = file.FileName,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Service for registering LRLE dependencies
/// </summary>
public static class LrleServiceExtensions
{
    public static IServiceCollection AddLrleServices(this IServiceCollection services)
    {
        services.AddSingleton<ILRLEResourceFactory, LRLEResourceFactory>();
        services.AddScoped<TextureProcessingService>();
        return services;
    }
}

/// <summary>
/// Business logic service for texture processing
/// </summary>
public class TextureProcessingService
{
    private readonly ILRLEResourceFactory _lrleFactory;
    private readonly ILogger<TextureProcessingService> _logger;

    public TextureProcessingService(ILRLEResourceFactory lrleFactory, ILogger<TextureProcessingService> logger)
    {
        _lrleFactory = lrleFactory;
        _logger = logger;
    }

    public async Task<ProcessingResult> OptimizeImageAsync(
        Stream imageStream,
        OptimizationOptions options,
        CancellationToken cancellationToken = default)
    {
        // Pre-process image if needed
        if (options.MaxDimension.HasValue)
        {
            imageStream = await ResizeImageIfNeededAsync(imageStream, options.MaxDimension.Value, cancellationToken);
        }

        // Compress to LRLE
        using var lrleResource = await _lrleFactory.CreateResourceAsync(1, imageStream, cancellationToken);

        // Apply quality checks
        var quality = await AssessCompressionQualityAsync(imageStream, lrleResource, cancellationToken);

        return new ProcessingResult
        {
            CompressedData = lrleResource.GetRawData().ToArray(),
            Width = lrleResource.Width,
            Height = lrleResource.Height,
            MipLevels = lrleResource.MipMapCount,
            QualityScore = quality,
            CompressionRatio = (double)imageStream.Length / lrleResource.GetRawData().Length
        };
    }

    private async Task<Stream> ResizeImageIfNeededAsync(
        Stream imageStream,
        int maxDimension,
        CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

        if (image.Width <= maxDimension && image.Height <= maxDimension)
        {
            imageStream.Position = 0;
            return imageStream;
        }

        var scaleFactor = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);
        var newWidth = (int)(image.Width * scaleFactor);
        var newHeight = (int)(image.Height * scaleFactor);

        image.Mutate(x => x.Resize(newWidth, newHeight));

        var resizedStream = new MemoryStream();
        await image.SaveAsPngAsync(resizedStream, cancellationToken);
        resizedStream.Position = 0;

        return resizedStream;
    }

    private async Task<double> AssessCompressionQualityAsync(
        Stream originalStream,
        ILRLEResource lrleResource,
        CancellationToken cancellationToken)
    {
        // Simple quality assessment based on compression ratio and dimensions
        originalStream.Position = 0;
        using var originalImage = await Image.LoadAsync<Rgba32>(originalStream, cancellationToken);

        var compressionRatio = (double)originalStream.Length / lrleResource.GetRawData().Length;
        var dimensionScore = Math.Min(1.0, (double)(originalImage.Width * originalImage.Height) / (4096 * 4096));

        // Combine metrics for overall quality score
        return Math.Min(1.0, (compressionRatio / 10.0) * dimensionScore);
    }
}

// Data Transfer Objects (DTOs)

public record CompressedImageResponse
{
    public string OriginalFileName { get; init; } = "";
    public long OriginalSize { get; init; }
    public long CompressedSize { get; init; }
    public double CompressionRatio { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public byte MipLevels { get; init; }
    public string Version { get; init; } = "";
    public string CompressedData { get; init; } = "";
}

public record DecompressionRequest
{
    public string LrleData { get; init; } = "";
    public int MipLevel { get; init; } = 0;
}

public record InfoRequest
{
    public string LrleData { get; init; } = "";
}

public record LrleInfoResponse
{
    public int Width { get; init; }
    public int Height { get; init; }
    public byte MipLevels { get; init; }
    public string Version { get; init; } = "";
    public long CompressedSize { get; init; }
    public uint ColorCount { get; init; }
}

public record BatchCompressResult
{
    public string FileName { get; init; } = "";
    public bool Success { get; init; }
    public long OriginalSize { get; init; }
    public long CompressedSize { get; init; }
    public double CompressionRatio { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string CompressedData { get; init; } = "";
    public string? ErrorMessage { get; init; }
}

public record BatchCompressResponse
{
    public int ProcessedFiles { get; init; }
    public int SuccessfulFiles { get; init; }
    public int FailedFiles { get; init; }
    public long TotalOriginalSize { get; init; }
    public long TotalCompressedSize { get; init; }
    public List<BatchCompressResult> Results { get; init; } = new();
}

public record OptimizationOptions
{
    public int? MaxDimension { get; init; }
    public bool GenerateMipmaps { get; init; } = true;
    public double QualityThreshold { get; init; } = 0.8;
}

public record ProcessingResult
{
    public byte[] CompressedData { get; init; } = Array.Empty<byte>();
    public int Width { get; init; }
    public int Height { get; init; }
    public byte MipLevels { get; init; }
    public double QualityScore { get; init; }
    public double CompressionRatio { get; init; }
}
