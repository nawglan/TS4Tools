# LRLE Resource Usage Guide

## Overview

The LRLE (Lossless Run-Length Encoded) resource format provides efficient compression for images with limited color palettes, commonly used in The Sims 4 texture resources.

## Quick Start

### Basic Usage

```csharp
using TS4Tools.Resources.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// Create LRLE resource from PNG image
var factory = new LRLEResourceFactory();
using var imageStream = File.OpenRead("texture.png");
var lrleResource = await factory.CreateResourceAsync(1, imageStream);

// Access image properties
Console.WriteLine($"Dimensions: {lrleResource.Width}x{lrleResource.Height}");
Console.WriteLine($"Mip Levels: {lrleResource.MipMapCount}");
Console.WriteLine($"Format Version: {lrleResource.Version}");

// Convert back to bitmap
using var bitmapStream = await lrleResource.ToBitmapAsync();
using var image = await Image.LoadAsync<Rgba32>(bitmapStream);
await image.SaveAsPngAsync("output.png");
```

### Working with Existing LRLE Files

```csharp
// Load existing LRLE file
using var lrleStream = File.OpenRead("existing.lrle");
var lrleResource = await factory.CreateResourceAsync(1, lrleStream);

// Extract specific mip level
using var mipStream = await lrleResource.ToBitmapAsync(mipLevel: 2);

// Get raw LRLE data
var rawData = lrleResource.GetRawData();
Console.WriteLine($"Compressed size: {rawData.Length} bytes");
```

## Advanced Usage

### Custom Image Compression

```csharp
public class TextureProcessor
{
    private readonly LRLEResourceFactory _factory;
    
    public TextureProcessor()
    {
        _factory = new LRLEResourceFactory();
    }
    
    public async Task<ILRLEResource> CompressTextureAsync(
        Stream imageStream, 
        bool generateMipmaps = true,
        CancellationToken cancellationToken = default)
    {
        var resource = new LRLEResource();
        await resource.CreateFromImageAsync(imageStream, generateMipmaps, cancellationToken);
        return resource;
    }
    
    public async Task<Stream> DecompressToStreamAsync(
        ILRLEResource lrleResource,
        int mipLevel = 0,
        CancellationToken cancellationToken = default)
    {
        return await lrleResource.ToBitmapAsync(mipLevel, cancellationToken);
    }
}
```

### Batch Processing

```csharp
public async Task ProcessTexturesAsync(string inputDir, string outputDir)
{
    var factory = new LRLEResourceFactory();
    var pngFiles = Directory.GetFiles(inputDir, "*.png");
    
    await Parallel.ForEachAsync(pngFiles, async (pngFile, ct) =>
    {
        var fileName = Path.GetFileNameWithoutExtension(pngFile);
        
        try
        {
            // Compress to LRLE
            using var imageStream = File.OpenRead(pngFile);
            var lrleResource = await factory.CreateResourceAsync(1, imageStream, ct);
            
            // Save compressed data
            var lrleData = lrleResource.GetRawData();
            await File.WriteAllBytesAsync(
                Path.Combine(outputDir, $"{fileName}.lrle"), 
                lrleData.ToArray(), 
                ct);
                
            Console.WriteLine($"Compressed {fileName}: {new FileInfo(pngFile).Length} -> {lrleData.Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process {fileName}: {ex.Message}");
        }
    });
}
```

## Error Handling

### Validation and Error Recovery

```csharp
public async Task<ILRLEResource?> SafeLoadAsync(string filePath)
{
    var factory = new LRLEResourceFactory();
    
    try
    {
        // Validate file exists and has data
        if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
        {
            return null;
        }
        
        using var stream = File.OpenRead(filePath);
        
        // Factory validates the data format
        if (!factory.ValidateData(stream))
        {
            Console.WriteLine($"Invalid LRLE format: {filePath}");
            return null;
        }
        
        return await factory.CreateResourceAsync(1, stream);
    }
    catch (InvalidDataException ex)
    {
        Console.WriteLine($"Corrupted LRLE file {filePath}: {ex.Message}");
        return null;
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Invalid parameters for {filePath}: {ex.Message}");
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error loading {filePath}: {ex.Message}");
        return null;
    }
}
```

## Performance Considerations

### Memory Management

```csharp
// ✅ Good: Using 'using' statements for automatic disposal
public async Task ProcessLargeImageAsync(string imagePath)
{
    using var imageStream = File.OpenRead(imagePath);
    using var lrleResource = await new LRLEResourceFactory()
        .CreateResourceAsync(1, imageStream);
    
    // Process the resource...
    var dimensions = (lrleResource.Width, lrleResource.Height);
    
    // Resources automatically disposed when leaving scope
}

// ❌ Bad: Not disposing resources
public async Task ProcessImageWrong(string imagePath)
{
    var imageStream = File.OpenRead(imagePath); // Never disposed!
    var lrleResource = await new LRLEResourceFactory()
        .CreateResourceAsync(1, imageStream); // Never disposed!
    
    // Memory leaks...
}
```

