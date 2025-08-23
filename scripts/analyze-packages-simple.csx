#!/usr/bin/env dotnet-script
#r "nuget: System.Text.Json, 9.0.0"
#r "nuget: Microsoft.Extensions.Configuration, 9.0.0"
#r "nuget: Microsoft.Extensions.Configuration.Json, 9.0.0"

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Simplified package analysis script that finds the smallest package containing each resource type.
/// Can be used as a dotnet script or as a utility for golden master testing.
/// </summary>

// Configuration model
public class AppSettings
{
    public string? GameInstallPath { get; set; }
    public bool UseRealPackages { get; set; } = true;
}

// Result models
public class ResourceAnalysisResult
{
    [JsonPropertyName("resources")]
    public Dictionary<string, string> Resources { get; set; } = new();
    
    [JsonPropertyName("unknown")]
    public List<string> Unknown { get; set; } = new();
    
    [JsonPropertyName("summary")]
    public AnalysisSummary Summary { get; set; } = new();
}

public class AnalysisSummary
{
    [JsonPropertyName("totalPackages")]
    public int TotalPackages { get; set; }
    
    [JsonPropertyName("totalResourceTypes")]
    public int TotalResourceTypes { get; set; }
    
    [JsonPropertyName("knownResourceTypes")]
    public int KnownResourceTypes { get; set; }
    
    [JsonPropertyName("unknownResourceTypes")]
    public int UnknownResourceTypes { get; set; }
    
    [JsonPropertyName("analysisTime")]
    public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
}

// Known resource types from TS4Tools
var knownResourceTypes = new HashSet<uint>
{
    // Images and Textures
    0x00B2D882, // DDS
    0x2E75C764, // THUM
    0x2E75C765, // IMG
    0x3C2A8647, // Additional Thumbnail
    0x3C1AF1F2, // PNG Thumbnail
    
    // String Tables
    0x220557DA, // STBL
    
    // 3D Models and Meshes
    0x015A1849, // GEOM
    0x01661233, // MODL
    0x736884F1, // MLOD
    0x01D10F34, // MLOD (Object Geometry LODs)
    
    // Catalog Resources
    0x319E4F1D, // CASP
    0x034AEECB, // OBJD
    0xC0DB5AE7, // OBJDEF
    0x0355E0A6, // CAS
    
    // Animation and Rigs
    0x6B20C4F3, // CLIP
    0x8EAF13DE, // RIG
    0xBC4A5044, // CLHD (Clip Header) - our new implementation
    
    // Audio
    0x18D878AF, // SNR
    0x01A527DB, // Audio SNR (Voice/Audio)
    0xFD04E3BE, // Audio Configuration
    
    // Materials
    0x545AC67A, // Material/SWB Resource
    
    // Scripts and Tuning
    0x6017E896, // SIMO
    0x62E94D38, // BHV
    
    // UI and Layout
    0x0166038C, // LAYO
    
    // Default/Unknown
    0x00000000  // UNKN
};

// Mock packages for testing when no real game install is available
var mockPackages = new Dictionary<string, string[]>
{
    ["mock/test-package-simple-mock.package"] = new[] { "0x220557DA", "0x00B2D882" },
    ["mock/test-package-complex-mock.package"] = new[] { "0x319E4F1D", "0x034AEECB", "0x6B20C4F3" },
    ["mock/test-package-edge-case-mock.package"] = new[] { "0xBC4A5044", "0x545AC67A", "0x01A527DB" }
};

/// <summary>
/// Main analysis function that can be called from other code or run as a script.
/// </summary>
/// <param name="configPath">Path to appsettings.json, or null to use default</param>
/// <param name="outputPath">Path for output JSON, or null for console output</param>
/// <returns>Analysis result</returns>
public static async Task<ResourceAnalysisResult> AnalyzePackagesAsync(string? configPath = null, string? outputPath = null)
{
    var config = LoadConfiguration(configPath);
    var result = new ResourceAnalysisResult();
    
    if (ShouldUseRealPackages(config))
    {
        Console.WriteLine("Analyzing real game packages...");
        await AnalyzeRealPackages(config.GameInstallPath!, result);
    }
    else
    {
        Console.WriteLine("Using mock packages for testing...");
        AnalyzeMockPackages(result);
    }
    
    // Categorize resource types
    CategorizeResourceTypes(result);
    
    // Output results
    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
    { 
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
    
    if (!string.IsNullOrEmpty(outputPath))
    {
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"Analysis saved to: {outputPath}");
    }
    else
    {
        Console.WriteLine(json);
    }
    
    return result;
}

