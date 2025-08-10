using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Package;
using TS4Tools.Extensions;
using TS4Tools.Extensions.ResourceTypes;

namespace BasicPackageReader;

/// <summary>
/// Basic example demonstrating how to read and analyze a Sims 4 package file.
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
                Console.WriteLine("Usage: BasicPackageReader <package-file-path>");
                Console.WriteLine("Example: BasicPackageReader C:\\path\\to\\your\\file.package");
                return;
            }

            var packagePath = args[0];

            if (!File.Exists(packagePath))
            {
                Console.WriteLine($"Error: File not found: {packagePath}");
                return;
            }

            logger.LogInformation("Loading package: {PackagePath}", packagePath);

            // Get the package factory service
            var packageFactory = host.Services.GetRequiredService<IPackageFactory>();
            var resourceTypeRegistry = host.Services.GetRequiredService<IResourceTypeRegistry>();

            // Load the package
            using var package = await packageFactory.LoadFromFileAsync(packagePath);

            // Display basic package information
            Console.WriteLine();
            Console.WriteLine("=== Package Information ===");
            Console.WriteLine($"File Name: {package.FileName}");
            Console.WriteLine($"Created: {package.CreatedDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Modified: {package.ModifiedDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Total Resources: {package.ResourceIndex.Count:N0}");

            // Analyze resources by type
            Console.WriteLine();
            Console.WriteLine("=== Resource Analysis ===");

            var resourcesByType = package.ResourceIndex
                .GroupBy(entry => entry.ResourceType)
                .OrderByDescending(group => group.Count())
                .ToList();

            Console.WriteLine($"{"Type ID",-12} {"Count",-8} {"Total Size",-15} {"Type Name",-30}");
            Console.WriteLine(new string('-', 75));

            long totalSize = 0;
            foreach (var group in resourcesByType)
            {
                var typeId = group.Key;
                var count = group.Count();
                var typeSize = group.Sum(entry => (long)entry.FileSize);
                totalSize += typeSize;

                // Try to get a friendly name for the resource type
                var typeName = resourceTypeRegistry.GetTag(typeId) ?? "Unknown";

                Console.WriteLine($"0x{typeId:X8}   {count,-8:N0} {FormatBytes(typeSize),-15} {typeName,-30}");
            }

            Console.WriteLine(new string('-', 75));
            Console.WriteLine($"{"TOTAL",-12} {package.ResourceIndex.Count,-8:N0} {FormatBytes(totalSize),-15}");

            // Show some sample resources
            Console.WriteLine();
            Console.WriteLine("=== Sample Resources ===");

            var sampleResources = package.ResourceIndex.Take(5).ToList();
            foreach (var entry in sampleResources)
            {
                Console.WriteLine($"Resource Key: T=0x{entry.ResourceType:X8}, G=0x{entry.ResourceGroup:X8}, I=0x{entry.Instance:X16}");
                Console.WriteLine($"  Size: {FormatBytes(entry.FileSize)} (compressed: {entry.Compressed != 0})");
                Console.WriteLine($"  Position: 0x{entry.ChunkOffset:X8}");

                // Try to load the resource data
                try
                {
                    var resource = package.GetResource(entry);
                    if (resource != null)
                    {
                        var data = resource.AsBytes;
                        Console.WriteLine($"  Data loaded: {data.Length:N0} bytes");

                        // Show first few bytes as hex
                        var previewLength = Math.Min(16, data.Length);
                        var hexPreview = string.Join(" ", data.Take(previewLength).Select(b => $"{b:X2}"));
                        Console.WriteLine($"  Preview: {hexPreview}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error loading resource: {ex.Message}");
                }

                Console.WriteLine();
            }

            logger.LogInformation("Package analysis completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing the package");
            Console.WriteLine($"Error: {ex.Message}");
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
