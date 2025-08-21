using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Package;
using TS4Tools.Extensions;
using TS4Tools.Extensions.ResourceTypes;

namespace PackageAnalysisScript;

/// <summary>
/// Comprehensive analysis script that scans all .package files in the Sims 4 game directory
/// and reports on resource identification and parsing success rates.
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
            logger.LogInformation("Starting comprehensive package analysis...");

            // Get the configuration to find the game directory
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var gameDir = configuration.GetValue<string>("ApplicationSettings:Game:InstallationDirectory");

            if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
            {
                Console.WriteLine("Error: Game installation directory not found or not configured.");
                Console.WriteLine("Please ensure the 'ApplicationSettings:Game:InstallationDirectory' is set in appsettings.json");
                return;
            }

            logger.LogInformation("Scanning game directory: {GameDir}", gameDir);

            // Get the package factory and resource registry services
            var packageFactory = host.Services.GetRequiredService<IPackageFactory>();
            var resourceTypeRegistry = host.Services.GetRequiredService<IResourceTypeRegistry>();

            // Find all .package files
            var packageFiles = Directory.GetFiles(gameDir, "*.package", SearchOption.AllDirectories).ToList();
            Console.WriteLine($"Found {packageFiles.Count:N0} .package files to analyze");

            // Initialize statistics tracking
            var stats = new AnalysisStats();
            var resourceTypeStats = new ConcurrentDictionary<uint, ResourceTypeStats>();
            var unknownResourceTypes = new ConcurrentDictionary<uint, int>();

            Console.WriteLine();
            Console.WriteLine("=== Package Analysis Results ===");
            Console.WriteLine($"{"Package File",-50} {"Resources",-10} {"Size",-15} {"Status",-20}");
            Console.WriteLine(new string('-', 100));

            // Process packages in parallel for better performance
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount); // Limit concurrent operations
            var tasks = packageFiles.Select(async packageFile =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await AnalyzePackageAsync(packageFile, packageFactory, resourceTypeRegistry, stats, resourceTypeStats, unknownResourceTypes, logger);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var packageResults = await Task.WhenAll(tasks);

            // Display individual package results
            foreach (var result in packageResults.Where(r => r != null))
            {
                var fileName = Path.GetFileName(result.FilePath);
                if (fileName.Length > 47) fileName = fileName.Substring(0, 44) + "...";
                
                Console.WriteLine($"{fileName,-50} {result.ResourceCount,-10:N0} {FormatBytes(result.FileSize),-15} {result.Status,-20}");
            }

            Console.WriteLine(new string('-', 100));

            // Display summary statistics
            DisplaySummaryStatistics(stats, packageFiles.Count);

            // Display resource type analysis
            DisplayResourceTypeAnalysis(resourceTypeStats, resourceTypeRegistry, unknownResourceTypes);

            // Generate detailed report
            await GenerateDetailedReportAsync(stats, resourceTypeStats, unknownResourceTypes, resourceTypeRegistry);

            logger.LogInformation("Package analysis completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during package analysis");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task<PackageAnalysisResult?> AnalyzePackageAsync(
        string packageFile, 
        IPackageFactory packageFactory, 
        IResourceTypeRegistry resourceTypeRegistry,
        AnalysisStats stats,
        ConcurrentDictionary<uint, ResourceTypeStats> resourceTypeStats,
        ConcurrentDictionary<uint, int> unknownResourceTypes,
        ILogger logger)
    {
        try
        {
            var fileInfo = new FileInfo(packageFile);
            
            using var package = await packageFactory.LoadFromFileAsync(packageFile, readOnly: true);
            
            var result = new PackageAnalysisResult
            {
                FilePath = packageFile,
                ResourceCount = package.ResourceIndex.Count,
                FileSize = fileInfo.Length,
                Status = "Success"
            };

            // Update overall statistics
            Interlocked.Increment(ref stats.TotalPackages);
            Interlocked.Add(ref stats.TotalResources, package.ResourceIndex.Count);
            Interlocked.Add(ref stats.TotalSizeBytes, fileInfo.Length);

            // Analyze each resource in the package
            var resourceAnalysisTasks = package.ResourceIndex.Select(async entry =>
            {
                try
                {
                    // Check if this resource type is known
                    var typeName = resourceTypeRegistry.GetTag(entry.ResourceType);
                    var isKnown = !string.IsNullOrEmpty(typeName);

                    // Update resource type statistics
                    var typeStats = resourceTypeStats.GetOrAdd(entry.ResourceType, _ => new ResourceTypeStats 
                    { 
                        ResourceType = entry.ResourceType,
                        TypeName = typeName ?? "Unknown",
                        IsKnown = isKnown
                    });

                    Interlocked.Increment(ref typeStats.Count);
                    Interlocked.Add(ref typeStats.TotalSize, entry.FileSize);

                    // Try to load the resource to test parsing
                    try
                    {
                        var resource = package.GetResource(entry);
                        if (resource != null)
                        {
                            // Successfully loaded and parsed
                            Interlocked.Increment(ref typeStats.SuccessfullyParsed);
                            Interlocked.Increment(ref stats.SuccessfullyParsedResources);
                            
                            // Try to access the resource data to ensure it's fully parseable
                            var data = resource.AsBytes;
                            if (data.Length > 0)
                            {
                                Interlocked.Increment(ref typeStats.FullyIdentified);
                                Interlocked.Increment(ref stats.FullyIdentifiedResources);
                            }
                        }
                        else
                        {
                            Interlocked.Increment(ref typeStats.FailedToParse);
                        }
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref typeStats.FailedToParse);
                    }

                    // Track unknown resource types
                    if (!isKnown)
                    {
                        unknownResourceTypes.AddOrUpdate(entry.ResourceType, 1, (key, value) => value + 1);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Error analyzing resource in {Package}: {Error}", packageFile, ex.Message);
                }
            });

            await Task.WhenAll(resourceAnalysisTasks);
            
            Interlocked.Increment(ref stats.SuccessfulPackages);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to analyze package {Package}: {Error}", packageFile, ex.Message);
            
            Interlocked.Increment(ref stats.FailedPackages);
            
            return new PackageAnalysisResult
            {
                FilePath = packageFile,
                ResourceCount = 0,
                FileSize = new FileInfo(packageFile).Length,
                Status = $"Failed: {ex.Message}"
            };
        }
    }

    private static void DisplaySummaryStatistics(AnalysisStats stats, int totalFiles)
    {
        Console.WriteLine();
        Console.WriteLine("=== Summary Statistics ===");
        Console.WriteLine($"Total package files found: {totalFiles:N0}");
        Console.WriteLine($"Successfully analyzed packages: {stats.SuccessfulPackages:N0}");
        Console.WriteLine($"Failed to analyze packages: {stats.FailedPackages:N0}");
        Console.WriteLine($"Total resources found: {stats.TotalResources:N0}");
        Console.WriteLine($"Successfully parsed resources: {stats.SuccessfullyParsedResources:N0}");
        Console.WriteLine($"Fully identified resources: {stats.FullyIdentifiedResources:N0}");
        Console.WriteLine($"Total data size: {FormatBytes(stats.TotalSizeBytes)}");
        
        if (stats.TotalResources > 0)
        {
            var parseSuccessRate = (double)stats.SuccessfullyParsedResources / stats.TotalResources * 100;
            var identificationSuccessRate = (double)stats.FullyIdentifiedResources / stats.TotalResources * 100;
            
            Console.WriteLine($"Parse success rate: {parseSuccessRate:F2}%");
            Console.WriteLine($"Full identification success rate: {identificationSuccessRate:F2}%");
        }
    }

    private static void DisplayResourceTypeAnalysis(
        ConcurrentDictionary<uint, ResourceTypeStats> resourceTypeStats,
        IResourceTypeRegistry resourceTypeRegistry,
        ConcurrentDictionary<uint, int> unknownResourceTypes)
    {
        Console.WriteLine();
        Console.WriteLine("=== Resource Type Analysis ===");
        Console.WriteLine($"{"Resource Type",-15} {"Type Name",-40} {"Count",-10} {"Size",-15} {"Success Rate",-12} {"Status",-10}");
        Console.WriteLine(new string('-', 125));

        var sortedStats = resourceTypeStats.Values
            .OrderByDescending(s => s.Count)
            .ToList();

        foreach (var stat in sortedStats)
        {
            var successRate = stat.Count > 0 ? (double)stat.SuccessfullyParsed / stat.Count * 100 : 0;
            var status = stat.IsKnown ? "Known" : "Unknown";
            var typeName = stat.TypeName;
            if (typeName.Length > 37) typeName = typeName.Substring(0, 34) + "...";

            Console.WriteLine($"0x{stat.ResourceType:X8}   {typeName,-40} {stat.Count,-10:N0} {FormatBytes(stat.TotalSize),-15} {successRate,-12:F1}% {status,-10}");
        }

        Console.WriteLine();
        Console.WriteLine("=== Unknown Resource Types Summary ===");
        if (unknownResourceTypes.Any())
        {
            Console.WriteLine($"Total unknown resource types: {unknownResourceTypes.Count:N0}");
            Console.WriteLine("Most common unknown types:");
            
            var topUnknown = unknownResourceTypes
                .OrderByDescending(kvp => kvp.Value)
                .Take(20)
                .ToList();

            foreach (var unknown in topUnknown)
            {
                Console.WriteLine($"  0x{unknown.Key:X8}: {unknown.Value:N0} occurrences");
            }
        }
        else
        {
            Console.WriteLine("All resource types are known and identified!");
        }
    }

    private static async Task GenerateDetailedReportAsync(
        AnalysisStats stats,
        ConcurrentDictionary<uint, ResourceTypeStats> resourceTypeStats,
        ConcurrentDictionary<uint, int> unknownResourceTypes,
        IResourceTypeRegistry resourceTypeRegistry)
    {
        var reportPath = "PackageAnalysisReport.json";
        
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            Summary = new
            {
                TotalPackages = stats.TotalPackages,
                SuccessfulPackages = stats.SuccessfulPackages,
                FailedPackages = stats.FailedPackages,
                TotalResources = stats.TotalResources,
                SuccessfullyParsedResources = stats.SuccessfullyParsedResources,
                FullyIdentifiedResources = stats.FullyIdentifiedResources,
                TotalSizeBytes = stats.TotalSizeBytes,
                ParseSuccessRate = stats.TotalResources > 0 ? (double)stats.SuccessfullyParsedResources / stats.TotalResources * 100 : 0,
                IdentificationSuccessRate = stats.TotalResources > 0 ? (double)stats.FullyIdentifiedResources / stats.TotalResources * 100 : 0
            },
            ResourceTypes = resourceTypeStats.Values.Select(s => new
            {
                ResourceType = $"0x{s.ResourceType:X8}",
                TypeName = s.TypeName,
                Count = s.Count,
                TotalSize = s.TotalSize,
                SuccessfullyParsed = s.SuccessfullyParsed,
                FullyIdentified = s.FullyIdentified,
                FailedToParse = s.FailedToParse,
                IsKnown = s.IsKnown,
                SuccessRate = s.Count > 0 ? (double)s.SuccessfullyParsed / s.Count * 100 : 0
            }).OrderByDescending(x => x.Count),
            UnknownResourceTypes = unknownResourceTypes.Select(kvp => new
            {
                ResourceType = $"0x{kvp.Key:X8}",
                Count = kvp.Value
            }).OrderByDescending(x => x.Count)
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        await File.WriteAllTextAsync(reportPath, json);
        Console.WriteLine($"\nDetailed report saved to: {reportPath}");
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number = number / 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }
}

public class AnalysisStats
{
    public int TotalPackages;
    public int SuccessfulPackages;
    public int FailedPackages;
    public long TotalResources;
    public long SuccessfullyParsedResources;
    public long FullyIdentifiedResources;
    public long TotalSizeBytes;
}

public class ResourceTypeStats
{
    public uint ResourceType;
    public string TypeName = "";
    public int Count;
    public long TotalSize;
    public int SuccessfullyParsed;
    public int FullyIdentified;
    public int FailedToParse;
    public bool IsKnown;
}

public class PackageAnalysisResult
{
    public string FilePath = "";
    public int ResourceCount;
    public long FileSize;
    public string Status = "";
}
