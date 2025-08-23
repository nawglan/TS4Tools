using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Package;
using TS4Tools.Core.Package.DependencyInjection;
using TS4Tools.Core.Resources;
using TS4Tools.Core.DependencyInjection;
using TS4Tools.Core.Settings;

namespace TS4Tools.Tests.Common;

/// <summary>
/// Result containing resource type to package mappings and metadata.
/// </summary>
public sealed record TestPackageResult
{
    /// <summary>
    /// Dictionary mapping resource type IDs to the smallest package path containing that resource type.
    /// </summary>
    public required Dictionary<string, string> Resources { get; init; }

    /// <summary>
    /// List of resource type IDs that are not supported by TS4Tools yet.
    /// </summary>
    public required IReadOnlyList<string> Unknown { get; init; }

    /// <summary>
    /// Whether real game packages were used (true) or mock packages (false).
    /// </summary>
    public bool UsedRealPackages { get; init; }

    /// <summary>
    /// Total number of packages analyzed.
    /// </summary>
    public int PackagesAnalyzed { get; init; }

    /// <summary>
    /// Total number of unique resource types found.
    /// </summary>
    public int UniqueResourceTypes { get; init; }
}

/// <summary>
/// Utility class for finding and managing test packages for resource type testing.
/// Supports both real game packages and mock packages based on configuration.
/// </summary>
public static partial class TestPackageDiscovery
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Real game installation found at: {Path}")]
    private static partial void LogRealGameInstallFound(ILogger logger, string path);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "No real game installation found, using mock packages")]
    private static partial void LogUsingMockPackages(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error during package discovery, falling back to mock packages")]
    private static partial void LogPackageDiscoveryError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Found {Count} packages to analyze (limited to {Max})")]
    private static partial void LogPackagesFound(ILogger logger, int count, int max);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Analyzed {Count} packages, found {ResourceTypes} unique resource types")]
    private static partial void LogAnalysisProgress(ILogger logger, int count, int resourceTypes);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Failed to analyze package: {Path}")]
    private static partial void LogPackageAnalysisFailure(ILogger logger, Exception ex, string path);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Analysis complete: {Resources} resource types mapped, {Unknown} unknown types")]
    private static partial void LogAnalysisComplete(ILogger logger, int resources, int unknown);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Error testing resource type {Type:X8}: {Error}")]
    private static partial void LogResourceTypeTestError(ILogger logger, uint type, string error);

    /// <summary>
    /// Gets test packages for specific resource types, finding the smallest package containing each type.
    /// When no real game installation is available, returns mock packages.
    /// </summary>
    /// <param name="filterResourceTypes">
    /// Optional list of resource type IDs to filter for. If empty/null, returns all found resource types.
    /// </param>
    /// <param name="configPath">Optional path to configuration file. Uses default discovery if null.</param>
    /// <param name="maxPackagesToAnalyze">Maximum number of packages to analyze (for performance). Default 100.</param>
    /// <returns>TestPackageResult with resource mappings and metadata.</returns>
    public static async Task<TestPackageResult> GetTestPackagesAsync(
        IEnumerable<uint>? filterResourceTypes = null,
        string? configPath = null,
        int maxPackagesToAnalyze = 100)
    {
        var filterSet = filterResourceTypes?.ToHashSet();
        var logger = CreateLogger();

        try
        {
            // Load configuration to determine if real packages are available
            var configuration = LoadConfiguration(configPath);
            var settings = new ApplicationSettings();
            configuration.Bind("ApplicationSettings", settings);

            // Check if real game installation is available
            var gameInstallPath = settings?.Game?.InstallationDirectory;
            var hasRealInstall = !string.IsNullOrEmpty(gameInstallPath) && 
                                 Directory.Exists(gameInstallPath);

            if (hasRealInstall)
            {
                LogRealGameInstallFound(logger, gameInstallPath!);
                return await AnalyzeRealPackagesAsync(gameInstallPath!, filterSet, maxPackagesToAnalyze, logger);
            }
            else
            {
                LogUsingMockPackages(logger);
                return GetMockPackages(filterSet);
            }
        }
        catch (Exception ex)
        {
            LogPackageDiscoveryError(logger, ex);
            return GetMockPackages(filterSet);
        }
    }

    /// <summary>
    /// Analyzes real game packages to find the smallest package containing each resource type.
    /// </summary>
    private static async Task<TestPackageResult> AnalyzeRealPackagesAsync(
        string gameInstallPath,
        HashSet<uint>? filterResourceTypes,
        int maxPackagesToAnalyze,
        ILogger logger)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddTS4ToolsPackageServices();
        services.AddTS4ToolsResourceServices();

        using var serviceProvider = services.BuildServiceProvider();
        var packageFactory = serviceProvider.GetRequiredService<IPackageFactory>();
        var resourceManager = serviceProvider.GetRequiredService<IResourceManager>();

        // Find all package files
        var packagePaths = new List<string>();
        var searchPaths = new[]
        {
            Path.Combine(gameInstallPath, "Data", "Client"),
            Path.Combine(gameInstallPath, "Data", "Shared"),
            Path.Combine(gameInstallPath, "Data"),
            gameInstallPath
        };

        foreach (var searchPath in searchPaths.Where(Directory.Exists))
        {
            var packages = Directory.GetFiles(searchPath, "*.package", SearchOption.AllDirectories);
            packagePaths.AddRange(packages);
        }

        // Sort by file size (smallest first) for better results
        var packageInfos = packagePaths
            .Select(path => new { Path = path, Size = new FileInfo(path).Length })
            .OrderBy(info => info.Size)
            .Take(maxPackagesToAnalyze)
            .ToList();

        LogPackagesFound(logger, packageInfos.Count, maxPackagesToAnalyze);

        // Dictionary to track smallest package for each resource type
        var resourceToPackage = new Dictionary<uint, string>();
        var resourceToSize = new Dictionary<uint, long>();
        var allFoundResourceTypes = new HashSet<uint>();

        var packagesAnalyzed = 0;
        foreach (var packageInfo in packageInfos)
        {
            try
            {
                using var package = await packageFactory.LoadFromFileAsync(packageInfo.Path, readOnly: true);
                var resourceTypesInPackage = new HashSet<uint>();

                foreach (var resourceInfo in package.ResourceIndex)
                {
                    var resourceType = resourceInfo.ResourceType;
                    allFoundResourceTypes.Add(resourceType);
                    resourceTypesInPackage.Add(resourceType);

                    // If filtering and this type is not in filter, skip
                    if (filterResourceTypes != null && !filterResourceTypes.Contains(resourceType))
                        continue;

                    // If we haven't seen this resource type yet, or this package is smaller, update
                    if (!resourceToPackage.ContainsKey(resourceType) || 
                        packageInfo.Size < resourceToSize[resourceType])
                    {
                        resourceToPackage[resourceType] = packageInfo.Path;
                        resourceToSize[resourceType] = packageInfo.Size;
                    }
                }

                packagesAnalyzed++;
                
                if (packagesAnalyzed % 10 == 0)
                {
                    LogAnalysisProgress(logger, packagesAnalyzed, allFoundResourceTypes.Count);
                }
            }
            catch (Exception ex)
            {
                LogPackageAnalysisFailure(logger, ex, packageInfo.Path);
            }
        }

        // Determine which resource types are unknown (not supported by TS4Tools)
        var unknownResourceTypes = await DetermineUnknownResourceTypesAsync(
            allFoundResourceTypes, resourceManager, logger);

        // Convert to string format for JSON serialization
        var resourceDict = resourceToPackage.ToDictionary(
            kvp => $"0x{kvp.Key:X8}",
            kvp => kvp.Value
        );

        var unknownList = unknownResourceTypes.Select(id => $"0x{id:X8}").ToList();

        LogAnalysisComplete(logger, resourceDict.Count, unknownList.Count);

        return new TestPackageResult
        {
            Resources = resourceDict,
            Unknown = unknownList.AsReadOnly(),
            UsedRealPackages = true,
            PackagesAnalyzed = packagesAnalyzed,
            UniqueResourceTypes = allFoundResourceTypes.Count
        };
    }

    /// <summary>
    /// Determines which resource types are not supported by TS4Tools yet.
    /// </summary>
    private static async Task<List<uint>> DetermineUnknownResourceTypesAsync(
        IEnumerable<uint> allResourceTypes,
        IResourceManager resourceManager,
        ILogger logger)
    {
        var unknownTypes = new List<uint>();

        foreach (var resourceType in allResourceTypes)
        {
            try
            {
                var resource = await resourceManager.CreateResourceAsync(resourceType.ToString("X8", CultureInfo.InvariantCulture), 1);
                if (resource == null)
                {
                    unknownTypes.Add(resourceType);
                }
            }
            catch (NotImplementedException)
            {
                unknownTypes.Add(resourceType);
            }
            catch (Exception ex)
            {
                LogResourceTypeTestError(logger, resourceType, ex.Message);
                unknownTypes.Add(resourceType);
            }
        }

        return unknownTypes;
    }

    /// <summary>
    /// Returns mock package mappings when no real game installation is available.
    /// </summary>
    private static TestPackageResult GetMockPackages(HashSet<uint>? filterResourceTypes)
    {
        // Mock packages for common resource types (used when no real installation available)
        var allMockResources = new Dictionary<string, string>
        {
            { "0x220557DA", "test-data/mock/string-table-mock.package" },
            { "0x2E75C764", "test-data/mock/image-dds-mock.package" },
            { "0x2F7D0004", "test-data/mock/image-png-mock.package" },
            { "0x319E4F1D", "test-data/mock/catalog-mock.package" },
            { "0x8EAF13DE", "test-data/mock/animation-mock.package" },
            { "0xBC4A5044", "test-data/mock/clip-header-mock.package" },
            { "0x015A1849", "test-data/mock/geometry-mock.package" },
            { "0xF0582F9A", "test-data/mock/audio-mock.package" },
            { "0x0355E0A6", "test-data/mock/neighborhood-mock.package" },
            { "0x0604ABDA", "test-data/mock/lot-mock.package" }
        };

        // Apply filter if specified
        Dictionary<string, string> filteredResources;
        if (filterResourceTypes != null)
        {
            var filterStringSet = filterResourceTypes.Select(id => $"0x{id:X8}").ToHashSet();
            filteredResources = allMockResources
                .Where(kvp => filterStringSet.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        else
        {
            filteredResources = allMockResources;
        }

        // For mock packages, assume a few types are unknown for testing
        var unknownTypes = new List<string>
        {
            "0x12345678", // Example unknown type
            "0x87654321"  // Another example unknown type
        };

        return new TestPackageResult
        {
            Resources = filteredResources,
            Unknown = unknownTypes.AsReadOnly(),
            UsedRealPackages = false,
            PackagesAnalyzed = allMockResources.Count,
            UniqueResourceTypes = allMockResources.Count
        };
    }

    /// <summary>
    /// Loads configuration from the specified path or uses default discovery.
    /// </summary>
    private static IConfigurationRoot LoadConfiguration(string? configPath)
    {
        var builder = new ConfigurationBuilder();

        if (!string.IsNullOrEmpty(configPath))
        {
            var configDirectory = Path.GetDirectoryName(configPath);
            var fileName = Path.GetFileName(configPath);
            
            if (!string.IsNullOrEmpty(configDirectory) && Directory.Exists(configDirectory))
            {
                builder.SetBasePath(configDirectory)
                       .AddJsonFile(fileName, optional: true);
            }
            else
            {
                // If directory doesn't exist or path is just a filename, try current directory
                builder.SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile(configPath, optional: true);
            }
        }
        else
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: true)
                   .AddJsonFile("appsettings.template.json", optional: true)
                   .AddJsonFile("appsettings.Development.json", optional: true);
        }

        return builder.AddEnvironmentVariables().Build();
    }

    /// <summary>
    /// Creates a logger for the package discovery process.
    /// </summary>
    private static ILogger CreateLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.SetMinimumLevel(LogLevel.Information)
                   .AddConsole());
        
        return loggerFactory.CreateLogger("TestPackageDiscovery");
    }
}
