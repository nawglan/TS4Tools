# LRLE Resource Examples

## Emoji Legend

**Status Icons:**

- ✅ Best Practice/Recommended Approach
- ❌ Common Mistake/Avoid This
- ⚠️ Important Warning/Caution

This directory contains comprehensive examples demonstrating how to use the TS4Tools LRLE (Lossless Run-Length Encoded)
image resource system.

## Examples Overview

### 1. BasicLRLEExample.cs

**Purpose**: Demonstrates fundamental LRLE operations\
**What you'll learn**:

- How to compress PNG images to LRLE format
- How to decompress LRLE back to PNG
- Basic resource properties and metadata
- File I/O operations with LRLE resources

**Run**: `dotnet run --project BasicLRLEExample`

### 2. AdvancedLRLEExample.cs

**Purpose**: Shows advanced usage patterns and optimization techniques\
**What you'll learn**:

- Batch processing multiple images in parallel
- Error handling and validation
- Performance benchmarking and profiling
- Quality comparison between original and compressed images
- Memory management best practices

**Run**: `dotnet run --project AdvancedLRLEExample`

### 3. WebApiExample.cs

**Purpose**: Integration with ASP.NET Core web applications\
**What you'll learn**:

- RESTful API endpoints for LRLE processing
- Dependency injection setup
- File upload handling
- Async processing with cancellation tokens
- Batch processing in web contexts

**Setup**: Add to your ASP.NET Core project and register services

## Quick Start

### Prerequisites

- .NET 9.0 or later
- SixLabors.ImageSharp package
- TS4Tools.Resources.Images package

### Running the Basic Example

```bash
# Clone the repository
git clone https://github.com/nawglan/TS4Tools.git
cd TS4Tools/examples/LRLE

# Restore packages
dotnet restore

# Run basic example
dotnet run BasicLRLEExample.cs

# Run advanced examples
dotnet run AdvancedLRLEExample.cs
```

### Expected Output

The basic example will:

1. Create a sample 128x128 test image with solid color blocks
2. Compress it to LRLE format (typically 70-80% size reduction)
3. Decompress back to PNG
4. Extract all mip levels as separate images
5. Display compression statistics

```
=== Basic LRLE Compression Example ===
Creating sample image: sample_texture.png
Original PNG size: 12,547 bytes
Compressed LRLE size: 2,891 bytes
Compression ratio: 4.34x
Space saved: 77.0%
Saved compressed LRLE: sample_texture.lrle
Image dimensions: 128x128
LRLE version: Version2
Mip levels: 8

=== Basic LRLE Decompression Example ===
Decompressed image dimensions: 128x128
Saved decompressed PNG: decompressed_texture.png
Mip level 1: 64x64 -> decompressed_texture_mip1.png
Mip level 2: 32x32 -> decompressed_texture_mip2.png
...
```

## Understanding LRLE Compression

### When LRLE Works Best

LRLE compression is most effective with images that have:

- **Limited color palettes** (< 256 unique colors)
- **Solid color regions** or simple gradients
- **Repeating patterns** or textures
- **UI elements** like buttons, icons, interface graphics

### Compression Ratios by Image Type

| Image Type | Typical Compression | Best Case | Worst Case |
|------------|-------------------|-----------|------------| | Solid colors | 10-50x | 100x+ | 3x | | Simple gradients |
3-8x | 15x | 2x | | Complex photos | 1.2-2x | 3x | 0.8x | | UI graphics | 5-15x | 30x | 2x |

### Version Differences

- **Version 1**: Basic run-length encoding, no color palette
- **Version 2**: Advanced encoding with color palette support (recommended)

## Code Patterns and Best Practices

### ✅ Proper Resource Management

```csharp
// Good: Using statements ensure proper disposal
using var factory = new LRLEResourceFactory();
using var imageStream = File.OpenRead("image.png");
using var lrleResource = await factory.CreateResourceAsync(1, imageStream);

// Process the resource...
var data = lrleResource.GetRawData();
```

### ✅ Async Operations with Cancellation

```csharp
public async Task ProcessImageAsync(string imagePath, CancellationToken cancellationToken)
{
    var factory = new LRLEResourceFactory();
    
    using var imageStream = File.OpenRead(imagePath);
    using var lrleResource = await factory.CreateResourceAsync(1, imageStream, cancellationToken);
    
    // Long-running operation with cancellation support
    using var bitmapStream = await lrleResource.ToBitmapAsync(0, cancellationToken);
    
    // Save result
    using var outputImage = await Image.LoadAsync<Rgba32>(bitmapStream, cancellationToken);
    await outputImage.SaveAsPngAsync("output.png", cancellationToken);
}
```

### ✅ Error Handling

```csharp
try
{
    using var lrleResource = await factory.CreateResourceAsync(1, stream);
    // Process successfully...
}
catch (InvalidDataException ex)
{
    // Handle corrupted LRLE data
    Console.WriteLine($"Invalid LRLE format: {ex.Message}");
}
catch (ArgumentException ex)
{
    // Handle invalid parameters
    Console.WriteLine($"Invalid arguments: {ex.Message}");
}
catch (OperationCanceledException)
{
    // Handle cancellation
    Console.WriteLine("Operation was cancelled");
}
```

### ❌ Common Mistakes to Avoid

```csharp
// Don't: Forget to dispose resources
var resource = await factory.CreateResourceAsync(1, stream); // Memory leak!

// Don't: Block async operations
var resource = factory.CreateResourceAsync(1, stream).Result; // Can deadlock!

// Don't: Ignore cancellation tokens
await ProcessLongRunningOperation(); // Should accept CancellationToken

// Don't: Process images that are too large without validation
// Should check image dimensions before processing
```