/// <summary>
/// Loads configuration from appsettings.json
/// </summary>
static AppSettings LoadConfiguration(string? configPath = null)
{
    var builder = new ConfigurationBuilder();
    
    // Default to appsettings.json in current directory
    var settingsPath = configPath ?? "appsettings.json";
    
    if (File.Exists(settingsPath))
    {
        builder.AddJsonFile(settingsPath, optional: false);
    }
    
    var configuration = builder.Build();
    var settings = new AppSettings();
    configuration.Bind(settings);
    
    return settings;
}

/// <summary>
/// Determines whether to use real packages based on configuration
/// </summary>
static bool ShouldUseRealPackages(AppSettings config)
{
    if (!config.UseRealPackages)
        return false;
        
    if (string.IsNullOrEmpty(config.GameInstallPath))
        return false;
        
    if (!Directory.Exists(config.GameInstallPath))
        return false;
        
    // Check if this looks like a real Sims 4 install
    var dataPath = Path.Combine(config.GameInstallPath, "Data");
    if (!Directory.Exists(dataPath))
        return false;
        
    // Look for common Sims 4 packages
    var commonPackages = new[] { "Simulation.package", "UI.package", "GameplayData.package" };
    return commonPackages.Any(pkg => File.Exists(Path.Combine(dataPath, pkg)));
}

