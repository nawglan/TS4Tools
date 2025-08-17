using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Extensions;
using TS4Tools.Extensions.ResourceTypes;

namespace PackageCreator;

/// <summary>
/// Example demonstrating how to create a new Sims 4 package file with custom resources.
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        // Set up dependency injection and logging
        var builder = Host.CreateApplicationBuilder(args);

        // Add TS4Tools services
        builder.Services.AddTS4ToolsCore(builder.Configuration);

        // Add console logging
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: PackageCreator <output-package-path>");
                Console.WriteLine("Example: PackageCreator C:\\path\\to\\output\\new-package.package");
                return;
            }

            var outputPath = args[0];

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                logger.LogInformation("Created output directory: {Directory}", outputDir);
            }

            logger.LogInformation("Creating new package: {OutputPath}", outputPath);

            // Get the package factory service
            var packageFactory = host.Services.GetRequiredService<IPackageFactory>();
            var resourceTypeRegistry = host.Services.GetRequiredService<IResourceTypeRegistry>();

            // Create a new empty package
            using var package = await packageFactory.CreateEmptyPackageAsync();

            Console.WriteLine("=== Creating New Package ===");
            Console.WriteLine($"Output path: {outputPath}");
            Console.WriteLine();

            // Add some sample resources
            AddSampleTextResource(package, logger);
            AddSampleBinaryResource(package, logger);
            AddSampleMetadataResource(package, logger);

            // Display package statistics before saving
            Console.WriteLine();
            Console.WriteLine("=== Package Statistics ===");
            Console.WriteLine($"Total resources: {package.ResourceIndex.Count}");

            foreach (var entry in package.ResourceIndex)
            {
                var typeName = resourceTypeRegistry.GetTag(entry.ResourceType) ?? "Custom Resource";

                Console.WriteLine($"  {typeName}: T=0x{entry.ResourceType:X8}, Size={entry.FileSize} bytes");
            }

            // Save the package
            Console.WriteLine();
            Console.WriteLine("Saving package...");
            await package.SaveAsAsync(outputPath);

            // Verify the saved file
            var fileInfo = new FileInfo(outputPath);
            Console.WriteLine($"Package saved successfully!");
            Console.WriteLine($"File size: {FormatBytes(fileInfo.Length)}");
            Console.WriteLine($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");

            // Optional: Load and verify the created package
            Console.WriteLine();
            Console.WriteLine("=== Verification ===");
            await VerifyCreatedPackage(packageFactory, outputPath, logger);

            logger.LogInformation("Package creation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating the package");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a sample text resource to the package.
    /// </summary>
    private static void AddSampleTextResource(IPackage package, ILogger logger)
    {
        logger.LogInformation("Adding sample text resource");

        // Create a sample text content
        var textContent = """
            This is a sample text resource created by TS4Tools PackageCreator example.
            
            Package creation time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
            
            This demonstrates how to:
            1. Create a new package
            2. Add custom resources
            3. Save the package to disk
            
            The resource system supports any binary data, making it suitable for
            storing game assets, configuration files, or custom content.
            """;

        var textBytes = Encoding.UTF8.GetBytes(textContent);

        // Create resource key (using custom type IDs)
        var textResourceKey = new ResourceKey(
            resourceType: 0x12345678,  // Custom text resource type
            resourceGroup: 0x00000000,
            instance: 0x1000000000000001
        );

        // Create the resource
        var textResource = new CustomResource(textBytes);

        // Add to package
        package.AddResource(textResourceKey, textBytes);

        Console.WriteLine($"Added text resource: {textBytes.Length} bytes");
    }

    /// <summary>
    /// Adds a sample binary resource to the package.
    /// </summary>
    private static void AddSampleBinaryResource(IPackage package, ILogger logger)
    {
        logger.LogInformation("Adding sample binary resource");

        // Create sample binary data (simulating an image header or similar)
        var binaryData = new byte[]
        {
            // Fake PNG header
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            // Some sample data
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x10,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x91, 0x68,
            // Additional padding
            0x36, 0x00, 0x00, 0x00, 0x15, 0x49, 0x44, 0x41,
        };

        // Create resource key for binary data
        var binaryResourceKey = new ResourceKey(
            resourceType: 0x87654321,  // Custom binary resource type
            resourceGroup: 0x00000000,
            instance: 0x2000000000000001
        );

        // Create the resource
        var binaryResource = new CustomResource(binaryData);

        // Add to package
        package.AddResource(binaryResourceKey, binaryData);

        Console.WriteLine($"Added binary resource: {binaryData.Length} bytes");
    }

    /// <summary>
    /// Adds a sample metadata resource to the package.
    /// <summary>
    /// Adds a sample metadata resource to the package.
    /// </summary>
    private static void AddSampleMetadataResource(IPackage package, ILogger logger)
    {
        logger.LogInformation("Adding sample metadata resource");

        // Create JSON metadata
        var metadata = new
        {
            PackageName = "TS4Tools Example Package",
            Version = "1.0.0",
            CreatedBy = "TS4Tools PackageCreator",
            CreationDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Description = "This package was created using the TS4Tools PackageCreator example",
            Resources = new[]
            {
                new { Type = "Text", Count = 1, Description = "Sample text content" },
                new { Type = "Binary", Count = 1, Description = "Sample binary data" },
                new { Type = "Metadata", Count = 1, Description = "This metadata file" }
            }
        };

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        var metadataBytes = Encoding.UTF8.GetBytes(jsonContent);

        // Create resource key for metadata
        var metadataResourceKey = new ResourceKey(
            resourceType: 0xABCDEF00,  // Custom metadata resource type
            resourceGroup: 0x00000000,
            instance: 0x3000000000000001
        );

        // Create the resource
        var metadataResource = new CustomResource(metadataBytes);

        // Add to package
        package.AddResource(metadataResourceKey, metadataBytes);

        Console.WriteLine($"Added metadata resource: {metadataBytes.Length} bytes");
    }

    /// <summary>
    /// Verifies the created package by loading it again.
    /// </summary>
    private static async Task VerifyCreatedPackage(IPackageFactory packageFactory, string packagePath, ILogger logger)
    {
        try
        {
            logger.LogInformation("Verifying created package");

            using var verifyPackage = await packageFactory.LoadFromFileAsync(packagePath);

            Console.WriteLine($"Verification successful!");
            Console.WriteLine($"  Resources loaded: {verifyPackage.ResourceIndex.Count}");
            Console.WriteLine($"  Package path: {packagePath}");
            Console.WriteLine($"  Creation time: {verifyPackage.CreatedDate:yyyy-MM-dd HH:mm:ss}");

            // Try to read back one of the resources
            foreach (var entry in verifyPackage.ResourceIndex.Take(1))
            {
                var resource = verifyPackage.GetResource(entry);
                if (resource != null)
                {
                    var data = resource.AsBytes;
                    Console.WriteLine($"  Sample resource data length: {data.Length} bytes");

                    // If it's text data, show a preview
                    if (entry.ResourceType == 0x12345678)
                    {
                        var textContent = Encoding.UTF8.GetString(data);
                        var preview = textContent.Length > 100
                            ? textContent[..100] + "..."
                            : textContent;
                        Console.WriteLine($"  Text preview: {preview.Replace('\n', ' ').Replace('\r', ' ')}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify created package");
            Console.WriteLine($"Verification failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats byte counts into human-readable strings.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number = number / 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }
}

/// <summary>
/// Custom resource implementation for demonstration purposes.
/// </summary>
public sealed class CustomResource : IResource
{
    private readonly MemoryStream _stream;
    private readonly byte[] _data;
    private bool _disposed;

    public CustomResource(byte[] data)
    {
        _data = data;
        _stream = new MemoryStream(data);
    }

    public Stream Stream => _stream;

    public byte[] AsBytes => _data;

    public event EventHandler? ResourceChanged;

    // IApiVersion implementation
    public int RequestedApiVersion => 1;
    public int RecommendedApiVersion => 1;

    // IContentFields implementation
    public IReadOnlyList<string> ContentFields => Array.Empty<string>();

    public TypedValue this[int index]
    {
        get => throw new NotSupportedException("ContentFields indexing not supported by CustomResource");
        set => throw new NotSupportedException("ContentFields indexing not supported by CustomResource");
    }

    public TypedValue this[string name]
    {
        get => throw new NotSupportedException("ContentFields indexing not supported by CustomResource");
        set => throw new NotSupportedException("ContentFields indexing not supported by CustomResource");
    }

    private void OnResourceChanged() => ResourceChanged?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _stream?.Dispose();
            }
            _disposed = true;
        }
    }
}