## Integration Examples

### Console Application

```csharp
// Program.cs
using TS4Tools.Resources.Images;

var factory = new LRLEResourceFactory();

if (args.Length < 2)
{
    Console.WriteLine("Usage: app <input.png> <output.lrle>");
    return;
}

using var input = File.OpenRead(args[0]);
using var resource = await factory.CreateResourceAsync(1, input);

await File.WriteAllBytesAsync(args[1], resource.GetRawData().ToArray());
Console.WriteLine($"Compressed {args[0]} -> {args[1]}");
```

### ASP.NET Core Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register LRLE services
builder.Services.AddSingleton<ILRLEResourceFactory, LRLEResourceFactory>();

var app = builder.Build();

// Minimal API endpoint
app.MapPost("/compress", async (IFormFile file, ILRLEResourceFactory factory) =>
{
    using var stream = file.OpenReadStream();
    using var resource = await factory.CreateResourceAsync(1, stream);
    
    return Results.Json(new
    {
        OriginalSize = file.Length,
        CompressedSize = resource.GetRawData().Length,
        Width = resource.Width,
        Height = resource.Height
    });
});

app.Run();
```

### Dependency Injection with Services

```csharp
// Services/TextureService.cs
public class TextureService
{
    private readonly ILRLEResourceFactory _factory;
    
    public TextureService(ILRLEResourceFactory factory)
    {
        _factory = factory;
    }
    
    public async Task<CompressedTexture> CompressTextureAsync(Stream imageStream)
    {
        using var resource = await _factory.CreateResourceAsync(1, imageStream);
        
        return new CompressedTexture
        {
            Data = resource.GetRawData().ToArray(),
            Width = resource.Width,
            Height = resource.Height,
            MipLevels = resource.MipMapCount
        };
    }
}

// Program.cs registration
services.AddScoped<TextureService>();
services.AddSingleton<ILRLEResourceFactory, LRLEResourceFactory>();
```

## Performance Guidelines

### Memory Usage

- Use `using` statements to ensure prompt disposal
- Process large batches with `SemaphoreSlim` to limit concurrency
- Consider streaming for very large images

### Processing Speed

- LRLE compression: ~1-5ms per 256x256 image
- LRLE decompression: ~0.5-2ms per 256x256 image
- Mipmap generation adds ~20-30% to processing time

### Optimization Tips

1. **Validate inputs** before processing to fail fast
2. **Use appropriate mip levels** for your use case
3. **Batch similar-sized images** together for better cache usage
4. **Consider async parallel processing** for multiple images

## Testing Your Implementation

### Unit Test Example

```csharp
[Test]
public async Task Should_Preserve_Image_Dimensions()
{
    // Arrange
    var testImage = CreateTestImage(128, 128);
    var factory = new LRLEResourceFactory();
    
    // Act
    using var imageStream = new MemoryStream(testImage);
    using var lrleResource = await factory.CreateResourceAsync(1, imageStream);
    
    // Assert
    Assert.AreEqual(128, lrleResource.Width);
    Assert.AreEqual(128, lrleResource.Height);
    Assert.IsTrue(lrleResource.MipMapCount > 0);
}
```

### Integration Test Example

```csharp
[Test]
public async Task Should_Round_Trip_Successfully()
{
    // Arrange
    var originalImage = await Image.LoadAsync<Rgba32>("test_image.png");
    var factory = new LRLEResourceFactory();
    
    // Act - Compress
    using var pngStream = new MemoryStream();
    await originalImage.SaveAsPngAsync(pngStream);
    pngStream.Position = 0;
    
    using var lrleResource = await factory.CreateResourceAsync(1, pngStream);
    
    // Act - Decompress  
    using var decompressedStream = await lrleResource.ToBitmapAsync();
    using var decompressedImage = await Image.LoadAsync<Rgba32>(decompressedStream);
    
    // Assert
    Assert.AreEqual(originalImage.Width, decompressedImage.Width);
    Assert.AreEqual(originalImage.Height, decompressedImage.Height);
    
    // Verify pixel accuracy (sample-based)
    AssertImagesAreSimilar(originalImage, decompressedImage, threshold: 0.99);
}
```

## Troubleshooting

### Common Issues

**Problem**: "Invalid LRLE magic number"\
**Solution**: Ensure the input file is actually LRLE format. Use `ValidateData()` to check.

**Problem**: Out of memory with large images\
**Solution**: Implement size limits and consider resizing before compression.

**Problem**: Poor compression ratios\
**Solution**: LRLE works best with images that have limited colors. Consider color quantization.

**Problem**: Slow processing\
**Solution**: Use parallel processing with `SemaphoreSlim` and async patterns.

### Debug Information

Enable detailed logging to see what's happening:

```csharp
// Add logging to see internal operations
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});
```

### File Format Validation

Use the factory's validation method before processing:

```csharp
var factory = new LRLEResourceFactory();
using var stream = File.OpenRead("suspicious_file.lrle");

if (!factory.ValidateData(stream))
{
    Console.WriteLine("File is not valid LRLE format");
    return;
}

// Safe to process...
```

## Further Reading

- [LRLE Implementation Review](../LRLE_IMPLEMENTATION_REVIEW.md)
- [LRLE Usage Guide](../LRLE_USAGE_GUIDE.md)
- [TS4Tools Documentation](../../README.md)
- [SixLabors.ImageSharp Documentation](https://docs.sixlabors.com/api/ImageSharp/)

## Contributing

Found an issue or want to improve these examples? Please:

1. Fork the repository
2. Create a feature branch
3. Add tests for your changes
4. Submit a pull request

## License

These examples are part of the TS4Tools project and are licensed under the same terms as the main project.
