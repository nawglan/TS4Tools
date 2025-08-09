using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TS4Tools.Core.Package;
using TS4Tools.Core.Package.DependencyInjection;
using TS4Tools.Core.Settings;
using TS4Tools.Core.Interfaces;

namespace TS4Tools.Analysis.ResourceFrequency;

/// <summary>
/// Resource frequency analyzer for Phase 4.13
/// Analyzes real Sims 4 packages to determine resource type priority
/// </summary>
public class ResourceFrequencyAnalyzer
{
    private readonly ILogger<ResourceFrequencyAnalyzer> _logger;
    private readonly IPackageFactory _packageFactory;

    public ResourceFrequencyAnalyzer(ILogger<ResourceFrequencyAnalyzer> logger, IPackageFactory packageFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _packageFactory = packageFactory ?? throw new ArgumentNullException(nameof(packageFactory));
    }

    public async Task<ResourceTypeFrequencyReport> AnalyzePackagesAsync(IEnumerable<string> packagePaths)
    {
        var frequencyMap = new Dictionary<uint, ResourceTypeStats>();
        var processedPackages = 0;
        var totalResourcesProcessed = 0L;

        _logger.LogInformation("Starting resource type frequency analysis on {PackageCount} packages",
            packagePaths.Count());

        foreach (var packagePath in packagePaths)
        {
            try
            {
                _logger.LogDebug("Analyzing package: {PackagePath}", packagePath);

                var package = await _packageFactory.LoadFromFileAsync(packagePath, readOnly: true);
                var stats = await AnalyzePackageAsync(package, packagePath);

                // Aggregate statistics
                foreach (var kvp in stats)
                {
                    var resourceType = kvp.Key;
                    var packageStats = kvp.Value;

                    if (frequencyMap.ContainsKey(resourceType))
                    {
                        frequencyMap[resourceType].TotalOccurrences += packageStats.TotalOccurrences;
                        frequencyMap[resourceType].TotalBytes += packageStats.TotalBytes;
                        frequencyMap[resourceType].PackagesContaining++;
                        frequencyMap[resourceType].AverageSize = frequencyMap[resourceType].TotalBytes / frequencyMap[resourceType].TotalOccurrences;
                    }
                    else
                    {
                        frequencyMap[resourceType] = new ResourceTypeStats
                        {
                            ResourceType = resourceType,
                            TotalOccurrences = packageStats.TotalOccurrences,
                            TotalBytes = packageStats.TotalBytes,
                            PackagesContaining = 1,
                            AverageSize = packageStats.AverageSize
                        };
                    }
                }

                processedPackages++;
                totalResourcesProcessed += package.ResourceCount;

                package.Dispose(); // Clean up after analysis
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze package: {PackagePath}", packagePath);
            }
        }

        _logger.LogInformation("Analysis complete. Processed {PackageCount} packages, {ResourceCount} total resources",
            processedPackages, totalResourcesProcessed);

        return new ResourceTypeFrequencyReport
        {
            PackagesAnalyzed = processedPackages,
            TotalResourcesAnalyzed = totalResourcesProcessed,
            ResourceTypeStats = frequencyMap.Values.OrderByDescending(s => s.TotalOccurrences).ToList(),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<Dictionary<uint, ResourceTypeStats>> AnalyzePackageAsync(IPackage package, string packagePath)
    {
        var stats = new Dictionary<uint, ResourceTypeStats>();

        foreach (var entry in package.GetResourceList())
        {
            var resourceType = entry.ResourceType;

            if (stats.ContainsKey(resourceType))
            {
                stats[resourceType].TotalOccurrences++;
                stats[resourceType].TotalBytes += entry.Filesize;
            }
            else
            {
                stats[resourceType] = new ResourceTypeStats
                {
                    ResourceType = resourceType,
                    TotalOccurrences = 1,
                    TotalBytes = entry.Filesize,
                    PackagesContaining = 1,
                    AverageSize = entry.Filesize
                };
            }
        }

        // Calculate averages
        foreach (var kvp in stats)
        {
            kvp.Value.AverageSize = kvp.Value.TotalBytes / kvp.Value.TotalOccurrences;
        }

        return stats;
    }
}

public class ResourceTypeStats
{
    public uint ResourceType { get; set; }
    public long TotalOccurrences { get; set; }
    public long TotalBytes { get; set; }
    public int PackagesContaining { get; set; }
    public long AverageSize { get; set; }
    public string ResourceTypeHex => $"0x{ResourceType:X8}";
    public bool HasImplementation { get; set; }
    public string? ImplementedBy { get; set; }
    public ResourcePriority Priority { get; set; }
}

public class ResourceTypeFrequencyReport
{
    public int PackagesAnalyzed { get; set; }
    public long TotalResourcesAnalyzed { get; set; }
    public List<ResourceTypeStats> ResourceTypeStats { get; set; } = new();
    public DateTimeOffset GeneratedAt { get; set; }

    public IEnumerable<ResourceTypeStats> GetTopResourceTypes(int count) =>
        ResourceTypeStats.Take(count);

    public IEnumerable<ResourceTypeStats> GetUnimplementedResourceTypes() =>
        ResourceTypeStats.Where(s => !s.HasImplementation);

    public IEnumerable<ResourceTypeStats> GetCriticalPriorityTypes() =>
        ResourceTypeStats.Where(s => s.Priority == ResourcePriority.Critical);

    public void GenerateMarkdownReport(string outputPath)
    {
        var markdown = $@"# Resource Type Frequency Analysis Report

**Generated:** {GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC
**Packages Analyzed:** {PackagesAnalyzed}
**Total Resources:** {TotalResourcesAnalyzed:N0}

## Top 20 Most Frequent Resource Types

| Rank | Resource Type | Occurrences | Total Size | Avg Size | Packages | Implementation Status |
|------|--------------|-------------|------------|----------|----------|---------------------|
{string.Join("\n", ResourceTypeStats.Take(20).Select((s, i) =>
    $"| {i + 1} | `{s.ResourceTypeHex}` | {s.TotalOccurrences:N0} | {FormatBytes(s.TotalBytes)} | {FormatBytes(s.AverageSize)} | {s.PackagesContaining} | {GetImplementationStatus(s)} |"))}

## Unimplemented Resource Types by Priority

### Critical Priority (Top 5 Unimplemented)
{string.Join("\n", GetUnimplementedResourceTypes().Take(5).Select(s =>
    $"- **{s.ResourceTypeHex}**: {s.TotalOccurrences:N0} occurrences, {FormatBytes(s.TotalBytes)} total size"))}

### High Priority (Next 10 Unimplemented)
{string.Join("\n", GetUnimplementedResourceTypes().Skip(5).Take(10).Select(s =>
    $"- **{s.ResourceTypeHex}**: {s.TotalOccurrences:N0} occurrences, {FormatBytes(s.TotalBytes)} total size"))}

## Analysis Summary

- **Total Unique Resource Types Found:** {ResourceTypeStats.Count}
- **Implemented Types:** {ResourceTypeStats.Count(s => s.HasImplementation)}
- **Missing Types:** {ResourceTypeStats.Count(s => !s.HasImplementation)}
- **Implementation Coverage:** {(double)ResourceTypeStats.Count(s => s.HasImplementation) / ResourceTypeStats.Count * 100:F1}%

";

        File.WriteAllText(outputPath, markdown);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        if (bytes == 0) return "0 B";

        int order = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
        double size = bytes / Math.Pow(1024, order);
        return $"{size:F2} {sizes[order]}";
    }

    private static string GetImplementationStatus(ResourceTypeStats stats)
    {
        if (stats.HasImplementation && !string.IsNullOrEmpty(stats.ImplementedBy))
            return $"✅ {stats.ImplementedBy}";
        if (stats.HasImplementation)
            return "✅ Implemented";
        return "❌ Missing";
    }
}

public enum ResourcePriority
{
    Critical,
    High,
    Medium,
    Low
}
