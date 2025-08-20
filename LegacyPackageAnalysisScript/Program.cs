using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using s4pi.Interfaces;
using s4pi.Package;

namespace LegacyPackageAnalysisScript
{
    /// <summary>
    /// Comprehensive analysis script using the legacy s4pi library to scan all .package files 
    /// in the Sims 4 game directory and reports on resource identification and parsing success rates.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting comprehensive package analysis using legacy s4pi library...");

                // Get the game directory from environment or default
                var gameDir = Environment.GetEnvironmentVariable("SIMS4_GAME_DIR") 
                    ?? "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4";

                if (!Directory.Exists(gameDir))
                {
                    Console.WriteLine($"Error: Game installation directory not found: {gameDir}");
                    Console.WriteLine("Please set the SIMS4_GAME_DIR environment variable or ensure the default path exists.");
                    return;
                }

                Console.WriteLine($"Scanning game directory: {gameDir}");

                // Find all .package files
                var packageFiles = Directory.GetFiles(gameDir, "*.package", SearchOption.AllDirectories).ToList();
                Console.WriteLine($"Found {packageFiles.Count:N0} .package files to analyze");

                // Initialize statistics tracking
                var stats = new AnalysisStats();
                var resourceTypeStats = new Dictionary<uint, ResourceTypeStats>();
                var unknownResourceTypes = new Dictionary<uint, int>();

                Console.WriteLine();
                Console.WriteLine("=== Package Analysis Results ===");
                Console.WriteLine($"{"Package File",-50} {"Resources",-10} {"Size",-15} {"Status",-20}");
                Console.WriteLine(new string('-', 100));

                // Process packages sequentially to avoid overwhelming the system
                foreach (var packageFile in packageFiles)
                {
                    var result = AnalyzePackage(packageFile, stats, resourceTypeStats, unknownResourceTypes);
                    
                    var fileName = Path.GetFileName(result.FilePath);
                    if (fileName.Length > 47) fileName = fileName.Substring(0, 44) + "...";
                    
                    Console.WriteLine($"{fileName,-50} {result.ResourceCount,-10:N0} {FormatBytes(result.FileSize),-15} {result.Status,-20}");
                }

                Console.WriteLine(new string('-', 100));

                // Display summary statistics
                DisplaySummaryStatistics(stats, packageFiles.Count);

                // Display resource type analysis
                DisplayResourceTypeAnalysis(resourceTypeStats, unknownResourceTypes);

                // Generate detailed report
                GenerateDetailedReport(stats, resourceTypeStats, unknownResourceTypes);

                Console.WriteLine("Package analysis completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static PackageAnalysisResult AnalyzePackage(
            string packageFile, 
            AnalysisStats stats,
            Dictionary<uint, ResourceTypeStats> resourceTypeStats,
            Dictionary<uint, int> unknownResourceTypes)
        {
            try
            {
                var fileInfo = new FileInfo(packageFile);
                
                using var package = (APackage)Package.OpenPackage(0, packageFile, true);
                
                var result = new PackageAnalysisResult
                {
                    FilePath = packageFile,
                    ResourceCount = package.GetResourceList.Count,
                    FileSize = fileInfo.Length,
                    Status = "Success"
                };

                // Update overall statistics
                stats.TotalPackages++;
                stats.TotalResources += package.GetResourceList.Count;
                stats.TotalSizeBytes += fileInfo.Length;

                // Analyze each resource in the package
                foreach (IResourceIndexEntry entry in package.GetResourceList)
                {
                    try
                    {
                        // Check if this resource type is known by looking it up in the known types
                        var typeName = GetResourceTypeName(entry.ResourceType);
                        var isKnown = !string.IsNullOrEmpty(typeName);

                        // Update resource type statistics
                        if (!resourceTypeStats.ContainsKey(entry.ResourceType))
                        {
                            resourceTypeStats[entry.ResourceType] = new ResourceTypeStats 
                            { 
                                ResourceType = entry.ResourceType,
                                TypeName = typeName ?? "Unknown",
                                IsKnown = isKnown
                            };
                        }

                        var typeStats = resourceTypeStats[entry.ResourceType];
                        typeStats.Count++;
                        typeStats.TotalSize += entry.Filesize;

                        // Try to load the resource to test parsing
                        try
                        {
                            var resource = s4pi.WrapperDealer.WrapperDealer.GetResource(0, package, entry);
                            if (resource != null)
                            {
                                // Successfully loaded and parsed
                                typeStats.SuccessfullyParsed++;
                                stats.SuccessfullyParsedResources++;
                                
                                // Try to access the resource data to ensure it's fully parseable
                                var data = resource.AsBytes;
                                if (data.Length > 0)
                                {
                                    typeStats.FullyIdentified++;
                                    stats.FullyIdentifiedResources++;
                                }
                            }
                            else
                            {
                                typeStats.FailedToParse++;
                            }
                        }
                        catch (Exception)
                        {
                            typeStats.FailedToParse++;
                        }

                        // Track unknown resource types
                        if (!isKnown)
                        {
                            if (!unknownResourceTypes.ContainsKey(entry.ResourceType))
                                unknownResourceTypes[entry.ResourceType] = 0;
                            unknownResourceTypes[entry.ResourceType]++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Error analyzing resource in {packageFile}: {ex.Message}");
                    }
                }
                
                stats.SuccessfulPackages++;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to analyze package {packageFile}: {ex.Message}");
                
                stats.FailedPackages++;
                
                return new PackageAnalysisResult
                {
                    FilePath = packageFile,
                    ResourceCount = 0,
                    FileSize = new FileInfo(packageFile).Length,
                    Status = $"Failed: {ex.Message}"
                };
            }
        }

        private static string GetResourceTypeName(uint resourceType)
        {
            // Map of known resource types - this could be extended with more types
            var knownTypes = new Dictionary<uint, string>
            {
                { 0x00B2D882, "DDS Image" },
                { 0x220557DA, "String Table (STBL)" },
                { 0x015A1849, "Geometry (GEOM)" },
                { 0x01661233, "Model (MODL)" },
                { 0x736884F1, "Model LOD (MLOD)" },
                { 0x319E4F1D, "CAS Part (CASP)" },
                { 0x034AEECB, "Object Definition (OBJD)" },
                { 0x0355E0A6, "CAS Resource" },
                { 0x6B20C4F3, "Animation Clip" },
                { 0x8EAF13DE, "Rig" },
                { 0x18D878AF, "Sound (SNR)" },
                { 0x6017E896, "Sim Outfit (SIMO)" },
                { 0x62E94D38, "Behavior (BHV)" },
                { 0x0166038C, "Layout (LAYO)" },
                { 0x2E75C764, "Thumbnail (THUM)" },
                // Add more types as discovered
            };

            return knownTypes.TryGetValue(resourceType, out var name) ? name : null;
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
            Dictionary<uint, ResourceTypeStats> resourceTypeStats,
            Dictionary<uint, int> unknownResourceTypes)
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

        private static void GenerateDetailedReport(
            AnalysisStats stats,
            Dictionary<uint, ResourceTypeStats> resourceTypeStats,
            Dictionary<uint, int> unknownResourceTypes)
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
            
            File.WriteAllText(reportPath, json);
            Console.WriteLine($"\nDetailed report saved to: {reportPath}");
        }

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
}