/// <summary>
/// Analyzes real game packages to find smallest package for each resource type
/// </summary>
static async Task AnalyzeRealPackages(string gameInstallPath, ResourceAnalysisResult result)
{
    var dataPath = Path.Combine(gameInstallPath, "Data");
    var packageFiles = Directory.GetFiles(dataPath, "*.package", SearchOption.AllDirectories);
    
    var resourceTypeToSmallestPackage = new Dictionary<uint, (string Path, long Size)>();
    var allResourceTypes = new HashSet<uint>();
    
    result.Summary.TotalPackages = packageFiles.Length;
    
    foreach (var packagePath in packageFiles)
    {
        try
        {
            var fileInfo = new FileInfo(packagePath);
            var relativePath = Path.GetRelativePath(gameInstallPath, packagePath);
            
            // Simple package parsing - just look for resource type patterns
            var resourceTypes = await ExtractResourceTypesFromPackage(packagePath);
            
            foreach (var resourceType in resourceTypes)
            {
                allResourceTypes.Add(resourceType);
                
                // Track the smallest package containing each resource type
                if (!resourceTypeToSmallestPackage.ContainsKey(resourceType) ||
                    fileInfo.Length < resourceTypeToSmallestPackage[resourceType].Size)
                {
                    resourceTypeToSmallestPackage[resourceType] = (relativePath, fileInfo.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not analyze {packagePath}: {ex.Message}");
        }
    }
    
    // Build result
    foreach (var kvp in resourceTypeToSmallestPackage)
    {
        var resourceTypeHex = $"0x{kvp.Key:X8}";
        result.Resources[resourceTypeHex] = kvp.Value.Path;
    }
    
    result.Summary.TotalResourceTypes = allResourceTypes.Count;
}

/// <summary>
/// Simple resource type extraction from package files
/// This is a basic implementation that looks for DBPF headers and resource entries
/// </summary>
static async Task<HashSet<uint>> ExtractResourceTypesFromPackage(string packagePath)
{
    var resourceTypes = new HashSet<uint>();
    
    using var stream = File.OpenRead(packagePath);
    using var reader = new BinaryReader(stream);
    
    // Check for DBPF header
    var magic = reader.ReadUInt32();
    if (magic != 0x46504244) // "DBPF"
        return resourceTypes;
        
    // Skip version fields
    reader.ReadUInt32(); // Major version
    reader.ReadUInt32(); // Minor version
    reader.ReadUInt32(); // Unknown
    reader.ReadUInt32(); // Unknown
    reader.ReadUInt32(); // Unknown
    reader.ReadUInt32(); // Created date
    reader.ReadUInt32(); // Modified date
    reader.ReadUInt32(); // Index major version
    
    var indexEntryCount = reader.ReadUInt32();
    reader.ReadUInt32(); // Index offset (we'll calculate it)
    reader.ReadUInt32(); // Index size
    
    // Simple approach: look through the file for resource type patterns
    // In a real implementation, we'd parse the index properly
    stream.Position = 0;
    var buffer = new byte[Math.Min(stream.Length, 1024 * 1024)]; // Read first 1MB
    await stream.ReadAsync(buffer, 0, buffer.Length);
    
    // Look for common resource type patterns in the data
    for (int i = 0; i < buffer.Length - 4; i += 4)
    {
        var possibleResourceType = BitConverter.ToUInt32(buffer, i);
        
        // Filter to reasonable resource type values
        if (IsValidResourceType(possibleResourceType))
        {
            resourceTypes.Add(possibleResourceType);
        }
    }
    
    return resourceTypes;
}

/// <summary>
/// Checks if a value looks like a valid Sims 4 resource type
/// </summary>
static bool IsValidResourceType(uint value)
{
    // Resource types are typically non-zero and in certain ranges
    if (value == 0 || value == 0xFFFFFFFF)
        return false;
        
    // Most Sims 4 resource types are in certain ranges
    // This is a heuristic - in practice we'd use the actual DBPF index
    return value > 0x1000 && value < 0xFFFF0000;
}

/// <summary>
/// Uses mock packages when real game install is not available
/// </summary>
static void AnalyzeMockPackages(ResourceAnalysisResult result)
{
    foreach (var mockPackage in mockPackages)
    {
        foreach (var resourceTypeHex in mockPackage.Value)
        {
            // Only add if we don't already have a smaller package for this type
            if (!result.Resources.ContainsKey(resourceTypeHex))
            {
                result.Resources[resourceTypeHex] = mockPackage.Key;
            }
        }
    }
    
    result.Summary.TotalPackages = mockPackages.Count;
    result.Summary.TotalResourceTypes = result.Resources.Count;
}

/// <summary>
/// Categorizes resource types into known and unknown
/// </summary>
static void CategorizeResourceTypes(ResourceAnalysisResult result)
{
    var unknown = new List<string>();
    var known = 0;
    
    foreach (var resourceTypeHex in result.Resources.Keys)
    {
        if (uint.TryParse(resourceTypeHex.Replace("0x", ""), NumberStyles.HexNumber, null, out var resourceType))
        {
            if (knownResourceTypes.Contains(resourceType))
            {
                known++;
            }
            else
            {
                unknown.Add(resourceTypeHex);
            }
        }
    }
    
    result.Unknown = unknown.OrderBy(x => x).ToList();
    result.Summary.KnownResourceTypes = known;
    result.Summary.UnknownResourceTypes = unknown.Count;
}

/// <summary>
/// Function that can be called from golden master tests to get test packages
/// </summary>
public static async Task<Dictionary<string, string>> GetTestPackagesForResourceTypesAsync(IEnumerable<uint> resourceTypes, string? configPath = null)
{
    var analysis = await AnalyzePackagesAsync(configPath);
    var result = new Dictionary<string, string>();
    
    foreach (var resourceType in resourceTypes)
    {
        var resourceTypeHex = $"0x{resourceType:X8}";
        if (analysis.Resources.TryGetValue(resourceTypeHex, out var packagePath))
        {
            result[resourceTypeHex] = packagePath;
        }
    }
    
    return result;
}

// Main execution when run as a script
if (Args.Length == 0 || Args[0] != "--library-mode")
{
    var outputPath = Args.Length > 0 ? Args[0] : "package-analysis-simple.json";
    await AnalyzePackagesAsync(outputPath: outputPath);
}
