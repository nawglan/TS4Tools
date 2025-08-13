// Example: Advanced LRLE Operations
// Demonstrates batch processing, error handling, and performance optimization

using TS4Tools.Resources.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TS4Tools.Examples.LRLE;

public class AdvancedLRLEExample
{
    private readonly LRLEResourceFactory _factory;

    public AdvancedLRLEExample()
    {
        _factory = new LRLEResourceFactory();
    }

    /// <summary>
    /// Demonstrates batch processing of multiple images with parallel execution
    /// </summary>
    public async Task BatchProcessImagesAsync(string inputDirectory, string outputDirectory)
    {
        Console.WriteLine("=== Batch Processing Example ===");

        // Ensure output directory exists
        Directory.CreateDirectory(outputDirectory);

        // Find all PNG files in input directory
        var pngFiles = Directory.GetFiles(inputDirectory, "*.png", SearchOption.TopDirectoryOnly);
        if (pngFiles.Length == 0)
        {
            Console.WriteLine($"No PNG files found in {inputDirectory}");
            await CreateSampleBatchAsync(inputDirectory);
            pngFiles = Directory.GetFiles(inputDirectory, "*.png", SearchOption.TopDirectoryOnly);
        }

        Console.WriteLine($"Processing {pngFiles.Length} images...");

        // Track processing results
        var results = new ConcurrentBag<ProcessingResult>();
        var stopwatch = Stopwatch.StartNew();

        // Process images in parallel with throttling
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = pngFiles.Select(async filePath =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await ProcessSingleImageAsync(filePath, outputDirectory);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var processingResults = await Task.WhenAll(tasks);
        foreach (var result in processingResults.Where(r => r != null))
        {
            results.Add(result!);
        }

        stopwatch.Stop();

        // Display summary statistics
        DisplayBatchResults(results, stopwatch.Elapsed);
    }

    /// <summary>
    /// Processes a single image with comprehensive error handling
    /// </summary>
    private async Task<ProcessingResult?> ProcessSingleImageAsync(string inputPath, string outputDirectory)
    {
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var outputPath = Path.Combine(outputDirectory, $"{fileName}.lrle");

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Validate and load source image
            using var imageStream = File.OpenRead(inputPath);
            var originalSize = imageStream.Length;

            // Pre-validate the image
            using var tempImage = await Image.LoadAsync<Rgba32>(imageStream);
            if (tempImage.Width > 4096 || tempImage.Height > 4096)
            {
                Console.WriteLine($"Skipping {fileName}: Image too large ({tempImage.Width}x{tempImage.Height})");
                return null;
            }

            // Reset stream position for compression
            imageStream.Position = 0;

            // Compress to LRLE with cancellation support
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30s timeout
            using var lrleResource = await _factory.CreateResourceAsync(1, imageStream, cts.Token);

            // Get compressed data
            var compressedData = lrleResource.GetRawData();
            var compressedSize = compressedData.Length;

            // Save to file
            await File.WriteAllBytesAsync(outputPath, compressedData.ToArray(), cts.Token);

            stopwatch.Stop();

            return new ProcessingResult
            {
                FileName = fileName,
                OriginalSize = originalSize,
                CompressedSize = compressedSize,
                ProcessingTime = stopwatch.Elapsed,
                Width = lrleResource.Width,
                Height = lrleResource.Height,
                MipLevels = (byte)lrleResource.MipCount,
                Success = true
            };
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Timeout processing {fileName}");
            return new ProcessingResult { FileName = fileName, Success = false, ErrorMessage = "Timeout" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing {fileName}: {ex.Message}");
            return new ProcessingResult { FileName = fileName, Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Demonstrates quality comparison between original and decompressed images
    /// </summary>
    public async Task QualityComparisonAsync(string imagePath)
    {
        Console.WriteLine("\n=== Quality Comparison Example ===");

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Creating test image: {imagePath}");
            await CreateDetailedTestImageAsync(imagePath);
        }

        // Load original image
        using var originalImage = await Image.LoadAsync<Rgba32>(imagePath);
        Console.WriteLine($"Original image: {originalImage.Width}x{originalImage.Height}");

        // Compress to LRLE
        using var imageStream = File.OpenRead(imagePath);
        using var lrleResource = await _factory.CreateResourceAsync(1, imageStream);

        // Decompress back to image
        using var decompressedStream = await lrleResource.ToBitmapAsync();
        using var decompressedImage = await Image.LoadAsync<Rgba32>(decompressedStream);

        // Compare dimensions
        bool dimensionsMatch = originalImage.Width == decompressedImage.Width &&
                              originalImage.Height == decompressedImage.Height;
        Console.WriteLine($"Dimensions preserved: {dimensionsMatch}");

        // Pixel-level comparison (sample pixels)
        var samplePoints = GenerateSamplePoints(originalImage.Width, originalImage.Height, 100);
        int matchingPixels = 0;

        foreach (var (x, y) in samplePoints)
        {
            var originalPixel = originalImage[x, y];
            var decompressedPixel = decompressedImage[x, y];

            if (ColorsMatch(originalPixel, decompressedPixel))
            {
                matchingPixels++;
            }
        }

        double pixelAccuracy = (double)matchingPixels / samplePoints.Count;
        Console.WriteLine($"Pixel accuracy: {pixelAccuracy:P2} ({matchingPixels}/{samplePoints.Count} samples)");

        // Save comparison images
        string baseName = Path.GetFileNameWithoutExtension(imagePath);
        await decompressedImage.SaveAsPngAsync($"{baseName}_decompressed.png");

        // Create difference image
        await CreateDifferenceImageAsync(originalImage, decompressedImage, $"{baseName}_difference.png");
    }

    /// <summary>
    /// Demonstrates performance benchmarking of LRLE operations
    /// </summary>
    public async Task PerformanceBenchmarkAsync()
    {
        Console.WriteLine("\n=== Performance Benchmark Example ===");

        var imageSizes = new[] { (64, 64), (128, 128), (256, 256), (512, 512) };

        foreach (var (width, height) in imageSizes)
        {
            Console.WriteLine($"\nBenchmarking {width}x{height} images:");

            // Create test images with different characteristics
            var testImages = new[]
            {
                ("solid_colors", await CreateSolidColorImageAsync(width, height)),
                ("gradient", await CreateGradientImageAsync(width, height)),
                ("noise", await CreateNoiseImageAsync(width, height))
            };

            foreach (var (name, imageData) in testImages)
            {
                await BenchmarkImageAsync(name, imageData, width, height);
            }
        }
    }

    /// <summary>
    /// Benchmarks compression and decompression of a single image
    /// </summary>
    private async Task BenchmarkImageAsync(string imageName, byte[] imageData, int width, int height)
    {
        const int iterations = 10;
        var compressionTimes = new List<TimeSpan>();
        var decompressionTimes = new List<TimeSpan>();
        long compressedSize = 0;

        for (int i = 0; i < iterations; i++)
        {
            // Benchmark compression
            using var imageStream = new MemoryStream(imageData);
            var compressionStopwatch = Stopwatch.StartNew();

            using var lrleResource = await _factory.CreateResourceAsync(1, imageStream);
            compressionStopwatch.Stop();
            compressionTimes.Add(compressionStopwatch.Elapsed);

            if (i == 0) compressedSize = lrleResource.GetRawData().Length;

            // Benchmark decompression
            var decompressionStopwatch = Stopwatch.StartNew();
            using var decompressedStream = await lrleResource.ToBitmapAsync();
            decompressionStopwatch.Stop();
            decompressionTimes.Add(decompressionStopwatch.Elapsed);
        }

        // Calculate averages
        var avgCompression = TimeSpan.FromTicks((long)compressionTimes.Average(t => t.Ticks));
        var avgDecompression = TimeSpan.FromTicks((long)decompressionTimes.Average(t => t.Ticks));
        var originalSize = imageData.Length;
        var ratio = (double)originalSize / compressedSize;

        Console.WriteLine($"  {imageName}: {avgCompression.TotalMilliseconds:F1}ms compress, " +
                         $"{avgDecompression.TotalMilliseconds:F1}ms decompress, " +
                         $"{ratio:F2}x compression");
    }

    // Helper methods for creating test images and processing results

    private async Task CreateSampleBatchAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        Console.WriteLine($"Creating sample images in {directory}...");

        var tasks = new[]
        {
            CreateSolidColorImageFileAsync(Path.Combine(directory, "solid_red.png"), 128, 128, new Rgba32(255, 0, 0, 255)),
            CreateSolidColorImageFileAsync(Path.Combine(directory, "solid_green.png"), 128, 128, new Rgba32(0, 255, 0, 255)),
            CreateSolidColorImageFileAsync(Path.Combine(directory, "solid_blue.png"), 128, 128, new Rgba32(0, 0, 255, 255)),
            CreateGradientImageFileAsync(Path.Combine(directory, "gradient.png"), 256, 256),
            CreateDetailedTestImageAsync(Path.Combine(directory, "detailed.png"))
        };

        await Task.WhenAll(tasks);
        Console.WriteLine("Sample images created.");
    }

    private async Task CreateSolidColorImageFileAsync(string filePath, int width, int height, Rgba32 color)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                row.Fill(color);
            }
        });
        await image.SaveAsPngAsync(filePath);
    }

    private async Task CreateGradientImageFileAsync(string filePath, int width, int height)
    {
        var imageData = await CreateGradientImageAsync(width, height);
        await File.WriteAllBytesAsync(filePath, imageData);
    }

    private async Task<byte[]> CreateSolidColorImageAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            var colors = new[] { new Rgba32(255, 0, 0, 255), new Rgba32(0, 255, 0, 255), new Rgba32(0, 0, 255, 255) };
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var color = colors[y / (height / 3) % 3];
                row.Fill(color);
            }
        });

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        return stream.ToArray();
    }

    private async Task<byte[]> CreateGradientImageAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var r = (byte)(x * 255 / width);
                var g = (byte)(y * 255 / height);
                var b = (byte)((x + y) * 255 / (width + height));
                image[x, y] = new Rgba32(r, g, b, 255);
            }
        }

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        return stream.ToArray();
    }

    private async Task<byte[]> CreateNoiseImageAsync(int width, int height)
    {
        var random = new Random(42); // Fixed seed for consistent benchmarks
        using var image = new Image<Rgba32>(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var r = (byte)random.Next(256);
                var g = (byte)random.Next(256);
                var b = (byte)random.Next(256);
                image[x, y] = new Rgba32(r, g, b, 255);
            }
        }

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        return stream.ToArray();
    }

    private async Task CreateDetailedTestImageAsync(string filePath)
    {
        const int size = 256;
        using var image = new Image<Rgba32>(size, size);

        // Create a detailed test pattern
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Checkerboard pattern with gradients
                bool isCheckerSquare = ((x / 32) + (y / 32)) % 2 == 0;

                if (isCheckerSquare)
                {
                    // Solid color squares
                    var r = (byte)((x / 32) * 40 % 256);
                    var g = (byte)((y / 32) * 40 % 256);
                    image[x, y] = new Rgba32(r, g, 128, 255);
                }
                else
                {
                    // Gradient squares
                    var r = (byte)(x * 255 / size);
                    var g = (byte)(y * 255 / size);
                    image[x, y] = new Rgba32(r, g, 0, 255);
                }
            }
        }

        await image.SaveAsPngAsync(filePath);
    }

    private void DisplayBatchResults(ConcurrentBag<ProcessingResult> results, TimeSpan totalTime)
    {
        var successful = results.Where(r => r.Success).ToList();
        var failed = results.Where(r => !r.Success).ToList();

        Console.WriteLine($"\n=== Batch Processing Results ===");
        Console.WriteLine($"Total time: {totalTime.TotalSeconds:F2} seconds");
        Console.WriteLine($"Successful: {successful.Count}");
        Console.WriteLine($"Failed: {failed.Count}");

        if (successful.Any())
        {
            var totalOriginalSize = successful.Sum(r => r.OriginalSize);
            var totalCompressedSize = successful.Sum(r => r.CompressedSize);
            var avgCompressionRatio = (double)totalOriginalSize / totalCompressedSize;
            var avgProcessingTime = successful.Average(r => r.ProcessingTime.TotalMilliseconds);

            Console.WriteLine($"Total original size: {totalOriginalSize:N0} bytes");
            Console.WriteLine($"Total compressed size: {totalCompressedSize:N0} bytes");
            Console.WriteLine($"Average compression ratio: {avgCompressionRatio:F2}x");
            Console.WriteLine($"Average processing time: {avgProcessingTime:F1}ms per image");
            Console.WriteLine($"Space saved: {((double)(totalOriginalSize - totalCompressedSize) / totalOriginalSize):P1}");
        }

        if (failed.Any())
        {
            Console.WriteLine("\nFailed files:");
            foreach (var failure in failed)
            {
                Console.WriteLine($"  {failure.FileName}: {failure.ErrorMessage}");
            }
        }
    }

    private List<(int x, int y)> GenerateSamplePoints(int width, int height, int count)
    {
        var random = new Random(42); // Fixed seed for consistent results
        var points = new List<(int, int)>();

        for (int i = 0; i < count; i++)
        {
            var x = random.Next(width);
            var y = random.Next(height);
            points.Add((x, y));
        }

        return points;
    }

    private bool ColorsMatch(Rgba32 color1, Rgba32 color2, byte threshold = 2)
    {
        return Math.Abs(color1.R - color2.R) <= threshold &&
               Math.Abs(color1.G - color2.G) <= threshold &&
               Math.Abs(color1.B - color2.B) <= threshold &&
               Math.Abs(color1.A - color2.A) <= threshold;
    }

    private async Task CreateDifferenceImageAsync(Image<Rgba32> original, Image<Rgba32> decompressed, string outputPath)
    {
        using var diffImage = new Image<Rgba32>(original.Width, original.Height);

        for (int y = 0; y < original.Height; y++)
        {
            for (int x = 0; x < original.Width; x++)
            {
                var originalPixel = original[x, y];
                var decompressedPixel = decompressed[x, y];

                // Calculate absolute difference
                var rDiff = Math.Abs(originalPixel.R - decompressedPixel.R);
                var gDiff = Math.Abs(originalPixel.G - decompressedPixel.G);
                var bDiff = Math.Abs(originalPixel.B - decompressedPixel.B);

                // Amplify differences for visibility
                var maxDiff = Math.Max(Math.Max(rDiff, gDiff), bDiff);
                var amplifiedDiff = (byte)Math.Min(255, maxDiff * 10);

                diffImage[x, y] = new Rgba32(amplifiedDiff, amplifiedDiff, amplifiedDiff, 255);
            }
        }

        await diffImage.SaveAsPngAsync(outputPath);
    }

    /// <summary>
    /// Runs all advanced LRLE examples
    /// </summary>
    public async Task RunBatchProcessingAsync()
    {
        try
        {
            // Create directories for examples
            Directory.CreateDirectory("input");
            Directory.CreateDirectory("output");

            await BatchProcessImagesAsync("input", "output");
            await QualityComparisonAsync("test_image.png");
            await PerformanceBenchmarkAsync();

            Console.WriteLine("\n=== Advanced examples completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// Represents the result of processing a single image
    /// </summary>
    private record ProcessingResult
    {
        public string FileName { get; init; } = "";
        public long OriginalSize { get; init; }
        public long CompressedSize { get; init; }
        public TimeSpan ProcessingTime { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public byte MipLevels { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
