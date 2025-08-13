// Example: Basic LRLE Compression and Decompression
// Demonstrates the fundamental operations of the LRLE resource system

using TS4Tools.Resources.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TS4Tools.Examples.LRLE;

public class BasicLRLEExample
{
    /// <summary>
    /// Demonstrates basic compression of a PNG image to LRLE format
    /// </summary>
    public async Task CompressPngToLrleAsync()
    {
        Console.WriteLine("=== Basic LRLE Compression Example ===");

        // Create a factory for LRLE resources
        var factory = new LRLEResourceFactory();

        // Load a PNG image from file
        string inputPath = "sample_texture.png";
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Creating sample image: {inputPath}");
            await CreateSampleImageAsync(inputPath);
        }

        // Compress PNG to LRLE
        using var imageStream = File.OpenRead(inputPath);
        using var lrleResource = await factory.CreateResourceAsync(1, imageStream);

        // Display compression results
        var originalSize = new FileInfo(inputPath).Length;
        var compressedData = lrleResource.GetRawData();
        var compressedSize = compressedData.Length;

        Console.WriteLine($"Original PNG size: {originalSize:N0} bytes");
        Console.WriteLine($"Compressed LRLE size: {compressedSize:N0} bytes");
        Console.WriteLine($"Compression ratio: {(double)originalSize / compressedSize:F2}x");
        Console.WriteLine($"Space saved: {((double)(originalSize - compressedSize) / originalSize):P1}");

        // Save compressed LRLE file
        string outputPath = "sample_texture.lrle";
        await File.WriteAllBytesAsync(outputPath, compressedData.ToArray());
        Console.WriteLine($"Saved compressed LRLE: {outputPath}");

        // Verify resource properties
        Console.WriteLine($"Image dimensions: {lrleResource.Width}x{lrleResource.Height}");
        Console.WriteLine($"LRLE version: {lrleResource.Version}");
        Console.WriteLine($"Mip levels: {lrleResource.MipMapCount}");
    }

    /// <summary>
    /// Demonstrates decompression of LRLE back to PNG
    /// </summary>
    public async Task DecompressLrleToPngAsync()
    {
        Console.WriteLine("\n=== Basic LRLE Decompression Example ===");

        var factory = new LRLEResourceFactory();
        string lrlePath = "sample_texture.lrle";

        if (!File.Exists(lrlePath))
        {
            Console.WriteLine($"LRLE file not found: {lrlePath}");
            Console.WriteLine("Run CompressPngToLrleAsync() first to create the file.");
            return;
        }

        // Load LRLE resource
        using var lrleStream = File.OpenRead(lrlePath);
        using var lrleResource = await factory.CreateResourceAsync(1, lrleStream);

        // Extract main image (mip level 0)
        using var bitmapStream = await lrleResource.ToBitmapAsync(0);
        using var image = await Image.LoadAsync<Rgba32>(bitmapStream);

        // Save as PNG
        string outputPath = "decompressed_texture.png";
        await image.SaveAsPngAsync(outputPath);

        Console.WriteLine($"Decompressed image dimensions: {image.Width}x{image.Height}");
        Console.WriteLine($"Saved decompressed PNG: {outputPath}");

        // Extract additional mip levels if available
        for (int mipLevel = 1; mipLevel < lrleResource.MipMapCount; mipLevel++)
        {
            using var mipStream = await lrleResource.ToBitmapAsync(mipLevel);
            using var mipImage = await Image.LoadAsync<Rgba32>(mipStream);

            string mipPath = $"decompressed_texture_mip{mipLevel}.png";
            await mipImage.SaveAsPngAsync(mipPath);

            Console.WriteLine($"Mip level {mipLevel}: {mipImage.Width}x{mipImage.Height} -> {mipPath}");
        }
    }

    /// <summary>
    /// Creates a sample image with various colors and patterns
    /// </summary>
    private async Task CreateSampleImageAsync(string filePath)
    {
        const int width = 128;
        const int height = 128;

        using var image = new Image<Rgba32>(width, height);

        // Create a gradient pattern with limited colors (good for LRLE compression)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Create a pattern that compresses well with run-length encoding
                byte r, g, b;

                if (x < width / 4)
                {
                    // Solid red block
                    r = 255; g = 0; b = 0;
                }
                else if (x < width / 2)
                {
                    // Solid green block
                    r = 0; g = 255; b = 0;
                }
                else if (x < 3 * width / 4)
                {
                    // Solid blue block
                    r = 0; g = 0; b = 255;
                }
                else
                {
                    // Gradient in the last quarter
                    r = (byte)(y * 255 / height);
                    g = (byte)(x * 255 / width);
                    b = 128;
                }

                image[x, y] = new Rgba32(r, g, b, 255);
            }
        }

        await image.SaveAsPngAsync(filePath);
    }

    /// <summary>
    /// Main entry point for the basic example
    /// </summary>
    public static async Task Main(string[] args)
    {
        var example = new BasicLRLEExample();

        try
        {
            await example.CompressPngToLrleAsync();
            await example.DecompressLrleToPngAsync();

            Console.WriteLine("\n=== Example completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
