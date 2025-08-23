using System.Text.Json;
using TS4Tools.Tests.Common;

namespace PackageAnalysisScript;

/// <summary>
/// Simplified package analysis script that generates a concise report showing
/// the smallest package containing each resource type and lists unknown resource types.
/// Uses real game packages when available, falls back to mock data otherwise.
/// </summary>
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("TS4Tools Package Analysis Script");
            Console.WriteLine("Analyzing packages to find smallest package containing each resource type...");
            Console.WriteLine();

            // Get all resource types (no filtering) for complete analysis
            var result = await TestPackageDiscovery.GetTestPackagesAsync(
                filterResourceTypes: null, // Get all resource types
                configPath: "appsettings.json",
                maxPackagesToAnalyze: 200 // Analyze more packages for better coverage
            );

            // Create the JSON report
            var report = new
            {
                results = result.Resources,
                unknown = result.Unknown,
                metadata = new
                {
                    usedRealPackages = result.UsedRealPackages,
                    packagesAnalyzed = result.PackagesAnalyzed,
                    uniqueResourceTypes = result.UniqueResourceTypes,
                    knownResourceTypes = result.Resources.Count,
                    unknownResourceTypes = result.Unknown.Count,
                    analysisDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            };

            // Write the JSON report
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonOutput = JsonSerializer.Serialize(report, jsonOptions);
            
            // Write to file
            var outputPath = "PackageAnalysisReport.json";
            await File.WriteAllTextAsync(outputPath, jsonOutput);

            // Also write to console for immediate viewing
            Console.WriteLine("Analysis Results:");
            Console.WriteLine($"- Data source: {(result.UsedRealPackages ? "Real game packages" : "Mock packages")}");
            Console.WriteLine($"- Packages analyzed: {result.PackagesAnalyzed}");
            Console.WriteLine($"- Unique resource types found: {result.UniqueResourceTypes}");
            Console.WriteLine($"- Known resource types: {result.Resources.Count}");
            Console.WriteLine($"- Unknown resource types: {result.Unknown.Count}");
            Console.WriteLine();

            if (result.Unknown.Count > 0)
            {
                Console.WriteLine("Unknown resource types (not yet supported by TS4Tools):");
                foreach (var unknownType in result.Unknown.Take(10)) // Show first 10
                {
                    Console.WriteLine($"  - {unknownType}");
                }
                if (result.Unknown.Count > 10)
                {
                    Console.WriteLine($"  ... and {result.Unknown.Count - 10} more");
                }
                Console.WriteLine();
            }

            // Show some examples of known resource types
            Console.WriteLine("Examples of known resource types and their smallest packages:");
            foreach (var (resourceType, packagePath) in result.Resources.Take(5))
            {
                var fileName = Path.GetFileName(packagePath);
                Console.WriteLine($"  - {resourceType}: {fileName}");
            }
            Console.WriteLine();

            // Check specifically for BC4A5044 (our Clip Header resource)
            const string clipHeaderType = "0xBC4A5044";
            if (result.Resources.ContainsKey(clipHeaderType))
            {
                Console.WriteLine($"✓ BC4A5044 (Clip Header) found in: {Path.GetFileName(result.Resources[clipHeaderType])}");
            }
            else if (result.Unknown.Contains(clipHeaderType))
            {
                Console.WriteLine("⚠ BC4A5044 (Clip Header) found but marked as unknown - implementation may need testing");
            }
            else
            {
                Console.WriteLine("ℹ BC4A5044 (Clip Header) not found in analyzed packages");
            }

            Console.WriteLine();
            Console.WriteLine($"Full report written to: {Path.GetFullPath(outputPath)}");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during analysis: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}