### Async Best Practices

```csharp
public async Task ProcessMultipleImagesAsync(IEnumerable<string> imagePaths)
{
    var factory = new LRLEResourceFactory();
    var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    
    var tasks = imagePaths.Select(async path =>
    {
        await semaphore.WaitAsync();
        try
        {
            using var stream = File.OpenRead(path);
            using var resource = await factory.CreateResourceAsync(1, stream);
            
            // Process resource...
            return resource.Width * resource.Height; // Return some metric
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    var results = await Task.WhenAll(tasks);
    Console.WriteLine($"Processed {results.Length} images");
}
```

## Integration with Dependency Injection

### ASP.NET Core Setup

```csharp
// Program.cs
builder.Services.AddSingleton<ILRLEResourceFactory, LRLEResourceFactory>();
builder.Services.AddScoped<ITextureService, TextureService>();

// TextureService.cs
public class TextureService : ITextureService
{
    private readonly ILRLEResourceFactory _lrleFactory;
    
    public TextureService(ILRLEResourceFactory lrleFactory)
    {
        _lrleFactory = lrleFactory;
    }
    
    public async Task<ILRLEResource> ProcessUploadedImageAsync(
        IFormFile imageFile,
        CancellationToken cancellationToken)
    {
        using var stream = imageFile.OpenReadStream();
        return await _lrleFactory.CreateResourceAsync(1, stream, cancellationToken);
    }
}
```

## Testing Support

### Unit Testing with Mock Data

```csharp
[Test]
public async Task Should_Compress_And_Decompress_Successfully()
{
    // Arrange
    var testImageData = CreateTestPngData(64, 64);
    using var imageStream = new MemoryStream(testImageData);
    
    var factory = new LRLEResourceFactory();
    
    // Act - Compress
    using var lrleResource = await factory.CreateResourceAsync(1, imageStream);
    
    // Assert - Verify properties
    Assert.AreEqual(64, lrleResource.Width);
    Assert.AreEqual(64, lrleResource.Height);
    Assert.IsTrue(lrleResource.MipMapCount > 0);
    
    // Act - Decompress
    using var decompressedStream = await lrleResource.ToBitmapAsync();
    using var decompressedImage = await Image.LoadAsync<Rgba32>(decompressedStream);
    
    // Assert - Verify round trip
    Assert.AreEqual(64, decompressedImage.Width);
    Assert.AreEqual(64, decompressedImage.Height);
}

private static byte[] CreateTestPngData(int width, int height)
{
    using var image = new Image<Rgba32>(width, height);
    
    // Fill with test pattern
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            var color = new Rgba32(
                (byte)(x * 255 / width),
                (byte)(y * 255 / height),
                128, 255);
            image[x, y] = color;
        }
    }
    
    using var stream = new MemoryStream();
    image.SaveAsPng(stream);
    return stream.ToArray();
}
```

## Common Issues and Solutions

### Issue: "Invalid LRLE magic number"

```csharp
// Check if file is actually LRLE format
var factory = new LRLEResourceFactory();
using var stream = File.OpenRead("suspicious_file.lrle");

if (!factory.ValidateData(stream))
{
    Console.WriteLine("File is not valid LRLE format");
    // Try alternative format detection...
}
```

### Issue: Memory usage with large images

```csharp
// Use streaming approach for large images
public async Task ProcessLargeImageAsync(string imagePath)
{
    const int MaxImageSize = 2048 * 2048 * 4; // 16MB threshold
    
    var fileInfo = new FileInfo(imagePath);
    if (fileInfo.Length > MaxImageSize)
    {
        // Process in chunks or resize first
        using var image = await Image.LoadAsync<Rgba32>(imagePath);
        if (image.Width > 2048 || image.Height > 2048)
        {
            // Resize before compressing
            image.Mutate(x => x.Resize(2048, 2048));
        }
        
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        
        var factory = new LRLEResourceFactory();
        using var lrleResource = await factory.CreateResourceAsync(1, stream);
        
        // Continue processing...
    }
}
```

### Issue: Cross-platform compatibility

```csharp
// Ensure proper path handling across platforms
public string GetTextureOutputPath(string baseDir, string textureName)
{
    // ✅ Good: Use Path.Combine for cross-platform compatibility
    return Path.Combine(baseDir, "textures", $"{textureName}.lrle");
    
    // ❌ Bad: Hardcoded path separators
    // return baseDir + "\\textures\\" + textureName + ".lrle";
}
```

## API Reference Summary

### Core Classes

- **`LRLEResource`**: Main implementation of LRLE compression/decompression
- **`LRLEResourceFactory`**: Factory for creating LRLE resources with validation
- **`LRLEColorTable`**: Manages color palettes for efficient compression

### Key Interfaces

- **`ILRLEResource`**: Primary interface for LRLE operations
- **`ILRLEResourceFactory`**: Factory interface for dependency injection

### Extension Points

The implementation supports custom color quantization algorithms and compression strategies through the extensible architecture.
